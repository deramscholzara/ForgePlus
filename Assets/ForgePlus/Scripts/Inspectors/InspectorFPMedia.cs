﻿using ForgePlus.LevelManipulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Weland;
using UnityEngine.UI;
using ForgePlus.ShapesCollections;

namespace ForgePlus.Inspection
{
    public class InspectorFPMedia : InspectorBase
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Type;
        public TextMeshProUGUI Value_LowHeight;
        public TextMeshProUGUI Value_HighHeight;
        public TextMeshProUGUI Value_FlowDirection;
        public TextMeshProUGUI Value_FlowMagnitude;
        public TextMeshProUGUI Value_LightIndex;
        public TextMeshProUGUI Value_MinimumLightIntensity;
        public TextMeshProUGUI Value_Flags;

        public override void PopulateValues(IFPInspectable inspectedObject)
        {
            var fpMedia = inspectedObject as FPMedia;

            Value_Id.text =                     fpMedia.Index.ToString();
            Value_Type.text =                   fpMedia.WelandObject.Type.ToString();
            Value_LowHeight.text =              fpMedia.WelandObject.Low.ToString();
            Value_HighHeight.text =             fpMedia.WelandObject.High.ToString();
            Value_FlowDirection.text =          fpMedia.WelandObject.Direction.ToString();
            Value_FlowMagnitude.text =          fpMedia.WelandObject.CurrentMagnitude.ToString();
            Value_LightIndex.text =             fpMedia.WelandObject.LightIndex.ToString();
            Value_MinimumLightIntensity.text =  fpMedia.WelandObject.MinimumLightIntensity.ToString();
            Value_Flags.text =                  fpMedia.WelandObject.SoundObstructedByFloor.ToString();
        }

        public override void UpdateValuesInInspectedObject(IFPInspectable inspectedObject)
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject
            throw new System.NotImplementedException();
        }
    }
}
