#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;

namespace ForgePlus.LevelManipulation
{
    // Faces a height edit reveals get sides (map_constructors.cpp: new_side), and faces it hides lose them, as a leftover
    // side would become full (recalculate_side_type) and be drawn as a wall across the opening. Faces the edit doesn't
    // reveal or hide (such as ones loaded without sides) are left as they are.
    public class SideExposure
    {
        private readonly Dictionary<(short LineIndex, bool IsClockwise), bool> exposedBefore = new Dictionary<(short LineIndex, bool IsClockwise), bool>();

        // Before the edit changes anything. A face already recorded keeps how it was first recorded (such as before a drag began).
        public void RecordPolygon(MapLevel level, polygon_data polygon)
        {
            for (var i = 0; i < polygon.vertex_count; i++)
            {
                RecordFace(level, polygon.line_indexes[i], isClockwise: true);
                RecordFace(level, polygon.line_indexes[i], isClockwise: false);
            }
        }

        // Once the edit is done, or if it fails partway
        public void Clear()
        {
            exposedBefore.Clear();
        }

        // After the edit. Returns the lines whose sides were added, removed, or moved to another index, which are built again.
        public HashSet<short> ApplyToSides(MapLevel level)
        {
            var changedLines = new HashSet<short>();

            foreach (var face in exposedBefore)
            {
                var (lineIndex, isClockwise) = face.Key;
                var wasExposed = face.Value;
                var isExposed = LevelEntity_Side.IsFaceExposed(level, lineIndex, isClockwise);

                if (isExposed == wasExposed)
                {
                    continue;
                }

                var line = level.LineList[lineIndex];
                var sideIndex = line.GetSideIndex(isClockwise);
                var polygonIndex = line.GetPolygonOwner(isClockwise);

                if (isExposed && sideIndex == cstypes.NONE && polygonIndex != cstypes.NONE)
                {
                    map_constructors.new_side(level, polygonIndex, lineIndex);
                    changedLines.Add(lineIndex);
                }
                else if (!isExposed && sideIndex != cstypes.NONE)
                {
                    RemoveSide(level, lineIndex, isClockwise, changedLines);
                }
            }

            return changedLines;
        }

        private void RecordFace(MapLevel level, short lineIndex, bool isClockwise)
        {
            var face = (lineIndex, isClockwise);

            if (!exposedBefore.ContainsKey(face))
            {
                exposedBefore[face] = LevelEntity_Side.IsFaceExposed(level, lineIndex, isClockwise);
            }
        }

        // Sides after it would each move down an index, which lines and polygons refer to them by, so the last side
        // takes its index instead
        private static void RemoveSide(MapLevel level, short lineIndex, bool isClockwise, HashSet<short> changedLines)
        {
            var line = level.LineList[lineIndex];
            var sideIndex = line.GetSideIndex(isClockwise);
            var lastIndex = (short) (level.SideList.Count - 1);

            line.SetSideIndex(isClockwise, cstypes.NONE);
            RecalculatePolygonSides(level, line.GetPolygonOwner(isClockwise));
            changedLines.Add(lineIndex);

            if (sideIndex != lastIndex)
            {
                level.SideList[sideIndex] = level.SideList[lastIndex];

                // A side on no line has nothing referring to it
                if (TryFindLineWithSide(level, lastIndex, out var movedLineIndex, out var movedIsClockwise))
                {
                    var movedLine = level.LineList[movedLineIndex];

                    movedLine.SetSideIndex(movedIsClockwise, sideIndex);
                    RecalculatePolygonSides(level, movedLine.GetPolygonOwner(movedIsClockwise));
                    changedLines.Add(movedLineIndex);
                }
            }

            level.SideList.RemoveAt(lastIndex);
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
            if (polygonIndex != cstypes.NONE)
            {
                map_constructors.calculate_adjacent_sides(level, polygonIndex, level.PolygonList[polygonIndex].side_indexes);
            }
        }
    }
}
#endif
