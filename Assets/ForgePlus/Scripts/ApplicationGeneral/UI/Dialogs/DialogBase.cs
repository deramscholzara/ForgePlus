using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    public abstract class DialogBase : MonoBehaviour
    {
        private bool submit = false;
        private object selectedOption = null;

        public async Task<object> Display(Transform parent, IList<object> options)
        {
            var dialogInstance = Instantiate(this, parent);

            dialogInstance.Populate(options);

            while (!submit)
            {
                await Task.Yield();
            }

            return selectedOption;
        }

        protected abstract void Populate(IList<object> options);

        public void SetSelection(object selection)
        {
            selectedOption = selection;
        }

        public void Submit()
        {
            submit = true;

            Destroy(gameObject);
        }
    }
}
