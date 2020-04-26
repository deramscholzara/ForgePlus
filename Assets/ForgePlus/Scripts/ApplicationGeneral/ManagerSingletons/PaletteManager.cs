using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using ForgePlus.ShapesCollections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Weland;
using Weland.Extensions;

namespace ForgePlus.Palette
{
    public class PaletteManager : SingletonMonoBehaviour<PaletteManager>
    {
        [SerializeField]
        private Transform swatchesParent = null;

        [SerializeField]
        private ToggleGroup toggleGroup = null;

        [SerializeField]
        private SwatchTexture textureSwatchPrefab = null;

        [SerializeField]
        private SwatchFPLight lightSwatchPrefab = null;

        [SerializeField]
        private SwatchFPMedia mediaSwatchPrefab = null;

        [SerializeField]
        private GameObject horizontalLayoutHelperPrefab = null;

        private readonly List<GameObject> currentSwatches = new List<GameObject>();
        private readonly List<GameObject> currentLayoutHelpers = new List<GameObject>();

        private bool selectionEventStartedOverEmptiness = false;

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
                case SelectionManager.SceneSelectionFilters.Annotations:
                    SetToAnnotations(shouldSet: true);
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

            foreach (var helper in currentLayoutHelpers)
            {
                Destroy(helper);
            }

            currentLayoutHelpers.Clear();
        }

        public void SelectSwatchForTexture(ShapeDescriptor shapeDescriptor, bool invokeToggleEvents = false)
        {
            var matchingSwatch = currentSwatches.First(swatch => (ushort)swatch.GetComponent<SwatchTexture>().ShapeDescriptor == (ushort)shapeDescriptor);
            var matchingToggle = matchingSwatch.GetComponent<Toggle>();

            ActivateToggle(matchingToggle, invokeToggleEvents);
        }

        public void SelectSwatchForLight(FPLight fpLight, bool invokeToggleEvents = false)
        {
            var matchingSwatch = currentSwatches.First(swatch => swatch.GetComponent<SwatchFPLight>().FPLight == fpLight);
            var matchingToggle = matchingSwatch.GetComponent<Toggle>();

            ActivateToggle(matchingToggle, invokeToggleEvents);
        }

        public void SelectSwatchForMedia(FPMedia fpMedia, bool invokeToggleEvents = false)
        {
            var matchingSwatch = currentSwatches.First(swatch => swatch.GetComponent<SwatchFPMedia>().FPMedia == fpMedia);
            var matchingToggle = matchingSwatch.GetComponent<Toggle>();

            ActivateToggle(matchingToggle, invokeToggleEvents);
        }

        private void ActivateToggle(Toggle toggle, bool invokeToggleEvents)
        {
            if (invokeToggleEvents)
            {
                toggle.isOn = true;
            }
            else
            {
                //// TODO needed to prevent multi-swatch-select?: toggleGroup.SetAllTogglesOff(sendCallback: false);
                toggle.SetIsOnWithoutNotify(true);
            }
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

            var loadedTextureEntries = WallsCollection.GetAllLoadedTextures().ToList();
            loadedTextureEntries.Sort((entryA, entryB) => (entryA.Key.Collection == entryB.Key.Collection ?
                                                           entryA.Key.Bitmap.CompareTo(entryB.Key.Bitmap) :
                                                           ((entryA.Key.UsesLandscapeCollection() || entryB.Key.UsesLandscapeCollection()) ?
                                                            -entryA.Key.Collection.CompareTo(entryB.Key.Collection) :
                                                            entryA.Key.Collection.CompareTo(entryB.Key.Collection))));

            GameObject currentHorizontalHelper = null;
            var activatedFirstSwatch = false;

            foreach (var textureEntry in loadedTextureEntries)
            {
                if (textureEntry.Key.UsesLandscapeCollection())
                {
                    currentHorizontalHelper = null;

                    var swatch = Instantiate(textureSwatchPrefab, swatchesParent);
                    swatch.SetInitialValues(textureEntry, toggleGroup);
                    currentSwatches.Add(swatch.gameObject);
                }
                else
                {
                    if (!currentHorizontalHelper)
                    {
                        currentHorizontalHelper = Instantiate(horizontalLayoutHelperPrefab, swatchesParent);
                        currentLayoutHelpers.Add(currentHorizontalHelper);

                        var swatch = Instantiate(textureSwatchPrefab, currentHorizontalHelper.transform);
                        swatch.SetInitialValues(textureEntry, toggleGroup);
                        currentSwatches.Add(swatch.gameObject);
                    }
                    else
                    {
                        var swatch = Instantiate(textureSwatchPrefab, currentHorizontalHelper.transform);
                        swatch.SetInitialValues(textureEntry, toggleGroup);
                        currentSwatches.Add(swatch.gameObject);

                        currentHorizontalHelper = null;
                    }
                }

                if (!activatedFirstSwatch)
                {
                    currentSwatches[0].GetComponent<Toggle>().isOn = true;

                    activatedFirstSwatch = true;
                }
            }
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

        private void SetToAnnotations(bool shouldSet)
        {
            Clear();
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

        private void Update()
        {
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
                    if (toggleGroup.allowSwitchOff)
                    {
                        toggleGroup.SetAllTogglesOff(sendCallback: true);
                    }
                }

                selectionEventStartedOverEmptiness = false;
            }
        }
    }
}
