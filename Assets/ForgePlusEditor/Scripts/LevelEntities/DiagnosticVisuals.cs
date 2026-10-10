#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Marks problem geometry in the level view itself while Diagnostic Visuals is on (in every mode): a concave polygon's
    // floor is overlaid in red. Overlays share their surface's mesh (and follow its transform), so they keep up with its
    // shape as it's edited and with its platform as it moves. Found again whenever the level's errors are.
    [AutoStaticsCleanup]
    public static partial class DiagnosticVisuals
    {
        private static readonly Color ConcaveColor = new Color(0.75f, 0f, 0f, 0.75f);

        private static readonly int colorPropertyId = Shader.PropertyToID("_Color");

        // Recreated when destroyed, such as at the end of a Play session
        private static Material concaveMaterial;

        private static Dictionary<LevelEntity_Polygon, GameObject> overlays = new Dictionary<LevelEntity_Polygon, GameObject>();

        private static Material ConcaveMaterial
        {
            get
            {
                if (!concaveMaterial)
                {
                    concaveMaterial = new Material(Shader.Find("ForgePlus/DiagnosticOverlay")) { name = "Diagnostic Overlay (Concave)" };
                    concaveMaterial.SetColor(colorPropertyId, ConcaveColor);
                }

                return concaveMaterial;
            }
        }

        public static void Refresh()
        {
            var level = LevelEntity_Level.Instance;
            var marked = new HashSet<LevelEntity_Polygon>();

            if (level && level.Polygons != null && SettingsManager.Instance.DiagnosticVisualsEnabled && !LevelEditing.IsRebuildPending)
            {
                var data = level.Level;
                var issues = new List<GeometryIssue>();
                var lineCounts = GeometryValidation.LineCountsAtPoints(data);
                var straightCornerPoints = new HashSet<short>();

                for (short polygonIndex = 0; polygonIndex < data.PolygonList.Count; polygonIndex++)
                {
                    issues.Clear();
                    GeometryValidation.CheckPolygon(data, polygonIndex, issues, lineCounts, straightCornerPoints);

                    if (issues.Any(issue => issue.Kind == GeometryIssueKind.ConcaveCorner) && level.Polygons.TryGetValue(polygonIndex, out var polygon))
                    {
                        marked.Add(polygon);
                    }
                }
            }

            // Those no longer marked (or whose surfaces are gone, with a level that was rebuilt or closed)
            foreach (var overlay in overlays.ToList())
            {
                if (!overlay.Key || !overlay.Value || !marked.Contains(overlay.Key))
                {
                    if (overlay.Value)
                    {
                        // Destroying waits for the end of the frame
                        overlay.Value.SetActive(false);
                        Object.Destroy(overlay.Value);
                    }

                    overlays.Remove(overlay.Key);
                }
            }

            foreach (var polygon in marked)
            {
                if (!overlays.ContainsKey(polygon) && polygon.FloorSurface)
                {
                    overlays[polygon] = CreateOverlay(polygon.FloorSurface, ConcaveMaterial);
                }
            }
        }

        private static GameObject CreateOverlay(RuntimeSurfaceGeometry surface, Material material)
        {
            var overlay = new GameObject("Diagnostic Overlay");
            overlay.transform.SetParent(surface.transform, worldPositionStays: false);

            overlay.AddComponent<MeshFilter>().sharedMesh = surface.SurfaceMesh;

            var overlayRenderer = overlay.AddComponent<MeshRenderer>();
            RuntimeSurfaceGeometry.ConfigureRenderer(overlayRenderer);
            overlayRenderer.sharedMaterial = material;

            return overlay;
        }
    }
}
#endif
