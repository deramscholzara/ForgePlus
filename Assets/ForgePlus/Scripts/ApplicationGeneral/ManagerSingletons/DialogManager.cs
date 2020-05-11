using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    public class DialogManager : SingletonMonoBehaviour<DialogManager>
    {
        private class QueuedDialog
        {
            public DialogBase DialogPrefab { get; private set; }

            public IList<object> VariableOptions { get; private set; }

            public QueuedDialog(DialogBase dialogPrefab, IList<object> variableOptions)
            {
                DialogPrefab = dialogPrefab;
                VariableOptions = variableOptions;
            }

            public async Task<object> Display(Transform parent)
            {
                var result = await DialogPrefab.Display(parent, VariableOptions);

                return result;
            }
        }

        private readonly List<QueuedDialog> dialogQueue = new List<QueuedDialog>();

        public async Task<object> DisplayQueuedDialog(DialogBase dialogPrefab, IList<object> variableOptions)
        {
            if (dialogPrefab == null)
            {
                Debug.LogError("Attempting to display null dialog - attempt will be ignored.");
                return null;
            }

            if (dialogQueue.Count == 0)
            {
                UIBlocking.Instance.Block();
            }

            var queuedDialog = new QueuedDialog(dialogPrefab, variableOptions);

            dialogQueue.Add(queuedDialog);

            while (dialogQueue[0] != queuedDialog)
            {
                await Task.Yield();
            }

            var result = await queuedDialog.Display(parent: transform);

            dialogQueue.Remove(queuedDialog);

            if (dialogQueue.Count == 0)
            {
                UIBlocking.Instance.Unblock();
            }

            return result;
        }
    }
}
