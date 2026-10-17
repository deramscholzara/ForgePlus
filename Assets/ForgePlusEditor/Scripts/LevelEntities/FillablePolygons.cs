#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.History;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.PolygonContainment;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // With Geometry mode's Draw tool, each loop of lines that could be filled with a polygon (PolygonClosing:
    // FindEmptyLoops) shows the floor that polygon would have, in see-through cyan. Double-clicking it fills the loop: the
    // polygon copies one at its corners, as closing a loop by drawing does, or else is a plain one. Its floor counts as
    // empty space for drawing a line from it (a press, release and press, then a drag: EmptySpaceDrawing). Found again
    // whenever the lines (or the floors they could copy) change.
    [AutoStaticsCleanup]
    public static partial class FillablePolygons
    {
        private static readonly Color PreviewColor = new Color(0f, 0.85f, 1f, 0.35f);

        private static readonly int colorPropertyId = Shader.PropertyToID("_Color");

        // Recreated when destroyed, such as at the end of a Play session
        private static Material material;

        private static List<GameObject> previews = new List<GameObject>();

        private static bool hasShown;
        private static int shownSignature;

        // Once a frame, from ForgePlusUI
        public static void Update()
        {
            var level = LevelEntity_Level.Instance;
            var isShown = level && level.Points != null && !LevelEditing.IsRebuildPending &&
                          ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry &&
                          ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Drawing;

            var signature = isShown ? Signature(level) : 0;
            if (hasShown && signature == shownSignature)
            {
                return;
            }

            hasShown = true;
            shownSignature = signature;

            Clear();

            if (!isShown)
            {
                return;
            }

            var data = level.Level;
            foreach (var loop in PolygonClosing.FindEmptyLoops(data, data.BuildEndpointLines()))
            {
                previews.Add(CreatePreview(level, loop));
            }
        }

        // Fills the loop with its polygon, rebuilding the level (its structure has changed), with the polygon selected (as
        // one action)
        public static void Fill(PolygonClosing.Loop loop)
        {
            var level = LevelEntity_Level.Instance;
            if (!level || LevelEditing.IsRebuildPending)
            {
                return;
            }

            SelectionManager.Instance.DeselectAll();

            var data = level.Level;
            var polygon = PolygonClosing.AddPolygon(data, loop, loop.Corners[0]);

            // Until the level is rebuilt, which works these out for its entities
            level.EndpointLines = data.BuildEndpointLines();
            PolygonContainmentMap.MarkChanged();

            LevelEditing.RebuildLevel(() =>
            {
                var rebuiltLevel = LevelEntity_Level.Instance;
                if (!rebuiltLevel || rebuiltLevel.Polygons == null)
                {
                    return;
                }

                var polygonIndex = (short) rebuiltLevel.Level.PolygonList.IndexOf(polygon);
                if (rebuiltLevel.Polygons.TryGetValue(polygonIndex, out var filled))
                {
                    // Undone and redone with the edit (which the rebuild has recorded)
                    SelectionHistory.JoinNextToLast();
                    SelectionManager.Instance.SelectObject(filled, multiSelect: false);
                }
            });
        }

        private static void Clear()
        {
            foreach (var preview in previews)
            {
                if (preview)
                {
                    // Destroying waits for the end of the frame, so it's hidden (and out of the pointer's way) now
                    preview.SetActive(false);
                    Object.Destroy(preview);
                }
            }

            previews.Clear();
        }

        // What the loops are found from: the level, its points, its lines and their polygons, and its floors' heights
        private static int Signature(LevelEntity_Level level)
        {
            var data = level.Level;
            var signature = new System.HashCode();
            signature.Add(level);
            signature.Add(data.EndpointList.Count);
            signature.Add(data.LineList.Count);
            signature.Add(data.PolygonList.Count);

            foreach (var endpoint in data.EndpointList)
            {
                signature.Add(endpoint.vertex.x);
                signature.Add(endpoint.vertex.y);
            }

            foreach (var line in data.LineList)
            {
                signature.Add(line.endpoint_indexes[0]);
                signature.Add(line.endpoint_indexes[1]);
                signature.Add(line.clockwise_polygon_owner);
                signature.Add(line.counterclockwise_polygon_owner);
            }

            foreach (var polygon in data.PolygonList)
            {
                signature.Add(polygon.floor_height);
            }

            return signature.ToHashCode();
        }

        // Its floor, at the height its polygon would have, under the level (so it goes with it)
        private static GameObject CreatePreview(LevelEntity_Level level, PolygonClosing.Loop loop)
        {
            var data = level.Level;
            var height = PolygonClosing.FloorHeight(data, loop) / GeometryUtilities.WorldUnitIncrementsPerMeter;

            var vertices = new Vector3[loop.Corners.Count];
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = data.EndpointList[loop.Corners[i]].vertex;
                vertices[i] = new Vector3(vertex.x / GeometryUtilities.WorldUnitIncrementsPerMeter, height, -vertex.y / GeometryUtilities.WorldUnitIncrementsPerMeter);
            }

            // A fan from its first corner (it's convex)
            var triangles = new int[(vertices.Length - 2) * 3];
            for (var i = 0; i < vertices.Length - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            var mesh = new Mesh { name = "Fillable Polygon", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var preview = new GameObject("Fillable Polygon");
            preview.transform.SetParent(level.transform, worldPositionStays: true);

            preview.AddComponent<MeshFilter>().sharedMesh = mesh;

            var previewRenderer = preview.AddComponent<MeshRenderer>();
            RuntimeSurfaceGeometry.ConfigureRenderer(previewRenderer);
            previewRenderer.sharedMaterial = Material;
            GeometryUtilities.ExcludeFromGpuResidentDrawer(previewRenderer);
            SurfaceCacheRenderingLayers.Exclude(previewRenderer);

            preview.AddComponent<MeshCollider>().sharedMesh = mesh;
            preview.AddComponent<FillablePolygonPreview>().Loop = loop;

            return preview;
        }

        private static Material Material
        {
            get
            {
                if (!material)
                {
                    material = new Material(Shader.Find("ForgePlus/DiagnosticOverlay")) { name = "Fillable Polygon Preview" };
                    material.SetColor(colorPropertyId, PreviewColor);
                }

                return material;
            }
        }
    }
}
#endif
