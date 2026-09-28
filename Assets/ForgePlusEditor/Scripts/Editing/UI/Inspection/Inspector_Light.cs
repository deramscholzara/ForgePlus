using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities;
using TMPro;
using UnityEngine.UI;

namespace ForgePlus.Inspection
{
    public class Inspector_Light : Inspector_Base
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Tag;
        public TextMeshProUGUI Value_Type;
        public TextMeshProUGUI Value_Phase;

        public Toggle Value_Flags_StartsActive;
        public Toggle Value_Flags_SlavedIntensities;
        public Toggle Value_Flags_CycleAllStates;

        public TextMeshProUGUI Value_BecomingActive_Function;
        public TextMeshProUGUI Value_BecomingActive_Period;
        public TextMeshProUGUI Value_BecomingActive_DeltaPeriod;
        public TextMeshProUGUI Value_BecomingActive_Intensity;
        public TextMeshProUGUI Value_BecomingActive_DeltaIntensity;

        public TextMeshProUGUI Value_PrimaryActive_Function;
        public TextMeshProUGUI Value_PrimaryActive_Period;
        public TextMeshProUGUI Value_PrimaryActive_DeltaPeriod;
        public TextMeshProUGUI Value_PrimaryActive_Intensity;
        public TextMeshProUGUI Value_PrimaryActive_DeltaIntensity;

        public TextMeshProUGUI Value_SecondaryActive_Function;
        public TextMeshProUGUI Value_SecondaryActive_Period;
        public TextMeshProUGUI Value_SecondaryActive_DeltaPeriod;
        public TextMeshProUGUI Value_SecondaryActive_Intensity;
        public TextMeshProUGUI Value_SecondaryActive_DeltaIntensity;

        public TextMeshProUGUI Value_BecomingInactive_Function;
        public TextMeshProUGUI Value_BecomingInactive_Period;
        public TextMeshProUGUI Value_BecomingInactive_DeltaPeriod;
        public TextMeshProUGUI Value_BecomingInactive_Intensity;
        public TextMeshProUGUI Value_BecomingInactive_DeltaIntensity;

        public TextMeshProUGUI Value_PrimaryInactive_Function;
        public TextMeshProUGUI Value_PrimaryInactive_Period;
        public TextMeshProUGUI Value_PrimaryInactive_DeltaPeriod;
        public TextMeshProUGUI Value_PrimaryInactive_Intensity;
        public TextMeshProUGUI Value_PrimaryInactive_DeltaIntensity;

        public TextMeshProUGUI Value_SecondaryInactive_Function;
        public TextMeshProUGUI Value_SecondaryInactive_Period;
        public TextMeshProUGUI Value_SecondaryInactive_DeltaPeriod;
        public TextMeshProUGUI Value_SecondaryInactive_Intensity;
        public TextMeshProUGUI Value_SecondaryInactive_DeltaIntensity;

        public override void RefreshValuesInInspector()
        {
            var light = inspectedObject as LevelEntity_Light;

            Value_Id.text = light.NativeIndex.ToString();
            Value_Tag.text = light.NativeObject.tag.ToString();
            Value_Type.text = AlephOneNames.LightType(light.NativeObject.type);
            Value_Phase.text = light.NativeObject.phase.ToString();

            Value_Flags_StartsActive.SetIsOnWithoutNotify(lightsource.LIGHT_IS_INITIALLY_ACTIVE(light.NativeObject));
            Value_Flags_SlavedIntensities.SetIsOnWithoutNotify(csmacros.TEST_FLAG16(light.NativeObject.flags, lightsource._light_has_slaved_intensities));
            Value_Flags_CycleAllStates.SetIsOnWithoutNotify(lightsource.LIGHT_IS_STATELESS(light.NativeObject));

            PopulateFunction(Value_PrimaryInactive_Function,
                             Value_PrimaryInactive_Period,
                             Value_PrimaryInactive_DeltaPeriod,
                             Value_PrimaryInactive_Intensity,
                             Value_PrimaryInactive_DeltaIntensity,
                             light.NativeObject.primary_inactive);

            PopulateFunction(Value_PrimaryActive_Function,
                             Value_PrimaryActive_Period,
                             Value_PrimaryActive_DeltaPeriod,
                             Value_PrimaryActive_Intensity,
                             Value_PrimaryActive_DeltaIntensity,
                             light.NativeObject.primary_active);

            PopulateFunction(Value_SecondaryActive_Function,
                             Value_SecondaryActive_Period,
                             Value_SecondaryActive_DeltaPeriod,
                             Value_SecondaryActive_Intensity,
                             Value_SecondaryActive_DeltaIntensity,
                             light.NativeObject.secondary_active);

            PopulateFunction(Value_BecomingInactive_Function,
                             Value_BecomingInactive_Period,
                             Value_BecomingInactive_DeltaPeriod,
                             Value_BecomingInactive_Intensity,
                             Value_BecomingInactive_DeltaIntensity,
                             light.NativeObject.becoming_inactive);

            PopulateFunction(Value_BecomingActive_Function,
                             Value_BecomingActive_Period,
                             Value_BecomingActive_DeltaPeriod,
                             Value_BecomingActive_Intensity,
                             Value_BecomingActive_DeltaIntensity,
                             light.NativeObject.becoming_active);

            PopulateFunction(Value_SecondaryInactive_Function,
                             Value_SecondaryInactive_Period,
                             Value_SecondaryInactive_DeltaPeriod,
                             Value_SecondaryInactive_Intensity,
                             Value_SecondaryInactive_DeltaIntensity,
                             light.NativeObject.secondary_inactive);
        }

        private void PopulateFunction(
            TextMeshProUGUI functionTypeField,
            TextMeshProUGUI periodField,
            TextMeshProUGUI deltaPeriodField,
            TextMeshProUGUI intensityField,
            TextMeshProUGUI deltaIntensityField,
            lighting_function_specification stateFunction)
        {
            functionTypeField.text = AlephOneNames.LightingFunction(stateFunction.function);
            periodField.text = stateFunction.period.ToString();
            deltaPeriodField.text = stateFunction.delta_period.ToString();
            intensityField.text = AlephOneExtensions.FixedToFloat(stateFunction.intensity).ToString();
            deltaIntensityField.text = AlephOneExtensions.FixedToFloat(stateFunction.delta_intensity).ToString();
        }
    }
}
