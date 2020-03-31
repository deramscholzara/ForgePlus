using ForgePlus.Inspection;
using System.Collections.Generic;

namespace ForgePlus.LevelManipulation
{
    public class SelectionManager : OnDemandSingleton<SelectionManager>
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
        private SceneSelectionFilters currentSceneSelectionFilter = SceneSelectionFilters.Objects;

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

        public void UpdateSelectionToMatchFilter()
        {
            if (FPLevel.Instance)
            {
                switch (currentSceneSelectionFilter)
                {
                    case SceneSelectionFilters.Geometry:
                        // TODO: Selecting FPSurfaceFloor or FPSurfaceCeiling should forward the selection to the parent FPPolygon
                        //       - Also do this with a base abstract FPSurfaceBase and also derive FPSurfaceSide, FPSurfaceFloor, FPSurfaceCeiling from it.
                        // TODO: Somehow make it so raycasts don't hit objects or platforms
                        //       - Do this by disabling colliders on FPMapObjects, and by not forwarding polygon selections to plaforms

                        ////SetSelectability<FPPolygon>(FindObjectsOfType<FPPolygon>(), enabled: true);
                        ////SetSelectability<FPSurfaceFloor>(FindObjectsOfType<FPSurfaceFloor>(), enabled: true);
                        ////SetSelectability<FPSurfaceCeiling>(FindObjectsOfType<FPSurfaceCeiling>(), enabled: true);
                        ////SetSelectability<FPLine>(FindObjectsOfType<FPLine>(), enabled: true);
                        ////SetSelectability<FPSide>(FindObjectsOfType<FPSide>(), enabled: true);
                        ////SetSelectability<FPSurfaceSide>(FindObjectsOfType<FPSurfaceSide>(), enabled: true);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FindObjectsOfType<FPMedia>(), enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        var blah = FPLevel.Instance.FPMapObjects.Values;
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);
                        break;
                    case SceneSelectionFilters.Lights:
                        ////SetSelectability<FPPolygon>(FindObjectsOfType<FPPolygon>(), enabled: true);
                        ////SetSelectability<FPSurfaceFloor>(FindObjectsOfType<FPSurfaceFloor>(), enabled: true);
                        ////SetSelectability<FPSurfaceCeiling>(FindObjectsOfType<FPSurfaceCeiling>(), enabled: true);
                        ////SetSelectability<FPLine>(FindObjectsOfType<FPLine>(), enabled: false);
                        ////SetSelectability<FPSide>(FindObjectsOfType<FPSide>(), enabled: true);
                        ////SetSelectability<FPSurfaceSide>(FindObjectsOfType<FPSurfaceSide>(), enabled: true);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FindObjectsOfType<FPMedia>(), enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);
                        break;
                    case SceneSelectionFilters.Media:
                        ////SetSelectability<FPPolygon>(FindObjectsOfType<FPPolygon>(), enabled: true);
                        ////SetSelectability<FPSurfaceFloor>(FindObjectsOfType<FPSurfaceFloor>(), enabled: true);
                        ////SetSelectability<FPSurfaceCeiling>(FindObjectsOfType<FPSurfaceCeiling>(), enabled: true);
                        ////SetSelectability<FPLine>(FindObjectsOfType<FPLine>(), enabled: false);
                        ////SetSelectability<FPSide>(FindObjectsOfType<FPSide>(), enabled: false);
                        ////SetSelectability<FPSurfaceSide>(FindObjectsOfType<FPSurfaceSide>(), enabled: false);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FindObjectsOfType<FPMedia>(), enabled: true);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);
                        break;
                    case SceneSelectionFilters.Platforms:
                        // TODO: When the selection filter is Platforms, selecting an FPSurfaceFloor or FPSurfaceCeiling should forward to the FPPolygon, which would in-turn forward to the FPPlatform

                        ////SetSelectability<FPPolygon>(FindObjectsOfType<FPPolygon>(), enabled: true);
                        ////SetSelectability<FPSurfaceFloor>(FindObjectsOfType<FPSurfaceFloor>(), enabled: false);
                        ////SetSelectability<FPSurfaceCeiling>(FindObjectsOfType<FPSurfaceCeiling>(), enabled: false);
                        ////SetSelectability<FPLine>(FindObjectsOfType<FPLine>(), enabled: false);
                        ////SetSelectability<FPSide>(FindObjectsOfType<FPSide>(), enabled: false);
                        ////SetSelectability<FPSurfaceSide>(FindObjectsOfType<FPSurfaceSide>(), enabled: false);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FindObjectsOfType<FPMedia>(), enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: true);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);
                        break;
                    case SceneSelectionFilters.Objects:
                        // TODO: Make it so geometry will capture raycasts, but not respond to selection events

                        ////SetSelectability<FPPolygon>(FindObjectsOfType<FPPolygon>(), enabled: false);
                        ////SetSelectability<FPSurfaceFloor>(FindObjectsOfType<FPSurfaceFloor>(), enabled: false);
                        ////SetSelectability<FPSurfaceCeiling>(FindObjectsOfType<FPSurfaceCeiling>(), enabled: false);
                        ////SetSelectability<FPLine>(FindObjectsOfType<FPLine>(), enabled: false);
                        ////SetSelectability<FPSide>(FindObjectsOfType<FPSide>(), enabled: false);
                        ////SetSelectability<FPSurfaceSide>(FindObjectsOfType<FPSurfaceSide>(), enabled: false);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FindObjectsOfType<FPMedia>(), enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, true);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);
                        break;
                    case SceneSelectionFilters.Level:
                        ////SetSelectability<FPPolygon>(FindObjectsOfType<FPPolygon>(), enabled: false);
                        ////SetSelectability<FPSurfaceFloor>(FindObjectsOfType<FPSurfaceFloor>(), enabled: false);
                        ////SetSelectability<FPSurfaceCeiling>(FindObjectsOfType<FPSurfaceCeiling>(), enabled: false);
                        ////SetSelectability<FPLine>(FindObjectsOfType<FPLine>(), enabled: false);
                        ////SetSelectability<FPSide>(FindObjectsOfType<FPSide>(), enabled: false);
                        ////SetSelectability<FPSurfaceSide>(FindObjectsOfType<FPSurfaceSide>(), enabled: false);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FindObjectsOfType<FPMedia>(), enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: true);
                        break;
                    case SceneSelectionFilters.Textures:
                    case SceneSelectionFilters.None:
                    default:
                        ////SetSelectability<FPPolygon>(FindObjectsOfType<FPPolygon>(), enabled: false);
                        ////SetSelectability<FPSurfaceFloor>(FindObjectsOfType<FPSurfaceFloor>(), enabled: false);
                        ////SetSelectability<FPSurfaceCeiling>(FindObjectsOfType<FPSurfaceCeiling>(), enabled: false);
                        ////SetSelectability<FPLine>(FindObjectsOfType<FPLine>(), enabled: false);
                        ////SetSelectability<FPSide>(FindObjectsOfType<FPSide>(), enabled: false);
                        ////SetSelectability<FPSurfaceSide>(FindObjectsOfType<FPSurfaceSide>(), enabled: false);
                        ////SetSelectability<FPLight>(FPLight.FPLights.ToArray(), enabled: false);
                        ////SetSelectability<FPMedia>(FindObjectsOfType<FPMedia>(), enabled: false);
                        ////SetSelectability<FPPlatform>(FindObjectsOfType<FPPlatform>(), enabled: false);
                        SetSelectability<FPMapObject>(FPLevel.Instance.FPMapObjects.Values, false);
                        ////SetSelectability<FPLevel>(FPLevel.Instance, enabled: false);
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

                    (selection as IFPInspectable).Inspect();
                }

                SelectedObjects.Add(selection);

                selection.DisplaySelectionState(true);
            }
        }

        public void DeselectObject(IFPSelectable selection, bool multiSelect = false)
        {
            InspectorPanel.Instance.ClearAllInspectors();

            if (SelectedObjects.Contains(selection))
            {
                if (multiSelect || SelectedObjects.Count <= 1)
                {
                    selection.DisplaySelectionState(false);

                    SelectedObjects.Remove(selection);
                }
                else
                {
                    foreach (var selectedObject in SelectedObjects)
                    {
                        if (selectedObject != selection)
                        {
                            selectedObject.DisplaySelectionState(false);
                        }
                    }

                    SelectedObjects.RemoveAll(selectedObject => selectedObject != selection);
                }

                if (SelectedObjects.Count == 1)
                {
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
    }
}
