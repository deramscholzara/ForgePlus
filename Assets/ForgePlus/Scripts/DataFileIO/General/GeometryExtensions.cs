using ForgePlus.LevelManipulation;
using System;

namespace Weland.Extensions
{
    public static class GeometryExtensions
    {
        public static bool HasDataSource(this Side side, FPSide.DataSources dataSource)
        {
            switch(dataSource)
            {
                case FPSide.DataSources.Primary:
                    return !side.Primary.Texture.IsEmpty();
                case FPSide.DataSources.Secondary:
                    return !side.Secondary.Texture.IsEmpty();
                case FPSide.DataSources.Transparent:
                    return !side.Transparent.Texture.IsEmpty();
                default:
                    throw new NotImplementedException($"FPSide DataSource \"{dataSource}\" is not implemented.");
            }
        }

        public static bool HasLayeredTransparentSide(this Side side, Level level)
        {
            var destinationLine = level.Lines[side.LineIndex];

            return (destinationLine.Flags & LineFlags.HasTransparentSide) != 0 &&
                   side.Type == SideType.Full &&
                   !side.Transparent.Texture.IsEmpty();
        }

        public static bool IsClockwise(this Side side, Level level)
        {
            var line = level.Lines[side.LineIndex];

            return GetIsClockwise(side, level, line);
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

        // TODO: Keeping this around in case it's useful in the future (like for aligned offset dragging).
        // TODO: Will likely want a NeighboringPolygons variant, as well.
        ////private static List<Side> NeighboringSides(this Side side, Level level, bool left)
        ////{
        ////    var line = level.Lines[side.LineIndex];
        ////    var endpointIndex = side.EndpointIndex(level, line, left);
        ////    var endpointLines = level.EndpointLines[endpointIndex];

        ////    var neighboringSides = new List<Side>();

        ////    foreach (var neighborLine in endpointLines)
        ////    {
        ////        if (neighborLine == line)
        ////        {
        ////            continue;
        ////        }

        ////        var neighborFlowsOutward = neighborLine.EndpointIndexes[0] == endpointIndex;
        ////        var neighborIsClockwise = neighborFlowsOutward != left;

        ////        var neighborSide = neighborLine.Side(level, neighborIsClockwise);

        ////        if (neighborSide == null)
        ////        {
        ////            continue;
        ////        }

        ////        neighboringSides.Add(neighborSide);
        ////    }

        ////    return neighboringSides;
        ////}

        private static bool GetIsClockwise(Side side, Level level, Line line)
        {
            var clockwiseSide = line.Side(level, clockwiseSide: true);

            return side == clockwiseSide;
        }

        private static short EndpointIndex(this Side side, Level level, Line line, bool left)
        {
            return line.EndpointIndexes[GetIsClockwise(side, level, line) == left ? 0 : 1];
        }
    }
}
