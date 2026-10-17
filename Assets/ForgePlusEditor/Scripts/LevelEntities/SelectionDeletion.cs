#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.Entities.Geometry;
using ForgePlus.Extensions;
using ForgePlus.History;
using ForgePlus.Inspection;
using ForgePlus.Palette;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System.Collections.Generic;
using System.Linq;

namespace ForgePlus.LevelManipulation
{
    // Delete Selected (Delete or Backspace) deletes or clears what's selected, as the mode has it, as one action:
    // - Geometry: deletes the selected polygons, points and lines (GeometryDeletion)
    // - Objects: deletes the selected objects
    // - Annotations: deletes the selected annotations
    // - Sounds: clears the selected polygons' ambient and random sounds
    // - Media: clears the media of the polygon it was selected in (its media becomes none)
    // - Lights: sets the face it was selected on to light 0
    // - Platforms: makes the selected platforms' polygons normal ones
    // - Textures: clears the selected surfaces' textures (ClearTextures)
    public static class SelectionDeletion
    {
        // Once a frame, from ForgePlusUI
        public static void Update()
        {
            if (!Hotkeys.WasPressed(ForgePlusInput.Editing.DeleteSelected) || Hotkeys.IsShortcutModifierPressed)
            {
                return;
            }

            var level = LevelEntity_Level.Instance;
            if (!level || LevelEditing.IsRebuildPending)
            {
                return;
            }

            var selection = SelectionManager.Instance.Selection.ToList();

            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    GeometryDeletion.DeleteSelection(level);
                    break;
                case ModeManager.PrimaryModes.Objects:
                    DeleteObjects(level, selection.OfType<LevelEntity_MapObject>().Select(mapObject => mapObject.NativeObject).ToList(),
                                  new List<map_annotation>());
                    break;
                case ModeManager.PrimaryModes.Annotations:
                    DeleteObjects(level, new List<map_object>(),
                                  selection.OfType<LevelEntity_Annotation>().Select(annotation => annotation.NativeObject).ToList());
                    break;
                case ModeManager.PrimaryModes.Sounds:
                    ClearSounds(selection);
                    break;
                case ModeManager.PrimaryModes.Media:
                    ClearMedia();
                    break;
                case ModeManager.PrimaryModes.Lights:
                    ClearLight(level);
                    break;
                case ModeManager.PrimaryModes.Platforms:
                    ClearPlatforms(level, selection.OfType<LevelEntity_Platform>().ToList());
                    break;
                case ModeManager.PrimaryModes.Textures:
                    ClearTextures(selection);
                    break;
            }
        }

        // Each selected polygon's and side's face takes no texture, as painting "None" does: the face it was clicked on, and
        // the same face of the rest of the selection (a polygon's floor or ceiling, or a side's primary, secondary or
        // transparent texture, its outer layer while Target Outer Layer is held), or else a polygon's floor and a side's
        // primary. A placeholder side has no texture to clear, and a control panel's primary texture is set by its panel.
        private static void ClearTextures(List<ISelectable> selection)
        {
            var clicked = SelectionManager.Instance.ClickedSurface;
            var polygonSource = clicked is EditableSurface_Polygon clickedPolygon && clickedPolygon ?
                                clickedPolygon.DataSource :
                                LevelEntity_Polygon.DataSources.Floor;
            var sideSource = clicked is EditableSurface_Side clickedSide && clickedSide ?
                             clickedSide.DataSource :
                             LevelEntity_Side.DataSources.Primary;

            var cleared = false;

            foreach (var polygon in selection.OfType<LevelEntity_Polygon>())
            {
                polygon.SetShapeDescriptor(polygonSource, cstypes.UNONE);
                cleared = true;
            }

            foreach (var side in selection.OfType<LevelEntity_Side>())
            {
                if (side.NativeObject == null)
                {
                    continue;
                }

                var dataSource = sideSource;
                if (side.NativeObject.HasLayeredTransparentSide(side.ParentLevel.Level) && Hotkeys.IsPressed(ForgePlusInput.Editing.TargetOuterLayer))
                {
                    dataSource = LevelEntity_Side.DataSources.Transparent;
                }

                if (dataSource == LevelEntity_Side.DataSources.Primary && side.PanelSetsPrimaryTexture)
                {
                    continue;
                }

                side.SetShapeDescriptor(dataSource, cstypes.UNONE);
                cleared = true;
            }

            if (!cleared)
            {
                return;
            }

            LevelHistory.MarkEdited();
            PaletteManager.Instance.SelectSwatchForTexture(cstypes.UNONE);
            InspectorPanel.Instance.RefreshAllInspectors();
        }

        // Removing them renumbers those after them, so the level's rebuilt (with nothing selected)
        private static void DeleteObjects(LevelEntity_Level level, List<map_object> mapObjects, List<map_annotation> annotations)
        {
            if (mapObjects.Count == 0 && annotations.Count == 0)
            {
                return;
            }

            SelectionManager.Instance.DeselectAll();

            var data = level.Level;
            data.SavedObjectList.RemoveAll(mapObjects.Contains);
            data.MapAnnotationList.RemoveAll(annotations.Contains);

            LevelEditing.RebuildLevel();
        }

        // The polygons play no sounds
        private static void ClearSounds(List<ISelectable> selection)
        {
            foreach (var polygon in selection.OfType<LevelEntity_Polygon>())
            {
                foreach (var kind in SoundImageEditing.Kinds)
                {
                    SoundImageEditing.Assign(polygon, kind, cstypes.NONE);
                }
            }

            LevelHistory.MarkEdited();
        }

        // The selected media's face is the media surface of the polygon it was clicked in
        private static void ClearMedia()
        {
            if (!(SelectionManager.Instance.SelectedFace is EditableSurface_Media mediaSurface) || !mediaSurface.Polygon)
            {
                return;
            }

            var polygon = mediaSurface.Polygon;

            SelectionManager.Instance.DeselectAll();
            PaletteManager.Instance.SelectSwatchForMedia(null);

            polygon.SetMedia(null);
            LevelHistory.MarkEdited();
            InspectorPanel.Instance.RefreshAllInspectors();
        }

        // The face the selected light was clicked on takes light 0, which is then selected (as clicking the face would)
        private static void ClearLight(LevelEntity_Level level)
        {
            var face = SelectionManager.Instance.SelectedFace;
            if (!face || !level.Lights.TryGetValue(0, out var firstLight) || face.RuntimeLight == firstLight)
            {
                return;
            }

            switch (face)
            {
                case EditableSurface_Polygon polygonSurface:
                    polygonSurface.ParentPolygon.SetLight(polygonSurface.DataSource, 0);
                    break;
                case EditableSurface_Side sideSurface:
                    sideSurface.ParentSide.SetLight(sideSurface.DataSource, 0);
                    break;
                case EditableSurface_Media mediaSurface:
                    mediaSurface.Polygon.SetLight(LevelEntity_Polygon.DataSources.Media, 0);
                    break;
                default:
                    return;
            }

            LevelHistory.MarkEdited();

            SelectionManager.Instance.ToggleSelectionOnFace(firstLight);
            PaletteManager.Instance.SelectSwatchForLight(firstLight);
            InspectorPanel.Instance.RefreshAllInspectors();
        }

        // Their polygons become normal ones (both halves of a split platform are one platform), and the level's rebuilt
        private static void ClearPlatforms(LevelEntity_Level level, List<LevelEntity_Platform> selected)
        {
            var data = level.Level;
            var polygons = selected.Select(platform => platform.NativeObject.polygon_index)
                                   .Distinct()
                                   .Select(polygonIndex => data.PolygonList[polygonIndex])
                                   .ToList();

            if (polygons.Count == 0)
            {
                return;
            }

            SelectionManager.Instance.DeselectAll();

            // Each removal renumbers the platforms after it, so each is found by its polygon as it goes
            foreach (var polygon in polygons)
            {
                var polygonIndex = (short) data.PolygonList.IndexOf(polygon);
                if (polygon.type == map._polygon_is_platform)
                {
                    PlatformEditing.RemovePlatformData(data, polygonIndex, map._polygon_is_normal);
                }
            }

            LevelEditing.RebuildLevel();
        }
    }
}
#endif
