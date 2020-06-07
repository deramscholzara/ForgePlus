using RuntimeCore.Entities.Geometry;
using TMPro;
using UnityEngine.UI;
using Weland;

namespace ForgePlus.Inspection
{
    public class Inspector_Side : Inspector_Base
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

        public override void RefreshValuesInInspector()
        {
            var line = inspectedObject as LevelEntity_Side;

            Value_Id.text =                         line.NativeIndex.ToString();
            Value_Type.text =                       line.NativeObject.Type.ToString();
            Value_LineIndex.text =                  line.NativeObject.LineIndex.ToString();
            Value_PolygonIndex.text =               line.NativeObject.PolygonIndex.ToString();
            Value_AmbientDelta.text =               line.NativeObject.AmbientDelta.ToString();

            Value_Flags_IsControlPanel.SetIsOnWithoutNotify((line.NativeObject.Flags & SideFlags.IsControlPanel) != 0);
            Value_Flags_InitiallyActive.SetIsOnWithoutNotify((line.NativeObject.Flags & SideFlags.ControlPanelStatus) != 0);
            Value_Flags_IsRepairSwitch.SetIsOnWithoutNotify((line.NativeObject.Flags & SideFlags.IsRepairSwitch) != 0);
            Value_Flags_CanBeDestroyed.SetIsOnWithoutNotify((line.NativeObject.Flags & SideFlags.SwitchCanBeDestroyed) != 0);
            Value_Flags_IsDestructiveSwitch.SetIsOnWithoutNotify((line.NativeObject.Flags & SideFlags.IsDestructiveSwitch) != 0);
            Value_Flags_ProjectilesOnly.SetIsOnWithoutNotify((line.NativeObject.Flags & SideFlags.SwitchCanOnlyBeHitByProjectiles) != 0);
            Value_Flags_LightedMustBeAbove75Percent.SetIsOnWithoutNotify((line.NativeObject.Flags & SideFlags.IsLightedSwitch) != 0);
            Value_Flags_Dirty.SetIsOnWithoutNotify((line.NativeObject.Flags & SideFlags.Dirty) != 0);

            Value_ControlPanelType.text =           line.NativeObject.IsControlPanel ? line.NativeObject.GetControlPanelClass().ToString() : "-";
            Value_ControlPanelPermutation.text =    line.NativeObject.IsControlPanel ? line.NativeObject.ControlPanelPermutation.ToString() : "-";

            var hasPrimaryData =                    !line.NativeObject.Primary.Texture.IsEmpty();
            Value_Primary_LightIndex.text =         hasPrimaryData ? line.NativeObject.PrimaryLightsourceIndex.ToString() : "-";

            var hasSecondaryData =                  line.NativeObject.Secondary.Texture.IsEmpty();
            Value_Secondary_LightIndex.text =       hasSecondaryData ? line.NativeObject.SecondaryLightsourceIndex.ToString() : "-";

            var hasTransparentData =                line.NativeObject.Transparent.Texture.IsEmpty();
            Value_Transparent_LightIndex.text =     hasTransparentData ? line.NativeObject.TransparentLightsourceIndex.ToString() : "-";
        }
    }
}
