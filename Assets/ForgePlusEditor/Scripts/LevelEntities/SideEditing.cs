#if !NO_EDITING
using AlephOne;
using ForgePlus.History;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Sides as the inspectors edit them: adding one to a face of a line (with a polygon on that face) that has none, and
    // whether one fills its line's opening. A side's type is what decides that (Aleph One draws a full side floor to
    // ceiling across the opening, and a high, low or split side only the panels its polygons' heights expose:
    // RenderRasterize.cpp, render_node), so it's only set through these. The line's surfaces are built again afterward.
    public static class SideEditing
    {
        // A side the polygon on the face doesn't have yet, copying the textures and lights of one of its sides beside it
        // (or blank, if it has none). With panels (a step or drop), it leaves the line's opening open; with none (a
        // window), it fills it, as that's all a side there is for. Returns its index (or NONE).
        public static short AddSide(LevelEntity_Level level, short lineIndex, bool isClockwise)
        {
            var data = level.Level;
            var line = data.LineList[lineIndex];
            var polygonIndex = isClockwise ? line.clockwise_polygon_owner : line.counterclockwise_polygon_owner;
            var existing = isClockwise ? line.clockwise_polygon_side_index : line.counterclockwise_polygon_side_index;

            if (polygonIndex == cstypes.NONE || existing != cstypes.NONE)
            {
                return existing;
            }

            var source = GeometryConstruction.FindSideToCopy(data, polygonIndex, new short[0], line.endpoint_indexes);
            var sideIndex = GeometryConstruction.AddSide(data, polygonIndex, lineIndex, source);

            var polygon = data.PolygonList[polygonIndex];
            map_constructors.calculate_adjacent_sides(data, polygonIndex, polygon.side_indexes);
            LineFlagsEditing.UpdateForSide(data, data.SideList[sideIndex]);

            LevelHistory.MarkEdited();
            RebuildLine(level, lineIndex);

            return sideIndex;
        }

        // Whether the side's polygons' heights leave an opening for it to fill
        public static bool CanFillOpening(MapLevel data, side_data side)
        {
            return side != null && side.line_index >= 0 && side.line_index < data.LineList.Count && GeometryConstruction.HasOpening(data, side.line_index);
        }

        // Filled by its transparent texture (drawn across the opening for any type: RenderRasterize.cpp, render_node), or
        // as a full side by its primary (drawn floor to ceiling). Either is only drawn with a texture (render_node_side).
        public static bool FillsOpening(MapLevel data, side_data side)
        {
            return CanFillOpening(data, side) &&
                   ((side.type == map._full_side && side.primary_texture.texture != cstypes.UNONE) ||
                    side.transparent_texture.texture != cstypes.UNONE);
        }

        // Where its polygons' heights expose panels (a step up to the floor beyond, or a drop down to the ceiling beyond),
        // it keeps the type that draws them (high, low or split, never full), each with its own texture and light, and
        // its transparent texture fills the opening between them (starting as its primary, or another of its textures, or
        // a side of its polygon beside it), or is cleared to leave it open. Where they expose neither (a window out of a
        // constriction, or between polygons of the same heights), the side only fills the opening, as a full side, so
        // leaving it open removes the side (with the selection moving to its line, as one action).
        public static void SetFillsOpening(LevelEntity_Side side, bool fills)
        {
            var level = side.ParentLevel;
            var data = level.Level;
            var nativeSide = side.NativeObject;
            var lineIndex = side.ParentLineIndex;

            if (!CanFillOpening(data, nativeSide) || FillsOpening(data, nativeSide) == fills)
            {
                return;
            }

            if (GeometryConstruction.IsWindowFace(data, lineIndex, side.IsClockwise))
            {
                if (fills)
                {
                    // With a texture to fill it with (a full side without one draws nothing)
                    nativeSide.type = map._full_side;
                    FillWithTexture(data, nativeSide, lineIndex, isPrimary: true);
                }
                else
                {
                    RemoveSide(level, lineIndex, side.IsClockwise);
                    return;
                }
            }
            else
            {
                nativeSide.type = GeometryConstruction.SideTypeForHeights(data, side.NativeIndex);

                if (!fills)
                {
                    nativeSide.transparent_texture.texture = cstypes.UNONE;
                }
                else
                {
                    FillWithTexture(data, nativeSide, lineIndex, isPrimary: false);
                }
            }

            LineFlagsEditing.UpdateForSide(data, nativeSide);

            LevelHistory.MarkEdited();
            RebuildLinesNextFrame(level, new[] { lineIndex }, selectLine: false);
        }

        // Unless it has one already, the first texture there is of its primary (for its transparent texture), its
        // transparent texture (for its primary), its secondary, or a side of its polygon beside it (as copying one does),
        // for its primary (a window's full side) or its transparent texture
        private static void FillWithTexture(MapLevel data, side_data side, short lineIndex, bool isPrimary)
        {
            if ((isPrimary ? side.primary_texture : side.transparent_texture).texture != cstypes.UNONE)
            {
                return;
            }

            var line = data.LineList[lineIndex];
            var beside = GeometryConstruction.FindSideToCopy(data, side.polygon_index, new short[0], line.endpoint_indexes, excludedLine: lineIndex);
            var candidates = new List<(side_texture_definition Texture, short Light, short TransferMode)>
            {
                isPrimary ?
                    (side.transparent_texture, side.transparent_lightsource_index, side.transparent_transfer_mode) :
                    (side.primary_texture, side.primary_lightsource_index, side.primary_transfer_mode),
                (side.secondary_texture, side.secondary_lightsource_index, side.secondary_transfer_mode),
            };

            if (beside != null)
            {
                candidates.Add((beside.primary_texture, beside.primary_lightsource_index, beside.primary_transfer_mode));
            }

            foreach (var (texture, light, transferMode) in candidates)
            {
                if (texture.texture == cstypes.UNONE)
                {
                    continue;
                }

                if (isPrimary)
                {
                    side.primary_texture = texture.Clone();
                    side.primary_lightsource_index = light;
                    side.primary_transfer_mode = transferMode;
                }
                else
                {
                    side.transparent_texture = texture.Clone();
                    side.transparent_lightsource_index = light;
                    side.transparent_transfer_mode = transferMode;
                }

                return;
            }
        }

        // The side on the face, which no longer has any data to keep
        private static void RemoveSide(LevelEntity_Level level, short lineIndex, bool isClockwise)
        {
            var data = level.Level;
            var changedLines = new HashSet<short> { lineIndex };

            LevelTopology.RemoveSide(data, lineIndex, isClockwise, changedLines);
            map_constructors.recalculate_redundant_line_data(data, lineIndex);

            LevelHistory.MarkEdited();
            RebuildLinesNextFrame(level, changedLines, selectLine: true);
        }

        // Its sides built again (with those selected selected again) next frame, once whatever changed them (such as an
        // inspector's control, or painting) has finished with them
        public static void RebuildLineSoon(LevelEntity_Level level, short lineIndex)
        {
            RebuildLinesNextFrame(level, new[] { lineIndex }, selectLine: false);
        }

        // Once the inspector whose control changed it has finished with the (soon rebuilt) sides. Removing a side moves
        // another (the last) to its index, so that one's line is built again too.
        private static async void RebuildLinesNextFrame(LevelEntity_Level level, ICollection<short> lineIndexes, bool selectLine)
        {
            var firstLineIndex = lineIndexes.First();

            await Awaitable.NextFrameAsync();

            if (!level || level != LevelEntity_Level.Instance || LevelEditing.IsRebuildPending)
            {
                return;
            }

            if (selectLine)
            {
                foreach (var lineIndex in lineIndexes)
                {
                    if (level.Lines.TryGetValue(lineIndex, out var line))
                    {
                        line.RegenerateSurfaces();
                    }
                }

                // Undone and redone with the removal (recorded by now)
                if (level.Lines.TryGetValue(firstLineIndex, out var removedFrom))
                {
                    SelectionHistory.JoinNextToLast();
                    SelectionManager.Instance.SelectObject(removedFrom, multiSelect: false);
                }

                return;
            }

            foreach (var lineIndex in lineIndexes)
            {
                RebuildLine(level, lineIndex);
            }
        }

        // Its sides are built again (which surfaces they have depends on their types), and those that were selected are
        // selected again
        private static void RebuildLine(LevelEntity_Level level, short lineIndex)
        {
            if (!level.Lines.TryGetValue(lineIndex, out var line))
            {
                return;
            }

            var selectedSides = new List<short>();
            foreach (var side in new[] { line.ClockwiseSide, line.CounterclockwiseSide })
            {
                if (side && side.IsSelected && side.NativeIndex >= 0)
                {
                    selectedSides.Add(side.NativeIndex);
                }
            }

            line.RegenerateSurfaces();

            foreach (var sideIndex in selectedSides)
            {
                if (level.Sides.TryGetValue(sideIndex, out var rebuilt) && rebuilt)
                {
                    SelectionManager.Instance.SelectObject(rebuilt, multiSelect: true);
                }
            }
        }
    }
}
#endif
