using RuntimeCore.Entities.Geometry;
using TMPro;
using UnityEngine.UI;

namespace ForgePlus.Inspection
{
    public class Inspector_Media : Inspector_Base
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Type;
        public TextMeshProUGUI Value_LowHeight;
        public TextMeshProUGUI Value_HighHeight;
        public TextMeshProUGUI Value_FlowDirection;
        public TextMeshProUGUI Value_FlowMagnitude;
        public TextMeshProUGUI Value_LightIndex;
        public TextMeshProUGUI Value_MinimumLightIntensity;
        
        public Toggle Value_Flags_FloorObstructsSound;

        public override void RefreshValuesInInspector()
        {
            var media = inspectedObject as LevelEntity_Media;

            Value_Id.text =                     media.NativeIndex.ToString();
            Value_Type.text =                   media.NativeObject.Type.ToString();
            Value_LowHeight.text =              media.NativeObject.Low.ToString();
            Value_HighHeight.text =             media.NativeObject.High.ToString();
            Value_FlowDirection.text =          media.NativeObject.Direction.ToString();
            Value_FlowMagnitude.text =          media.NativeObject.CurrentMagnitude.ToString();
            Value_LightIndex.text =             media.NativeObject.LightIndex.ToString();
            Value_MinimumLightIntensity.text =  media.NativeObject.MinimumLightIntensity.ToString();

            Value_Flags_FloorObstructsSound.SetIsOnWithoutNotify(media.NativeObject.SoundObstructedByFloor);
        }
    }
}
