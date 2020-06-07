using ForgePlus.LevelManipulation;
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
            var fpLine = inspectedObject as LevelEntity_Side;

            Value_Id.text =                         fpLine.NativeIndex.ToString();
            Value_Type.text =                       fpLine.NativeObject.Type.ToString();
            Value_LineIndex.text =                  fpLine.NativeObject.LineIndex.ToString();
            Value_PolygonIndex.text =               fpLine.NativeObject.PolygonIndex.ToString();
            Value_AmbientDelta.text =               fpLine.NativeObject.AmbientDelta.ToString();

            Value_Flags_IsControlPanel.SetIsOnWithoutNotify((fpLine.NativeObject.Flags & SideFlags.IsControlPanel) != 0);
            Value_Flags_InitiallyActive.SetIsOnWithoutNotify((fpLine.NativeObject.Flags & SideFlags.ControlPanelStatus) != 0);
            Value_Flags_IsRepairSwitch.SetIsOnWithoutNotify((fpLine.NativeObject.Flags & SideFlags.IsRepairSwitch) != 0);
            Value_Flags_CanBeDestroyed.SetIsOnWithoutNotify((fpLine.NativeObject.Flags & SideFlags.SwitchCanBeDestroyed) != 0);
            Value_Flags_IsDestructiveSwitch.SetIsOnWithoutNotify((fpLine.NativeObject.Flags & SideFlags.IsDestructiveSwitch) != 0);
            Value_Flags_ProjectilesOnly.SetIsOnWithoutNotify((fpLine.NativeObject.Flags & SideFlags.SwitchCanOnlyBeHitByProjectiles) != 0);
            Value_Flags_LightedMustBeAbove75Percent.SetIsOnWithoutNotify((fpLine.NativeObject.Flags & SideFlags.IsLightedSwitch) != 0);
            Value_Flags_Dirty.SetIsOnWithoutNotify((fpLine.NativeObject.Flags & SideFlags.Dirty) != 0);

            Value_ControlPanelType.text =           fpLine.NativeObject.IsControlPanel ? fpLine.NativeObject.GetControlPanelClass().ToString() : "-";
            Value_ControlPanelPermutation.text =    fpLine.NativeObject.IsControlPanel ? fpLine.NativeObject.ControlPanelPermutation.ToString() : "-";

            var hasPrimaryData =                    !fpLine.NativeObject.Primary.Texture.IsEmpty();
            Value_Primary_LightIndex.text =         hasPrimaryData ? fpLine.NativeObject.PrimaryLightsourceIndex.ToString() : "-";

            var hasSecondaryData =                  fpLine.NativeObject.Secondary.Texture.IsEmpty();
            Value_Secondary_LightIndex.text =       hasSecondaryData ? fpLine.NativeObject.SecondaryLightsourceIndex.ToString() : "-";

            var hasTransparentData =                fpLine.NativeObject.Transparent.Texture.IsEmpty();
            Value_Transparent_LightIndex.text =     hasTransparentData ? fpLine.NativeObject.TransparentLightsourceIndex.ToString() : "-";
        }
    }
}
