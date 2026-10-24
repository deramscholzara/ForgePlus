using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.InputSystem;

namespace ForgePlus.ApplicationGeneral
{
    // ForgePlus's input actions (ForgePlusEditor/Input/ForgePlus.inputactions, the project-wide actions asset),
    // which the Input System enables when it starts
    [AutoStaticsCleanup]
    public static partial class ForgePlusInput
    {
        public static class Camera
        {
            public static InputAction Strafe
            {
                get
                {
                    return Find("Camera/Strafe");
                }
            }

            public static InputAction Elevate
            {
                get
                {
                    return Find("Camera/Elevate");
                }
            }

            public static InputAction Advance
            {
                get
                {
                    return Find("Camera/Advance");
                }
            }

            public static InputAction Turbo
            {
                get
                {
                    return Find("Camera/Turbo");
                }
            }

            public static InputAction Look
            {
                get
                {
                    return Find("Camera/Look");
                }
            }

            public static InputAction EnableLook
            {
                get
                {
                    return Find("Camera/EnableLook");
                }
            }

            public static InputAction WheelMove
            {
                get
                {
                    return Find("Camera/WheelMove");
                }
            }

            public static InputAction FrameSelected
            {
                get
                {
                    return Find("Camera/FrameSelected");
                }
            }

            // O: switches between the perspective view and the top-down (orthographic) view
            public static InputAction ToggleOrthographic
            {
                get
                {
                    return Find("Camera/ToggleOrthographic");
                }
            }

            // Held while pressing Select (the left mouse button), it grabs the level to pan across it
            public static InputAction PanModifier
            {
                get
                {
                    return Find("Camera/PanModifier");
                }
            }

            // The middle mouse button, which grabs the level to pan across it on its own
            public static InputAction Pan
            {
                get
                {
                    return Find("Camera/Pan");
                }
            }
        }

        public static class Editing
        {
            public static InputAction Select
            {
                get
                {
                    return Find("Editing/Select");
                }
            }

            public static InputAction LockX
            {
                get
                {
                    return Find("Editing/LockX");
                }
            }

            public static InputAction LockY
            {
                get
                {
                    return Find("Editing/LockY");
                }
            }

            // Uses the platform in front of the camera as a player's action key would (PlatformSimulationHotkeys)
            public static InputAction UsePlatform
            {
                get
                {
                    return Find("Editing/UsePlatform");
                }
            }

            // Starts or stops the simulation of the selected platforms (PlatformSimulationHotkeys)
            public static InputAction ActivatePlatform
            {
                get
                {
                    return Find("Editing/ActivatePlatform");
                }
            }

            // Obstructs the extension of the selected platforms (PlatformSimulationHotkeys)
            public static InputAction ObstructPlatform
            {
                get
                {
                    return Find("Editing/ObstructPlatform");
                }
            }

            // Deletes or clears what's selected, as the mode has it (SelectionDeletion)
            public static InputAction DeleteSelected
            {
                get
                {
                    return Find("Editing/DeleteSelected");
                }
            }

            public static InputAction NudgeUp
            {
                get
                {
                    return Find("Editing/NudgeUp");
                }
            }

            public static InputAction NudgeDown
            {
                get
                {
                    return Find("Editing/NudgeDown");
                }
            }

            public static InputAction NudgeLeft
            {
                get
                {
                    return Find("Editing/NudgeLeft");
                }
            }

            public static InputAction NudgeRight
            {
                get
                {
                    return Find("Editing/NudgeRight");
                }
            }

            public static InputAction InvertGridSnap
            {
                get
                {
                    return Find("Editing/InvertGridSnap");
                }
            }

            public static InputAction AlignToSelection
            {
                get
                {
                    return Find("Editing/AlignToSelection");
                }
            }

            public static InputAction TargetOuterLayer
            {
                get
                {
                    return Find("Editing/TargetOuterLayer");
                }
            }

            public static InputAction AlignContiguous
            {
                get
                {
                    return Find("Editing/AlignContiguous");
                }
            }

            public static InputAction KeepRelativeHeight
            {
                get
                {
                    return Find("Editing/KeepRelativeHeight");
                }
            }

            public static InputAction KeepTexturesWithPoint
            {
                get
                {
                    return Find("Editing/KeepTexturesWithPoint");
                }
            }

            public static InputAction AllowInvalidGeometry
            {
                get
                {
                    return Find("Editing/AllowInvalidGeometry");
                }
            }

            // Held while clicking (or selecting by rectangle), it adds to the selection rather than replacing it
            public static InputAction AddToSelection
            {
                get
                {
                    return Find("Editing/AddToSelection");
                }
            }

            // Held while selecting by rectangle, it selects what the rectangle touches, rather than only what's wholly in it
            public static InputAction SelectTouching
            {
                get
                {
                    return Find("Editing/SelectTouching");
                }
            }

            // Stops selecting by rectangle, leaving the selection as it was
            public static InputAction CancelSelectionBox
            {
                get
                {
                    return Find("Editing/CancelSelectionBox");
                }
            }
        }

        public static class Interface
        {
            public static InputAction ToggleUI
            {
                get
                {
                    return Find("Interface/ToggleUI");
                }
            }

            public static InputAction ToggleMenu
            {
                get
                {
                    return Find("Interface/ToggleMenu");
                }
            }

            // Ctrl+Z (Command+Z)
            public static InputAction Undo
            {
                get
                {
                    return Find("Interface/Undo");
                }
            }

            // Ctrl+Y, or Ctrl+Shift+Z (Command+Shift+Z), whose Ctrl+Z is also Undo's
            public static InputAction Redo
            {
                get
                {
                    return Find("Interface/Redo");
                }
            }
        }

        // Play mode's (PlayThisLevel), which act only while it's on, as the others don't
        public static class Play
        {
            public const string MapName = "Play";

            public static InputAction Advance
            {
                get
                {
                    return Find("Play/Advance");
                }
            }

            public static InputAction Strafe
            {
                get
                {
                    return Find("Play/Strafe");
                }
            }

            public static InputAction Look
            {
                get
                {
                    return Find("Play/Look");
                }
            }

            // Held, the player walks rather than runs
            public static InputAction Walk
            {
                get
                {
                    return Find("Play/Walk");
                }
            }

            // The player's action key, which uses doors, platforms, switches and terminals
            public static InputAction Action
            {
                get
                {
                    return Find("Play/Action");
                }
            }

            // Held with the head under a liquid, the player swims up
            public static InputAction Swim
            {
                get
                {
                    return Find("Play/Swim");
                }
            }

            // Reading a terminal, on to its next page (or out of it, after its last)
            public static InputAction NextPage
            {
                get
                {
                    return Find("Play/NextPage");
                }
            }

            public static InputAction PreviousPage
            {
                get
                {
                    return Find("Play/PreviousPage");
                }
            }

            // Leaves the terminal being read, or else Play mode
            public static InputAction Exit
            {
                get
                {
                    return Find("Play/Exit");
                }
            }
        }

        // Looked up once each, since they're read every frame
        private static Dictionary<string, InputAction> actionsByPath;

        // The action maps they were found in. Reimporting the actions asset (even during a Play session) replaces its maps,
        // leaving the found actions without input state, so they're found again.
        // The asset owns the map, so it isn't disposed (resetting actionsByPath is enough to find it again).
        [NoAutoStaticsCleanup]
        private static InputActionMap firstActionMap;

        private static InputAction Find(string actionPath)
        {
            var asset = InputSystem.actions;
            var currentFirstActionMap = asset.actionMaps.Count > 0 ? asset.actionMaps[0] : null;

            if (actionsByPath == null || firstActionMap != currentFirstActionMap)
            {
                actionsByPath = new Dictionary<string, InputAction>();
                firstActionMap = currentFirstActionMap;

                // The Inputs tab's changes, which new maps don't have
                InputBindings.Load(asset);
            }

            if (!actionsByPath.TryGetValue(actionPath, out var action))
            {
                action = asset.FindAction(actionPath, throwIfNotFound: true);
                actionsByPath[actionPath] = action;
            }

            return action;
        }
    }
}
