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

        // When the game would play one of a platform's sounds (play_platform_sound, in platforms.cpp): the platform, the
        // sound code (_starting_sound, _stopping_sound or _obstructed_sound), whether it's extending, and whether it's
        // fully contracted. A split platform's (whose floor and ceiling both move) come from its floor only.
        public static event Action<LevelEntity_Platform, short, bool, bool> OnRuntimeSound;

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

        // An initially active platform starts moving silently (new_platform, in platforms.cpp, starts it moving rather
        // than waiting to), as does one carrying on in the other direction after being obstructed
        private bool startsMovingSilently;

        // Whether the last move went anywhere (so it stopped somewhere)
        private bool lastMoveMoved;

        public float CurrentHeightInWorldUnitIncrements
        {
            get
            {
                return currentPosition * GeometryUtilities.WorldUnitIncrementsPerMeter;
            }
        }

        public bool IsRuntimeActive
        {
            get
            {
                return platformBehaviorCTS != null;
            }
        }

        // Whether it's active and extending or contracting (rather than waiting at either end), as with
        // PLATFORM_IS_ACTIVE and PLATFORM_IS_MOVING in the game
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

            currentState = PLATFORM_IS_INITIALLY_EXTENDED(NativeObject.static_flags) ? States.Extended : States.Contracted;

            currentPosition = PLATFORM_IS_INITIALLY_EXTENDED(NativeObject.static_flags) ? extendedPosition : contractedPosition;

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

        public void ObstructRuntimeBehavior()
        {
            if (IsRuntimeActive)
            {
                if (currentState == States.Extending &&
                    PLATFORM_REVERSES_DIRECTION_WHEN_OBSTRUCTED(NativeObject.static_flags))
                {
                    // Whichever part is obstructed plays it (and it carries on, the other way, without starting again)
                    OnRuntimeSound?.Invoke(this, _obstructed_sound, true, false);

                    startsMovingSilently = true;
                    BeginState(States.Contracting, loop: false);
                }
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

                        if (lastMoveMoved)
                        {
                            PlayRuntimeSound(_stopping_sound);
                        }

                        if (loop)
                        {
                            currentPosition = contractedPosition;
                        }
                        else
                        {
                            currentState = States.Extended;

                            if (PLATFORM_DEACTIVATES_AT_EACH_LEVEL(NativeObject.static_flags) || (PLATFORM_IS_INITIALLY_EXTENDED(NativeObject.static_flags) && PLATFORM_DEACTIVATES_AT_INITIAL_LEVEL(NativeObject.static_flags)))
                            {
                                DeactivateRuntimeBehavior();
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

                        if (lastMoveMoved)
                        {
                            PlayRuntimeSound(_stopping_sound);
                        }

                        if (loop)
                        {
                            currentPosition = extendedPosition;
                        }
                        else
                        {
                            currentState = States.Contracted;

                            if (PLATFORM_DEACTIVATES_AT_EACH_LEVEL(NativeObject.static_flags) || (!PLATFORM_IS_INITIALLY_EXTENDED(NativeObject.static_flags) && PLATFORM_DEACTIVATES_AT_INITIAL_LEVEL(NativeObject.static_flags)))
                            {
                                DeactivateRuntimeBehavior();
                                return;
                            }
                        }

                        break;
                }
            }
        }

        private async Awaitable Hold(CancellationToken cancellationToken, float duration, float holdPosition)
        {
            currentPosition = holdPosition;

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
            lastMoveMoved = false;

            if (currentPosition == targetPosition)
            {
                // Already there, so there's no time to spend moving
                return;
            }

            lastMoveMoved = true;

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

            while (GetStateOffsetRealTimeSinceStartup() < endTime)
            {
                // Do Movement
                var remainingProgress = (endTime - GetStateOffsetRealTimeSinceStartup()) / duration;

                currentPosition = Mathf.Lerp(targetPosition, startingPosition, remainingProgress);

                await Awaitable.NextFrameAsync();

                if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                {
                    return;
                }
            }

            currentPosition = targetPosition;

            remainingStateTime = GetStateOffsetRealTimeSinceStartup() - endTime;
        }

        // From its floor, for a split platform (so its sounds play once), as it is now: extending, or fully contracted
        // (stopped there)
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

        private void Update()
        {
            if (NativeObject != null)
            {
                transform.position = new Vector3(0f, currentPosition, 0f);
            }
        }
    }
}
