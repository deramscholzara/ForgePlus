using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    public class MenuPanel : UIPanel
    {
        private PanelSlot tabSlot;
        private VisualElement tabActions;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/Menu";
            }
        }

        protected override void OnLoaded()
        {
            var editor = ForgePlusUI.Instance.Editor;

            Root.Find<RadioButtonGroup>("tabs").BindValue(editor, nameof(EditorViewModel.MenuTabIndex));

            tabSlot = new PanelSlot(Root.Q("tab-slot"));
            tabActions = Root.Q("tab-actions");

            Root.Find<Button>("close").clicked += () => editor.MenuOpen = false;

            editor.OnMenuTabChanged += ShowSelectedTab;
            ShowSelectedTab();
        }

        protected override void OnUnloading()
        {
            ForgePlusUI.Instance.Editor.OnMenuTabChanged -= ShowSelectedTab;

            tabSlot.Hide();
            tabSlot = null;
            tabActions.Clear();
        }

        private void ShowSelectedTab()
        {
            tabActions.Clear();

            switch (ForgePlusUI.Instance.Editor.MenuTabIndex)
            {
                case 0:
                    tabSlot.Show<FilesTabPanel>();
                    break;
                case 1:
                    tabSlot.Show(new LayoutPanel("UI/Panels/InputsTab"));
                    break;
                case 2:
                    tabSlot.Show<SettingsTabPanel>();
                    break;
            }

            if (tabSlot.Panel is IMenuTab tab && tab.Actions != null)
            {
                tabActions.Add(tab.Actions);
            }
        }
    }
}
