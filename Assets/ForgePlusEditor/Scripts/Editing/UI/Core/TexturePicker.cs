using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Palette;
using RuntimeCore.Materials;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The textures that can be assigned (as the texture palette lists them: "None", then the loaded textures), four to a
    // row (and a row for each landscape, which is wide), opened over the whole UI below a field (PopupLayer). Choosing
    // one closes it.
    public static class TexturePicker
    {
        private const float MinimumWidth = 400f;
        private const float MaximumHeight = 480f;

        public static void Show(VisualElement anchor, ushort currentTexture, Action<ushort> onChosen)
        {
            var box = PopupLayer.Open(anchor, "fp-texture-picker", MinimumWidth, MaximumHeight);
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
                var isNone = shapeDescriptor.IsEmptyShapeDescriptor();
                var isLandscape = !isNone && shapeDescriptor.UsesLandscapeCollection();

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

                // "None" shows what an unassigned surface looks like in the inspectors
                instance.Q<Label>("label").text = isNone ? "None" : $"C: {shapeDescriptor.GetCollection()} B: {shapeDescriptor.GetShape()}";
                instance.Q<Image>("preview").image = isNone ? Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder") : texture;
                instance.Q("in-use").style.display = !isNone && MaterialGeneration_Geometry.GetTextureIsInUse(shapeDescriptor) ? DisplayStyle.Flex : DisplayStyle.None;

                swatches.Add(instance);
            }

            AddSwatch(cstypes.UNONE, null);

            foreach (var textureEntry in PaletteManager.SortedLoadedTextures())
            {
                AddSwatch(textureEntry.Key, textureEntry.Value);
            }

            // Once it's laid out, so the current texture has a position to scroll to
            if (currentToggle != null)
            {
                void ScrollToCurrent(GeometryChangedEvent geometryChangedEvent)
                {
                    currentToggle.UnregisterCallback<GeometryChangedEvent>(ScrollToCurrent);
                    box.schedule.Execute(() => box.ScrollTo(currentToggle));
                }

                currentToggle.RegisterCallback<GeometryChangedEvent>(ScrollToCurrent);
            }
        }

        public static void Close()
        {
            PopupLayer.Close();
        }
    }
}
