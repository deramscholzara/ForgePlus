using ForgePlus.Extensions;
using ForgePlus.Palette;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    public class MediaPalettePanel : PalettePanel
    {
        protected override string Header
        {
            get
            {
                return "Media";
            }
        }

        protected override string SwatchTemplateName
        {
            get
            {
                return "SwatchMedia";
            }
        }

        protected override bool Shows(PaletteManager.Swatch swatch)
        {
            return swatch.Media != null;
        }

        protected override void FillSwatch(TemplateContainer instance, PaletteManager.Swatch swatch)
        {
            instance.Q<Label>("index").text = swatch.Media.NativeIndex.ToString();
            instance.Q<Label>("type").text = AlephOneNames.MediaType(swatch.Media.NativeObject.type);
        }
    }
}
