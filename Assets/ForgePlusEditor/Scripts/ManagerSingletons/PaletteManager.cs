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
    // The current mode's palette (textures, lights, media or polygons) and which swatch is selected.
    // The palette panels show it; selecting a swatch here behaves as clicking it there.
    public class PaletteManager : SingletonMonoBehaviour<PaletteManager>
    {
        public enum SwatchKinds
        {
            Texture,
            Light,
            Media,
            Polygon,
        }

        // A texture swatch with no texture, or a media swatch with no media, is the palette's "None" (for removing
        // what's assigned)
        public class Swatch
        {
            public SwatchKinds Kind;
            public ushort ShapeDescriptor = cstypes.UNONE;
            public Texture2D Texture;
            public LevelEntity_Light Light;
            public LevelEntity_Media Media;
            public LevelEntity_Polygon Polygon;

            public bool IsNone
            {
                get
                {
                    return (Kind == SwatchKinds.Texture && ShapeDescriptor.IsEmptyShapeDescriptor()) ||
                           (Kind == SwatchKinds.Media && Media == null);
                }
            }

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
        // (an empty shape descriptor is the "None" texture)
        public void SelectSwatchForTexture(ushort shapeDescriptor)
        {
            Select(swatches.FirstOrDefault(swatch => swatch.Kind == SwatchKinds.Texture && swatch.ShapeDescriptor.Equals(shapeDescriptor)), updateLevelSelection: false);
        }

        // Whether a texture swatch (maybe "None", an empty shape descriptor) is selected, for painting
        public bool TryGetSelectedTexture(out ushort shapeDescriptor)
        {
            var isSelected = SelectedSwatch != null && SelectedSwatch.Kind == SwatchKinds.Texture;
            shapeDescriptor = isSelected ? SelectedSwatch.ShapeDescriptor : cstypes.UNONE;

            return isSelected;
        }

        public void SelectSwatchForLight(LevelEntity_Light light)
        {
            Select(swatches.FirstOrDefault(swatch => swatch.Kind == SwatchKinds.Light && swatch.Light == light), updateLevelSelection: false);
        }

        public LevelEntity_Light GetSelectedLight()
        {
            return SelectedSwatch?.Light;
        }

        // No media deselects the swatches (rather than selecting "None", which is only chosen, for painting)
        public void SelectSwatchForMedia(LevelEntity_Media media)
        {
            Select(media != null ? swatches.FirstOrDefault(swatch => swatch.Kind == SwatchKinds.Media && swatch.Media == media) : null, updateLevelSelection: false);
        }

        // Whether a media swatch (maybe "None", null media) is selected, for painting
        public bool TryGetSelectedMedia(out LevelEntity_Media media)
        {
            var isSelected = SelectedSwatch != null && SelectedSwatch.Kind == SwatchKinds.Media;
            media = isSelected ? SelectedSwatch.Media : null;

            return isSelected;
        }

        // The swatch of the polygon the selected annotation is linked to (none, for no polygon)
        public void SelectSwatchForPolygon(LevelEntity_Polygon polygon)
        {
            Select(polygon ? swatches.FirstOrDefault(swatch => swatch.Polygon == polygon) : null, updateLevelSelection: false);
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

        // Textures and "None" media clear the level's selection (for painting), lights and media select their object,
        // and polygons become the selected annotation's
        private void UpdateLevelSelection(Swatch swatch, bool isSelected)
        {
            if (swatch.Kind == SwatchKinds.Texture || (swatch.IsNone && isSelected))
            {
                SelectionManager.Instance.DeselectAll();
                return;
            }

            if (swatch.IsNone)
            {
                return;
            }

            if (swatch.Kind == SwatchKinds.Polygon)
            {
                if (isSelected)
                {
                    LevelEntity_Annotation.LinkSelectedAnnotation(swatch.Polygon);
                }

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

                        swatches.Add(new Swatch { Kind = SwatchKinds.Texture });

                        foreach (var textureEntry in loadedTextureEntries)
                        {
                            swatches.Add(new Swatch { Kind = SwatchKinds.Texture, ShapeDescriptor = textureEntry.Key, Texture = textureEntry.Value });
                        }

                        break;
                    case ModeManager.PrimaryModes.Lights:
                        // No "None", since a surface always has a light
                        allowSwitchOff = true;

                        foreach (var light in LevelEntity_Level.Instance.Lights.Values)
                        {
                            swatches.Add(new Swatch { Kind = SwatchKinds.Light, Light = light });
                        }

                        break;
                    case ModeManager.PrimaryModes.Media:
                        allowSwitchOff = true;

                        swatches.Add(new Swatch { Kind = SwatchKinds.Media });

                        foreach (var media in LevelEntity_Level.Instance.Medias.Values)
                        {
                            swatches.Add(new Swatch { Kind = SwatchKinds.Media, Media = media });
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
                        // An annotation is always linked to a polygon, so its polygon can be changed but not cleared
                        allowSwitchOff = false;

                        foreach (var polygon in LevelEntity_Level.Instance.Polygons.OrderBy(pair => pair.Key).Select(pair => pair.Value))
                        {
                            swatches.Add(new Swatch { Kind = SwatchKinds.Polygon, Polygon = polygon });
                        }

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

            // The texture palette starts with its first texture selected (after "None")
            var firstTexture = swatches.FirstOrDefault(swatch => swatch.Kind == SwatchKinds.Texture && !swatch.IsNone);
            if (primaryMode == ModeManager.PrimaryModes.Textures && firstTexture != null)
            {
                Select(firstTexture, updateLevelSelection: true);
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
            SelectionManager.Instance.OnSelectionChanged += OnLevelSelectionChanged;
        }

        // The polygon palette shows the selected annotation's polygon
        private void OnLevelSelectionChanged()
        {
            if (ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Annotations)
            {
                var annotation = LevelEntity_Annotation.SelectedAnnotation;
                SelectSwatchForPolygon(annotation ? annotation.LinkedPolygon : null);
            }
        }

        private void OnClickEmptySpace()
        {
            Select(null, updateLevelSelection: true);
        }
    }
}
