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

            Root.Find<Button>("reset-to-defaults").clicked += () => ApplicationGeneral.SettingsManager.Instance.ResetToDefaults();

            // As Player Settings has it (the build's version)
            Root.Q<Label>("version").text = Localization.Strings.Get(Localization.Strings.Menu, "SettingsTab.Version.Text", UnityEngine.Application.version);

            Root.Find<Toggle>("full-screen").BindValue(settings, nameof(SettingsViewModel.IsFullScreen));
            Root.Find<Slider>("minimum-light").BindValue(settings, nameof(SettingsViewModel.MinimumLight));
            Root.Find<Toggle>("bloom").BindValue(settings, nameof(SettingsViewModel.BloomEnabled));
            Root.Find<Toggle>("vignette").BindValue(settings, nameof(SettingsViewModel.VignetteEnabled));
            Root.Find<Toggle>("color-adjustment").BindValue(settings, nameof(SettingsViewModel.ColorCorrectionEnabled));
            Root.Find<Toggle>("ambient-occlusion").BindValue(settings, nameof(SettingsViewModel.AmbientOcclusionEnabled));
            Root.Find<Toggle>("surface-cache-global-illumination").BindValue(settings, nameof(SettingsViewModel.SurfaceCacheGlobalIlluminationEnabled));
            Root.Find<Toggle>("improper-fractions").BindValue(settings, nameof(SettingsViewModel.ImproperFractionsEnabled));
            Root.Find<RadioButtonGroup>("merged-save-checksum").BindValue(settings, nameof(SettingsViewModel.MergedSaveChecksum));
            Root.Find<IntegerField>("undo-steps").BindValue(settings, nameof(SettingsViewModel.UndoSteps));


            var pointClickArea = Root.Q("point-click-area");
            pointClickArea.Q<SliderInt>("control").BindValue(settings, nameof(SettingsViewModel.PointClickAreaSteps));
            pointClickArea.Q<Label>("value").Bind("text", settings, nameof(SettingsViewModel.PointClickAreaText));

            var pointSnapDistance = Root.Q("point-snap-distance");
            pointSnapDistance.Q<SliderInt>("control").BindValue(settings, nameof(SettingsViewModel.PointSnapDistance));
            pointSnapDistance.Q<Label>("value").Bind("text", settings, nameof(SettingsViewModel.PointSnapDistanceText));

            var uiScale = Root.Q("ui-scale");
            uiScale.Q<SliderInt>("control").BindValue(settings, nameof(SettingsViewModel.UIScaleStep));
            uiScale.Q<Label>("value").Bind("text", settings, nameof(SettingsViewModel.UIScaleText));

            var orthographicGridOpacity = Root.Q("orthographic-grid-opacity");
            orthographicGridOpacity.Q<SliderInt>("control").BindValue(settings, nameof(SettingsViewModel.OrthographicGridOpacityPercent));
            orthographicGridOpacity.Q<Label>("value").Bind("text", settings, nameof(SettingsViewModel.OrthographicGridOpacityText));
        }
    }
}
