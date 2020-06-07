using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    public class DialogManager : SingletonMonoBehaviour<DialogManager>
    {
        private class QueuedDialog
        {
            public Dialog_Base DialogPrefab { get; private set; }

            public IList<string> VariableOptions { get; private set; }

            public IList<string> VariableOptionLabels { get; private set; }

            public QueuedDialog(Dialog_Base dialogPrefab, IList<string> variableOptions, IList<string> variableOptionLabels)
            {
                DialogPrefab = dialogPrefab;
                VariableOptions = variableOptions;
                VariableOptionLabels = variableOptionLabels;
            }

            public async Task<string> Display(Transform parent)
            {
                var result = await DialogPrefab.Display(parent, VariableOptions, VariableOptionLabels);

                return result;
            }
        }

        private readonly List<QueuedDialog> dialogQueue = new List<QueuedDialog>();

        public async Task<string> DisplayQueuedDialog(Dialog_Base dialogPrefab, IList<string> variableOptions, IList<string> variableOptionLabels = null)
        {
            if (dialogPrefab == null)
            {
                Debug.LogError("Attempting to display null dialog - attempt will be ignored.");
                return null;
            }

            if (dialogQueue.Count == 0)
            {
                UIBlocking.Instance.Block();
                gameObject.SetActive(true);
            }

            var queuedDialog = new QueuedDialog(dialogPrefab, variableOptions, variableOptionLabels);

            dialogQueue.Add(queuedDialog);

            while (dialogQueue[0] != queuedDialog)
            {
                await Task.Yield();
            }

            var result = await queuedDialog.Display(parent: transform);

            dialogQueue.Remove(queuedDialog);

            if (dialogQueue.Count == 0)
            {
                gameObject.SetActive(false);
                UIBlocking.Instance.Unblock();
            }

            return result;
        }

        private void Start()
        {
            gameObject.SetActive(false);
        }
    }
}
