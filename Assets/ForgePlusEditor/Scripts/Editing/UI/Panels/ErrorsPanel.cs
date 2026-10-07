using ForgePlus.Localization;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ForgePlus.UI
{
    // The level's errors (in the menu's place), as its tab filters them, each with its fix where it has one, and a fix
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

            var itemTemplate = LoadTemplate("ErrorItem");
            var fixText = Strings.Get(Strings.Menu, "Errors.Fix.Control.Text");

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

                return item;
            };
            errorList.bindItem = (item, index) =>
            {
                var error = shownErrors[index];
                item.Q<Label>("description").text = error.Description;

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
