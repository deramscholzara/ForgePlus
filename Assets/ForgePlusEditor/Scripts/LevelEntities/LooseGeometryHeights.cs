#if !NO_EDITING
using AlephOne;
using System.Collections.Generic;

namespace ForgePlus.LevelManipulation
{
    // Points and lines that aren't part of any polygon have no height of their own, so they're shown (and selected) at
    // one: the lowest floor of the polygons their chain of such lines reaches (at the corners those lines meet), or 0
    // where it reaches none. Heights are in world units.
    public static class LooseGeometryHeights
    {
        // Each point no polygon has as a corner, and each line with no polygon on either side, and its height
        public static void Find(MapLevel data, Dictionary<short, short> pointHeights, Dictionary<short, short> lineHeights)
        {
            pointHeights.Clear();
            lineHeights.Clear();

            var pointCount = data.EndpointList.Count;

            // The lowest floor at each point that's a polygon's corner
            var lowestFloors = new short?[pointCount];
            foreach (var polygon in data.PolygonList)
            {
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    var pointIndex = polygon.endpoint_indexes[i];
                    if (pointIndex < 0 || pointIndex >= pointCount)
                    {
                        continue;
                    }

                    var lowest = lowestFloors[pointIndex];
                    if (!lowest.HasValue || polygon.floor_height < lowest.Value)
                    {
                        lowestFloors[pointIndex] = polygon.floor_height;
                    }
                }
            }

            // Chains: the points each loose line joins
            var roots = new int[pointCount];
            for (var i = 0; i < pointCount; i++)
            {
                roots[i] = i;
            }

            int Root(int pointIndex)
            {
                while (roots[pointIndex] != pointIndex)
                {
                    roots[pointIndex] = roots[roots[pointIndex]];
                    pointIndex = roots[pointIndex];
                }

                return pointIndex;
            }

            foreach (var line in data.LineList)
            {
                if (IsLoose(line) && IsPoint(line.endpoint_indexes[0], pointCount) && IsPoint(line.endpoint_indexes[1], pointCount))
                {
                    roots[Root(line.endpoint_indexes[0])] = Root(line.endpoint_indexes[1]);
                }
            }

            // Each chain's lowest floor, where it reaches a polygon
            var chainFloors = new Dictionary<int, short>();
            for (var pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                var floor = lowestFloors[pointIndex];
                if (!floor.HasValue)
                {
                    continue;
                }

                var root = Root(pointIndex);
                if (!chainFloors.TryGetValue(root, out var chainFloor) || floor.Value < chainFloor)
                {
                    chainFloors[root] = floor.Value;
                }
            }

            short ChainHeight(int pointIndex)
            {
                return chainFloors.TryGetValue(Root(pointIndex), out var height) ? height : (short) 0;
            }

            for (short pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                if (!lowestFloors[pointIndex].HasValue)
                {
                    pointHeights[pointIndex] = ChainHeight(pointIndex);
                }
            }

            for (short lineIndex = 0; lineIndex < data.LineList.Count; lineIndex++)
            {
                var line = data.LineList[lineIndex];
                if (IsLoose(line) && IsPoint(line.endpoint_indexes[0], pointCount))
                {
                    lineHeights[lineIndex] = ChainHeight(line.endpoint_indexes[0]);
                }
            }
        }

        // The height of one point or line (NONE if it's part of a polygon). For many, Find is far quicker.
        public static bool TryGetPointHeight(MapLevel data, short pointIndex, out short height)
        {
            var points = new Dictionary<short, short>();
            Find(data, points, new Dictionary<short, short>());

            return points.TryGetValue(pointIndex, out height);
        }

        public static bool TryGetLineHeight(MapLevel data, short lineIndex, out short height)
        {
            var lines = new Dictionary<short, short>();
            Find(data, new Dictionary<short, short>(), lines);

            return lines.TryGetValue(lineIndex, out height);
        }

        private static bool IsLoose(line_data line)
        {
            return line.clockwise_polygon_owner == cstypes.NONE && line.counterclockwise_polygon_owner == cstypes.NONE;
        }

        private static bool IsPoint(short pointIndex, int pointCount)
        {
            return pointIndex >= 0 && pointIndex < pointCount;
        }
    }
}
#endif
