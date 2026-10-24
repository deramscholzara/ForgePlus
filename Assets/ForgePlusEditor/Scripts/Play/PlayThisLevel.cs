using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using ForgePlus.PolygonContainment;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Entities.MapObjects;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using static AlephOne.player;

namespace ForgePlus.Play
{
    // Play mode ("Play This Level"): the level seen and walked around as a player would, from one of its player starts
    // picked at random, with the UI hidden and the editor's inputs off (its own, ForgePlusInput.Play, on), until Exit
    // (Escape) ends it and puts everything back as it was. Its view is culled as Aleph One's is (Culling Simulation,
    // from the camera). The player's action key uses doors, platforms, switches and terminals (a terminal is read in
    // the Play layer). Teleporters (and terminals' teleports) move the player within the level at once, and a teleport
    // out of the level ends Play mode. Nothing else in the game (monsters, items, weapons, the HUD) is simulated.
    // Nothing played is an edit: the level's data never changes (PlayWorld), and the undo history records nothing
    // meanwhile.
    //
    // The session itself (the hidden UI, the inputs, the camera taking a player's view, culling, and putting it all
    // back) is kept apart from what moves the player (PlayPlayer, in PlayWorld), so another source of the player's view
    // and the level's state (such as a game run by Aleph One itself) could drive the same session.
    [AutoStaticsCleanup]
    public static partial class PlayThisLevel
    {
        // Aleph One's normal field of view (in degrees), across a 4:3 view, so a wider view sees more at its sides
        private const float FieldOfView = 80f;
        private const float FieldOfViewAspect = 4f / 3f;

        // The most ticks a slow frame catches up on
        private const int MaximumTicksPerFrame = 10;

        private const float TickDuration = 1f / map.TICKS_PER_SECOND;

        // The editor's action maps, which don't act during Play mode
        private static readonly string[] EditorActionMaps = { "Camera", "Editing", "Interface" };

        private static PlayWorld world;
        private static PlayPlayer player;
        private static float tickTime;
        private static bool isBeginning;

        private static PanelSlot terminalSlot;
        private static PlayTerminalPages terminalPages;
        private static bool loadedTerminalStrings;

        private static CullingSimulation.Modes previousCullingMode;
        private static int previousCullingMask;
        private static CursorLockMode previousCursorLockState;
        private static bool previousCursorVisible;
        private static bool wasWorldPointerEnabled;

        public static bool IsActive
        {
            get
            {
                return world != null;
            }
        }

        // With a level open, that has a player start to begin at
        // With a level open (which it can begin in if it has a player start, or from here, if here is in it)
        public static bool CanBegin
        {
            get
            {
                return !IsActive && !isBeginning && LevelEntity_Level.Instance && !LevelEditing.IsRebuildPending;
            }
        }

        public static bool HasPlayerStarts
        {
            get
            {
                return LevelEntity_Level.Instance && GetPlayerStarts(LevelEntity_Level.Instance).Count > 0;
            }
        }

        // Whether "here" (the Culling Simulation's reference point, or else the camera) is in a polygon, between its
        // floor and ceiling (as the level is edited, with a platform's whole travel counted)
        public static bool CanBeginFromHere
        {
            get
            {
                return CanBegin && TryGetStartHere(out _);
            }
        }

        // At one of the level's player starts, picked at random, or from here: on the floor below, in the polygon here
        // is in, facing the way the camera does
        public static async void Begin(bool fromHere = false)
        {
            if (!CanBegin)
            {
                return;
            }

            isBeginning = true;

            try
            {
                // A terminal's text uses the Terminals table, which is only loaded in Terminals mode
                if (!Strings.IsLoaded(Strings.Terminals))
                {
                    await Strings.LoadTableAsync(Strings.Terminals);
                    loadedTerminalStrings = true;
                }
            }
            finally
            {
                isBeginning = false;
            }

            var level = LevelEntity_Level.Instance;
            map_object start = null;
            if (level && !IsActive)
            {
                if (fromHere)
                {
                    TryGetStartHere(out start);
                }
                else
                {
                    var starts = GetPlayerStarts(level);
                    start = starts.Count > 0 ? starts[Random.Range(0, starts.Count)] : null;
                }
            }

            if (start == null)
            {
                ReleaseTerminalStrings();
                return;
            }

#if !NO_EDITING
            // Whatever was being edited is recorded before anything stops being recorded
            History.UndoHistory.Recorder?.RecordPendingEdit();
#endif

            world = new PlayWorld(level);
            world.Begin();
            world.OnTerminalUsed += OpenTerminal;

            player = new PlayPlayer(world, PhysicsLoading.Instance.Model, start);
            world.SetPlayer(player);
            tickTime = 0f;

            var ui = ForgePlusUI.Instance;
            terminalSlot = new PanelSlot(ui.SetPlaying(true));

            var editorCamera = ui.EditorCamera;
            var viewCamera = editorCamera.GetComponent<Camera>();
            editorCamera.BeginExternalControl();
            viewCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(FieldOfView, FieldOfViewAspect);
            previousCullingMask = viewCamera.cullingMask;
            viewCamera.cullingMask &= ~(1 << SelectionManager.SelectionIndicatorLayer);

            previousCullingMode = CullingSimulation.Mode;
            CullingSimulation.Mode = CullingSimulation.Modes.Camera;

            var actions = InputSystem.actions;
            foreach (var mapName in EditorActionMaps)
            {
                actions.FindActionMap(mapName)?.Disable();
            }

            actions.FindActionMap(ForgePlusInput.Play.MapName)?.Enable();

            var worldPointer = WorldPointer.Instance;
            wasWorldPointerEnabled = worldPointer && worldPointer.enabled;
            if (worldPointer)
            {
                worldPointer.enabled = false;
            }

            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            ApplyView(0f);
        }

        // Everything goes back as it was before Play mode began, apart from the level's lights, media and platforms,
        // which start over
        public static void End()
        {
            if (!IsActive)
            {
                return;
            }

            CloseTerminal();
            terminalSlot = null;

#if !NO_EDITING
            PlayTargetHighlight.Clear();
#endif

            // Ended either way, so a level that can't be put back (or anything after it) doesn't keep it going
            var endingWorld = world;
            world = null;
            player = null;

            endingWorld.OnTerminalUsed -= OpenTerminal;
            endingWorld.SetPlayer(null);

            try
            {
                endingWorld.End();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }

            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;

            var worldPointer = WorldPointer.Instance;
            if (worldPointer)
            {
                worldPointer.enabled = wasWorldPointerEnabled;
            }

            var actions = InputSystem.actions;
            foreach (var mapName in EditorActionMaps)
            {
                actions.FindActionMap(mapName)?.Enable();
            }

            CullingSimulation.Mode = previousCullingMode;

            var ui = ForgePlusUI.Instance;
            if (ui)
            {
                var editorCamera = ui.EditorCamera;
                if (editorCamera)
                {
                    editorCamera.GetComponent<Camera>().cullingMask = previousCullingMask;
                    editorCamera.EndExternalControl();
                }

                ui.SetPlaying(false);
            }

            ReleaseTerminalStrings();
        }

        // Once a frame (from ForgePlusUI, before culling): input, then as many ticks as have passed, then the view
        public static void Update()
        {
            if (!IsActive)
            {
                return;
            }

            if (!LevelEntity_Level.Instance || LevelEntity_Level.Instance != world.Level || LevelEditing.IsRebuildPending)
            {
                // The level went away (or is being rebuilt) underneath it
                End();
                return;
            }

            if (terminalPages != null)
            {
                UpdateTerminal();
            }
            else
            {
                if (ForgePlusInput.Play.Exit.WasPressedThisFrame())
                {
                    End();
                    return;
                }

                var look = ForgePlusInput.Play.Look.ReadValue<Vector2>();
                player.Turn(look.x, look.y);

                if (ForgePlusInput.Play.Action.WasPressedThisFrame())
                {
                    UseActionKey();
                }
            }

            if (!IsActive)
            {
                // A terminal teleported the player out of the level
                return;
            }

            tickTime += Time.unscaledDeltaTime;

            var ticks = 0;
            while (tickTime >= TickDuration && ticks < MaximumTicksPerFrame)
            {
                tickTime -= TickDuration;
                ticks++;

                world.SyncPlatformHeights(player);
                player.Tick(terminalPages != null ? 0 : GetActionFlags());

                if (terminalPages == null && !UpdateTeleporter())
                {
                    // Teleported out of the level
                    return;
                }
            }

            if (ticks == MaximumTicksPerFrame)
            {
                tickTime = 0f;
            }

            if (ticks > 0)
            {
                world.SyncSwitches();
            }

            UpdateTargetHighlight();

            ApplyView(tickTime / TickDuration);
        }

        // update_player_teleport (player.cpp), without its effects: a player standing still in a teleporter is at its
        // destination at once. One that leads to another level ends Play mode. An automatic exit only leads out once
        // the level's goals are met, which aren't tracked, so it never does. Returns whether Play mode goes on.
        private static bool UpdateTeleporter()
        {
            var polygon = world.Map.PolygonList[player.PolygonIndex];
            if (polygon.type != map._polygon_is_teleporter || !player.IsStandingStill)
            {
                return true;
            }

            return Teleport(polygon.permutation);
        }

        // To a polygon in the level, or (for a negative destination, as Aleph One encodes another level) out of it,
        // which ends Play mode. Returns whether Play mode goes on.
        private static bool Teleport(short destination)
        {
            if (destination < 0)
            {
                End();
                return false;
            }

            if (destination < world.Map.PolygonList.Count)
            {
                player.TeleportTo(destination);
            }

            return true;
        }

        private static void ApplyView(float tickFraction)
        {
            var cameraTransform = ForgePlusUI.Instance.EditorCamera.transform;
            cameraTransform.SetPositionAndRotation(player.GetCameraPosition(tickFraction), player.CameraRotation);
        }

        // The player's movement keys, as action flags (run, unless walking)
        private static uint GetActionFlags()
        {
            uint flags = 0;

            var advance = ForgePlusInput.Play.Advance.ReadValue<float>();
            if (advance > 0f) flags |= _moving_forward;
            if (advance < 0f) flags |= _moving_backward;

            var strafe = ForgePlusInput.Play.Strafe.ReadValue<float>();
            if (strafe < 0f) flags |= _sidestepping_left;
            if (strafe > 0f) flags |= _sidestepping_right;

            if (!ForgePlusInput.Play.Walk.IsPressed()) flags |= _run_dont_walk;
            if (ForgePlusInput.Play.Swim.IsPressed()) flags |= _swim;

            return flags;
        }

        // update_action_key (devices.cpp)
        // Unlike the game, it uses what the player's looking at however far away it is (and a switch only projectiles can
        // hit, from anywhere), as switches meant to be shot at from afar can't be otherwise
        private static void UseActionKey()
        {
            var target = FindLookedAtTarget();

            switch (target.Type)
            {
                case ActionKeyTargeting.TargetTypes.Platform:
                    target.Platform.PlayerTouchRuntimeBehavior();
                    break;
                case ActionKeyTargeting.TargetTypes.ControlPanel:
                    world.UsePanel(target.SideIndex);
                    break;
                default:
                    if (target.UnusableSideIndex != cstypes.NONE)
                    {
                        world.PlayUnusableSound(target.UnusableSideIndex);
                    }

                    break;
            }
        }

        // What the action key can use in front of the player, however far, and whether the game's action key would reach
        // it (find_action_key_target in devices.cpp)
        private static ActionKeyTargeting.Target FindLookedAtTarget()
        {
            return FindTarget(ignoresReach: true);
        }

        private static ActionKeyTargeting.Target FindTarget(bool ignoresReach)
        {
            return ActionKeyTargeting.Find(world.Level, world.Map, player.PolygonIndex,
                new world_point2d(player.Location.x, player.Location.y), player.Facing, player.ReachOrigin,
                sideIndex => world.CanTogglePanel(sideIndex, withActionKey: !ignoresReach), ignoresReach);
        }

        // The looked-at target is overlaid, green if the game's action key would use it, or else yellow
        private static void UpdateTargetHighlight()
        {
#if !NO_EDITING
            if (terminalPages != null)
            {
                PlayTargetHighlight.Clear();
                return;
            }

            var target = FindLookedAtTarget();
            var isInReach = target.Type != ActionKeyTargeting.TargetTypes.None && target.Matches(FindTarget(ignoresReach: false));
            PlayTargetHighlight.Show(target, isInReach);
#endif
        }

        private static void OpenTerminal(short terminalIndex)
        {
            var terminal = computer_interface.get_indexed_terminal_data(world.Map, terminalIndex);
            if (terminal == null)
            {
                return;
            }

            terminalPages = new PlayTerminalPages(terminal, ActivateTag, TeleportFromTerminal);
            terminalPages.OnFinished += CloseTerminal;
            terminalSlot.Show(new TerminalPanel(terminalPages));

            // Which may end it at once (with nothing to read), and even teleport the player
            terminalPages.Start();
        }

        private static void UpdateTerminal()
        {
            if (ForgePlusInput.Play.Exit.WasPressedThisFrame())
            {
                CloseTerminal();
            }
            else if (ForgePlusInput.Play.NextPage.WasPressedThisFrame())
            {
                terminalPages.Next();
            }
            else if (ForgePlusInput.Play.PreviousPage.WasPressedThisFrame())
            {
                terminalPages.Back();
            }
        }

        private static void CloseTerminal()
        {
            if (terminalPages != null)
            {
                terminalPages.OnFinished -= CloseTerminal;
                terminalPages = null;
            }

            terminalSlot?.Hide();
        }

        // A terminal's teleport group, once its reading has ended
        private static void TeleportFromTerminal(short destination)
        {
            if (IsActive)
            {
                Teleport(destination);
            }
        }

        // A terminal's tag group (computer_interface.cpp): its tagged lights and platforms turn on
        private static void ActivateTag(short tag)
        {
            world.ActivateTag(tag);
        }

        private static void ReleaseTerminalStrings()
        {
            if (loadedTerminalStrings && ModeManager.Instance.PrimaryMode != ModeManager.PrimaryModes.Terminals)
            {
                Strings.ReleaseTable(Strings.Terminals);
            }

            loadedTerminalStrings = false;
        }

        // A player start on the floor straight below "here" (CanBeginFromHere), facing the camera's way. The player is
        // kept out of walls as it's placed (PlayPlayer), so one too close to a wall starts just clear of it.
        private static bool TryGetStartHere(out map_object start)
        {
            start = null;

            var ui = ForgePlusUI.Instance;
            var containment = PolygonContainmentMap.Current;
            if (!ui || containment == null || ui.EditorCamera.IsOrthographic)
            {
                return false;
            }

            // The reference point if it's in a polygon, or else the camera
            var cameraTransform = ui.EditorCamera.transform;
            var point = PolygonContainmentTracker.ToWorldPoint(cameraTransform.position);
            var polygonIndex = cstypes.NONE;

            if (CullingSimulation.TryGetReferencePoint(out var referencePosition))
            {
                point = PolygonContainmentTracker.ToWorldPoint(referencePosition);
                polygonIndex = containment.FindPolygon(point, withHeight: true);

                if (polygonIndex == cstypes.NONE)
                {
                    point = PolygonContainmentTracker.ToWorldPoint(cameraTransform.position);
                }
            }

            if (polygonIndex == cstypes.NONE)
            {
                polygonIndex = containment.FindPolygon(point, withHeight: true);
            }

            if (polygonIndex == cstypes.NONE)
            {
                return false;
            }

            // The camera's yaw is Aleph One's facing turned a quarter (PlayPlayer.CameraRotation)
            var facing = AlephOne.world.NORMALIZE_ANGLE(Mathf.RoundToInt((cameraTransform.eulerAngles.y - 90f) * AlephOne.world.FULL_CIRCLE / 360f));

            start = new map_object
            {
                type = map._saved_player,
                polygon_index = polygonIndex,
                location = new world_point3d((short) point.x, (short) point.y, 0),
                facing = facing,
            };

            return true;
        }

        private static List<map_object> GetPlayerStarts(LevelEntity_Level level)
        {
            var starts = new List<map_object>();
            foreach (var mapObject in level.MapObjects.Values)
            {
                var savedObject = mapObject.NativeObject;
                if (savedObject.type == map._saved_player && savedObject.polygon_index >= 0 &&
                    savedObject.polygon_index < level.Level.PolygonList.Count)
                {
                    starts.Add(savedObject);
                }
            }

            return starts;
        }
    }
}
