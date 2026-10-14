using UnityEngine;

namespace ForgePlus.LevelManipulation.Utilities
{
    // The rendering layers that decide what Surface Cache Global Illumination takes in: its filter (the Effects volume
    // profile's lightTransportRenderingLayerMask) excludes Excluded. Light layers aren't used, so the layers change nothing
    // else about how renderers are drawn.
    public static class SurfaceCacheRenderingLayers
    {
        // Level geometry it takes in
        public const uint Included = 1u << 0;

        // Level geometry it's yet to take in (SurfaceCacheAdmission), and tooling visuals (selection indicators, sprites,
        // overlays, grids), which shouldn't add or reflect light, and aren't worth the work of tracking as they change
        public const uint Excluded = 1u << 7;

        // For a tooling visual's renderer, as it's made (before it's first drawn)
        public static void Exclude(Renderer renderer)
        {
            renderer.renderingLayerMask = Excluded;
        }
    }
}
