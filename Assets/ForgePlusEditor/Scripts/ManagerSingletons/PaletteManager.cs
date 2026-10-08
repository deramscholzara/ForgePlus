using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation;
using ForgePlus.UI;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForgePlus.Palette
{
    // The current mode's palette and which swatch is selected (in each of its sets, for one with several).
    // The palette panels show it; selecting a swatch here behaves as clicking it there.
    public class PaletteManager : SingletonMonoBehaviour<PaletteManager>
    {
        public enum SwatchKinds
        {
            Texture,
            Light,
            Media,
            Polygon,
            Platform,
            AmbientSound,
            RandomSound,
            FloorHeight,
            CeilingHeight,
        }

        // A texture or media swatch with none is the palette's "None", for removing what's assigned
        public class Swatch
        {
            public SwatchKinds Kind;
            public ushort ShapeDescriptor = cstypes.UNONE;
            public Texture2D Texture;
            public LevelEntity_Light Light;
            public LevelEntity_Media Media;
            public LevelEntity_Polygon Polygon;
            public LevelEntity_Platform Platform;

            // Which of the palette's sets it's in (such as the Sounds palette's ambient and random lists), each of which
            // can show a swatch selected at once, though painting paints with one
            public int Group;

            // NONE for the list's "None"
            public short SoundIndex = cstypes.NONE;

            // A floor or ceiling height swatch's
            public short Height;

            public bool IsHeight
            {
                get
                {
                    return Kind == SwatchKinds.FloorHeight || Kind == SwatchKinds.CeilingHeight;
                }
            }

            public LevelEntity_Polygon.DataSources HeightDataSource
            {
                get
                {
                    return Kind == SwatchKinds.CeilingHeight ? LevelEntity_Polygon.DataSources.Ceiling : LevelEntity_Polygon.DataSources.Floor;
                }
            }

            public bool IsSound
            {
                get
                {
                    return Kind == SwatchKinds.AmbientSound || Kind == SwatchKinds.RandomSound;
                }
            }

            public SoundImageKinds SoundKind
            {
                get
                {
                    return Kind == SwatchKinds.AmbientSound ? SoundImageKinds.Ambient : SoundImageKinds.Random;
                }
            }

            public bool IsNone
            {
                get
                {
                    return (Kind == SwatchKinds.Texture && ShapeDescriptor.IsEmptyShapeDescriptor()) ||
                           (Kind == SwatchKinds.Media && Media == null) ||
                           (IsSound && SoundIndex < 0);
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

        // By group
        private readonly Dictionary<int, Swatch> selectedSwatches = new Dictionary<int, Swatch>();

        // The set whose swatch painting keeps
        private int lastSelectedGroup;

        // While a swatch's selection is changing the level's (whose change it shouldn't then follow)
        private bool isUpdatingLevelSelection;

        public IReadOnlyList<Swatch> Swatches
        {
            get
            {
                return swatches;
            }
        }

        // The selected swatch of the first set
        public Swatch SelectedSwatch
        {
            get
            {
                return GetSelectedSwatch(0);
            }
        }

        public Swatch GetSelectedSwatch(int group)
        {
            return selectedSwatches.TryGetValue(group, out var swatch) ? swatch : null;
        }

        public bool IsSelected(Swatch swatch)
        {
            return swatch != null && GetSelectedSwatch(swatch.Group) == swatch;
        }

        // The Sounds palette's set of each list
        public static int SoundGroup(SoundImageKinds kind)
        {
            return kind == SoundImageKinds.Ambient ? 0 : 1;
        }

        // The SelectSwatchFor methods show a swatch as selected, to match what's already selected in the level
        // (an empty shape descriptor is the "None" texture)
        public void SelectSwatchForTexture(ushort shapeDescriptor)
        {
            Select(swatches.FirstOrDefault(swatch => swatch.Kind == SwatchKinds.Texture && swatch.ShapeDescriptor.Equals(shapeDescriptor)), updateLevelSelection: false);
        }

        // The loaded textures in the palette's order: landscapes first, then each collection's in order
        public static List<KeyValuePair<ushort, Texture2D>> SortedLoadedTextures()
        {
            var loadedTextureEntries = MaterialGeneration_Geometry.GetAllLoadedTextures().ToList();
            loadedTextureEntries.Sort((entryA, entryB) => (entryA.Key.GetCollection() == entryB.Key.GetCollection() ?
                                                           entryA.Key.GetShape().CompareTo(entryB.Key.GetShape()) :
                                                           ((entryA.Key.UsesLandscapeCollection() || entryB.Key.UsesLandscapeCollection()) ?
                                                            -entryA.Key.GetCollection().CompareTo(entryB.Key.GetCollection()) :
                                                            entryA.Key.GetCollection().CompareTo(entryB.Key.GetCollection()))));

            return loadedTextureEntries;
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

        // No media deselects the swatches ("None" is only chosen for painting)
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

        public void SelectSwatchForPolygon(LevelEntity_Polygon polygon)
        {
            Select(polygon ? swatches.FirstOrDefault(swatch => swatch.Polygon == polygon) : null, updateLevelSelection: false);
        }

        // Either half of a platform that goes both ways
        public void SelectSwatchForPlatform(LevelEntity_Platform platform)
        {
            Select(platform ? swatches.FirstOrDefault(swatch => swatch.Kind == SwatchKinds.Platform && swatch.Platform.NativeIndex == platform.NativeIndex) : null, updateLevelSelection: false);
        }

        // Whether a swatch of the list (maybe its "None", NONE) is selected, for painting
        public bool TryGetSelectedSound(SoundImageKinds kind, out short index)
        {
            var swatch = GetSelectedSwatch(SoundGroup(kind));
            index = swatch != null ? swatch.SoundIndex : cstypes.NONE;

            return swatch != null;
        }

        // In its list's set, keeping the other list's
        public void SelectSwatchForSound(SoundImageEntry entry)
        {
            Select(FindSoundSwatch(entry.Kind, entry.Index), updateLevelSelection: false);
        }

        // Each list's "None" for no sound
        public void SelectSwatchesForPolygonSounds(LevelEntity_Polygon polygon)
        {
            foreach (var kind in SoundImageEditing.Kinds)
            {
                var swatch = FindSoundSwatch(kind, SoundImageEditing.IndexOf(polygon.NativeObject, kind));
                SetSelected(SoundGroup(kind), swatch, updateLevelSelection: false);
            }
        }

        // Selects the sound's swatch as clicking it does (such as for an entry just added)
        public void ClickSound(SoundImageKinds kind, short index)
        {
            var swatch = FindSoundSwatch(kind, index);
            if (swatch != null && !IsSelected(swatch))
            {
                Select(swatch, updateLevelSelection: true);
            }
        }

        private Swatch FindSoundSwatch(SoundImageKinds kind, short index)
        {
            return swatches.FirstOrDefault(swatch => swatch.IsSound && swatch.SoundKind == kind && swatch.SoundIndex == index);
        }

        // The Heights palette's set of floors or of ceilings (one swatch is selected across both)
        public static int HeightGroup(LevelEntity_Polygon.DataSources dataSource)
        {
            return dataSource == LevelEntity_Polygon.DataSources.Ceiling ? 1 : 0;
        }

        // The face's height (no polygon deselects the swatches)
        public void SelectSwatchForHeight(LevelEntity_Polygon polygon, LevelEntity_Polygon.DataSources dataSource)
        {
            if (!polygon)
            {
                ClearSelection(updateLevelSelection: false);
                return;
            }

            Select(FindHeightSwatch(dataSource, HeightsEditing.GetHeight(polygon, dataSource)), updateLevelSelection: false);
        }

        // Whether the selected swatch is the face's height
        public bool ShowsHeightOf(LevelEntity_Polygon polygon, LevelEntity_Polygon.DataSources dataSource)
        {
            var swatch = GetSelectedSwatch(HeightGroup(dataSource));

            return swatch != null && swatch.IsHeight && swatch.Height == HeightsEditing.GetHeight(polygon, dataSource);
        }

        // Whether a height swatch is selected (for painting floors or ceilings, as its set is)
        public bool TryGetSelectedHeight(out LevelEntity_Polygon.DataSources dataSource, out short height)
        {
            var swatch = selectedSwatches.Values.FirstOrDefault(selected => selected.IsHeight);

            dataSource = swatch != null ? swatch.HeightDataSource : LevelEntity_Polygon.DataSources.Floor;
            height = swatch != null ? swatch.Height : (short) 0;

            return swatch != null;
        }

        // Selects the height's swatch as clicking it does (such as for one just added)
        public void ClickHeight(LevelEntity_Polygon.DataSources dataSource, short height)
        {
            var swatch = FindHeightSwatch(dataSource, height);
            if (swatch != null && !IsSelected(swatch))
            {
                Select(swatch, updateLevelSelection: true);
            }
        }

        private Swatch FindHeightSwatch(LevelEntity_Polygon.DataSources dataSource, short height)
        {
            return swatches.FirstOrDefault(swatch => swatch.IsHeight && swatch.HeightDataSource == dataSource && swatch.Height == height);
        }

        // For a swatch's contents changing (such as a platform's type)
        public void RefreshSwatches()
        {
            OnSwatchesChanged?.Invoke();
        }

        // Selects the swatch (in its set), or deselects it if it was selected and the palette allows that
        public void Click(Swatch swatch)
        {
            if (!IsSelected(swatch))
            {
                Select(swatch, updateLevelSelection: true);
            }
            else if (allowSwitchOff)
            {
                SetSelected(swatch.Group, null, updateLevelSelection: true);
            }
        }

        // Selects the swatch in its set (or, for none, deselects the first set's)
        private void Select(Swatch swatch, bool updateLevelSelection)
        {
            SetSelected(swatch != null ? swatch.Group : 0, swatch, updateLevelSelection);
        }

        private void ClearSelection(bool updateLevelSelection)
        {
            foreach (var group in selectedSwatches.Keys.ToList())
            {
                SetSelected(group, null, updateLevelSelection);
            }
        }

        private void SetSelected(int group, Swatch swatch, bool updateLevelSelection)
        {
            var previousSwatch = GetSelectedSwatch(group);
            if (swatch == previousSwatch)
            {
                return;
            }

            if (swatch != null)
            {
                selectedSwatches[group] = swatch;

                // Painting paints with one swatch, so choosing one in a set deselects the others' (as heights always do)
                if (ModeManager.Instance.SecondaryMode == ModeManager.SecondaryModes.Painting || swatch.IsHeight)
                {
                    KeepOnlyGroup(group);
                }

                lastSelectedGroup = group;
            }
            else
            {
                selectedSwatches.Remove(group);
            }

            // The previous swatch's object is deselected before the new one's is selected
            if (updateLevelSelection)
            {
                isUpdatingLevelSelection = true;

                try
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
                finally
                {
                    isUpdatingLevelSelection = false;
                }
            }

            OnSelectionChanged?.Invoke();
        }

        // Textures and "None" clear the level's selection (for painting), lights and media select their object, and
        // polygons link to the selected annotation
        private void UpdateLevelSelection(Swatch swatch, bool isSelected)
        {
            // A height is picked to paint with or to change, not to select anything
            if (swatch.IsHeight)
            {
                return;
            }

            if (swatch.Kind == SwatchKinds.Texture || (swatch.IsNone && isSelected))
            {
                SelectionManager.Instance.DeselectAll();
                return;
            }

            if (swatch.IsNone)
            {
                return;
            }

            // A sound's entry is selected to inspect it, in either tool mode
            if (swatch.IsSound)
            {
                var entry = SoundImageEditing.GetEntry(swatch.SoundKind, swatch.SoundIndex);
                if (entry == null)
                {
                    return;
                }

                if (isSelected)
                {
                    SelectionManager.Instance.SelectObject(entry, multiSelect: false);
                }
                else
                {
                    SelectionManager.Instance.DeselectObject(entry, multiSelect: false);
                }

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

            // Clicking the selected platform deselects it, as in the level
            if (swatch.Kind == SwatchKinds.Platform)
            {
                if (isSelected)
                {
                    FocusPlatform(swatch.Platform);
                }
                else if (SelectedSwatch == null)
                {
                    SelectionManager.Instance.DeselectObject(swatch.Platform, multiSelect: false);
                }

                return;
            }

            var selectable = (ISelectable)swatch.Light ?? swatch.Media;
            if (isSelected)
            {
                // Selected from the palette, not on any face
                SelectionManager.Instance.ClickedSurface = null;

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
            selectedSwatches.Clear();

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

                        swatches.Add(new Swatch { Kind = SwatchKinds.Texture });

                        foreach (var textureEntry in SortedLoadedTextures())
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
                    case ModeManager.PrimaryModes.Sounds:
                        allowSwitchOff = true;

                        // Each list, after its "None" (for painting a polygon's sound off)
                        foreach (var kind in SoundImageEditing.Kinds)
                        {
                            var swatchKind = kind == SoundImageKinds.Ambient ? SwatchKinds.AmbientSound : SwatchKinds.RandomSound;
                            var count = SoundImageEditing.Count(kind);

                            for (short index = cstypes.NONE; index < count; index++)
                            {
                                swatches.Add(new Swatch { Kind = swatchKind, SoundIndex = index, Group = SoundGroup(kind) });
                            }
                        }

                        break;
                    case ModeManager.PrimaryModes.Heights:
                        allowSwitchOff = true;

                        foreach (var dataSource in new[] { LevelEntity_Polygon.DataSources.Floor, LevelEntity_Polygon.DataSources.Ceiling })
                        {
                            var swatchKind = dataSource == LevelEntity_Polygon.DataSources.Floor ? SwatchKinds.FloorHeight : SwatchKinds.CeilingHeight;

                            foreach (var height in HeightSwatches.Heights(dataSource))
                            {
                                swatches.Add(new Swatch { Kind = swatchKind, Height = height, Group = HeightGroup(dataSource) });
                            }
                        }

                        break;
                    case ModeManager.PrimaryModes.Platforms:
                        allowSwitchOff = true;

                        // One swatch for a platform that goes both ways, and none for one that moves neither way
                        var runtimeLevel = LevelEntity_Level.Instance;
                        for (short platformIndex = 0; platformIndex < runtimeLevel.Level.PlatformList.Count; platformIndex++)
                        {
                            var platform = LevelEntity_Platform.GetSelectablePlatform(runtimeLevel, platformIndex);
                            if (platform)
                            {
                                swatches.Add(new Swatch { Kind = SwatchKinds.Platform, Platform = platform });
                            }
                        }
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
                    case ModeManager.PrimaryModes.Map:
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
            ModeManager.Instance.OnSecondaryModeChanged += OnSecondaryModeChanged;
            SelectionManager.Instance.OnClickEmptySpace += OnClickEmptySpace;
            SelectionManager.Instance.OnSelectionChanged += OnLevelSelectionChanged;
            SoundImageEditing.OnChanged += OnSoundImagesChanged;
            HeightSwatches.OnChanged += OnHeightsChanged;
            HeightsEditing.OnHeightsChanged += OnHeightsChanged;
        }

        private void OnDestroy()
        {
            HeightSwatches.OnChanged -= OnHeightsChanged;
            HeightsEditing.OnHeightsChanged -= OnHeightsChanged;
        }

        // The listed heights follow the level's, keeping the selected one
        private void OnHeightsChanged()
        {
            if (ModeManager.Instance.PrimaryMode != ModeManager.PrimaryModes.Heights)
            {
                return;
            }

            var previous = selectedSwatches.Values.Where(swatch => swatch.IsHeight).ToList();
            UpdatePaletteToMatchMode(ModeManager.PrimaryModes.Heights);

            foreach (var swatch in previous)
            {
                SetSelected(swatch.Group, FindHeightSwatch(swatch.HeightDataSource, swatch.Height), updateLevelSelection: false);
            }
        }

        // The palette shows what's selected in the level
        private void OnLevelSelectionChanged()
        {
            switch (ModeManager.Instance.PrimaryMode)
            {
                case ModeManager.PrimaryModes.Annotations:
                    var annotation = LevelEntity_Annotation.SelectedAnnotation;
                    SelectSwatchForPolygon(annotation ? annotation.LinkedPolygon : null);
                    break;
                case ModeManager.PrimaryModes.Platforms:
                    SelectSwatchForPlatform(SelectionManager.Instance.SelectedObject as LevelEntity_Platform);
                    break;
                case ModeManager.PrimaryModes.Heights:
                    // Deselecting the polygon deselects its height, unless that's picked for painting
                    if (ModeManager.Instance.SecondaryMode != ModeManager.SecondaryModes.Painting &&
                        !isUpdatingLevelSelection &&
                        SelectionManager.Instance.SelectedObject == null)
                    {
                        ClearSelection(updateLevelSelection: false);
                    }

                    break;
                case ModeManager.PrimaryModes.Sounds:
                    // The swatches picked for painting stay picked; otherwise the palette shows the selection's sounds
                    if (ModeManager.Instance.SecondaryMode != ModeManager.SecondaryModes.Painting && !isUpdatingLevelSelection)
                    {
                        var selectedObject = SelectionManager.Instance.SelectedObject;

                        if (selectedObject is LevelEntity_Polygon selectedPolygon)
                        {
                            SelectSwatchesForPolygonSounds(selectedPolygon);
                        }
                        else if (selectedObject is SoundImageEntry selectedEntry)
                        {
                            SelectSwatchForSound(selectedEntry);
                        }
                        else if (selectedObject == null)
                        {
                            ClearSelection(updateLevelSelection: false);
                        }
                    }

                    break;
            }
        }

        // The swatch picked for painting stays picked
        private void OnSoundImagesChanged()
        {
            if (ModeManager.Instance.PrimaryMode != ModeManager.PrimaryModes.Sounds)
            {
                return;
            }

            // The swatches (each list's, after its "None") are the same while the lists are as long
            var swatchCount = SoundImageEditing.Kinds.Sum(kind => 1 + SoundImageEditing.Count(kind));
            if (swatches.Count == swatchCount)
            {
                return;
            }

            var previous = selectedSwatches.Values.ToList();
            UpdatePaletteToMatchMode(ModeManager.PrimaryModes.Sounds);

            foreach (var swatch in previous)
            {
                if (swatch.IsSound)
                {
                    SetSelected(swatch.Group, FindSoundSwatch(swatch.SoundKind, swatch.SoundIndex), updateLevelSelection: false);
                }
            }
        }

        private static void FocusPlatform(LevelEntity_Platform platform)
        {
            SelectionManager.Instance.SelectObject(platform, multiSelect: false);
            ForgePlusUI.Instance.EditorCamera.FrameSelected();
        }

        // Painting paints with one swatch, so it keeps only the one selected last
        private void OnSecondaryModeChanged(ModeManager.SecondaryModes secondaryMode)
        {
            if (secondaryMode != ModeManager.SecondaryModes.Painting || selectedSwatches.Count < 2)
            {
                return;
            }

            KeepOnlyGroup(lastSelectedGroup);

            OnSelectionChanged?.Invoke();
        }

        private void KeepOnlyGroup(int group)
        {
            foreach (var otherGroup in selectedSwatches.Keys.Where(key => key != group).ToList())
            {
                selectedSwatches.Remove(otherGroup);
            }
        }

        private void OnClickEmptySpace()
        {
            ClearSelection(updateLevelSelection: true);
        }
    }
}
