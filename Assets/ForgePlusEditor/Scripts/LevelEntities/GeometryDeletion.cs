#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.Extensions;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using static AlephOne.map;

namespace ForgePlus.LevelManipulation
{
    // In Geometry mode, Delete Selected (Delete or Backspace: SelectionDeletion) deletes the selected polygons and points,
    // and the points at both ends of the selected lines (which takes the lines with them), as one action:
    //
    // A deleted polygon leaves its lines and points behind, empty (what was in it, its objects and annotations, goes with
    // it). Its neighbors' faces toward it become walls, with sides where they're seen.
    //
    // A deleted point is taken out of its chain of lines, rather than leaving a gap: with exactly two lines (to points
    // not already joined), they become one, as the Errors panel's fix for a redundant straight corner does. Otherwise
    // each polygon at it loses that corner, its neighbors on either side of it joined by a line (an existing one, or a new
    // wall), and the point's lines go. A polygon left with fewer than three corners is deleted too (as is one whose new
    // line is already another polygon's on that side). What was in the polygons that shrink is kept inside them.
    //
    // Points that end up with no lines are removed as well (OrphanedPoints). The level's structure changes, so it's
    // rebuilt afterward, with nothing selected.
    public static class GeometryDeletion
    {
        public static void DeleteSelection(LevelEntity_Level level)
        {
            if (level.Points == null || LineDrawing.Current != null || LineExtrusion.Current != null || PointEditing.Feedback != null)
            {
                return;
            }

            var selection = SelectionManager.Instance.Selection;
            var polygons = selection.OfType<LevelEntity_Polygon>().Select(polygon => polygon.NativeObject).ToList();
            var points = selection.OfType<LevelEntity_Point>().Select(point => point.NativeObject).ToList();

            // A line goes by deleting its points
            foreach (var line in selection.OfType<LevelEntity_Line>())
            {
                foreach (var pointIndex in line.NativeObject.endpoint_indexes)
                {
                    var point = level.Level.EndpointList[pointIndex];
                    if (!points.Contains(point))
                    {
                        points.Add(point);
                    }
                }
            }

            if (polygons.Count > 0 || points.Count > 0)
            {
                Delete(level, polygons, points);
            }
        }

        public static void Delete(LevelEntity_Level level, IEnumerable<polygon_data> polygons, IEnumerable<endpoint_data> points)
        {
            SelectionManager.Instance.DeselectAll();

            var data = level.Level;
            var affectedPolygons = new HashSet<polygon_data>();
            var touchedLines = new HashSet<line_data>();
            var placements = new ObjectPlacement.Snapshot();
            var orphanCandidates = new HashSet<endpoint_data>();

            foreach (var polygon in polygons.ToList())
            {
                RemovePolygon(data, polygon, affectedPolygons, touchedLines);
            }

            foreach (var point in points.ToList())
            {
                RemovePoint(data, point, affectedPolygons, touchedLines, placements, orphanCandidates);
            }

            RepairFaces(data, touchedLines);
            OrphanedPoints.RemoveNewlyOrphaned(data, orphanCandidates);

            GeometryRepair.FinishCollapse(level, affectedPolygons, placements, keptPoint: null);
        }

        // Leaving its lines, with what's in it gone, and its neighbors noted (their faces toward it become walls)
        private static void RemovePolygon(MapLevel data, polygon_data polygon, HashSet<polygon_data> affectedPolygons, HashSet<line_data> touchedLines)
        {
            var polygonIndex = (short) data.PolygonList.IndexOf(polygon);
            if (polygonIndex < 0)
            {
                return;
            }

            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var line = data.LineList[polygon.line_indexes[i]];
                touchedLines.Add(line);

                foreach (var isClockwise in new[] { true, false })
                {
                    var neighbor = line.GetPolygonOwner(isClockwise);
                    if (neighbor != cstypes.NONE && neighbor != polygonIndex)
                    {
                        affectedPolygons.Add(data.PolygonList[neighbor]);
                    }
                }
            }

            RemoveContents(data, polygonIndex);

            affectedPolygons.Remove(polygon);
            LevelTopology.RemovePolygon(data, polygon, keepsLines: true);
        }

        // The objects and annotations in a polygon that's going (which can't be anywhere else)
        private static void RemoveContents(MapLevel data, short polygonIndex)
        {
            data.SavedObjectList.RemoveAll(mapObject => mapObject.polygon_index == polygonIndex);
            data.MapAnnotationList.RemoveAll(annotation => annotation.polygon_index == polygonIndex);
        }

        private static void RemovePoint(MapLevel data, endpoint_data point, HashSet<polygon_data> affectedPolygons, HashSet<line_data> touchedLines,
            ObjectPlacement.Snapshot placements, HashSet<endpoint_data> orphanCandidates)
        {
            var pointIndex = (short) data.EndpointList.IndexOf(point);
            if (pointIndex < 0)
            {
                return;
            }

            orphanCandidates.Remove(point);

            var lineIndexes = new List<short>();
            for (short lineIndex = 0; lineIndex < data.LineList.Count; lineIndex++)
            {
                var line = data.LineList[lineIndex];
                if (line.endpoint_indexes[0] == pointIndex || line.endpoint_indexes[1] == pointIndex)
                {
                    lineIndexes.Add(lineIndex);
                    orphanCandidates.Add(data.EndpointList[OtherEnd(line, pointIndex)]);
                }
            }

            var polygonsAtPoint = new List<polygon_data>();
            foreach (var polygon in data.PolygonList)
            {
                if (CornerOf(polygon, pointIndex) >= 0)
                {
                    polygonsAtPoint.Add(polygon);
                }
            }

            if (CanMergeLines(data, pointIndex, lineIndexes, polygonsAtPoint.Count > 0))
            {
                foreach (var lineIndex in lineIndexes)
                {
                    touchedLines.Add(data.LineList[lineIndex]);
                }

                GeometryRepair.MergeLinesAtPoint(data, pointIndex, affectedPolygons, placements);
                return;
            }

            // Each polygon at it keeps the rest of its corners, joined across where it was, unless that leaves it too few
            // or the line joining them already has a polygon on that side
            var collapsed = new List<polygon_data>();
            var kept = new List<polygon_data>();
            foreach (var polygon in polygonsAtPoint)
            {
                if (polygon.vertex_count - 1 < 3)
                {
                    collapsed.Add(polygon);
                }
                else
                {
                    kept.Add(polygon);
                }
            }

            foreach (var polygon in kept.ToList())
            {
                var polygonIndex = (short) data.PolygonList.IndexOf(polygon);
                var (before, after) = Neighbors(polygon, pointIndex);
                var joining = FindLine(data, before, after);
                if (joining == cstypes.NONE)
                {
                    continue;
                }

                var line = data.LineList[joining];
                var owner = line.GetPolygonOwner(line.endpoint_indexes[0] == before);
                if (owner != cstypes.NONE && owner != polygonIndex && !collapsed.Contains(data.PolygonList[owner]))
                {
                    kept.Remove(polygon);
                    collapsed.Add(polygon);
                }
            }

            var lines = lineIndexes.Select(lineIndex => data.LineList[lineIndex]).ToList();

            placements.Record(data, kept.Select(polygon => (short) data.PolygonList.IndexOf(polygon)).ToList());

            foreach (var polygon in collapsed)
            {
                RemovePolygon(data, polygon, affectedPolygons, touchedLines);
            }

            foreach (var polygon in kept)
            {
                var polygonIndex = (short) data.PolygonList.IndexOf(polygon);
                var (before, after) = Neighbors(polygon, pointIndex);

                // Going clockwise around it, it goes from the corner before to the one after, so it's on that side of the
                // line the way the line goes from one to the other
                var joining = FindLine(data, before, after);
                if (joining == cstypes.NONE)
                {
                    joining = GeometryConstruction.AddLine(data, before, after, polygonIndex, cstypes.NONE);
                }
                else
                {
                    var line = data.LineList[joining];
                    if (line.endpoint_indexes[0] == before)
                    {
                        line.clockwise_polygon_owner = polygonIndex;
                    }
                    else
                    {
                        line.counterclockwise_polygon_owner = polygonIndex;
                    }
                }

                RemoveCorner(polygon, CornerOf(polygon, pointIndex), joining);

                touchedLines.Add(data.LineList[joining]);
                affectedPolygons.Add(polygon);
            }

            // Its lines go (with their sides), then it does
            foreach (var line in lines)
            {
                line.clockwise_polygon_owner = cstypes.NONE;
                line.counterclockwise_polygon_owner = cstypes.NONE;
                touchedLines.Remove(line);
                LevelTopology.RemoveLine(data, line);
            }

            LevelTopology.RemoveEndpoint(data, point);
        }

        // Its two lines can become one: they lead to two different points not already joined, and a polygon's corner
        // there is cut across inside it, while a line with no polygon mustn't cross or run through anything
        private static bool CanMergeLines(MapLevel data, short pointIndex, List<short> lineIndexes, bool isCorner)
        {
            if (lineIndexes.Count != 2)
            {
                return false;
            }

            var a = OtherEnd(data.LineList[lineIndexes[0]], pointIndex);
            var b = OtherEnd(data.LineList[lineIndexes[1]], pointIndex);
            if (a == b || FindLine(data, a, b) != cstypes.NONE)
            {
                return false;
            }

            if (isCorner)
            {
                return true;
            }

            var start = GeometryConstruction.ToVector(data.EndpointList[a].vertex);
            var end = GeometryConstruction.ToVector(data.EndpointList[b].vertex);
            if (GeometryConstruction.SegmentTouchesLines(data, start, end, a, b, ignoredLineIndexes: lineIndexes) ||
                GeometryConstruction.SegmentTouchesPoints(data, start, end, a, b, pointIndex))
            {
                return false;
            }

            var middle = ((UnityEngine.Vector2) start + end) * 0.5f;

            return !data.PolygonList.Any(polygon => GeometryConstruction.IsInsidePolygon(data, polygon, middle));
        }

        // Lines left with a polygon on one side are walls (solid, with a side where its face is seen), and lines left with
        // none are solid with no sides. What's worked out from them is worked out again.
        private static void RepairFaces(MapLevel data, IEnumerable<line_data> lines)
        {
            foreach (var line in lines)
            {
                var lineIndex = (short) data.LineList.IndexOf(line);
                if (lineIndex < 0)
                {
                    continue;
                }

                var clockwiseOwner = line.clockwise_polygon_owner;
                var counterclockwiseOwner = line.counterclockwise_polygon_owner;
                var isShared = clockwiseOwner != cstypes.NONE && counterclockwiseOwner != cstypes.NONE;

                if (!isShared)
                {
                    LineFlagsEditing.SetSolidity(data, lineIndex, true);
                    SET_LINE_TRANSPARENCY(line, false);
                    SET_LINE_VARIABLE_ELEVATION(line, false);

                    foreach (var isClockwise in new[] { true, false })
                    {
                        var owner = line.GetPolygonOwner(isClockwise);
                        var sideIndex = line.GetSideIndex(isClockwise);

                        if (owner == cstypes.NONE)
                        {
                            LevelTopology.RemoveSide(data, lineIndex, isClockwise);
                        }
                        else if (sideIndex != cstypes.NONE)
                        {
                            // A wall shows its primary texture floor to ceiling
                            data.SideList[sideIndex].type = GeometryConstruction.SideTypeForHeights(data, sideIndex);
                            map_constructors.recalculate_redundant_side_data(data, sideIndex, lineIndex);
                            LineFlagsEditing.UpdateForSide(data, data.SideList[sideIndex]);
                        }
                        else if (LevelEntity_Side.IsFaceExposed(data, lineIndex, isClockwise))
                        {
                            var source = GeometryConstruction.FindSideToCopy(data, owner, new short[0], line.endpoint_indexes, lineIndex);
                            GeometryConstruction.AddSide(data, owner, lineIndex, source);
                        }
                    }
                }

                map_constructors.recalculate_redundant_line_data(data, lineIndex);
                map_constructors.recalculate_redundant_endpoint_data(data, line.endpoint_indexes[0]);
                map_constructors.recalculate_redundant_endpoint_data(data, line.endpoint_indexes[1]);
            }
        }

        // Its corners before and after the point (going clockwise)
        private static (short Before, short After) Neighbors(polygon_data polygon, short pointIndex)
        {
            var count = polygon.vertex_count;
            var corner = CornerOf(polygon, pointIndex);

            return (polygon.endpoint_indexes[(corner + count - 1) % count], polygon.endpoint_indexes[(corner + 1) % count]);
        }

        // The corner goes, and the edge that led to it leads along the joining line to the corner after it
        private static void RemoveCorner(polygon_data polygon, int corner, short joiningLine)
        {
            var count = polygon.vertex_count;

            for (var i = corner; i < count - 1; i++)
            {
                polygon.endpoint_indexes[i] = polygon.endpoint_indexes[i + 1];
                polygon.line_indexes[i] = polygon.line_indexes[i + 1];
                polygon.adjacent_polygon_indexes[i] = polygon.adjacent_polygon_indexes[i + 1];
                polygon.side_indexes[i] = polygon.side_indexes[i + 1];
            }

            polygon.vertex_count--;
            polygon.endpoint_indexes[polygon.vertex_count] = cstypes.NONE;
            polygon.line_indexes[polygon.vertex_count] = cstypes.NONE;
            polygon.adjacent_polygon_indexes[polygon.vertex_count] = cstypes.NONE;
            polygon.side_indexes[polygon.vertex_count] = cstypes.NONE;

            polygon.line_indexes[(corner + polygon.vertex_count - 1) % polygon.vertex_count] = joiningLine;
        }

        private static int CornerOf(polygon_data polygon, short pointIndex)
        {
            return System.Array.IndexOf(polygon.endpoint_indexes, pointIndex, 0, polygon.vertex_count);
        }

        private static short OtherEnd(line_data line, short pointIndex)
        {
            return line.endpoint_indexes[0] == pointIndex ? line.endpoint_indexes[1] : line.endpoint_indexes[0];
        }

        private static short FindLine(MapLevel data, short a, short b)
        {
            for (short lineIndex = 0; lineIndex < data.LineList.Count; lineIndex++)
            {
                var line = data.LineList[lineIndex];
                if ((line.endpoint_indexes[0] == a && line.endpoint_indexes[1] == b) || (line.endpoint_indexes[0] == b && line.endpoint_indexes[1] == a))
                {
                    return lineIndex;
                }
            }

            return cstypes.NONE;
        }
    }
}
#endif
