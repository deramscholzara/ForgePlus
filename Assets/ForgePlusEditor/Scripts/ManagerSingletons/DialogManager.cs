using ForgePlus.UI;
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

            var dialog = new ObjectSelectorDialog(title, options, optionLabels);
            DialogSlot.Show(dialog);

            var result = await dialog.Selection;

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
