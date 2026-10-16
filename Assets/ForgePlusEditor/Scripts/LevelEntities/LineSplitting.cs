#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.History;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.PolygonContainment;
using RuntimeCore.Entities;
using System.Collections.Generic;
using UnityEngine;
using static AlephOne.map;

namespace ForgePlus.LevelManipulation
{
    // Double-clicking a line (in Geometry mode) adds a point on it where it's clicked, splitting it in two: the new line
    // (from the point to the line's last point) has the line's flags, and copies of its sides, and each polygon along it
    // gains the point as a corner (so it can't if a polygon along it already has Marathon's limit of eight). Textures stay
    // where they are in the world (each side's offset along the line follows where its part starts). The point snaps to
    // sixteenths of a world unit along the line while snapping is on. The level's structure changes, so it's rebuilt,
    // with the new point selected (as part of the same action).
    public static class LineSplitting
    {
        // Returns whether the point was added
        public static bool TrySplit(LevelEntity_Level level, short lineIndex, Vector3 worldPosition)
        {
            if (!level || LevelEditing.IsRebuildPending)
            {
                return false;
            }

            var data = level.Level;
            var line = data.LineList[lineIndex];
            var owners = new[] { line.clockwise_polygon_owner, line.counterclockwise_polygon_owner };

            foreach (var owner in owners)
            {
                if (owner != cstypes.NONE && data.PolygonList[owner].vertex_count >= MAXIMUM_VERTICES_PER_POLYGON)
                {
                    return false;
                }
            }

            var firstIndex = line.endpoint_indexes[0];
            var lastIndex = line.endpoint_indexes[1];
            var first = data.EndpointList[firstIndex].vertex;
            var last = data.EndpointList[lastIndex].vertex;

            if (!TryFindPoint(first, last, worldPosition, out var location))
            {
                return false;
            }

            var pointIndex = GeometryConstruction.AddEndpoint(data, location);

            // The line from its first point to the new one, and a new one from there to its last
            var newLineIndex = GeometryConstruction.AddLine(data, pointIndex, lastIndex, line.clockwise_polygon_owner, line.counterclockwise_polygon_owner);
            var newLine = data.LineList[newLineIndex];
            newLine.flags = line.flags;
            line.endpoint_indexes[1] = pointIndex;

            // A clockwise side runs from the first point (so the new line's part starts at the new point), and a
            // counterclockwise one from the last (so the line's own part now starts at the new point)
            var fromFirst = world.distance2d(first, location);
            var fromLast = world.distance2d(last, location);

            foreach (var isClockwise in new[] { true, false })
            {
                var sideIndex = line.GetSideIndex(isClockwise);
                if (sideIndex == cstypes.NONE)
                {
                    continue;
                }

                var side = data.SideList[sideIndex];
                var copy = side.Clone();
                copy.line_index = newLineIndex;

                ShiftTextures(isClockwise ? copy : side, isClockwise ? fromFirst : fromLast);

                var copyIndex = (short) data.SideList.Count;
                data.SideList.Add(copy);
                newLine.SetSideIndex(isClockwise, copyIndex);
            }

            // Each polygon along it gains the point as a corner between the two lines
            var polygons = new List<polygon_data>();
            foreach (var isClockwise in new[] { true, false })
            {
                var owner = line.GetPolygonOwner(isClockwise);
                if (owner != cstypes.NONE)
                {
                    InsertCorner(data.PolygonList[owner], lineIndex, newLineIndex, pointIndex, isClockwise);
                    polygons.Add(data.PolygonList[owner]);
                }
            }

            // What's worked out from them (but not their sides' types, which their heights leave as they were)
            foreach (var polygon in polygons)
            {
                map_constructors.recalculate_redundant_polygon_data(data, (short) data.PolygonList.IndexOf(polygon));
            }

            map_constructors.recalculate_redundant_line_data(data, lineIndex);
            map_constructors.recalculate_redundant_line_data(data, newLineIndex);

            foreach (var endpointIndex in new[] { firstIndex, lastIndex, pointIndex })
            {
                map_constructors.recalculate_redundant_endpoint_data(data, endpointIndex);
            }

            LevelEditing.RecalculateMapIndexes(data);

            // Until the level is rebuilt, which works these out for its entities
            level.EndpointLines = data.BuildEndpointLines();
            PolygonContainmentMap.MarkChanged();

            var point = data.EndpointList[pointIndex];
            LevelEditing.RebuildLevel(() =>
            {
                var rebuiltLevel = LevelEntity_Level.Instance;
                if (!rebuiltLevel || rebuiltLevel.Points == null)
                {
                    return;
                }

                var index = (short) rebuiltLevel.Level.EndpointList.IndexOf(point);
                if (rebuiltLevel.Points.TryGetValue(index, out var added))
                {
                    // Undone and redone with the point (the edit, which the rebuild has recorded)
                    SelectionHistory.JoinNextToLast();
                    SelectionManager.Instance.SelectObject(added, multiSelect: false);
                }
            });

            return true;
        }

        // Where on the line (between its points, and not on either) it's clicked, snapped along it
        private static bool TryFindPoint(world_point2d first, world_point2d last, Vector3 worldPosition, out world_point2d location)
        {
            location = default;

            var start = new Vector2(first.x, first.y);
            var along = new Vector2(last.x, last.y) - start;
            var length = along.magnitude;
            if (length < 2f)
            {
                return false;
            }

            var clicked = new Vector2(worldPosition.x, -worldPosition.z) * GeometryUtilities.WorldUnitIncrementsPerMeter;
            var distance = Mathf.Clamp(Vector2.Dot(clicked - start, along) / length, 0f, length);

            if (AxisLocks.Instance.SnapToGrid)
            {
                distance = Mathf.Round(distance / HeightsEditing.SnapIncrement) * HeightsEditing.SnapIncrement;
            }

            var position = start + along * (distance / length);
            location = new world_point2d((short) Mathf.RoundToInt(position.x), (short) Mathf.RoundToInt(position.y));

            return (location.x != first.x || location.y != first.y) && (location.x != last.x || location.y != last.y);
        }

        // Its textures continue from where its part of the line now starts, the distance further along
        private static void ShiftTextures(side_data side, int distance)
        {
            foreach (var texture in new[] { side.primary_texture, side.secondary_texture, side.transparent_texture })
            {
                var x0 = texture.x0 + distance;
                texture.x0 = (short) (x0 < short.MinValue || x0 > short.MaxValue ? x0 % world.WORLD_ONE : x0);
            }
        }

        // The polygon goes along the line from its first point to its last (as its clockwise owner), or the other way, and
        // gains the point between them, with the line and the new one on either side of it in its order
        private static void InsertCorner(polygon_data polygon, short lineIndex, short newLineIndex, short pointIndex, bool isClockwiseOwner)
        {
            var count = polygon.vertex_count;
            var edge = System.Array.IndexOf(polygon.line_indexes, lineIndex, 0, count);
            if (edge < 0)
            {
                return;
            }

            for (var i = count; i > edge + 1; i--)
            {
                polygon.endpoint_indexes[i] = polygon.endpoint_indexes[i - 1];
                polygon.line_indexes[i] = polygon.line_indexes[i - 1];
                polygon.adjacent_polygon_indexes[i] = polygon.adjacent_polygon_indexes[i - 1];
                polygon.side_indexes[i] = polygon.side_indexes[i - 1];
            }

            polygon.endpoint_indexes[edge + 1] = pointIndex;
            polygon.line_indexes[edge] = isClockwiseOwner ? lineIndex : newLineIndex;
            polygon.line_indexes[edge + 1] = isClockwiseOwner ? newLineIndex : lineIndex;
            polygon.vertex_count = (ushort) (count + 1);
        }
    }
}
#endif
