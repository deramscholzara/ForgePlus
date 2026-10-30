using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The view options, in the More Display Options pop-up (ViewOptionsPanel), each grayed out (and shown on, where the
    // mode always shows it) by the settings' rules
    public class DisplayOptionsPanel : UIPanel
    {
        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/DisplayOptions";
            }
        }

        protected override void OnLoaded()
        {
            var settings = ForgePlusUI.Instance.Settings;

            Root.Find<Toggle>("grid").BindValue(settings, nameof(SettingsViewModel.GridEnabled));
            Root.Find<Toggle>("simple-visuals").BindValue(settings, nameof(SettingsViewModel.SimpleVisualsEnabled));
            Root.Find<Toggle>("diagnostic-visuals").BindValue(settings, nameof(SettingsViewModel.DiagnosticVisualsEnabled));
            Root.Find<Toggle>("show-lighting").BindValue(settings, nameof(SettingsViewModel.ShowLightingEnabled));
            Root.Find<Toggle>("media-fog").BindValue(settings, nameof(SettingsViewModel.MediaFogEnabled));

            Root.Find<Toggle>("sprite-previews").BindValue(settings, nameof(SettingsViewModel.SpritePreviewsEnabled));
            Root.Find<Toggle>("clip-platform-sides").BindValue(settings, nameof(SettingsViewModel.ClipPlatformSidesEnabled));
            Root.Find<Toggle>("show-invalid-sides").BindValue(settings, nameof(SettingsViewModel.ShowInvalidSidesEnabled));

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
