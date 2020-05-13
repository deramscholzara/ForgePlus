using ForgePlus.Inspection;
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

        private readonly List<IFPSelectable> SelectedObjects = new List<IFPSelectable>(500);

        private bool selectionEventStartedOverEmptiness = false;

        public IFPSelectable SelectedObject
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

            if (FPLevel.Instance)
            {
                switch (primaryMode)
                {
                    case ModeManager.PrimaryModes.Geometry:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: true);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: true);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: true);
                        SetSelectability<FPLight>(FPLevel.Instance.FPLights.Values, enabled: false);
                        SetSelectability<FPMedia>(FPLevel.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        SetSelectability<FPAnnotation>(FPLevel.Instance.FPAnnotations.Values, false);
                        SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPInteractiveSurfacePolygon>(FPLevel.Instance.FPInteractiveSurfacePolygons, enabled: true);
                        SetSelectability<FPInteractiveSurfaceSide>(FPLevel.Instance.FPInteractiveSurfaceSides, enabled: true);
                        // TODO: Make this true when media subfilter is available
                        SetSelectability<FPInteractiveSurfaceMedia>(FPLevel.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                    case ModeManager.PrimaryModes.Textures:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: true);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: true);
                        SetSelectability<FPLight>(FPLevel.Instance.FPLights.Values, enabled: false);
                        SetSelectability<FPMedia>(FPLevel.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        SetSelectability<FPAnnotation>(FPLevel.Instance.FPAnnotations.Values, false);
                        SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPInteractiveSurfacePolygon>(FPLevel.Instance.FPInteractiveSurfacePolygons, enabled: true);
                        SetSelectability<FPInteractiveSurfaceSide>(FPLevel.Instance.FPInteractiveSurfaceSides, enabled: true);
                        SetSelectability<FPInteractiveSurfaceMedia>(FPLevel.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                    case ModeManager.PrimaryModes.Lights:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        SetSelectability<FPLight>(FPLevel.Instance.FPLights.Values, enabled: true); // Shown in right-palette and just selects and inspects the light
                        SetSelectability<FPMedia>(FPLevel.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        SetSelectability<FPAnnotation>(FPLevel.Instance.FPAnnotations.Values, false);
                        SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPInteractiveSurfacePolygon>(FPLevel.Instance.FPInteractiveSurfacePolygons, enabled: true);
                        SetSelectability<FPInteractiveSurfaceSide>(FPLevel.Instance.FPInteractiveSurfaceSides, enabled: true);
                        SetSelectability<FPInteractiveSurfaceMedia>(FPLevel.Instance.FPInteractiveSurfaceMedias, enabled: true);
                        break;
                    case ModeManager.PrimaryModes.Media:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        SetSelectability<FPLight>(FPLevel.Instance.FPLights.Values, enabled: false);
                        SetSelectability<FPMedia>(FPLevel.Instance.FPMedias.Values, enabled: true); // Shown in right-palette and just selects and inspects the media
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        SetSelectability<FPAnnotation>(FPLevel.Instance.FPAnnotations.Values, false);
                        SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPInteractiveSurfacePolygon>(FPLevel.Instance.FPInteractiveSurfacePolygons, enabled: true);
                        SetSelectability<FPInteractiveSurfaceSide>(FPLevel.Instance.FPInteractiveSurfaceSides, enabled: true);
                        SetSelectability<FPInteractiveSurfaceMedia>(FPLevel.Instance.FPInteractiveSurfaceMedias, enabled: true);
                        break;
                    case ModeManager.PrimaryModes.Platforms:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        SetSelectability<FPLight>(FPLevel.Instance.FPLights.Values, enabled: false);
                        SetSelectability<FPMedia>(FPLevel.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPCeilingFpPlatforms.Values, enabled: true);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPFloorFpPlatforms.Values, enabled: true);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        SetSelectability<FPAnnotation>(FPLevel.Instance.FPAnnotations.Values, false);
                        SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPInteractiveSurfacePolygon>(FPLevel.Instance.FPInteractiveSurfacePolygons, enabled: true);
                        SetSelectability<FPInteractiveSurfaceSide>(FPLevel.Instance.FPInteractiveSurfaceSides, enabled: true);
                        SetSelectability<FPInteractiveSurfaceMedia>(FPLevel.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                    case ModeManager.PrimaryModes.Objects:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        SetSelectability<FPLight>(FPLevel.Instance.FPLights.Values, enabled: false);
                        SetSelectability<FPMedia>(FPLevel.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, true);
                        SetSelectability<FPAnnotation>(FPLevel.Instance.FPAnnotations.Values, false);
                        SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPInteractiveSurfacePolygon>(FPLevel.Instance.FPInteractiveSurfacePolygons, enabled: false);
                        SetSelectability<FPInteractiveSurfaceSide>(FPLevel.Instance.FPInteractiveSurfaceSides, enabled: false);
                        SetSelectability<FPInteractiveSurfaceMedia>(FPLevel.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                    case ModeManager.PrimaryModes.Annotations:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        SetSelectability<FPLight>(FPLevel.Instance.FPLights.Values, enabled: false);
                        SetSelectability<FPMedia>(FPLevel.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        SetSelectability<FPAnnotation>(FPLevel.Instance.FPAnnotations.Values, true);
                        SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPInteractiveSurfacePolygon>(FPLevel.Instance.FPInteractiveSurfacePolygons, enabled: false);
                        SetSelectability<FPInteractiveSurfaceSide>(FPLevel.Instance.FPInteractiveSurfaceSides, enabled: false);
                        SetSelectability<FPInteractiveSurfaceMedia>(FPLevel.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                    case ModeManager.PrimaryModes.Level:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        SetSelectability<FPLight>(FPLevel.Instance.FPLights.Values, enabled: false);
                        SetSelectability<FPMedia>(FPLevel.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        SetSelectability<FPAnnotation>(FPLevel.Instance.FPAnnotations.Values, false);
                        SetSelectability<FPLevel>(FPLevel.Instance, enabled: true);

                        SetSelectability<FPInteractiveSurfacePolygon>(FPLevel.Instance.FPInteractiveSurfacePolygons, enabled: false);
                        SetSelectability<FPInteractiveSurfaceSide>(FPLevel.Instance.FPInteractiveSurfaceSides, enabled: false);
                        SetSelectability<FPInteractiveSurfaceMedia>(FPLevel.Instance.FPInteractiveSurfaceMedias, enabled: false);

                        // Select the level here, since there's no visual way to select it besides the mode button
                        if (FPLevel.Instance)
                        {
                            SelectObject(FPLevel.Instance, multiSelect: false);
                        }
                        break;
                    case ModeManager.PrimaryModes.None:
                    default:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        SetSelectability<FPLight>(FPLevel.Instance.FPLights.Values, enabled: false);
                        SetSelectability<FPMedia>(FPLevel.Instance.FPMedias.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPCeilingFpPlatforms.Values, enabled: false);
                        SetSelectability<FPPlatform>(FPLevel.Instance.FPFloorFpPlatforms.Values, enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        SetSelectability<FPAnnotation>(FPLevel.Instance.FPAnnotations.Values, false);
                        SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPInteractiveSurfacePolygon>(FPLevel.Instance.FPInteractiveSurfacePolygons, enabled: false);
                        SetSelectability<FPInteractiveSurfaceSide>(FPLevel.Instance.FPInteractiveSurfaceSides, enabled: false);
                        SetSelectability<FPInteractiveSurfaceMedia>(FPLevel.Instance.FPInteractiveSurfaceMedias, enabled: false);
                        break;
                }
            }
        }

        public void ToggleObjectSelection(IFPSelectable selection, bool multiSelect = false)
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

        public void SelectObject(IFPSelectable selection, bool multiSelect = false)
        {
            if (!SelectedObjects.Contains(selection))
            {
                InspectorPanel.Instance.ClearAllInspectors();

                if (!multiSelect)
                {
                    DeselectAll();
                }

                // 1. Update displayed selection
                if (selection is IFPSelectionDisplayable)
                {
                    (selection as IFPSelectionDisplayable).DisplaySelectionState(true);
                }

                // 2. Update actual selection list
                SelectedObjects.Add(selection);

                if (SelectedObjects.Count == 1)
                {
                    // 3. Inspect selection
                    (selection as IFPInspectable).Inspect();
                }
            }
        }

        public void DeselectObject(IFPSelectable selection, bool multiSelect = false)
        {
            if (SelectedObjects.Contains(selection))
            {
                InspectorPanel.Instance.ClearAllInspectors();

                if (multiSelect || SelectedObjects.Count == 1)
                {
                    // 1. Update displayed selection
                    if (selection is IFPSelectionDisplayable)
                    {
                        (selection as IFPSelectionDisplayable).DisplaySelectionState(false);
                    }

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
                            if (selection is IFPSelectionDisplayable)
                            {
                                (selection as IFPSelectionDisplayable).DisplaySelectionState(false);
                            }
                        }
                    }

                    // 2. Update actual selection list
                    SelectedObjects.RemoveAll(selectedObject => selectedObject != selection);
                }

                if (SelectedObjects.Count == 1)
                {
                    // 3. Inspect selection
                    (selection as IFPInspectable).Inspect();
                }
            }
        }

        public void DeselectAll()
        {
            InspectorPanel.Instance.ClearAllInspectors();

            foreach (var selectedObject in SelectedObjects)
            {
                if (selectedObject is IFPSelectionDisplayable)
                {
                    (selectedObject as IFPSelectionDisplayable).DisplaySelectionState(false);
                }
            }

            SelectedObjects.Clear();
        }

        private void SetSelectability<T>(T selectable, bool enabled) where T : IFPSelectable
        {
            selectable.SetSelectability(enabled);
        }

        private void SetSelectability<T>(Dictionary<short, T>.ValueCollection selectables, bool enabled) where T : IFPSelectable
        {
            foreach (var selectable in selectables)
            {
                SetSelectability<T>(selectable, enabled);
            }
        }

        private void SetSelectability<T>(List<T> selectables, bool enabled) where T : IFPSelectable
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
