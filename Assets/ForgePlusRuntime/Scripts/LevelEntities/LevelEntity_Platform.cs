using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Common;
using System;
using System.Threading;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using AlephOne;
using static AlephOne.platforms;
using static AlephOne.map;

namespace RuntimeCore.Entities.Geometry
{
    [AutoStaticsCleanup]
    public partial class LevelEntity_Platform : LevelEntity_Base, IDestructionPreparable, ISelectable, IInspectable
    {
        public enum LinkedSurfaces
        {
            Floor,
            Ceiling,
        }

        public enum States
        {
            Extending,
            Extended,
            Contracting,
            Contracted,
        }

        // TODO: Implement Crushing as a test -event-?
        //       What should trigger this?
        //       A button in the inspector "Impact"
        //       & "Stop Impact" - no "Stop Impact" if it reverses?

        public new platform_data NativeObject => base.NativeObject as platform_data;

        // TODO: Add this to IInspectable so it must be implemented in all inspectables
        public event Action<LevelEntity_Platform> OnInspectionStateChange;

        // In each frame it moves in, before LateUpdate, so the surfaces that follow or are trimmed by it update then
        // (PlatformConstraint, PlatformSideClipping)
        public event Action Moved;

        // play_platform_sound (platforms.cpp): the platform, sound code, whether it's extending, and whether it's fully
        // contracted. A split platform's come from its floor only.
        public static event Action<LevelEntity_Platform, short, bool, bool> OnRuntimeSound;

        // Whether a platform may move its half to a height (meters), as legal_polygon_height_change (map.cpp) checks that
        // nothing in its polygon is crushed, or null while nothing can be (such as outside Play mode)
        public static Func<LevelEntity_Platform, float, bool> IsHeightChangeLegal;

        // TODO: Use this for checking "is active" state for toggling?
        private CancellationTokenSource platformBehaviorCTS;

        private LinkedSurfaces linkedSurface;
        private float speed = 1f;
        private float contractingSpeed = 1f;
        private float delay = 1f;
        private float extendedPosition = 0f;
        private float contractedPosition = 1f;

        private float currentPosition;

        private States currentState = States.Contracted;

        private float remainingStateTime = 0f;

        // An initially active platform starts moving without its starting sound (platforms.cpp: new_platform), as does
        // one reversing after being obstructed
        private bool startsMovingSilently;

        // A move that went nowhere stops without its stopping sound
        private bool lastMoveTraveled;

        // As PLATFORM_HAS_BEEN_ACTIVATED, for one that activates only once (since its simulation began)
        private bool hasBeenActivated;

        // The platform that turned it on (platform_data.parent_platform_index), or NONE
        private short parentPlatformIndex = cstypes.NONE;

        // The frame its state was last changed in (as PLATFORM_WAS_JUST_ACTIVATED_OR_DEACTIVATED marks the tick)
        private int lastStateChangeFrame = -1;

        public float CurrentHeightInWorldUnitIncrements
        {
            get
            {
                return currentPosition * GeometryUtilities.WorldUnitIncrementsPerMeter;
            }
        }

        // As PLATFORM_HAS_BEEN_ACTIVATED (since its simulation began)
        public bool HasBeenRuntimeActivated
        {
            get
            {
                return hasBeenActivated;
            }
        }

        // Which of its polygon's surfaces it moves
        public LinkedSurfaces LinkedSurface
        {
            get
            {
                return linkedSurface;
            }
        }

        public bool IsRuntimeActive
        {
            get
            {
                return platformBehaviorCTS != null;
            }
        }

        // As PLATFORM_IS_ACTIVE and PLATFORM_IS_MOVING (rather than waiting at either end)
        public bool IsRuntimeMoving
        {
            get
            {
                return IsRuntimeActive && (currentState == States.Extending || currentState == States.Contracting);
            }
        }

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank - no current reason to toggle this, as its selection comes from already-gated EditableSurface components
        }

        public void Inspect()
        {
            var inspector = new Inspector_Platform(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void PrepareForDestruction()
        {
            DeactivateRuntimeBehavior();
        }

        public override void InitializeEntity(LevelEntity_Level parentLevel, short nativeIndex, object nativeObject)
        {
            base.InitializeEntity(parentLevel, nativeIndex, nativeObject);
        }

        public void UpdatePlatformValues(LinkedSurfaces linkedSurface)
        {
            this.linkedSurface = linkedSurface;

            // Platforms contracting slower move a quarter of their speed, rounded down (platforms.cpp: update_platforms)
            speed = GetSpeedInMetersPerSecond(NativeObject.speed);
            contractingSpeed = GetSpeedInMetersPerSecond(PLATFORM_CONTRACTS_SLOWER(NativeObject.static_flags) ? (short)(NativeObject.speed >> 2) : NativeObject.speed);

            delay = (float)NativeObject.delay / TICKS_PER_SECOND;

            // A platform that goes both ways already meets at the midpoint in its extrema (platforms.cpp: calculate_platform_extrema)
            if (linkedSurface == LinkedSurfaces.Floor)
            {
                extendedPosition = (float)NativeObject.maximum_floor_height / GeometryUtilities.WorldUnitIncrementsPerMeter;
                contractedPosition = (float)NativeObject.minimum_floor_height / GeometryUtilities.WorldUnitIncrementsPerMeter;
            }
            else
            {
                extendedPosition = (float)NativeObject.minimum_ceiling_height / GeometryUtilities.WorldUnitIncrementsPerMeter;
                contractedPosition = (float)NativeObject.maximum_ceiling_height / GeometryUtilities.WorldUnitIncrementsPerMeter;
            }
        }

        public void BeginRuntimeStyleBehavior()
        {
            startsMovingSilently = true;
            hasBeenActivated = false;
            parentPlatformIndex = cstypes.NONE;
            lastStateChangeFrame = -1;

            currentState = PLATFORM_IS_INITIALLY_EXTENDED(NativeObject.static_flags) ? States.Extended : States.Contracted;

            SetCurrentPosition(PLATFORM_IS_INITIALLY_EXTENDED(NativeObject.static_flags) ? extendedPosition : contractedPosition);

            if (PLATFORM_IS_INITIALLY_ACTIVE(NativeObject.static_flags))
            {
                ActivateRuntimeBehavior();
            }
            else
            {
                DeactivateRuntimeBehavior();
            }
        }

        public void SetRuntimeActive(bool value, bool isRootActivation = true)
        {
            if (value)
            {
                hasBeenActivated = true;
            }

            // Deactivating one that's moving stops it as if obstructed (set_platform_state, in platforms.cpp)
            if (!value && IsRuntimeMoving)
            {
                PlayRuntimeSound(_obstructed_sound);
            }

            if (value)
            {
                ActivateRuntimeBehavior();
            }
            else
            {
                DeactivateRuntimeBehavior();
            }

            if (isRootActivation)
            {
                // Activate opposed platform if this is a split platform
                if (linkedSurface == LinkedSurfaces.Floor)
                {
                    if (PLATFORM_COMES_FROM_CEILING(NativeObject.static_flags))
                    {
                        ParentLevel.CeilingPlatforms[NativeIndex].SetRuntimeActive(value, isRootActivation: false);
                    }
                }
                else
                {
                    if (PLATFORM_COMES_FROM_FLOOR(NativeObject.static_flags))
                    {
                        ParentLevel.FloorPlatforms[NativeIndex].SetRuntimeActive(value, isRootActivation: false);
                    }
                }
            }
        }

        // set_platform_state (platforms.cpp): as the game turns a platform on or off (rather than as the inspector's Active
        // and Activate Platform simulate it alone), with what that does to other things. Turning on, it turns its
        // polygon's lights on (if it activates its light), remembers the platform that turned it on (its parent), and turns
        // adjacent platforms on or off (as it activates or deactivates them as it activates); turning off, likewise for
        // the lights it deactivates and the adjacent platforms it changes as it deactivates. One that activates only once
        // can't be turned on again, and none changes twice in a frame (a tick, in the game), so adjacent platforms can't
        // keep turning each other on and off. Returns whether it's on now.
        public bool SetRuntimeState(bool state, short parentPlatformIndex)
        {
            var flags = NativeObject.static_flags;

            if (lastStateChangeFrame == Time.frameCount ||
                (state && PLATFORM_ACTIVATES_ONLY_ONCE(flags) && hasBeenActivated) ||
                state == IsRuntimeActive)
            {
                return IsRuntimeActive;
            }

            // The state of this platform cannot be changed again this tick
            lastStateChangeFrame = Time.frameCount;

            if (state)
            {
                if (PLATFORM_ACTIVATES_LIGHT(flags))
                {
                    SetPolygonLightsRuntimeStatus(true);
                }

                this.parentPlatformIndex = parentPlatformIndex;

                if (PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_WHEN_ACTIVATING(flags)) SetAdjacentRuntimeStates(true);
                if (PLATFORM_DEACTIVATES_ADJACENT_PLATFORMS_WHEN_ACTIVATING(flags)) SetAdjacentRuntimeStates(false);
            }
            else
            {
                if (PLATFORM_DEACTIVATES_LIGHT(flags))
                {
                    SetPolygonLightsRuntimeStatus(false);
                }

                if (PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_WHEN_DEACTIVATING(flags)) SetAdjacentRuntimeStates(true);
                if (PLATFORM_DEACTIVATES_ADJACENT_PLATFORMS_WHEN_DEACTIVATING(flags)) SetAdjacentRuntimeStates(false);
            }

            SetRuntimeActive(state);

            return state;
        }

        // update_platforms (platforms.cpp), as a platform reaches either level: it turns adjacent platforms on if it does
        // that at each level, and then itself off if it deactivates at each level, or at its initial one (which it's at,
        // if isInitialLevel). Only once for a split platform (from its primary half). Returns whether it's off now.
        private bool ReachLevel(bool isInitialLevel)
        {
            var flags = NativeObject.static_flags;
            var deactivates = PLATFORM_DEACTIVATES_AT_EACH_LEVEL(flags) || (isInitialLevel && PLATFORM_DEACTIVATES_AT_INITIAL_LEVEL(flags));

            if (this != PrimaryHalf)
            {
                if (deactivates)
                {
                    DeactivateRuntimeBehavior();
                }

                return deactivates;
            }

            if (PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_AT_EACH_LEVEL(flags))
            {
                SetAdjacentRuntimeStates(true);
            }

            if (deactivates)
            {
                // Not a change it's already made this frame, which would leave it running
                lastStateChangeFrame = -1;
                SetRuntimeState(false, cstypes.NONE);
            }

            return deactivates;
        }

        // set_adjacent_platform_states (platforms.cpp): every platform beside its polygon, as their parent, apart from its own
        // parent if it doesn't activate its parent
        private void SetAdjacentRuntimeStates(bool state)
        {
            var level = ParentLevel.Level;
            var polygon = level.PolygonList[NativeObject.polygon_index];

            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var adjacentPolygonIndex = polygon.adjacent_polygon_indexes[i];
                if (adjacentPolygonIndex < 0 || adjacentPolygonIndex >= level.PolygonList.Count ||
                    level.PolygonList[adjacentPolygonIndex].type != _polygon_is_platform)
                {
                    continue;
                }

                var adjacentPlatformIndex = level.PolygonList[adjacentPolygonIndex].permutation;
                if (!PLATFORM_DOES_NOT_ACTIVATE_PARENT(NativeObject.static_flags) || parentPlatformIndex != adjacentPlatformIndex)
                {
                    var adjacentPlatform = GetSelectablePlatform(ParentLevel, adjacentPlatformIndex);
                    if (adjacentPlatform)
                    {
                        adjacentPlatform.SetRuntimeState(state, NativeIndex);
                    }
                }
            }
        }

        // set_light_status (lightsource.cpp) for its polygon's floor and ceiling lights
        private void SetPolygonLightsRuntimeStatus(bool status)
        {
            var polygon = ParentLevel.Level.PolygonList[NativeObject.polygon_index];

            if (ParentLevel.Lights.TryGetValue(polygon.floor_lightsource_index, out var floorLight))
            {
                floorLight.SetRuntimeStatus(status);
            }

            if (ParentLevel.Lights.TryGetValue(polygon.ceiling_lightsource_index, out var ceilingLight))
            {
                ceilingLight.SetRuntimeStatus(status);
            }
        }

        // A split platform's halves are one platform, so both reverse (the obstructed one playing the sound)
        public void ObstructRuntimeBehavior()
        {
            ObstructRuntimeBehavior(playsSound: true);

            OtherHalf?.ObstructRuntimeBehavior(playsSound: false);
        }

        private void ObstructRuntimeBehavior(bool playsSound)
        {
            if (IsRuntimeActive)
            {
                if (currentState == States.Extending &&
                    PLATFORM_REVERSES_DIRECTION_WHEN_OBSTRUCTED(NativeObject.static_flags))
                {
                    // It reverses without its starting sound
                    if (playsSound)
                    {
                        OnRuntimeSound?.Invoke(this, _obstructed_sound, true, false);
                    }

                    startsMovingSilently = true;
                    BeginState(States.Contracting, loop: false);
                }
            }
        }

        // Whether a player could use it (find_action_key_target in devices.cpp, with platform_is_legal_player_target)
        public bool IsLegalPlayerTarget
        {
            get
            {
                return platform_is_legal_player_target(ParentLevel.Level, NativeIndex, hasBeenActivated);
            }
        }

        // As a player's action key does (player_touch_platform_state in platforms.cpp), for both halves of a split
        // platform. ForgePlus: the player is taken to have any key item it needs.
        public void PlayerTouchRuntimeBehavior()
        {
            var flags = NativeObject.static_flags;

            if (!PLATFORM_IS_PLAYER_CONTROLLABLE(flags))
            {
                SoundHalf.PlayRuntimeSound(_uncontrollable_sound);
                return;
            }

            if (!IsRuntimeActive)
            {
                SetRuntimeState(true, cstypes.NONE);
                return;
            }

            if (PLATFORM_CANNOT_BE_EXTERNALLY_DEACTIVATED(flags))
            {
                SoundHalf.PlayRuntimeSound(_uncontrollable_sound);
                return;
            }

            if (!IsRuntimeMoving)
            {
                // Waiting at either end, it starts moving again now (its delay set to nothing)
                RestartRuntimeBehavior();
                OtherHalf?.RestartRuntimeBehavior();
            }
            else if (PLATFORM_REVERSES_DIRECTION_WHEN_OBSTRUCTED(flags))
            {
                // It reverses, starting again with its starting sound
                ReverseRuntimeBehavior();
                OtherHalf?.ReverseRuntimeBehavior();
            }
            else
            {
                SoundHalf.PlayRuntimeSound(_uncontrollable_sound);
            }
        }

        private void RestartRuntimeBehavior()
        {
            if (IsRuntimeActive && !IsRuntimeMoving)
            {
                BeginState(currentState == States.Extended ? States.Contracting : States.Extending, loop: false);
            }
        }

        private void ReverseRuntimeBehavior()
        {
            if (IsRuntimeMoving)
            {
                BeginState(currentState == States.Extending ? States.Contracting : States.Extending, loop: false);
            }
        }

        // The other half of a split platform (its ceiling's, for its floor's), or null
        private LevelEntity_Platform OtherHalf
        {
            get
            {
                if (!PLATFORM_GOES_BOTH_WAYS(NativeObject.static_flags))
                {
                    return null;
                }

                var halves = linkedSurface == LinkedSurfaces.Floor ? ParentLevel.CeilingPlatforms : ParentLevel.FloorPlatforms;

                return halves.TryGetValue(NativeIndex, out var otherHalf) ? otherHalf : null;
            }
        }

        // A split platform's sounds come from its floor (PlayRuntimeSound)
        private LevelEntity_Platform SoundHalf
        {
            get
            {
                return linkedSurface == LinkedSurfaces.Ceiling && OtherHalf ? OtherHalf : this;
            }
        }

        // The half that speaks for a split platform, as one platform (its floor's, as for its sounds)
        private LevelEntity_Platform PrimaryHalf
        {
            get
            {
                return SoundHalf;
            }
        }

        private void ActivateRuntimeBehavior()
        {
            if (currentState == States.Extended)
            {
                BeginState(States.Contracting, loop: false);
            }
            else if (currentState == States.Contracted)
            {
                BeginState(States.Extending, loop: false);
            }
            else
            {
                BeginState(currentState, loop: false);
            }

            OnInspectionStateChange?.Invoke(this);
        }

        public void DeactivateRuntimeBehavior()
        {
            platformBehaviorCTS?.Cancel();
            platformBehaviorCTS = null;

            OnInspectionStateChange?.Invoke(this);
        }

        public async void BeginState(States state, bool loop = false)
        {
            platformBehaviorCTS?.Cancel();

            platformBehaviorCTS = new CancellationTokenSource();
            var cancellationToken = platformBehaviorCTS.Token;

            remainingStateTime = 0f;

            currentState = state;

            if (!loop && PLATFORM_DELAYS_BEFORE_ACTIVATION(NativeObject.static_flags) &&
                (state == States.Extended || state == States.Contracted))
            {
                // TODO: Should this also delay if reactivating during the Extending and Contracting states?
                await Hold(cancellationToken, delay, currentPosition);
            }

            while (!cancellationToken.IsCancellationRequested && Application.isPlaying)
            {
                switch (currentState)
                {
                    case States.Extended:
                        await Hold(cancellationToken, delay, extendedPosition);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        if (!loop)
                        {
                            currentState = States.Contracting;
                        }

                        break;
                    case States.Extending:
                        await Move(cancellationToken, speed, extendedPosition);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        if (lastMoveTraveled)
                        {
                            PlayRuntimeSound(_stopping_sound);
                        }

                        if (loop)
                        {
                            SetCurrentPosition(contractedPosition);
                        }
                        else
                        {
                            currentState = States.Extended;

                            if (ReachLevel(PLATFORM_IS_INITIALLY_EXTENDED(NativeObject.static_flags)))
                            {
                                return;
                            }
                        }

                        break;
                    case States.Contracted:
                        await Hold(cancellationToken, delay, contractedPosition);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        if (!loop)
                        {
                            currentState = States.Extending;
                        }

                        break;
                    case States.Contracting:
                        await Move(cancellationToken, contractingSpeed, contractedPosition);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        if (lastMoveTraveled)
                        {
                            PlayRuntimeSound(_stopping_sound);
                        }

                        if (loop)
                        {
                            SetCurrentPosition(extendedPosition);
                        }
                        else
                        {
                            currentState = States.Contracted;

                            if (ReachLevel(!PLATFORM_IS_INITIALLY_EXTENDED(NativeObject.static_flags)))
                            {
                                return;
                            }
                        }

                        break;
                }
            }
        }

        private async Awaitable Hold(CancellationToken cancellationToken, float duration, float holdPosition)
        {
            SetCurrentPosition(holdPosition);

            var endTime = Time.realtimeSinceStartup + duration;
            while (GetStateOffsetRealTimeSinceStartup() < endTime)
            {
                await Awaitable.NextFrameAsync();

                if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                {
                    return;
                }
            }

            remainingStateTime = GetStateOffsetRealTimeSinceStartup() - endTime;
        }

        private async Awaitable Move(CancellationToken cancellationToken, float speed, float targetPosition)
        {
            var startsSilently = startsMovingSilently;
            startsMovingSilently = false;
            lastMoveTraveled = false;

            if (currentPosition == targetPosition)
            {
                // Already there, so there's no time to spend moving
                return;
            }

            lastMoveTraveled = true;

            if (!startsSilently)
            {
                PlayRuntimeSound(_starting_sound);
            }

            if (speed <= 0f)
            {
                // Aleph One keeps a platform with no speed moving without it getting anywhere (platforms.cpp: update_platforms)
                while (!cancellationToken.IsCancellationRequested && Application.isPlaying)
                {
                    await Awaitable.NextFrameAsync();
                }

                return;
            }

            var duration = Mathf.Abs(currentPosition - targetPosition) / speed;
            var endTime = Time.realtimeSinceStartup + duration;

            var startingPosition = currentPosition;
            var lastTime = GetStateOffsetRealTimeSinceStartup();
            var wasBlocked = false;

            while (true)
            {
                // Do Movement
                var now = GetStateOffsetRealTimeSinceStartup();
                var remainingProgress = Mathf.Max(0f, (endTime - now) / duration);
                var position = Mathf.Lerp(targetPosition, startingPosition, remainingProgress);

                // Staying put is never obstructed (so one that just reversed doesn't reverse again before it moves)
                if (position != currentPosition && IsHeightChangeLegal != null && !IsHeightChangeLegal(this, position))
                {
                    // Obstructed (update_platforms in platforms.cpp): it plays its sound the first time it's blocked, and
                    // reverses if it does, or else waits where it is (its time to travel put off by the time it waited). A
                    // split platform's halves are blocked together, so each reverses (or waits) on its own.
                    if (!wasBlocked)
                    {
                        PlayRuntimeSound(_obstructed_sound);
                    }

                    wasBlocked = true;

                    if (PLATFORM_REVERSES_DIRECTION_WHEN_OBSTRUCTED(NativeObject.static_flags))
                    {
                        startsMovingSilently = true;
                        BeginState(currentState == States.Extending ? States.Contracting : States.Extending, loop: false);
                        return;
                    }

                    endTime += now - lastTime;
                }
                else
                {
                    wasBlocked = false;
                    SetCurrentPosition(position);

                    if (remainingProgress <= 0f)
                    {
                        break;
                    }
                }

                lastTime = now;

                await Awaitable.NextFrameAsync();

                if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                {
                    return;
                }
            }

            remainingStateTime = GetStateOffsetRealTimeSinceStartup() - endTime;
        }

        // A split platform's play from its floor only, so they play once
        private void PlayRuntimeSound(short soundCode)
        {
            if (linkedSurface == LinkedSurfaces.Ceiling && PLATFORM_COMES_FROM_FLOOR(NativeObject.static_flags))
            {
                return;
            }

            var isExtending = currentState == States.Extending;
            var isFullyContracted = currentState == States.Contracting && currentPosition == contractedPosition;

            OnRuntimeSound?.Invoke(this, soundCode, isExtending, isFullyContracted);
        }

        private static float GetSpeedInMetersPerSecond(short worldDistancePerTick)
        {
            return (float)worldDistancePerTick * TICKS_PER_SECOND / GeometryUtilities.WorldUnitIncrementsPerMeter;
        }

        private float GetStateOffsetRealTimeSinceStartup()
        {
            return Time.realtimeSinceStartup + remainingStateTime;
        }

        // Enabled only until Update has carried a new position to the transform, so a still platform costs no Update
        private void SetCurrentPosition(float position)
        {
            currentPosition = position;

            if (!enabled && this)
            {
                enabled = true;
            }
        }

        private void Update()
        {
            if (NativeObject == null)
            {
                return;
            }

            // Only as it moves, as Surface Cache Global Illumination updates every moved surface
            if (transform.position.y != currentPosition)
            {
                transform.position = new Vector3(0f, currentPosition, 0f);

                Moved?.Invoke();
            }

            enabled = false;
        }
    }
}
