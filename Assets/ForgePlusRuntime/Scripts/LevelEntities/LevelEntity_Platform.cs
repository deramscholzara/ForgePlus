using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Weland;
using Weland.Extensions;

namespace RuntimeCore.Entities.Geometry
{
    public class LevelEntity_Platform : MonoBehaviour, IManipulatable<Platform>, IDestructionPreparable, ISelectable, IInspectable
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

        public short NativeIndex { get; set; }
        public Platform NativeObject { get; set; }

        public LevelEntity_Level FPLevel { private get; set; }

        // TODO: Add this to IFPInspectable so it must be implemented in all inspectables
        public event Action<LevelEntity_Platform> OnInspectionStateChange;

        // TODO: Use this for checking "is active" state for toggling?
        private CancellationTokenSource platformBehaviorCTS;

        private LinkedSurfaces linkedSurface;
        private float speed = 1f;
        private float delay = 1f;
        private float extendedPosition = 0f;
        private float contractedPosition = 1f;

        private float currentPosition;

        private States currentState = States.Contracted;

        private float remainingStateTime = 0f;

        public bool IsRuntimeActive
        {
            get
            {
                return platformBehaviorCTS != null;
            }
        }

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank - no current reason to toggle this, as its selection comes from already-gated FPInteractiveSurface components
        }

        public void Inspect()
        {
            var inspectorPrefab = Resources.Load<Inspector_Platform>("Inspectors/Inspector - Platform");
            var inspector = UnityEngine.Object.Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void PrepareForDestruction()
        {
            DeactivateRuntimeBehavior();
        }

        public void SetPlatform(short index, Platform platform, LevelEntity_Level fpLevel, LinkedSurfaces linkedSurface)
        {
            NativeIndex = index;
            NativeObject = platform;
            FPLevel = fpLevel;

            UpdatePlatformValues(linkedSurface);
        }

        public void UpdatePlatformValues(LinkedSurfaces linkedSurface)
        {
            this.linkedSurface = linkedSurface;
            speed = (float)NativeObject.Speed / 30f;
            delay = (float)NativeObject.Delay / 30f;

            var minimumHeight = NativeObject.RuntimeMinimumHeight(FPLevel.Level);
            var maximumHeight = NativeObject.RuntimeMaximumHeight(FPLevel.Level);

            if (NativeObject.ComesFromFloor && NativeObject.ComesFromCeiling)
            {
                extendedPosition = (float)(maximumHeight + minimumHeight) / 2f / GeometryUtilities.WorldUnitIncrementsPerMeter;

                if (linkedSurface == LinkedSurfaces.Floor)
                {
                    contractedPosition = (float)minimumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                }
                else
                {
                    contractedPosition = (float)maximumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                }
            }
            else
            {
                if (linkedSurface == LinkedSurfaces.Floor)
                {
                    extendedPosition = (float)maximumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                    contractedPosition = (float)minimumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                }
                else
                {
                    extendedPosition = (float)minimumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                    contractedPosition = (float)maximumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                }
            }
        }

        public void BeginRuntimeStyleBehavior()
        {
            currentState = NativeObject.InitiallyExtended ? States.Extended : States.Contracted;

            currentPosition = NativeObject.InitiallyExtended ? extendedPosition : contractedPosition;

            if (NativeObject.InitiallyActive)
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
                    if (NativeObject.ComesFromCeiling)
                    {
                        FPLevel.FPCeilingFpPlatforms[NativeIndex].SetRuntimeActive(value, isRootActivation: false);
                    }
                }
                else
                {
                    if (NativeObject.ComesFromFloor)
                    {
                        FPLevel.FPFloorFpPlatforms[NativeIndex].SetRuntimeActive(value, isRootActivation: false);
                    }
                }
            }
        }

        public void ObstructRuntimeBehavior()
        {
            if (IsRuntimeActive)
            {
                if (currentState == States.Extending &&
                    NativeObject.ReversesDirectionWhenObstructed)
                {
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

            if (!loop && NativeObject.DelaysBeforeActivation &&
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

                        if (loop)
                        {
                            currentPosition = contractedPosition;
                        }
                        else
                        {
                            currentState = States.Extended;

                            if (NativeObject.DeactivatesAtEachLevel || (NativeObject.InitiallyExtended && NativeObject.DeactivatesAtInitialLevel))
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
                        if (NativeObject.ContractsSlower)
                        {
                            await Move(cancellationToken, speed * 0.25f, contractedPosition);
                        }
                        else
                        {
                            await Move(cancellationToken, speed, contractedPosition);
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        if (loop)
                        {
                            currentPosition = extendedPosition;
                        }
                        else
                        {
                            currentState = States.Contracted;

                            if (NativeObject.DeactivatesAtEachLevel || (!NativeObject.InitiallyExtended && NativeObject.DeactivatesAtInitialLevel))
                            {
                                DeactivateRuntimeBehavior();
                                return;
                            }
                        }

                        break;
                }
            }
        }

        private async Task Hold(CancellationToken cancellationToken, float duration, float holdPosition)
        {
            currentPosition = holdPosition;

            var endTime = Time.realtimeSinceStartup + duration;
            while (GetStateOffsetRealTimeSinceStartup() < endTime)
            {
                await Task.Yield();

                if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                {
                    return;
                }
            }

            remainingStateTime = GetStateOffsetRealTimeSinceStartup() - endTime;
        }

        private async Task Move(CancellationToken cancellationToken, float speed, float targetPosition)
        {
            var duration = Mathf.Abs(currentPosition - targetPosition) / speed;
            var endTime = Time.realtimeSinceStartup + duration;

            var startingPosition = currentPosition;

            while (GetStateOffsetRealTimeSinceStartup() < endTime)
            {
                // Do Movement
                var remainingProgress = (endTime - GetStateOffsetRealTimeSinceStartup()) / duration;

                currentPosition = Mathf.Lerp(targetPosition, startingPosition, remainingProgress);

                await Task.Yield();

                if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                {
                    return;
                }
            }

            currentPosition = targetPosition;

            remainingStateTime = GetStateOffsetRealTimeSinceStartup() - endTime;
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
