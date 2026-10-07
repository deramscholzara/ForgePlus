#if !NO_EDITING
using AlephOne;
using System;
using static AlephOne.map;

namespace ForgePlus.LevelManipulation
{
    // Keeps derived line flags in step with edits, as the engine only recalculates them for levels saved without 'iidx'
    // (recalculate_redundant_line_data). Each edit sets only its own flags, since recalculating a line wholly would take
    // variable elevation from solidity, which a running platform sets (adjust_platform_endpoint_and_line_heights).
    public static class LineFlagsEditing
    {
        // Landscape when either side's primary surface is one, and a transparent side when either has a transparent texture
        public static void UpdateForSide(MapLevel level, side_data side)
        {
            if (side.line_index < 0 || side.line_index >= level.LineList.Count)
            {
                return;
            }

            var line = level.LineList[side.line_index];
            var clockwiseSide = SideOf(level, line.clockwise_polygon_side_index);
            var counterclockwiseSide = SideOf(level, line.counterclockwise_polygon_side_index);

            SET_LINE_LANDSCAPE_STATUS(line,
                (clockwiseSide != null && clockwiseSide.primary_transfer_mode == _xfer_landscape) ||
                (counterclockwiseSide != null && counterclockwiseSide.primary_transfer_mode == _xfer_landscape));

            SET_LINE_HAS_TRANSPARENT_SIDE(line,
                (clockwiseSide != null && clockwiseSide.transparent_texture.texture != cstypes.UNONE) ||
                (counterclockwiseSide != null && counterclockwiseSide.transparent_texture.texture != cstypes.UNONE));
        }

        // A line beside a platform has a variable elevation and is saved open (build_export_wad); otherwise it's solid or
        // transparent as its polygons' heights leave it (adjust_platform_endpoint_and_line_heights)
        public static void UpdateForPlatformChange(MapLevel level, polygon_data polygon)
        {
            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var lineIndex = polygon.line_indexes[i];
                if (lineIndex < 0 || lineIndex >= level.LineList.Count)
                {
                    continue;
                }

                var line = level.LineList[lineIndex];
                var clockwisePolygon = PolygonOf(level, line.clockwise_polygon_owner);
                var counterclockwisePolygon = PolygonOf(level, line.counterclockwise_polygon_owner);

                if (clockwisePolygon == null || counterclockwisePolygon == null)
                {
                    // A wall (with a polygon on one side only) is solid, so it never has a variable elevation
                    continue;
                }

                var isVariableElevation = clockwisePolygon.type == _polygon_is_platform || counterclockwisePolygon.type == _polygon_is_platform;
                if (isVariableElevation)
                {
                    SET_LINE_VARIABLE_ELEVATION(line, true);
                    SET_LINE_SOLIDITY(line, false);
                    SET_LINE_TRANSPARENCY(line, true);
                }
                else if (LINE_IS_VARIABLE_ELEVATION(line))
                {
                    var highestFloor = Math.Max(clockwisePolygon.floor_height, counterclockwisePolygon.floor_height);
                    var lowestCeiling = Math.Min(clockwisePolygon.ceiling_height, counterclockwisePolygon.ceiling_height);

                    SET_LINE_VARIABLE_ELEVATION(line, false);
                    SET_LINE_TRANSPARENCY(line, highestFloor < lowestCeiling);
                    SET_LINE_SOLIDITY(line, highestFloor >= lowestCeiling);
                }
            }
        }

        // Its endpoints are solid when any of their lines is (map_constructors.cpp: recalculate_redundant_endpoint_data)
        public static void SetSolidity(MapLevel level, short lineIndex, bool solid)
        {
            var line = level.LineList[lineIndex];
            SET_LINE_SOLIDITY(line, solid);

            foreach (var endpointIndex in line.endpoint_indexes)
            {
                if (endpointIndex < 0 || endpointIndex >= level.EndpointList.Count)
                {
                    continue;
                }

                var endpointIsSolid = false;
                foreach (var otherLine in level.LineList)
                {
                    if ((otherLine.endpoint_indexes[0] == endpointIndex || otherLine.endpoint_indexes[1] == endpointIndex) && LINE_IS_SOLID(otherLine))
                    {
                        endpointIsSolid = true;
                        break;
                    }
                }

                SET_ENDPOINT_SOLIDITY(level.EndpointList[endpointIndex], endpointIsSolid);
            }
        }

        private static side_data SideOf(MapLevel level, short sideIndex)
        {
            return sideIndex >= 0 && sideIndex < level.SideList.Count ? level.SideList[sideIndex] : null;
        }

        private static polygon_data PolygonOf(MapLevel level, short polygonIndex)
        {
            return polygonIndex >= 0 && polygonIndex < level.PolygonList.Count ? level.PolygonList[polygonIndex] : null;
        }
    }
}
#endif
