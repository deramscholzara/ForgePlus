using ForgePlus.DataFileIO;
using ForgePlus.Inspection;
using ForgePlus.Palette;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ForgePlus.LevelManipulation
{
    public class SelectionManager : SingletonMonoBehaviour<SelectionManager>
    {
        public enum SceneSelectionFilters
        {
            None,
            Geometry,
            Textures,
            Lights,
            Media,
            Platforms,
            Objects,
            Annotations,
            Level,
        }

        public static int DefaultLayer;
        public static int SelectionIndicatorLayer;

        // Using read-only to force this to only ever be modified/cleared
        private readonly List<IFPSelectable> SelectedObjects = new List<IFPSelectable>(500);

        // TODO: Change this to Geometry when that mode is finished and becomes the default.
        private SceneSelectionFilters currentSceneSelectionFilter = SceneSelectionFilters.Geometry;

        public SceneSelectionFilters CurrentSceneSelectionFilter
        {
            get
            {
                return currentSceneSelectionFilter;

            }
            set
            {
                if (currentSceneSelectionFilter != value)
                {
                    DeselectAll();

                    currentSceneSelectionFilter = value;

                    UpdateSelectionToMatchFilter();
                }
            }
        }

        public void SetToNone(bool shouldSet)
        {
            if (shouldSet)
            {
                CurrentSceneSelectionFilter = SceneSelectionFilters.None;
            }
        }

        public void SetToGeometry(bool shouldSet)
        {
            if (shouldSet)
            {
                CurrentSceneSelectionFilter = SceneSelectionFilters.Geometry;
            }
        }

        public void SetToTextures(bool shouldSet)
        {
            if (shouldSet)
            {
                CurrentSceneSelectionFilter = SceneSelectionFilters.Textures;
            }
        }

        public void SetToLights(bool shouldSet)
        {
            if (shouldSet)
            {
                CurrentSceneSelectionFilter = SceneSelectionFilters.Lights;
            }
        }

        public void SetToMedia(bool shouldSet)
        {
            if (shouldSet)
            {
                CurrentSceneSelectionFilter = SceneSelectionFilters.Media;
            }
        }

        public void SetToPlatforms(bool shouldSet)
        {
            if (shouldSet)
            {
                CurrentSceneSelectionFilter = SceneSelectionFilters.Platforms;
            }
        }

        public void SetToObjects(bool shouldSet)
        {
            if (shouldSet)
            {
                CurrentSceneSelectionFilter = SceneSelectionFilters.Objects;
            }
        }

        public void SetToAnnotations(bool shouldSet)
        {
            if (shouldSet)
            {
                CurrentSceneSelectionFilter = SceneSelectionFilters.Annotations;
            }
        }

        public void SetToLevel(bool shouldSet)
        {
            if (shouldSet)
            {
                CurrentSceneSelectionFilter = SceneSelectionFilters.Level;

                if (FPLevel.Instance)
                {
                    SelectObject(FPLevel.Instance, multiSelect: false);
                }
            }
        }

        public void UpdateSelectionToMatchFilter()
        {
            if (FPLevel.Instance)
            {
                switch (currentSceneSelectionFilter)
                {
                    case SceneSelectionFilters.Geometry:
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
                    case SceneSelectionFilters.Textures:
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
                    case SceneSelectionFilters.Lights:
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
                    case SceneSelectionFilters.Media:
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
                    case SceneSelectionFilters.Platforms:
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
                    case SceneSelectionFilters.Objects:
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
                    case SceneSelectionFilters.Annotations:
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
                    case SceneSelectionFilters.Level:
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
                        break;
                    case SceneSelectionFilters.None:
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

                PaletteManager.Instance.UpdatePaletteToMatchSelectionMode();
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
            InspectorPanel.Instance.ClearAllInspectors();

            if (!SelectedObjects.Contains(selection))
            {
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
            InspectorPanel.Instance.ClearAllInspectors();

            if (SelectedObjects.Contains(selection))
            {
                if (multiSelect || SelectedObjects.Count <= 1)
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

            LevelData.OnLevelOpened += OnLevelOpened;
            LevelData.OnLevelClosed += OnLevelClosed;

            UpdateSelectionToMatchFilter();
        }

        private void OnLevelOpened(string levelName)
        {
            UpdateSelectionToMatchFilter();
        }

        private void OnLevelClosed()
        {
            DeselectAll();
        }

        private void Update()
        {
            if (Input.GetMouseButtonUp(0) ||
                (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    SelectionManager.Instance.DeselectAll();
                }
            }
        }
    }
}
