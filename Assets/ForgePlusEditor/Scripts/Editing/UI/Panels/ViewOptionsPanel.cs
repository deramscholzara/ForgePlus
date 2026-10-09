using ForgePlus.Localization;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The Display Options dropdown, which shows the view options (DisplayOptionsPanel) in a pop-up while open
    public class ViewOptionsPanel : UIPanel
    {
        private const float MinimumWidth = 200f;
        private const float MaximumHeight = 400f;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/ViewOptions";
            }
        }

        protected override void OnLoaded()
        {
            var displayOptions = Root.Find<DropdownField>("display-options");
            displayOptions.SetValueWithoutNotify(Strings.Get(Strings.Common, "ViewOptions.DisplayOptions.Text"));
            displayOptions.OpensOnPress(() => ShowDisplayOptions(displayOptions), replacesOwnHandling: true);
        }

        // Unloaded (with its bindings) as the pop-up closes
        private static void ShowDisplayOptions(VisualElement anchor)
        {
            var options = new DisplayOptionsPanel();
            var box = PopupLayer.Open(anchor, MinimumWidth, MaximumHeight, options.Unload);
            if (box != null)
            {
                options.Load(box);
            }
        }
    }
}
