using ForgePlus.Inspection;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    public class InspectorColumnPanel : UIPanel
    {
        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/InspectorColumn";
            }
        }

        protected override void OnLoaded()
        {
            InspectorPanel.Instance.SetContainer(Root.Find<ScrollView>("inspectors").contentContainer);
        }

        protected override void OnUnloading()
        {
            InspectorPanel.Instance.SetContainer(null);
        }
    }
}
