#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;

namespace ForgePlus.LevelManipulation
{
    // Keeps the sides of the faces an edit changes (a height edit, a platform's travel, or new geometry beside them) true
    // to what was seen before it, recorded before it changes anything (or before a drag began).
    //
    // A side between two polygons has up to three surfaces, which Aleph One draws from its type and its three textures
    // (RenderRasterize.cpp: render_node): a drop down to a lower ceiling beyond (a high or split side's primary), a step
    // up to a higher floor beyond (a low side's primary, or a split side's secondary), and the opening between them (its
    // transparent texture). Where its polygons' heights expose no panels (a window out of a constriction, or between
    // polygons of the same heights), the side is only there to fill the opening, as a full side (its primary drawn floor
    // to ceiling), or there's no side at all. So a side's type always follows its polygons' heights (GeometryConstruction:
    // FaceType): high, low or split where they expose panels (never full), and full where they expose none.
    //
    // As an edit reveals and hides its panels, its textures are rearranged so that each surface seen before keeps its
    // texture and light (textured or not): a drop or step wherever its new type keeps it, and a filled opening as the
    // transparent texture, or as a window's full side. A window left open has no data to keep, so its side is removed
    // once the edit is committed (until then, as a drag goes on, it's a high side with no height, which draws nothing).
    // Only faces that had a polygon beyond them before the edit had an opening to keep (a wall a new polygon opens up
    // doesn't stay filled). Faces whose polygons' heights the edit left as they were are left as they are.
    //
    // A panel the edit reveals takes the texture left for it from when it was last there (a split side's secondary), or
    // else the side's own texture (primary, if it has one), or else that of the side beside it on its left (as it's
    // seen), then on its right (for the same panel, or its primary), or else the floor (for a step) or ceiling (for a
    // drop) of the polygon beyond. Failing all of those, it's left untextured.
    //
    // A face the edit reveals (that had nothing to draw) gets a side (map_constructors.cpp: new_side), textured as a
    // revealed panel.
    public class SideExposure
    {
        private enum Panel
        {
            Top,
            Bottom,
        }

        private enum Layer
        {
            Primary,
            Secondary,
            Transparent,
        }

        // A texture, with the light and transfer mode it's drawn with
        private sealed class Surface
        {
            public side_texture_definition Texture;
            public short Light;
            public short TransferMode;

            public bool IsTextured
            {
                get
                {
                    return Texture.texture != cstypes.UNONE;
                }
            }
        }

        // A face's side as it was before the edit
        private sealed class SideBefore
        {
            public short Type;
            public Surface Primary;
            public Surface Secondary;
            public Surface Transparent;
            public bool HadPolygonBeyond;

            // Its polygons (and their heights, and the type they called for), which an edit that leaves them as they were
            // leaves it
            public (short, short, short, short, short, short, short) Geometry;

            // The panel's surface, if it had the panel (textured or not)
            public Surface PanelSurface(Panel panel)
            {
                if (!HasPanel(Type, panel))
                {
                    return null;
                }

                return LayerFor(Type, panel) == Layer.Primary ? Primary : Secondary;
            }

            // What filled its opening, or null if it was open: a full side's primary (drawn floor to ceiling), or else its
            // transparent texture (such as a grate in a window, which a full side with no primary draws alone)
            public Surface Opening
            {
                get
                {
                    if (!HadPolygonBeyond)
                    {
                        return null;
                    }

                    if (Type == map._full_side && Primary.IsTextured)
                    {
                        return Primary;
                    }

                    return Transparent.IsTextured ? Transparent : null;
                }
            }

            // Where it was, for a window to keep it there
            public Layer OpeningLayer
            {
                get
                {
                    return Type == map._full_side && Primary.IsTextured ? Layer.Primary : Layer.Transparent;
                }
            }
        }

        private readonly Dictionary<(short LineIndex, bool IsClockwise), bool> exposedBefore = new Dictionary<(short LineIndex, bool IsClockwise), bool>();

        // Null where the face had no side
        private readonly Dictionary<(short LineIndex, bool IsClockwise), SideBefore> sidesBefore = new Dictionary<(short LineIndex, bool IsClockwise), SideBefore>();

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
            sidesBefore.Clear();
        }

        // As the edit goes on (such as during a drag): the sides' types and textures, as they'll be once it's committed
        // (but with no side added or removed). Returns the lines whose sides changed.
        public HashSet<short> ArrangeSides(MapLevel level)
        {
            var changedLines = new HashSet<short>();

            foreach (var face in sidesBefore)
            {
                if (Arrange(level, face.Key.LineIndex, face.Key.IsClockwise, face.Value, isCommitting: false, changedLines))
                {
                    changedLines.Add(face.Key.LineIndex);
                }
            }

            return changedLines;
        }

        // After the edit. Returns the lines whose sides were added, removed, moved to another index, or changed, which
        // are built again.
        public HashSet<short> ApplyToSides(MapLevel level)
        {
            var changedLines = new HashSet<short>();

            foreach (var face in exposedBefore)
            {
                var (lineIndex, isClockwise) = face.Key;
                var wasExposed = face.Value;

                if (wasExposed || !LevelEntity_Side.IsFaceExposed(level, lineIndex, isClockwise))
                {
                    continue;
                }

                var line = level.LineList[lineIndex];
                var polygonIndex = line.GetPolygonOwner(isClockwise);

                if (line.GetSideIndex(isClockwise) == cstypes.NONE && polygonIndex != cstypes.NONE)
                {
                    map_constructors.new_side(level, polygonIndex, lineIndex);
                    changedLines.Add(lineIndex);
                }
            }

            // Removing a side moves another to its index, so each face's side is found as it is when it's arranged
            foreach (var face in sidesBefore)
            {
                if (Arrange(level, face.Key.LineIndex, face.Key.IsClockwise, face.Value, isCommitting: true, changedLines))
                {
                    changedLines.Add(face.Key.LineIndex);
                }
            }

            return changedLines;
        }

        private void RecordFace(MapLevel level, short lineIndex, bool isClockwise)
        {
            var face = (lineIndex, isClockwise);

            if (exposedBefore.ContainsKey(face))
            {
                return;
            }

            exposedBefore[face] = LevelEntity_Side.IsFaceExposed(level, lineIndex, isClockwise);

            var sideIndex = level.LineList[lineIndex].GetSideIndex(isClockwise);
            if (sideIndex == cstypes.NONE)
            {
                sidesBefore[face] = null;
                return;
            }

            var side = level.SideList[sideIndex];
            sidesBefore[face] = new SideBefore
            {
                Type = side.type,
                Primary = GetSurface(side, Layer.Primary),
                Secondary = GetSurface(side, Layer.Secondary),
                Transparent = GetSurface(side, Layer.Transparent),
                HadPolygonBeyond = level.LineList[lineIndex].GetPolygonOwner(!isClockwise) != cstypes.NONE,
                Geometry = FaceGeometry(level, lineIndex, isClockwise),
            };
        }

        private static (short, short, short, short, short, short, short) FaceGeometry(MapLevel level, short lineIndex, bool isClockwise)
        {
            var line = level.LineList[lineIndex];
            var facingIndex = line.GetPolygonOwner(isClockwise);
            var beyondIndex = line.GetPolygonOwner(!isClockwise);
            var facing = facingIndex != cstypes.NONE ? level.PolygonList[facingIndex] : null;
            var beyond = beyondIndex != cstypes.NONE ? level.PolygonList[beyondIndex] : null;

            return (beyondIndex,
                    facing?.floor_height ?? (short) 0, facing?.ceiling_height ?? (short) 0,
                    beyond?.floor_height ?? (short) 0, beyond?.ceiling_height ?? (short) 0,
                    facing != null ? facing.type : (short) -1,
                    GeometryConstruction.FaceType(level, lineIndex, isClockwise));
        }

        // Returns whether the face's side's type or textures changed (a removed side adds its line, and the line of the
        // side moved to its index, to the changed lines itself)
        private static bool Arrange(MapLevel level, short lineIndex, bool isClockwise, SideBefore before, bool isCommitting, HashSet<short> changedLines)
        {
            var line = level.LineList[lineIndex];
            var sideIndex = line.GetSideIndex(isClockwise);

            // A wall (with nothing beyond it) has no panels or opening
            if (sideIndex == cstypes.NONE || line.GetPolygonOwner(!isClockwise) == cstypes.NONE)
            {
                return false;
            }

            var side = level.SideList[sideIndex];
            var original = side.Clone();

            // As it was where the edit left its polygons' heights as they were (as a drag can go back to them)
            if (before != null && FaceGeometry(level, lineIndex, isClockwise) == before.Geometry)
            {
                side.type = before.Type;
                SetSurface(side, Layer.Primary, before.Primary);
                SetSurface(side, Layer.Secondary, before.Secondary);
                SetSurface(side, Layer.Transparent, before.Transparent);

                return IsChanged(level, side, original);
            }

            var opening = before?.Opening;
            var faceType = GeometryConstruction.FaceType(level, lineIndex, isClockwise);

            if (faceType == map._full_side)
            {
                // A window: a filled one is a full side of what filled it (as its primary, or its transparent texture if
                // that's what filled it), and an open one has nothing to keep
                if (opening != null)
                {
                    side.type = map._full_side;
                    SetSurface(side, before.Type == map._full_side ? before.OpeningLayer : Layer.Primary, opening);
                }
                else if (isCommitting)
                {
                    LevelTopology.RemoveSide(level, lineIndex, isClockwise, changedLines);
                    map_constructors.recalculate_redundant_line_data(level, lineIndex);
                    return true;
                }
                else
                {
                    side.type = map._high_side;
                }
            }
            else
            {
                side.type = faceType;

                // Panels it had keep their textures (textured or not), wherever its type now keeps them
                foreach (var panel in new[] { Panel.Top, Panel.Bottom })
                {
                    var had = before?.PanelSurface(panel);
                    if (had != null && HasPanel(side.type, panel))
                    {
                        SetSurface(side, LayerFor(side.type, panel), had);
                    }
                }

                // The opening keeps what filled it
                if (opening != null)
                {
                    SetSurface(side, Layer.Transparent, opening);
                }

                // Panels it reveals
                foreach (var panel in new[] { Panel.Top, Panel.Bottom })
                {
                    if (!HasPanel(side.type, panel) || (before != null && HasPanel(before.Type, panel)))
                    {
                        continue;
                    }

                    var layer = LayerFor(side.type, panel);

                    // A split side's secondary only draws its step, so a texture left there is the step's from when it
                    // was last there (the primary drew whatever was seen before, so what's left there isn't this panel's)
                    if (layer == Layer.Secondary && GetSurface(side, layer).IsTextured)
                    {
                        continue;
                    }

                    var surface = OwnTexture(before) ??
                                  BesideTexture(level, side, lineIndex, isLeft: true, panel) ??
                                  BesideTexture(level, side, lineIndex, isLeft: false, panel) ??
                                  BeyondTexture(level, line, isClockwise, panel);

                    if (surface != null)
                    {
                        SetSurface(side, layer, surface);
                    }
                }
            }

            return IsChanged(level, side, original);
        }

        // Whether its type or textures differ from the original's (its line's flags following them if they do)
        private static bool IsChanged(MapLevel level, side_data side, side_data original)
        {
            var changed = side.type != original.type ||
                          !SameSurface(GetSurface(side, Layer.Primary), GetSurface(original, Layer.Primary)) ||
                          !SameSurface(GetSurface(side, Layer.Secondary), GetSurface(original, Layer.Secondary)) ||
                          !SameSurface(GetSurface(side, Layer.Transparent), GetSurface(original, Layer.Transparent));

            if (changed)
            {
                LineFlagsEditing.UpdateForSide(level, side);
            }

            return changed;
        }

        private static bool SameSurface(Surface a, Surface b)
        {
            return a.Texture.texture == b.Texture.texture && a.Texture.x0 == b.Texture.x0 && a.Texture.y0 == b.Texture.y0 &&
                   a.Light == b.Light && a.TransferMode == b.TransferMode;
        }

        // Its own texture before the edit (primary first), if it had one
        private static Surface OwnTexture(SideBefore before)
        {
            if (before == null)
            {
                return null;
            }

            foreach (var surface in new[] { before.Primary, before.Secondary, before.Transparent })
            {
                if (surface.IsTextured)
                {
                    return surface;
                }
            }

            return null;
        }

        // The side of its polygon beside it, on its left or right as it's seen from its polygon: the polygon's corners go
        // clockwise (as the map shows them), so its previous edge is on the left, and its next on the right. Its texture
        // for the same panel, or else its primary.
        private static Surface BesideTexture(MapLevel level, side_data side, short lineIndex, bool isLeft, Panel panel)
        {
            if (side.polygon_index < 0 || side.polygon_index >= level.PolygonList.Count)
            {
                return null;
            }

            var polygon = level.PolygonList[side.polygon_index];
            var count = polygon.vertex_count;
            var edge = System.Array.IndexOf(polygon.line_indexes, lineIndex, 0, count);
            if (edge < 0)
            {
                return null;
            }

            var besideLineIndex = polygon.line_indexes[(edge + (isLeft ? count - 1 : 1)) % count];
            var besideLine = level.LineList[besideLineIndex];
            var besideSideIndex = besideLine.GetSideIndex(besideLine.clockwise_polygon_owner == side.polygon_index);
            if (besideSideIndex == cstypes.NONE)
            {
                return null;
            }

            var beside = level.SideList[besideSideIndex];
            if (HasPanel(beside.type, panel))
            {
                var surface = GetSurface(beside, LayerFor(beside.type, panel));
                if (surface.IsTextured)
                {
                    return surface;
                }
            }

            var primary = GetSurface(beside, Layer.Primary);

            return primary.IsTextured ? primary : null;
        }

        // The polygon beyond the line: its floor for a step up to it, or its ceiling for a drop down to it
        private static Surface BeyondTexture(MapLevel level, line_data line, bool isClockwise, Panel panel)
        {
            var beyondIndex = line.GetPolygonOwner(!isClockwise);
            if (beyondIndex == cstypes.NONE)
            {
                return null;
            }

            var beyond = level.PolygonList[beyondIndex];
            var isFloor = panel == Panel.Bottom;
            var texture = isFloor ? beyond.floor_texture : beyond.ceiling_texture;

            if (texture == cstypes.UNONE)
            {
                return null;
            }

            return new Surface
            {
                Texture = new side_texture_definition { texture = texture },
                Light = isFloor ? beyond.floor_lightsource_index : beyond.ceiling_lightsource_index,
                TransferMode = LevelEditing.TransferModeForTexture(texture, isFloor ? beyond.floor_transfer_mode : beyond.ceiling_transfer_mode),
            };
        }

        // Which panels a side's type draws (RenderRasterize.cpp: render_node)
        private static bool HasPanel(short type, Panel panel)
        {
            return panel == Panel.Top ?
                   type == map._high_side || type == map._split_side :
                   type == map._low_side || type == map._split_side;
        }

        // A split side's step is its secondary texture; otherwise each panel is its primary
        private static Layer LayerFor(short type, Panel panel)
        {
            return panel == Panel.Bottom && type == map._split_side ? Layer.Secondary : Layer.Primary;
        }

        private static Surface GetSurface(side_data side, Layer layer)
        {
            switch (layer)
            {
                case Layer.Primary:
                    return new Surface { Texture = side.primary_texture.Clone(), Light = side.primary_lightsource_index, TransferMode = side.primary_transfer_mode };
                case Layer.Secondary:
                    return new Surface { Texture = side.secondary_texture.Clone(), Light = side.secondary_lightsource_index, TransferMode = side.secondary_transfer_mode };
                default:
                    return new Surface { Texture = side.transparent_texture.Clone(), Light = side.transparent_lightsource_index, TransferMode = side.transparent_transfer_mode };
            }
        }

        private static void SetSurface(side_data side, Layer layer, Surface surface)
        {
            switch (layer)
            {
                case Layer.Primary:
                    side.primary_texture = surface.Texture.Clone();
                    side.primary_lightsource_index = surface.Light;
                    side.primary_transfer_mode = surface.TransferMode;
                    break;
                case Layer.Secondary:
                    side.secondary_texture = surface.Texture.Clone();
                    side.secondary_lightsource_index = surface.Light;
                    side.secondary_transfer_mode = surface.TransferMode;
                    break;
                default:
                    side.transparent_texture = surface.Texture.Clone();
                    side.transparent_lightsource_index = surface.Light;
                    side.transparent_transfer_mode = surface.TransferMode;
                    break;
            }
        }
    }
}
#endif
