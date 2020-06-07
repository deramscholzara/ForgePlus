using ForgePlus.LevelManipulation;
using RuntimeCore.Entities;
using TMPro;

namespace ForgePlus.Inspection
{
    public class Inspector_Annotation : Inspector_Base
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Text;

        public override void RefreshValuesInInspector()
        {
            var fpAnnotation = inspectedObject as LevelEntity_Annotation;

            Value_Id.text = fpAnnotation.NativeIndex.ToString();
            Value_Text.text = fpAnnotation.NativeObject.Text;
        }
    }
}
