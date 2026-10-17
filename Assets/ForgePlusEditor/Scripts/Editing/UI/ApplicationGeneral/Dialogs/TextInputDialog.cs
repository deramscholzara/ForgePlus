using ForgePlus.Localization;
using ForgePlus.UI;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.ApplicationGeneral
{
    // A titled text field, which completes with its text (trimmed) for OK, or null for Cancel. OK is grayed out while the
    // text isn't valid. Enter is OK (while it's valid) and Escape is Cancel.
    public class TextInputDialog : UIPanel
    {
        // Continues after the click that completes it, rather than inside it
        private readonly TaskCompletionSource<string> result = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly string title;
        private readonly string message;
        private readonly string initialText;
        private readonly int maxLength;
        private readonly Func<string, bool> isValid;

        private TextField textField;
        private Button okButton;

        public TextInputDialog(string title, string message, string initialText, int maxLength, Func<string, bool> isValid)
        {
            this.title = title;
            this.message = message;
            this.initialText = initialText ?? string.Empty;
            this.maxLength = maxLength;
            this.isValid = isValid;
        }

        public Task<string> Result
        {
            get
            {
                return result.Task;
            }
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Dialogs/TextInputDialog";
            }
        }

        protected override void OnLoaded()
        {
            Strings.SetText(Root.Q<Label>("title"), title);

            var messageLabel = Root.Q<Label>("message");
            messageLabel.text = message ?? string.Empty;
            messageLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;

            textField = Root.Q<TextField>("text");
            textField.maxLength = maxLength;
            textField.SetValueWithoutNotify(initialText);
            textField.RegisterValueChangedCallback(changeEvent => UpdateOkButton());

            Root.Find<Button>("cancel").clicked += Cancel;

            okButton = Root.Find<Button>("ok");
            okButton.clicked += Submit;

            Root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            UpdateOkButton();

            // Ready to type in, once it's laid out
            textField.schedule.Execute(() =>
            {
                textField.Focus();
                textField.SelectAll();
            });
        }

        protected override void OnUnloading()
        {
            // Unloaded before it was completed (such as when the UI is torn down)
            result.TrySetResult(null);
        }

        private string Text
        {
            get
            {
                return (textField.value ?? string.Empty).Trim();
            }
        }

        private bool IsValid
        {
            get
            {
                return isValid == null || isValid(Text);
            }
        }

        private void UpdateOkButton()
        {
            okButton.SetEnabled(IsValid);
        }

        private void Submit()
        {
            if (IsValid)
            {
                result.TrySetResult(Text);
            }
        }

        private void Cancel()
        {
            result.TrySetResult(null);
        }

        private void OnKeyDown(KeyDownEvent keyDownEvent)
        {
            switch (keyDownEvent.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Submit();
                    keyDownEvent.StopPropagation();
                    break;
                case KeyCode.Escape:
                    Cancel();
                    keyDownEvent.StopPropagation();
                    break;
            }
        }
    }
}
