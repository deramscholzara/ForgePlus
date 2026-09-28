using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities.Geometry;
using TMPro;
using UnityEngine.UI;

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
            Value_Type.text =                       AlephOneNames.SideType(line.NativeObject.type);
            Value_LineIndex.text =                  line.NativeObject.line_index.ToString();
            Value_PolygonIndex.text =               line.NativeObject.polygon_index.ToString();
            Value_AmbientDelta.text =               line.NativeObject.ambient_delta.ToString();

            Value_Flags_IsControlPanel.SetIsOnWithoutNotify(map.SIDE_IS_CONTROL_PANEL(line.NativeObject));
            Value_Flags_InitiallyActive.SetIsOnWithoutNotify(map.GET_CONTROL_PANEL_STATUS(line.NativeObject));
            Value_Flags_IsRepairSwitch.SetIsOnWithoutNotify(map.SIDE_IS_REPAIR_SWITCH(line.NativeObject));
            Value_Flags_CanBeDestroyed.SetIsOnWithoutNotify(csmacros.TEST_FLAG(line.NativeObject.flags, map._side_switch_can_be_destroyed));
            Value_Flags_IsDestructiveSwitch.SetIsOnWithoutNotify(csmacros.TEST_FLAG(line.NativeObject.flags, map._side_is_destructive_switch));
            Value_Flags_ProjectilesOnly.SetIsOnWithoutNotify(csmacros.TEST_FLAG(line.NativeObject.flags, map._side_switch_can_only_be_hit_by_projectiles));
            Value_Flags_LightedMustBeAbove75Percent.SetIsOnWithoutNotify(csmacros.TEST_FLAG(line.NativeObject.flags, map._side_is_lighted_switch));
            Value_Flags_Dirty.SetIsOnWithoutNotify(map.SIDE_IS_DIRTY(line.NativeObject));

            Value_ControlPanelType.text =           map.SIDE_IS_CONTROL_PANEL(line.NativeObject) ? AlephOneNames.ControlPanelClass(devices.get_panel_class(line.NativeObject.control_panel_type)) : "-";
            Value_ControlPanelPermutation.text =    map.SIDE_IS_CONTROL_PANEL(line.NativeObject) ? line.NativeObject.control_panel_permutation.ToString() : "-";

            var hasPrimaryData =                    !line.NativeObject.primary_texture.texture.IsEmptyShapeDescriptor();
            Value_Primary_LightIndex.text =         hasPrimaryData ? line.NativeObject.primary_lightsource_index.ToString() : "-";

            var hasSecondaryData =                  !line.NativeObject.secondary_texture.texture.IsEmptyShapeDescriptor();
            Value_Secondary_LightIndex.text =       hasSecondaryData ? line.NativeObject.secondary_lightsource_index.ToString() : "-";

            var hasTransparentData =                !line.NativeObject.transparent_texture.texture.IsEmptyShapeDescriptor();
            Value_Transparent_LightIndex.text =     hasTransparentData ? line.NativeObject.transparent_lightsource_index.ToString() : "-";
        }
    }
}
