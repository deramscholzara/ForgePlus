using ForgePlus.Localization;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The level's errors (in the menu's place), as its tab filters them (with warnings after them, unless they're hidden),
    // each marked as an error or a warning, with its fix where it has one and a button that shows what it's in, and a fix
    // for all the auto-fixable ones
    public class ErrorsPanel : UIPanel
    {
        private readonly List<LevelError> shownErrors = new List<LevelError>();

        private ErrorsViewModel errors;
        private ListView errorList;
        private Label emptyNote;

        protected override string LayoutPath
        {
            get
            {
                return "UI/Panels/Errors";
            }
        }

        protected override void OnLoaded()
        {
            errors = ForgePlusUI.Instance.Errors;

            Root.Find<RadioButtonGroup>("tabs").BindValue(errors, nameof(ErrorsViewModel.FilterIndex));
            Root.Find<Toggle>("show-warnings").BindValue(errors, nameof(ErrorsViewModel.ShowWarnings));

            var itemTemplate = LoadTemplate("ErrorItem");
            var fixText = Strings.Get(Strings.Menu, "Errors.Fix.Control.Text");
            var showText = Strings.Get(Strings.Menu, "Errors.Show.Control.Text");
            var showTooltip = Strings.Get(Strings.Menu, "Errors.Show.Tooltip");

            errorList = Root.Q<ListView>("errors");
            errorList.itemsSource = shownErrors;
            errorList.makeItem = () =>
            {
                var item = itemTemplate.Instantiate();
                var fix = item.Q<Button>("fix");
                fix.text = fixText;
                fix.clicked += () =>
                {
                    if (fix.userData is LevelError error)
                    {
                        errors.Fix(error);
                    }
                };

                var show = item.Q<Button>("show");
                show.text = showText;
                show.tooltip = showTooltip;
                show.clicked += () =>
                {
                    if (show.userData is LevelError error)
                    {
                        error.Show?.Invoke(ShownViewArea());
                    }
                };

                return item;
            };
            errorList.bindItem = (item, index) =>
            {
                var error = shownErrors[index];
                item.Q<Label>("description").text = error.Description;

                var errorItem = item.Q(className: "fp-error-item");
                errorItem.EnableInClassList("fp-error-item--error", !error.IsWarning);
                errorItem.EnableInClassList("fp-error-item--warning", error.IsWarning);

                var show = item.Q<Button>("show");
                show.userData = error;
                show.style.display = error.Show != null ? DisplayStyle.Flex : DisplayStyle.None;

                var fix = item.Q<Button>("fix");
                fix.userData = error;
                fix.style.display = error.IsAutoFixable ? DisplayStyle.Flex : DisplayStyle.None;
            };

            emptyNote = Root.Q<Label>("empty");

            Root.Find<Button>("close").clicked += () => ForgePlusUI.Instance.Editor.ErrorsOpen = false;

            var fixAll = Root.Find<Button>("fix-all");
            fixAll.clicked += errors.FixAll;
            fixAll.BindEnabled(errors, nameof(ErrorsViewModel.CanFixAll));
            Root.Q("fix-all").BindShown(errors, nameof(ErrorsViewModel.IsFixAllShown));

            errors.OnErrorsChanged += ShowErrors;
            ShowErrors();
        }

        protected override void OnUnloading()
        {
            errors.OnErrorsChanged -= ShowErrors;
        }

        // What's shown is framed beside the panel (to its right), or in the whole view if there isn't room there
        private UnityEngine.Rect ShownViewArea()
        {
            var panelBounds = Root.panel.visualTree.worldBound;
            var bounds = Root.worldBound;
            var left = bounds.xMax / panelBounds.width;

            return left < 0.8f ? new UnityEngine.Rect(left, 0f, 1f - left, 1f) : new UnityEngine.Rect(0f, 0f, 1f, 1f);
        }

        private void ShowErrors()
        {
            shownErrors.Clear();
            shownErrors.AddRange(errors.FilteredErrors);
            errorList.RefreshItems();

            var hasErrors = shownErrors.Count > 0;
            errorList.style.display = hasErrors ? DisplayStyle.Flex : DisplayStyle.None;
            emptyNote.style.display = hasErrors ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
