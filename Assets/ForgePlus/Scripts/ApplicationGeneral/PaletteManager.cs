﻿using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ForgePlus.Palette
{
    public class PaletteManager : SingletonMonoBehaviour<PaletteManager>
    {
        [SerializeField]
        private Transform swatchesParent = null;

        [SerializeField]
        private ToggleGroup toggleGroup = null;

        [SerializeField]
        private SwatchFPLight lightSwatchPrefab = null;

        [SerializeField]
        private SwatchFPMedia mediaSwatchPrefab = null;

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

        public void SelectSwatchForLight(FPLight fpLight)
        {
            var matchingSwatch = currentSwatches.First(swatch => swatch.GetComponent<SwatchFPLight>().FPLight == fpLight);
            var matchingToggle = matchingSwatch.GetComponent<Toggle>();

            matchingToggle.isOn = true;
        }

        public void SelectSwatchForMedia(FPMedia fpMedia)
        {
            var matchingSwatch = currentSwatches.First(swatch => swatch.GetComponent<SwatchFPMedia>().FPMedia == fpMedia);
            var matchingToggle = matchingSwatch.GetComponent<Toggle>();

            matchingToggle.isOn = true;
        }

        private void SetToNone(bool shouldSet)
        {
            Clear();
        }

        private void SetToGeometry(bool shouldSet)
        {
            Clear();

            toggleGroup.allowSwitchOff = false;

            // TODO: populate with tools
            //       - Line Drawing
            //       - Polygon Fill (oh man... should this auto-fill when a poly is completed?)
        }

        private void SetToTextures(bool shouldSet)
        {
            Clear();

            toggleGroup.allowSwitchOff = false;

            // TODO: populate with textures for painting
        }

        private void SetToLights(bool shouldSet)
        {
            Clear();

            toggleGroup.allowSwitchOff = true;

            foreach (var fpLight in FPLevel.Instance.FPLights.Values)
            {
                var swatch = Instantiate(lightSwatchPrefab, swatchesParent);
                swatch.SetInitialValues(fpLight, toggleGroup);
                currentSwatches.Add(swatch.gameObject);
            }
        }

        private void SetToMedia(bool shouldSet)
        {
            Clear();

            toggleGroup.allowSwitchOff = true;

            foreach (var fpMedia in FPLevel.Instance.FPMedias.Values)
            {
                var swatch = Instantiate(mediaSwatchPrefab, swatchesParent);
                swatch.SetInitialValues(fpMedia, toggleGroup);
                currentSwatches.Add(swatch.gameObject);
            }
        }

        private void SetToPlatforms(bool shouldSet)
        {
            Clear();

            toggleGroup.allowSwitchOff = true;

            // TODO: populate with shortcuts that focus the camera on the associated platform polygon when clicked.
        }

        private void SetToObjects(bool shouldSet)
        {
            Clear();

            toggleGroup.allowSwitchOff = false;

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
