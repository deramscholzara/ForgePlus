using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using RuntimeCore.Common;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using AlephOne;
using static AlephOne.lightsource;
using ForgePlus.Extensions;
using Random = UnityEngine.Random;

namespace RuntimeCore.Entities
{
    // TODO: Should inherit from LevelEntity_Base, and should have a representative GameObject in the scene
    [AutoStaticsCleanup]
    public partial class LevelEntity_Light : IDestructionPreparable, ISelectable, IInspectable
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
        private const float minimumTickDuration = 1f / 30f;
        
        private readonly AnimationCurve smoothLightCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

        public short NativeIndex { get; set; }
        public static_light_data NativeObject { get; set; }

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

        public LevelEntity_Light(short index, static_light_data light, LevelEntity_Level level)
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
            var inspector = new Inspector_Light(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void PrepareForDestruction()
        {
            lightPhaseCTS?.Cancel();
            lightPhaseCTS = null;
        }

        public void BeginRuntimeStyleBehavior()
        {
            if (LIGHT_IS_INITIALLY_ACTIVE(NativeObject))
            {
                CurrentLinearIntensity = AlephOneExtensions.FixedToFloat(NativeObject.primary_active.intensity);
                BeginPhase(States.PrimaryActive, loop: false);
            }
            else
            {
                CurrentLinearIntensity = AlephOneExtensions.FixedToFloat(NativeObject.primary_inactive.intensity);
                BeginPhase(States.PrimaryInactive, loop: false);
            }
        }

        public async void BeginPhase(States state, bool loop = false)
        {
            lightPhaseCTS?.Cancel();

            // Each phase loop cancels only its own token source: the field may be replaced by a newer loop,
            // or cleared by PrepareForDestruction, while this one is awaiting
            var phaseCTS = new CancellationTokenSource();
            lightPhaseCTS = phaseCTS;
            var cancellationToken = phaseCTS.Token;

            remainingPhaseOffset = NativeObject.phase;

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
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.becoming_active);

                        if (!loop)
                        {
                            currentState = States.PrimaryActive;
                        }

                        break;
                    case States.PrimaryActive:
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.primary_active);

                        if (!loop)
                        {
                            if (LIGHT_IS_STATELESS(NativeObject) ||
                                (NativeObject.secondary_active.period > 0 &&
                                 (NativeObject.secondary_active.function != _constant_lighting_function ||
                                  NativeObject.secondary_active.intensity != NativeObject.primary_active.intensity)))
                            {
                                // Only go to the second phase if it has a lasting duration
                                // and if it's not constant at the same intensity as the primary phase.
                                currentState = States.SecondaryActive;
                            }
                            else if (NativeObject.primary_inactive.function == _constant_lighting_function)
                            {
                                // If there's no second phase, and the primary phase is constant,
                                // then there's no reason to keep updating lighting values.
                                phaseCTS.Cancel();
                            }
                        }

                        break;
                    case States.SecondaryActive:
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.secondary_active);

                        if (!loop)
                        {
                            if (LIGHT_IS_STATELESS(NativeObject))
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
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.becoming_inactive);

                        if (!loop)
                        {
                            currentState = States.PrimaryInactive;
                        }

                        break;
                    case States.PrimaryInactive:
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.primary_inactive);

                        if (!loop)
                        {
                            if (LIGHT_IS_STATELESS(NativeObject) ||
                                (NativeObject.secondary_inactive.period > 0 &&
                                 (NativeObject.secondary_inactive.function != _constant_lighting_function ||
                                  NativeObject.secondary_inactive.intensity != NativeObject.primary_inactive.intensity)))
                            {
                                // Only go to the second phase if it has a lasting duration
                                // and if it's not constant at the same intensity as the primary phase.
                                currentState = States.SecondaryInactive;
                            }
                            else if (NativeObject.primary_inactive.function == _constant_lighting_function)
                            {
                                // If there's no second phase, and the primary phase is constant,
                                // then there's no reason to keep updating lighting values.
                                phaseCTS.Cancel();
                            }
                        }

                        break;
                    case States.SecondaryInactive:
                        await RunIntensityPhaseFunction(cancellationToken, NativeObject.secondary_inactive);

                        if (!loop)
                        {
                            if (LIGHT_IS_STATELESS(NativeObject))
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

        private async Awaitable RunIntensityPhaseFunction(CancellationToken cancellationToken, lighting_function_specification lightingFunction)
        {
            var functionPhaseOffset = 0f;

            if (remainingPhaseOffset > 0)
            {
                remainingPhaseOffset -= (short)(lightingFunction.period);

                if (remainingPhaseOffset > 0)
                {
                    // There's still offset time remaining, so continue to the next State
                    return;
                }
                else
                {
                    // Note: This adds any remaining offset, which will be <= 0,
                    //       because Phase is intended to be a "backwards" shift through time
                    functionPhaseOffset = (float)(lightingFunction.period + remainingPhaseOffset) / 30f;
                }
            }

            functionPhaseOffset += remainingPhaseTime;

            // Each state adds a random part of the deltas (lightsource.cpp: change_light_state), and runs at least a tick
            var periodTicks = lightingFunction.period + Random.Range(0, lightingFunction.delta_period + 1);
            var duration = Mathf.Max(1, periodTicks) / 30f;
            var finalIntensity = Mathf.Clamp01(AlephOneExtensions.FixedToFloat(lightingFunction.intensity + Random.Range(0, lightingFunction.delta_intensity + 1)));

            switch (lightingFunction.function)
            {
                case _constant_lighting_function:
                    await ConstantIntensityPhaseFunction(cancellationToken, duration, functionPhaseOffset, finalIntensity);
                    return;
                case _linear_lighting_function:
                    await LinearIntensityPhaseFunction(cancellationToken, duration, functionPhaseOffset, finalIntensity);
                    return;
                case _smooth_lighting_function:
                    await SmoothIntensityPhaseFunction(cancellationToken, duration, functionPhaseOffset, finalIntensity);
                    return;
                case _flicker_lighting_function:
                case _random_lighting_function:
                case _fluorescent_lighting_function:
                    await TickingIntensityPhaseFunction(cancellationToken, duration, functionPhaseOffset, finalIntensity, lightingFunction.function);
                    return;
                default:
                    throw new System.NotImplementedException($"Lighting Function: {lightingFunction.function}");
            }
        }

        private async Awaitable ConstantIntensityPhaseFunction(CancellationToken cancellationToken, float duration, float phaseOffset, float finalIntensity)
        {
            CurrentLinearIntensity = finalIntensity;

            var endTime = Time.realtimeSinceStartup + duration;

            while (GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
            {
                await Awaitable.NextFrameAsync();

                if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                {
                    return;
                }
            }

            remainingPhaseTime = GetPhaseOffsetRealTimeSinceStartup(phaseOffset) - endTime;
        }

        private async Awaitable LinearIntensityPhaseFunction(CancellationToken cancellationToken, float duration, float phaseOffset, float finalIntensity)
        {
            var endTime = Time.realtimeSinceStartup + duration;

            var initialIntensity = CurrentLinearIntensity;

            while (GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
            {
                var remainingProgress = (endTime - GetPhaseOffsetRealTimeSinceStartup(phaseOffset)) / duration;

                CurrentLinearIntensity = Mathf.Lerp(finalIntensity, initialIntensity, remainingProgress);

                await Awaitable.NextFrameAsync();

                if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                {
                    return;
                }
            }

            remainingPhaseTime = GetPhaseOffsetRealTimeSinceStartup(phaseOffset) - endTime;
        }

        private async Awaitable SmoothIntensityPhaseFunction(CancellationToken cancellationToken, float duration, float phaseOffset, float finalIntensity)
        {
            var endTime = Time.realtimeSinceStartup + duration;

            var initialIntensity = CurrentLinearIntensity;

            while (GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
            {
                var elapsedProgress = 1f - ((endTime - GetPhaseOffsetRealTimeSinceStartup(phaseOffset)) / duration);

                CurrentLinearIntensity = Mathf.Lerp(initialIntensity, finalIntensity, smoothLightCurve.Evaluate(elapsedProgress));

                await Awaitable.NextFrameAsync();

                if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
                {
                    return;
                }
            }

            remainingPhaseTime = GetPhaseOffsetRealTimeSinceStartup(phaseOffset) - endTime;
        }

        // A new intensity each tick (lightsource.cpp): flicker's between the smooth and final ones, random's between the
        // initial and final ones, and fluorescent's either one
        private async Awaitable TickingIntensityPhaseFunction(CancellationToken cancellationToken, float duration, float phaseOffset, float finalIntensity, short function)
        {
            var endTime = Time.realtimeSinceStartup + duration;

            var initialIntensity = CurrentLinearIntensity;

            while (GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
            {
                switch (function)
                {
                    case _flicker_lighting_function:
                        var elapsedProgress = 1f - ((endTime - GetPhaseOffsetRealTimeSinceStartup(phaseOffset)) / duration);
                        var smoothIntensity = Mathf.Lerp(initialIntensity, finalIntensity, smoothLightCurve.Evaluate(elapsedProgress));
                        CurrentLinearIntensity = Mathf.Lerp(smoothIntensity, finalIntensity, Random.value);
                        break;
                    case _random_lighting_function:
                        CurrentLinearIntensity = Mathf.Lerp(initialIntensity, finalIntensity, Random.value);
                        break;
                    default:
                        CurrentLinearIntensity = Random.Range(0, 2) == 0 ? initialIntensity : finalIntensity;
                        break;
                }

                var tickEndTime = Time.realtimeSinceStartup + minimumTickDuration;
                while (Time.realtimeSinceStartup < tickEndTime && GetPhaseOffsetRealTimeSinceStartup(phaseOffset) < endTime)
                {
                    await Awaitable.NextFrameAsync();

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
