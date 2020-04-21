using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPPlatform : MonoBehaviour, IFPManipulatable<Platform>, IFPDestructionPreparable, IFPSelectable, IFPInspectable
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

        public short? Index { get; set; }
        public Platform WelandObject { get; set; }

        public FPLevel FPLevel { private get; set; }

        // TODO: Add this to IFPInspectable so it must be implemented in all inspectables
        public event Action<FPPlatform> OnInspectionStateChange;

        // TODO: Use this for checking "is active" state for toggling?
        private CancellationTokenSource platformBehaviorCTS;

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
            var inspectorPrefab = Resources.Load<InspectorFPPlatform>("Inspectors/Inspector - FPPlatform");
            var inspector = UnityEngine.Object.Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void PrepareForDestruction()
        {
            DeactivateRuntimeBehavior();
        }

        public void SetPlatform(short index, Platform platform, LinkedSurfaces linkedSurface)
        {
            Index = index;
            WelandObject = platform;

            UpdatePlatformValues(linkedSurface);
        }

        public void UpdatePlatformValues(LinkedSurfaces linkedSurface)
        {
            speed = (float)WelandObject.Speed / 30f;
            delay = (float)WelandObject.Delay / 30f;

            if (WelandObject.ComesFromFloor && WelandObject.ComesFromCeiling)
            {
                extendedPosition = (float)(WelandObject.MaximumHeight + WelandObject.MinimumHeight) / 2f / GeometryUtilities.WorldUnitIncrementsPerMeter;

                if (linkedSurface == LinkedSurfaces.Floor)
                {
                    contractedPosition = (float)WelandObject.MinimumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                }
                else
                {
                    contractedPosition = (float)WelandObject.MaximumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                }
            }
            else
            {
                if (linkedSurface == LinkedSurfaces.Floor)
                {
                    extendedPosition = (float)WelandObject.MaximumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                    contractedPosition = (float)WelandObject.MinimumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                }
                else
                {
                    extendedPosition = (float)WelandObject.MinimumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                    contractedPosition = (float)WelandObject.MaximumHeight / GeometryUtilities.WorldUnitIncrementsPerMeter;
                }
            }
        }

        public void BeginRuntimeStyleBehavior()
        {
            currentState = WelandObject.InitiallyExtended ? States.Extended : States.Contracted;

            currentPosition = WelandObject.InitiallyExtended ? extendedPosition : contractedPosition;

            if (WelandObject.InitiallyActive)
            {
                ActivateRuntimeBehavior();
            }
            else
            {
                DeactivateRuntimeBehavior();
            }
        }

        public void SetRuntimeActive(bool value)
        {
            if (value)
            {
                ActivateRuntimeBehavior();
            }
            else
            {
                DeactivateRuntimeBehavior();
            }
        }

        public void ObstructRuntimeBehavior()
        {
            if (IsRuntimeActive)
            {
                if (currentState == States.Extending &&
                    WelandObject.ReversesDirectionWhenObstructed)
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

            if (!loop && WelandObject.DelaysBeforeActivation &&
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

                            if (WelandObject.DeactivatesAtEachLevel || (WelandObject.InitiallyExtended && WelandObject.DeactivatesAtInitialLevel))
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
                        if (WelandObject.ContractsSlower)
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

                            if (WelandObject.DeactivatesAtEachLevel || (!WelandObject.InitiallyExtended && WelandObject.DeactivatesAtInitialLevel))
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
            if (WelandObject != null)
            {
                transform.position = new Vector3(0f, currentPosition, 0f);
            }
        }
    }
}
