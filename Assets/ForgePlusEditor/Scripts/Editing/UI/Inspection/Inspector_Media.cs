using ForgePlus.Extensions;
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
            Value_Type.text =                   AlephOneNames.MediaType(media.NativeObject.type);
            Value_LowHeight.text =              media.NativeObject.low.ToString();
            Value_HighHeight.text =             media.NativeObject.high.ToString();
            Value_FlowDirection.text =          AlephOneExtensions.AngleToDegrees(media.NativeObject.current_direction).ToString();
            Value_FlowMagnitude.text =          media.NativeObject.current_magnitude.ToString();
            Value_LightIndex.text =             media.NativeObject.light_index.ToString();
            Value_MinimumLightIntensity.text =  AlephOneExtensions.FixedToFloat(media.NativeObject.minimum_light_intensity).ToString();

            Value_Flags_FloorObstructsSound.SetIsOnWithoutNotify(AlephOne.media.MEDIA_SOUND_OBSTRUCTED_BY_FLOOR(media.NativeObject));
        }
    }
}
