using ForgePlus.Extensions;
using ForgePlus.Localization;
using RuntimeCore.Entities;
using RuntimeCore.Materials;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // What the palettes' and pickers' swatches show
    public static class Swatches
    {
        // "None" shows what an unassigned surface looks like in the inspectors
        public static void FillTexture(VisualElement instance, ushort shapeDescriptor, Texture2D texture)
        {
            var isNone = shapeDescriptor.IsEmptyShapeDescriptor();

            instance.Q<Label>("label").text = isNone ? Strings.Get(Strings.Textures, "Palette.Texture.None") : Strings.Get(Strings.Textures, "Palette.Texture.Label", shapeDescriptor.GetCollection(), shapeDescriptor.GetShape());
            instance.Q<Image>("preview").image = isNone ? Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder") : texture;
            instance.Q("in-use").style.display = !isNone && MaterialGeneration_Geometry.GetTextureIsInUse(shapeDescriptor) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Returns its preview, which ShowIntensities keeps up with the light
        public static VisualElement FillLight(VisualElement instance, LevelEntity_Light light)
        {
            instance.Q<Label>("index").text = light.NativeIndex.ToString();

            return instance.Q("preview");
        }

        // Lights animate, so their previews follow them every frame
        public static void ShowIntensities(List<KeyValuePair<LevelEntity_Light, VisualElement>> previews)
        {
            foreach (var preview in previews)
            {
                ShowIntensity(preview.Value, preview.Key);
            }
        }

        public static void ShowIntensity(VisualElement preview, LevelEntity_Light light)
        {
            var intensity = light.CurrentDisplayIntensity;
            preview.style.backgroundColor = new Color(intensity, intensity, intensity, 1f);
        }
    }
}
