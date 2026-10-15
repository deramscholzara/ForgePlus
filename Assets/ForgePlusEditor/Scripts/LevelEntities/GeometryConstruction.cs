#if !NO_EDITING
using AlephOne;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Steps shared by the tools that add geometry (LineExtrusion, LineDrawing): adding points and lines to the level's
    // data, and telling whether a new line would cross or touch what's already there. Locations are in world units.
    public static class GeometryConstruction
    {
        public static short AddEndpoint(MapLevel data, world_point2d location)
        {
            data.EndpointList.Add(new endpoint_data
            {
                vertex = location,
                supporting_polygon_index = cstypes.NONE,
            });

            return (short) (data.EndpointList.Count - 1);
        }

        // Between the points, with the polygons (or NONE) on its sides, and no sides yet
        public static short AddLine(MapLevel data, short firstIndex, short lastIndex, short clockwiseOwner, short counterclockwiseOwner)
        {
            var line = new line_data
            {
                clockwise_polygon_owner = clockwiseOwner,
                counterclockwise_polygon_owner = counterclockwiseOwner,
                clockwise_polygon_side_index = cstypes.NONE,
                counterclockwise_polygon_side_index = cstypes.NONE,
            };

            line.endpoint_indexes[0] = firstIndex;
            line.endpoint_indexes[1] = lastIndex;
            line.length = world.distance2d(data.EndpointList[firstIndex].vertex, data.EndpointList[lastIndex].vertex);

            data.LineList.Add(line);

            return (short) (data.LineList.Count - 1);
        }

        // Whether a new line from start to end would cross or touch the level's lines (other than the ignored one). Its
        // ends may be existing points (their indexes, or NONE): lines that meet it there only count if they run along it,
        // and a line already joining both counts.
        public static bool SegmentTouchesLines(MapLevel data, Vector2Int start, Vector2Int end, short startIndex, short endIndex, short ignoredLineIndex = cstypes.NONE)
        {
            for (short lineIndex = 0; lineIndex < data.LineList.Count; lineIndex++)
            {
                if (lineIndex == ignoredLineIndex)
                {
                    continue;
                }

                var line = data.LineList[lineIndex];
                var p = ToVector(data.EndpointList[line.endpoint_indexes[0]].vertex);
                var q = ToVector(data.EndpointList[line.endpoint_indexes[1]].vertex);

                var sharesStart = startIndex != cstypes.NONE && (line.endpoint_indexes[0] == startIndex || line.endpoint_indexes[1] == startIndex);
                var sharesEnd = endIndex != cstypes.NONE && (line.endpoint_indexes[0] == endIndex || line.endpoint_indexes[1] == endIndex);

                if (sharesStart && sharesEnd)
                {
                    return true;
                }

                if (sharesStart || sharesEnd)
                {
                    // Meeting at a point, they only overlap if they run along each other
                    var sharedIndex = sharesStart ? startIndex : endIndex;
                    var lineFar = line.endpoint_indexes[0] == sharedIndex ? q : p;
                    var segmentFar = sharesStart ? end : start;

                    if (IsOnSegment(start, end, lineFar) || IsOnSegment(p, q, segmentFar))
                    {
                        return true;
                    }
                }
                else if (SegmentsTouch(start, end, p, q))
                {
                    return true;
                }
            }

            return false;
        }

        // Whether any point (other than its ends) is on the segment, such as one no line reaches
        public static bool SegmentTouchesPoints(MapLevel data, Vector2Int start, Vector2Int end, short startIndex, short endIndex)
        {
            for (short pointIndex = 0; pointIndex < data.EndpointList.Count; pointIndex++)
            {
                if (pointIndex != startIndex && pointIndex != endIndex && IsOnSegment(start, end, ToVector(data.EndpointList[pointIndex].vertex)))
                {
                    return true;
                }
            }

            return false;
        }

        // Twice the area its corners enclose, positive for corners that go clockwise as the map shows them (its Y down)
        public static long SignedArea(MapLevel data, polygon_data polygon)
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

        // Strictly inside the (convex) polygon, not on its edges
        public static bool IsInsidePolygon(MapLevel data, polygon_data polygon, Vector2 location)
        {
            var orientation = Mathf.Sign(SignedArea(data, polygon));
            if (orientation == 0f || polygon.vertex_count < 3)
            {
                return false;
            }

            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var current = data.EndpointList[polygon.endpoint_indexes[i]].vertex;
                var next = data.EndpointList[polygon.endpoint_indexes[(i + 1) % polygon.vertex_count]].vertex;
                var cross = ((double) next.x - current.x) * (location.y - current.y) - ((double) next.y - current.y) * (location.x - current.x);

                if (cross * orientation <= 0d)
                {
                    return false;
                }
            }

            return true;
        }

        public static Vector2Int ToVector(world_point2d point)
        {
            return new Vector2Int(point.x, point.y);
        }

        public static long Cross(Vector2Int origin, Vector2Int a, Vector2Int b)
        {
            return (long) (a.x - origin.x) * (b.y - origin.y) - (long) (a.y - origin.y) * (b.x - origin.x);
        }

        // On the segment, including its ends
        public static bool IsOnSegment(Vector2Int start, Vector2Int end, Vector2Int point)
        {
            return Cross(start, end, point) == 0 &&
                   point.x >= Mathf.Min(start.x, end.x) && point.x <= Mathf.Max(start.x, end.x) &&
                   point.y >= Mathf.Min(start.y, end.y) && point.y <= Mathf.Max(start.y, end.y);
        }

        // Crossing or touching
        public static bool SegmentsTouch(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int d)
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
