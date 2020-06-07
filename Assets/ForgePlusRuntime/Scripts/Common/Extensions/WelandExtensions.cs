using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using UnityEngine;
using Weland;

namespace Weland.Extensions
{
    public static class WelandExtensions
    {
        public static short GetOpposingPlatformIndex(this Side side, Level level)
        {
            var opposingPolygonIndex = side.OpposingPolygonIndex(level);

            if (opposingPolygonIndex < 0)
            {
                return -1;
            }

            var opposingPolygon = level.Polygons[opposingPolygonIndex];

            if (opposingPolygon.Type != PolygonType.Platform)
            {
                return -1;
            }

            return opposingPolygon.Permutation;
        }

        public static bool HasOpposingPolygon(this Side side, Level level)
        {
            var opposingPolygonIndex = side.OpposingPolygonIndex(level);

            return opposingPolygonIndex >= 0;
        }

        public static short OpposingPolygonIndex(this Side side, Level level)
        {
            var line = level.Lines[side.LineIndex];

            return !side.IsClockwise(level) ? line.ClockwisePolygonOwner : line.CounterclockwisePolygonOwner;
        }

        public static bool SurfaceShouldBeOpaque(this Side side, LevelEntity_Side.DataSources dataSource, Level level)
        {
            var line = level.Lines[side.LineIndex];

            return dataSource == LevelEntity_Side.DataSources.Primary ||
                   dataSource == LevelEntity_Side.DataSources.Secondary ||
                   side.Type == SideType.Full ||
                   !line.Transparent ||
                   !side.HasOpposingPolygon(level);
        }

        public static short RuntimeMinimumHeight(this Platform platform, Level level)
        {
            if (platform.ExtendsFloorToCeiling)
            {
                return level.Polygons[platform.PolygonIndex].FloorHeight;
            }

            if (platform.MinimumHeight != -1)
            {
                return platform.MinimumHeight;
            }

            return level.AutocalPlatformMinimum((short)level.Platforms.IndexOf(platform));
        }

        public static short RuntimeMaximumHeight(this Platform platform, Level level)
        {
            if (platform.ExtendsFloorToCeiling)
            {
                return level.Polygons[platform.PolygonIndex].CeilingHeight;
            }

            if (platform.MaximumHeight != -1)
            {
                return platform.MaximumHeight;
            }

            return level.AutocalPlatformMaximum((short)level.Platforms.IndexOf(platform));
        }

        public static bool HasDataSource(this Side side, LevelEntity_Side.DataSources dataSource)
        {
            switch (dataSource)
            {
                case LevelEntity_Side.DataSources.Primary:
                    return !side.Primary.Texture.IsEmpty();
                case LevelEntity_Side.DataSources.Secondary:
                    return !side.Secondary.Texture.IsEmpty();
                case LevelEntity_Side.DataSources.Transparent:
                    return !side.Transparent.Texture.IsEmpty();
                default:
                    throw new NotImplementedException($"FPSide DataSource \"{dataSource}\" is not implemented.");
            }
        }

        public static bool HasLayeredTransparentSide(this Side side, Level level)
        {
            if (side == null)
            {
                return false;
            }

            var destinationLine = level.Lines[side.LineIndex];

            return (destinationLine.Flags & LineFlags.HasTransparentSide) != 0 &&
                   side.Type == SideType.Full &&
                   !side.Transparent.Texture.IsEmpty() &&
                   !side.HasOpposingPolygon(level);
        }

        public static bool IsClockwise(this Side side, Level level)
        {
            var line = level.Lines[side.LineIndex];

            return GetIsClockwise(side, level, line);
        }

        public static LevelEntity_Side GetFPSide(this Line line, Level level, bool clockwiseSide)
        {
            var sideIndex = clockwiseSide ? line.ClockwisePolygonSideIndex : line.CounterclockwisePolygonSideIndex;

            if (sideIndex < 0 || !LevelEntity_Level.Instance.FPSides.ContainsKey(sideIndex))
            {
                return null;
            }

            return LevelEntity_Level.Instance.FPSides[sideIndex];
        }

        private static Side Side(this Line line, Level level, bool clockwiseSide)
        {
            var sideIndex = clockwiseSide ? line.ClockwisePolygonSideIndex : line.CounterclockwisePolygonSideIndex;

            if (sideIndex < 0)
            {
                return null;
            }

            return level.Sides[sideIndex];
        }

        public static bool SideIsNeighbor(this Side side, Level level, Side possibleNeighbor, out bool neighborFlowsOutward, out bool neighborIsLeft)
        {
            if (side.SideIsNeighbor(level, possibleNeighbor, left: true, out neighborFlowsOutward))
            {
                neighborIsLeft = true;

                return true;
            }

            neighborIsLeft = false;

            return side.SideIsNeighbor(level, possibleNeighbor, left: false, out neighborFlowsOutward);
        }

        private static bool SideIsNeighbor(this Side side, Level level, Side possibleNeighbor)
        {
            return side.SideIsNeighbor(level, possibleNeighbor, left: true) ||
                   side.SideIsNeighbor(level, possibleNeighbor, left: false);
        }

        private static bool SideIsNeighbor(this Side side, Level level, Side possibleNeighbor, bool left)
        {
            return side.SideIsNeighbor(level, possibleNeighbor, left, out _);
        }

        private static bool SideIsNeighbor(this Side side, Level level, Side possibleNeighbor, bool left, out bool neighborFlowsOutward)
        {
            var line = level.Lines[side.LineIndex];
            var endpointIndex = side.EndpointIndex(level, line, left);
            var endpointLines = level.EndpointLines[endpointIndex];

            foreach (var neighborLine in endpointLines)
            {
                if (neighborLine == line)
                {
                    continue;
                }

                neighborFlowsOutward = neighborLine.EndpointIndexes[0] == endpointIndex;
                var neighborIsClockwise = neighborFlowsOutward != left;

                var neighborSide = neighborLine.Side(level, neighborIsClockwise);

                if (neighborSide == null)
                {
                    continue;
                }

                if (neighborSide == possibleNeighbor)
                {
                    return true;
                }
            }

            neighborFlowsOutward = false;

            return false;
        }

        private static bool GetIsClockwise(Side side, Level level, Line line)
        {
            var clockwiseSide = line.Side(level, clockwiseSide: true);

            return side == clockwiseSide;
        }

        public static short EndpointIndex(this Side side, Level level, Line line, bool left)
        {
            return line.EndpointIndexes[GetIsClockwise(side, level, line) == left ? 0 : 1];
        }
    }
}
