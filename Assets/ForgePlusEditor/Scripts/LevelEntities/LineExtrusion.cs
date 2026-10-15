#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.Extensions;
using ForgePlus.History;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.Localization;
using ForgePlus.PolygonContainment;
using RuntimeCore.Entities;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using static AlephOne.map;

namespace ForgePlus.LevelManipulation
{
    // Geometry mode's Draw tool, dragging a line beside a polygon: a copy of the line follows the pointer (across the level
    // plane, snapping how far it moves to the grid as moving a line does), and letting go adds a polygon between the line
    // and its copy, on the line's open side. The polygon has the floor and ceiling (heights, textures, lights and transfer
    // modes) of the one beside the line, and its three new walls copy that polygon's side on the line (its textures,
    // lights and transfer modes, but not a control panel). The line then joins the two polygons, and its side (now hidden)
    // goes, as a height edit hides it (SideExposure).
    //
    // What it would add is shown as it goes (Current, which PointHandles draws). It can't be added where the line has no
    // polygon beside it, or already has one on the side it's dragged to. While Prevent Invalid Moves is on (the setting),
    // it also can't overlap other lines or points, unless Allow Invalid Move (shift) is held when letting go.
    [AutoStaticsCleanup]
    public sealed partial class LineExtrusion
    {
        // What the drag would add, as it's shown
        public sealed class Preview
        {
            // The line's points (in the new polygon's clockwise order), and their copies
            public world_point2d A;
            public world_point2d B;
            public world_point2d CopyA;
            public world_point2d CopyB;

            // Whether there's a polygon to show (it's been dragged away from the line), and the floor and ceiling it'd have
            // (in meters)
            public bool HasShape;
            public float FloorHeight;
            public float CeilingHeight;

            // Where the note is shown (in meters): the height the line was grabbed at
            public float Height;

            // Whether it can't be added (as it is), or it can but overlaps something, and what the note says (or null)
            public bool IsBlocked;
            public bool Overlaps;
            public string Message;
        }

        // While a line is dragged in Draw mode
        public static Preview Current { get; private set; }

        private readonly LevelEntity_Level level;
        private readonly short lineIndex;
        private readonly LevelPlaneDrag drag;
        private readonly Preview preview = new Preview();

        // How far the copy is from the line
        private Vector2Int offset;

        // The polygon on each of the line's sides (clockwise, counterclockwise), or NONE
        private readonly short clockwiseOwner;
        private readonly short counterclockwiseOwner;

        // Whether the polygons beside the line go clockwise as their corners' area does (as the map shows them), which
        // tells which side of the line the copy is on
        private readonly bool areaIsPositiveClockwise = true;

        // How the drag would add the polygon: which polygon it's beside, and whether the new one goes along the line from
        // its first point to its last
        private short sourcePolygon = cstypes.NONE;
        private bool followsLine;

        public static LineExtrusion Begin(LevelEntity_Level level, short lineIndex, Vector3 pressWorldPosition)
        {
            if (!level || LevelEditing.IsRebuildPending || lineIndex < 0 || lineIndex >= level.Level.LineList.Count)
            {
                return null;
            }

            return new LineExtrusion(level, lineIndex, pressWorldPosition);
        }

        private LineExtrusion(LevelEntity_Level level, short lineIndex, Vector3 pressWorldPosition)
        {
            this.level = level;
            this.lineIndex = lineIndex;

            var line = level.Level.LineList[lineIndex];
            clockwiseOwner = line.clockwise_polygon_owner;
            counterclockwiseOwner = line.counterclockwise_polygon_owner;

            var owner = clockwiseOwner != cstypes.NONE ? clockwiseOwner : counterclockwiseOwner;
            if (owner != cstypes.NONE)
            {
                areaIsPositiveClockwise = SignedArea(level.Level, level.Level.PolygonList[owner]) >= 0;
            }

            // Dragged as a movement (from nothing), so it's the movement that snaps
            drag = new LevelPlaneDrag(pressWorldPosition, new world_point2d(0, 0));
            preview.Height = pressWorldPosition.y;

            // Recorded as one action when it ends
            LevelHistory.BeginGesture();

            Current = preview;
        }

        public void Drag(Ray pointerRay)
        {
            var dragged = drag.DraggedLocation(pointerRay);
            offset = new Vector2Int(dragged.x, dragged.y);

            UpdatePreview();
        }

        // Adds the polygon, if it can be added
        public void End()
        {
            Current = null;

            try
            {
                // As it would be added now (with Allow Invalid Move as it is now)
                UpdatePreview();

                if (preview.HasShape && !preview.IsBlocked)
                {
                    AddPolygon();
                }
            }
            finally
            {
                LevelHistory.EndGesture();
            }
        }

        // The side the new polygon would be on already has one
        private bool IsSideTaken
        {
            get
            {
                return (followsLine ? clockwiseOwner : counterclockwiseOwner) != cstypes.NONE;
            }
        }

        private void UpdatePreview()
        {
            var data = level.Level;
            var line = data.LineList[lineIndex];
            var first = data.EndpointList[line.endpoint_indexes[0]].vertex;
            var last = data.EndpointList[line.endpoint_indexes[1]].vertex;

            preview.HasShape = false;
            preview.IsBlocked = false;
            preview.Overlaps = false;
            preview.Message = null;

            if (offset == Vector2Int.zero)
            {
                return;
            }

            // Along the line from its first point to its last, the copy is on the clockwise side when the corners would go
            // clockwise (as the polygons' do)
            var cross = (long) (last.x - first.x) * offset.y - (long) (last.y - first.y) * offset.x;
            followsLine = cross > 0 == areaIsPositiveClockwise;

            preview.A = followsLine ? first : last;
            preview.B = followsLine ? last : first;
            preview.CopyA = Moved(preview.A, offset);
            preview.CopyB = Moved(preview.B, offset);

            // The polygon on the line's other side, which it's shown with the heights of (or, if the copy is on that
            // polygon's side, that one's)
            sourcePolygon = followsLine ? counterclockwiseOwner : clockwiseOwner;
            var shownPolygon = sourcePolygon != cstypes.NONE ? sourcePolygon : clockwiseOwner != cstypes.NONE ? clockwiseOwner : counterclockwiseOwner;

            if (cross == 0)
            {
                preview.IsBlocked = true;
                preview.Message = Strings.Get(Strings.Common, "LineExtrusion.Parallel");
                return;
            }

            if (shownPolygon == cstypes.NONE)
            {
                preview.IsBlocked = true;
                preview.Message = Strings.Get(Strings.Common, "LineExtrusion.NoPolygon");
                return;
            }

            var polygon = data.PolygonList[shownPolygon];
            preview.FloorHeight = polygon.floor_height / GeometryUtilities.WorldUnitIncrementsPerMeter;
            preview.CeilingHeight = polygon.ceiling_height / GeometryUtilities.WorldUnitIncrementsPerMeter;
            preview.HasShape = true;

            if (IsSideTaken)
            {
                preview.IsBlocked = true;
                preview.Message = Strings.Get(Strings.Common, "LineExtrusion.SideTaken");
                return;
            }

            preview.Overlaps = OverlapsGeometry(data, line);
            if (preview.Overlaps)
            {
                var allowsOverlap = !SettingsManager.Instance.PreventInvalidGeometryEnabled || ForgePlusInput.Editing.AllowInvalidGeometry.IsPressed();

                preview.IsBlocked = !allowsOverlap;
                preview.Message = Strings.Get(Strings.Common, allowsOverlap ? "LineExtrusion.OverlapsAllowed" : "LineExtrusion.Overlaps");
            }
        }

        // Whether the new polygon's walls cross (or touch) other lines, or other points are in it or on its walls
        private bool OverlapsGeometry(MapLevel data, line_data line)
        {
            var aIndex = followsLine ? line.endpoint_indexes[0] : line.endpoint_indexes[1];
            var bIndex = followsLine ? line.endpoint_indexes[1] : line.endpoint_indexes[0];

            var a = ToVector(preview.A);
            var b = ToVector(preview.B);
            var copyA = ToVector(preview.CopyA);
            var copyB = ToVector(preview.CopyB);

            // Each new wall, the line's point at one end of it (if it has one), and its other end
            var walls = new[] { (b, copyB, bIndex, copyB), (copyB, copyA, cstypes.NONE, copyA), (copyA, a, aIndex, copyA) };

            for (short otherIndex = 0; otherIndex < data.LineList.Count; otherIndex++)
            {
                if (otherIndex == lineIndex)
                {
                    continue;
                }

                var other = data.LineList[otherIndex];
                var p = ToVector(data.EndpointList[other.endpoint_indexes[0]].vertex);
                var q = ToVector(data.EndpointList[other.endpoint_indexes[1]].vertex);

                foreach (var (start, end, pointIndex, wallFar) in walls)
                {
                    if (pointIndex != cstypes.NONE && (other.endpoint_indexes[0] == pointIndex || other.endpoint_indexes[1] == pointIndex))
                    {
                        // Meeting at the line's point, they only overlap if they run along each other
                        var otherFar = other.endpoint_indexes[0] == pointIndex ? q : p;

                        if (IsOnSegment(start, end, otherFar) || IsOnSegment(p, q, wallFar))
                        {
                            return true;
                        }
                    }
                    else if (SegmentsTouch(start, end, p, q))
                    {
                        return true;
                    }
                }
            }

            // Points in it (but not those on the line it starts from)
            var side = b - a;
            var determinant = (double) side.x * offset.y - (double) side.y * offset.x;
            for (short pointIndex = 0; pointIndex < data.EndpointList.Count; pointIndex++)
            {
                if (pointIndex == aIndex || pointIndex == bIndex)
                {
                    continue;
                }

                var relative = ToVector(data.EndpointList[pointIndex].vertex) - a;
                var along = (relative.x * (double) offset.y - relative.y * (double) offset.x) / determinant;
                var across = (side.x * (double) relative.y - side.y * (double) relative.x) / determinant;

                if (along >= 0d && along <= 1d && across > 0d && across <= 1d)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddPolygon()
        {
            var data = level.Level;
            var line = data.LineList[lineIndex];
            var source = data.PolygonList[sourcePolygon];

            var aIndex = followsLine ? line.endpoint_indexes[0] : line.endpoint_indexes[1];
            var bIndex = followsLine ? line.endpoint_indexes[1] : line.endpoint_indexes[0];

            // The source polygon's side on the line, which the new walls copy (as it was, before it's hidden)
            var sourceSideIndex = line.GetSideIndex(!followsLine);
            var sourceSide = sourceSideIndex != cstypes.NONE ? data.SideList[sourceSideIndex].Clone() : null;

            var exposure = new SideExposure();
            exposure.RecordPolygon(data, source);

            var polygonIndex = (short) data.PolygonList.Count;

            var copyAIndex = AddEndpoint(data, preview.CopyA);
            var copyBIndex = AddEndpoint(data, preview.CopyB);

            // Clockwise around the new polygon: along the line, out to the copy, back along it, and back to the line
            var nearWallIndex = AddLine(data, bIndex, copyBIndex, polygonIndex);
            var copyIndex = AddLine(data, copyBIndex, copyAIndex, polygonIndex);
            var farWallIndex = AddLine(data, copyAIndex, aIndex, polygonIndex);

            if (followsLine)
            {
                line.clockwise_polygon_owner = polygonIndex;
            }
            else
            {
                line.counterclockwise_polygon_owner = polygonIndex;
            }

            var polygon = new polygon_data
            {
                type = _polygon_is_normal,
                vertex_count = 4,
                floor_texture = source.floor_texture,
                ceiling_texture = source.ceiling_texture,
                floor_height = source.floor_height,
                ceiling_height = source.ceiling_height,
                floor_lightsource_index = source.floor_lightsource_index,
                ceiling_lightsource_index = source.ceiling_lightsource_index,
                floor_transfer_mode = source.floor_transfer_mode,
                ceiling_transfer_mode = source.ceiling_transfer_mode,
                floor_origin = source.floor_origin,
                ceiling_origin = source.ceiling_origin,
                first_object = cstypes.NONE,
                media_index = cstypes.NONE,
                media_lightsource_index = source.media_lightsource_index,
                sound_source_indexes = cstypes.NONE,
                ambient_sound_image_index = cstypes.NONE,
                random_sound_image_index = cstypes.NONE,
            };

            var corners = new[] { aIndex, bIndex, copyBIndex, copyAIndex };
            var edges = new[] { lineIndex, nearWallIndex, copyIndex, farWallIndex };
            for (var i = 0; i < MAXIMUM_VERTICES_PER_POLYGON; i++)
            {
                polygon.endpoint_indexes[i] = i < 4 ? corners[i] : cstypes.NONE;
                polygon.line_indexes[i] = i < 4 ? edges[i] : cstypes.NONE;
                polygon.adjacent_polygon_indexes[i] = cstypes.NONE;
                polygon.side_indexes[i] = cstypes.NONE;
            }

            data.PolygonList.Add(polygon);

            foreach (var wallIndex in new[] { nearWallIndex, copyIndex, farWallIndex })
            {
                AddSide(data, polygonIndex, wallIndex, sourceSide);
            }

            // The line's hidden side goes (or one it reveals is added)
            exposure.ApplyToSides(data);

            // The line opens between the polygons, unless their heights close it; the new walls are solid
            var isClosed = LineFlagsEditing.IsClosedByHeights(data, lineIndex);
            LineFlagsEditing.SetSolidity(data, lineIndex, isClosed);
            SET_LINE_TRANSPARENCY(line, !isClosed);

            foreach (var wallIndex in new[] { nearWallIndex, copyIndex, farWallIndex })
            {
                LineFlagsEditing.SetSolidity(data, wallIndex, true);
                map_constructors.recalculate_redundant_line_data(data, wallIndex);
            }

            map_constructors.recalculate_redundant_line_data(data, lineIndex);
            LineCollapse.RecalculateAround(data, new[] { source, polygon });

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

                var index = (short) rebuiltLevel.Level.PolygonList.IndexOf(polygon);
                if (rebuiltLevel.Polygons.TryGetValue(index, out var added))
                {
                    // Undone and redone with the polygon (the edit, which the rebuild has recorded)
                    SelectionHistory.JoinNextToLast();
                    SelectionManager.Instance.SelectObject(added, multiSelect: false);
                }
            });
        }

        private static short AddEndpoint(MapLevel data, world_point2d location)
        {
            data.EndpointList.Add(new endpoint_data
            {
                vertex = location,
                supporting_polygon_index = cstypes.NONE,
            });

            return (short) (data.EndpointList.Count - 1);
        }

        // Owned on its clockwise side by the polygon
        private static short AddLine(MapLevel data, short firstIndex, short lastIndex, short polygonIndex)
        {
            var line = new line_data
            {
                clockwise_polygon_owner = polygonIndex,
                counterclockwise_polygon_owner = cstypes.NONE,
                clockwise_polygon_side_index = cstypes.NONE,
                counterclockwise_polygon_side_index = cstypes.NONE,
            };

            line.endpoint_indexes[0] = firstIndex;
            line.endpoint_indexes[1] = lastIndex;
            line.length = world.distance2d(data.EndpointList[firstIndex].vertex, data.EndpointList[lastIndex].vertex);

            data.LineList.Add(line);

            return (short) (data.LineList.Count - 1);
        }

        // A copy of the source side (its textures, lights and transfer modes, but not a control panel), or a blank one if
        // there's none (map_constructors.cpp: new_side), on the polygon's (clockwise) side of the wall
        private static void AddSide(MapLevel data, short polygonIndex, short wallIndex, side_data source)
        {
            if (source == null)
            {
                map_constructors.new_side(data, polygonIndex, wallIndex);
                return;
            }

            var side = source.Clone();
            side.flags = 0;
            side.control_panel_type = 0;
            side.control_panel_permutation = 0;
            side.polygon_index = polygonIndex;
            side.line_index = wallIndex;

            var sideIndex = (short) data.SideList.Count;
            data.SideList.Add(side);
            data.LineList[wallIndex].clockwise_polygon_side_index = sideIndex;

            map_constructors.recalculate_redundant_side_data(data, sideIndex, wallIndex);
            map_constructors.recalculate_side_type(data, sideIndex);
        }

        // Twice the area its corners enclose, positive for corners that go clockwise as the map shows them (its Y down)
        private static long SignedArea(MapLevel data, polygon_data polygon)
        {
            long area = 0;
            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var current = data.EndpointList[polygon.endpoint_indexes[i]].vertex;
                var next = data.EndpointList[polygon.endpoint_indexes[(i + 1) % polygon.vertex_count]].vertex;
                area += (long) current.x * next.y - (long) next.x * current.y;
            }

            return area;
        }

        private static world_point2d Moved(world_point2d start, Vector2Int movement)
        {
            return new world_point2d((short) Mathf.Clamp(start.x + movement.x, short.MinValue, short.MaxValue),
                                     (short) Mathf.Clamp(start.y + movement.y, short.MinValue, short.MaxValue));
        }

        private static Vector2Int ToVector(world_point2d point)
        {
            return new Vector2Int(point.x, point.y);
        }

        private static long Cross(Vector2Int origin, Vector2Int a, Vector2Int b)
        {
            return (long) (a.x - origin.x) * (b.y - origin.y) - (long) (a.y - origin.y) * (b.x - origin.x);
        }

        // On the segment, including its ends
        private static bool IsOnSegment(Vector2Int start, Vector2Int end, Vector2Int point)
        {
            return Cross(start, end, point) == 0 &&
                   point.x >= Mathf.Min(start.x, end.x) && point.x <= Mathf.Max(start.x, end.x) &&
                   point.y >= Mathf.Min(start.y, end.y) && point.y <= Mathf.Max(start.y, end.y);
        }

        // Crossing or touching
        private static bool SegmentsTouch(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int d)
        {
            var d1 = System.Math.Sign(Cross(c, d, a));
            var d2 = System.Math.Sign(Cross(c, d, b));
            var d3 = System.Math.Sign(Cross(a, b, c));
            var d4 = System.Math.Sign(Cross(a, b, d));

            if (d1 * d2 < 0 && d3 * d4 < 0)
            {
                return true;
            }

            return d1 == 0 && IsOnSegment(c, d, a) ||
                   d2 == 0 && IsOnSegment(c, d, b) ||
                   d3 == 0 && IsOnSegment(a, b, c) ||
                   d4 == 0 && IsOnSegment(a, b, d);
        }
    }
}
#endif
