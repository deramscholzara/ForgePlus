using System.Collections.Generic;
using ForgePlus.LevelManipulation.Utilities;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForgePlus.ApplicationGeneral
{
    // Surface Cache Global Illumination renders each new material (its Meta pass) into a texture array that it grows by a
    // slice at a time, copying every slice over each time, so a level's worth of new materials at once can stall the GPU
    // until the device is lost. Surfaces with materials it doesn't have yet are held out of it (on the rendering layer it
    // excludes: SurfaceCacheRenderingLayers), and let in a few new materials a frame.
    [AutoStaticsCleanup]
    public static partial class SurfaceCacheAdmission
    {
        private const int NewMaterialsPerFrame = 4;

        private static bool isEnabled;
        private static bool isAdmittingEachFrame;

        // Rendering begins for each view (such as the Game and Scene views), but they share a frame's admissions
        private static int lastAdmissionFrame = -1;

        // Every surface renderer
        private static readonly HashSet<MeshRenderer> surfaceRenderers = new HashSet<MeshRenderer>();

        // Those still held out, by the materials each waits on (those not let in yet). Each held renderer is listed under
        // every material it waits on, and queued in the order it was held. Lists and the queue can keep renderers that have
        // since been let in or held again (they're skipped), so neither needs searching when that happens.
        private static readonly Dictionary<MeshRenderer, HashSet<Material>> heldRenderers = new Dictionary<MeshRenderer, HashSet<Material>>();
        private static readonly Dictionary<Material, List<MeshRenderer>> heldRenderersByMaterial = new Dictionary<Material, List<MeshRenderer>>();
        private static readonly Queue<MeshRenderer> heldRendererQueue = new Queue<MeshRenderer>();

        // The materials of the surfaces let in
        private static readonly HashSet<Material> admittedMaterials = new HashSet<Material>();

        // Surfaces it never takes in (placeholders, and sides too small to matter)
        private static readonly HashSet<MeshRenderer> excludedRenderers = new HashSet<MeshRenderer>();

        // Reused, so admitting doesn't allocate
        private static readonly List<Material> rendererMaterials = new List<Material>();
        private static readonly Stack<List<Material>> materialListPool = new Stack<List<Material>>();
        private static readonly Stack<HashSet<Material>> materialSetPool = new Stack<HashSet<Material>>();
        private static readonly Stack<List<MeshRenderer>> rendererListPool = new Stack<List<MeshRenderer>>();

        // Before Surface Cache Global Illumination is turned on, so its first frame doesn't take every surface at once
        public static void SetEnabled(bool enabled)
        {
            isEnabled = enabled;

            surfaceRenderers.RemoveWhere(renderer => !renderer);
            ClearHeld();
            admittedMaterials.Clear();

            excludedRenderers.RemoveWhere(renderer => !renderer);

            foreach (var renderer in surfaceRenderers)
            {
                if (!excludedRenderers.Contains(renderer))
                {
                    Place(renderer);
                }
            }

            if (enabled)
            {
                StartAdmittingEachFrame();
            }
        }

        // After a surface's materials or size change. Its materials, if they're known (null otherwise), save reading them
        // back from the renderer.
        public static void Admit(MeshRenderer renderer, IReadOnlyList<Material> materials, bool isExcluded)
        {
            surfaceRenderers.Add(renderer);
            Release(renderer);

            if (isExcluded)
            {
                excludedRenderers.Add(renderer);
                SetRenderingLayer(renderer, SurfaceCacheRenderingLayers.Excluded);
                return;
            }

            excludedRenderers.Remove(renderer);

            Place(renderer, materials);

            if (isEnabled)
            {
                StartAdmittingEachFrame();
            }
        }

        // A closed level's surfaces and materials are gone
        public static void Forget()
        {
            surfaceRenderers.Clear();
            ClearHeld();
            admittedMaterials.Clear();
            excludedRenderers.Clear();
        }

        // Held out while it has materials not let in yet (and Surface Cache Global Illumination is on), otherwise let in
        private static void Place(MeshRenderer renderer, IReadOnlyList<Material> materials = null)
        {
            if (materials == null)
            {
                renderer.GetSharedMaterials(rendererMaterials);
                materials = rendererMaterials;
            }

            if (!isEnabled)
            {
                LetIn(renderer, materials);
                return;
            }

            HashSet<Material> waitingOn = null;

            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material && !admittedMaterials.Contains(material))
                {
                    waitingOn ??= materialSetPool.Count > 0 ? materialSetPool.Pop() : new HashSet<Material>();
                    waitingOn.Add(material);
                }
            }

            if (waitingOn == null)
            {
                LetIn(renderer, materials);
                return;
            }

            SetRenderingLayer(renderer, SurfaceCacheRenderingLayers.Excluded);

            heldRenderers.Add(renderer, waitingOn);
            heldRendererQueue.Enqueue(renderer);

            foreach (var material in waitingOn)
            {
                if (!heldRenderersByMaterial.TryGetValue(material, out var waiting))
                {
                    waiting = rendererListPool.Count > 0 ? rendererListPool.Pop() : new List<MeshRenderer>();
                    heldRenderersByMaterial.Add(material, waiting);
                }

                waiting.Add(renderer);
            }
        }

        // Its materials are read from it if they aren't given
        private static void LetIn(MeshRenderer renderer, IReadOnlyList<Material> materials = null)
        {
            SetRenderingLayer(renderer, SurfaceCacheRenderingLayers.Included);

            // Its own list, as letting in the renderers that were only waiting on these materials lets them in here too
            List<Material> readMaterials = null;
            if (materials == null)
            {
                readMaterials = materialListPool.Count > 0 ? materialListPool.Pop() : new List<Material>();
                renderer.GetSharedMaterials(readMaterials);
                materials = readMaterials;
            }

            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material && admittedMaterials.Add(material))
                {
                    LetInRenderersWaitingOnlyOn(material);
                }
            }

            if (readMaterials != null)
            {
                readMaterials.Clear();
                materialListPool.Push(readMaterials);
            }
        }

        // A material that's in now brings in no new material with the renderers that were only waiting on it
        private static void LetInRenderersWaitingOnlyOn(Material material)
        {
            if (!heldRenderersByMaterial.Remove(material, out var waiting))
            {
                return;
            }

            foreach (var renderer in waiting)
            {
                if (heldRenderers.TryGetValue(renderer, out var waitingOn) &&
                    waitingOn.Remove(material) &&
                    waitingOn.Count == 0)
                {
                    Release(renderer);

                    if (renderer)
                    {
                        LetIn(renderer);
                    }
                }
            }

            waiting.Clear();
            rendererListPool.Push(waiting);
        }

        // No longer held (its lists and queue entry are skipped from now on)
        private static void Release(MeshRenderer renderer)
        {
            if (heldRenderers.Remove(renderer, out var waitingOn))
            {
                waitingOn.Clear();
                materialSetPool.Push(waitingOn);
            }
        }

        private static void ClearHeld()
        {
            foreach (var waitingOn in heldRenderers.Values)
            {
                waitingOn.Clear();
                materialSetPool.Push(waitingOn);
            }

            foreach (var waiting in heldRenderersByMaterial.Values)
            {
                waiting.Clear();
                rendererListPool.Push(waiting);
            }

            heldRenderers.Clear();
            heldRenderersByMaterial.Clear();
            heldRendererQueue.Clear();
        }

        // Only when it changes, as Surface Cache Global Illumination updates every renderer that's changed
        private static void SetRenderingLayer(MeshRenderer renderer, uint renderingLayerMask)
        {
            if (renderer.renderingLayerMask != renderingLayerMask)
            {
                renderer.renderingLayerMask = renderingLayerMask;
            }
        }

        private static void StartAdmittingEachFrame()
        {
            if (isAdmittingEachFrame)
            {
                return;
            }

            isAdmittingEachFrame = true;
            RenderPipelineManager.beginContextRendering += AdmitHeldRenderers;
        }

        private static void StopAdmittingEachFrame()
        {
            isAdmittingEachFrame = false;
            RenderPipelineManager.beginContextRendering -= AdmitHeldRenderers;
        }

        // In the order they were held, until this frame's new materials are used up (letting each one in also lets in
        // every renderer that was only waiting on its materials)
        private static void AdmitHeldRenderers(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (!isEnabled || heldRenderers.Count == 0)
            {
                heldRendererQueue.Clear();
                StopAdmittingEachFrame();
                return;
            }

            if (Time.frameCount == lastAdmissionFrame)
            {
                return;
            }

            lastAdmissionFrame = Time.frameCount;

            var remainingNewMaterials = NewMaterialsPerFrame;

            while (remainingNewMaterials > 0 && heldRendererQueue.Count > 0)
            {
                var renderer = heldRendererQueue.Peek();
                if (!heldRenderers.TryGetValue(renderer, out var waitingOn))
                {
                    heldRendererQueue.Dequeue();
                    continue;
                }

                if (!renderer)
                {
                    heldRendererQueue.Dequeue();
                    Release(renderer);
                    continue;
                }

                // A surface with more new materials than a frame allows still gets in, on a frame of its own
                if (waitingOn.Count > remainingNewMaterials &&
                    remainingNewMaterials < NewMaterialsPerFrame)
                {
                    break;
                }

                remainingNewMaterials -= waitingOn.Count;

                heldRendererQueue.Dequeue();
                Release(renderer);
                LetIn(renderer);
            }
        }
    }
}
