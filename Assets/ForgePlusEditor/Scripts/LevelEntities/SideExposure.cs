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
                    LevelTopology.RemoveSide(level, lineIndex, isClockwise, changedLines);
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
    }
}
#endif
