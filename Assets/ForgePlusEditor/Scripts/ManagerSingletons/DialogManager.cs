using ForgePlus.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgePlus.ApplicationGeneral
{
    // Shows dialogs one at a time, in the order they're asked for, over the rest of the UI (which is blocked meanwhile)
    [RequireComponent(typeof(UIDocument))]
    public class DialogManager : SingletonMonoBehaviour<DialogManager>
    {
        private readonly List<object> dialogQueue = new List<object>();

        private PanelSlot dialogSlot;

        private PanelSlot DialogSlot
        {
            get
            {
                if (dialogSlot == null)
                {
                    dialogSlot = new PanelSlot(GetComponent<UIDocument>().rootVisualElement.Q("dialog-layer"));
                }

                return dialogSlot;
            }
        }

        // The chosen option, or null if the dialog was cancelled
        public async Task<string> DisplayQueuedDialog(string title, IList<string> options, IList<string> optionLabels = null)
        {
            return (await DisplayQueuedDialog(title, message: null, options, optionLabels, checkboxLabel: null)).Option;
        }

        // With a message, and a checkbox (if it has a label) whose state is returned with the chosen option
        public async Task<(string Option, bool IsChecked)> DisplayQueuedDialog(string title, string message, IList<string> options,
            IList<string> optionLabels, string checkboxLabel)
        {
            return await DisplayQueued(
                () => new ObjectSelectorDialog(title, options, optionLabels, message, checkboxLabel),
                async dialog => (await dialog.Selection, dialog.IsChecked));
        }

        // The entered text (trimmed), or null if the dialog was cancelled. OK is only enabled while isValid accepts the text.
        public async Task<string> DisplayQueuedTextDialog(string title, string message, string initialText, int maxLength,
            Func<string, bool> isValid)
        {
            return await DisplayQueued(
                () => new TextInputDialog(title, message, initialText, maxLength, isValid),
                dialog => dialog.Result);
        }

        // Waits for the dialogs before it, then shows it until it completes
        private async Task<TResult> DisplayQueued<TDialog, TResult>(Func<TDialog> createDialog, Func<TDialog, Task<TResult>> getResult)
            where TDialog : UIPanel
        {
            if (dialogQueue.Count == 0)
            {
                UIBlocking.Instance.Block();
            }

            var queuedDialog = new object();
            dialogQueue.Add(queuedDialog);

            while (dialogQueue[0] != queuedDialog)
            {
                await Awaitable.NextFrameAsync();
            }

            var dialog = createDialog();
            DialogSlot.Show(dialog);

            var result = await getResult(dialog);

            DialogSlot.Hide();
            dialogQueue.Remove(queuedDialog);

            if (dialogQueue.Count == 0)
            {
                UIBlocking.Instance.Unblock();
            }

            return result;
        }

        private void OnDestroy()
        {
            dialogSlot?.Hide();
        }
    }
}
