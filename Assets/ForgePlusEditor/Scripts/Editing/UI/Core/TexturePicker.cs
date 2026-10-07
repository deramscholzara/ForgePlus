using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Palette;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The textures that can be assigned, as the texture palette lists them, four to a row (and a row for each
    // landscape), in a pop-up (PopupLayer) that choosing one closes
    public static class TexturePicker
    {
        private const float MinimumWidth = 400f;
        private const float MaximumHeight = 480f;

        public static void Show(VisualElement anchor, ushort currentTexture, Action<ushort> onChosen)
        {
            var box = PopupLayer.Open(anchor, MinimumWidth, MaximumHeight);
            if (box == null)
            {
                return;
            }

            var swatches = new VisualElement();
            swatches.AddToClassList("fp-palette__swatches");
            swatches.AddToClassList("fp-palette__swatches--textures");
            box.Add(swatches);

            var template = Resources.Load<VisualTreeAsset>("UI/Templates/SwatchTexture");
            Toggle currentToggle = null;

            void AddSwatch(ushort shapeDescriptor, Texture2D texture)
            {
                var isLandscape = !shapeDescriptor.IsEmptyShapeDescriptor() && shapeDescriptor.UsesLandscapeCollection();

                var instance = template.Instantiate();
                instance.AddToClassList("fp-swatch-slot");
                instance.AddToClassList(isLandscape ? "fp-swatch-slot--full" : "fp-swatch-slot--quarter");
                instance.pickingMode = PickingMode.Ignore;

                var toggle = instance.Q<Toggle>();
                toggle.SetValueWithoutNotify(shapeDescriptor == currentTexture);
                if (shapeDescriptor == currentTexture)
                {
                    currentToggle = toggle;
                }

                toggle.RegisterValueChangedCallback(changeEvent =>
                {
                    PopupLayer.Close();
                    onChosen(shapeDescriptor);
                });

                Swatches.FillTexture(instance, shapeDescriptor, texture);

                swatches.Add(instance);
            }

            AddSwatch(cstypes.UNONE, null);

            foreach (var textureEntry in PaletteManager.SortedLoadedTextures())
            {
                AddSwatch(textureEntry.Key, textureEntry.Value);
            }

            if (currentToggle != null)
            {
                box.ScrollToAfterLayout(currentToggle);
            }
        }
    }
}
