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
            var annotation = inspectedObject as LevelEntity_Annotation;

            Value_Id.text = annotation.NativeIndex.ToString();
            Value_Text.text = annotation.NativeObject.Text;
        }
    }
}
