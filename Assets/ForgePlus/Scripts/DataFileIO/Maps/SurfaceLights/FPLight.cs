using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Weland;


namespace ForgePlus.LevelManipulation
{
    public class FPLight
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

        // TODO: Convert to indexed dictionary instead of list
        private static readonly List<FPLight> FPLights = new List<FPLight>(32);

        // One "tick" = 1/30 seconds.  This is used to maintain classic flicker frequency.
        private const float mininumFlickerDuration = 1f / 30f;

        private readonly AnimationCurve smoothLightCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

        private Weland.Light light;

        private CancellationTokenSource lightPhaseCTS;

        private States currentState = States.BecomingActive;
        private short remainingPhaseOffset;
        private float remainingPhaseTime = 0f;

        public float CurrentIntensity { get; private set; }

        private FPLight(Weland.Light light)
        {
            this.light = light;
        }

        public static FPLight GetFPLight(Weland.Light light)
        {

            var foundLight = FPLights.FirstOrDefault(item => item.IsDrivenByLight(light));

            if (foundLight == null)
            {
                foundLight = new FPLight(light);
                FPLights.Add(foundLight);
            }

            return foundLight;
        }

        public static void ClearFPLightsList()
        {
            foreach (var fpLight in FPLights)
            {
                if (fpLight.lightPhaseCTS != null)
                {
                    fpLight.lightPhaseCTS.Cancel();
                }
            }

            FPLights.Clear();
        }

        public void BeginRuntimeStyleBehavior()
        {
            if (light.InitiallyActive)
            {
                CurrentIntensity = (float)light.PrimaryActive.Intensity;
                BeginPhase(States.PrimaryActive, loop: false);
            }
            else
            {
                CurrentIntensity = (float)light.PrimaryInactive.Intensity;
                BeginPhase(States.PrimaryInactive, loop: false);
            }
        }

        public async void BeginPhase(States state, bool loop = false)
        {
            lightPhaseCTS?.Cancel();

            lightPhaseCTS = new CancellationTokenSource();
            var cancellationToken = lightPhaseCTS.Token;

            remainingPhaseOffset = light.Phase;

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
                        await RunIntensityPhaseFunction(cancellationToken, light.BecomingActive);

                        if (!loop)
                        {
                            currentState = States.PrimaryActive;
                        }

                        break;
                    case States.PrimaryActive:
                        await RunIntensityPhaseFunction(cancellationToken, light.PrimaryActive);

                        if (!loop)
                        {
                            if (light.Stateless ||
                                (light.SecondaryActive.Period > 0 &&
                                 (light.SecondaryActive.LightingFunction != LightingFunction.Constant ||
                                  light.SecondaryActive.Intensity != light.PrimaryActive.Intensity)))
                            {
                                // Only go to the second phase if it has a lasting duration
                                // and if it's not constant at the same intensity as the primary phase.
                                currentState = States.SecondaryActive;
                            }
                            else if (light.PrimaryInactive.LightingFunction == LightingFunction.Constant)
                            {
                                // If there's no second phase, and the primary phase is constant,
                                // then there's no reason to keep updating lighting values.
                                lightPhaseCTS.Cancel();
                            }
                        }

                        break;
                    case States.SecondaryActive:
                        await RunIntensityPhaseFunction(cancellationToken, light.SecondaryActive);

                        if (!loop)
                        {
                            if (light.Stateless)
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
                        await RunIntensityPhaseFunction(cancellationToken, light.BecomingInactive);

                        if (!loop)
                        {
                            currentState = States.PrimaryInactive;
                        }

                        break;
                    case States.PrimaryInactive:
                        await RunIntensityPhaseFunction(cancellationToken, light.PrimaryInactive);

                        if (!loop)
                        {
                            if (light.Stateless ||
                                (light.SecondaryInactive.Period > 0 &&
                                 (light.SecondaryInactive.LightingFunction != LightingFunction.Constant ||
                                  light.SecondaryInactive.Intensity != light.PrimaryInactive.Intensity)))
                            {
                                // Only go to the second phase if it has a lasting duration
                                // and if it's not constant at the same intensity as the primary phase.
                                currentState = States.SecondaryInactive;
                            }
                            else if (light.PrimaryInactive.LightingFunction == LightingFunction.Constant)
                            {
                                // If there's no second phase, and the primary phase is constant,
                                // then there's no reason to keep updating lighting values.
                                lightPhaseCTS.Cancel();
                            }
                        }

                        break;
                    case States.SecondaryInactive:
                        await RunIntensityPhaseFunction(cancellationToken, light.SecondaryInactive);

                        if (!loop)
                        {
                            if (light.Stateless)
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

        private bool IsDrivenByLight(Weland.Light light)
        {
            return light == this.light;
        }
    }
}
