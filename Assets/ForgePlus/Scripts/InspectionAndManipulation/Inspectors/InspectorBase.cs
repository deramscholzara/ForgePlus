using UnityEngine;

namespace ForgePlus.Inspection
{
    public abstract class InspectorBase : MonoBehaviour
    {
        protected IFPInspectable inspectedObject;

        public void PopulateValues(IFPInspectable inspectedObject)
        {
            this.inspectedObject = inspectedObject;

            RefreshValuesInInspector();
        }

        public abstract void RefreshValuesInInspector();

        public abstract void UpdateValuesInInspectedObject();
    }
}
