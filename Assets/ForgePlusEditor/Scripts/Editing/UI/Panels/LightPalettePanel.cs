using ForgePlus.Palette;
using RuntimeCore.Entities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Lights, each with a preview of its current intensity
    public class LightPalettePanel : PalettePanel
    {
        private readonly List<KeyValuePair<LevelEntity_Light, VisualElement>> previews = new List<KeyValuePair<LevelEntity_Light, VisualElement>>();

        protected override string Header
        {
            get
            {
                return "Lights";
            }
        }

        protected override string SwatchTemplateName
        {
            get
            {
                return "SwatchLight";
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            // Lights animate, so their previews follow them every frame
            Root.schedule.Execute(UpdatePreviews).Every(0);
        }

        protected override void OnRebuilding()
        {
            previews.Clear();
        }

        protected override bool Shows(PaletteManager.Swatch swatch)
        {
            return swatch.Kind == PaletteManager.SwatchKinds.Light;
        }

        protected override void FillSwatch(TemplateContainer instance, PaletteManager.Swatch swatch)
        {
            instance.Q<Label>("index").text = swatch.Light.NativeIndex.ToString();
            previews.Add(new KeyValuePair<LevelEntity_Light, VisualElement>(swatch.Light, instance.Q("preview")));
        }

        private void UpdatePreviews()
        {
            foreach (var preview in previews)
            {
                var intensity = preview.Key.CurrentDisplayIntensity;
                preview.Value.style.backgroundColor = new Color(intensity, intensity, intensity, 1f);
            }
        }
    }
}
