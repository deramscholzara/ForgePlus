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

            // Not implemented yet
            Root.Find<Toggle>("visualization-effect").SetEnabled(false);

            var objectIcons = Root.Find<Toggle>("object-icons");
            objectIcons.BindValue(settings, nameof(SettingsViewModel.ObjectIconsEnabled));
            objectIcons.BindEnabled(settings, nameof(SettingsViewModel.ObjectIconsEditable));

            Root.Find<Toggle>("sprite-previews").BindValue(settings, nameof(SettingsViewModel.SpritePreviewsEnabled));
            Root.Find<Toggle>("clip-platform-sides").BindValue(settings, nameof(SettingsViewModel.ClipPlatformSidesEnabled));

            var soundDirection = Root.Find<Toggle>("sound-direction");
            soundDirection.BindValue(settings, nameof(SettingsViewModel.SoundDirectionEnabled));
            soundDirection.BindEnabled(settings, nameof(SettingsViewModel.SoundDisplaysEditable));

            var soundVolume = Root.Find<Toggle>("sound-volume");
            soundVolume.BindValue(settings, nameof(SettingsViewModel.SoundVolumeEnabled));
            soundVolume.BindEnabled(settings, nameof(SettingsViewModel.SoundDisplaysEditable));

            var playLevelAudio = Root.Find<Toggle>("play-level-audio");
            playLevelAudio.BindValue(settings, nameof(SettingsViewModel.PlayLevelAudioEnabled));
            playLevelAudio.BindEnabled(settings, nameof(SettingsViewModel.PlayLevelAudioEditable));
        }
    }
}
