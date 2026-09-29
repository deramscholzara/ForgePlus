using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    public class ManipulationPanel : UIPanel
    {
        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/Manipulation";
            }
        }

        protected override void OnLoaded()
        {
            var editor = ForgePlusUI.Instance.Editor;

            var frameSelected = Root.Find<Button>("frame-selected");
            frameSelected.BindEnabled(editor, nameof(EditorViewModel.HasSelection));
            frameSelected.clicked += OnFrameSelected;

            Root.Find<Toggle>("lock-x").BindValue(editor, nameof(EditorViewModel.XLocked));
            Root.Find<Toggle>("lock-y").BindValue(editor, nameof(EditorViewModel.YLocked));
            Root.Find<Toggle>("snap").BindValue(editor, nameof(EditorViewModel.SnapToGrid));
        }

        private static void OnFrameSelected()
        {
            ForgePlusUI.Instance.EditorCamera.FrameSelected();
        }
    }
}
