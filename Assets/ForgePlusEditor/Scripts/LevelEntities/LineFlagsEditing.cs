#if !NO_EDITING
using AlephOne;
using static AlephOne.map;

namespace ForgePlus.LevelManipulation
{
    // Keeps the line flags that follow from other data in step with edits to that data. Aleph One works them out
    // (recalculate_redundant_line_data) only for a level saved without map indexes ('iidx'), which every level Bungie
    // shipped has, so otherwise the engine uses them as they're saved. Each edit sets only the flags it affects, as
    // Aleph One works them out: recalculating a line wholly would also work out its variable elevation from its
    // solidity, which a platform sets while it runs (adjust_platform_endpoint_and_line_heights).
    public static class LineFlagsEditing
    {
        // A line is drawn as a landscape on the overhead map when either of its sides' primary surface is a landscape,
        // and has a transparent side (which projectiles hit, unless the line is decorative) when either has a
        // transparent texture
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

        // A line between two polygons where either is a platform has a variable elevation (so a platform makes it solid
        // while it's closed, and monsters can path through it), and is saved open, as Aleph One saves a level
        // (build_export_wad). One that no longer has one is solid or transparent as its polygons' heights leave it
        // (as a platform leaves it, adjust_platform_endpoint_and_line_heights).
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
                    var highestFloor = System.Math.Max(clockwisePolygon.floor_height, counterclockwisePolygon.floor_height);
                    var lowestCeiling = System.Math.Min(clockwisePolygon.ceiling_height, counterclockwisePolygon.ceiling_height);

                    SET_LINE_VARIABLE_ELEVATION(line, false);
                    SET_LINE_TRANSPARENCY(line, highestFloor < lowestCeiling);
                    SET_LINE_SOLIDITY(line, highestFloor >= lowestCeiling);
                }
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
