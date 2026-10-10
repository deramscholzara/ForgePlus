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
    // Lines (shown while Geometry mode shows lines) are drawn the same way along polygons' edges, at their floors and
    // ceilings, behind the poles: white where there's a polygon on only one side, light blue where the floors (or
    // ceilings) on either side are at the same height, and light green at both heights where they aren't (red while
    // selected). They fade with distance as handles do (from fully opaque). Clicking or dragging one (where no handle or
    // pole is clicked) selects its line, or moves it.
    //
    // Looking straight down (the camera's orthographic view), only each point's uppermost handle (of those below the
    // camera) is shown, only each line's lowest (at a floor), there are no poles, and nothing fades with distance.
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

        // Each pole (or line) has 20 vertices (5 rectangles)
        private const int SegmentsPerAllocation = 3000;

        private static readonly Color OneSidedLineColor = Color.white;
        private static readonly Color LevelLineColor = new Color(0.6f, 0.82f, 1f);
        private static readonly Color StepLineColor = new Color(0.6f, 1f, 0.6f);

        // Meters apart that heights count as the same
        private const float SameHeightTolerance = 0.0005f;

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

        // A pole (for its point, which it flashes with) or a line (for its line, which it also flashes with)
        private struct Segment
        {
            public LevelEntity_Point Point;
            public LevelEntity_Line Line;
            public Color Fill;

            // Its ends (a pole's bottom first; in front of the camera), in the world and in panel space, and how faded each is
            public Vector3 StartWorld;
            public Vector3 EndWorld;
            public Vector2 Start;
            public Vector2 End;
            public float StartOpacity;
            public float EndOpacity;

            // Along the camera's view, at its middle
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

        // Each point's highest handle in view, while looking straight down
        private readonly Dictionary<short, float> uppermostHeights = new Dictionary<short, float>();

        private bool isOrthographic;

        // Those in view, and each point's lowest and highest handle heights (in meters) they're found from
        private readonly List<Segment> poles = new List<Segment>();

        // Those in view (farthest first), and the heights (and colors) of the line being collected
        private readonly List<Segment> lines = new List<Segment>();
        private readonly List<(float Height, Color Fill)> lineHeights = new List<(float, Color)>();
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
            var hadHandles = handles.Count > 0 || poles.Count > 0 || lines.Count > 0;
            handles.Clear();
            poles.Clear();
            lines.Clear();

            var level = LevelEntity_Level.Instance;
            var camera = Camera.main;
            var panel = layer.panel;

            var hadFeedback = outlines.Count > 0 || ghosts.Count > 0 || ringedPoint != cstypes.NONE;
            outlines.Clear();
            ghosts.Clear();
            ringedPoint = cstypes.NONE;
            status.style.display = DisplayStyle.None;

            var editorCamera = ForgePlusUI.Instance.EditorCamera;
            isOrthographic = editorCamera && editorCamera.IsOrthographic;

            if (IsLayerShown(level) && camera && panel != null)
            {
                if (SettingsManager.Instance.PointsAreShown)
                {
                    CollectHandles(level, camera, panel);
                    CollectFeedback(level, camera, panel);
                }

                if (SettingsManager.Instance.LinesAreShown)
                {
                    CollectLines(level, camera, panel);
                }
            }

            if (hadHandles || handles.Count > 0 || poles.Count > 0 || lines.Count > 0 || hadFeedback)
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
            if (handles.Count == 0 || panel == null || !IsLayerShown(LevelEntity_Level.Instance) || !SettingsManager.Instance.PointsAreShown)
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

            return TryPickPole(screenPosition, position, out handler, out worldPosition) ||
                   TryPickLine(screenPosition, position, out handler, out worldPosition);
        }

        // Over its width, scaled as handles' click areas are (the nearest, where several are), and from where it's clicked
        // (for dragging at that height)
        private bool TryPickLine(Vector2 screenPosition, Vector2 panelPosition, out IWorldPointerHandler handler, out Vector3 worldPosition)
        {
            handler = null;
            worldPosition = default;

            var camera = Camera.main;
            if (!camera || !SettingsManager.Instance.LinesAreShown)
            {
                return false;
            }

            var halfClickWidth = PoleWidth * SettingsManager.Instance.PointClickAreaScale * 0.5f;
            var ray = camera.ScreenPointToRay(screenPosition);
            var nearestDistance = float.MaxValue;

            foreach (var line in lines)
            {
                var along = line.End - line.Start;
                var t = along.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(panelPosition - line.Start, along) / along.sqrMagnitude) : 0f;
                if (Vector2.Distance(panelPosition, line.Start + along * t) > halfClickWidth)
                {
                    continue;
                }

                var clicked = NearestToRay(ray, line.StartWorld, line.EndWorld);
                var distance = Vector3.Distance(ray.origin, clicked);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    handler = line.Line;
                    worldPosition = clicked;
                }
            }

            return handler != null;
        }

        // Where on the segment the ray passes nearest
        private static Vector3 NearestToRay(Ray ray, Vector3 start, Vector3 end)
        {
            var along = end - start;
            var toStart = start - ray.origin;
            var a = Vector3.Dot(along, along);
            var b = Vector3.Dot(along, ray.direction);
            var denominator = a - b * b;

            if (a <= 0f || denominator <= 0.000001f)
            {
                return start;
            }

            var t = (b * Vector3.Dot(toStart, ray.direction) - Vector3.Dot(toStart, along)) / denominator;
            return start + along * Mathf.Clamp01(t);
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
                var along = pole.End - pole.Start;
                var t = along.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(panelPosition - pole.Start, along) / along.sqrMagnitude) : 0f;
                if (Vector2.Distance(panelPosition, pole.Start + along * t) > halfClickWidth)
                {
                    continue;
                }

                // Where along it (a vertical line) the pointer's ray passes nearest
                var height = HeightNearestRay(ray, pole.StartWorld, pole.EndWorld);
                var clicked = new Vector3(pole.StartWorld.x, height, pole.StartWorld.z);
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
        private bool IsLayerShown(LevelEntity_Level level)
        {
            return level && level.Points != null && !LevelEditing.IsRebuildPending &&
                   layer.parent != null && layer.parent.resolvedStyle.display != DisplayStyle.None;
        }

        private void CollectHandles(LevelEntity_Level level, Camera camera, IPanel panel)
        {
            placedCorners.Clear();
            poleHeights.Clear();

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

                // Centered on whole pixels, so the edges of its (even) width are sharp where it's upright on screen
                if (TryProjectSegment(camera, panel, viewRect, bottom, top, point, null, point.IsSelected ? SelectedFillColor : FillColor, roundsToPixels: true, out var pole))
                {
                    poles.Add(pole);
                }
            }
        }

        // Each line's at its polygons' floors and ceilings (where they're drawn, as platforms' floors and ceilings move)
        private void CollectLines(LevelEntity_Level level, Camera camera, IPanel panel)
        {
            var data = level.Level;
            var viewRect = camera.pixelRect;

            for (short lineIndex = 0; lineIndex < data.LineList.Count; lineIndex++)
            {
                var line = data.LineList[lineIndex];
                level.Polygons.TryGetValue(line.clockwise_polygon_owner, out var clockwisePolygon);
                level.Polygons.TryGetValue(line.counterclockwise_polygon_owner, out var counterclockwisePolygon);

                lineHeights.Clear();

                if (clockwisePolygon && counterclockwisePolygon)
                {
                    AddLineHeights(SurfaceHeight(clockwisePolygon, isFloor: true), SurfaceHeight(counterclockwisePolygon, isFloor: true));
                    AddLineHeights(SurfaceHeight(clockwisePolygon, isFloor: false), SurfaceHeight(counterclockwisePolygon, isFloor: false));
                }
                else if (clockwisePolygon || counterclockwisePolygon)
                {
                    var polygon = clockwisePolygon ? clockwisePolygon : counterclockwisePolygon;
                    AddLineHeight(SurfaceHeight(polygon, isFloor: true), OneSidedLineColor);
                    AddLineHeight(SurfaceHeight(polygon, isFloor: false), OneSidedLineColor);
                }
                else
                {
                    // With no polygon on either side, it's at no height
                    continue;
                }

                // Looking straight down, the others are right over its lowest (a floor)
                if (isOrthographic)
                {
                    KeepLowestLineHeight();
                }

                var start = GeometryUtilities.GetMeshVertex(data, line.endpoint_indexes[0]);
                var end = GeometryUtilities.GetMeshVertex(data, line.endpoint_indexes[1]);

                level.Lines.TryGetValue(lineIndex, out var lineEntity);
                var isSelected = lineEntity && lineEntity.IsSelected;

                foreach (var (height, fill) in lineHeights)
                {
                    start.y = height;
                    end.y = height;

                    if (TryProjectSegment(camera, panel, viewRect, start, end, null, lineEntity, isSelected ? SelectedFillColor : fill, roundsToPixels: false, out var segment))
                    {
                        lines.Add(segment);
                    }
                }
            }

            // A selected line's over the others
            lines.Sort((a, b) =>
            {
                var aSelected = a.Line && a.Line.IsSelected;
                var bSelected = b.Line && b.Line.IsSelected;

                return aSelected != bSelected ? aSelected.CompareTo(bSelected) : b.Depth.CompareTo(a.Depth);
            });
        }

        // The floors (or ceilings) on either side of a line: at one height, or a step between two
        private void AddLineHeights(float a, float b)
        {
            if (Mathf.Abs(a - b) <= SameHeightTolerance)
            {
                AddLineHeight(a, LevelLineColor);
            }
            else
            {
                AddLineHeight(a, StepLineColor);
                AddLineHeight(b, StepLineColor);
            }
        }

        private void KeepLowestLineHeight()
        {
            var lowest = 0;
            for (var i = 1; i < lineHeights.Count; i++)
            {
                if (lineHeights[i].Height < lineHeights[lowest].Height)
                {
                    lowest = i;
                }
            }

            var kept = lineHeights[lowest];
            lineHeights.Clear();
            lineHeights.Add(kept);
        }

        // Once at each height (a floor can meet a ceiling, where one polygon is closed off)
        private void AddLineHeight(float height, Color fill)
        {
            foreach (var (existing, _) in lineHeights)
            {
                if (Mathf.Abs(existing - height) <= SameHeightTolerance)
                {
                    return;
                }
            }

            lineHeights.Add((height, fill));
        }

        private static float SurfaceHeight(LevelEntity_Polygon polygon, bool isFloor)
        {
            Component surface = isFloor ? polygon.FloorSurface : polygon.CeilingSurface;
            if (surface)
            {
                return surface.transform.position.y;
            }

            return (isFloor ? polygon.NativeObject.floor_height : polygon.NativeObject.ceiling_height) / GeometryUtilities.WorldUnitIncrementsPerMeter;
        }

        // A pole (or line) between two places in the level, cut to what's in front of the camera, while it's in view and
        // any of it is near enough (as handles are; looking straight down, it's always near enough and doesn't fade)
        private bool TryProjectSegment(Camera camera, IPanel panel, UnityEngine.Rect viewRect, Vector3 start, Vector3 end, LevelEntity_Point point, LevelEntity_Line line, Color fill, bool roundsToPixels, out Segment segment)
        {
            segment = default;

            var cameraPosition = camera.transform.position;
            var cameraForward = camera.transform.forward;
            var isSelected = (point != null && point.IsSelected) || (line && line.IsSelected);
            var isAlwaysShown = isOrthographic || isSelected;

            if (!isAlwaysShown)
            {
                var along = end - start;
                var t = along.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(cameraPosition - start, along) / along.sqrMagnitude) : 0f;
                if (Vector3.Distance(cameraPosition, start + along * t) > FadeEndDistance)
                {
                    return false;
                }
            }

            // Only what's in front of the camera
            var nearDepth = camera.nearClipPlane * 1.01f;
            var startDepth = Vector3.Dot(start - cameraPosition, cameraForward);
            var endDepth = Vector3.Dot(end - cameraPosition, cameraForward);
            if (startDepth < nearDepth && endDepth < nearDepth)
            {
                return false;
            }

            if (startDepth < nearDepth)
            {
                start = Vector3.Lerp(start, end, (nearDepth - startDepth) / (endDepth - startDepth));
            }
            else if (endDepth < nearDepth)
            {
                end = Vector3.Lerp(end, start, (nearDepth - endDepth) / (startDepth - endDepth));
            }

            var startScreen = camera.WorldToScreenPoint(start);
            var endScreen = camera.WorldToScreenPoint(end);
            if ((startScreen.x < viewRect.xMin && endScreen.x < viewRect.xMin) || (startScreen.x > viewRect.xMax && endScreen.x > viewRect.xMax) ||
                (startScreen.y < viewRect.yMin && endScreen.y < viewRect.yMin) || (startScreen.y > viewRect.yMax && endScreen.y > viewRect.yMax))
            {
                return false;
            }

            var startPanel = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(startScreen.x, Screen.height - startScreen.y));
            var endPanel = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(endScreen.x, Screen.height - endScreen.y));
            if (roundsToPixels)
            {
                startPanel.x = Mathf.Round(startPanel.x);
                endPanel.x = Mathf.Round(endPanel.x);
            }

            segment = new Segment
            {
                Point = point,
                Line = line,
                Fill = fill,
                StartWorld = start,
                EndWorld = end,
                Start = startPanel,
                End = endPanel,
                StartOpacity = isSelected ? 1f : SegmentOpacityAt(point, Vector3.Distance(cameraPosition, start)),
                EndOpacity = isSelected ? 1f : SegmentOpacityAt(point, Vector3.Distance(cameraPosition, end)),
                Depth = (startScreen.z + endScreen.z) * 0.5f,
            };

            return true;
        }

        // Poles (with points) are fainter; lines fade as handles do (their fills alone set them apart)
        private float SegmentOpacityAt(LevelEntity_Point point, float distance)
        {
            if (point == null)
            {
                return isOrthographic ? 1f : OpacityAt(null, distance);
            }

            if (isOrthographic)
            {
                return point.IsSelected ? 1f : PoleMaximumOpacity;
            }

            return OpacityAt(point, distance, PoleMaximumOpacity, PoleMinimumOpacity);
        }

        private static float OpacityAt(LevelEntity_Point point, float distance, float maximumOpacity = 1f, float minimumOpacity = MinimumOpacity)
        {
            return point != null && point.IsSelected ? 1f : Mathf.Lerp(maximumOpacity, minimumOpacity, Mathf.InverseLerp(FadeStartDistance, FadeEndDistance, distance));
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
            // Behind every handle: lines, then poles
            AddSegments(context, lines);
            AddSegments(context, poles);

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
        private static void AddSegments(MeshGenerationContext context, List<Segment> segments)
        {
            for (var start = 0; start < segments.Count; start += SegmentsPerAllocation)
            {
                var end = Mathf.Min(start + SegmentsPerAllocation, segments.Count);
                var mesh = context.Allocate((end - start) * 5 * 4, (end - start) * 5 * 6);
                var vertexCount = 0;

                for (var i = start; i < end; i++)
                {
                    AddSegment(mesh, ref vertexCount, segments[i]);
                }
            }
        }

        private static void AddSegment(MeshWriteData mesh, ref int vertexCount, Segment segment)
        {
            var start = segment.Start;
            var end = segment.End;
            var length = Vector2.Distance(start, end);
            var direction = length > 0.001f ? (end - start) / length : Vector2.down;
            var across = new Vector2(-direction.y, direction.x);

            var outlineWidth = (PoleWidth - PoleFillWidth) * 0.5f;
            var halfWidth = PoleWidth * 0.5f;
            var halfFillWidth = PoleFillWidth * 0.5f;

            // The outline caps its ends, too
            var outerStart = start - direction * outlineWidth;
            var outerEnd = end + direction * outlineWidth;

            var startBorder = WithOpacity(BorderColor, segment.StartOpacity);
            var endBorder = WithOpacity(BorderColor, segment.EndOpacity);
            var flashIntensity = segment.Point != null ? FocusFlash.PointIntensity(segment.Point) : segment.Line ? FocusFlash.LineIntensity(segment.Line) : 0f;
            var fill = Color.Lerp(segment.Fill, FocusFlash.FlashColor, flashIntensity);
            var startFill = WithOpacity(fill, segment.StartOpacity);
            var endFill = WithOpacity(fill, segment.EndOpacity);

            AddStrip(mesh, ref vertexCount, outerStart, outerEnd, across, halfWidth, halfFillWidth, startBorder, endBorder);
            AddStrip(mesh, ref vertexCount, outerStart, outerEnd, across, -halfFillWidth, -halfWidth, startBorder, endBorder);
            AddStrip(mesh, ref vertexCount, outerStart, start, across, halfFillWidth, -halfFillWidth, startBorder, startBorder);
            AddStrip(mesh, ref vertexCount, end, outerEnd, across, halfFillWidth, -halfFillWidth, endBorder, endBorder);
            AddStrip(mesh, ref vertexCount, start, end, across, halfFillWidth, -halfFillWidth, startFill, endFill);
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
