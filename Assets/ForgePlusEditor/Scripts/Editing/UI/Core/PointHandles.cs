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
    // Points' handles, drawn over the level (behind the panels) at the top and bottom corners of their polygons, one for
    // each height a point's polygons meet it at, with nearer ones over farther ones. They're shown while Geometry mode
    // shows points, and are clicked and dragged through WorldPointer (as its screen picker), over a click area that can be
    // bigger than they're drawn (the Point Click Area setting). Each point with handles at more than one height has a
    // pole, a vertical line from its lowest handle to its highest, drawn behind all handles, that selects and drags it as
    // they do (where no handle is clicked).
    //
    // Looking straight down (the camera's orthographic view), only each point's uppermost handle (of those below the
    // camera) is shown, none fade with distance, and there are no poles.
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

        // Each handle has up to 52 vertices (13 rectangles), which an allocation indexes with 16 bits
        private const int HandlesPerAllocation = 1200;

        // Meters from the camera: fully opaque nearer than the start, faded to the minimum opacity at the end, and not shown
        // (or clickable) beyond it. Selected ones are always shown, fully opaque.
        private const float FadeStartDistance = 5f;
        private const float FadeEndDistance = 20f;
        private const float MinimumOpacity = 0.05f;

        private static readonly Color BorderColor = Color.black;
        private static readonly Color FillColor = Color.white;
        private static readonly Color SelectedFillColor = Color.red;
        private static readonly Color CenterColor = Color.black;

        // As the Errors panel marks errors and warnings (--fp-error and --fp-warning)
        private static readonly Color ErrorColor = new Color32(210, 40, 40, 255);
        private static readonly Color WarningColor = new Color32(240, 200, 30, 255);
        private static readonly Color BlockedGhostColor = new Color(210f / 255f, 40f / 255f, 40f / 255f, 0.5f);
        private static readonly Color LastValidGhostColor = new Color(1f, 1f, 1f, 0.5f);

        // Pixels: a black outline around a white fill (red while selected), as wide as both together
        private const float PoleWidth = 6f;
        private const float PoleFillWidth = 4f;

        // Poles are fainter than handles, over the same distances (and fully opaque while selected)
        private const float PoleMaximumOpacity = 0.3f;
        private const float PoleMinimumOpacity = 0.015f;

        // Each pole has 20 vertices (5 rectangles)
        private const int PolesPerAllocation = 3000;

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

            // Faded with distance from the camera
            public float Opacity;
        }

        private struct Pole
        {
            public LevelEntity_Point Point;

            // Its bottom and top ends (in front of the camera), in the world and in panel space, and how faded each is
            public Vector3 BottomWorld;
            public Vector3 TopWorld;
            public Vector2 Bottom;
            public Vector2 Top;
            public float BottomOpacity;
            public float TopOpacity;
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

        // Each point's highest handle in view, while looking straight down
        private readonly Dictionary<short, float> uppermostHeights = new Dictionary<short, float>();

        private bool isOrthographic;

        // Those in view, and each point's lowest and highest handle heights (in meters) they're found from
        private readonly List<Pole> poles = new List<Pole>();
        private readonly Dictionary<short, (float Bottom, float Top)> poleHeights = new Dictionary<short, (float, float)>();

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

        // After the camera has moved for the frame (and platforms, whose floors and ceilings carry their handles)
        public void Update()
        {
            var hadHandles = handles.Count > 0 || poles.Count > 0;
            handles.Clear();
            poles.Clear();

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

            if (hadHandles || handles.Count > 0 || poles.Count > 0 || hadFeedback)
            {
                layer.MarkDirtyRepaint();
            }
        }

        // The nearest handle whose click area is under the pointer, or else the nearest pole
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

            return TryPickPole(screenPosition, position, out handler, out worldPosition);
        }

        // Over its width, scaled as handles' click areas are, and from where it's clicked (for dragging at that height)
        private bool TryPickPole(Vector2 screenPosition, Vector2 panelPosition, out IWorldPointerHandler handler, out Vector3 worldPosition)
        {
            handler = null;
            worldPosition = default;

            var camera = Camera.main;
            if (!camera)
            {
                return false;
            }

            var halfClickWidth = PoleWidth * SettingsManager.Instance.PointClickAreaScale * 0.5f;
            var ray = camera.ScreenPointToRay(screenPosition);
            var nearestDistance = float.MaxValue;

            foreach (var pole in poles)
            {
                var along = pole.Top - pole.Bottom;
                var t = along.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(panelPosition - pole.Bottom, along) / along.sqrMagnitude) : 0f;
                if (Vector2.Distance(panelPosition, pole.Bottom + along * t) > halfClickWidth)
                {
                    continue;
                }

                // Where along it (a vertical line) the pointer's ray passes nearest
                var height = HeightNearestRay(ray, pole.BottomWorld, pole.TopWorld);
                var clicked = new Vector3(pole.BottomWorld.x, height, pole.BottomWorld.z);
                var distance = Vector3.Distance(ray.origin, clicked);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    handler = pole.Point;
                    worldPosition = clicked;
                }
            }

            return handler != null;
        }

        private static float HeightNearestRay(Ray ray, Vector3 bottom, Vector3 top)
        {
            // The closest points of the ray and the (vertical) line, kept to the line's ends
            var toLine = bottom - ray.origin;
            var b = ray.direction.y;
            var denominator = 1f - b * b;
            var lineDistance = denominator > 0.0001f ? (b * Vector3.Dot(ray.direction, toLine) - toLine.y) / denominator : 0f;

            return Mathf.Clamp(bottom.y + lineDistance, bottom.y, top.y);
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
            poleHeights.Clear();

            var editorCamera = ForgePlusUI.Instance.EditorCamera;
            isOrthographic = editorCamera && editorCamera.IsOrthographic;

            var viewRect = camera.pixelRect;

            foreach (var polygon in level.Polygons.Values)
            {
                CollectCorners(level, polygon, polygon.FloorSurface, camera, panel, viewRect);
                CollectCorners(level, polygon, polygon.CeilingSurface, camera, panel, viewRect);
            }

            if (isOrthographic)
            {
                KeepUppermostHandles();
                return;
            }

            handles.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            CollectPoles(level, camera, panel, viewRect);
        }

        // Its others are right under it
        private void KeepUppermostHandles()
        {
            uppermostHeights.Clear();

            foreach (var handle in handles)
            {
                var pointIndex = handle.Point.NativeIndex;
                if (!uppermostHeights.TryGetValue(pointIndex, out var height) || handle.WorldPosition.y > height)
                {
                    uppermostHeights[pointIndex] = handle.WorldPosition.y;
                }
            }

            handles.RemoveAll(handle => handle.WorldPosition.y < uppermostHeights[handle.Point.NativeIndex]);
            handles.Sort((a, b) => b.Depth.CompareTo(a.Depth));
        }

        // From each point's lowest handle to its highest, wherever its handles are (in view or not)
        private void CollectPoles(LevelEntity_Level level, Camera camera, IPanel panel, UnityEngine.Rect viewRect)
        {
            var cameraPosition = camera.transform.position;
            var cameraForward = camera.transform.forward;
            var nearDepth = camera.nearClipPlane * 1.01f;

            float DepthOf(Vector3 position) => Vector3.Dot(position - cameraPosition, cameraForward);

            foreach (var pair in poleHeights)
            {
                var (bottomHeight, topHeight) = pair.Value;
                if (topHeight - bottomHeight < 0.001f || !level.Points.TryGetValue(pair.Key, out var point))
                {
                    continue;
                }

                var bottom = GeometryUtilities.GetMeshVertex(level.Level, pair.Key);
                bottom.y = bottomHeight;
                var top = new Vector3(bottom.x, topHeight, bottom.z);

                // Shown while any of it is near enough (as handles are)
                var nearest = new Vector3(bottom.x, Mathf.Clamp(cameraPosition.y, bottomHeight, topHeight), bottom.z);
                if (Vector3.Distance(cameraPosition, nearest) > FadeEndDistance && !point.IsSelected)
                {
                    continue;
                }

                // Only what's in front of the camera
                var bottomDepth = DepthOf(bottom);
                var topDepth = DepthOf(top);
                if (bottomDepth < nearDepth && topDepth < nearDepth)
                {
                    continue;
                }

                if (bottomDepth < nearDepth)
                {
                    bottom = Vector3.Lerp(bottom, top, (nearDepth - bottomDepth) / (topDepth - bottomDepth));
                }
                else if (topDepth < nearDepth)
                {
                    top = Vector3.Lerp(top, bottom, (nearDepth - topDepth) / (bottomDepth - topDepth));
                }

                var bottomScreen = camera.WorldToScreenPoint(bottom);
                var topScreen = camera.WorldToScreenPoint(top);
                if ((bottomScreen.x < viewRect.xMin && topScreen.x < viewRect.xMin) || (bottomScreen.x > viewRect.xMax && topScreen.x > viewRect.xMax) ||
                    (bottomScreen.y < viewRect.yMin && topScreen.y < viewRect.yMin) || (bottomScreen.y > viewRect.yMax && topScreen.y > viewRect.yMax))
                {
                    continue;
                }

                // Centered on whole pixels, so the edges of its (even) width are sharp where it's upright on screen
                var bottomPanel = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(bottomScreen.x, Screen.height - bottomScreen.y));
                var topPanel = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(topScreen.x, Screen.height - topScreen.y));

                poles.Add(new Pole
                {
                    Point = point,
                    BottomWorld = bottom,
                    TopWorld = top,
                    Bottom = new Vector2(Mathf.Round(bottomPanel.x), bottomPanel.y),
                    Top = new Vector2(Mathf.Round(topPanel.x), topPanel.y),
                    BottomOpacity = OpacityAt(point, Vector3.Distance(cameraPosition, bottom), PoleMaximumOpacity, PoleMinimumOpacity),
                    TopOpacity = OpacityAt(point, Vector3.Distance(cameraPosition, top), PoleMaximumOpacity, PoleMinimumOpacity),
                });
            }
        }

        private static float OpacityAt(LevelEntity_Point point, float distance, float maximumOpacity = 1f, float minimumOpacity = MinimumOpacity)
        {
            return point.IsSelected ? 1f : Mathf.Lerp(maximumOpacity, minimumOpacity, Mathf.InverseLerp(FadeStartDistance, FadeEndDistance, distance));
        }

        // A polygon's corners at its floor or ceiling (where it's drawn, as a platform's floor and ceiling move)
        private void CollectCorners(LevelEntity_Level level, LevelEntity_Polygon polygon, Component surface, Camera camera, IPanel panel, UnityEngine.Rect viewRect)
        {
            if (!surface)
            {
                return;
            }

            var data = level.Level;
            var height = surface.transform.position.y;
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

                poleHeights[pointIndex] = poleHeights.TryGetValue(pointIndex, out var heights) ? (Mathf.Min(heights.Bottom, height), Mathf.Max(heights.Top, height)) : (height, height);

                var worldPosition = GeometryUtilities.GetMeshVertex(data, pointIndex);
                worldPosition.y = height;

                var screenPosition = camera.WorldToScreenPoint(worldPosition);
                if (screenPosition.z <= 0f || !viewRect.Contains(screenPosition))
                {
                    continue;
                }

                var distance = Vector3.Distance(camera.transform.position, worldPosition);
                if (distance > FadeEndDistance && !point.IsSelected && !isOrthographic)
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
                    Opacity = isOrthographic ? 1f : OpacityAt(point, distance),
                });
            }
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
            // Behind every handle
            for (var start = 0; start < poles.Count; start += PolesPerAllocation)
            {
                var end = Mathf.Min(start + PolesPerAllocation, poles.Count);
                var mesh = context.Allocate((end - start) * 5 * 4, (end - start) * 5 * 6);
                var vertexCount = 0;

                for (var i = start; i < end; i++)
                {
                    AddPole(mesh, ref vertexCount, poles[i]);
                }
            }

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

                // Its pieces don't overlap, so they fade evenly: four for each handle's border, one for its fill (or four
                // around a selected one's center, and the center), and four for a ringed one's ring
                var boxCount = (end - start) * 5 + selectedCount * 4 + ringCount * 4;
                var mesh = context.Allocate(boxCount * 4, boxCount * 6);
                var vertexCount = 0;

                for (var i = start; i < end; i++)
                {
                    var handle = handles[i];
                    var isSelected = handle.Point.IsSelected;

                    // Flashing green, growing to twice its size, and fully opaque while it's focused on (FocusFlash)
                    var flashIntensity = FocusFlash.PointIntensity(handle.Point);
                    var scale = 1f + flashIntensity;
                    var opacity = Mathf.Lerp(handle.Opacity, 1f, flashIntensity);
                    var center = handle.Corner + Vector2.one * (Size * 0.5f);

                    // The ring (a move's feedback) isn't faded
                    if (handle.Point.NativeIndex == ringedPoint)
                    {
                        AddCenteredFrame(mesh, ref vertexCount, center, (Size + RingWidth * 2f) * scale, Size * scale, ringColor);
                    }

                    var fillSize = (Size - BorderWidth * 2f) * scale;
                    AddCenteredFrame(mesh, ref vertexCount, center, Size * scale, fillSize, WithOpacity(BorderColor, opacity));
                    var fillColor = WithOpacity(Color.Lerp(isSelected ? SelectedFillColor : FillColor, FocusFlash.FlashColor, flashIntensity), opacity);

                    if (isSelected)
                    {
                        AddCenteredFrame(mesh, ref vertexCount, center, fillSize, CenterSize * scale, fillColor);
                        AddCenteredBox(mesh, ref vertexCount, center, CenterSize * scale, WithOpacity(CenterColor, opacity));
                    }
                    else
                    {
                        AddCenteredBox(mesh, ref vertexCount, center, fillSize, fillColor);
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

        // An outlined line, with the pieces of its outline apart from its fill (so they fade evenly), flashing green (but
        // not growing) while its point is focused on (FocusFlash)
        private static void AddPole(MeshWriteData mesh, ref int vertexCount, Pole pole)
        {
            var bottom = pole.Bottom;
            var top = pole.Top;
            var length = Vector2.Distance(bottom, top);
            var direction = length > 0.001f ? (top - bottom) / length : Vector2.down;
            var across = new Vector2(-direction.y, direction.x);

            var outlineWidth = (PoleWidth - PoleFillWidth) * 0.5f;
            var halfWidth = PoleWidth * 0.5f;
            var halfFillWidth = PoleFillWidth * 0.5f;

            // The outline caps its ends, too
            var outerBottom = bottom - direction * outlineWidth;
            var outerTop = top + direction * outlineWidth;

            var bottomBorder = WithOpacity(BorderColor, pole.BottomOpacity);
            var topBorder = WithOpacity(BorderColor, pole.TopOpacity);
            var fill = Color.Lerp(pole.Point.IsSelected ? SelectedFillColor : FillColor, FocusFlash.FlashColor, FocusFlash.PointIntensity(pole.Point));
            var bottomFill = WithOpacity(fill, pole.BottomOpacity);
            var topFill = WithOpacity(fill, pole.TopOpacity);

            AddStrip(mesh, ref vertexCount, outerBottom, outerTop, across, halfWidth, halfFillWidth, bottomBorder, topBorder);
            AddStrip(mesh, ref vertexCount, outerBottom, outerTop, across, -halfFillWidth, -halfWidth, bottomBorder, topBorder);
            AddStrip(mesh, ref vertexCount, outerBottom, bottom, across, halfFillWidth, -halfFillWidth, bottomBorder, bottomBorder);
            AddStrip(mesh, ref vertexCount, top, outerTop, across, halfFillWidth, -halfFillWidth, topBorder, topBorder);
            AddStrip(mesh, ref vertexCount, bottom, top, across, halfFillWidth, -halfFillWidth, bottomFill, topFill);
        }

        // Along a line from start to end, between two offsets across it (the first greater, wound as AddLine is)
        private static void AddStrip(MeshWriteData mesh, ref int vertexCount, Vector2 start, Vector2 end, Vector2 across, float offsetA, float offsetB, Color startColor, Color endColor)
        {
            var startA = start + across * offsetA;
            var startB = start + across * offsetB;
            var endA = end + across * offsetA;
            var endB = end + across * offsetB;

            mesh.SetNextVertex(new Vertex { position = new Vector3(startA.x, startA.y, Vertex.nearZ), tint = startColor });
            mesh.SetNextVertex(new Vertex { position = new Vector3(startB.x, startB.y, Vertex.nearZ), tint = startColor });
            mesh.SetNextVertex(new Vertex { position = new Vector3(endB.x, endB.y, Vertex.nearZ), tint = endColor });
            mesh.SetNextVertex(new Vertex { position = new Vector3(endA.x, endA.y, Vertex.nearZ), tint = endColor });

            mesh.SetNextIndex((ushort) vertexCount);
            mesh.SetNextIndex((ushort) (vertexCount + 1));
            mesh.SetNextIndex((ushort) (vertexCount + 2));
            mesh.SetNextIndex((ushort) (vertexCount + 2));
            mesh.SetNextIndex((ushort) (vertexCount + 3));
            mesh.SetNextIndex((ushort) vertexCount);

            vertexCount += 4;
        }

        private static Color WithOpacity(Color color, float opacity)
        {
            return new Color(color.r, color.g, color.b, color.a * opacity);
        }

        // In whole pixels, so its edges stay sharp (and, unscaled, exactly where AddBox would put them from the corner)
        private static Vector2 CenteredCorner(Vector2 center, float size)
        {
            return new Vector2(Mathf.Floor(center.x - size * 0.5f), Mathf.Floor(center.y - size * 0.5f));
        }

        private static void AddCenteredBox(MeshWriteData mesh, ref int vertexCount, Vector2 center, float size, Color color)
        {
            size = Mathf.Round(size);
            AddBox(mesh, ref vertexCount, CenteredCorner(center, size), size, color);
        }

        // A square's outer part, around the inner square (both centered), as four rectangles
        private static void AddCenteredFrame(MeshWriteData mesh, ref int vertexCount, Vector2 center, float outerSize, float innerSize, Color color)
        {
            outerSize = Mathf.Round(outerSize);
            innerSize = Mathf.Round(innerSize);
            var outer = CenteredCorner(center, outerSize);
            var inner = CenteredCorner(center, innerSize);

            AddRectangle(mesh, ref vertexCount, outer.x, outer.y, outer.x + outerSize, inner.y, color);
            AddRectangle(mesh, ref vertexCount, outer.x, inner.y + innerSize, outer.x + outerSize, outer.y + outerSize, color);
            AddRectangle(mesh, ref vertexCount, outer.x, inner.y, inner.x, inner.y + innerSize, color);
            AddRectangle(mesh, ref vertexCount, inner.x + innerSize, inner.y, outer.x + outerSize, inner.y + innerSize, color);
        }

        // A square, from its top left
        private static void AddBox(MeshWriteData mesh, ref int vertexCount, Vector2 corner, float size, Color color)
        {
            AddRectangle(mesh, ref vertexCount, corner.x, corner.y, corner.x + size, corner.y + size, color);
        }

        private static void AddRectangle(MeshWriteData mesh, ref int vertexCount, float left, float top, float right, float bottom, Color color)
        {
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
