using AlephOne;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using static AlephOne.map;

namespace ForgePlus.Extensions
{
    public static class LevelDataExtensions
    {
        public static bool HasOpposingPolygon(this side_data side, MapLevel level)
        {
            var opposingPolygonIndex = side.OpposingPolygonIndex(level);

            return opposingPolygonIndex >= 0;
        }

        public static short OpposingPolygonIndex(this side_data side, MapLevel level)
        {
            return find_adjacent_polygon(level, side.polygon_index, side.line_index);
        }

        public static bool SurfaceShouldBeOpaque(this side_data side, LevelEntity_Side.DataSources dataSource, MapLevel level)
        {
            var line = get_line_data(level, side.line_index);

            return !LINE_IS_TRANSPARENT(line) ||
                   !side.HasOpposingPolygon(level) ||
                   dataSource == LevelEntity_Side.DataSources.Secondary ||
                   (dataSource == LevelEntity_Side.DataSources.Primary &&
                   side.type != _full_side);
        }

        public static side_texture_definition GetTexture(this side_data side, LevelEntity_Side.DataSources dataSource)
        {
            switch (dataSource)
            {
                case LevelEntity_Side.DataSources.Primary:
                    return side.primary_texture;
                case LevelEntity_Side.DataSources.Secondary:
                    return side.secondary_texture;
                case LevelEntity_Side.DataSources.Transparent:
                    return side.transparent_texture;
                default:
                    throw new NotImplementedException($"Side DataSource \"{dataSource}\" is not implemented.");
            }
        }

        public static short GetTransferMode(this side_data side, LevelEntity_Side.DataSources dataSource)
        {
            switch (dataSource)
            {
                case LevelEntity_Side.DataSources.Primary:
                    return side.primary_transfer_mode;
                case LevelEntity_Side.DataSources.Secondary:
                    return side.secondary_transfer_mode;
                case LevelEntity_Side.DataSources.Transparent:
                    return side.transparent_transfer_mode;
                default:
                    throw new NotImplementedException($"Side DataSource \"{dataSource}\" is not implemented.");
            }
        }

        public static short GetLightsourceIndex(this side_data side, LevelEntity_Side.DataSources dataSource)
        {
            switch (dataSource)
            {
                case LevelEntity_Side.DataSources.Primary:
                    return side.primary_lightsource_index;
                case LevelEntity_Side.DataSources.Secondary:
                    return side.secondary_lightsource_index;
                case LevelEntity_Side.DataSources.Transparent:
                    return side.transparent_lightsource_index;
                default:
                    throw new NotImplementedException($"Side DataSource \"{dataSource}\" is not implemented.");
            }
        }

        public static bool HasDataSource(this side_data side, LevelEntity_Side.DataSources dataSource)
        {
            return !side.GetTexture(dataSource).texture.IsEmptyShapeDescriptor();
        }

        public static bool HasLayeredTransparentSide(this side_data side, MapLevel level)
        {
            if (side == null)
            {
                return false;
            }

            var destinationLine = get_line_data(level, side.line_index);

            return LINE_HAS_TRANSPARENT_SIDE(destinationLine) &&
                   side.type == _full_side &&
                   !side.transparent_texture.texture.IsEmptyShapeDescriptor() &&
                   !side.HasOpposingPolygon(level);
        }

        public static short GetPolygonOwner(this line_data line, bool clockwise)
        {
            return clockwise ? line.clockwise_polygon_owner : line.counterclockwise_polygon_owner;
        }

        public static short GetSideIndex(this line_data line, bool clockwise)
        {
            return clockwise ? line.clockwise_polygon_side_index : line.counterclockwise_polygon_side_index;
        }

        public static LevelEntity_Side GetRuntimeSide(this line_data line, bool clockwiseSide)
        {
            var sideIndex = line.GetSideIndex(clockwiseSide);

            if (sideIndex < 0 || !LevelEntity_Level.Instance.Sides.ContainsKey(sideIndex))
            {
                return null;
            }

            return LevelEntity_Level.Instance.Sides[sideIndex];
        }

        public static platform_data GetPlatform(this polygon_data polygon, MapLevel level)
        {
            // A platform polygon's permutation is its platform index (platforms.cpp: new_platform)
            return polygon.type == _polygon_is_platform ? platforms.get_platform_data(level, polygon.permutation) : null;
        }

        // The lowest and highest the polygon's floor and ceiling get as its platform (if any) moves
        public static void GetHeightRange(this polygon_data polygon, MapLevel level, out short lowestFloor, out short highestFloor, out short lowestCeiling, out short highestCeiling)
        {
            lowestFloor = highestFloor = polygon.floor_height;
            lowestCeiling = highestCeiling = polygon.ceiling_height;

            var platform = polygon.GetPlatform(level);
            if (platform == null)
            {
                return;
            }

            if (platforms.PLATFORM_COMES_FROM_FLOOR(platform.static_flags))
            {
                lowestFloor = Math.Min(lowestFloor, platform.minimum_floor_height);
                highestFloor = Math.Max(highestFloor, platform.maximum_floor_height);
            }

            if (platforms.PLATFORM_COMES_FROM_CEILING(platform.static_flags))
            {
                lowestCeiling = Math.Min(lowestCeiling, platform.minimum_ceiling_height);
                highestCeiling = Math.Max(highestCeiling, platform.maximum_ceiling_height);
            }
        }

        public static List<short>[] BuildEndpointLines(this MapLevel level)
        {
            var endpointLines = new List<short>[level.EndpointList.Count];
            for (var i = 0; i < endpointLines.Length; i++)
            {
                endpointLines[i] = new List<short>();
            }

            for (short lineIndex = 0; lineIndex < level.LineList.Count; lineIndex++)
            {
                var line = level.LineList[lineIndex];
                endpointLines[line.endpoint_indexes[0]].Add(lineIndex);
                endpointLines[line.endpoint_indexes[1]].Add(lineIndex);
            }

            return endpointLines;
        }

        public static bool SideIsNeighbor(this side_data side, LevelEntity_Level level, side_data possibleNeighbor, out bool neighborFlowsOutward, out bool neighborIsLeft)
        {
            if (side.SideIsNeighbor(level, possibleNeighbor, left: true, out neighborFlowsOutward))
            {
                neighborIsLeft = true;

                return true;
            }

            neighborIsLeft = false;

            return side.SideIsNeighbor(level, possibleNeighbor, left: false, out neighborFlowsOutward);
        }

        public static short EndpointIndex(this side_data side, line_data line, bool left)
        {
            var isClockwise = line.clockwise_polygon_owner == side.polygon_index;

            return line.endpoint_indexes[isClockwise == left ? 0 : 1];
        }

        private static bool SideIsNeighbor(this side_data side, LevelEntity_Level level, side_data possibleNeighbor, bool left, out bool neighborFlowsOutward)
        {
            var line = get_line_data(level.Level, side.line_index);
            var endpointIndex = side.EndpointIndex(line, left);

            foreach (var neighborLineIndex in level.EndpointLines[endpointIndex])
            {
                var neighborLine = get_line_data(level.Level, neighborLineIndex);

                if (neighborLine == line)
                {
                    continue;
                }

                neighborFlowsOutward = neighborLine.endpoint_indexes[0] == endpointIndex;
                var neighborIsClockwise = neighborFlowsOutward != left;

                var neighborSideIndex = neighborLine.GetSideIndex(neighborIsClockwise);

                if (neighborSideIndex < 0)
                {
                    continue;
                }

                if (get_side_data(level.Level, neighborSideIndex) == possibleNeighbor)
                {
                    return true;
                }
            }

            neighborFlowsOutward = false;

            return false;
        }
    }
}
