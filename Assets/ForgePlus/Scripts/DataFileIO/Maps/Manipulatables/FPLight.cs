using ForgePlus.Inspection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Weland;


namespace ForgePlus.LevelManipulation
{
    public class FPLight : IFPManipulatable<Weland.Light>, IFPDestructionPreparable, IFPSelectable, IFPInspectable
    {
        public enum States
        {
            BecomingActive,
            PrimaryActive,
            SecondaryActive,
            BecomingInactive,
            PrimaryInactive,
            SecondaryInactive,
        }

        // One "tick" = 1/30 seconds.  This is used to maintain classic flicker frequency.
        private const float mininumFlickerDuration = 1f / 30f;

        private static readonly int lightIntensityPropertyId = Shader.PropertyToID("_LightIntensity");
        private readonly AnimationCurve smoothLightCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

        public short? Index { get; set; }
        public Weland.Light WelandObject { get; set; }

        public FPLevel FPLevel { private get; set; }

        public float CurrentIntensity
        {
            get
            {
                return currentIntensity;
            }
            private set
            {
                currentIntensity = value;

                foreach (var material in subscribedMaterials)
                {
                    material.SetFloat(lightIntensityPropertyId, currentIntensity);
                }
            }
        }

        private float currentIntensity = 0f;

        private States currentState = States.BecomingActive;
        private short remainingPhaseOffset;
        private float remainingPhaseTime = 0f;

        private List<Material> subscribedMaterials = new List<Material>();

        private CancellationTokenSource lightPhaseCTS;

        public FPLight(short index, Weland.Light light, FPLevel fpLevel)
        {
            Index = index;
            WelandObject = light;
            FPLevel = fpLevel;

            BeginRuntimeStyleBehavior();
        }

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank - no current reason to toggle this, as its selection comes from the palette or already-gated FPInteractiveSurface components
        }

        public void Inspect()
        {
            var inspectorPrefab = Resources.Load<InspectorFPLight>("Inspectors/Inspector - FPLight");
            var inspector = Object.Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void PrepareForDestruction()
        {
            lightPhaseCTS?.Cancel();
            lightPhaseCTS = null;
        }

        public void SubscribeMaterial(Material material)
        {
            subscribedMaterials.Add(material);

            material.SetFloat(lightIntensityPropertyId, CurrentIntensity);
        }

        public void UnsubscribeMaterial(Material material)
        {
            subscribedMaterials.Remove(material);
        }

        public void BeginRuntimeStyleBehavior()
        {
            if (WelandObject.InitiallyActive)
            {
                CurrentIntensity = (float)WelandObject.PrimaryActive.Intensity;
                BeginPhase(States.PrimaryActive, loop: false);
            }
            else
            {
                CurrentIntensity = (float)WelandObject.PrimaryInactive.Intensity;
                BeginPhase(States.PrimaryInactive, loop: false);
            }
        }

        public async void BeginPhase(States state, bool loop = false)
        {
            lightPhaseCTS?.Cancel();

            lightPhaseCTS = new CancellationTokenSource();
            var cancellationToken = lightPhaseCTS.Token;

            remainingPhaseOffset = WelandObject.Phase;

            if (loop)
            {
                // If we're looping (such as for an editor state-preview mode)
                // then we should not incur an "phase" offset to adjust the
                // current State or position therein.
                remainingPhaseOffset = 0;
            }

            remainingPhaseTime = 0f;

            currentState = state;

            while (!cancellationToken.IsCancellationRequested && Application.isPlaying)
            {
                switch (currentState)
                {
                    case States.BecomingActive:
                        await RunIntensityPhaseFunction(cancellationToken, WelandObject.BecomingActive);

                        if (!loop)
                        {
                            currentState = States.PrimaryActive;
                        }

                        break;
                    case States.PrimaryActive:
                        await RunIntensityPhaseFunction(cancellationToken, WelandObject.PrimaryActive);

                        if (!loop)
                        {
                            if (WelandObject.Stateless ||
                                (WelandObject.SecondaryActive.Period > 0 &&
                                 (WelandObject.SecondaryActive.LightingFunction != LightingFunction.Constant ||
                                  WelandObject.SecondaryActive.Intensity != WelandObject.PrimaryActive.Intensity)))
                            {
                                // Only go to the second phase if it has a lasting duration
                                // and if it's not constant at the same intensity as the primary phase.
                                currentState = States.SecondaryActive;
                            }
                            else if (WelandObject.PrimaryInactive.LightingFunction == LightingFunction.Constant)
                            {
                                // If there's no second phase, and the primary phase is constant,
                                // then there's no reason to keep updating lighting values.
                                lightPhaseCTS.Cancel();
                            }
                        }

                        break;
                    case States.SecondaryActive:
                        await RunIntensityPhaseFunction(cancellationToken, WelandObject.SecondaryActive);

                        if (!loop)
                        {
                            if (WelandObject.Stateless)
                            {
                                currentState = States.BecomingInactive;
                            }
                            else
                            {
                                currentState = States.PrimaryActive;
                            }
                        }

                        break;
                    case States.BecomingInactive:
                        await RunIntensityPhaseFunction(cancellationToken, WelandObject.BecomingInactive);

                        if (!loop)
                        {
                            currentState = States.PrimaryInactive;
                        }

                        break;
                    case States.PrimaryInactive:
                        await RunIntensityPhaseFunction(cancellationToken, WelandObject.PrimaryInactive);

                        if (!loop)
                        {
                            if (WelandObject.Stateless ||
                                (WelandObject.SecondaryInactive.Period > 0 &&
                                 (WelandObject.SecondaryInactive.LightingFunction != LightingFunction.Constant ||
                                  WelandObject.SecondaryInactive.Intensity != WelandObject.PrimaryInactive.Intensity)))
                            {
                                // Only go to the second phase if it has a lasting duration
                                // and if it's not constant at the same intensity as the primary phase.
                                currentState = States.SecondaryInactive;
                            }
                            else if (WelandObject.PrimaryInactive.LightingFunction == LightingFunction.Constant)
                            {
                                // If there's no second phase, and the primary phase is constant,
                                // then there's no reason to keep updating lighting values.
                                lightPhaseCTS.Cancel();
                            }
                        }

                        break;
                    case States.SecondaryInactive:
                        await RunIntensityPhaseFunction(cancellationToken, WelandObject.SecondaryInactive);

                        if (!loop)
                        {
                            if (WelandObject.Stateless)
                            {
                                currentState = States.BecomingActive;
                            }
                            else
                            {
                                currentState = States.PrimaryInactive;
                            }
                        }

                        break;
                }
            }
        }

        private async Task RunIntensityPhaseFunction(CancellationToken cancellationToken, Weland.Light.Function lightingFunction)
        {
            var functionPhaseOffset = 0f;

            if (remainingPhaseOffset > 0)
            {
                remainingPhaseOffset -= (short)(lightingFunction.Period - 1);

                if (remainingPhaseOffset > 0)
                {
                    // There's still offset time remaining, so continue to the next State
                    return;
                }
                else
                {
                    functionPhaseOffset = (float)(lightingFunction.Period + remainingPhaseOffset) / 30f;
                }
            }

            functionPhaseOffset += remainingPhaseTime;

            // Note: Clamps the randomized phase to no less than 1 tick
            //       to ensure all phases run for at least 1 tick.
            var duration = Mathf.Max(1, ((int)lightingFunction.Period + UnityEngine.Random.Range(-lightingFunction.DeltaPeriod, lightingFunction.DeltaPeriod))) / 30f;

            switch (lightingFunction.LightingFunction)
            {
                case LightingFunction.Constant:
                    await ConstantIntensityPhaseFunction(cancellationToken, duration, functionPhaseOffset, (float)lightingFunction.Intensity);
                    return;
                case LightingFunction.Linear:
                    await LinearIntensityPhaseFunction(cancellationToken, duration, functionPhaseOffset, (float)lightingFunction.Intensity, (float)lightingFunction.DeltaIntensity);
                    return;
                case LightingFunction.Smooth:
                    await SmoothIntensityPhaseFunction(cancellationToken, duration, functionPhaseOffset, (float)lightingFunction.Intensity, (float)lightingFunction.DeltaIntensity);
                    return;
                case LightingFunction.Flicker:
                    await FlickerIntensityPhaseFunction(cancellationToken, duration, functionPhaseOffset, (float)lightingFunction.Intensity, (float)lightingFunction.DeltaIntensity);
                    return;
            }
        }

        private async Task ConstantIntensityPhaseFunction(CancellationToken cancellationToken, float duration, float phaseOffset, float intensity)
        {
            CurrentIntensity = intensity;

            var endTime = Time.realtimeSinceStartup + duration;

            while (GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
            {
                await Task.Yield();

                if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                {
                    return;
                }
            }

            remainingPhaseTime = GetPhaseOffsetRealTimeSinceStartup(phaseOffset) - endTime;
        }

        private async Task LinearIntensityPhaseFunction(CancellationToken cancellationToken, float duration, float phaseOffset, float intensity, float intensityDelta)
        {
            var endTime = Time.realtimeSinceStartup + duration;

            var startingIntensity = CurrentIntensity;
            var actualIntensityDelta = (float)(intensityDelta * intensity);
            var targetIntensity = (float)intensity + UnityEngine.Random.Range(-actualIntensityDelta, actualIntensityDelta);

            while (GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
            {
                var remainingProgress = (endTime - GetPhaseOffsetRealTimeSinceStartup(phaseOffset)) / duration;

                CurrentIntensity = Mathf.Lerp(targetIntensity, startingIntensity, remainingProgress);

                await Task.Yield();

                if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                {
                    return;
                }
            }

            remainingPhaseTime = GetPhaseOffsetRealTimeSinceStartup(phaseOffset) - endTime;
        }

        private async Task SmoothIntensityPhaseFunction(CancellationToken cancellationToken, float duration, float phaseOffset, float intensity, float intensityDelta)
        {
            var endTime = Time.realtimeSinceStartup + duration;

            var startingIntensity = CurrentIntensity;
            var actualIntensityDelta = (float)(intensityDelta * intensity);
            var targetIntensity = (float)intensity + UnityEngine.Random.Range(-actualIntensityDelta, actualIntensityDelta);

            while (GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
            {
                var elapsedProgress = 1f - ((endTime - GetPhaseOffsetRealTimeSinceStartup(phaseOffset)) / duration);

                CurrentIntensity = Mathf.Lerp(startingIntensity, targetIntensity, smoothLightCurve.Evaluate(elapsedProgress));

                await Task.Yield();

                if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                {
                    return;
                }
            }

            remainingPhaseTime = GetPhaseOffsetRealTimeSinceStartup(phaseOffset) - endTime;
        }

        private async Task FlickerIntensityPhaseFunction(CancellationToken cancellationToken, float duration, float phaseOffset, float intensity, float intensityDelta)
        {
            var endTime = Time.realtimeSinceStartup + duration;

            var actualIntensityDelta = (float)(intensityDelta * intensity);

            while (GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
            {
                var flickerIntensity = intensity;
                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    // TODO: should this random range be from -actualIntensityDelto to 0f,
                    //       so it only flickers darker?  Or should it flicker brighter, too?
                    flickerIntensity = (float)intensity + UnityEngine.Random.Range(-actualIntensityDelta, actualIntensityDelta);
                }

                CurrentIntensity = flickerIntensity;

                var flickerEndTime = Time.realtimeSinceStartup + mininumFlickerDuration;
                while (Time.realtimeSinceStartup < flickerEndTime && GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
                {
                    await Task.Yield();

                    if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                    {
                        return;
                    }
                }
            }

            remainingPhaseTime = GetPhaseOffsetRealTimeSinceStartup(phaseOffset) - endTime;
        }

        private float GetPhaseOffsetRealTimeSinceStartup(float phaseOffset)
        {
            return Time.realtimeSinceStartup + phaseOffset;
        }
    }
}
