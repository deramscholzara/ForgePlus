using ForgePlus.Extensions;
using ForgePlus.Palette;
using RuntimeCore.Materials;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    public class TexturePalettePanel : PalettePanel
    {
        private bool nextIsRight = false;

        protected override string SwatchTemplateName
        {
            get
            {
                return "SwatchTexture";
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            SwatchesContainer.AddToClassList("fp-palette__swatches--textures");
        }

        protected override void OnRebuilding()
        {
            nextIsRight = false;
        }

        protected override bool Shows(PaletteManager.Swatch swatch)
        {
            return swatch.Kind == PaletteManager.SwatchKinds.Texture;
        }

        protected override void FillSwatch(TemplateContainer instance, PaletteManager.Swatch swatch)
        {
            instance.AddToClassList("fp-swatch-slot");

            if (swatch.IsLandscape)
            {
                instance.AddToClassList("fp-swatch-slot--full");
                nextIsRight = false;
            }
            else
            {
                instance.AddToClassList(nextIsRight ? "fp-swatch-slot--right" : "fp-swatch-slot--left");
                nextIsRight = !nextIsRight;
            }

            // "None" shows what an unassigned surface looks like in the inspectors
            instance.Q<Label>("label").text = swatch.IsNone ? "None" : $"C: {swatch.ShapeDescriptor.GetCollection()} B: {swatch.ShapeDescriptor.GetShape()}";
            instance.Q<Image>("preview").image = swatch.IsNone ? Resources.Load<Texture2D>("Walls/UnassignedSurfaceUIPlaceholder") : swatch.Texture;
            instance.Q("in-use").style.display = !swatch.IsNone && MaterialGeneration_Geometry.GetTextureIsInUse(swatch.ShapeDescriptor) ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
