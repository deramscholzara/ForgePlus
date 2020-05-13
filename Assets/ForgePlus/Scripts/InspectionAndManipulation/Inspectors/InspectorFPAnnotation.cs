using ForgePlus.LevelManipulation;
using TMPro;

namespace ForgePlus.Inspection
{
    public class InspectorFPAnnotation : InspectorBase
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Text;

        public override void RefreshValuesInInspector()
        {
            var fpAnnotation = inspectedObject as FPAnnotation;

            Value_Id.text = fpAnnotation.Index.ToString();
            Value_Text.text = fpAnnotation.WelandObject.Text;
        }

        public override void UpdateValuesInInspectedObject()
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject
            throw new System.NotImplementedException();
        }
    }
}
