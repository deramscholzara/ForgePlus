#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation.Utilities;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // While looking straight down (the camera's orthographic view), a grid aligned to world units (the grid texture,
    // repeated once per world unit) is drawn under the whole level, as see-through as the Orthographic Grid Opacity
    // setting, while the Grid view option is on. It never moves: it's centered on the world, under the lowest floor any
    // level can have, and reaches well past the edges of the whole area any level can use.
    [AutoStaticsCleanup]
    public static partial class OrthographicGrid
    {
        // Meters past the edges of the area a level can use that it reaches, and below the lowest possible floor it's drawn
        private const float Margin = 500f;
        private const float Depth = 0.01f;

        private static readonly int opacityPropertyId = Shader.PropertyToID("_Opacity");
        private static readonly int metersPerWorldUnitPropertyId = Shader.PropertyToID("_MetersPerWorldUnit");

        // Recreated when destroyed, such as at the end of a Play session
        private static GameObject grid;
        private static Material material;
        private static float appliedOpacity = float.NaN;

        // Each frame (EditorCamera), with the level's bounds if a level is open
        public static void Update(bool isShown, Bounds? levelBounds)
        {
            if (!isShown || levelBounds == null || !SettingsManager.Instance.GridEnabled)
            {
                if (grid && grid.activeSelf)
                {
                    grid.SetActive(false);
                }

                return;
            }

            if (!grid)
            {
                Create();
            }

            var opacity = SettingsManager.Instance.OrthographicGridOpacity;
            if (opacity != appliedOpacity)
            {
                appliedOpacity = opacity;
                material.SetFloat(opacityPropertyId, opacity);
            }

            if (!grid.activeSelf)
            {
                grid.SetActive(true);
            }
        }

        private static void Create()
        {
            material = new Material(Shader.Find("ForgePlus/OrthographicGrid")) { name = "Orthographic Grid" };
            material.mainTexture = Resources.Load<Texture2D>("Walls/Grid");
            material.SetFloat(metersPerWorldUnitPropertyId, world.WORLD_ONE / GeometryUtilities.WorldUnitIncrementsPerMeter);

            // A unit square, facing up
            var mesh = new Mesh { name = "Orthographic Grid" };
            mesh.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();

            grid = new GameObject("Orthographic Grid");
            grid.AddComponent<MeshFilter>().sharedMesh = mesh;

            var gridRenderer = grid.AddComponent<MeshRenderer>();
            gridRenderer.sharedMaterial = material;
            gridRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            gridRenderer.receiveShadows = false;
            SurfaceCacheRenderingLayers.Exclude(gridRenderer);

            // Map coordinates and heights are shorts, so no level reaches past these
            var scale = GeometryUtilities.WorldUnitIncrementsPerMeter;
            var extent = (-(float)short.MinValue / scale) * 2f + Margin * 2f;
            grid.transform.position = new Vector3(0f, short.MinValue / scale - Depth, 0f);
            grid.transform.localScale = new Vector3(extent, 1f, extent);

            appliedOpacity = float.NaN;
        }
    }
}
#endif
