#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Marks geometry in the level view itself while Diagnostic Visuals is on (in every mode): a concave polygon's floor is
    // overlaid in red, and the floors of polygons whose volumes intersect (which is allowed, so it isn't an error) in
    // faint yellow. Overlays share their surface's mesh (and follow its transform), so they keep up with its shape as it's
    // edited and with its platform as it moves. Found again whenever the level's errors are, and as geometry (or its
    // heights) is edited (RequestRefresh, at most once a frame).
    [AutoStaticsCleanup]
    public static partial class DiagnosticVisuals
    {
        private enum Marking
        {
            Concave,
            Intersecting,
        }

        private static readonly Color ConcaveColor = new Color(0.75f, 0f, 0f, 0.75f);
        private static readonly Color IntersectingColor = new Color(1f, 1f, 0f, 0.2f);

        private static readonly int colorPropertyId = Shader.PropertyToID("_Color");

        // Recreated when destroyed, such as at the end of a Play session
        private static Material concaveMaterial;
        private static Material intersectingMaterial;

        private static Dictionary<(LevelEntity_Polygon, Marking), GameObject> overlays = new Dictionary<(LevelEntity_Polygon, Marking), GameObject>();

        private static bool isRefreshRequested;

        private static Material MaterialFor(Marking marking)
        {
            switch (marking)
            {
                case Marking.Concave:
                    return GetMaterial(ref concaveMaterial, "Concave", ConcaveColor);
                default:
                    return GetMaterial(ref intersectingMaterial, "Intersecting", IntersectingColor);
            }
        }

        private static Material GetMaterial(ref Material material, string name, Color color)
        {
            if (!material)
            {
                material = new Material(Shader.Find("ForgePlus/DiagnosticOverlay")) { name = "Diagnostic Overlay (" + name + ")" };
                material.SetColor(colorPropertyId, color);
            }

            return material;
        }

        // Once, as the frame ends (ForgePlusUI)
        public static void RequestRefresh()
        {
            isRefreshRequested = true;
        }

        public static void RefreshIfRequested()
        {
            if (isRefreshRequested)
            {
                Refresh();
            }
        }

        public static void Refresh()
        {
            isRefreshRequested = false;

            var level = LevelEntity_Level.Instance;
            var marked = new HashSet<(LevelEntity_Polygon, Marking)>();

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
                        marked.Add((polygon, Marking.Concave));
                    }
                }

                foreach (var polygonIndex in GeometryValidation.OverlappingPolygons(data))
                {
                    if (level.Polygons.TryGetValue(polygonIndex, out var polygon))
                    {
                        marked.Add((polygon, Marking.Intersecting));
                    }
                }
            }

            // Those no longer marked (or whose surfaces are gone, with a level that was rebuilt or closed)
            foreach (var overlay in overlays.ToList())
            {
                if (!overlay.Key.Item1 || !overlay.Value || !marked.Contains(overlay.Key))
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

            foreach (var mark in marked)
            {
                var (polygon, marking) = mark;
                if (!overlays.ContainsKey(mark) && polygon.FloorSurface)
                {
                    overlays[mark] = CreateOverlay(polygon.FloorSurface, MaterialFor(marking));
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
            GeometryUtilities.ExcludeFromGpuResidentDrawer(overlayRenderer);
            SurfaceCacheRenderingLayers.Exclude(overlayRenderer);

            return overlay;
        }
    }
}
#endif
