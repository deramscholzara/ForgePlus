using AlephOne;
using ForgePlus.Entities.Geometry;
using ForgePlus.Palette;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System.Collections.Generic;
using System.Linq;

namespace ForgePlus.LevelManipulation
{
    // Switching modes selects the new mode's equivalent of what was selected (or of the face it was clicked on). In
    // Terminals mode, the sides that show the selected terminal are shown as selected.
    public partial class SelectionManager
    {
        // A selection, and what it's on in the level
        private sealed class SelectionContext
        {
            // Null for a terminal's side that was clicked
            public ISelectable Selection;

            // The surface it was clicked on, or else its polygon's or side's first
            public EditableSurface_Base Surface;

            public LevelEntity_Polygon Polygon;
            public LevelEntity_Side Side;
            public LevelEntity_Line Line;
            public LevelEntity_Light Light;
            public LevelEntity_Media Media;
            public LevelEntity_Platform Platform;
            public LevelEntity_MapObject MapObject;
            public LevelEntity_Annotation Annotation;
            public short TerminalIndex = cstypes.NONE;
        }

        private readonly List<SelectionContext> carriedSelection = new List<SelectionContext>();
        private readonly List<LevelEntity_Side> shownTerminalSides = new List<LevelEntity_Side>();

        private ModeManager.PrimaryModes carriedSelectionMode = ModeManager.PrimaryModes.None;

        // Set before the click selects anything
        public EditableSurface_Base ClickedSurface { get; set; }

        // Before the mode changes (and anything, such as the palette, changes the selection for the new mode)
        private void CollectCarriedSelection(ModeManager.PrimaryModes newMode)
        {
            var previousMode = ModeManager.Instance.PrimaryMode;
            carriedSelectionMode = newMode;

            carriedSelection.Clear();

            if (!LevelEntity_Level.Instance || newMode == ModeManager.PrimaryModes.None)
            {
                return;
            }

            foreach (var selection in SelectedObjects)
            {
                var context = GetContext(selection);
                if (context != null)
                {
                    carriedSelection.Add(context);
                }
            }

            // Terminals mode selects nothing, but the terminal's side that was clicked is carried
            if (previousMode == ModeManager.PrimaryModes.Terminals &&
                ClickedSurface is EditableSurface_Side clickedSide &&
                clickedSide &&
                clickedSide.ParentSide.TryGetTerminalIndex(out var terminalIndex) &&
                terminalIndex == ForgePlusUI.Instance.Terminals.TerminalIndex)
            {
                var context = GetContext(clickedSide.ParentSide);
                context.Selection = null;
                carriedSelection.Add(context);
            }
        }

        private void SelectCarriedSelection()
        {
            if (carriedSelection.Count == 0)
            {
                return;
            }

            var contexts = carriedSelection.ToList();
            carriedSelection.Clear();

            var primaryMode = ModeManager.Instance.PrimaryMode;
            if (!LevelEntity_Level.Instance || primaryMode != carriedSelectionMode)
            {
                return;
            }

            // Terminals mode shows a terminal instead of selecting anything
            if (primaryMode == ModeManager.PrimaryModes.Terminals)
            {
                var terminalContext = contexts.FirstOrDefault(context => context.TerminalIndex >= 0);
                if (terminalContext != null)
                {
                    ClickedSurface = terminalContext.Surface;
                    ForgePlusUI.Instance.ShowTerminal(terminalContext.TerminalIndex);
                }

                return;
            }

            var targets = new List<(ISelectable Target, SelectionContext Context)>();
            foreach (var context in contexts)
            {
                var target = GetEquivalent(primaryMode, context);
                if (target != null && !targets.Any(existing => existing.Target == target))
                {
                    targets.Add((target, context));
                }
            }

            for (var i = 0; i < targets.Count; i++)
            {
                SelectObject(targets[i].Target, multiSelect: i > 0);
            }

            // One selection is shown as clicking it would show it
            if (targets.Count == 1)
            {
                ShowAsClicked(primaryMode, targets[0].Target, targets[0].Context);
            }
        }

        // The selection itself where the mode selects it, otherwise its equivalent there (or null)
        private static ISelectable GetEquivalent(ModeManager.PrimaryModes primaryMode, SelectionContext context)
        {
            switch (primaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                case ModeManager.PrimaryModes.Textures:
                    if (context.Selection is LevelEntity_Polygon ||
                        context.Selection is LevelEntity_Side ||
                        (context.Selection is LevelEntity_Line && primaryMode == ModeManager.PrimaryModes.Geometry))
                    {
                        return context.Selection;
                    }

                    var side = ShownSide(context.Side);
                    if (side)
                    {
                        return side;
                    }

                    return context.Polygon;
                case ModeManager.PrimaryModes.Lights:
                    return context.Light;
                case ModeManager.PrimaryModes.Media:
                    return context.Media;
                case ModeManager.PrimaryModes.Sounds:
                    // Sound sources are the only objects selected here
                    if (context.MapObject && context.MapObject.NativeObject.type == map._saved_sound_source)
                    {
                        return context.MapObject;
                    }

                    return context.Polygon;
                case ModeManager.PrimaryModes.Heights:
                    return HeightsEditing.CanEdit(context.Polygon) ? context.Polygon : null;
                case ModeManager.PrimaryModes.Platforms:
                    return context.Platform;
                case ModeManager.PrimaryModes.Objects:
                    return context.MapObject;
                case ModeManager.PrimaryModes.Annotations:
                    return context.Annotation;
                default:
                    // Level mode selects the level, and Map mode selects nothing
                    return null;
            }
        }

        // Null for a placeholder while placeholders are hidden
        private static LevelEntity_Side ShownSide(LevelEntity_Side side)
        {
            return side && (side.NativeObject != null || LevelEntity_Side.PlaceholdersAreVisible) ? side : null;
        }

        // The palette's swatch for what the target is painted with, and (in Textures mode) nudging its face's texture
        private void ShowAsClicked(ModeManager.PrimaryModes primaryMode, ISelectable target, SelectionContext context)
        {
            var surface = GetSurface(target, context);
            if (surface)
            {
                ClickedSurface = surface;
            }

            switch (primaryMode)
            {
                case ModeManager.PrimaryModes.Textures:
                    if (surface)
                    {
                        surface.InputListener(target);
                        PaletteManager.Instance.SelectSwatchForTexture(surface.SurfaceShapeDescriptor);
                    }

                    break;
                case ModeManager.PrimaryModes.Lights:
                    PaletteManager.Instance.SelectSwatchForLight(target as LevelEntity_Light);
                    break;
                case ModeManager.PrimaryModes.Media:
                    PaletteManager.Instance.SelectSwatchForMedia(target as LevelEntity_Media);
                    break;
                case ModeManager.PrimaryModes.Heights:
                    if (surface is EditableSurface_Polygon polygonSurface)
                    {
                        PaletteManager.Instance.SelectSwatchForHeight(polygonSurface.ParentPolygon, polygonSurface.DataSource);
                    }

                    break;
            }

            ShowSelectedFace();
        }

        // The face the selection was on, if it's the target's, or else the target's first
        private static EditableSurface_Base GetSurface(ISelectable target, SelectionContext context)
        {
            switch (target)
            {
                case LevelEntity_Polygon polygon:
                    if (context.Surface is EditableSurface_Polygon polygonSurface && polygonSurface && polygonSurface.ParentPolygon == polygon)
                    {
                        return polygonSurface;
                    }

                    return GetPolygonSurface(polygon, LevelEntity_Polygon.DataSources.Floor);
                case LevelEntity_Side side:
                    if (context.Surface is EditableSurface_Side sideSurface && sideSurface && sideSurface.ParentSide == side)
                    {
                        return sideSurface;
                    }

                    return GetSideSurface(side);
                default:
                    return context.Surface;
            }
        }

        private SelectionContext GetContext(ISelectable selection)
        {
            var context = new SelectionContext { Selection = selection };

            switch (selection)
            {
                case LevelEntity_Polygon polygon:
                    context.Polygon = polygon;
                    break;
                case LevelEntity_Side side:
                    context.Side = side;
                    break;
                case LevelEntity_Line line:
                    context.Line = line;
                    break;
                case LevelEntity_Light light:
                    context.Light = light;
                    break;
                case LevelEntity_Media media:
                    context.Media = media;
                    break;
                case LevelEntity_Platform platform:
                    context.Platform = platform.SelectablePlatform;
                    break;
                case LevelEntity_MapObject mapObject:
                    context.MapObject = mapObject;
                    break;
                case LevelEntity_Annotation annotation:
                    context.Annotation = annotation;
                    break;
                default:
                    // The level and the sound entries have no equivalent in other modes
                    return null;
            }

            // The surface clicked last, if the selection is on it (a light is on any face it lights)
            var clickedContext = GetSurfaceContext(ClickedSurface);
            if (clickedContext != null && IsOn(context, clickedContext))
            {
                context.Surface = clickedContext.Surface;
                context.Light = context.Light ?? clickedContext.Light;

                if (!context.Polygon)
                {
                    context.Polygon = clickedContext.Polygon;
                }

                if (!context.Side)
                {
                    context.Side = clickedContext.Side;
                }

                if (!context.Platform)
                {
                    context.Platform = clickedContext.Platform;
                }
            }

            Complete(context);

            return context;
        }

        private static SelectionContext GetSurfaceContext(EditableSurface_Base surface)
        {
            SelectionContext context;

            switch (surface)
            {
                case EditableSurface_Polygon polygonSurface when polygonSurface:
                    context = new SelectionContext
                    {
                        Polygon = polygonSurface.ParentPolygon,
                        Platform = polygonSurface.Platform ? polygonSurface.Platform.SelectablePlatform : null,
                    };
                    break;
                case EditableSurface_Side sideSurface when sideSurface:
                    context = new SelectionContext
                    {
                        Side = sideSurface.ParentSide,
                        Platform = sideSurface.Platform ? sideSurface.Platform.SelectablePlatform : null,
                    };
                    break;
                case EditableSurface_Media mediaSurface when mediaSurface:
                    context = new SelectionContext
                    {
                        Polygon = mediaSurface.Polygon,
                    };
                    break;
                default:
                    return null;
            }

            context.Surface = surface;
            Complete(context);

            return context;
        }

        // Whether the selection is the face's, or on it (a platform is on its polygon's floor or ceiling, not on the sides
        // that move with it)
        private static bool IsOn(SelectionContext selection, SelectionContext face)
        {
            return (selection.Polygon && selection.Polygon == face.Polygon) ||
                   (selection.Side && selection.Side == face.Side) ||
                   (selection.Light != null && selection.Light == face.Light) ||
                   (selection.Media != null && selection.Media == face.Media) ||
                   (selection.Platform && selection.Platform == face.Platform && !face.Side);
        }

        // Fills in what the selection is on from what it is
        private static void Complete(SelectionContext context)
        {
            var level = LevelEntity_Level.Instance;

            // A line is on its first shown side
            if (context.Line && !context.Side)
            {
                context.Side = ShownSide(context.Line.ClockwiseSide);

                if (!context.Side)
                {
                    context.Side = ShownSide(context.Line.CounterclockwiseSide);
                }
            }

            // A side is on the polygon it faces into
            if (context.Side)
            {
                if (!context.Line && level.Lines.TryGetValue(context.Side.ParentLineIndex, out var line))
                {
                    context.Line = line;
                }

                if (!context.Polygon)
                {
                    context.Polygon = context.Side.FacingPolygon;
                }

                if (context.Side.TryGetTerminalIndex(out var terminalIndex))
                {
                    context.TerminalIndex = terminalIndex;
                }
            }

            // Platforms, objects and annotations are their polygons'
            if (!context.Polygon)
            {
                var polygonIndex = context.Platform ? context.Platform.NativeObject.polygon_index :
                                   context.MapObject ? context.MapObject.NativeObject.polygon_index :
                                   context.Annotation ? context.Annotation.NativeObject.polygon_index :
                                   cstypes.NONE;

                if (level.Polygons.TryGetValue(polygonIndex, out var polygon))
                {
                    context.Polygon = polygon;
                }
            }

            if (context.Polygon)
            {
                var polygon = context.Polygon;

                if (!context.Platform && polygon.NativeObject.type == map._polygon_is_platform)
                {
                    context.Platform = LevelEntity_Platform.GetSelectablePlatform(level, polygon.NativeObject.permutation);
                }

                context.Media = context.Media ?? polygon.Media;

                if (!context.Annotation)
                {
                    context.Annotation = level.Annotations.OrderBy(entry => entry.Key)
                                                          .Select(entry => entry.Value)
                                                          .FirstOrDefault(annotation => annotation.NativeObject.polygon_index == polygon.NativeIndex);
                }
            }

            // A side's face, or its polygon's floor (or ceiling, for an object hanging from it or a platform that only
            // comes from it)
            if (!context.Surface)
            {
                if (context.Side)
                {
                    context.Surface = GetSideSurface(context.Side);
                }
                else if (context.Polygon)
                {
                    var isOnCeiling = (context.MapObject && (context.MapObject.NativeObject.flags & map._map_object_hanging_from_ceiling) != 0) ||
                                      (context.Platform && !level.FloorPlatforms.ContainsKey(context.Platform.NativeIndex));

                    context.Surface = GetPolygonSurface(context.Polygon, isOnCeiling ? LevelEntity_Polygon.DataSources.Ceiling : LevelEntity_Polygon.DataSources.Floor);
                }
            }

            if (context.Light == null && context.Surface)
            {
                context.Light = context.Surface.RuntimeLight;
            }
        }

        // The media surface of the polygon a face is in (a side's, the one it faces into)
        private static EditableSurface_Media GetSurfaceMediaSurface(EditableSurface_Base surface)
        {
            LevelEntity_Polygon polygon;
            switch (surface)
            {
                case EditableSurface_Media mediaSurface when mediaSurface:
                    return mediaSurface;
                case EditableSurface_Polygon polygonSurface when polygonSurface:
                    polygon = polygonSurface.ParentPolygon;
                    break;
                case EditableSurface_Side sideSurface when sideSurface:
                    polygon = sideSurface.ParentSide.FacingPolygon;
                    break;
                default:
                    return null;
            }

            return polygon && polygon.MediaSurface ? polygon.MediaSurface.GetComponent<EditableSurface_Media>() : null;
        }

        private static EditableSurface_Polygon GetPolygonSurface(LevelEntity_Polygon polygon, LevelEntity_Polygon.DataSources dataSource)
        {
            return LevelEntity_Level.Instance.EditableSurface_Polygons.FirstOrDefault(surface => surface.ParentPolygon == polygon && surface.DataSource == dataSource);
        }

        // Its primary surface, if it shows one, or else its first
        private static EditableSurface_Side GetSideSurface(LevelEntity_Side side)
        {
            var surfaces = LevelEntity_Level.Instance.EditableSurface_Sides.Where(surface => surface.ParentSide == side).ToList();

            var primarySurface = surfaces.FirstOrDefault(surface => surface.DataSource == LevelEntity_Side.DataSources.Primary);
            if (primarySurface)
            {
                return primarySurface;
            }

            return surfaces.FirstOrDefault();
        }

        // In Terminals mode, shows the sides that show the selected terminal as selected (without selecting them)
        public void ShowTerminalSides()
        {
            HideTerminalSides();

            var level = LevelEntity_Level.Instance;
            var terminals = ForgePlusUI.Instance ? ForgePlusUI.Instance.Terminals : null;

            if (!level || terminals == null || terminals.TerminalIndex < 0 || ModeManager.Instance.PrimaryMode != ModeManager.PrimaryModes.Terminals)
            {
                return;
            }

            foreach (var side in level.Sides.Values)
            {
                if (side.TryGetTerminalIndex(out var terminalIndex) && terminalIndex == terminals.TerminalIndex)
                {
                    side.DisplaySelectionState(true);
                    shownTerminalSides.Add(side);
                }
            }
        }

        private void HideTerminalSides()
        {
            foreach (var side in shownTerminalSides)
            {
                // Not those of a level that's been closed
                if (side)
                {
                    side.DisplaySelectionState(false);
                }
            }

            shownTerminalSides.Clear();
        }
    }
}
