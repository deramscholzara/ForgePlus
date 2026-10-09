using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The toggle (in the screen's bottom right corner) that opens and closes the Errors panel. While the panel is closed,
    // it flashes red if the level has errors (not for warnings alone), and can't be pressed if it has neither errors nor
    // warnings (there being nothing to show).
    public class ErrorsTogglePanel : UIPanel
    {
        // Milliseconds between the flash's changes
        private const long FlashInterval = 500;

        private EditorViewModel editor;
        private ErrorsViewModel errors;
        private Toggle toggle;
        private IVisualElementScheduledItem flash;
        private bool isFlashing;
        private bool isFlashedRed;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/ErrorsToggle";
            }
        }

        protected override void OnLoaded()
        {
            editor = ForgePlusUI.Instance.Editor;
            errors = ForgePlusUI.Instance.Errors;

            toggle = Root.Find<Toggle>("errors");
            toggle.BindValue(editor, nameof(EditorViewModel.ErrorsOpen));

            editor.OnErrorsOpenChanged += UpdateState;
            errors.OnErrorsChanged += UpdateState;

            flash = toggle.schedule.Execute(Flash).Every(FlashInterval);
            UpdateState();
        }

        protected override void OnUnloading()
        {
            editor.OnErrorsOpenChanged -= UpdateState;
            errors.OnErrorsChanged -= UpdateState;

            flash.Pause();
        }

        private void UpdateState()
        {
            var isOpen = editor.ErrorsOpen;

            toggle.SetEnabled(isOpen || errors.HasProblems);

            isFlashing = !isOpen && errors.HasErrors;
            if (!isFlashing)
            {
                SetFlashedRed(false);
            }
        }

        private void Flash()
        {
            if (isFlashing)
            {
                SetFlashedRed(!isFlashedRed);
            }
        }

        private void SetFlashedRed(bool isRed)
        {
            isFlashedRed = isRed;
            toggle.EnableInClassList("fp-errors-toggle--alert", isRed);
        }
    }
}
