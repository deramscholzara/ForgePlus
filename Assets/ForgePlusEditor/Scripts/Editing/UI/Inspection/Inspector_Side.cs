using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.UI;
using RuntimeCore.Entities.Geometry;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_Side : Inspector_Base<LevelEntity_Side>
    {
        public Inspector_Side(LevelEntity_Side side) : base(side)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Side";
            }
        }

        private side_data Side
        {
            get
            {
                return Entity.NativeObject;
            }
        }

        [CreateProperty]
        public string Id
        {
            get
            {
                return Entity.NativeIndex.ToString();
            }
        }

        [CreateProperty]
        public string Type
        {
            get
            {
                return AlephOneNames.SideType(Side.type);
            }
        }

        [CreateProperty]
        public string AmbientDelta
        {
            get
            {
                return Side.ambient_delta.ToString();
            }
        }

        [CreateProperty]
        public bool IsControlPanel
        {
            get
            {
                return map.SIDE_IS_CONTROL_PANEL(Side);
            }
        }

        [CreateProperty]
        public bool InitiallyActive
        {
            get
            {
                return map.GET_CONTROL_PANEL_STATUS(Side);
            }
        }

        [CreateProperty]
        public bool IsRepairSwitch
        {
            get
            {
                return map.SIDE_IS_REPAIR_SWITCH(Side);
            }
        }

        [CreateProperty]
        public bool CanBeDestroyed
        {
            get
            {
                return csmacros.TEST_FLAG(Side.flags, map._side_switch_can_be_destroyed);
            }
        }

        [CreateProperty]
        public bool IsDestructiveSwitch
        {
            get
            {
                return csmacros.TEST_FLAG(Side.flags, map._side_is_destructive_switch);
            }
        }

        [CreateProperty]
        public bool ProjectilesOnly
        {
            get
            {
                return csmacros.TEST_FLAG(Side.flags, map._side_switch_can_only_be_hit_by_projectiles);
            }
        }

        // A switch that only works while its primary surface's light is above 75% (devices.cpp: switch_can_be_toggled)
        [CreateProperty]
        public bool LightedMustBeAbove75Percent
        {
            get
            {
                return csmacros.TEST_FLAG(Side.flags, map._side_is_lighted_switch);
            }
            set
            {
                SetFlag(map._side_is_lighted_switch, value);
            }
        }

        [CreateProperty]
        public bool IsLightedMustBeAbove75PercentEditable
        {
            get
            {
                return IsControlPanel;
            }
        }

        // Aleph One only: Marathon 1's lighted switch, which only works while its light is above 50%. Aleph One sets it
        // on a Marathon 1 level's switches (in vacuum levels), and the 75% flag outranks it.
        [CreateProperty]
        public bool LightedMustBeAbove50Percent
        {
            get
            {
                return csmacros.TEST_FLAG(Side.flags, map._side_is_m1_lighted_switch);
            }
            set
            {
                SetFlag(map._side_is_m1_lighted_switch, value);
            }
        }

        [CreateProperty]
        public bool IsLightedMustBeAbove50PercentEditable
        {
            get
            {
                return IsControlPanel && !LightedMustBeAbove75Percent;
            }
        }

        // Aleph One only: a tag switch that needs an item (such as a chip insertion) works without it, as Marathon 1's do
        // (Aleph One sets it on a Marathon 1 level's sides)
        [CreateProperty]
        public bool ItemIsOptional
        {
            get
            {
                return csmacros.TEST_FLAG(Side.flags, map._side_item_is_optional);
            }
            set
            {
                SetFlag(map._side_item_is_optional, value);
            }
        }

        [CreateProperty]
        public bool IsItemIsOptionalEditable
        {
            get
            {
                if (!IsControlPanel)
                {
                    return false;
                }

                var definition = devices.get_control_panel_definition(Side.control_panel_type);

                return definition != null && definition._class == map._panel_is_tag_switch && definition.item != cstypes.NONE;
            }
        }

        [CreateProperty]
        public string ControlPanelType
        {
            get
            {
                return IsControlPanel ? AlephOneNames.ControlPanelClass(devices.get_panel_class(Side.control_panel_type)) : "-";
            }
        }

        [CreateProperty]
        public string ControlPanelPermutation
        {
            get
            {
                return IsControlPanel ? Side.control_panel_permutation.ToString() : "-";
            }
        }

        // Each surface's light, which a surface without a texture hasn't, and a landscape doesn't use (so it can't be
        // set, as painting a light can't set it)
        [CreateProperty]
        public int PrimaryLightIndex
        {
            get
            {
                return Side.primary_texture.texture.IsEmptyShapeDescriptor() ? LightIndexField.NoLight : Side.primary_lightsource_index;
            }
            set
            {
                SetLight(LevelEntity_Side.DataSources.Primary, value);
            }
        }

        [CreateProperty]
        public bool IsPrimaryLightIndexEditable
        {
            get
            {
                return UsesLight(Side.primary_texture.texture);
            }
        }

        [CreateProperty]
        public int SecondaryLightIndex
        {
            get
            {
                return Side.secondary_texture.texture.IsEmptyShapeDescriptor() ? LightIndexField.NoLight : Side.secondary_lightsource_index;
            }
            set
            {
                SetLight(LevelEntity_Side.DataSources.Secondary, value);
            }
        }

        [CreateProperty]
        public bool IsSecondaryLightIndexEditable
        {
            get
            {
                return UsesLight(Side.secondary_texture.texture);
            }
        }

        [CreateProperty]
        public int TransparentLightIndex
        {
            get
            {
                return Side.transparent_texture.texture.IsEmptyShapeDescriptor() ? LightIndexField.NoLight : Side.transparent_lightsource_index;
            }
            set
            {
                SetLight(LevelEntity_Side.DataSources.Transparent, value);
            }
        }

        [CreateProperty]
        public bool IsTransparentLightIndexEditable
        {
            get
            {
                return UsesLight(Side.transparent_texture.texture);
            }
        }

        private static bool UsesLight(ushort texture)
        {
            return !texture.IsEmptyShapeDescriptor() && !texture.UsesLandscapeCollection();
        }

        private void SetLight(LevelEntity_Side.DataSources dataSource, int lightIndex)
        {
            if (lightIndex >= 0)
            {
                Edit(side => side.SetLight(dataSource, (short) lightIndex));
            }
        }

        private void SetFlag(ushort flag, bool isSet)
        {
            Edit(side => side.NativeObject.flags = isSet ? (ushort) (side.NativeObject.flags | flag) : (ushort) (side.NativeObject.flags & ~flag));
        }
    }
}
