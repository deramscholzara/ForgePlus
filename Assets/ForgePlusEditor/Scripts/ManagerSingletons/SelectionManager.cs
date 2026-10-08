using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.Entities.Geometry;
using ForgePlus.Inspection;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    [AutoStaticsCleanup]
    public partial class SelectionManager : SingletonMonoBehaviour<SelectionManager>
    {
        // TODO: Add selection subfilters here

        public static int DefaultLayer;
        public static int SelectionIndicatorLayer;

        public event Action OnClickEmptySpace;

        public event Action OnSelectionChanged;

        private readonly List<ISelectable> SelectedObjects = new List<ISelectable>(500);

        // The face shown as selected for what's selected through it
        private EditableSurface_Base shownFace;

        public ISelectable SelectedObject
        {
            get
            {
                if (SelectedObjects.Count == 0)
                {
                    return null;
                }

                return SelectedObjects[0];
            }
        }

        public IReadOnlyList<ISelectable> Selection
        {
            get
            {
                return SelectedObjects;
            }
        }

        public void UpdateSelectionToMatchMode(ModeManager.PrimaryModes primaryMode)
        {
            HideTerminalSides();

            // A level opened or closed has none of the previous selection to select again
            if (primaryMode != carriedSelectionMode)
            {
                carriedSelection.Clear();
            }

            DeselectAll();

            var level = LevelEntity_Level.Instance;
            if (level)
            {
                var selectsGeometry = SelectsGeometry(primaryMode);

                SetSelectability<LevelEntity_Polygon>(level.Polygons.Values, enabled: selectsGeometry);
                SetSelectability<LevelEntity_Line>(level.Lines.Values, enabled: primaryMode == ModeManager.PrimaryModes.Geometry);
                SetSelectability<LevelEntity_Side>(level.Sides.Values, enabled: selectsGeometry);
                SetSelectability<LevelEntity_Side>(level.PlaceholderSides, enabled: selectsGeometry);
                SetSelectability<LevelEntity_Light>(level.Lights.Values, enabled: primaryMode == ModeManager.PrimaryModes.Lights);
                SetSelectability<LevelEntity_Media>(level.Medias.Values, enabled: primaryMode == ModeManager.PrimaryModes.Media);
                SetSelectability<LevelEntity_Platform>(level.CeilingPlatforms.Values, enabled: primaryMode == ModeManager.PrimaryModes.Platforms);
                SetSelectability<LevelEntity_Platform>(level.FloorPlatforms.Values, enabled: primaryMode == ModeManager.PrimaryModes.Platforms);
                SetSelectability<LevelEntity_Annotation>(level.Annotations.Values, enabled: primaryMode == ModeManager.PrimaryModes.Annotations);
                SetSelectability<LevelEntity_Level>(level, enabled: primaryMode == ModeManager.PrimaryModes.Level);

                // Sound sources are the only objects selected in Sounds mode
                foreach (var mapObject in level.MapObjects.Values)
                {
                    var isSelectable = primaryMode == ModeManager.PrimaryModes.Objects ||
                                       (primaryMode == ModeManager.PrimaryModes.Sounds && mapObject.NativeObject.type == map._saved_sound_source);

                    SetSelectability<LevelEntity_MapObject>(mapObject, enabled: isSelectable);
                }

                foreach (var polygonSurface in level.EditableSurface_Polygons)
                {
                    SetSelectability<EditableSurface_Polygon>(polygonSurface, enabled: PolygonSurfaceIsSelectable(primaryMode, polygonSurface));
                }

                SetSelectability<EditableSurface_Side>(level.EditableSurface_Sides, enabled: SideSurfacesAreSelectable(primaryMode));
                SetSelectability<EditableSurface_Media>(level.EditableSurface_Medias, enabled: MediaSurfacesAreSelectable(primaryMode));

                // Select the level here, since there's no visual way to select it besides the mode button
                if (primaryMode == ModeManager.PrimaryModes.Level)
                {
                    SelectObject(level, multiSelect: false);
                }
            }

            ShowTerminalSides();
        }

        public bool GetIsSelected(ISelectable selectable)
        {
            return SelectedObjects.Contains(selectable);
        }

        public void ToggleObjectSelection(ISelectable selection, bool multiSelect = false)
        {
            if (SelectedObjects.Contains(selection))
            {
                DeselectObject(selection, multiSelect);
            }
            else
            {
                SelectObject(selection, multiSelect);
            }
        }

        public void SelectObject(ISelectable selection, bool multiSelect = false)
        {
            if (!SelectedObjects.Contains(selection))
            {
                InspectorPanel.Instance.ClearAllInspectors();

                if (!multiSelect)
                {
                    // The new selection is inspected instead
                    DeselectAll(inspectNothingSelected: false);
                }

                // 1. Update displayed selection
                if (selection is ISelectionDisplayable)
                {
                    (selection as ISelectionDisplayable).DisplaySelectionState(true);
                }

                // 2. Update actual selection list
                SelectedObjects.Add(selection);

                if (SelectedObjects.Count == 1)
                {
                    // 3. Inspect selection
                    (selection as IInspectable).Inspect();
                }

                OnSelectionChanged?.Invoke();
            }
        }

        public void DeselectObject(ISelectable selection, bool multiSelect = false)
        {
            if (SelectedObjects.Contains(selection))
            {
                InspectorPanel.Instance.ClearAllInspectors();

                if (multiSelect || SelectedObjects.Count == 1)
                {
                    // 1. Update displayed selection
                    if (selection is ISelectionDisplayable)
                    {
                        (selection as ISelectionDisplayable).DisplaySelectionState(false);
                    }

                    // 2. Update actual selection list
                    SelectedObjects.Remove(selection);
                }
                else
                {
                    // When single-deselecting with multiple selections,
                    // deselect everything else, instead - better for UX
                    foreach (var selectedObject in SelectedObjects)
                    {
                        if (selectedObject != selection)
                        {
                            // 1. Update displayed selection
                            if (selectedObject is ISelectionDisplayable)
                            {
                                (selectedObject as ISelectionDisplayable).DisplaySelectionState(false);
                            }
                        }
                    }

                    // 2. Update actual selection list
                    SelectedObjects.RemoveAll(selectedObject => selectedObject != selection);
                }

                if (SelectedObjects.Count == 1)
                {
                    // 3. Inspect selection
                    (selection as IInspectable).Inspect();
                }
                else if (SelectedObjects.Count == 0)
                {
                    InspectNothingSelected();
                }

                OnSelectionChanged?.Invoke();
            }
        }

        public void DeselectAll(bool inspectNothingSelected = true)
        {
            InspectorPanel.Instance.ClearAllInspectors();

            foreach (var selectedObject in SelectedObjects)
            {
                // 1. Update displayed selection
                if (selectedObject is ISelectionDisplayable)
                {
                    (selectedObject as ISelectionDisplayable).DisplaySelectionState(false);
                }
            }

            // 2. Update actual selection list
            SelectedObjects.Clear();

            if (inspectNothingSelected)
            {
                InspectNothingSelected();
            }

            OnSelectionChanged?.Invoke();
        }

        // With nothing selected, Objects mode inspects the level's item and monster placement
        private void InspectNothingSelected()
        {
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Objects && LevelEntity_Level.Instance)
            {
                InspectorPanel.Instance.AddInspector(new Inspector_Placements(LevelEntity_Level.Instance));
            }
        }

        // Clicking the shown face again deselects the selection, and clicking another of its faces shows that one instead
        public void ToggleSelectionOnFace(ISelectable selection)
        {
            if (SelectedObjects.Count == 1 && SelectedObjects[0] == selection)
            {
                var face = GetSelectedFace();
                if (face && face != shownFace)
                {
                    ShowSelectedFace();
                    return;
                }
            }

            ToggleObjectSelection(selection, multiSelect: false);
        }

        // The face the selected light was clicked on, or the media surface of the polygon the selected media was clicked in
        private EditableSurface_Base GetSelectedFace()
        {
            if (SelectedObjects.Count != 1 || !ClickedSurface)
            {
                return null;
            }

            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Lights:
                    return SelectedObjects[0] is LevelEntity_Light selectedLight && ClickedSurface.RuntimeLight == selectedLight ?
                           ClickedSurface :
                           null;
                case ModeManager.PrimaryModes.Media:
                    var mediaSurface = GetSurfaceMediaSurface(ClickedSurface);
                    return SelectedObjects[0] is LevelEntity_Media selectedMedia && mediaSurface && mediaSurface.Polygon.Media == selectedMedia ?
                           mediaSurface :
                           null;
                default:
                    return null;
            }
        }

        private void ShowSelectedFace()
        {
            var face = GetSelectedFace();

            if (face == shownFace)
            {
                return;
            }

            // Not one of a level that's been closed
            if (shownFace)
            {
                shownFace.DisplayFaceSelectionState(false);
            }

            shownFace = face;

            if (shownFace)
            {
                shownFace.DisplayFaceSelectionState(true);
            }
        }

        // For a surface made while in a mode (such as by painting media)
        public void MatchSelectabilityToMode(EditableSurface_Media surface)
        {
            surface.SetSelectability(MediaSurfacesAreSelectable(ModeManager.Instance.PrimaryMode));
        }

        // For a side rebuilt while in a mode (such as by changing a height)
        public void MatchSelectabilityToMode(LevelEntity_Side side)
        {
            var primaryMode = ModeManager.Instance.PrimaryMode;

            side.SetSelectability(SelectsGeometry(primaryMode));

            foreach (var surface in side.GetComponentsInChildren<EditableSurface_Side>(includeInactive: true))
            {
                surface.SetSelectability(SideSurfacesAreSelectable(primaryMode));
            }
        }

        private static bool SelectsGeometry(ModeManager.PrimaryModes primaryMode)
        {
            return primaryMode == ModeManager.PrimaryModes.Geometry ||
                   primaryMode == ModeManager.PrimaryModes.Textures;
        }

        // Clicking a polygon's faces acts on it, or on what's on it
        private static bool SelectsFaces(ModeManager.PrimaryModes primaryMode)
        {
            return SelectsGeometry(primaryMode) ||
                   primaryMode == ModeManager.PrimaryModes.Lights ||
                   primaryMode == ModeManager.PrimaryModes.Media ||
                   primaryMode == ModeManager.PrimaryModes.Sounds ||
                   primaryMode == ModeManager.PrimaryModes.Platforms ||
                   primaryMode == ModeManager.PrimaryModes.Annotations;
        }

        // Heights mode clicks floors and ceilings, except a platform's (whose heights are its platform's)
        private static bool PolygonSurfaceIsSelectable(ModeManager.PrimaryModes primaryMode, EditableSurface_Polygon surface)
        {
            if (primaryMode == ModeManager.PrimaryModes.Heights)
            {
                return HeightsEditing.CanEdit(surface.ParentPolygon);
            }

            return SelectsFaces(primaryMode);
        }

        // Terminals mode clicks sides (computer terminal panels) to preview their terminals
        private static bool SideSurfacesAreSelectable(ModeManager.PrimaryModes primaryMode)
        {
            return SelectsFaces(primaryMode) || primaryMode == ModeManager.PrimaryModes.Terminals;
        }

        // Otherwise clicks pass through media surfaces to the floor
        // TODO: Include geometry mode when a media subfilter is available
        private static bool MediaSurfacesAreSelectable(ModeManager.PrimaryModes primaryMode)
        {
            return primaryMode == ModeManager.PrimaryModes.Lights || primaryMode == ModeManager.PrimaryModes.Media || primaryMode == ModeManager.PrimaryModes.Sounds;
        }

        private void SetSelectability<T>(T selectable, bool enabled) where T : ISelectable
        {
            selectable.SetSelectability(enabled);
        }

        private void SetSelectability<T>(Dictionary<short, T>.ValueCollection selectables, bool enabled) where T : ISelectable
        {
            foreach (var selectable in selectables)
            {
                SetSelectability<T>(selectable, enabled);
            }
        }

        private void SetSelectability<T>(List<T> selectables, bool enabled) where T : ISelectable
        {
            foreach (var selectable in selectables)
            {
                SetSelectability<T>(selectable, enabled);
            }
        }

        private void Start()
        {
            DefaultLayer = LayerMask.NameToLayer("Default");
            SelectionIndicatorLayer = LayerMask.NameToLayer("SelectionVisualization");

            OnSelectionChanged += ShowSelectedFace;
            ModeManager.Instance.OnPrimaryModeChanging += CollectCarriedSelection;
            ModeManager.Instance.OnPrimaryModeChanged += UpdateSelectionToMatchMode;
            WorldPointer.Instance.OnClickEmptySpace += OnPointerClickEmptySpace;
        }

        // After the rest of the editor (such as the palette) has switched modes
        private void LateUpdate()
        {
            SelectCarriedSelection();
        }

        private void OnDestroy()
        {
            var worldPointer = WorldPointer.Instance;
            if (worldPointer)
            {
                worldPointer.OnClickEmptySpace -= OnPointerClickEmptySpace;
            }
        }

        // Except in Level mode, where the level is always selected
        private void OnPointerClickEmptySpace()
        {
            if (ModeManager.Instance.PrimaryMode != ModeManager.PrimaryModes.Level)
            {
                DeselectAll();
            }

            OnClickEmptySpace?.Invoke();
        }
    }
}
