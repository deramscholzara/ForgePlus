using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using AlephOne;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Points' handles, drawn over the level (behind the panels) at the top corners of their polygons, one for each height
    // a point's polygons meet it at, with nearer ones over farther ones. They're shown while Geometry mode shows points,
    // and are clicked and dragged through WorldPointer (as its screen picker), over a click area that can be bigger than
    // they're drawn (the Point Click Area setting).
    //
    // While a point is moved, what the move would break (or breaks) is shown (PointEditing.Feedback): the lines involved
    // are outlined (red for errors, yellow for warnings), a ghost of the point shows where it's held back from (or where
    // it last was valid), its handles are ringed, and a note beside it says what's wrong.
    public class PointHandles : IScreenPointerPicker
    {
        // Pixels: a black border around a white fill (red while selected, with a black center, as in ForgePlus's logo)
        private const float Size = 9f;
        private const float BorderWidth = 2f;
        private const float CenterSize = 3f;

        // Each handle has up to 12 vertices, which an allocation indexes with 16 bits
        private const int HandlesPerAllocation = 5000;

        private static readonly Color BorderColor = Color.black;
        private static readonly Color FillColor = Color.white;
        private static readonly Color SelectedFillColor = Color.red;
        private static readonly Color CenterColor = Color.black;

        // As the Errors panel marks errors and warnings (--fp-error and --fp-warning)
        private static readonly Color ErrorColor = new Color32(210, 40, 40, 255);
        private static readonly Color WarningColor = new Color32(240, 200, 30, 255);
        private static readonly Color BlockedGhostColor = new Color(210f / 255f, 40f / 255f, 40f / 255f, 0.5f);
        private static readonly Color LastValidGhostColor = new Color(1f, 1f, 1f, 0.5f);

        private const float RingWidth = 2f;
        private const float OutlineWidth = 2f;

        // Beside the point it's about, in pixels
        private static readonly Vector2 StatusOffset = new Vector2(12f, 12f);

        private struct Handle
        {
            public LevelEntity_Point Point;
            public Vector3 WorldPosition;

            // The drawn box's top left (whole pixels, so its edges are sharp)
            public Vector2 Corner;

            // Along the camera's view
            public float Depth;
        }

        private readonly VisualElement layer;
        private readonly Label status;

        // The current move's feedback, in panel space
        private readonly List<(Vector2 A, Vector2 B, Color Color)> outlines = new List<(Vector2, Vector2, Color)>();
        private readonly List<(Vector2 Corner, Color Color)> ghosts = new List<(Vector2, Color)>();
        private short ringedPoint = cstypes.NONE;
        private Color ringColor;

        // Those in view, farthest first (the order they're drawn in)
        private readonly List<Handle> handles = new List<Handle>();

        // Each point's heights that have a handle (as point index and height, in world units)
        private readonly HashSet<long> placedCorners = new HashSet<long>();

        public PointHandles(VisualElement root)
        {
            layer = WorldLayer.Create(root, "point-handles");
            layer.generateVisualContent += GenerateVisualContent;

            // Level text isn't markup
            status = new Label { enableRichText = false, pickingMode = PickingMode.Ignore };
            status.AddToClassList("fp-point-status");
            status.style.display = DisplayStyle.None;
            layer.Add(status);
        }

        // After the camera has moved for the frame (and platforms, whose ceilings carry their handles)
        public void Update()
        {
            var hadHandles = handles.Count > 0;
            handles.Clear();

            var level = LevelEntity_Level.Instance;
            var camera = Camera.main;
            var panel = layer.panel;

            var hadFeedback = outlines.Count > 0 || ghosts.Count > 0 || ringedPoint != cstypes.NONE;
            outlines.Clear();
            ghosts.Clear();
            ringedPoint = cstypes.NONE;
            status.style.display = DisplayStyle.None;

            if (IsShown(level) && camera && panel != null)
            {
                CollectHandles(level, camera, panel);
                CollectFeedback(level, camera, panel);
            }

            if (hadHandles || handles.Count > 0 || hadFeedback)
            {
                layer.MarkDirtyRepaint();
            }
        }

        // The nearest handle whose click area is under the pointer
        public bool TryPick(Vector2 screenPosition, out IWorldPointerHandler handler, out Vector3 worldPosition)
        {
            handler = null;
            worldPosition = default;

            var panel = layer.panel;
            if (handles.Count == 0 || panel == null || !IsShown(LevelEntity_Level.Instance))
            {
                return false;
            }

            var position = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            var halfClickSize = Size * SettingsManager.Instance.PointClickAreaScale * 0.5f;

            for (var i = handles.Count - 1; i >= 0; i--)
            {
                var handle = handles[i];
                var center = handle.Corner + new Vector2(Size * 0.5f, Size * 0.5f);

                if (Mathf.Abs(position.x - center.x) <= halfClickSize && Mathf.Abs(position.y - center.y) <= halfClickSize)
                {
                    handler = handle.Point;
                    worldPosition = handle.WorldPosition;
                    return true;
                }
            }

            return false;
        }

        // Not while the UI is hidden, or while the level's data is ahead of its (soon rebuilt) entities
        private bool IsShown(LevelEntity_Level level)
        {
            return level && level.Points != null && !LevelEditing.IsRebuildPending &&
                   SettingsManager.Instance.PointsAreShown &&
                   layer.parent != null && layer.parent.resolvedStyle.display != DisplayStyle.None;
        }

        private void CollectHandles(LevelEntity_Level level, Camera camera, IPanel panel)
        {
            placedCorners.Clear();

            var data = level.Level;
            var viewRect = camera.pixelRect;

            foreach (var polygon in level.Polygons.Values)
            {
                // Where it's drawn (a platform's ceiling moves)
                var ceiling = polygon.CeilingSurface;
                if (!ceiling)
                {
                    continue;
                }

                var height = ceiling.transform.position.y;
                var heightKey = (uint) Mathf.RoundToInt(height * GeometryUtilities.WorldUnitIncrementsPerMeter);
                var nativePolygon = polygon.NativeObject;

                for (var i = 0; i < nativePolygon.vertex_count; i++)
                {
                    var pointIndex = nativePolygon.endpoint_indexes[i];

                    if (!placedCorners.Add(((long) pointIndex << 32) | heightKey) ||
                        !level.Points.TryGetValue(pointIndex, out var point))
                    {
                        continue;
                    }

                    var worldPosition = GeometryUtilities.GetMeshVertex(data, pointIndex);
                    worldPosition.y = height;

                    var screenPosition = camera.WorldToScreenPoint(worldPosition);
                    if (screenPosition.z <= 0f || !viewRect.Contains(screenPosition))
                    {
                        continue;
                    }

                    var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));

                    handles.Add(new Handle
                    {
                        Point = point,
                        WorldPosition = worldPosition,
                        Corner = new Vector2(Mathf.Floor(panelPosition.x) - Mathf.Floor(Size * 0.5f), Mathf.Floor(panelPosition.y) - Mathf.Floor(Size * 0.5f)),
                        Depth = screenPosition.z,
                    });
                }
            }

            handles.Sort((a, b) => b.Depth.CompareTo(a.Depth));
        }

        // What the current move would break (or breaks), as PointEditing reports it
        private void CollectFeedback(LevelEntity_Level level, Camera camera, IPanel panel)
        {
            var feedback = PointEditing.Feedback;
            if (feedback == null || feedback.PointIndex < 0 || feedback.PointIndex >= level.Level.EndpointList.Count)
            {
                return;
            }

            var data = level.Level;

            // The point is shown where it's held back from, if it is
            Vector3 WorldPosition(short pointIndex)
            {
                var vertex = pointIndex == feedback.PointIndex && feedback.IsBlocked ? feedback.BlockedLocation : data.EndpointList[pointIndex].vertex;

                return new Vector3(vertex.x, 0f, -vertex.y) / GeometryUtilities.WorldUnitIncrementsPerMeter + Vector3.up * feedback.Height;
            }

            foreach (var issue in feedback.Issues)
            {
                var color = issue.IsWarning ? WarningColor : ErrorColor;
                foreach (var (a, b) in issue.Segments)
                {
                    if (TryProject(WorldPosition(a), camera, panel, out var panelA) && TryProject(WorldPosition(b), camera, panel, out var panelB))
                    {
                        outlines.Add((panelA, panelB, color));
                    }
                }
            }

            var statusAt = WorldPosition(feedback.PointIndex);

            if (feedback.IsBlocked)
            {
                AddGhost(statusAt, BlockedGhostColor, camera, panel);
            }
            else
            {
                ringedPoint = feedback.PointIndex;
                ringColor = feedback.HasErrors ? ErrorColor : WarningColor;

                if (feedback.IsInvalid)
                {
                    var lastValid = feedback.LastValidLocation;
                    AddGhost(new Vector3(lastValid.x, 0f, -lastValid.y) / GeometryUtilities.WorldUnitIncrementsPerMeter + Vector3.up * feedback.Height, LastValidGhostColor, camera, panel);
                }
            }

            if (!string.IsNullOrEmpty(feedback.Message) && TryProject(statusAt, camera, panel, out var statusPosition))
            {
                status.text = feedback.Message;
                status.EnableInClassList("fp-point-status--error", feedback.HasErrors);
                status.EnableInClassList("fp-point-status--warning", !feedback.HasErrors);
                // Beside the point, or to its left if there isn't room on its right (its width is as it was last laid out)
                var width = float.IsNaN(status.layout.width) ? 0f : status.layout.width;
                var left = statusPosition.x + StatusOffset.x;
                if (left + width > layer.layout.width)
                {
                    left = Mathf.Max(0f, statusPosition.x - StatusOffset.x - width);
                }

                status.style.left = left;
                status.style.top = statusPosition.y + StatusOffset.y;
                status.style.display = DisplayStyle.Flex;
            }
        }

        private void AddGhost(Vector3 worldPosition, Color color, Camera camera, IPanel panel)
        {
            if (TryProject(worldPosition, camera, panel, out var position))
            {
                ghosts.Add((new Vector2(Mathf.Floor(position.x) - Mathf.Floor(Size * 0.5f), Mathf.Floor(position.y) - Mathf.Floor(Size * 0.5f)), color));
            }
        }

        // In front of the camera (not necessarily in its view)
        private static bool TryProject(Vector3 worldPosition, Camera camera, IPanel panel, out Vector2 panelPosition)
        {
            var screenPosition = camera.WorldToScreenPoint(worldPosition);
            panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));

            return screenPosition.z > 0f;
        }

        private void GenerateVisualContent(MeshGenerationContext context)
        {
            for (var start = 0; start < handles.Count; start += HandlesPerAllocation)
            {
                var end = Mathf.Min(start + HandlesPerAllocation, handles.Count);

                var selectedCount = 0;
                var ringCount = 0;
                for (var i = start; i < end; i++)
                {
                    if (handles[i].Point.IsSelected)
                    {
                        selectedCount++;
                    }

                    if (handles[i].Point.NativeIndex == ringedPoint)
                    {
                        ringCount++;
                    }
                }

                // Two boxes for each handle (its border and fill), a third for a selected one's center, and one behind a
                // ringed one
                var boxCount = (end - start) * 2 + selectedCount + ringCount;
                var mesh = context.Allocate(boxCount * 4, boxCount * 6);
                var vertexCount = 0;

                for (var i = start; i < end; i++)
                {
                    var handle = handles[i];
                    var isSelected = handle.Point.IsSelected;

                    if (handle.Point.NativeIndex == ringedPoint)
                    {
                        AddBox(mesh, ref vertexCount, handle.Corner - Vector2.one * RingWidth, Size + RingWidth * 2f, ringColor);
                    }

                    AddBox(mesh, ref vertexCount, handle.Corner, Size, BorderColor);
                    AddBox(mesh, ref vertexCount, handle.Corner + Vector2.one * BorderWidth, Size - BorderWidth * 2f, isSelected ? SelectedFillColor : FillColor);

                    if (isSelected)
                    {
                        AddBox(mesh, ref vertexCount, handle.Corner + Vector2.one * ((Size - CenterSize) * 0.5f), CenterSize, CenterColor);
                    }
                }
            }

            // Over the handles
            if (outlines.Count > 0 || ghosts.Count > 0)
            {
                var quadCount = outlines.Count + ghosts.Count * 2;
                var mesh = context.Allocate(quadCount * 4, quadCount * 6);
                var vertexCount = 0;

                foreach (var (a, b, color) in outlines)
                {
                    AddLine(mesh, ref vertexCount, a, b, color);
                }

                foreach (var (corner, color) in ghosts)
                {
                    AddBox(mesh, ref vertexCount, corner, Size, new Color(BorderColor.r, BorderColor.g, BorderColor.b, color.a));
                    AddBox(mesh, ref vertexCount, corner + Vector2.one * BorderWidth, Size - BorderWidth * 2f, color);
                }
            }
        }

        private static void AddLine(MeshWriteData mesh, ref int vertexCount, Vector2 a, Vector2 b, Color color)
        {
            var direction = (b - a).normalized;
            var across = new Vector2(-direction.y, direction.x) * (OutlineWidth * 0.5f);

            // Wound as the boxes are (clockwise on screen), since the other way isn't drawn
            mesh.SetNextVertex(new Vertex { position = new Vector3(a.x + across.x, a.y + across.y, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex { position = new Vector3(a.x - across.x, a.y - across.y, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex { position = new Vector3(b.x - across.x, b.y - across.y, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex { position = new Vector3(b.x + across.x, b.y + across.y, Vertex.nearZ), tint = color });

            mesh.SetNextIndex((ushort) vertexCount);
            mesh.SetNextIndex((ushort) (vertexCount + 1));
            mesh.SetNextIndex((ushort) (vertexCount + 2));
            mesh.SetNextIndex((ushort) (vertexCount + 2));
            mesh.SetNextIndex((ushort) (vertexCount + 3));
            mesh.SetNextIndex((ushort) vertexCount);

            vertexCount += 4;
        }

        // A square, from its top left
        private static void AddBox(MeshWriteData mesh, ref int vertexCount, Vector2 corner, float size, Color color)
        {
            var left = corner.x;
            var top = corner.y;
            var right = corner.x + size;
            var bottom = corner.y + size;

            mesh.SetNextVertex(new Vertex { position = new Vector3(left, bottom, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex { position = new Vector3(left, top, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex { position = new Vector3(right, top, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex { position = new Vector3(right, bottom, Vertex.nearZ), tint = color });

            mesh.SetNextIndex((ushort) vertexCount);
            mesh.SetNextIndex((ushort) (vertexCount + 1));
            mesh.SetNextIndex((ushort) (vertexCount + 2));
            mesh.SetNextIndex((ushort) (vertexCount + 2));
            mesh.SetNextIndex((ushort) (vertexCount + 3));
            mesh.SetNextIndex((ushort) vertexCount);

            vertexCount += 4;
        }
    }
}
