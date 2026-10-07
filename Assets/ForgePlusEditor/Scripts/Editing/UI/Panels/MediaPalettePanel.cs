using ForgePlus.Extensions;
using ForgePlus.Localization;
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
                return Strings.Get(Strings.Media, "Palette.Media.Header");
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
            return swatch.Kind == PaletteManager.SwatchKinds.Media;
        }

        protected override void FillSwatch(TemplateContainer instance, PaletteManager.Swatch swatch)
        {
            if (swatch.IsNone)
            {
                instance.Q<Label>("index").text = string.Empty;
                instance.Q<Label>("type").text = Strings.Get(Strings.Media, "Palette.Media.None");
                return;
            }

            instance.Q<Label>("index").text = swatch.Media.NativeIndex.ToString();
            instance.Q<Label>("type").text = AlephOneNames.MediaType(swatch.Media.NativeObject.type);
        }
    }
}
