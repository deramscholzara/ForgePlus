using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using RuntimeCore.Common;
using System;
using System.Collections.Generic;
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

        // Every light's display intensity, which surfaces read by their material's _LightIndex as they're drawn. A global
        // array, so it changes no materials (Surface Cache Global Illumination re-renders any material that changes).
        public static readonly int lightIntensitiesPropertyId = Shader.PropertyToID("_LightIntensities");
        public const int LightIntensitiesLength = 1024;

        // The array's last entry is always full intensity, for surfaces with no light (such as a null side's)
        public const int FullIntensityLightIndex = LightIntensitiesLength - 1;

        // Each surface material is lit by one light (SurfaceMaterials): its index into the array, and the intensity Surface
        // Cache Global Illumination takes from its Meta pass, which changes far less often than the light does
        public static readonly int lightIndexPropertyId = Shader.PropertyToID("_LightIndex");
        public static readonly int lightIntensityPropertyId = Shader.PropertyToID("_LightIntensity");

        // Surface Cache Global Illumination's intensity skips changes smaller than this (of the light's own 0-1
        // intensity), and is applied to this many materials a frame at most, as it re-renders each material it changes
        private const float surfaceCacheIntensityThreshold = 0.45f;
        private const int surfaceCacheMaterialsPerFrame = 2;

        private static readonly float[] displayIntensities = CreateFullIntensities();
        private static bool displayIntensitiesChanged = true;

        // Lights whose Surface Cache Global Illumination intensity is still being applied to their materials, oldest first
        private static readonly List<LevelEntity_Light> surfaceCachePending = new List<LevelEntity_Light>();
        private static int lastSurfaceCacheFrame = -1;

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
                SetIntensity(value, value);
            }
        }

        private float currentLinearIntensity = 0f;

        // The surface materials it lights
        private readonly List<Material> subscribedMaterials = new List<Material>();

        // The intensity (the light's own, 0-1) Surface Cache Global Illumination was last given, and how many of the
        // materials have it so far
        private float surfaceCacheIntensity = float.NaN;
        private int surfaceCacheMaterialsApplied;

        // Set as the light changes between active and inactive, so the next phase gives every material its intensity at once
        // (and holds it through that phase)
        private bool statusChanged;
        private bool holdsSurfaceCacheIntensity;

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
            surfaceCachePending.Remove(this);
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

            EnterState(state);
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

            // Once as the light becomes active or inactive, whatever the change, so bounced light follows it: the phase's
            // final intensity, held through the phase (which may start from the old one) so it isn't taken back
            holdsSurfaceCacheIntensity = false;

            if (statusChanged)
            {
                statusChanged = false;
                SetSurfaceCacheIntensity(phaseFinalIntensity, immediately: true);
                holdsSurfaceCacheIntensity = true;
            }

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
        // initial and final ones, and fluorescent's either one. Surface Cache Global Illumination is given the average of
        // that range instead, as it re-renders the materials of every change it takes.
        private void ShowTick(float progress)
        {
            switch (phaseFunction.function)
            {
                case _flicker_lighting_function:
                    var smoothIntensity = Mathf.Lerp(phaseInitialIntensity, phaseFinalIntensity, SmoothProgress(progress));
                    SetIntensity(Mathf.Lerp(smoothIntensity, phaseFinalIntensity, Random.value), (smoothIntensity + phaseFinalIntensity) / 2f);
                    break;
                case _random_lighting_function:
                    SetIntensity(Mathf.Lerp(phaseInitialIntensity, phaseFinalIntensity, Random.value), (phaseInitialIntensity + phaseFinalIntensity) / 2f);
                    break;
                default:
                    SetIntensity(Random.Range(0, 2) == 0 ? phaseInitialIntensity : phaseFinalIntensity, (phaseInitialIntensity + phaseFinalIntensity) / 2f);
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
                    EnterState(States.PrimaryActive);
                    return true;
                case States.PrimaryActive:
                    if (GoesToSecondaryPhase(NativeObject.primary_active, NativeObject.secondary_active))
                    {
                        EnterState(States.SecondaryActive);
                        return true;
                    }

                    return !IsUnchanging(NativeObject.primary_active);
                case States.SecondaryActive:
                    EnterState(LIGHT_IS_STATELESS(NativeObject) ? States.BecomingInactive : States.PrimaryActive);
                    return true;
                case States.BecomingInactive:
                    EnterState(States.PrimaryInactive);
                    return true;
                case States.PrimaryInactive:
                    if (GoesToSecondaryPhase(NativeObject.primary_inactive, NativeObject.secondary_inactive))
                    {
                        EnterState(States.SecondaryInactive);
                        return true;
                    }

                    return !IsUnchanging(NativeObject.primary_inactive);
                case States.SecondaryInactive:
                    EnterState(LIGHT_IS_STATELESS(NativeObject) ? States.BecomingActive : States.PrimaryInactive);
                    return true;
                default:
                    throw new Exception($"Light State: {currentState}");
            }
        }

        // Notes a change between active and inactive (a stateless light's own cycle, or a switch or trigger), which the next
        // phase gives Surface Cache Global Illumination at once
        private void EnterState(States state)
        {
            if (IsActiveState(state) != IsActiveState(currentState))
            {
                statusChanged = true;
            }

            currentState = state;
        }

        private static bool IsActiveState(States state)
        {
            return state == States.BecomingActive || state == States.PrimaryActive || state == States.SecondaryActive;
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
                            light.SettleSurfaceCacheIntensity();

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

        public void SubscribeMaterial(Material material)
        {
            subscribedMaterials.Add(material);

            ApplyIntensity(material);
        }

        public void UnsubscribeMaterial(Material material)
        {
            subscribedMaterials.Remove(material);
        }

        // Its index into the display intensities, and Surface Cache Global Illumination's intensity. Shaders without the
        // properties don't keep them, so a material that changes shader has them applied again.
        public void ApplyIntensity(Material material)
        {
            material.SetFloat(lightIndexPropertyId, NativeIndex >= 0 && NativeIndex < FullIntensityLightIndex ? NativeIndex : FullIntensityLightIndex);
            ApplySurfaceCacheIntensity(material);
        }

        private void SetIntensity(float linearIntensity, float surfaceCacheLinearIntensity)
        {
            currentLinearIntensity = linearIntensity;
            CurrentDisplayIntensity = ToDisplayIntensity(linearIntensity);

            if (NativeIndex >= 0 && NativeIndex < FullIntensityLightIndex && displayIntensities[NativeIndex] != CurrentDisplayIntensity)
            {
                displayIntensities[NativeIndex] = CurrentDisplayIntensity;
                displayIntensitiesChanged = true;
            }

            if (!holdsSurfaceCacheIntensity &&
                (float.IsNaN(surfaceCacheIntensity) ||
                 Mathf.Abs(surfaceCacheLinearIntensity - surfaceCacheIntensity) >= surfaceCacheIntensityThreshold))
            {
                SetSurfaceCacheIntensity(surfaceCacheLinearIntensity, immediately: false);
            }
        }

        // Square to convert to gamma-space values (only needed if the project is in Linear space)
        private static float ToDisplayIntensity(float linearIntensity)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? linearIntensity * linearIntensity : linearIntensity;
        }

        // Right away to every material, or a few materials a frame (ApplyPendingSurfaceCacheIntensities)
        private void SetSurfaceCacheIntensity(float linearIntensity, bool immediately)
        {
            surfaceCacheIntensity = linearIntensity;

            if (immediately)
            {
                foreach (var material in subscribedMaterials)
                {
                    ApplySurfaceCacheIntensity(material);
                }

                surfaceCacheMaterialsApplied = subscribedMaterials.Count;
                surfaceCachePending.Remove(this);
            }
            else
            {
                surfaceCacheMaterialsApplied = 0;

                if (!surfaceCachePending.Contains(this))
                {
                    surfaceCachePending.Add(this);
                }
            }
        }

        // A light that stops changing leaves Surface Cache Global Illumination where it stopped, however small the
        // difference, rather than where the threshold last let it be
        private void SettleSurfaceCacheIntensity()
        {
            if (surfaceCacheIntensity != currentLinearIntensity)
            {
                SetSurfaceCacheIntensity(currentLinearIntensity, immediately: false);
            }
        }

        // False when the material already had it (setting a value it has would still re-render it)
        private bool ApplySurfaceCacheIntensity(Material material)
        {
            var intensity = ToDisplayIntensity(float.IsNaN(surfaceCacheIntensity) ? currentLinearIntensity : surfaceCacheIntensity);

            if (material.HasFloat(lightIntensityPropertyId) && material.GetFloat(lightIntensityPropertyId) == intensity)
            {
                return false;
            }

            material.SetFloat(lightIntensityPropertyId, intensity);
            return true;
        }

        private static float[] CreateFullIntensities()
        {
            var intensities = new float[LightIntensitiesLength];
            for (var i = 0; i < intensities.Length; i++)
            {
                intensities[i] = 1f;
            }

            return intensities;
        }

        // Full intensity until a level's lights are set (in Edit mode, in material and Shader Graph previews, and after
        // Play mode ends)
        private static void ResetDisplayIntensities()
        {
            for (var i = 0; i < displayIntensities.Length; i++)
            {
                displayIntensities[i] = 1f;
            }

            Shader.SetGlobalFloatArray(lightIntensitiesPropertyId, displayIntensities);
            displayIntensitiesChanged = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void WatchRendering()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginContextRendering -= OnBeginRendering;
            UnityEngine.Rendering.RenderPipelineManager.beginContextRendering += OnBeginRendering;

            // In the Editor this is when Play mode ends
            Application.quitting -= ResetDisplayIntensities;
            Application.quitting += ResetDisplayIntensities;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void ResetDisplayIntensitiesInEditor()
        {
            ResetDisplayIntensities();
        }
#endif

        // Before each view renders: the display intensities once they've changed, and Surface Cache Global Illumination's
        // pending intensities once a frame (it re-renders each material it changes)
        private static void OnBeginRendering(UnityEngine.Rendering.ScriptableRenderContext context, List<Camera> cameras)
        {
            if (displayIntensitiesChanged)
            {
                Shader.SetGlobalFloatArray(lightIntensitiesPropertyId, displayIntensities);
                displayIntensitiesChanged = false;
            }

            if (Time.frameCount == lastSurfaceCacheFrame)
            {
                return;
            }

            lastSurfaceCacheFrame = Time.frameCount;

            ApplyPendingSurfaceCacheIntensities();
        }

        private static void ApplyPendingSurfaceCacheIntensities()
        {
            var remainingMaterials = surfaceCacheMaterialsPerFrame;

            while (remainingMaterials > 0 && surfaceCachePending.Count > 0)
            {
                var light = surfaceCachePending[0];

                if (light.surfaceCacheMaterialsApplied >= light.subscribedMaterials.Count)
                {
                    surfaceCachePending.RemoveAt(0);
                    continue;
                }

                var material = light.subscribedMaterials[light.surfaceCacheMaterialsApplied++];
                if (material && light.ApplySurfaceCacheIntensity(material))
                {
                    remainingMaterials--;
                }
            }
        }
    }
}
