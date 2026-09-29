using ForgePlus.Extensions;
using ForgePlus.Palette;
using RuntimeCore.Materials;
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
            return swatch.Texture;
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

            instance.Q<Label>("label").text = $"C: {swatch.ShapeDescriptor.GetCollection()} B: {swatch.ShapeDescriptor.GetShape()}";
            instance.Q<Image>("preview").image = swatch.Texture;
            instance.Q("in-use").style.display = MaterialGeneration_Geometry.GetTextureIsInUse(swatch.ShapeDescriptor) ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
