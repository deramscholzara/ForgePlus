using UnityEngine;

namespace ForgePlus.Inspection
{
    public abstract class Inspector_Base : MonoBehaviour
    {
        protected IInspectable inspectedObject;

        public void PopulateValues(IInspectable inspectedObject)
        {
            this.inspectedObject = inspectedObject;

            RefreshValuesInInspector();
        }

        public abstract void RefreshValuesInInspector();
    }
}
