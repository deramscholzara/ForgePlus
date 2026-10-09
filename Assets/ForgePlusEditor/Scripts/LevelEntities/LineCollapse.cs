#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using static AlephOne.map;

namespace ForgePlus.LevelManipulation
{
    // A line with no length collapses into a single point: its two points become one, and the line (with its sides) is
    // gone. Each polygon along it loses that corner, and one left with fewer than three corners is gone too. Lines that
    // then join the same two points are merged into one, between the polygons on their outer sides. The level's structure
    // changes, so it's rebuilt afterward.
    public static class LineCollapse
    {
        // Whether the line can collapse: its two points are where they meet, and no polygon has both points without
        // having the line between them (it would be pinched into two)
        public static bool CanCollapse(MapLevel level, short lineIndex)
        {
            var line = level.LineList[lineIndex];
            var a = line.endpoint_indexes[0];
            var b = line.endpoint_indexes[1];

            if (level.EndpointList[a].vertex.x != level.EndpointList[b].vertex.x ||
                level.EndpointList[a].vertex.y != level.EndpointList[b].vertex.y)
            {
                return false;
            }

            foreach (var polygon in level.PolygonList)
            {
                var hasA = HasCorner(polygon, a);
                var hasB = HasCorner(polygon, b);

                if (hasA && hasB && a != b && !HasLine(polygon, lineIndex))
                {
                    return false;
                }
            }

            return true;
        }

        // Keeps the given point (one of the line's), and returns it (or null, if it can't collapse). Affected polygons are
        // added to the given set, by their data (as indexes change along the way).
        public static endpoint_data Collapse(MapLevel level, short lineIndex, short keptPointIndex, HashSet<polygon_data> affectedPolygons)
        {
            if (!CanCollapse(level, lineIndex))
            {
                return null;
            }

            var line = level.LineList[lineIndex];
            var removedPointIndex = line.endpoint_indexes[0] == keptPointIndex ? line.endpoint_indexes[1] : line.endpoint_indexes[0];
            var keptPoint = level.EndpointList[keptPointIndex];
            var removedPoint = level.EndpointList[removedPointIndex];

            // Each polygon along it loses the line, and the corner where it started (the next line then starts at the
            // kept point, once the removed one is renamed)
            var degeneratePolygons = new List<polygon_data>();
            foreach (var polygon in level.PolygonList)
            {
                var corner = IndexOfLine(polygon, lineIndex);
                if (corner < 0)
                {
                    continue;
                }

                RemoveCorner(polygon, corner);
                affectedPolygons.Add(polygon);

                if (polygon.vertex_count < 3)
                {
                    degeneratePolygons.Add(polygon);
                }
            }

            // Everything at the removed point is at the kept one
            if (removedPointIndex != keptPointIndex)
            {
                LevelTopology.RenameEndpoint(level, removedPointIndex, keptPointIndex);
            }

            line.clockwise_polygon_owner = cstypes.NONE;
            line.counterclockwise_polygon_owner = cstypes.NONE;
            LevelTopology.RemoveLine(level, line);

            foreach (var polygon in degeneratePolygons)
            {
                affectedPolygons.Remove(polygon);
                AddNeighbors(level, polygon, affectedPolygons);
                LevelTopology.RemovePolygon(level, polygon);
            }

            MergeDuplicateLines(level, keptPoint, affectedPolygons);

            if (removedPoint != keptPoint)
            {
                LevelTopology.RemoveEndpoint(level, removedPoint);
            }

            return keptPoint;
        }

        // What's worked out from the polygons' shapes and neighbors, after collapsing: their own data, their lines' and
        // sides' (keeping a line's solidity as it was, unless its heights now close or open it), their points', their
        // platforms' travel, and the level's map indexes
        public static void RecalculateAround(MapLevel level, IEnumerable<polygon_data> polygons)
        {
            var polygonIndexes = polygons.Select(polygon => (short) level.PolygonList.IndexOf(polygon)).Where(index => index >= 0).ToList();
            var lineIndexes = new HashSet<short>();
            var pointIndexes = new HashSet<short>();

            foreach (var polygonIndex in polygonIndexes)
            {
                map_constructors.recalculate_redundant_polygon_data(level, polygonIndex);

                var polygon = level.PolygonList[polygonIndex];
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    lineIndexes.Add(polygon.line_indexes[i]);
                    pointIndexes.Add(polygon.endpoint_indexes[i]);
                }
            }

            foreach (var lineIndex in lineIndexes)
            {
                var line = level.LineList[lineIndex];
                line.length = world.distance2d(level.EndpointList[line.endpoint_indexes[0]].vertex, level.EndpointList[line.endpoint_indexes[1]].vertex);
                LineFlagsEditing.UpdateForHeightChange(level, lineIndex, LineFlagsEditing.IsClosedByHeights(level, lineIndex));

                foreach (var sideIndex in new[] { line.clockwise_polygon_side_index, line.counterclockwise_polygon_side_index })
                {
                    if (sideIndex != cstypes.NONE)
                    {
                        map_constructors.recalculate_redundant_side_data(level, sideIndex, lineIndex);
                        map_constructors.recalculate_side_type(level, sideIndex);
                        LineFlagsEditing.UpdateForSide(level, level.SideList[sideIndex]);
                    }
                }
            }

            foreach (var pointIndex in pointIndexes)
            {
                map_constructors.recalculate_redundant_endpoint_data(level, pointIndex);
            }

            HeightsEditing.RefreshPlatformsAround(level, polygonIndexes);
            LevelEditing.RecalculateMapIndexes(level);
        }

        // Lines between the same two points become one, if their polygons are on opposite sides of it (otherwise, as in
        // overlapping space, they're left as they are)
        private static void MergeDuplicateLines(MapLevel level, endpoint_data point, HashSet<polygon_data> affectedPolygons)
        {
            var pointIndex = (short) level.EndpointList.IndexOf(point);
            var lines = level.LineList.Where(line => line.endpoint_indexes[0] == pointIndex || line.endpoint_indexes[1] == pointIndex).ToList();

            for (var i = 0; i < lines.Count; i++)
            {
                for (var j = i + 1; j < lines.Count; j++)
                {
                    if (lines[i] != null && lines[j] != null && TryMerge(level, lines[i], lines[j], affectedPolygons))
                    {
                        lines[j] = null;
                    }
                }
            }
        }

        private static bool TryMerge(MapLevel level, line_data kept, line_data merged, HashSet<polygon_data> affectedPolygons)
        {
            var isSameDirection = kept.endpoint_indexes[0] == merged.endpoint_indexes[0] && kept.endpoint_indexes[1] == merged.endpoint_indexes[1];
            var isReversed = kept.endpoint_indexes[0] == merged.endpoint_indexes[1] && kept.endpoint_indexes[1] == merged.endpoint_indexes[0];
            if (!isSameDirection && !isReversed)
            {
                return false;
            }

            // The merged line's faces, as faces of the kept line
            var clockwiseOwner = isSameDirection ? merged.clockwise_polygon_owner : merged.counterclockwise_polygon_owner;
            var clockwiseSide = isSameDirection ? merged.clockwise_polygon_side_index : merged.counterclockwise_polygon_side_index;
            var counterclockwiseOwner = isSameDirection ? merged.counterclockwise_polygon_owner : merged.clockwise_polygon_owner;
            var counterclockwiseSide = isSameDirection ? merged.counterclockwise_polygon_side_index : merged.clockwise_polygon_side_index;

            if ((clockwiseOwner != cstypes.NONE && kept.clockwise_polygon_owner != cstypes.NONE) ||
                (counterclockwiseOwner != cstypes.NONE && kept.counterclockwise_polygon_owner != cstypes.NONE))
            {
                return false;
            }

            var keptIndex = (short) level.LineList.IndexOf(kept);
            var mergedIndex = (short) level.LineList.IndexOf(merged);

            if (clockwiseOwner != cstypes.NONE)
            {
                kept.clockwise_polygon_owner = clockwiseOwner;
                kept.clockwise_polygon_side_index = clockwiseSide;
            }

            if (counterclockwiseOwner != cstypes.NONE)
            {
                kept.counterclockwise_polygon_owner = counterclockwiseOwner;
                kept.counterclockwise_polygon_side_index = counterclockwiseSide;
            }

            // What referred to the merged line refers to the kept one, and it goes
            foreach (var polygon in level.PolygonList)
            {
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    if (polygon.line_indexes[i] == mergedIndex)
                    {
                        polygon.line_indexes[i] = keptIndex;
                        affectedPolygons.Add(polygon);
                    }
                }
            }

            foreach (var side in level.SideList)
            {
                if (side.line_index == mergedIndex)
                {
                    side.line_index = keptIndex;
                }
            }

            merged.clockwise_polygon_owner = merged.counterclockwise_polygon_owner = cstypes.NONE;
            merged.clockwise_polygon_side_index = merged.counterclockwise_polygon_side_index = cstypes.NONE;
            LevelTopology.RemoveLine(level, merged);

            keptIndex = (short) level.LineList.IndexOf(kept);

            // Now between two polygons, it's open (or solid and opaque where their heights leave no opening), or follows a
            // platform beside it
            if (kept.clockwise_polygon_owner != cstypes.NONE && kept.counterclockwise_polygon_owner != cstypes.NONE)
            {
                var isClosed = LineFlagsEditing.IsClosedByHeights(level, keptIndex);
                SET_LINE_SOLIDITY(kept, isClosed);
                SET_LINE_TRANSPARENCY(kept, !isClosed);
                SET_LINE_VARIABLE_ELEVATION(kept, false);

                LineFlagsEditing.UpdateForPlatformChange(level, level.PolygonList[kept.clockwise_polygon_owner]);
                LineFlagsEditing.UpdateForPlatformChange(level, level.PolygonList[kept.counterclockwise_polygon_owner]);
            }

            // Faces it no longer shows lose their sides (a full side would be a wall across the opening), and faces it now
            // shows gain them
            foreach (var isClockwise in new[] { true, false })
            {
                var isExposed = LevelEntity_Side.IsFaceExposed(level, keptIndex, isClockwise);
                var sideIndex = kept.GetSideIndex(isClockwise);
                var owner = kept.GetPolygonOwner(isClockwise);

                if (!isExposed && sideIndex != cstypes.NONE)
                {
                    LevelTopology.RemoveSide(level, keptIndex, isClockwise);
                }
                else if (isExposed && sideIndex == cstypes.NONE && owner != cstypes.NONE)
                {
                    map_constructors.new_side(level, owner, keptIndex);
                }
            }

            return true;
        }

        // The corner where the polygon's given line starts, with the line
        private static void RemoveCorner(polygon_data polygon, int corner)
        {
            var count = polygon.vertex_count;

            for (var i = corner; i < count - 1; i++)
            {
                polygon.endpoint_indexes[i] = polygon.endpoint_indexes[i + 1];
                polygon.line_indexes[i] = polygon.line_indexes[i + 1];
                polygon.adjacent_polygon_indexes[i] = polygon.adjacent_polygon_indexes[i + 1];
                polygon.side_indexes[i] = polygon.side_indexes[i + 1];
            }

            polygon.endpoint_indexes[count - 1] = cstypes.NONE;
            polygon.line_indexes[count - 1] = cstypes.NONE;
            polygon.adjacent_polygon_indexes[count - 1] = cstypes.NONE;
            polygon.side_indexes[count - 1] = cstypes.NONE;
            polygon.vertex_count--;
        }

        private static void AddNeighbors(MapLevel level, polygon_data polygon, HashSet<polygon_data> polygons)
        {
            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var neighborIndex = polygon.adjacent_polygon_indexes[i];
                if (neighborIndex >= 0 && neighborIndex < level.PolygonList.Count)
                {
                    polygons.Add(level.PolygonList[neighborIndex]);
                }
            }
        }

        private static bool HasCorner(polygon_data polygon, short pointIndex)
        {
            for (var i = 0; i < polygon.vertex_count; i++)
            {
                if (polygon.endpoint_indexes[i] == pointIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasLine(polygon_data polygon, short lineIndex)
        {
            return IndexOfLine(polygon, lineIndex) >= 0;
        }

        private static int IndexOfLine(polygon_data polygon, short lineIndex)
        {
            for (var i = 0; i < polygon.vertex_count; i++)
            {
                if (polygon.line_indexes[i] == lineIndex)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
#endif
