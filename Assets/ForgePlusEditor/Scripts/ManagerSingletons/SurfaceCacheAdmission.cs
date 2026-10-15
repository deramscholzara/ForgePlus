using System.Collections.Generic;
using System.Linq;
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

        // Every surface renderer, and those still held out
        private static readonly HashSet<MeshRenderer> surfaceRenderers = new HashSet<MeshRenderer>();
        private static readonly List<MeshRenderer> heldRenderers = new List<MeshRenderer>();

        // The materials of the surfaces let in
        private static readonly HashSet<Material> admittedMaterials = new HashSet<Material>();

        // Surfaces it never takes in (placeholders, and sides too small to matter)
        private static readonly HashSet<MeshRenderer> excludedRenderers = new HashSet<MeshRenderer>();

        // Before Surface Cache Global Illumination is turned on, so its first frame doesn't take every surface at once
        public static void SetEnabled(bool enabled)
        {
            isEnabled = enabled;

            surfaceRenderers.RemoveWhere(renderer => !renderer);
            heldRenderers.Clear();
            admittedMaterials.Clear();

            excludedRenderers.RemoveWhere(renderer => !renderer);

            foreach (var renderer in surfaceRenderers)
            {
                if (!excludedRenderers.Contains(renderer))
                {
                    Place(renderer, hold: enabled);
                }
            }

            if (enabled)
            {
                StartAdmittingEachFrame();
            }
        }

        // After a surface's materials or size change
        public static void Admit(MeshRenderer renderer, bool isExcluded)
        {
            surfaceRenderers.Add(renderer);
            heldRenderers.Remove(renderer);

            if (isExcluded)
            {
                excludedRenderers.Add(renderer);
                SetRenderingLayer(renderer, SurfaceCacheRenderingLayers.Excluded);
                return;
            }

            excludedRenderers.Remove(renderer);

            Place(renderer, hold: isEnabled && renderer.sharedMaterials.Any(material => material && !admittedMaterials.Contains(material)));

            if (isEnabled)
            {
                StartAdmittingEachFrame();
            }
        }

        // A closed level's surfaces and materials are gone
        public static void Forget()
        {
            surfaceRenderers.Clear();
            heldRenderers.Clear();
            admittedMaterials.Clear();
            excludedRenderers.Clear();
        }

        private static void Place(MeshRenderer renderer, bool hold)
        {
            if (hold)
            {
                SetRenderingLayer(renderer, SurfaceCacheRenderingLayers.Excluded);
                heldRenderers.Add(renderer);
            }
            else
            {
                SetRenderingLayer(renderer, SurfaceCacheRenderingLayers.Included);
                admittedMaterials.UnionWith(renderer.sharedMaterials.Where(material => material));
            }
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

        private static void AdmitHeldRenderers(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (!isEnabled || heldRenderers.Count == 0)
            {
                StopAdmittingEachFrame();
                return;
            }

            if (Time.frameCount == lastAdmissionFrame)
            {
                return;
            }

            lastAdmissionFrame = Time.frameCount;

            var remainingNewMaterials = NewMaterialsPerFrame;
            var newMaterials = new HashSet<Material>();

            for (var i = 0; i < heldRenderers.Count; i++)
            {
                var renderer = heldRenderers[i];
                if (!renderer)
                {
                    heldRenderers.RemoveAt(i--);
                    continue;
                }

                newMaterials.Clear();
                newMaterials.UnionWith(renderer.sharedMaterials.Where(material => material && !admittedMaterials.Contains(material)));

                // A surface with more new materials than a frame allows still gets in, on a frame of its own
                if (newMaterials.Count > remainingNewMaterials &&
                    remainingNewMaterials < NewMaterialsPerFrame)
                {
                    continue;
                }

                remainingNewMaterials -= newMaterials.Count;
                heldRenderers.RemoveAt(i--);
                Place(renderer, hold: false);
            }
        }
    }
}
