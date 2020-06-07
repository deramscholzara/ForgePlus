using ForgePlus.LevelManipulation;
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
            var fpMedia = inspectedObject as LevelEntity_Media;

            Value_Id.text =                     fpMedia.NativeIndex.ToString();
            Value_Type.text =                   fpMedia.NativeObject.Type.ToString();
            Value_LowHeight.text =              fpMedia.NativeObject.Low.ToString();
            Value_HighHeight.text =             fpMedia.NativeObject.High.ToString();
            Value_FlowDirection.text =          fpMedia.NativeObject.Direction.ToString();
            Value_FlowMagnitude.text =          fpMedia.NativeObject.CurrentMagnitude.ToString();
            Value_LightIndex.text =             fpMedia.NativeObject.LightIndex.ToString();
            Value_MinimumLightIntensity.text =  fpMedia.NativeObject.MinimumLightIntensity.ToString();

            Value_Flags_FloorObstructsSound.SetIsOnWithoutNotify(fpMedia.NativeObject.SoundObstructedByFloor);
        }
    }
}
