using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using RuntimeCore.Common;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Rendering;
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

        // Every light's display intensity, read by GetLightIntensityFromStructuredBuffer.hlsl using the light index
        // that each surface stores in its UVs (UV0.z, or UV1.z for a layered side's outer layer)
        public static readonly int lightIntensitiesGlobalPropertyId = Shader.PropertyToID("_LightIntensities");
        private const int minimumIntensitiesCapacity = 256;

        private static GraphicsBuffer intensitiesBuffer;
        private static float[] intensities = Array.Empty<float>();

        // Range of intensities changed since the last upload (start inclusive, end exclusive), so however many lights
        // change in a frame, they are sent to the GPU together, once, just before rendering
        private static int dirtyStart = int.MaxValue;
        private static int dirtyEnd = 0;

        // One "tick" = 1/30 seconds.  This is used to maintain classic flicker frequency.
        private const float minimumTickDuration = 1f / 30f;

        // After a stall longer than this (such as the Editor being paused), lights carry on from the current time
        // rather than playing through every phase they missed
        private const float maximumCatchUpTime = 1f;

        // All animating lights advance together, from one loop, once per frame
        private static readonly List<LevelEntity_Light> animatingLights = new List<LevelEntity_Light>();
        private static bool animationLoopIsRunning;

        // Changes when Play mode ends, so a loop left waiting from that session stops rather than running alongside a new one
        private static int animationLoopGeneration;

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
                else
                {
                    CurrentDisplayIntensity = currentLinearIntensity;
                }

                SetIntensity(NativeIndex, CurrentDisplayIntensity);
            }
        }

        private float currentLinearIntensity = 0f;

        private States currentState = States.BecomingActive;
        private bool loopsCurrentState;
        private int remainingPhaseOffset;
        private bool isAnimating;

        // The current State's running phase. Until it starts, the next phase starts at phaseStartTime + phaseDuration.
        private bool phaseIsStarted;
        private lighting_function_specification phaseFunction;
        private float phaseStartTime;
        private float phaseDuration;
        private float phaseInitialIntensity;
        private float phaseFinalIntensity;
        private float nextTickTime;

        public LevelEntity_Light(short index, static_light_data light, LevelEntity_Level level)
        {
            EnsureIntensitiesCapacity(index + 1);

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
            StopAnimating();
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

        public void BeginPhase(States state, bool loop = false)
        {
            StopAnimating();

            if (!Application.isPlaying)
            {
                return;
            }

            currentState = state;
            loopsCurrentState = loop;

            // If we're looping (such as for an editor state-preview mode)
            // then we should not incur any "phase" offset to adjust the
            // current State or position therein.
            remainingPhaseOffset = loop ? 0 : NativeObject.phase;

            var now = Time.realtimeSinceStartup;

            phaseIsStarted = false;
            phaseStartTime = now;
            phaseDuration = 0f;

            // The first phase starts now, so the light changes right away rather than on the next frame
            try
            {
                if (Advance(now))
                {
                    StartAnimating();
                }
            }
            catch (Exception exception)
            {
                // A broken light stays as it is, rather than stopping whatever started it (such as loading the level)
                Debug.LogException(exception);
            }
        }

        // Plays the light up to the given time, through as many phases as have ended by then. Returns false when the
        // light has nothing more to change, so it no longer needs advancing.
        private bool Advance(float now)
        {
            var skippedPhases = 0;

            while (true)
            {
                if (!phaseIsStarted)
                {
                    var startTime = phaseStartTime + phaseDuration;

                    if (now - startTime > maximumCatchUpTime)
                    {
                        startTime = now;
                    }

                    var secondsIntoPhase = 0f;

                    // Phase is a "backwards" shift through time, which skips the States it covers entirely, then
                    // starts partway into the State it ends in
                    if (remainingPhaseOffset > 0)
                    {
                        var period = GetFunction(currentState).period;
                        remainingPhaseOffset -= period;

                        if (remainingPhaseOffset > 0)
                        {
                            // Without any periods to use it up (all are 0), the offset would never run out
                            if (++skippedPhases > 6)
                            {
                                remainingPhaseOffset = 0;
                            }

                            if (!MoveToNextState())
                            {
                                return false;
                            }

                            continue;
                        }

                        secondsIntoPhase = (period + remainingPhaseOffset) * minimumTickDuration;
                    }

                    StartPhase(startTime - secondsIntoPhase);
                }

                var elapsed = now - phaseStartTime;

                if (elapsed < phaseDuration)
                {
                    ShowPhase(now, elapsed / phaseDuration);
                    return true;
                }

                // Lights that move to an intensity end the phase exactly on it, which the next phase starts from
                if (phaseFunction.function == _linear_lighting_function || phaseFunction.function == _smooth_lighting_function)
                {
                    CurrentLinearIntensity = phaseFinalIntensity;
                }

                phaseIsStarted = false;

                if (!MoveToNextState())
                {
                    return false;
                }
            }
        }

        // Each state adds a random part of the deltas (lightsource.cpp: change_light_state), and runs at least a tick
        private void StartPhase(float startTime)
        {
            phaseFunction = GetFunction(currentState);

            var periodTicks = phaseFunction.period + Random.Range(0, phaseFunction.delta_period + 1);

            phaseStartTime = startTime;
            phaseDuration = Mathf.Max(1, periodTicks) * minimumTickDuration;
            phaseInitialIntensity = CurrentLinearIntensity;
            phaseFinalIntensity = Mathf.Clamp01(AlephOneExtensions.FixedToFloat(phaseFunction.intensity + Random.Range(0, phaseFunction.delta_intensity + 1)));
            nextTickTime = float.MinValue;
            phaseIsStarted = true;

            switch (phaseFunction.function)
            {
                case _constant_lighting_function:
                    CurrentLinearIntensity = phaseFinalIntensity;
                    break;
                case _linear_lighting_function:
                case _smooth_lighting_function:
                case _flicker_lighting_function:
                case _random_lighting_function:
                case _fluorescent_lighting_function:
                    break;
                default:
                    throw new NotImplementedException($"Lighting Function: {phaseFunction.function}");
            }
        }

        private void ShowPhase(float now, float progress)
        {
            switch (phaseFunction.function)
            {
                case _linear_lighting_function:
                    CurrentLinearIntensity = Mathf.Lerp(phaseInitialIntensity, phaseFinalIntensity, progress);
                    break;
                case _smooth_lighting_function:
                    CurrentLinearIntensity = Mathf.Lerp(phaseInitialIntensity, phaseFinalIntensity, SmoothProgress(progress));
                    break;
                case _flicker_lighting_function:
                case _random_lighting_function:
                case _fluorescent_lighting_function:
                    if (now >= nextTickTime)
                    {
                        nextTickTime = now + minimumTickDuration;
                        ShowTick(progress);
                    }

                    break;
            }
        }

        // A new intensity each tick (lightsource.cpp): flicker's between the smooth and final ones, random's between the
        // initial and final ones, and fluorescent's either one
        private void ShowTick(float progress)
        {
            switch (phaseFunction.function)
            {
                case _flicker_lighting_function:
                    var smoothIntensity = Mathf.Lerp(phaseInitialIntensity, phaseFinalIntensity, SmoothProgress(progress));
                    CurrentLinearIntensity = Mathf.Lerp(smoothIntensity, phaseFinalIntensity, Random.value);
                    break;
                case _random_lighting_function:
                    CurrentLinearIntensity = Mathf.Lerp(phaseInitialIntensity, phaseFinalIntensity, Random.value);
                    break;
                default:
                    CurrentLinearIntensity = Random.Range(0, 2) == 0 ? phaseInitialIntensity : phaseFinalIntensity;
                    break;
            }
        }

        // The "sine transition" of smooth (and flicker's) phases (lightsource.cpp), as a half
        // cosine wave from 0 to 1
        private static float SmoothProgress(float progress)
        {
            return 0.5f - 0.5f * Mathf.Cos(Mathf.PI * progress);
        }

        // Returns false when the light can stay as it is from now on
        private bool MoveToNextState()
        {
            if (loopsCurrentState)
            {
                return true;
            }

            switch (currentState)
            {
                case States.BecomingActive:
                    currentState = States.PrimaryActive;
                    return true;
                case States.PrimaryActive:
                    if (GoesToSecondaryPhase(NativeObject.primary_active, NativeObject.secondary_active))
                    {
                        currentState = States.SecondaryActive;
                        return true;
                    }

                    return !IsUnchanging(NativeObject.primary_active);
                case States.SecondaryActive:
                    currentState = LIGHT_IS_STATELESS(NativeObject) ? States.BecomingInactive : States.PrimaryActive;
                    return true;
                case States.BecomingInactive:
                    currentState = States.PrimaryInactive;
                    return true;
                case States.PrimaryInactive:
                    if (GoesToSecondaryPhase(NativeObject.primary_inactive, NativeObject.secondary_inactive))
                    {
                        currentState = States.SecondaryInactive;
                        return true;
                    }

                    return !IsUnchanging(NativeObject.primary_inactive);
                case States.SecondaryInactive:
                    currentState = LIGHT_IS_STATELESS(NativeObject) ? States.BecomingActive : States.PrimaryInactive;
                    return true;
                default:
                    throw new Exception($"Light State: {currentState}");
            }
        }

        // Only go to the second phase if it has a lasting duration
        // and if it's not constant at the same intensity as the primary phase.
        private bool GoesToSecondaryPhase(lighting_function_specification primary, lighting_function_specification secondary)
        {
            return LIGHT_IS_STATELESS(NativeObject) ||
                   (secondary.period > 0 &&
                    (secondary.function != _constant_lighting_function ||
                     secondary.intensity != primary.intensity));
        }

        // Repeating a constant phase with no random intensity changes nothing, so there's no reason to keep updating it
        private static bool IsUnchanging(lighting_function_specification lightingFunction)
        {
            return lightingFunction.function == _constant_lighting_function && lightingFunction.delta_intensity == 0;
        }

        private lighting_function_specification GetFunction(States state)
        {
            switch (state)
            {
                case States.BecomingActive:
                    return NativeObject.becoming_active;
                case States.PrimaryActive:
                    return NativeObject.primary_active;
                case States.SecondaryActive:
                    return NativeObject.secondary_active;
                case States.BecomingInactive:
                    return NativeObject.becoming_inactive;
                case States.PrimaryInactive:
                    return NativeObject.primary_inactive;
                case States.SecondaryInactive:
                    return NativeObject.secondary_inactive;
                default:
                    throw new Exception($"Light State: {state}");
            }
        }

        private void StartAnimating()
        {
            if (isAnimating)
            {
                return;
            }

            isAnimating = true;
            animatingLights.Add(this);

            if (!animationLoopIsRunning)
            {
                RunAnimationLoop();
            }
        }

        private void StopAnimating()
        {
            if (!isAnimating)
            {
                return;
            }

            isAnimating = false;
            animatingLights.Remove(this);
        }

        private static async void RunAnimationLoop()
        {
            var generation = animationLoopGeneration;
            animationLoopIsRunning = true;

            // In the Editor this is when Play mode ends
            Application.quitting -= StopAllAnimation;
            Application.quitting += StopAllAnimation;

            try
            {
                while (animatingLights.Count > 0 && Application.isPlaying)
                {
                    await Awaitable.NextFrameAsync();

                    if (generation != animationLoopGeneration)
                    {
                        return;
                    }

                    var now = Time.realtimeSinceStartup;

                    // Backwards, so a finished light can be swapped out for the last one, which has already advanced
                    for (var i = animatingLights.Count - 1; i >= 0; i--)
                    {
                        var light = animatingLights[i];
                        bool keepsAnimating;

                        try
                        {
                            keepsAnimating = light.Advance(now);
                        }
                        catch (Exception exception)
                        {
                            // One broken light stops alone, rather than stopping every light
                            Debug.LogException(exception);
                            keepsAnimating = false;
                        }

                        if (!keepsAnimating)
                        {
                            light.isAnimating = false;

                            var lastIndex = animatingLights.Count - 1;
                            animatingLights[i] = animatingLights[lastIndex];
                            animatingLights.RemoveAt(lastIndex);
                        }
                    }
                }
            }
            finally
            {
                if (generation == animationLoopGeneration)
                {
                    animationLoopIsRunning = false;
                }
            }
        }

        private static void StopAllAnimation()
        {
            Application.quitting -= StopAllAnimation;

            animationLoopGeneration++;
            animationLoopIsRunning = false;

            foreach (var light in animatingLights)
            {
                light.isAnimating = false;
            }

            animatingLights.Clear();
        }

        private static void SetIntensity(int index, float intensity)
        {
            if (intensities[index] == intensity)
            {
                return;
            }

            intensities[index] = intensity;
            dirtyStart = Mathf.Min(dirtyStart, index);
            dirtyEnd = Mathf.Max(dirtyEnd, index + 1);
        }

        // The buffer keeps its intensities when it grows, and is never shrunk, so it can be reused by every level
        private static void EnsureIntensitiesCapacity(int count)
        {
            if (intensitiesBuffer != null && intensitiesBuffer.IsValid() && intensitiesBuffer.count >= count)
            {
                return;
            }

            var capacity = Mathf.Max(minimumIntensitiesCapacity, Mathf.NextPowerOfTwo(count));

            Array.Resize(ref intensities, capacity);

            intensitiesBuffer?.Release();
            intensitiesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacity, sizeof(float));
            Shader.SetGlobalBuffer(lightIntensitiesGlobalPropertyId, intensitiesBuffer);

            dirtyStart = 0;
            dirtyEnd = capacity;

            RenderPipelineManager.beginContextRendering -= UploadIntensities;
            RenderPipelineManager.beginContextRendering += UploadIntensities;

            // In the Editor this is when Play mode ends
            Application.quitting -= ReleaseIntensities;
            Application.quitting += ReleaseIntensities;
        }

        private static void UploadIntensities(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (dirtyEnd <= dirtyStart || intensitiesBuffer == null)
            {
                return;
            }

            intensitiesBuffer.SetData(intensities, dirtyStart, dirtyStart, dirtyEnd - dirtyStart);

            dirtyStart = int.MaxValue;
            dirtyEnd = 0;
        }

        private static void ReleaseIntensities()
        {
            RenderPipelineManager.beginContextRendering -= UploadIntensities;
            Application.quitting -= ReleaseIntensities;

            intensitiesBuffer?.Release();
            intensitiesBuffer = null;
            intensities = Array.Empty<float>();
            dirtyStart = int.MaxValue;
            dirtyEnd = 0;
        }
    }
}
