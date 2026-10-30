using AlephOne;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ForgePlus.PolygonContainment
{
    // Which polygons can be seen from a point in one of them, as Aleph One's render visibility tree finds the polygons it
    // draws (RenderVisTree.cpp: build_render_tree, cast_render_ray, next_polygon_along_line, decide_where_vertex_leads),
    // but in every direction (with no view cone): rays are cast from the point at every endpoint of each polygon reached,
    // crossing from polygon to polygon through transparent lines, until they meet one that isn't. Only which polygons
    // are reached is kept (not the clipping the engine draws them with).
    public sealed partial class PolygonContainmentMap
    {
        // Ray biases (RenderVisTree.cpp): split at an endpoint the ray passes exactly through, or pass it on its
        // clockwise or counterclockwise side
        private const int NoBias = 0;
        private const int ClockwiseBias = 1;
        private const int CounterclockwiseBias = 2;

        // For each polygon vertex (in step with vertices): its endpoint, the polygon across the line from it to the next
        // vertex, and that line
        private NativeArray<int> vertexEndpoints;
        private NativeArray<short> vertexAdjacentPolygons;
        private NativeArray<short> vertexLines;

        // For each line, whether it can be seen through (LINE_IS_TRANSPARENT)
        private NativeArray<byte> transparentLines;

        private NativeArray<byte> visiblePolygons;
        private int endpointCount;

        // The lines beside platforms (variable elevation), and the polygon on each side of each, whose transparency
        // follows the heights the platforms are at as they're simulated (ApplyPlatformHeights)
        private short[] variableElevationLines;
        private short[] variableElevationLinePolygons;

        // Lines beside platforms are seen through only while their polygons' floors and ceilings (with each platform where
        // its simulation has it) leave a gap between them, as Aleph One sets them as platforms move
        // (adjust_platform_endpoint_and_line_heights, in platforms.cpp). Whether any changed since last applied.
        public bool ApplyPlatformHeights(LevelEntity_Level levelEntity)
        {
            if (variableElevationLines.Length == 0 || !levelEntity || levelEntity.Level != Level)
            {
                return false;
            }

            var isChanged = false;
            for (var i = 0; i < variableElevationLines.Length; i++)
            {
                var first = levelEntity.LiveHeights(variableElevationLinePolygons[i * 2]);
                var second = levelEntity.LiveHeights(variableElevationLinePolygons[i * 2 + 1]);
                var transparent = math.max(first.x, second.x) < math.min(first.y, second.y) ? (byte) 1 : (byte) 0;

                var lineIndex = variableElevationLines[i];
                if (transparentLines[lineIndex] != transparent)
                {
                    transparentLines[lineIndex] = transparent;
                    isChanged = true;
                }
            }

            return isChanged;
        }

        // Whether each polygon can be seen from the point (given in world units), which is in the origin polygon. Run on a
        // worker thread (with Burst), and waited for.
        public void FindVisiblePolygons(short originPolygonIndex, int2 point, bool[] visible)
        {
            for (var polygonIndex = 0; polygonIndex < visible.Length; polygonIndex++)
            {
                visible[polygonIndex] = false;
            }

            if (originPolygonIndex < 0 || originPolygonIndex >= PolygonCount)
            {
                return;
            }

            new VisibilityJob
            {
                Vertices = vertices,
                VertexRanges = vertexRanges,
                VertexEndpoints = vertexEndpoints,
                VertexAdjacentPolygons = vertexAdjacentPolygons,
                VertexLines = vertexLines,
                TransparentLines = transparentLines,
                EndpointCount = endpointCount,
                OriginPolygon = originPolygonIndex,
                Origin = point,
                Visible = visiblePolygons,
            }.Schedule().Complete();

            var count = math.min(visible.Length, PolygonCount);
            for (var polygonIndex = 0; polygonIndex < count; polygonIndex++)
            {
                visible[polygonIndex] = visiblePolygons[polygonIndex] != 0;
            }
        }

        private void BuildVisibilityData(MapLevel level)
        {
            var vertexCount = vertices.Length;
            endpointCount = level.EndpointList.Count;

            vertexEndpoints = new NativeArray<int>(vertexCount, Allocator.Persistent);
            vertexAdjacentPolygons = new NativeArray<short>(vertexCount, Allocator.Persistent);
            vertexLines = new NativeArray<short>(vertexCount, Allocator.Persistent);
            transparentLines = new NativeArray<byte>(level.LineList.Count, Allocator.Persistent);
            visiblePolygons = new NativeArray<byte>(PolygonCount, Allocator.Persistent);

            for (var polygonIndex = 0; polygonIndex < PolygonCount; polygonIndex++)
            {
                var polygon = level.PolygonList[polygonIndex];
                var range = vertexRanges[polygonIndex];

                // Polygons that contain nothing (vertexRanges) are reached, but not crossed
                for (var i = 0; i < range.y; i++)
                {
                    vertexEndpoints[range.x + i] = polygon.endpoint_indexes[i];
                    vertexAdjacentPolygons[range.x + i] = ValidPolygonOrNone(polygon.adjacent_polygon_indexes[i]);
                    vertexLines[range.x + i] = polygon.line_indexes[i] >= 0 && polygon.line_indexes[i] < level.LineList.Count ?
                        polygon.line_indexes[i] :
                        cstypes.NONE;
                }
            }

            var variableLines = new System.Collections.Generic.List<short>();
            var variableLinePolygons = new System.Collections.Generic.List<short>();

            for (var lineIndex = 0; lineIndex < level.LineList.Count; lineIndex++)
            {
                var line = level.LineList[lineIndex];
                transparentLines[lineIndex] = map.LINE_IS_TRANSPARENT(line) ? (byte) 1 : (byte) 0;

                var clockwisePolygon = ValidPolygonOrNone(line.clockwise_polygon_owner);
                var counterclockwisePolygon = ValidPolygonOrNone(line.counterclockwise_polygon_owner);
                if (map.LINE_IS_VARIABLE_ELEVATION(line) && clockwisePolygon != cstypes.NONE && counterclockwisePolygon != cstypes.NONE)
                {
                    variableLines.Add((short) lineIndex);
                    variableLinePolygons.Add(clockwisePolygon);
                    variableLinePolygons.Add(counterclockwisePolygon);
                }
            }

            variableElevationLines = variableLines.ToArray();
            variableElevationLinePolygons = variableLinePolygons.ToArray();
        }

        private short ValidPolygonOrNone(short polygonIndex)
        {
            return polygonIndex >= 0 && polygonIndex < PolygonCount ? polygonIndex : cstypes.NONE;
        }

        private void DisposeVisibilityData()
        {
            if (vertexEndpoints.IsCreated)
            {
                vertexEndpoints.Dispose();
                vertexAdjacentPolygons.Dispose();
                vertexLines.Dispose();
                transparentLines.Dispose();
                visiblePolygons.Dispose();
            }
        }

        [BurstCompile]
        private struct VisibilityJob : IJob
        {
            // next_polygon_along_line's search states
            private const int LookingForFirstNonzeroVertex = 0;
            private const int LookingClockwiseForRightVertex = 1;
            private const int LookingCounterclockwiseForLeftVertex = 2;
            private const int LookingForNextNonzeroVertex = 3;
            private const int Done = 4;

            [ReadOnly]
            public NativeArray<int2> Vertices;

            [ReadOnly]
            public NativeArray<int2> VertexRanges;

            [ReadOnly]
            public NativeArray<int> VertexEndpoints;

            [ReadOnly]
            public NativeArray<short> VertexAdjacentPolygons;

            [ReadOnly]
            public NativeArray<short> VertexLines;

            [ReadOnly]
            public NativeArray<byte> TransparentLines;

            public int EndpointCount;
            public int OriginPolygon;
            public int2 Origin;

            public NativeArray<byte> Visible;


            public void Execute()
            {
                for (var i = 0; i < Visible.Length; i++)
                {
                    Visible[i] = 0;
                }

                var visitedEndpoints = new NativeArray<byte>(EndpointCount, Allocator.Temp);
                var queue = new NativeList<int>(64, Allocator.Temp);
                var rays = new NativeList<int2>(8, Allocator.Temp);

                // A ray that doesn't come back out of polygons drawn wrongly (such as concave ones) goes no further
                var maximumSteps = Visible.Length * 2 + 8;

                Push(OriginPolygon, ref queue);

                // Pull polygons off the queue, firing at each of their endpoints not fired at yet
                while (queue.Length > 0)
                {
                    var polygonIndex = queue[queue.Length - 1];
                    queue.RemoveAt(queue.Length - 1);

                    var range = VertexRanges[polygonIndex];
                    for (var i = 0; i < range.y; i++)
                    {
                        var endpointIndex = VertexEndpoints[range.x + i];
                        if (endpointIndex < 0 || endpointIndex >= EndpointCount || visitedEndpoints[endpointIndex] != 0)
                        {
                            continue;
                        }

                        visitedEndpoints[endpointIndex] = 1;

                        var vector = Vertices[range.x + i] - Origin;
                        if (vector.x != 0 || vector.y != 0)
                        {
                            CastRay(vector, maximumSteps, ref queue, ref rays);
                        }
                    }
                }
            }

            // From the origin polygon along the vector, through each polygon it passes into, splitting in two at any
            // endpoint it passes exactly through (cast_render_ray)
            private void CastRay(int2 vector, int maximumSteps, ref NativeList<int> queue, ref NativeList<int2> rays)
            {
                rays.Clear();
                rays.Add(new int2(OriginPolygon, NoBias));

                while (rays.Length > 0)
                {
                    var ray = rays[rays.Length - 1];
                    rays.RemoveAt(rays.Length - 1);

                    var polygonIndex = ray.x;
                    for (var step = 0; step < maximumSteps; step++)
                    {
                        var nextPolygonIndex = NextPolygonAlongLine(polygonIndex, vector, ray.y, ref queue, out var split);
                        if (nextPolygonIndex == cstypes.NONE)
                        {
                            if (split)
                            {
                                rays.Add(new int2(polygonIndex, ClockwiseBias));
                                rays.Add(new int2(polygonIndex, CounterclockwiseBias));
                            }

                            break;
                        }

                        polygonIndex = nextPolygonIndex;
                    }
                }
            }

            // The polygon the ray (from the origin) leaves this one into, or NONE where it meets a line it can't see
            // through, or splits (next_polygon_along_line, with decide_where_vertex_leads)
            private int NextPolygonAlongLine(int polygonIndex, int2 vector, int bias, ref NativeList<int> queue, out bool split)
            {
                split = false;
                Push(polygonIndex, ref queue);

                var range = VertexRanges[polygonIndex];
                var first = range.x;
                var count = range.y;
                if (count < 3)
                {
                    return cstypes.NONE;
                }

                int nextPolygonIndex = cstypes.NONE;
                int crossedLineIndex = cstypes.NONE;

                // Searching clockwise from vertex zero; the vertex where the search last changed state is where it stops,
                // if it comes back round to it
                var state = LookingForFirstNonzeroVertex;
                var vertexIndex = 0;
                var vertexDelta = 1;
                var initialVertexIndex = 0;
                var changedState = true;

                do
                {
                    if (changedState)
                    {
                        changedState = false;
                    }
                    else if (vertexIndex == initialVertexIndex)
                    {
                        nextPolygonIndex = cstypes.NONE;
                        crossedLineIndex = cstypes.NONE;
                        break;
                    }

                    var vertex = Vertices[first + vertexIndex];
                    var crossProduct = (long) (vertex.x - Origin.x) * vector.y - (long) (vertex.y - Origin.y) * vector.x;

                    if (crossProduct < 0)
                    {
                        switch (state)
                        {
                            case LookingForFirstNonzeroVertex:
                                // Search counterclockwise for the transition (right to left)
                                state = LookingCounterclockwiseForLeftVertex;
                                vertexDelta = -1;
                                initialVertexIndex = vertexIndex;
                                changedState = true;
                                break;
                            case LookingClockwiseForRightVertex:
                            {
                                var edgeIndex = WrapLow(vertexIndex, count);
                                nextPolygonIndex = VertexAdjacentPolygons[first + edgeIndex];
                                crossedLineIndex = VertexLines[first + edgeIndex];
                                state = Done;
                                break;
                            }
                            case LookingForNextNonzeroVertex:
                                state = Done;
                                break;
                        }
                    }
                    else if (crossProduct > 0)
                    {
                        switch (state)
                        {
                            case LookingForFirstNonzeroVertex:
                                // Search clockwise for the transition (left to right)
                                state = LookingClockwiseForRightVertex;
                                initialVertexIndex = vertexIndex;
                                changedState = true;
                                break;
                            case LookingCounterclockwiseForLeftVertex:
                                nextPolygonIndex = VertexAdjacentPolygons[first + vertexIndex];
                                crossedLineIndex = VertexLines[first + vertexIndex];
                                state = Done;
                                break;
                            case LookingForNextNonzeroVertex:
                                state = Done;
                                break;
                        }
                    }
                    else if (state == LookingClockwiseForRightVertex || state == LookingCounterclockwiseForLeftVertex)
                    {
                        // The ray passes exactly through this vertex: split there, or leave along the line on the bias's
                        // side of it (decide_where_vertex_leads)
                        if (bias == NoBias)
                        {
                            split = true;
                            nextPolygonIndex = cstypes.NONE;
                            crossedLineIndex = cstypes.NONE;
                        }
                        else
                        {
                            var edgeIndex = bias == ClockwiseBias ? vertexIndex : WrapLow(vertexIndex, count);
                            nextPolygonIndex = VertexAdjacentPolygons[first + edgeIndex];
                            crossedLineIndex = VertexLines[first + edgeIndex];
                        }

                        state = LookingForNextNonzeroVertex;
                        initialVertexIndex = vertexIndex;
                        changedState = true;
                    }

                    vertexIndex = vertexDelta < 0 ? WrapLow(vertexIndex, count) : WrapHigh(vertexIndex, count);
                }
                while (state != Done);

                // Only through a line that can be seen through, into a polygon on its other side
                if (crossedLineIndex == cstypes.NONE || TransparentLines[crossedLineIndex] == 0)
                {
                    return cstypes.NONE;
                }

                return nextPolygonIndex;
            }

            // Reached (once each)
            private void Push(int polygonIndex, ref NativeList<int> queue)
            {
                if (Visible[polygonIndex] == 0)
                {
                    Visible[polygonIndex] = 1;
                    queue.Add(polygonIndex);
                }
            }

            private static int WrapLow(int index, int count)
            {
                return index == 0 ? count - 1 : index - 1;
            }

            private static int WrapHigh(int index, int count)
            {
                return index == count - 1 ? 0 : index + 1;
            }
        }
    }
}
