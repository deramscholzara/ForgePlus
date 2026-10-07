using ForgePlus.Palette;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Shows PaletteManager's swatches as toggles, with the selected swatch on (however it was selected)
    public abstract class PalettePanel : UIPanel
    {
        private readonly List<KeyValuePair<PaletteManager.Swatch, Toggle>> swatchToggles = new List<KeyValuePair<PaletteManager.Swatch, Toggle>>();

        private VisualElement swatchesContainer;
        private VisualTreeAsset swatchTemplate;
        private ScrollView scrollView;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/Palette";
            }
        }

        protected abstract string SwatchTemplateName { get; }

        // Saying what the swatches are (none, if they speak for themselves)
        protected virtual string Header
        {
            get
            {
                return null;
            }
        }

        // For long palettes whose selection follows the level's
        protected virtual bool ScrollsToSelection
        {
            get
            {
                return false;
            }
        }

        // PaletteManager may not have changed its swatches for a new mode yet, so each palette shows only its own kind
        protected abstract bool Shows(PaletteManager.Swatch swatch);

        protected VisualElement SwatchesContainer
        {
            get
            {
                return swatchesContainer;
            }
        }

        // The palette's one list, unless its layout has others
        protected virtual IEnumerable<VisualElement> SwatchContainers
        {
            get
            {
                yield return swatchesContainer;
            }
        }

        protected override void OnLoaded()
        {
            scrollView = Root.Find<ScrollView>("swatches");
            swatchesContainer = Root.Q("swatches-container");

            var header = Root.Q("header");
            if (header != null)
            {
                header.style.display = Header == null ? DisplayStyle.None : DisplayStyle.Flex;
                header.Q<Label>("label").text = Header;
            }

            swatchTemplate = LoadTemplate(SwatchTemplateName);

            PaletteManager.Instance.OnSwatchesChanged += Rebuild;
            PaletteManager.Instance.OnSelectionChanged += ShowSelection;

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

            swatchToggles.Clear();
        }

        protected virtual void OnRebuilding()
        {
        }

        protected virtual void OnRebuilt()
        {
        }

        protected virtual void OnSelectionShown()
        {
        }

        protected virtual VisualElement ContainerFor(PaletteManager.Swatch swatch)
        {
            return swatchesContainer;
        }

        protected abstract void FillSwatch(TemplateContainer instance, PaletteManager.Swatch swatch);

        private void Rebuild()
        {
            foreach (var container in SwatchContainers)
            {
                container.Clear();
            }

            swatchToggles.Clear();

            OnRebuilding();

            foreach (var swatch in PaletteManager.Instance.Swatches)
            {
                if (!Shows(swatch))
                {
                    continue;
                }

                var instance = swatchTemplate.Instantiate();
                var toggle = instance.Q<Toggle>();

                toggle.RegisterValueChangedCallback(changeEvent =>
                {
                    PaletteManager.Instance.Click(swatch);

                    // The palette decides what's selected (the click may not have changed it)
                    ShowSelection();
                });

                swatchToggles.Add(new KeyValuePair<PaletteManager.Swatch, Toggle>(swatch, toggle));

                FillSwatch(instance, swatch);
                ContainerFor(swatch).Add(instance);
            }

            foreach (var container in SwatchContainers)
            {
                container.IgnoreLayoutPicking();
            }

            OnRebuilt();
            ShowSelection();
        }

        private void ShowSelection()
        {
            foreach (var swatchToggle in swatchToggles)
            {
                var isSelected = PaletteManager.Instance.IsSelected(swatchToggle.Key);
                swatchToggle.Value.SetValueWithoutNotify(isSelected);

                if (isSelected && ScrollsToSelection)
                {
                    scrollView.ScrollToAfterLayout(swatchToggle.Value);
                }
            }

            OnSelectionShown();
        }
    }
}
