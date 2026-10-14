using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForgePlus.ApplicationGeneral
{
    // Surface Cache Global Illumination renders each new material (its Meta pass) into a texture array that it grows by a
    // slice at a time, copying every slice over each time, so a level's worth of new materials at once can stall the GPU
    // until the device is lost. Surfaces with materials it doesn't have yet are held out of it (on a rendering layer its
    // filter excludes: the Effects volume profile's lightTransportRenderingLayerMask), and let in a few new materials a
    // frame. Light layers aren't used, so the rendering layer changes nothing else about how surfaces are drawn.
    [AutoStaticsCleanup]
    public static partial class SurfaceCacheAdmission
    {
        private const uint AdmittedRenderingLayerMask = 1u << 0;
        private const uint HeldRenderingLayerMask = 1u << 7;

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

        // Before Surface Cache Global Illumination is turned on, so its first frame doesn't take every surface at once
        public static void SetEnabled(bool enabled)
        {
            isEnabled = enabled;

            surfaceRenderers.RemoveWhere(renderer => !renderer);
            heldRenderers.Clear();
            admittedMaterials.Clear();

            foreach (var renderer in surfaceRenderers)
            {
                Admit(renderer, hold: enabled);
            }

            if (enabled)
            {
                StartAdmittingEachFrame();
            }
        }

        // After a surface's materials change
        public static void Admit(MeshRenderer renderer)
        {
            surfaceRenderers.Add(renderer);
            heldRenderers.Remove(renderer);

            Admit(renderer, hold: isEnabled && renderer.sharedMaterials.Any(material => material && !admittedMaterials.Contains(material)));

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
        }

        private static void Admit(MeshRenderer renderer, bool hold)
        {
            if (hold)
            {
                renderer.renderingLayerMask = HeldRenderingLayerMask;
                heldRenderers.Add(renderer);
            }
            else
            {
                renderer.renderingLayerMask = AdmittedRenderingLayerMask;
                admittedMaterials.UnionWith(renderer.sharedMaterials.Where(material => material));
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
                Admit(renderer, hold: false);
            }
        }
    }
}
