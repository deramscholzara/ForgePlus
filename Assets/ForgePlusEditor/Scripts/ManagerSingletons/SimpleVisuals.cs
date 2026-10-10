using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    // While Simple Visuals is on, every surface is drawn with the grid texture (lit as it would be) instead of its own
    // texture. Surfaces (and their batches) share the materials SurfaceBatchingManager makes for them, so swapping those
    // materials' shader swaps every surface's, including surfaces made later.
    [AutoStaticsCleanup]
    public static partial class SimpleVisuals
    {
        private static readonly int gridTexturePropertyId = Shader.PropertyToID("_ForgePlusGridTexture");

        private static bool isEnabled;

        // Each swapped material's own shader and render queue, to put back
        private static Dictionary<Material, (Shader Shader, int RenderQueue)> originals = new Dictionary<Material, (Shader, int)>();

        private static Shader gridShader;

        private static Shader GridShader
        {
            get
            {
                if (!gridShader)
                {
                    gridShader = Shader.Find("ForgePlus/SimpleVisuals");
                    Shader.SetGlobalTexture(gridTexturePropertyId, Resources.Load<Texture2D>("Walls/Grid"));
                }

                return gridShader;
            }
        }

        public static void SetEnabled(bool enabled, IEnumerable<Material> surfaceMaterials)
        {
            isEnabled = enabled;

            if (enabled)
            {
                foreach (var material in surfaceMaterials)
                {
                    Apply(material);
                }

                return;
            }

            foreach (var original in originals.Where(original => original.Key))
            {
                original.Key.shader = original.Value.Shader;
                original.Key.renderQueue = original.Value.RenderQueue;
            }

            originals.Clear();
        }

        // For a surface material that's just been made (or every one, as Simple Visuals is turned on)
        public static void Apply(Material material)
        {
            if (!isEnabled || !material || originals.ContainsKey(material))
            {
                return;
            }

            originals[material] = (material.shader, material.renderQueue);
            material.shader = GridShader;
            material.renderQueue = -1;
        }

        // A closed level's materials are gone
        public static void Forget()
        {
            originals.Clear();
        }
    }
}
