using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    public class ViewOptionsPanel : UIPanel
    {
        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/ViewOptions";
            }
        }

        protected override void OnLoaded()
        {
            var settings = ForgePlusUI.Instance.Settings;

            var objectIcons = Root.Find<Toggle>("object-icons");
            objectIcons.BindValue(settings, nameof(SettingsViewModel.ObjectIconsEnabled));
            objectIcons.BindEnabled(settings, nameof(SettingsViewModel.ObjectIconsEditable));

            Root.Find<Toggle>("sprite-previews").BindValue(settings, nameof(SettingsViewModel.SpritePreviewsEnabled));
            Root.Find<Toggle>("clip-platform-sides").BindValue(settings, nameof(SettingsViewModel.ClipPlatformSidesEnabled));
        }
    }
}
