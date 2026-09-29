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

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/Palette";
            }
        }

        protected abstract string SwatchTemplateName { get; }

        // PaletteManager may not have changed its swatches for a new mode yet, so each palette shows only its own kind
        protected abstract bool Shows(PaletteManager.Swatch swatch);

        protected VisualElement SwatchesContainer
        {
            get
            {
                return swatchesContainer;
            }
        }

        protected override void OnLoaded()
        {
            swatchesContainer = Root.Q("swatches-container");
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

        protected abstract void FillSwatch(TemplateContainer instance, PaletteManager.Swatch swatch);

        private void Rebuild()
        {
            swatchesContainer.Clear();
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
                swatchesContainer.Add(instance);
            }

            swatchesContainer.IgnoreLayoutPicking();
            ShowSelection();
        }

        private void ShowSelection()
        {
            var selectedSwatch = PaletteManager.Instance.SelectedSwatch;

            foreach (var swatchToggle in swatchToggles)
            {
                swatchToggle.Value.SetValueWithoutNotify(swatchToggle.Key == selectedSwatch);
            }
        }
    }
}
