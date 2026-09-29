using AlephOne;
using ForgePlus.Extensions;
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
        public string LineIndex
        {
            get
            {
                return Side.line_index.ToString();
            }
        }

        [CreateProperty]
        public string PolygonIndex
        {
            get
            {
                return Side.polygon_index.ToString();
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

        [CreateProperty]
        public bool LightedMustBeAbove75Percent
        {
            get
            {
                return csmacros.TEST_FLAG(Side.flags, map._side_is_lighted_switch);
            }
        }

        [CreateProperty]
        public bool Dirty
        {
            get
            {
                return map.SIDE_IS_DIRTY(Side);
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

        [CreateProperty]
        public string PrimaryLightIndex
        {
            get
            {
                return Side.primary_texture.texture.IsEmptyShapeDescriptor() ? "-" : Side.primary_lightsource_index.ToString();
            }
        }

        [CreateProperty]
        public string SecondaryLightIndex
        {
            get
            {
                return Side.secondary_texture.texture.IsEmptyShapeDescriptor() ? "-" : Side.secondary_lightsource_index.ToString();
            }
        }

        [CreateProperty]
        public string TransparentLightIndex
        {
            get
            {
                return Side.transparent_texture.texture.IsEmptyShapeDescriptor() ? "-" : Side.transparent_lightsource_index.ToString();
            }
        }
    }
}
