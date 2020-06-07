using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using RuntimeCore.Materials;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        private ToggleGroup paletteToggleGroup = null;

        [SerializeField]
        private Swatch_Texture textureSwatchPrefab = null;

        [SerializeField]
        private Swatch_Light lightSwatchPrefab = null;

        [SerializeField]
        private Swatch_Media mediaSwatchPrefab = null;

        [SerializeField]
        private GameObject horizontalLayoutHelperPrefab = null;

        [SerializeField]
        private Toggle[] secondaryModeToggles = new Toggle[] { };

        private readonly List<GameObject> currentSwatches = new List<GameObject>();
        private readonly List<GameObject> currentLayoutHelpers = new List<GameObject>();

        public void SelectSwatchForTexture(ShapeDescriptor shapeDescriptor, bool invokeToggleEvents = false)
        {
            var matchingSwatch = currentSwatches.First(swatch => swatch.GetComponent<Swatch_Texture>().ShapeDescriptor.Equals(shapeDescriptor));
            var matchingToggle = matchingSwatch.GetComponent<Toggle>();

            ActivateToggle(matchingToggle, invokeToggleEvents);
        }

        public ShapeDescriptor GetSelectedTexture()
        {
            var activeToggle = paletteToggleGroup.GetFirstActiveToggle();

            if (!activeToggle)
            {
                return ShapeDescriptor.Empty;
            }

            return activeToggle.GetComponent<Swatch_Texture>().ShapeDescriptor;
        }

        public void SelectSwatchForLight(LevelEntity_Light fpLight, bool invokeToggleEvents = false)
        {
            var matchingSwatch = currentSwatches.First(swatch => swatch.GetComponent<Swatch_Light>().FPLight == fpLight);
            var matchingToggle = matchingSwatch.GetComponent<Toggle>();

            ActivateToggle(matchingToggle, invokeToggleEvents);
        }

        public LevelEntity_Light GetSelectedFPLight()
        {
            var activeToggle = paletteToggleGroup.GetFirstActiveToggle();

            if (!activeToggle)
            {
                return null;
            }

            return activeToggle.GetComponent<Swatch_Light>().FPLight;
        }

        public void SelectSwatchForMedia(LevelEntity_Media fpMedia, bool invokeToggleEvents = false)
        {
            var matchingSwatch = currentSwatches.First(swatch => swatch.GetComponent<Swatch_Media>().FPMedia == fpMedia);
            var matchingToggle = matchingSwatch.GetComponent<Toggle>();

            ActivateToggle(matchingToggle, invokeToggleEvents);
        }

        public LevelEntity_Media GetSelectedMedia()
        {
            var activeToggle = paletteToggleGroup.GetFirstActiveToggle();

            if (!activeToggle)
            {
                return null;
            }

            return activeToggle.GetComponent<Swatch_Media>().FPMedia;
        }

        private void UpdatePaletteToMatchMode(ModeManager.PrimaryModes primaryMode)
        {
            Clear();

            if (LevelEntity_Level.Instance)
            {
                switch (primaryMode)
            {
                case ModeManager.PrimaryModes.Geometry:
                    paletteToggleGroup.allowSwitchOff = false;

                    // TODO: populate with tools
                    //       - Line Drawing
                    //       - Polygon Fill (oh man... should this auto-fill when a poly is completed?)

                    break;

                case ModeManager.PrimaryModes.Textures:
                    paletteToggleGroup.allowSwitchOff = false;

                    var loadedTextureEntries = MaterialGeneration_Geometry.GetAllLoadedTextures().ToList();
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
                            swatch.SetInitialValues(textureEntry, paletteToggleGroup);
                            currentSwatches.Add(swatch.gameObject);
                        }
                        else
                        {
                            if (!currentHorizontalHelper)
                            {
                                currentHorizontalHelper = Instantiate(horizontalLayoutHelperPrefab, swatchesParent);
                                currentLayoutHelpers.Add(currentHorizontalHelper);

                                var swatch = Instantiate(textureSwatchPrefab, currentHorizontalHelper.transform);
                                swatch.SetInitialValues(textureEntry, paletteToggleGroup);
                                currentSwatches.Add(swatch.gameObject);
                            }
                            else
                            {
                                var swatch = Instantiate(textureSwatchPrefab, currentHorizontalHelper.transform);
                                swatch.SetInitialValues(textureEntry, paletteToggleGroup);
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

                    break;

                case ModeManager.PrimaryModes.Lights:
                    paletteToggleGroup.allowSwitchOff = true;

                    foreach (var fpLight in LevelEntity_Level.Instance.FPLights.Values)
                    {
                        var swatch = Instantiate(lightSwatchPrefab, swatchesParent);
                        swatch.SetInitialValues(fpLight, paletteToggleGroup);
                        currentSwatches.Add(swatch.gameObject);
                    }

                    break;

                case ModeManager.PrimaryModes.Media:
                    paletteToggleGroup.allowSwitchOff = true;

                    foreach (var fpMedia in LevelEntity_Level.Instance.FPMedias.Values)
                    {
                        var swatch = Instantiate(mediaSwatchPrefab, swatchesParent);
                        swatch.SetInitialValues(fpMedia, paletteToggleGroup);
                        currentSwatches.Add(swatch.gameObject);
                    }

                    break;

                case ModeManager.PrimaryModes.Platforms:
                    paletteToggleGroup.allowSwitchOff = true;

                    // TODO: populate with shortcuts that focus the camera on the associated platform polygon when clicked.

                    break;

                case ModeManager.PrimaryModes.Objects:
                    paletteToggleGroup.allowSwitchOff = false;

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

                    break;

                case ModeManager.PrimaryModes.Annotations:
                    break;

                case ModeManager.PrimaryModes.Level:
                    break;

                case ModeManager.PrimaryModes.None:
                default:
                    break;
            }
            }
        }

        private void Clear()
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

        private void UpdateSecondaryModeToggleAvailabilityToMatchPrimaryMode(ModeManager.PrimaryModes primaryMode)
        {
            switch (primaryMode)
            {
                case ModeManager.PrimaryModes.Textures:
                    foreach (var toggle in secondaryModeToggles)
                    {
                        toggle.interactable = true;
                    }

                    break;
                default:
                    foreach (var toggle in secondaryModeToggles)
                    {
                        if (toggle.GetComponent<Toggle_SelectSecondaryMode>().Mode == ModeManager.SecondaryModes.Selection)
                        {
                            toggle.interactable = true;
                        }
                        else
                        {
                            toggle.interactable = false;
                        }
                    }

                    ModeManager.Instance.SecondaryMode = ModeManager.SecondaryModes.Selection;

                    break;
            }
        }

        private void UpdateSecondaryModeTogglesToMatchSecondaryMode(ModeManager.SecondaryModes secondaryMode)
        {
            if (secondaryMode == ModeManager.SecondaryModes.None)
            {
                return;
            }

            var modeToggle = secondaryModeToggles.First(toggle => toggle.GetComponent<Toggle_SelectSecondaryMode>().Mode == secondaryMode);
            ActivateToggle(modeToggle, invokeToggleEvents: false);
        }

        private void ActivateToggle(Toggle toggle, bool invokeToggleEvents)
        {
            if (invokeToggleEvents)
            {
                toggle.isOn = true;
            }
            else
            {
                toggle.SetIsOnWithoutNotify(true);
            }
        }

        private void Start()
        {
            ModeManager.Instance.OnPrimaryModeChanged += UpdatePaletteToMatchMode;
            ModeManager.Instance.OnPrimaryModeChanged += UpdateSecondaryModeToggleAvailabilityToMatchPrimaryMode;
            ModeManager.Instance.OnSecondaryModeChanged += UpdateSecondaryModeTogglesToMatchSecondaryMode;
            SelectionManager.Instance.OnClickEmptySpace += OnClickEmptySpace;
        }

        private void OnClickEmptySpace()
        {
            paletteToggleGroup.SetAllTogglesOff(sendCallback: true);
        }
    }
}
