using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Geometry mode's visualization options, which show and hide kinds of geometry (in every mode, though Media mode always
    // shows media)
    public class VisualizationGeometryPanel : UIPanel
    {
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
            Root.Find<Toggle>("points").BindValue(settings, nameof(SettingsViewModel.PointsEnabled));

            var media = Root.Find<Toggle>("media");
            media.BindValue(settings, nameof(SettingsViewModel.MediaEnabled));
            media.BindEnabled(settings, nameof(SettingsViewModel.MediaEditable));

            // Not implemented yet (shown on, as everything of theirs is shown)
            Root.Find<Toggle>("polygons").SetEnabled(false);
            Root.Find<Toggle>("lines").SetEnabled(false);
            Root.Find<Toggle>("sides").SetEnabled(false);
        }
    }
}
