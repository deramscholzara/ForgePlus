#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.UI;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using static AlephOne.map;

namespace ForgePlus.LevelManipulation
{
    // Removes level data without leaving gaps: the last of a list takes the removed one's index, and whatever referred to
    // it by that index refers to its new one (so as little as possible is renumbered, for scripts and terminals that refer
    // to things by number). Removing changes the level's structure, which is rebuilt afterward.
    public static class LevelTopology
    {
        // The side on a line's face
        public static void RemoveSide(MapLevel level, short lineIndex, bool isClockwise, HashSet<short> changedLines = null)
        {
            var line = level.LineList[lineIndex];
            var sideIndex = line.GetSideIndex(isClockwise);
            if (sideIndex == cstypes.NONE)
            {
                return;
            }

            var lastIndex = (short) (level.SideList.Count - 1);

            line.SetSideIndex(isClockwise, cstypes.NONE);
            RecalculatePolygonSides(level, line.GetPolygonOwner(isClockwise));
            changedLines?.Add(lineIndex);

            if (sideIndex != lastIndex)
            {
                level.SideList[sideIndex] = level.SideList[lastIndex];

                // A side on no line has nothing referring to it
                if (TryFindLineWithSide(level, lastIndex, out var movedLineIndex, out var movedIsClockwise))
                {
                    var movedLine = level.LineList[movedLineIndex];

                    movedLine.SetSideIndex(movedIsClockwise, sideIndex);
                    RecalculatePolygonSides(level, movedLine.GetPolygonOwner(movedIsClockwise));
                    changedLines?.Add(movedLineIndex);
                }
            }

            level.SideList.RemoveAt(lastIndex);
        }

        // With its sides. Polygons no longer list it by then.
        public static void RemoveLine(MapLevel level, line_data line)
        {
            var lineIndex = (short) level.LineList.IndexOf(line);
            if (lineIndex < 0)
            {
                return;
            }

            RemoveSide(level, lineIndex, isClockwise: true);
            RemoveSide(level, lineIndex, isClockwise: false);

            var lastIndex = (short) (level.LineList.Count - 1);
            if (lineIndex != lastIndex)
            {
                level.LineList[lineIndex] = level.LineList[lastIndex];

                foreach (var polygon in level.PolygonList)
                {
                    for (var i = 0; i < polygon.vertex_count; i++)
                    {
                        if (polygon.line_indexes[i] == lastIndex)
                        {
                            polygon.line_indexes[i] = lineIndex;
                        }
                    }
                }

                foreach (var side in level.SideList)
                {
                    if (side.line_index == lastIndex)
                    {
                        side.line_index = lineIndex;
                    }
                }
            }

            level.LineList.RemoveAt(lastIndex);
        }

        // One no line or polygon uses
        public static void RemoveEndpoint(MapLevel level, endpoint_data endpoint)
        {
            var endpointIndex = (short) level.EndpointList.IndexOf(endpoint);
            if (endpointIndex < 0)
            {
                return;
            }

            var lastIndex = (short) (level.EndpointList.Count - 1);
            if (endpointIndex != lastIndex)
            {
                level.EndpointList[endpointIndex] = level.EndpointList[lastIndex];
                RenameEndpoint(level, lastIndex, endpointIndex);
            }

            level.EndpointList.RemoveAt(lastIndex);
        }

        // Every line and polygon corner at one point is at another instead
        public static void RenameEndpoint(MapLevel level, short fromIndex, short toIndex)
        {
            foreach (var line in level.LineList)
            {
                for (var i = 0; i < 2; i++)
                {
                    if (line.endpoint_indexes[i] == fromIndex)
                    {
                        line.endpoint_indexes[i] = toIndex;
                    }
                }
            }

            foreach (var polygon in level.PolygonList)
            {
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    if (polygon.endpoint_indexes[i] == fromIndex)
                    {
                        polygon.endpoint_indexes[i] = toIndex;
                    }
                }
            }
        }

        // With its sides, its platform (if it is one), and its lines that no other polygon has. What's in it moves into a
        // polygon beside it, and what refers to it (a teleporter's or trigger's destination, a terminal's teleport) no
        // longer does.
        public static void RemovePolygon(MapLevel level, polygon_data polygon)
        {
            var polygonIndex = (short) level.PolygonList.IndexOf(polygon);
            if (polygonIndex < 0)
            {
                return;
            }

            if (polygon.type == _polygon_is_platform)
            {
                PlatformEditing.RemovePlatformData(level, polygonIndex, _polygon_is_normal);
            }

            // What's in it goes to a neighbor (before its lines are gone)
            var neighborIndex = cstypes.NONE;
            for (var i = 0; i < polygon.vertex_count && neighborIndex == cstypes.NONE; i++)
            {
                neighborIndex = polygon.adjacent_polygon_indexes[i];
            }

            ObjectPlacement.MoveOutOfPolygon(level, polygonIndex, neighborIndex);

            var orphanedLines = new List<line_data>();
            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var lineIndex = polygon.line_indexes[i];
                var line = level.LineList[lineIndex];

                foreach (var isClockwise in new[] { true, false })
                {
                    if (line.GetPolygonOwner(isClockwise) == polygonIndex)
                    {
                        RemoveSide(level, lineIndex, isClockwise);
                        SetPolygonOwner(line, isClockwise, cstypes.NONE);
                    }
                }

                if (line.clockwise_polygon_owner == cstypes.NONE && line.counterclockwise_polygon_owner == cstypes.NONE)
                {
                    orphanedLines.Add(line);
                }
            }

            RenamePolygon(level, polygonIndex, cstypes.NONE);

            var lastIndex = (short) (level.PolygonList.Count - 1);
            if (polygonIndex != lastIndex)
            {
                level.PolygonList[polygonIndex] = level.PolygonList[lastIndex];
                RenamePolygon(level, lastIndex, polygonIndex);
            }

            level.PolygonList.RemoveAt(lastIndex);

            foreach (var line in orphanedLines)
            {
                RemoveLine(level, line);
            }
        }

        // What refers to a polygon by its index refers to another (or, for NONE, to none)
        private static void RenamePolygon(MapLevel level, short fromIndex, short toIndex)
        {
            foreach (var line in level.LineList)
            {
                if (line.clockwise_polygon_owner == fromIndex)
                {
                    line.clockwise_polygon_owner = toIndex;
                }

                if (line.counterclockwise_polygon_owner == fromIndex)
                {
                    line.counterclockwise_polygon_owner = toIndex;
                }
            }

            foreach (var side in level.SideList)
            {
                if (side.polygon_index == fromIndex)
                {
                    side.polygon_index = toIndex;
                }
            }

            foreach (var polygon in level.PolygonList)
            {
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    if (polygon.adjacent_polygon_indexes[i] == fromIndex)
                    {
                        polygon.adjacent_polygon_indexes[i] = toIndex;
                    }
                }

                // A destination that's gone leaves a normal polygon
                if (ReferencesPolygon(polygon) && polygon.permutation == fromIndex)
                {
                    if (toIndex == cstypes.NONE)
                    {
                        polygon.type = _polygon_is_normal;
                        polygon.permutation = 0;
                    }
                    else
                    {
                        polygon.permutation = toIndex;
                    }
                }
            }

            foreach (var mapObject in level.SavedObjectList)
            {
                if (mapObject.polygon_index == fromIndex)
                {
                    mapObject.polygon_index = toIndex;
                }
            }

            foreach (var annotation in level.MapAnnotationList)
            {
                if (annotation.polygon_index == fromIndex)
                {
                    annotation.polygon_index = toIndex;
                }
            }

            foreach (var platform in level.PlatformList)
            {
                if (platform.polygon_index == fromIndex)
                {
                    platform.polygon_index = toIndex;
                }
            }

            foreach (var staticPlatform in level.static_platforms)
            {
                if (staticPlatform.polygon_index == fromIndex)
                {
                    staticPlatform.polygon_index = toIndex;
                }
            }

            // Terminals' teleports within the level
            for (short terminalIndex = 0; terminalIndex < level.map_terminal_text.Count; terminalIndex++)
            {
                var terminal = computer_interface.get_indexed_terminal_data(level, terminalIndex);
                var groups = TerminalSource.GetGroups(terminal);
                var isChanged = false;

                foreach (var group in groups.Where(group => group.Type == computer_interface._intralevel_teleport_group && group.Permutation == fromIndex))
                {
                    group.Permutation = toIndex;
                    group.IsChanged = true;
                    isChanged = true;
                }

                if (isChanged)
                {
                    TerminalSource.SetGroups(terminal, groups);
                }
            }
        }

        // Teleporters and platform triggers name a polygon in their permutation
        private static bool ReferencesPolygon(polygon_data polygon)
        {
            return polygon.type == _polygon_is_teleporter ||
                   polygon.type == _polygon_is_platform_on_trigger ||
                   polygon.type == _polygon_is_platform_off_trigger;
        }

        private static void SetPolygonOwner(line_data line, bool isClockwise, short polygonIndex)
        {
            if (isClockwise)
            {
                line.clockwise_polygon_owner = polygonIndex;
            }
            else
            {
                line.counterclockwise_polygon_owner = polygonIndex;
            }
        }

        // Its own line, as the side records it, or whichever line has it
        private static bool TryFindLineWithSide(MapLevel level, short sideIndex, out short lineIndex, out bool isClockwise)
        {
            var recordedLineIndex = level.SideList[sideIndex].line_index;

            if (recordedLineIndex >= 0 && recordedLineIndex < level.LineList.Count && HasSide(level.LineList[recordedLineIndex], sideIndex, out isClockwise))
            {
                lineIndex = recordedLineIndex;
                return true;
            }

            for (short i = 0; i < level.LineList.Count; i++)
            {
                if (HasSide(level.LineList[i], sideIndex, out isClockwise))
                {
                    lineIndex = i;
                    return true;
                }
            }

            lineIndex = cstypes.NONE;
            isClockwise = false;
            return false;
        }

        private static bool HasSide(line_data line, short sideIndex, out bool isClockwise)
        {
            isClockwise = line.clockwise_polygon_side_index == sideIndex;

            return isClockwise || line.counterclockwise_polygon_side_index == sideIndex;
        }

        // A polygon's list of its sides is worked out from its lines
        private static void RecalculatePolygonSides(MapLevel level, short polygonIndex)
        {
            if (polygonIndex >= 0 && polygonIndex < level.PolygonList.Count)
            {
                map_constructors.calculate_adjacent_sides(level, polygonIndex, level.PolygonList[polygonIndex].side_indexes);
            }
        }
    }
}
#endif
