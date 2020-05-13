﻿using ForgePlus.LevelManipulation;
using TMPro;
using UnityEngine.UI;
using Weland;

namespace ForgePlus.Inspection
{
    public class InspectorFPLight : InspectorBase
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
            var fpLight = inspectedObject as FPLight;

            Value_Id.text = fpLight.Index.ToString();
            Value_Tag.text = fpLight.WelandObject.TagIndex.ToString();
            Value_Type.text = fpLight.WelandObject.Type.ToString();
            Value_Phase.text = fpLight.WelandObject.Phase.ToString();

            Value_Flags_StartsActive.SetIsOnWithoutNotify(fpLight.WelandObject.InitiallyActive);
            Value_Flags_SlavedIntensities.SetIsOnWithoutNotify((fpLight.WelandObject.Flags & LightFlags.SlavedIntensities) != 0);
            Value_Flags_CycleAllStates.SetIsOnWithoutNotify(fpLight.WelandObject.Stateless);

            PopulateFunction(Value_PrimaryInactive_Function,
                             Value_PrimaryInactive_Period,
                             Value_PrimaryInactive_DeltaPeriod,
                             Value_PrimaryInactive_Intensity,
                             Value_PrimaryInactive_DeltaIntensity,
                             fpLight.WelandObject.PrimaryInactive);

            PopulateFunction(Value_PrimaryActive_Function,
                             Value_PrimaryActive_Period,
                             Value_PrimaryActive_DeltaPeriod,
                             Value_PrimaryActive_Intensity,
                             Value_PrimaryActive_DeltaIntensity,
                             fpLight.WelandObject.PrimaryActive);

            PopulateFunction(Value_SecondaryActive_Function,
                             Value_SecondaryActive_Period,
                             Value_SecondaryActive_DeltaPeriod,
                             Value_SecondaryActive_Intensity,
                             Value_SecondaryActive_DeltaIntensity,
                             fpLight.WelandObject.SecondaryActive);

            PopulateFunction(Value_BecomingInactive_Function,
                             Value_BecomingInactive_Period,
                             Value_BecomingInactive_DeltaPeriod,
                             Value_BecomingInactive_Intensity,
                             Value_BecomingInactive_DeltaIntensity,
                             fpLight.WelandObject.BecomingInactive);

            PopulateFunction(Value_BecomingActive_Function,
                             Value_BecomingActive_Period,
                             Value_BecomingActive_DeltaPeriod,
                             Value_BecomingActive_Intensity,
                             Value_BecomingActive_DeltaIntensity,
                             fpLight.WelandObject.BecomingActive);

            PopulateFunction(Value_SecondaryInactive_Function,
                             Value_SecondaryInactive_Period,
                             Value_SecondaryInactive_DeltaPeriod,
                             Value_SecondaryInactive_Intensity,
                             Value_SecondaryInactive_DeltaIntensity,
                             fpLight.WelandObject.SecondaryInactive);
        }

        public override void UpdateValuesInInspectedObject()
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject
            throw new System.NotImplementedException();
        }

        private void PopulateFunction(
            TextMeshProUGUI functionTypeField,
            TextMeshProUGUI periodField,
            TextMeshProUGUI deltaPeriodField,
            TextMeshProUGUI intensityField,
            TextMeshProUGUI deltaIntensityField,
            Weland.Light.Function welandStateFunction)
        {
            functionTypeField.text = welandStateFunction.LightingFunction.ToString();
            periodField.text = welandStateFunction.Period.ToString();
            deltaPeriodField.text = welandStateFunction.DeltaPeriod.ToString();
            intensityField.text = welandStateFunction.Intensity.ToString();
            deltaIntensityField.text = welandStateFunction.DeltaIntensity.ToString();
        }
    }
}
