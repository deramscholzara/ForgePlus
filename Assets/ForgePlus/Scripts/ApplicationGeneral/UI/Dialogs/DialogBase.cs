using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    public abstract class DialogBase : MonoBehaviour
    {
        private bool submit = false;
        private string selectedOption = null;

        public async Task<string> Display(Transform parent, IList<string> options, IList<string> optionLabels)
        {
            var dialogInstance = Instantiate(this, parent);

            dialogInstance.Populate(options, optionLabels);

            while (!submit)
            {
                await Task.Yield();
            }

            return selectedOption;
        }

        protected abstract void Populate(IList<string> options, IList<string> optionLabels);

        protected void SetSelection(string selection)
        {
            selectedOption = selection;
        }

        protected void Submit()
        {
            submit = true;

            Destroy(gameObject);
        }
    }
}
