using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.Palette
{
    public class PaletteManager : SingletonMonoBehaviour<PaletteManager>
    {
        public Transform SwatchesParent;
        public ToggleGroup ToggleGroup;

        public SwatchFPLight LightSwatchPrefab;

        private List<GameObject> currentSwatches = new List<GameObject>();

        public void UpdatePaletteToMatchSelectionMode()
        {
            switch (SelectionManager.Instance.CurrentSceneSelectionFilter)
            {
                case SelectionManager.SceneSelectionFilters.Geometry:
                    SetToGeometry(shouldSet: true);
                    break;
                case SelectionManager.SceneSelectionFilters.Textures:
                    SetToTextures(shouldSet: true);
                    break;
                case SelectionManager.SceneSelectionFilters.Lights:
                    SetToLights(shouldSet: true);
                    break;
                case SelectionManager.SceneSelectionFilters.Media:
                    SetToMedia(shouldSet: true);
                    break;
                case SelectionManager.SceneSelectionFilters.Platforms:
                    SetToPlatforms(shouldSet: true);
                    break;
                case SelectionManager.SceneSelectionFilters.Objects:
                    SetToObjects(shouldSet: true);
                    break;
                case SelectionManager.SceneSelectionFilters.Level:
                    SetToLevel(shouldSet: true);
                    break;
                case SelectionManager.SceneSelectionFilters.None:
                default:
                    SetToNone(shouldSet: true);
                    break;
            }
        }

        public void Clear()
        {
            foreach (var swatch in currentSwatches)
            {
                Destroy(swatch);
            }

            currentSwatches.Clear();
        }

        private void SetToNone(bool shouldSet)
        {
            Clear();
        }

        private void SetToGeometry(bool shouldSet)
        {
            Clear();

            // TODO: populate with tools
            //       - Line Drawing
            //       - Polygon Fill (oh man... should this auto-fill when a poly is completed?)
        }

        private void SetToTextures(bool shouldSet)
        {
            Clear();

            // TODO: populate with textures for painting
        }

        private void SetToLights(bool shouldSet)
        {
            Clear();

            var turnedOnFirstItem = false;
            foreach (var fpLight in FPLevel.Instance.FPLights.Values)
            {
                var swatch = Instantiate(LightSwatchPrefab, SwatchesParent);
                swatch.SetInitialValues(fpLight, ToggleGroup);
            }
        }

        private void SetToMedia(bool shouldSet)
        {
            Clear();

            // TODO: populate with medias for selection and painting
        }

        private void SetToPlatforms(bool shouldSet)
        {
            Clear();

            // TODO: populate with shortcuts that focus the camera on the associated platform polygon when clicked.
        }

        private void SetToObjects(bool shouldSet)
        {
            Clear();

            // TODO: populate with placement tools
            //       - Players
            //       - Monsters
            //           - foldout to show all subtypes
            //           - load view-0, frame-0 sprite
            //       - Items
            //           - foldout to show all subtypes
            //           - load view-0, frame-0 sprite
            //       - Sceneries
            //           - foldout to show all subtypes
            //           - load view-0, frame-0 sprite
            //       - Sounds
            //           - foldout to show all subtypes
            //           - plays sound when clicked
            //       - Goals
        }

        private void SetToLevel(bool shouldSet)
        {
            Clear();
        }

        private void Start()
        {
            LevelData.OnLevelOpened += OnLevelOpened;
            LevelData.OnLevelClosed += OnLevelClosed;
        }

        private void OnLevelOpened(string levelName)
        {
            UpdatePaletteToMatchSelectionMode();
        }

        private void OnLevelClosed()
        {
            Clear();
        }
    }
}
