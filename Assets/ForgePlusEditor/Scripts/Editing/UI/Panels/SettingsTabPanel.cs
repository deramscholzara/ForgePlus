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

            var surfaceLightMultiplier = Root.Q("surface-light-multiplier");
            surfaceLightMultiplier.BindEnabled(settings, nameof(SettingsViewModel.SurfaceCacheGlobalIlluminationEnabled));
            surfaceLightMultiplier.Q<SliderInt>("control").BindValue(settings, nameof(SettingsViewModel.SurfaceLightMultiplierTenths));
            surfaceLightMultiplier.Q<Label>("value").Bind("text", settings, nameof(SettingsViewModel.SurfaceLightMultiplierText));

            var orthographicGridOpacity = Root.Q("orthographic-grid-opacity");
            orthographicGridOpacity.Q<SliderInt>("control").BindValue(settings, nameof(SettingsViewModel.OrthographicGridOpacityPercent));
            orthographicGridOpacity.Q<Label>("value").Bind("text", settings, nameof(SettingsViewModel.OrthographicGridOpacityText));

            AddMediaFogTypes(settings);
        }

        // A color and a density for each media type's fog
        private void AddMediaFogTypes(SettingsViewModel settings)
        {
            var container = Root.Q("media-fog-types");
            var template = LoadTemplate("MediaFogType");

            foreach (var mediaFogType in settings.MediaFogTypes)
            {
                var element = template.Instantiate();
                element.pickingMode = PickingMode.Ignore;
                container.Add(element);
                Localization.Strings.Localize(element);

                var color = element.Q("color");
                var colorField = color.Q<TextField>("control");
                colorField.Bind("label", mediaFogType, nameof(MediaFogTypeViewModel.ColorLabel));
                colorField.BindValue(mediaFogType, nameof(MediaFogTypeViewModel.ColorText));
                color.Q("swatch").Bind("style.backgroundColor", mediaFogType, nameof(MediaFogTypeViewModel.SwatchColor));

                var density = element.Q("density");
                var densitySlider = density.Q<SliderInt>("control");
                densitySlider.Bind("label", mediaFogType, nameof(MediaFogTypeViewModel.DensityLabel));
                densitySlider.BindValue(mediaFogType, nameof(MediaFogTypeViewModel.DensityThousandths));
                density.Q<Label>("value").Bind("text", mediaFogType, nameof(MediaFogTypeViewModel.DensityText));
            }
        }
    }
}
