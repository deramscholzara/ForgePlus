using ForgePlus.History;
using System;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // Undo and Redo (in the screen's bottom left corner, opposite the errors toggle)
    public class HistoryPanel : UIPanel
    {
        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/History";
            }
        }

        protected override void OnLoaded()
        {
            var editor = ForgePlusUI.Instance.Editor;

            BindAction(Root.Find<Button>("undo"), editor, nameof(EditorViewModel.CanUndo), nameof(EditorViewModel.UndoTooltip), UndoHistory.Undo);
            BindAction(Root.Find<Button>("redo"), editor, nameof(EditorViewModel.CanRedo), nameof(EditorViewModel.RedoTooltip), UndoHistory.Redo);
        }

        private static void BindAction(Button button, EditorViewModel editor, string canUseProperty, string tooltipProperty, Action onClicked)
        {
            button.BindEnabled(editor, canUseProperty);
            button.Bind("tooltip", editor, tooltipProperty);
            button.clicked += onClicked;
        }
    }
}
