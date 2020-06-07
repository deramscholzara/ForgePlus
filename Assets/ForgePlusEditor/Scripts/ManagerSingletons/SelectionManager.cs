using ForgePlus.Entities.Geometry;
using ForgePlus.Inspection;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgePlus.LevelManipulation
{
    public class SelectionManager : SingletonMonoBehaviour<SelectionManager>
    {
        // TODO: Add selection subfilters here

        public static int DefaultLayer;
        public static int SelectionIndicatorLayer;

        public event Action OnClickEmptySpace;

        private readonly List<ISelectable> SelectedObjects = new List<ISelectable>(500);

        private bool selectionEventStartedOverEmptiness = false;

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

        public void UpdateSelectionToMatchMode(ModeManager.PrimaryModes primaryMode)
        {
            DeselectAll();

            if (LevelEntity_Level.Instance)
            {
                switch (primaryMode)
                {
                    case ModeManager.PrimaryModes.Geometry:
                        SetSelectability<LevelEntity_Polygon>(LevelEntity_Level.Instance.FPPolygons.Values, enabled: true);
                        SetSelectability<LevelEntity_Line>(LevelEntity_Level.Instance.FPLines.Values, enabled: true);
                        SetSelectability<LevelEntity_Side>(LevelEntity_Level.Instance.FPSides.Values, enabled: true);
                        SetSelectability<LevelEntity_Light>(LevelEntity_Level.Instance.FPLights.Values, enabled: false);
                        SetSelectability<LevelEntity_Media>(LevelEntity_Level.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_MapObject>(LevelEntity_Level.Instance.FPMapObjects.Values, false);
                        SetSelectability<LevelEntity_Annotation>(LevelEntity_Level.Instance.FPAnnotations.Values, false);
                        SetSelectability<LevelEntity_Level>(LevelEntity_Level.Instance, enabled: false);

                        SetSelectability<EditableSurface_Polygon>(LevelEntity_Level.Instance.FPInteractiveSurfacePolygons, enabled: true);
                        SetSelectability<EditableSurface_Side>(LevelEntity_Level.Instance.FPInteractiveSurfaceSides, enabled: true);
                        // TODO: Make this true when media subfilter is available
                        SetSelectability<EditableSurface_Media>(LevelEntity_Level.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                    case ModeManager.PrimaryModes.Textures:
                        SetSelectability<LevelEntity_Polygon>(LevelEntity_Level.Instance.FPPolygons.Values, enabled: true);
                        SetSelectability<LevelEntity_Line>(LevelEntity_Level.Instance.FPLines.Values, enabled: false);
                        SetSelectability<LevelEntity_Side>(LevelEntity_Level.Instance.FPSides.Values, enabled: true);
                        SetSelectability<LevelEntity_Light>(LevelEntity_Level.Instance.FPLights.Values, enabled: false);
                        SetSelectability<LevelEntity_Media>(LevelEntity_Level.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_MapObject>(LevelEntity_Level.Instance.FPMapObjects.Values, false);
                        SetSelectability<LevelEntity_Annotation>(LevelEntity_Level.Instance.FPAnnotations.Values, false);
                        SetSelectability<LevelEntity_Level>(LevelEntity_Level.Instance, enabled: false);

                        SetSelectability<EditableSurface_Polygon>(LevelEntity_Level.Instance.FPInteractiveSurfacePolygons, enabled: true);
                        SetSelectability<EditableSurface_Side>(LevelEntity_Level.Instance.FPInteractiveSurfaceSides, enabled: true);
                        SetSelectability<EditableSurface_Media>(LevelEntity_Level.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                    case ModeManager.PrimaryModes.Lights:
                        SetSelectability<LevelEntity_Polygon>(LevelEntity_Level.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<LevelEntity_Line>(LevelEntity_Level.Instance.FPLines.Values, enabled: false);
                        SetSelectability<LevelEntity_Side>(LevelEntity_Level.Instance.FPSides.Values, enabled: false);
                        SetSelectability<LevelEntity_Light>(LevelEntity_Level.Instance.FPLights.Values, enabled: true); // Shown in right-palette and just selects and inspects the light
                        SetSelectability<LevelEntity_Media>(LevelEntity_Level.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_MapObject>(LevelEntity_Level.Instance.FPMapObjects.Values, false);
                        SetSelectability<LevelEntity_Annotation>(LevelEntity_Level.Instance.FPAnnotations.Values, false);
                        SetSelectability<LevelEntity_Level>(LevelEntity_Level.Instance, enabled: false);

                        SetSelectability<EditableSurface_Polygon>(LevelEntity_Level.Instance.FPInteractiveSurfacePolygons, enabled: true);
                        SetSelectability<EditableSurface_Side>(LevelEntity_Level.Instance.FPInteractiveSurfaceSides, enabled: true);
                        SetSelectability<EditableSurface_Media>(LevelEntity_Level.Instance.FPInteractiveSurfaceMedias, enabled: true);
                        break;
                    case ModeManager.PrimaryModes.Media:
                        SetSelectability<LevelEntity_Polygon>(LevelEntity_Level.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<LevelEntity_Line>(LevelEntity_Level.Instance.FPLines.Values, enabled: false);
                        SetSelectability<LevelEntity_Side>(LevelEntity_Level.Instance.FPSides.Values, enabled: false);
                        SetSelectability<LevelEntity_Light>(LevelEntity_Level.Instance.FPLights.Values, enabled: false);
                        SetSelectability<LevelEntity_Media>(LevelEntity_Level.Instance.FPMedias.Values, enabled: true); // Shown in right-palette and just selects and inspects the media
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_MapObject>(LevelEntity_Level.Instance.FPMapObjects.Values, false);
                        SetSelectability<LevelEntity_Annotation>(LevelEntity_Level.Instance.FPAnnotations.Values, false);
                        SetSelectability<LevelEntity_Level>(LevelEntity_Level.Instance, enabled: false);

                        SetSelectability<EditableSurface_Polygon>(LevelEntity_Level.Instance.FPInteractiveSurfacePolygons, enabled: true);
                        SetSelectability<EditableSurface_Side>(LevelEntity_Level.Instance.FPInteractiveSurfaceSides, enabled: true);
                        SetSelectability<EditableSurface_Media>(LevelEntity_Level.Instance.FPInteractiveSurfaceMedias, enabled: true);
                        break;
                    case ModeManager.PrimaryModes.Platforms:
                        SetSelectability<LevelEntity_Polygon>(LevelEntity_Level.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<LevelEntity_Line>(LevelEntity_Level.Instance.FPLines.Values, enabled: false);
                        SetSelectability<LevelEntity_Side>(LevelEntity_Level.Instance.FPSides.Values, enabled: false);
                        SetSelectability<LevelEntity_Light>(LevelEntity_Level.Instance.FPLights.Values, enabled: false);
                        SetSelectability<LevelEntity_Media>(LevelEntity_Level.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPCeilingFpPlatforms.Values, enabled: true);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPFloorFpPlatforms.Values, enabled: true);
                        SetSelectability<LevelEntity_MapObject>(LevelEntity_Level.Instance.FPMapObjects.Values, false);
                        SetSelectability<LevelEntity_Annotation>(LevelEntity_Level.Instance.FPAnnotations.Values, false);
                        SetSelectability<LevelEntity_Level>(LevelEntity_Level.Instance, enabled: false);

                        SetSelectability<EditableSurface_Polygon>(LevelEntity_Level.Instance.FPInteractiveSurfacePolygons, enabled: true);
                        SetSelectability<EditableSurface_Side>(LevelEntity_Level.Instance.FPInteractiveSurfaceSides, enabled: true);
                        SetSelectability<EditableSurface_Media>(LevelEntity_Level.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                    case ModeManager.PrimaryModes.Objects:
                        SetSelectability<LevelEntity_Polygon>(LevelEntity_Level.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<LevelEntity_Line>(LevelEntity_Level.Instance.FPLines.Values, enabled: false);
                        SetSelectability<LevelEntity_Side>(LevelEntity_Level.Instance.FPSides.Values, enabled: false);
                        SetSelectability<LevelEntity_Light>(LevelEntity_Level.Instance.FPLights.Values, enabled: false);
                        SetSelectability<LevelEntity_Media>(LevelEntity_Level.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_MapObject>(LevelEntity_Level.Instance.FPMapObjects.Values, true);
                        SetSelectability<LevelEntity_Annotation>(LevelEntity_Level.Instance.FPAnnotations.Values, false);
                        SetSelectability<LevelEntity_Level>(LevelEntity_Level.Instance, enabled: false);

                        SetSelectability<EditableSurface_Polygon>(LevelEntity_Level.Instance.FPInteractiveSurfacePolygons, enabled: false);
                        SetSelectability<EditableSurface_Side>(LevelEntity_Level.Instance.FPInteractiveSurfaceSides, enabled: false);
                        SetSelectability<EditableSurface_Media>(LevelEntity_Level.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                    case ModeManager.PrimaryModes.Annotations:
                        SetSelectability<LevelEntity_Polygon>(LevelEntity_Level.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<LevelEntity_Line>(LevelEntity_Level.Instance.FPLines.Values, enabled: false);
                        SetSelectability<LevelEntity_Side>(LevelEntity_Level.Instance.FPSides.Values, enabled: false);
                        SetSelectability<LevelEntity_Light>(LevelEntity_Level.Instance.FPLights.Values, enabled: false);
                        SetSelectability<LevelEntity_Media>(LevelEntity_Level.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_MapObject>(LevelEntity_Level.Instance.FPMapObjects.Values, false);
                        SetSelectability<LevelEntity_Annotation>(LevelEntity_Level.Instance.FPAnnotations.Values, true);
                        SetSelectability<LevelEntity_Level>(LevelEntity_Level.Instance, enabled: false);

                        SetSelectability<EditableSurface_Polygon>(LevelEntity_Level.Instance.FPInteractiveSurfacePolygons, enabled: false);
                        SetSelectability<EditableSurface_Side>(LevelEntity_Level.Instance.FPInteractiveSurfaceSides, enabled: false);
                        SetSelectability<EditableSurface_Media>(LevelEntity_Level.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                    case ModeManager.PrimaryModes.Level:
                        SetSelectability<LevelEntity_Polygon>(LevelEntity_Level.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<LevelEntity_Line>(LevelEntity_Level.Instance.FPLines.Values, enabled: false);
                        SetSelectability<LevelEntity_Side>(LevelEntity_Level.Instance.FPSides.Values, enabled: false);
                        SetSelectability<LevelEntity_Light>(LevelEntity_Level.Instance.FPLights.Values, enabled: false);
                        SetSelectability<LevelEntity_Media>(LevelEntity_Level.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_MapObject>(LevelEntity_Level.Instance.FPMapObjects.Values, false);
                        SetSelectability<LevelEntity_Annotation>(LevelEntity_Level.Instance.FPAnnotations.Values, false);
                        SetSelectability<LevelEntity_Level>(LevelEntity_Level.Instance, enabled: true);

                        SetSelectability<EditableSurface_Polygon>(LevelEntity_Level.Instance.FPInteractiveSurfacePolygons, enabled: false);
                        SetSelectability<EditableSurface_Side>(LevelEntity_Level.Instance.FPInteractiveSurfaceSides, enabled: false);
                        SetSelectability<EditableSurface_Media>(LevelEntity_Level.Instance.FPInteractiveSurfaceMedias, enabled: false);

                        // Select the level here, since there's no visual way to select it besides the mode button
                        if (LevelEntity_Level.Instance)
                        {
                            SelectObject(LevelEntity_Level.Instance, multiSelect: false);
                        }
                        break;
                    case ModeManager.PrimaryModes.None:
                    default:
                        SetSelectability<LevelEntity_Polygon>(LevelEntity_Level.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<LevelEntity_Line>(LevelEntity_Level.Instance.FPLines.Values, enabled: false);
                        SetSelectability<LevelEntity_Side>(LevelEntity_Level.Instance.FPSides.Values, enabled: false);
                        SetSelectability<LevelEntity_Light>(LevelEntity_Level.Instance.FPLights.Values, enabled: false);
                        SetSelectability<LevelEntity_Media>(LevelEntity_Level.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_Platform>(LevelEntity_Level.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<LevelEntity_MapObject>(LevelEntity_Level.Instance.FPMapObjects.Values, false);
                        SetSelectability<LevelEntity_Annotation>(LevelEntity_Level.Instance.FPAnnotations.Values, false);
                        SetSelectability<LevelEntity_Level>(LevelEntity_Level.Instance, enabled: false);

                        SetSelectability<EditableSurface_Polygon>(LevelEntity_Level.Instance.FPInteractiveSurfacePolygons, enabled: false);
                        SetSelectability<EditableSurface_Side>(LevelEntity_Level.Instance.FPInteractiveSurfaceSides, enabled: false);
                        SetSelectability<EditableSurface_Media>(LevelEntity_Level.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                }
            }
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
                    DeselectAll();
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
            }
        }

        public void DeselectAll()
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

            ModeManager.Instance.OnPrimaryModeChanged += UpdateSelectionToMatchMode;
        }

        private void Update()
        {
            // Handling for when the user clicks on empty space
            if (Input.GetMouseButtonDown(0) ||
                (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    selectionEventStartedOverEmptiness = true;
                }
            }

            if (Input.GetMouseButtonUp(0) ||
                (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
            {
                if (selectionEventStartedOverEmptiness && !EventSystem.current.IsPointerOverGameObject())
                {
                    if (ModeManager.Instance.PrimaryMode != ModeManager.PrimaryModes.Level)
                    {
                        DeselectAll();
                    }

                    OnClickEmptySpace?.Invoke();
                }

                selectionEventStartedOverEmptiness = false;
            }
        }
    }
}
