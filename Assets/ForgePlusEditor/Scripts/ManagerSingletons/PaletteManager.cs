using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForgePlus.Palette
{
    // The current mode's palette (textures, lights or media) and which swatch is selected.
    // The palette panels show it; selecting a swatch here behaves as clicking it there.
    public class PaletteManager : SingletonMonoBehaviour<PaletteManager>
    {
        public class Swatch
        {
            public ushort ShapeDescriptor = cstypes.UNONE;
            public Texture2D Texture;
            public LevelEntity_Light Light;
            public LevelEntity_Media Media;

            public bool IsLandscape
            {
                get
                {
                    return Texture && ShapeDescriptor.UsesLandscapeCollection();
                }
            }
        }

        public event Action OnSwatchesChanged;
        public event Action OnSelectionChanged;

        private readonly List<Swatch> swatches = new List<Swatch>();

        // Whether clicking the selected swatch deselects it
        private bool allowSwitchOff = false;

        public IReadOnlyList<Swatch> Swatches
        {
            get
            {
                return swatches;
            }
        }

        public Swatch SelectedSwatch { get; private set; }

        // The SelectSwatchFor methods show a swatch as selected, to match what's already selected in the level
        public void SelectSwatchForTexture(ushort shapeDescriptor)
        {
            Select(swatches.First(swatch => swatch.Texture && swatch.ShapeDescriptor.Equals(shapeDescriptor)), updateLevelSelection: false);
        }

        public ushort GetSelectedTexture()
        {
            return SelectedSwatch != null && SelectedSwatch.Texture ? SelectedSwatch.ShapeDescriptor : cstypes.UNONE;
        }

        public void SelectSwatchForLight(LevelEntity_Light light)
        {
            Select(swatches.First(swatch => swatch.Light == light), updateLevelSelection: false);
        }

        public LevelEntity_Light GetSelectedLight()
        {
            return SelectedSwatch?.Light;
        }

        public void SelectSwatchForMedia(LevelEntity_Media media)
        {
            Select(swatches.First(swatch => swatch.Media == media), updateLevelSelection: false);
        }

        public LevelEntity_Media GetSelectedMedia()
        {
            return SelectedSwatch?.Media;
        }

        // Selects the swatch, or deselects it if it was selected and the palette allows that
        public void Click(Swatch swatch)
        {
            if (swatch != SelectedSwatch)
            {
                Select(swatch, updateLevelSelection: true);
            }
            else if (allowSwitchOff)
            {
                Select(null, updateLevelSelection: true);
            }
        }

        private void Select(Swatch swatch, bool updateLevelSelection)
        {
            var previousSwatch = SelectedSwatch;
            if (swatch == previousSwatch)
            {
                return;
            }

            SelectedSwatch = swatch;

            // The previous swatch's object is deselected before the new one's is selected
            if (updateLevelSelection)
            {
                if (previousSwatch != null)
                {
                    UpdateLevelSelection(previousSwatch, isSelected: false);
                }

                if (swatch != null)
                {
                    UpdateLevelSelection(swatch, isSelected: true);
                }
            }

            OnSelectionChanged?.Invoke();
        }

        // Textures clear the level's selection (for painting), and lights and media select their object
        private void UpdateLevelSelection(Swatch swatch, bool isSelected)
        {
            if (swatch.Texture)
            {
                SelectionManager.Instance.DeselectAll();
                return;
            }

            var selectable = (ISelectable)swatch.Light ?? swatch.Media;
            if (isSelected)
            {
                SelectionManager.Instance.ToggleObjectSelection(selectable, multiSelect: false);
            }
            else
            {
                SelectionManager.Instance.DeselectObject(selectable, multiSelect: false);
            }
        }

        private void UpdatePaletteToMatchMode(ModeManager.PrimaryModes primaryMode)
        {
            swatches.Clear();
            SelectedSwatch = null;

            if (LevelEntity_Level.Instance)
            {
                switch (primaryMode)
                {
                    case ModeManager.PrimaryModes.Geometry:
                        allowSwitchOff = false;
                        // TODO: populate with tools
                        //       - Line Drawing
                        //       - Polygon Fill (oh man... should this auto-fill when a poly is completed?)
                        break;
                    case ModeManager.PrimaryModes.Textures:
                        allowSwitchOff = false;

                        var loadedTextureEntries = MaterialGeneration_Geometry.GetAllLoadedTextures().ToList();
                        loadedTextureEntries.Sort((entryA, entryB) => (entryA.Key.GetCollection() == entryB.Key.GetCollection() ?
                                                                       entryA.Key.GetShape().CompareTo(entryB.Key.GetShape()) :
                                                                       ((entryA.Key.UsesLandscapeCollection() || entryB.Key.UsesLandscapeCollection()) ?
                                                                        -entryA.Key.GetCollection().CompareTo(entryB.Key.GetCollection()) :
                                                                        entryA.Key.GetCollection().CompareTo(entryB.Key.GetCollection()))));

                        foreach (var textureEntry in loadedTextureEntries)
                        {
                            swatches.Add(new Swatch { ShapeDescriptor = textureEntry.Key, Texture = textureEntry.Value });
                        }

                        break;
                    case ModeManager.PrimaryModes.Lights:
                        allowSwitchOff = true;

                        foreach (var light in LevelEntity_Level.Instance.Lights.Values)
                        {
                            swatches.Add(new Swatch { Light = light });
                        }

                        break;
                    case ModeManager.PrimaryModes.Media:
                        allowSwitchOff = true;

                        foreach (var media in LevelEntity_Level.Instance.Medias.Values)
                        {
                            swatches.Add(new Swatch { Media = media });
                        }

                        break;
                    case ModeManager.PrimaryModes.Platforms:
                        allowSwitchOff = true;
                        // TODO: populate with shortcuts that focus the camera on the associated platform polygon when clicked.
                        break;
                    case ModeManager.PrimaryModes.Objects:
                        allowSwitchOff = false;
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
                    case ModeManager.PrimaryModes.Terminals:
                        break;
                    case ModeManager.PrimaryModes.None:
                    default:
                        break;
                }
            }

            OnSwatchesChanged?.Invoke();

            // The texture palette starts with its first texture selected
            if (primaryMode == ModeManager.PrimaryModes.Textures && swatches.Count > 0)
            {
                Select(swatches[0], updateLevelSelection: true);
            }
            else
            {
                OnSelectionChanged?.Invoke();
            }
        }

        private void Start()
        {
            ModeManager.Instance.OnPrimaryModeChanged += UpdatePaletteToMatchMode;
            SelectionManager.Instance.OnClickEmptySpace += OnClickEmptySpace;
        }

        private void OnClickEmptySpace()
        {
            Select(null, updateLevelSelection: true);
        }
    }
}
