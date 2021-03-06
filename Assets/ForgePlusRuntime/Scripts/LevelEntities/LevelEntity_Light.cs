﻿using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using RuntimeCore.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Weland;
using Random = UnityEngine.Random;

namespace RuntimeCore.Entities
{
    // TODO: Should inherit from LevelEntity_Base, and should have a representative GameObject in the scene
    public class LevelEntity_Light : IDestructionPreparable, ISelectable, IInspectable
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
        
        public static readonly int lightIntensityGlobalPropertyId = Shader.PropertyToID("_LightIntensity");
        public static Texture2D LightTexture { get; private set; }
        
        // One "tick" = 1/30 seconds.  This is used to maintain classic flicker frequency.
        private const float mininumFlickerDuration = 1f / 30f;
        
        private readonly AnimationCurve smoothLightCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

        public short NativeIndex { get; set; }
        public Weland.Light NativeObject { get; set; }

        public LevelEntity_Level ParentLevel { private get; set; }

        public float CurrentDisplayIntensity { get; private set; }

        public float CurrentLinearIntensity
        {
            get
            {
                return currentLinearIntensity;
            }
            private set
            {
                currentLinearIntensity = value;

                if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                {
                    // Square to convert to gamma-space values (only needed if the project is in Linear space)
                    CurrentDisplayIntensity = currentLinearIntensity * currentLinearIntensity;
                }
                
                LightTexture.SetPixel(NativeIndex, 0, new Color(CurrentDisplayIntensity, 0f, 0f, 0f), 0);
                LightTexture.Apply();
            }
        }

        private float currentLinearIntensity = 0f;

        private States currentState = States.BecomingActive;
        private short remainingPhaseOffset;
        private float remainingPhaseTime = 0f;

        private CancellationTokenSource lightPhaseCTS;

        public LevelEntity_Light(short index, Weland.Light light, LevelEntity_Level level)
        {
            if (!LightTexture)
            {
                LightTexture = new Texture2D(
                    width: 256,
                    height: 1,
                    textureFormat: TextureFormat.R16,
                    mipChain: false,
                    linear: false);

                LightTexture.wrapMode = TextureWrapMode.Clamp;
                LightTexture.filterMode = FilterMode.Point;
                Shader.SetGlobalTexture(lightIntensityGlobalPropertyId, LightTexture);
            }
            
            NativeIndex = index;
            NativeObject = light;
            ParentLevel = level;
            
            BeginRuntimeStyleBehavior();
        }

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank - no current reason to toggle this, as its selection comes from the palette or already-gated EditableSurface components
        }

        public void Inspect()
        {
            var inspectorPrefab = Resources.Load<Inspector_Light>("Inspectors/Inspector - Light");
            var inspector = Object.Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void PrepareForDestruction()
        {
            lightPhaseCTS?.Cancel();
            lightPhaseCTS = null;
        }

        public void BeginRuntimeStyleBehavior()
        {
            if (NativeObject.InitiallyActive)
            {
                CurrentLinearIntensity = (float)NativeObject.PrimaryActive.Intensity;
                BeginPhase(States.PrimaryActive, loop: false);
            }
            else
            {
                CurrentLinearIntensity = (float)NativeObject.PrimaryInactive.Intensity;
                BeginPhase(States.PrimaryInactive, loop: false);
            }
        }

        public async void BeginPhase(States state, bool loop = false)
        {
            lightPhaseCTS?.Cancel();

            lightPhaseCTS = new CancellationTokenSource();
            var cancellationToken = lightPhaseCTS.Token;

            remainingPhaseOffset = NativeObject.Phase;

            if (loop)
            {
                // If we're looping (such as for an editor state-preview mode)
                // then we should not incur any "phase" offset to adjust the
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
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.BecomingActive);

                        if (!loop)
                        {
                            currentState = States.PrimaryActive;
                        }

                        break;
                    case States.PrimaryActive:
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.PrimaryActive);

                        if (!loop)
                        {
                            if (NativeObject.Stateless ||
                                (NativeObject.SecondaryActive.Period > 0 &&
                                 (NativeObject.SecondaryActive.LightingFunction != LightingFunction.Constant ||
                                  NativeObject.SecondaryActive.Intensity != NativeObject.PrimaryActive.Intensity)))
                            {
                                // Only go to the second phase if it has a lasting duration
                                // and if it's not constant at the same intensity as the primary phase.
                                currentState = States.SecondaryActive;
                            }
                            else if (NativeObject.PrimaryInactive.LightingFunction == LightingFunction.Constant)
                            {
                                // If there's no second phase, and the primary phase is constant,
                                // then there's no reason to keep updating lighting values.
                                lightPhaseCTS.Cancel();
                            }
                        }

                        break;
                    case States.SecondaryActive:
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.SecondaryActive);

                        if (!loop)
                        {
                            if (NativeObject.Stateless)
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
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.BecomingInactive);

                        if (!loop)
                        {
                            currentState = States.PrimaryInactive;
                        }

                        break;
                    case States.PrimaryInactive:
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.PrimaryInactive);

                        if (!loop)
                        {
                            if (NativeObject.Stateless ||
                                (NativeObject.SecondaryInactive.Period > 0 &&
                                 (NativeObject.SecondaryInactive.LightingFunction != LightingFunction.Constant ||
                                  NativeObject.SecondaryInactive.Intensity != NativeObject.PrimaryInactive.Intensity)))
                            {
                                // Only go to the second phase if it has a lasting duration
                                // and if it's not constant at the same intensity as the primary phase.
                                currentState = States.SecondaryInactive;
                            }
                            else if (NativeObject.PrimaryInactive.LightingFunction == LightingFunction.Constant)
                            {
                                // If there's no second phase, and the primary phase is constant,
                                // then there's no reason to keep updating lighting values.
                                lightPhaseCTS.Cancel();
                            }
                        }

                        break;
                    case States.SecondaryInactive:
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.SecondaryInactive);

                        if (!loop)
                        {
                            if (NativeObject.Stateless)
                            {
                                currentState = States.BecomingActive;
                            }
                            else
                            {
                                currentState = States.PrimaryInactive;
                            }
                        }

                        break;
                    default:
                        throw new System.Exception($"Light State: {currentState}");
                }
            }
        }

        private async Task RunIntensityPhaseFunction(CancellationToken cancellationToken, Weland.Light.Function lightingFunction)
        {
            var functionPhaseOffset = 0f;

            if (remainingPhaseOffset > 0)
            {
                remainingPhaseOffset -= (short)(lightingFunction.Period);

                if (remainingPhaseOffset > 0)
                {
                    // There's still offset time remaining, so continue to the next State
                    return;
                }
                else
                {
                    // Note: This adds any remaining offset, which will be <= 0,
                    //       because Phase is intended to be a "backwards" shift through time
                    functionPhaseOffset = (float)(lightingFunction.Period + remainingPhaseOffset) / 30f;
                }
            }

            functionPhaseOffset += remainingPhaseTime;

            // Note: Clamps the randomized phase to no less than 1 tick
            //       to ensure all phases run for at least 1 tick.
            var duration = Mathf.Max(1, ((int)lightingFunction.Period + Random.Range(-lightingFunction.DeltaPeriod, lightingFunction.DeltaPeriod))) / 30f;

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
                default:
                    throw new System.NotImplementedException($"Lighting Function: {lightingFunction.LightingFunction}");
            }
        }

        private async Task ConstantIntensityPhaseFunction(CancellationToken cancellationToken, float duration, float phaseOffset, float intensity)
        {
            intensity = Mathf.Clamp01(intensity);

            CurrentLinearIntensity = intensity;

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
            intensity = Mathf.Clamp01(intensity);

            var endTime = Time.realtimeSinceStartup + duration;

            var startingIntensity = CurrentLinearIntensity;
            var actualIntensityDelta = (float)(intensityDelta * intensity);
            var targetIntensity = (float)intensity + Random.Range(-actualIntensityDelta, actualIntensityDelta);

            while (GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
            {
                var remainingProgress = (endTime - GetPhaseOffsetRealTimeSinceStartup(phaseOffset)) / duration;

                CurrentLinearIntensity = Mathf.Lerp(targetIntensity, startingIntensity, remainingProgress);

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
            intensity = Mathf.Clamp01(intensity);

            var endTime = Time.realtimeSinceStartup + duration;

            var startingIntensity = CurrentLinearIntensity;
            var actualIntensityDelta = (float)(intensityDelta * intensity);
            var targetIntensity = (float)intensity + Random.Range(-actualIntensityDelta, actualIntensityDelta);

            while (GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
            {
                var elapsedProgress = 1f - ((endTime - GetPhaseOffsetRealTimeSinceStartup(phaseOffset)) / duration);

                CurrentLinearIntensity = Mathf.Lerp(startingIntensity, targetIntensity, smoothLightCurve.Evaluate(elapsedProgress));

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
            intensity = Mathf.Clamp01(intensity);

            var endTime = Time.realtimeSinceStartup + duration;

            var startingIntensity = CurrentLinearIntensity;
            var actualIntensityDelta = (float)(intensityDelta * intensity);
            var targetIntensity = (float)intensity + Random.Range(-actualIntensityDelta, actualIntensityDelta);

            while (GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
            {
                var elapsedProgress = 1f - ((endTime - GetPhaseOffsetRealTimeSinceStartup(phaseOffset)) / duration);

                var currentInterpolatedIntensity = Mathf.Lerp(startingIntensity, targetIntensity, smoothLightCurve.Evaluate(elapsedProgress));

                var flickerIntensity = currentInterpolatedIntensity;
                if (Random.Range(0, 2) == 0)
                {
                    flickerIntensity = (float)intensity + Random.Range(-actualIntensityDelta, 0);
                    flickerIntensity = Mathf.Clamp01(flickerIntensity);
                }

                CurrentLinearIntensity = flickerIntensity;

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
