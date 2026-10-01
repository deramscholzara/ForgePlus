using AlephOne;
using ForgePlus.LevelManipulation;
using ForgePlus.Palette;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The level's ambient and random sounds, each list after its "None", with buttons for adding an entry to a list and
    // duplicating its selected one. Each swatch shows its sound, its volume, and whether polygons play it. An entry past
    // the original engine's limit (64 in each list) is Aleph One only.
    public class SoundPalettePanel : UIPanel
    {
        private readonly List<KeyValuePair<PaletteManager.Swatch, TemplateContainer>> swatchInstances = new List<KeyValuePair<PaletteManager.Swatch, TemplateContainer>>();

        private VisualTreeAsset swatchTemplate;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/SoundPalette";
            }
        }

        protected override void OnLoaded()
        {
            swatchTemplate = LoadTemplate("SwatchSound");

            Root.Find<Button>("add-ambient").clicked += () => Add(SoundImageKinds.Ambient);
            Root.Find<Button>("add-random").clicked += () => Add(SoundImageKinds.Random);
            Root.Find<Button>("duplicate-ambient").clicked += () => Duplicate(SoundImageKinds.Ambient);
            Root.Find<Button>("duplicate-random").clicked += () => Duplicate(SoundImageKinds.Random);

            PaletteManager.Instance.OnSwatchesChanged += Rebuild;
            PaletteManager.Instance.OnSelectionChanged += ShowSelection;
            SoundImageEditing.OnChanged += ShowContents;

            Rebuild();
        }

        protected override void OnUnloading()
        {
            var paletteManager = PaletteManager.Instance;
            if (paletteManager)
            {
                paletteManager.OnSwatchesChanged -= Rebuild;
                paletteManager.OnSelectionChanged -= ShowSelection;
            }

            SoundImageEditing.OnChanged -= ShowContents;

            swatchInstances.Clear();
        }

        private void Rebuild()
        {
            var ambientSwatches = Root.Q("ambient-swatches");
            var randomSwatches = Root.Q("random-swatches");
            ambientSwatches.Clear();
            randomSwatches.Clear();
            swatchInstances.Clear();

            foreach (var swatch in PaletteManager.Instance.Swatches)
            {
                if (!swatch.IsSound)
                {
                    continue;
                }

                var instance = swatchTemplate.Instantiate();
                var toggle = instance.Q<Toggle>();
                var clickedSwatch = swatch;

                toggle.RegisterValueChangedCallback(changeEvent =>
                {
                    PaletteManager.Instance.Click(clickedSwatch);

                    // The palette decides what's selected (the click may not have changed it)
                    ShowSelection();
                });

                swatchInstances.Add(new KeyValuePair<PaletteManager.Swatch, TemplateContainer>(swatch, instance));
                (swatch.SoundKind == SoundImageKinds.Ambient ? ambientSwatches : randomSwatches).Add(instance);
            }

            Root.IgnoreLayoutPicking();

            ShowContents();
            ShowSelection();
        }

        // What each swatch shows, and the buttons (which change with the lists)
        private void ShowContents()
        {
            var level = SoundImageEditing.Level;
            if (level == null)
            {
                return;
            }

            foreach (var swatchInstance in swatchInstances)
            {
                var swatch = swatchInstance.Key;
                var instance = swatchInstance.Value;
                var index = swatch.SoundIndex;

                if (swatch.IsNone || index >= SoundImageEditing.Count(swatch.SoundKind))
                {
                    instance.Q<Label>("index").text = string.Empty;
                    instance.Q<Label>("sound").text = "None";
                    instance.Q("volume").style.display = DisplayStyle.None;
                    instance.Q("in-use").style.display = DisplayStyle.None;
                    continue;
                }

                string sound;
                int volume;
                if (swatch.SoundKind == SoundImageKinds.Ambient)
                {
                    var entry = level.AmbientSoundImageList[index];
                    sound = SoundImageDescriptions.AmbientSoundName(entry.sound_index);
                    volume = entry.volume;
                }
                else
                {
                    var entry = level.RandomSoundImageList[index];
                    sound = SoundImageDescriptions.RandomSoundName(entry.sound_index);
                    volume = entry.volume;
                }

                instance.Q<Label>("index").text = index.ToString();
                instance.Q<Label>("sound").text = sound;
                instance.Q("volume").style.display = DisplayStyle.Flex;
                instance.Q("volume-fill").style.height = Length.Percent(VolumeBars.Percent(volume));
                instance.Q("in-use").style.display = SoundImageEditing.UsageCount(swatch.SoundKind, index) > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                instance.EnableInClassList("fp-aleph-one-only", index >= SoundImageEditing.MaximumOriginalEntries);
            }

            ShowAddButton(SoundImageKinds.Ambient, Root.Find<Button>("add-ambient"));
            ShowAddButton(SoundImageKinds.Random, Root.Find<Button>("add-random"));
        }

        // Adding past the original engine's limit makes an Aleph One only entry
        private static void ShowAddButton(SoundImageKinds kind, Button button)
        {
            var isAlephOneOnly = SoundImageEditing.Count(kind) >= SoundImageEditing.MaximumOriginalEntries;

            button.text = isAlephOneOnly ? "Add (Aleph One Only)" : "Add";
            button.GetFirstAncestorOfType<TemplateContainer>().EnableInClassList("fp-aleph-one-only", isAlephOneOnly);
        }

        private void ShowSelection()
        {
            var selectedSwatch = PaletteManager.Instance.SelectedSwatch;

            foreach (var swatchInstance in swatchInstances)
            {
                swatchInstance.Value.Q<Toggle>().SetValueWithoutNotify(swatchInstance.Key == selectedSwatch);
            }

            // An entry (not "None") of a list can be duplicated while it's selected
            var canDuplicateAmbient = selectedSwatch != null && selectedSwatch.Kind == PaletteManager.SwatchKinds.AmbientSound && !selectedSwatch.IsNone;
            var canDuplicateRandom = selectedSwatch != null && selectedSwatch.Kind == PaletteManager.SwatchKinds.RandomSound && !selectedSwatch.IsNone;
            Root.Find<Button>("duplicate-ambient").SetEnabled(canDuplicateAmbient);
            Root.Find<Button>("duplicate-random").SetEnabled(canDuplicateRandom);
        }

        // The new entry is selected (so it's inspected, or painted with)
        private static void Add(SoundImageKinds kind)
        {
            if (SoundImageEditing.Level == null)
            {
                return;
            }

            var index = SoundImageEditing.Add(kind);
            PaletteManager.Instance.ClickSound(kind, index);
        }

        private static void Duplicate(SoundImageKinds kind)
        {
            if (!PaletteManager.Instance.TryGetSelectedSound(out var selectedKind, out var selectedIndex) || selectedKind != kind || selectedIndex == cstypes.NONE)
            {
                return;
            }

            var index = SoundImageEditing.Duplicate(kind, selectedIndex);
            PaletteManager.Instance.ClickSound(kind, index);
        }
    }
}
