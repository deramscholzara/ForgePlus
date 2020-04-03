using ForgePlus.DataFileIO;
using ForgePlus.Inspection;
using System.Collections.Generic;

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
            Level,
        }

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

        public void SetToLevel(bool shouldSet)
        {
            if (shouldSet)
            {
                CurrentSceneSelectionFilter = SceneSelectionFilters.Level;
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
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FPLevel.Instance, enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPSurfacePolygon>(FPLevel.Instance.FPSurfacePolygons, enabled: true); // Forwards to polygon, which adds polygon inspector (and selects polygon)
                        SetSelectability<FPSurfaceSide>(FPLevel.Instance.FPSurfaceSides, enabled: true); // Forwards to side, which adds side & line inspector (and selects side)

                        // TODO: Make this true when media subfilter is available
                        SetSelectability<FPSurfaceMedia>(FPLevel.Instance.FPSurfaceMedias, enabled: false); // Forwards to polygon, which adds polygon inspector (and selects polygon)
                        break;
                    case SceneSelectionFilters.Lights:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: true); // Shown in right-palette and just selects and inspects the light
                        ////SetSelectability<FPMedia>(FPLevel.Instance, enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPSurfacePolygon>(FPLevel.Instance.FPSurfacePolygons, enabled: false);
                        SetSelectability<FPSurfaceSide>(FPLevel.Instance.FPSurfaceSides, enabled: false);
                        SetSelectability<FPSurfaceMedia>(FPLevel.Instance.FPSurfaceMedias, enabled: false);
                        break;
                    case SceneSelectionFilters.Media:
                        // TODO: When the selection filter is medias, selecting FPSurfaceMedia should forward to the FPPolygon, which would in-turn inspect the FPMedia (which should implement IInspectable)SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: true);

                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FPLevel.Instance, enabled: true); // Shown in right-palette and just selects and inspects the media
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPSurfacePolygon>(FPLevel.Instance.FPSurfacePolygons, enabled: true); // Forwards to polygon, which adds media & polygon inspector (and selects media)
                        SetSelectability<FPSurfaceSide>(FPLevel.Instance.FPSurfaceSides, enabled: true); // Forwards to polygon, which adds media & polygon inspector (and selects media)
                        SetSelectability<FPSurfaceMedia>(FPLevel.Instance.FPSurfaceMedias, enabled: true); // Forwards to polygon, which adds media & polygon inspector (and selects media)
                        break;
                    case SceneSelectionFilters.Platforms:
                        // TODO: When the selection filter is Platforms, selecting an FPSurfacePolygon or FPSurfaceSide should forward to the FPPolygon, which would in-turn forward to the FPPlatform

                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FPLevel.Instance, enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: true); // Shown in right-palette and just selects and inspects the media
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPSurfacePolygon>(FPLevel.Instance.FPSurfacePolygons, enabled: true); // Forwards to polygon, which adds platform & polygon inspector (and selects platform)
                        SetSelectability<FPSurfaceSide>(FPLevel.Instance.FPSurfaceSides, enabled: true); // Forwards to polygon, which adds platform & polygon inspector (and selects platform)
                        SetSelectability<FPSurfaceMedia>(FPLevel.Instance.FPSurfaceMedias, enabled: false);
                        break;
                    case SceneSelectionFilters.Objects:
                        // TODO: Make it so geometry will capture raycasts, but not respond to selection events

                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FPLevel.Instance, enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, true);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPSurfacePolygon>(FPLevel.Instance.FPSurfacePolygons, enabled: false);
                        SetSelectability<FPSurfaceSide>(FPLevel.Instance.FPSurfaceSides, enabled: false);
                        SetSelectability<FPSurfaceMedia>(FPLevel.Instance.FPSurfaceMedias, enabled: false);
                        break;
                    case SceneSelectionFilters.Level:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FPLevel.Instance, enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: true);

                        SetSelectability<FPSurfacePolygon>(FPLevel.Instance.FPSurfacePolygons, enabled: false);
                        SetSelectability<FPSurfaceSide>(FPLevel.Instance.FPSurfaceSides, enabled: false);
                        SetSelectability<FPSurfaceMedia>(FPLevel.Instance.FPSurfaceMedias, enabled: false);
                        break;
                    case SceneSelectionFilters.Textures:// Uses None mode because you can't actually "select" anything when texture painting (selection mode should be disabled)
                    case SceneSelectionFilters.None:
                    default:
                        SetSelectability<FPPolygon>(FPLevel.Instance.FPPolygons.Values, enabled: false);
                        SetSelectability<FPLine>(FPLevel.Instance.FPLines.Values, enabled: false);
                        SetSelectability<FPSide>(FPLevel.Instance.FPSides.Values, enabled: false);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FPLevel.Instance, enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);

                        SetSelectability<FPSurfacePolygon>(FPLevel.Instance.FPSurfacePolygons, enabled: false);
                        SetSelectability<FPSurfaceSide>(FPLevel.Instance.FPSurfaceSides, enabled: false);
                        SetSelectability<FPSurfaceMedia>(FPLevel.Instance.FPSurfaceMedias, enabled: false);
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
            InspectorPanel.Instance.ClearAllInspectors();

            if (!SelectedObjects.Contains(selection))
            {
                if (!multiSelect)
                {
                    DeselectAll();
                }

                // 1. Update displayed selection
                selection.DisplaySelectionState(true);

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
                    selection.DisplaySelectionState(false);

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
                            selectedObject.DisplaySelectionState(false);
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
                selectedObject.DisplaySelectionState(false);
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
    }
}
