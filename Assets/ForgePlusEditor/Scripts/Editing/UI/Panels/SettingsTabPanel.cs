using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    public class SettingsTabPanel : UIPanel
    {
        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/SettingsTab";
            }
        }

        protected override void OnLoaded()
        {
            var settings = ForgePlusUI.Instance.Settings;

            Root.Find<Toggle>("full-screen").BindValue(settings, nameof(SettingsViewModel.IsFullScreen));
            Root.Find<Slider>("minimum-light").BindValue(settings, nameof(SettingsViewModel.MinimumLight));
            Root.Find<Toggle>("bloom").BindValue(settings, nameof(SettingsViewModel.BloomEnabled));
            Root.Find<Toggle>("vignette").BindValue(settings, nameof(SettingsViewModel.VignetteEnabled));
            Root.Find<Toggle>("color-adjustment").BindValue(settings, nameof(SettingsViewModel.ColorCorrectionEnabled));
            Root.Find<Toggle>("ambient-occlusion").BindValue(settings, nameof(SettingsViewModel.AmbientOcclusionEnabled));
            Root.Find<Toggle>("improper-fractions").BindValue(settings, nameof(SettingsViewModel.ImproperFractionsEnabled));
            Root.Find<RadioButtonGroup>("merged-save-checksum").BindValue(settings, nameof(SettingsViewModel.MergedSaveChecksum));
            Root.Find<IntegerField>("undo-steps").BindValue(settings, nameof(SettingsViewModel.UndoSteps));
        }
    }
}
