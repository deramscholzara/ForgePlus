using ForgePlus.LevelManipulation.Utilities;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class FPPlatform : MonoBehaviour, IFPManipulatable<Platform>
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

        // TODO: Use this for checking "is active" state for toggling?
        private CancellationTokenSource platformBehaviorCTS;

        private float speed = 1f;
        private float delay = 1f;
        private float extendedPosition = 0f;
        private float contractedPosition = 1f;

        private float currentPosition;

        private States currentState = States.Contracted;

        private float remainingStateTime = 0f;

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
        }

        private void DeactivateRuntimeBehavior()
        {
            platformBehaviorCTS?.Cancel();
            platformBehaviorCTS = null;
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

                        if (!loop)
                        {
                            currentState = States.Contracting;
                        }

                        break;
                    case States.Extending:
                        await Move(cancellationToken, speed, extendedPosition);

                        if (WelandObject.DeactivatesAtEachLevel || (WelandObject.InitiallyExtended && WelandObject.DeactivatesAtInitialLevel))
                        {
                            DeactivateRuntimeBehavior();
                            return;
                        }

                        if (loop)
                        {
                            currentPosition = contractedPosition;
                        }
                        else
                        {
                            currentState = States.Extended;
                        }

                        break;
                    case States.Contracted:
                        await Hold(cancellationToken, delay, contractedPosition);

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

                        if (loop)
                        {
                            currentPosition = extendedPosition;
                        }
                        else
                        {
                            if (WelandObject.DeactivatesAtEachLevel || (!WelandObject.InitiallyExtended && WelandObject.DeactivatesAtInitialLevel))
                            {
                                DeactivateRuntimeBehavior();
                                return;
                            }

                            currentState = States.Contracted;
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

        // TODO: Use FixedUpdate and move a Rigidbody when collision is added and utilized
        private void Update()
        {
            if (WelandObject != null)
            {
                transform.position = new Vector3(0f, currentPosition, 0f);
            }
        }
    }
}
