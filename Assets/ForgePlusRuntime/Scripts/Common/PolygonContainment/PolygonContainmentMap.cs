using AlephOne;
using RuntimeCore.Entities;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.PolygonContainment
{
    // The open level's polygons, laid out for finding which one a point is in: each polygon's points (clockwise, in
    // world units), the box around them, and the heights it spans (a platform polygon's, the lowest its floor and the
    // highest its ceiling reach). A point is in a polygon when it's inside or on all of its edges (point_in_polygon in
    // map.cpp: Marathon polygons are convex), and, with heights, from its floor to its ceiling.
    // Built for the open level the first time it's needed, and again for another level (or after MarkChanged).
    [AutoStaticsCleanup]
    public sealed partial class PolygonContainmentMap : IDisposable
    {
        private const int PolygonsPerBatch = 64;

        private static PolygonContainmentMap current;
        private static int lastVersion;
        private static bool isChanged;

        private readonly short[][] adjacentPolygons;

        private NativeArray<int2> vertices;
        private NativeArray<int2> vertexRanges;
        private NativeArray<int4> bounds;
        private NativeArray<int2> heights;
        private NativeArray<byte> results;

        // The open level's map, or null while no level is open
        public static PolygonContainmentMap Current
        {
            get
            {
                var levelEntity = LevelEntity_Level.Instance;
                var level = levelEntity ? levelEntity.Level : null;

                if (current != null && (current.Level != level || isChanged))
                {
                    current.Dispose();
                    current = null;
                }

                if (current == null && level != null)
                {
                    current = new PolygonContainmentMap(level);

                    // In the Editor this is when Play mode ends
                    Application.quitting -= DisposeCurrent;
                    Application.quitting += DisposeCurrent;
                }

                isChanged = false;

                return current;
            }
        }

        public MapLevel Level { get; }

        // Different for each map built, so whatever found a polygon in an earlier one knows to look again
        public int Version { get; }

        public int PolygonCount { get; }

        private PolygonContainmentMap(MapLevel level)
        {
            Level = level;
            Version = ++lastVersion;
            PolygonCount = level.PolygonList.Count;

            var vertexCount = 0;
            foreach (var polygon in level.PolygonList)
            {
                vertexCount += polygon.vertex_count;
            }

            vertices = new NativeArray<int2>(vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            vertexRanges = new NativeArray<int2>(PolygonCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            bounds = new NativeArray<int4>(PolygonCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            heights = new NativeArray<int2>(PolygonCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            results = new NativeArray<byte>(PolygonCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            adjacentPolygons = new short[PolygonCount][];

            var firstVertex = 0;
            for (var polygonIndex = 0; polygonIndex < PolygonCount; polygonIndex++)
            {
                var polygon = level.PolygonList[polygonIndex];
                var polygonBounds = new int4(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);
                var count = 0;

                // A polygon the game ignores (detached), or too broken to be a shape, contains nothing
                if (!map.POLYGON_IS_DETACHED(polygon) && polygon.vertex_count >= 3 && IsComplete(level, polygon))
                {
                    count = polygon.vertex_count;

                    for (var i = 0; i < count; i++)
                    {
                        var vertex = level.EndpointList[polygon.endpoint_indexes[i]].vertex;
                        var point = new int2(vertex.x, vertex.y);

                        vertices[firstVertex + i] = point;
                        polygonBounds = new int4(math.min(polygonBounds.xy, point), math.max(polygonBounds.zw, point));
                    }
                }

                vertexRanges[polygonIndex] = new int2(firstVertex, count);
                bounds[polygonIndex] = polygonBounds;
                heights[polygonIndex] = HeightRange(level, polygon);

                var adjacent = new short[polygon.vertex_count];
                Array.Copy(polygon.adjacent_polygon_indexes, adjacent, adjacent.Length);
                adjacentPolygons[polygonIndex] = adjacent;

                firstVertex += polygon.vertex_count;
            }
        }

        // For when the level's polygons change shape or height, so the map is built again the next time it's needed
        public static void MarkChanged()
        {
            isChanged = true;
        }

        // Whether the point (in world units) is in the polygon
        public bool Contains(short polygonIndex, int3 point, bool withHeight)
        {
            return polygonIndex >= 0 && polygonIndex < PolygonCount &&
                Contains(vertices, vertexRanges, bounds, heights, polygonIndex, point, withHeight);
        }

        // The polygon next to the given one (across one of its sides) that the point is in, or NONE if it isn't in any
        public short FindAdjacentPolygon(short polygonIndex, int3 point, bool withHeight)
        {
            if (polygonIndex < 0 || polygonIndex >= PolygonCount)
            {
                return cstypes.NONE;
            }

            // On a point several of them share, it's in each of them, so the lowest is the one
            var found = cstypes.NONE;
            foreach (var adjacentIndex in adjacentPolygons[polygonIndex])
            {
                if (adjacentIndex != cstypes.NONE &&
                    (found == cstypes.NONE || adjacentIndex < found) &&
                    Contains(adjacentIndex, point, withHeight))
                {
                    found = adjacentIndex;
                }
            }

            return found;
        }

        // The polygon the point is in, testing all of them at once (on worker threads), or NONE if it isn't in any.
        // Where polygons overlap, it's the lowest index (as in world_point_to_polygon_index, in map.cpp).
        public short FindPolygon(int3 point, bool withHeight)
        {
            if (PolygonCount == 0)
            {
                return cstypes.NONE;
            }

            new ContainsJob
            {
                Vertices = vertices,
                VertexRanges = vertexRanges,
                Bounds = bounds,
                Heights = heights,
                Point = point,
                WithHeight = withHeight,
                Results = results,
            }.Schedule(PolygonCount, PolygonsPerBatch).Complete();

            for (short polygonIndex = 0; polygonIndex < PolygonCount; polygonIndex++)
            {
                if (results[polygonIndex] != 0)
                {
                    return polygonIndex;
                }
            }

            return cstypes.NONE;
        }

        public void Dispose()
        {
            if (vertices.IsCreated)
            {
                vertices.Dispose();
                vertexRanges.Dispose();
                bounds.Dispose();
                heights.Dispose();
                results.Dispose();
            }
        }

        private static void DisposeCurrent()
        {
            Application.quitting -= DisposeCurrent;

            current?.Dispose();
            current = null;
        }

        private static bool IsComplete(MapLevel level, polygon_data polygon)
        {
            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var endpointIndex = polygon.endpoint_indexes[i];
                if (endpointIndex < 0 || endpointIndex >= level.EndpointList.Count)
                {
                    return false;
                }
            }

            return true;
        }

        // From the lowest floor to the highest ceiling it has (a platform's, as far as it moves)
        private static int2 HeightRange(MapLevel level, polygon_data polygon)
        {
            var floor = (int) polygon.floor_height;
            var ceiling = (int) polygon.ceiling_height;

            if (polygon.type == map._polygon_is_platform)
            {
                var platform = platforms.get_platform_data(level, polygon.permutation);
                if (platform != null)
                {
                    floor = math.min(floor, platform.minimum_floor_height);
                    ceiling = math.max(ceiling, platform.maximum_ceiling_height);
                }
            }

            return new int2(floor, ceiling);
        }

        // Inside or on each edge: for each edge e0 to e1 (clockwise), the point is outside it when e0p cross e0e1 is
        // positive (as in point_in_polygon, in map.cpp, with no overflow)
        private static bool Contains(
            NativeArray<int2> vertices,
            NativeArray<int2> vertexRanges,
            NativeArray<int4> bounds,
            NativeArray<int2> heights,
            int polygonIndex,
            int3 point,
            bool withHeight)
        {
            var polygonBounds = bounds[polygonIndex];
            if (point.x < polygonBounds.x || point.y < polygonBounds.y || point.x > polygonBounds.z || point.y > polygonBounds.w)
            {
                return false;
            }

            if (withHeight)
            {
                var range = heights[polygonIndex];
                if (point.z < range.x || point.z > range.y)
                {
                    return false;
                }
            }

            var vertexRange = vertexRanges[polygonIndex];
            var first = vertexRange.x;
            var count = vertexRange.y;

            for (var i = 0; i < count; i++)
            {
                var e0 = vertices[first + i];
                var e1 = vertices[first + (i == count - 1 ? 0 : i + 1)];

                var crossProduct = (long) (point.x - e0.x) * (e1.y - e0.y) - (long) (point.y - e0.y) * (e1.x - e0.x);
                if (crossProduct > 0)
                {
                    return false;
                }
            }

            return count > 0;
        }

        [BurstCompile]
        private struct ContainsJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<int2> Vertices;

            [ReadOnly]
            public NativeArray<int2> VertexRanges;

            [ReadOnly]
            public NativeArray<int4> Bounds;

            [ReadOnly]
            public NativeArray<int2> Heights;

            public int3 Point;
            public bool WithHeight;

            [WriteOnly]
            public NativeArray<byte> Results;

            public void Execute(int polygonIndex)
            {
                Results[polygonIndex] = Contains(Vertices, VertexRanges, Bounds, Heights, polygonIndex, Point, WithHeight) ? (byte) 1 : (byte) 0;
            }
        }
    }
}
