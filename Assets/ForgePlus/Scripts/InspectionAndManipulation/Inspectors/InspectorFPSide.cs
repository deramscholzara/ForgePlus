﻿using ForgePlus.LevelManipulation;
using ForgePlus.ShapesCollections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Weland;

namespace ForgePlus.Inspection
{
    public class InspectorFPSide : InspectorBase
    {
        public TextMeshProUGUI Value_Id;
        public TextMeshProUGUI Value_Type;
        public TextMeshProUGUI Value_LineIndex;
        public TextMeshProUGUI Value_PolygonIndex;
        public TextMeshProUGUI Value_AmbientDelta;

        public Toggle Value_Flags_InitiallyActive;
        public Toggle Value_Flags_IsRepairSwitch;
        public Toggle Value_Flags_CanBeDestroyed;
        public Toggle Value_Flags_LightedMustBeAbove75Percent;
        public Toggle Value_Flags_ProjectilesOnly;
        public Toggle Value_Flags_IsControlPanel;
        public Toggle Value_Flags_IsDestructiveSwitch;
        public Toggle Value_Flags_Dirty;

        public TextMeshProUGUI Value_ControlPanelType;
        public TextMeshProUGUI Value_ControlPanelPermutation;

        public TextMeshProUGUI Value_Primary_LightIndex;

        public TextMeshProUGUI Value_Secondary_LightIndex;

        public TextMeshProUGUI Value_Transparent_LightIndex;

        public override void PopulateValues(IFPInspectable inspectedObject)
        {
            var fpLine = inspectedObject as FPSide;

            Value_Id.text =                         fpLine.Index.ToString();
            Value_Type.text =                       fpLine.WelandObject.Type.ToString();
            Value_LineIndex.text =                  fpLine.WelandObject.LineIndex.ToString();
            Value_PolygonIndex.text =               fpLine.WelandObject.PolygonIndex.ToString();
            Value_AmbientDelta.text =               fpLine.WelandObject.AmbientDelta.ToString();

            Value_Flags_IsControlPanel.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & SideFlags.IsControlPanel) != 0);
            Value_Flags_InitiallyActive.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & SideFlags.ControlPanelStatus) != 0);
            Value_Flags_IsRepairSwitch.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & SideFlags.IsRepairSwitch) != 0);
            Value_Flags_CanBeDestroyed.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & SideFlags.SwitchCanBeDestroyed) != 0);
            Value_Flags_IsDestructiveSwitch.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & SideFlags.IsDestructiveSwitch) != 0);
            Value_Flags_ProjectilesOnly.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & SideFlags.SwitchCanOnlyBeHitByProjectiles) != 0);
            Value_Flags_LightedMustBeAbove75Percent.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & SideFlags.IsLightedSwitch) != 0);
            Value_Flags_Dirty.SetIsOnWithoutNotify((fpLine.WelandObject.Flags & SideFlags.Dirty) != 0);

            Value_ControlPanelType.text =           fpLine.WelandObject.IsControlPanel ? fpLine.WelandObject.GetControlPanelClass().ToString() : "-";
            Value_ControlPanelPermutation.text =    fpLine.WelandObject.IsControlPanel ? fpLine.WelandObject.ControlPanelPermutation.ToString() : "-";

            var hasPrimaryData =                    (ushort)fpLine.WelandObject.Primary.Texture != ushort.MaxValue;
            Value_Primary_LightIndex.text =         hasPrimaryData ? fpLine.WelandObject.PrimaryLightsourceIndex.ToString() : "-";

            var hasSecondaryData =                  (ushort)fpLine.WelandObject.Secondary.Texture != ushort.MaxValue;
            Value_Secondary_LightIndex.text =       hasSecondaryData ? fpLine.WelandObject.SecondaryLightsourceIndex.ToString() : "-";

            var hasTransparentData =                (ushort)fpLine.WelandObject.Transparent.Texture != ushort.MaxValue;
            Value_Transparent_LightIndex.text =     hasTransparentData ? fpLine.WelandObject.TransparentLightsourceIndex.ToString() : "-";
        }

        public override void UpdateValuesInInspectedObject(IFPInspectable inspectedObject)
        {
            // TODO: Use this when editing is added - the UI editing controls should call this when their values change,
            //       this will then set the values from the controls onto the inspectedObject
            throw new System.NotImplementedException();
        }
    }
}
