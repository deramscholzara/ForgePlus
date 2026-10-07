using ForgePlus.Extensions;
using ForgePlus.Localization;
using ForgePlus.Palette;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Every platform, by index and type; clicking one selects it and frames the camera on it
    public class PlatformPalettePanel : PalettePanel
    {
        protected override string Header
        {
            get
            {
                return Strings.Get(Strings.Platforms, "Palette.Platforms.Header");
            }
        }

        protected override string SwatchTemplateName
        {
            get
            {
                return "SwatchPlatform";
            }
        }

        protected override bool ScrollsToSelection
        {
            get
            {
                return true;
            }
        }

        protected override bool Shows(PaletteManager.Swatch swatch)
        {
            return swatch.Kind == PaletteManager.SwatchKinds.Platform;
        }

        protected override void FillSwatch(TemplateContainer instance, PaletteManager.Swatch swatch)
        {
            instance.Q<Label>("index").text = swatch.Platform.NativeIndex.ToString();
            instance.Q<Label>("type").text = AlephOneNames.PlatformType(swatch.Platform.NativeObject.type);
        }
    }
}
