using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The visualization options (in every mode), which show and hide kinds of things (Media mode always shows media, and
    // Objects and Sounds modes always show objects; points and lines are only shown in Geometry mode), with the More
    // Display Options dropdown (ViewOptionsPanel) below them
    public class VisualizationGeometryPanel : UIPanel
    {
        private ViewOptionsPanel viewOptions;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/VisualizationGeometry";
            }
        }

        protected override void OnLoaded()
        {
            var settings = ForgePlusUI.Instance.Settings;

            Root.Find<Toggle>("show-everything").BindValue(settings, nameof(SettingsViewModel.ShowEverythingEnabled));

            var points = Root.Find<Toggle>("points");
            points.BindValue(settings, nameof(SettingsViewModel.PointsEnabled));
            points.BindEnabled(settings, nameof(SettingsViewModel.IsGeometryMode));

            var lines = Root.Find<Toggle>("lines");
            lines.BindValue(settings, nameof(SettingsViewModel.LinesEnabled));
            lines.BindEnabled(settings, nameof(SettingsViewModel.IsGeometryMode));

            var media = Root.Find<Toggle>("media");
            media.BindValue(settings, nameof(SettingsViewModel.MediaEnabled));
            media.BindEnabled(settings, nameof(SettingsViewModel.MediaEditable));

            var objects = Root.Find<Toggle>("objects");
            objects.BindValue(settings, nameof(SettingsViewModel.ObjectIconsEnabled));
            objects.BindEnabled(settings, nameof(SettingsViewModel.ObjectIconsEditable));

            Root.Find<Toggle>("polygons").BindValue(settings, nameof(SettingsViewModel.PolygonsEnabled));
            Root.Find<Toggle>("sides").BindValue(settings, nameof(SettingsViewModel.SidesEnabled));

            viewOptions = new ViewOptionsPanel();
            viewOptions.Load(Root.Q("view-options-slot"));
        }

        protected override void OnUnloading()
        {
            viewOptions?.Unload();
            viewOptions = null;
        }
    }
}
