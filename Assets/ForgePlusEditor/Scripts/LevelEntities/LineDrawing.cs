#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.Extensions;
using ForgePlus.History;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Localization;
using ForgePlus.PolygonContainment;
using ForgePlus.UI;
using RuntimeCore.Entities;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using static AlephOne.map;

namespace ForgePlus.LevelManipulation
{
    // Geometry mode's Draw tool, dragging a point: a new line follows the pointer from it (across the level plane at the
    // height it was grabbed at, snapping to the grid as moving a point does), or ends at another point once the pointer is
    // within the Point Snapping Distance (the setting, in pixels) of its handle. Letting go adds the line, and a new point
    // at its end unless it ended at one. Ending at a point, it may close a loop of lines into a polygon on either side of
    // it (PolygonClosing).
    //
    // A line from one of a polygon's corners across it to another (that isn't next to it) splits the polygon in two: the
    // new one (on one side of the line) has everything the polygon had (its floor and ceiling, media, sounds and so on),
    // its walls keep their sides, and what's on its side (objects and annotations) goes with it, so textures and objects
    // stay where they are.
    //
    // The line can't cross or touch other lines or points (other than those it starts and ends at), or go through a
    // polygon other than to split it, while Prevent Invalid Moves is on (the setting), unless Allow Invalid Move (shift)
    // is held when letting go (which never splits). Splitting a polygon ignores the lines of others that overlap it (at
    // other heights), as the new line can't meet them. What it would add is shown as it goes (Current, which PointHandles
    // draws). The level's structure changes, so it's rebuilt afterward, with the new point (or line) selected.
    //
    // A line can also be drawn from empty space (BeginFromEmptySpace, which a press, release and press there begins), from
    // a new point where it was pressed. It's drawn at the floor of the polygon whose corner is nearest that point (or at
    // 0, with no polygons), and once added, it's shown at the height its chain of lines takes (LooseGeometryHeights): the
    // lowest floor of the polygons it reaches, or 0 if it reaches none.
    [AutoStaticsCleanup]
    public sealed partial class LineDrawing
    {
        // What the drag would add, as it's shown
        public sealed class Preview
        {
            // Where the line starts and ends, and the heights it's shown at there (in meters)
            public world_point2d Start;
            public world_point2d End;

            // Whether it starts at a new point (drawn from empty space)
            public bool StartsNew;
            public float StartHeight;
            public float EndHeight;

            // The point it ends at, or NONE for a new one
            public short SnappedPoint = cstypes.NONE;

            // Whether there's a line to show (it's been dragged away from the point), and whether it would split a polygon
            public bool HasLine;
            public bool Splits;

            // The polygons it would close (PolygonClosing): their corners (clockwise), and their floor and ceiling heights
            // (in meters)
            public readonly List<(world_point2d[] Corners, float FloorHeight, float CeilingHeight)> ClosedPolygons = new List<(world_point2d[], float, float)>();

            // Whether it can't be added (as it is), or it can but crosses something, and what the note says (or null)
            public bool IsBlocked;
            public bool Overlaps;
            public string Message;
        }

        // While a point is dragged in Draw mode
        public static Preview Current { get; private set; }

        private readonly LevelEntity_Level level;
        private readonly short startIndex;
        private readonly LevelPlaneDrag drag;
        private readonly float pressHeight;
        private readonly Preview preview = new Preview();

        // The polygon it would split, or NONE
        private short splitPolygon = cstypes.NONE;

        public static LineDrawing Begin(LevelEntity_Level level, short pointIndex, Vector3 pressWorldPosition)
        {
            if (!level || LevelEditing.IsRebuildPending || pointIndex < 0 || pointIndex >= level.Level.EndpointList.Count)
            {
                return null;
            }

            return new LineDrawing(level, pointIndex, level.Level.EndpointList[pointIndex].vertex, pressWorldPosition);
        }

        // From a new point where the pointer's ray meets the level plane at the floor of the polygon whose corner is
        // nearest there (snapped to the grid as drawing is), or at 0 with no polygons
        public static LineDrawing BeginFromEmptySpace(LevelEntity_Level level, Ray pointerRay)
        {
            if (!level || LevelEditing.IsRebuildPending)
            {
                return null;
            }

            var data = level.Level;
            var height = 0f;
            if (!TryMeetPlane(pointerRay, height, out var pressWorldPosition))
            {
                return null;
            }

            // The nearest corner depends on where the ray meets the plane, which depends on its height (twice is enough to
            // settle on one)
            for (var i = 0; i < 2 && data.PolygonList.Count > 0; i++)
            {
                height = NearestCornerFloor(data, ToLocation(pressWorldPosition, snaps: false));
                if (!TryMeetPlane(pointerRay, height, out pressWorldPosition))
                {
                    return null;
                }
            }

            return new LineDrawing(level, cstypes.NONE, ToLocation(pressWorldPosition, AxisLocks.Instance.SnapToGrid), pressWorldPosition);
        }

        private LineDrawing(LevelEntity_Level level, short pointIndex, world_point2d start, Vector3 pressWorldPosition)
        {
            this.level = level;
            startIndex = pointIndex;
            pressHeight = pressWorldPosition.y;

            drag = new LevelPlaneDrag(pressWorldPosition, start);

            preview.Start = start;
            preview.StartsNew = pointIndex == cstypes.NONE;
            preview.End = start;
            preview.StartHeight = pressHeight;
            preview.EndHeight = pressHeight;

            // Recorded as one action when it ends
            LevelHistory.BeginGesture();

            Current = preview;
        }

        // Where the pointer is (on screen, in pixels from the bottom left), and its ray into the level
        public void Drag(Ray pointerRay, Vector2 screenPosition)
        {
            var pointHandles = ForgePlusUI.Instance ? ForgePlusUI.Instance.PointHandles : null;

            if (pointHandles != null && pointHandles.TryFindPointNear(screenPosition, SettingsManager.Instance.PointSnapDistance, startIndex, out var snappedIndex, out var snappedPosition))
            {
                preview.SnappedPoint = snappedIndex;
                preview.End = level.Level.EndpointList[snappedIndex].vertex;
                preview.EndHeight = snappedPosition.y;
            }
            else
            {
                preview.SnappedPoint = cstypes.NONE;
                preview.End = drag.DraggedLocation(pointerRay);
                preview.EndHeight = pressHeight;
            }

            UpdatePreview();
        }

        // Adds the line, if it can be added
        public void End()
        {
            Current = null;

            try
            {
                // As it would be added now (with Allow Invalid Move as it is now)
                UpdatePreview();

                if (preview.HasLine && !preview.IsBlocked)
                {
                    if (preview.Splits)
                    {
                        SplitPolygon();
                    }
                    else
                    {
                        AddLine();
                    }
                }
            }
            finally
            {
                LevelHistory.EndGesture();
            }
        }

        private void UpdatePreview()
        {
            var data = level.Level;
            var endIndex = preview.SnappedPoint;

            preview.HasLine = false;
            preview.Splits = false;
            preview.ClosedPolygons.Clear();
            preview.IsBlocked = false;
            preview.Overlaps = false;
            preview.Message = null;
            splitPolygon = cstypes.NONE;

            if (preview.Start.x == preview.End.x && preview.Start.y == preview.End.y)
            {
                return;
            }

            preview.HasLine = true;

            var start = GeometryConstruction.ToVector(preview.Start);
            var end = GeometryConstruction.ToVector(preview.End);

            // Never a second line between the same two points
            if (endIndex != cstypes.NONE && startIndex != cstypes.NONE && IsJoined(data, startIndex, endIndex))
            {
                preview.IsBlocked = true;
                preview.Message = Strings.Get(Strings.Common, "LineDrawing.Joined");
                return;
            }

            // The polygons it's inside (where its middle is)
            var middle = ((Vector2) start + end) * 0.5f;
            var insidePolygons = new List<short>();
            for (short polygonIndex = 0; polygonIndex < data.PolygonList.Count; polygonIndex++)
            {
                if (GeometryConstruction.IsInsidePolygon(data, data.PolygonList[polygonIndex], middle))
                {
                    insidePolygons.Add(polygonIndex);
                }
            }

            // Across a polygon from one corner to another, it's inside that (convex) polygon all the way, so it can't cross
            // the polygon's own lines, only those of others that overlap it (at other heights), which it doesn't meet
            var splitsPlatform = false;
            if (endIndex != cstypes.NONE)
            {
                foreach (var polygonIndex in insidePolygons)
                {
                    if (!IsAcrossCorners(data, polygonIndex, startIndex, endIndex))
                    {
                        continue;
                    }

                    if (data.PolygonList[polygonIndex].type == _polygon_is_platform)
                    {
                        splitsPlatform = true;
                        continue;
                    }

                    preview.Splits = true;
                    splitPolygon = polygonIndex;
                    return;
                }
            }

            var touches = GeometryConstruction.SegmentTouchesLines(data, start, end, startIndex, endIndex) ||
                          GeometryConstruction.SegmentTouchesPoints(data, start, end, startIndex, endIndex);

            if (!touches && insidePolygons.Count == 0)
            {
                // Ending at a point, it may close loops of lines into polygons (not from a new point, with no other line)
                if (endIndex != cstypes.NONE && startIndex != cstypes.NONE && level.EndpointLines != null)
                {
                    foreach (var loop in PolygonClosing.FindLoops(data, level.EndpointLines, startIndex, endIndex, cstypes.NONE))
                    {
                        var source = data.PolygonList[loop.Source];
                        preview.ClosedPolygons.Add((loop.Corners.Select(corner => data.EndpointList[corner].vertex).ToArray(),
                                                    source.floor_height / GeometryUtilities.WorldUnitIncrementsPerMeter,
                                                    source.ceiling_height / GeometryUtilities.WorldUnitIncrementsPerMeter));
                    }
                }

                return;
            }

            var allowsInvalid = !SettingsManager.Instance.PreventInvalidGeometryEnabled || ForgePlusInput.Editing.AllowInvalidGeometry.IsPressed();

            preview.Overlaps = true;
            preview.IsBlocked = !allowsInvalid;

            string key;
            if (splitsPlatform)
            {
                key = allowsInvalid ? "LineDrawing.InsidePolygonAllowed" : "LineDrawing.SplitsPlatform";
            }
            else if (touches)
            {
                key = allowsInvalid ? "LineDrawing.OverlapsAllowed" : "LineDrawing.Overlaps";
            }
            else
            {
                key = allowsInvalid ? "LineDrawing.InsidePolygonAllowed" : "LineDrawing.InsidePolygon";
            }

            preview.Message = Strings.Get(Strings.Common, key);
        }

        private static bool IsJoined(MapLevel data, short a, short b)
        {
            foreach (var line in data.LineList)
            {
                if ((line.endpoint_indexes[0] == a && line.endpoint_indexes[1] == b) || (line.endpoint_indexes[0] == b && line.endpoint_indexes[1] == a))
                {
                    return true;
                }
            }

            return false;
        }

        // Both are the polygon's corners, and neither is next to the other
        private static bool IsAcrossCorners(MapLevel data, short polygonIndex, short a, short b)
        {
            var polygon = data.PolygonList[polygonIndex];
            var count = polygon.vertex_count;
            var aCorner = System.Array.IndexOf(polygon.endpoint_indexes, a, 0, count);
            var bCorner = System.Array.IndexOf(polygon.endpoint_indexes, b, 0, count);

            if (aCorner < 0 || bCorner < 0)
            {
                return false;
            }

            var apart = (bCorner - aCorner + count) % count;

            return apart > 1 && apart < count - 1;
        }

        // A line with no polygon on either side (and a new point at its end, unless it ends at one, and at its start, if
        // it's drawn from empty space), and the polygons it closes (PolygonClosing), if it ends at a point
        private void AddLine()
        {
            var data = level.Level;

            var startIndex = this.startIndex != cstypes.NONE ? this.startIndex : GeometryConstruction.AddEndpoint(data, preview.Start);
            var endIndex = preview.SnappedPoint != cstypes.NONE ? preview.SnappedPoint : GeometryConstruction.AddEndpoint(data, preview.End);
            var lineIndex = GeometryConstruction.AddLine(data, startIndex, endIndex, cstypes.NONE, cstypes.NONE);

            // With no polygon on either side, nothing passes it
            LineFlagsEditing.SetSolidity(data, lineIndex, true);
            map_constructors.recalculate_redundant_line_data(data, lineIndex);
            map_constructors.recalculate_redundant_endpoint_data(data, startIndex);
            map_constructors.recalculate_redundant_endpoint_data(data, endIndex);

            var closedPolygons = new List<polygon_data>();
            if (preview.SnappedPoint != cstypes.NONE && !preview.StartsNew)
            {
                // Each found before any is added (as each is on its own side of the line)
                foreach (var loop in PolygonClosing.FindLoops(data, data.BuildEndpointLines(), startIndex, endIndex, lineIndex))
                {
                    closedPolygons.Add(PolygonClosing.AddPolygon(data, loop, endIndex));
                }
            }

            var line = data.LineList[lineIndex];
            var point = preview.SnappedPoint == cstypes.NONE ? data.EndpointList[endIndex] : null;

            Rebuild(rebuiltLevel =>
            {
                if (closedPolygons.Count > 0)
                {
                    var polygonIndex = (short) rebuiltLevel.Level.PolygonList.IndexOf(closedPolygons[0]);
                    return rebuiltLevel.Polygons.TryGetValue(polygonIndex, out var closed) ? closed : null;
                }

                if (point != null)
                {
                    var pointIndex = (short) rebuiltLevel.Level.EndpointList.IndexOf(point);
                    return rebuiltLevel.Points.TryGetValue(pointIndex, out var added) ? added : null;
                }

                var index = (short) rebuiltLevel.Level.LineList.IndexOf(line);
                return rebuiltLevel.Lines.TryGetValue(index, out var addedLine) ? addedLine : null;
            });
        }

        // The polygon keeps its corners from the line's start around (clockwise) to its end, and a new one (a copy of it)
        // takes those from its end around to its start, with the walls between them (their sides, and their polygon on
        // that side, go with them)
        private void SplitPolygon()
        {
            var data = level.Level;
            var polygonIndex = splitPolygon;
            var polygon = data.PolygonList[polygonIndex];
            var count = polygon.vertex_count;

            var startCorner = System.Array.IndexOf(polygon.endpoint_indexes, startIndex, 0, count);
            var endCorner = System.Array.IndexOf(polygon.endpoint_indexes, preview.SnappedPoint, 0, count);

            var newIndex = (short) data.PolygonList.Count;

            // The polygon goes along it from its end back to its start (on its counterclockwise side), and the new one from
            // its start to its end (on its clockwise side)
            var lineIndex = GeometryConstruction.AddLine(data, startIndex, preview.SnappedPoint, newIndex, polygonIndex);

            var keptCorners = new List<short>();
            var keptLines = new List<short>();
            for (var corner = startCorner; corner != endCorner; corner = (corner + 1) % count)
            {
                keptCorners.Add(polygon.endpoint_indexes[corner]);
                keptLines.Add(polygon.line_indexes[corner]);
            }

            keptCorners.Add(preview.SnappedPoint);
            keptLines.Add(lineIndex);

            var movedCorners = new List<short>();
            var movedLines = new List<short>();
            for (var corner = endCorner; corner != startCorner; corner = (corner + 1) % count)
            {
                movedCorners.Add(polygon.endpoint_indexes[corner]);
                movedLines.Add(polygon.line_indexes[corner]);
            }

            movedCorners.Add(startIndex);
            movedLines.Add(lineIndex);

            var added = polygon.Clone();
            added.first_object = cstypes.NONE;

            SetCorners(polygon, keptCorners, keptLines);
            SetCorners(added, movedCorners, movedLines);
            data.PolygonList.Add(added);

            // The walls that go with the new polygon, and the polygons across them (whose neighbor it now is)
            var neighbors = new List<polygon_data> { polygon, added };
            foreach (var movedLine in movedLines)
            {
                if (movedLine == lineIndex)
                {
                    continue;
                }

                var line = data.LineList[movedLine];
                var isClockwise = line.clockwise_polygon_owner == polygonIndex;

                if (isClockwise)
                {
                    line.clockwise_polygon_owner = newIndex;
                }
                else
                {
                    line.counterclockwise_polygon_owner = newIndex;
                }

                var sideIndex = line.GetSideIndex(isClockwise);
                if (sideIndex != cstypes.NONE)
                {
                    data.SideList[sideIndex].polygon_index = newIndex;
                }

                var across = line.GetPolygonOwner(!isClockwise);
                if (across != cstypes.NONE)
                {
                    neighbors.Add(data.PolygonList[across]);
                }
            }

            // What's on the new polygon's side goes with it (what's on the line stays)
            foreach (var mapObject in data.SavedObjectList)
            {
                if (mapObject.polygon_index == polygonIndex && GeometryConstruction.IsInsidePolygon(data, added, new Vector2(mapObject.location.x, mapObject.location.y)))
                {
                    mapObject.polygon_index = newIndex;
                }
            }

            foreach (var annotation in data.MapAnnotationList)
            {
                if (annotation.polygon_index == polygonIndex && GeometryConstruction.IsInsidePolygon(data, added, new Vector2(annotation.location.x, annotation.location.y)))
                {
                    annotation.polygon_index = newIndex;
                }
            }

            // Open between them, unless their heights close it (as they're the same, only where the polygon was closed)
            var isClosed = LineFlagsEditing.IsClosedByHeights(data, lineIndex);
            LineFlagsEditing.SetSolidity(data, lineIndex, isClosed);
            SET_LINE_TRANSPARENCY(data.LineList[lineIndex], !isClosed);
            map_constructors.recalculate_redundant_line_data(data, lineIndex);

            GeometryConstruction.RecalculateAfterAdding(data, neighbors, new[] { lineIndex });

            var splittingLine = data.LineList[lineIndex];
            Rebuild(rebuiltLevel =>
            {
                var index = (short) rebuiltLevel.Level.LineList.IndexOf(splittingLine);
                return rebuiltLevel.Lines.TryGetValue(index, out var addedLine) ? addedLine : null;
            });
        }

        private static bool TryMeetPlane(Ray ray, float height, out Vector3 worldPosition)
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
            if (plane.Raycast(ray, out var distance))
            {
                worldPosition = ray.GetPoint(distance);
                return true;
            }

            worldPosition = default;
            return false;
        }

        // A level location (its X along the world's X, and its Y along the world's Z, reversed)
        private static world_point2d ToLocation(Vector3 worldPosition, bool snaps)
        {
            var x = Mathf.Round(worldPosition.x * GeometryUtilities.WorldUnitIncrementsPerMeter);
            var y = Mathf.Round(-worldPosition.z * GeometryUtilities.WorldUnitIncrementsPerMeter);

            if (snaps)
            {
                x = Mathf.Round(x / HeightsEditing.SnapIncrement) * HeightsEditing.SnapIncrement;
                y = Mathf.Round(y / HeightsEditing.SnapIncrement) * HeightsEditing.SnapIncrement;
            }

            return new world_point2d((short) Mathf.Clamp(x, short.MinValue, short.MaxValue), (short) Mathf.Clamp(y, short.MinValue, short.MaxValue));
        }

        // The floor (in meters) of the polygon whose corner is nearest the location (the lowest, of those at that corner)
        private static float NearestCornerFloor(MapLevel data, world_point2d location)
        {
            var nearestDistance = long.MaxValue;
            var floor = 0;

            foreach (var polygon in data.PolygonList)
            {
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    var vertex = data.EndpointList[polygon.endpoint_indexes[i]].vertex;
                    long dx = vertex.x - location.x;
                    long dy = vertex.y - location.y;
                    var distance = dx * dx + dy * dy;

                    if (distance < nearestDistance || (distance == nearestDistance && polygon.floor_height < floor))
                    {
                        nearestDistance = distance;
                        floor = polygon.floor_height;
                    }
                }
            }

            return floor / GeometryUtilities.WorldUnitIncrementsPerMeter;
        }

        private static void SetCorners(polygon_data polygon, List<short> corners, List<short> lines)
        {
            polygon.vertex_count = (ushort) corners.Count;

            for (var i = 0; i < MAXIMUM_VERTICES_PER_POLYGON; i++)
            {
                polygon.endpoint_indexes[i] = i < corners.Count ? corners[i] : cstypes.NONE;
                polygon.line_indexes[i] = i < lines.Count ? lines[i] : cstypes.NONE;
                polygon.adjacent_polygon_indexes[i] = cstypes.NONE;
                polygon.side_indexes[i] = cstypes.NONE;
            }
        }

        // The level's structure has changed, so it's rebuilt, then what was added is selected (with the edit, as one action)
        private void Rebuild(System.Func<LevelEntity_Level, ISelectable> findAdded)
        {
            // Until the level is rebuilt, which works these out for its entities
            level.EndpointLines = level.Level.BuildEndpointLines();
            PolygonContainmentMap.MarkChanged();

            LevelEditing.RebuildLevel(() =>
            {
                var rebuiltLevel = LevelEntity_Level.Instance;
                if (!rebuiltLevel || rebuiltLevel.Points == null)
                {
                    return;
                }

                var added = findAdded(rebuiltLevel);
                if (added != null)
                {
                    // Undone and redone with the edit (which the rebuild has recorded)
                    SelectionHistory.JoinNextToLast();
                    SelectionManager.Instance.SelectObject(added, multiSelect: false);
                }
            });
        }
    }
}
#endif
