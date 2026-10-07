using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using ForgePlus.UI;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;

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

        // Added to the light of each of its surfaces as they're drawn, then pinned to between dark and full (render.c:
        // render_node_side), so a value past full light either way has no more effect
        [CreateProperty]
        public int AmbientDelta
        {
            get
            {
                return Side.ambient_delta;
            }
            set
            {
                var delta = Math.Clamp(value, -cstypes.FIXED_ONE, cstypes.FIXED_ONE);

                if (delta != Side.ambient_delta)
                {
                    Edit(side => side.NativeObject.ambient_delta = delta);
                }
                else
                {
                    RefreshInspectorsOf(Entity);
                }
            }
        }

        [CreateProperty]
        public int AmbientDeltaSlider
        {
            get
            {
                return Math.Clamp(AmbientDelta, -cstypes.FIXED_ONE, cstypes.FIXED_ONE);
            }
            set
            {
                if (value != AmbientDeltaSlider)
                {
                    AmbientDelta = value;
                }
            }
        }

        [CreateProperty]
        public string AmbientDeltaCaption
        {
            get
            {
                if (Side.ambient_delta == 0)
                {
                    return Strings.Get(Strings.Geometry, "Inspector.Side.AmbientDeltaCaption.None");
                }

                var percent = Math.Round(Math.Abs(Side.ambient_delta) * 100.0 / cstypes.FIXED_ONE, 1);

                return Strings.Get(Strings.Geometry, Side.ambient_delta > 0 ? "Inspector.Side.AmbientDeltaCaption.Brighter" : "Inspector.Side.AmbientDeltaCaption.Darker", percent);
            }
        }

        // Making a side a control panel gives it one of the level's panel types (unless it already has one) and that
        // type's texture. Its flags are grayed out while it isn't one.
        [CreateProperty]
        public bool IsControlPanel
        {
            get
            {
                return map.SIDE_IS_CONTROL_PANEL(Side);
            }
            set
            {
                if (value == IsControlPanel)
                {
                    return;
                }

                if (!value)
                {
                    SetFlag(map._side_is_control_panel, false);
                    return;
                }

                var type = ControlPanelTypes.IsLevelType(Side.control_panel_type) ? Side.control_panel_type : ControlPanelTypes.LevelDefault();
                Edit(side =>
                {
                    map.SET_SIDE_CONTROL_PANEL(side.NativeObject, true);
                    side.NativeObject.control_panel_type = type;
                });

                ApplyPanelTexture();
            }
        }

        // A tag switch starts in it (a light or platform switch starts as its light or platform is), and its texture
        // shows it
        [CreateProperty]
        public bool InitiallyActive
        {
            get
            {
                return map.GET_CONTROL_PANEL_STATUS(Side);
            }
            set
            {
                SetFlag(map._control_panel_status, value);
                ApplyPanelTexture();
            }
        }

        // Must be switched on to finish the level
        [CreateProperty]
        public bool IsRepairSwitch
        {
            get
            {
                return map.SIDE_IS_REPAIR_SWITCH(Side);
            }
            set
            {
                SetFlag(map._side_is_repair_switch, value);
            }
        }

        // A projectile that hits it switches it and breaks it
        [CreateProperty]
        public bool CanBeDestroyed
        {
            get
            {
                return csmacros.TEST_FLAG(Side.flags, map._side_switch_can_be_destroyed);
            }
            set
            {
                SetFlag(map._side_switch_can_be_destroyed, value);
            }
        }

        // Uses up the item it takes (such as a chip)
        [CreateProperty]
        public bool IsDestructiveSwitch
        {
            get
            {
                return csmacros.TEST_FLAG(Side.flags, map._side_is_destructive_switch);
            }
            set
            {
                SetFlag(map._side_is_destructive_switch, value);
            }
        }

        [CreateProperty]
        public bool ProjectilesOnly
        {
            get
            {
                return csmacros.TEST_FLAG(Side.flags, map._side_switch_can_only_be_hit_by_projectiles);
            }
            set
            {
                SetFlag(map._side_switch_can_only_be_hit_by_projectiles, value);
            }
        }

        // Only works while its primary surface's light is above 75% (devices.cpp: switch_can_be_toggled)
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

        // Aleph One only: Marathon 1's lighted switch (above 50%), which Aleph One sets on a Marathon 1 level's switches
        // in vacuum levels. The 75% flag outranks it.
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

        // Aleph One only: a tag switch that needs an item works without it, as Marathon 1's do (Aleph One sets it on a
        // Marathon 1 level's sides)
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

        // Gives it its class, textures and sounds. Choosing one gives the side its texture.
        [CreateProperty]
        public string ControlPanelType
        {
            get
            {
                return IsControlPanel ? ControlPanelTypes.Choice(Side.control_panel_type) : "-";
            }
            set
            {
                if (ControlPanelTypes.TryParse(Side.control_panel_type, value, out var type) && type != Side.control_panel_type)
                {
                    Edit(side => side.NativeObject.control_panel_type = type);
                    ApplyPanelTexture();
                }
            }
        }

        [CreateProperty]
        public List<string> ControlPanelTypeChoices
        {
            get
            {
                return ControlPanelTypes.All(Side.control_panel_type);
            }
        }

        [CreateProperty]
        public List<string> ControlPanelTypeAlephOneOnlyChoices
        {
            get
            {
                return ControlPanelTypes.AlephOneOnly(Side.control_panel_type);
            }
        }

        [CreateProperty]
        public bool IsControlPanelTypeEditable
        {
            get
            {
                return IsControlPanel;
            }
        }

        [CreateProperty]
        public string ControlPanelTypeNote
        {
            get
            {
                return IsControlPanel ? ControlPanelTypes.Note(Side.control_panel_type) : string.Empty;
            }
        }

        // The game sets a panel's primary texture from its panel type (as the level starts, and as it switches), so a
        // texture of the side's own isn't seen there
        [CreateProperty]
        public string ControlPanelTextureNote
        {
            get
            {
                return IsControlPanel && devices.get_control_panel_definition(Side.control_panel_type) != null && !Entity.ShowsPanelTexture ?
                       Strings.Get(Strings.Geometry, "Inspector.Side.ControlPanelTextureNote") :
                       string.Empty;
            }
        }

        // What it acts on, by its class (see ControlPanelPermutationCaption)
        [CreateProperty]
        public int ControlPanelPermutation
        {
            get
            {
                return Side.control_panel_permutation;
            }
            set
            {
                Edit(side => side.NativeObject.control_panel_permutation = ClampToShort(value));
            }
        }

        [CreateProperty]
        public bool IsControlPanelPermutationEditable
        {
            get
            {
                return IsControlPanel;
            }
        }

        [CreateProperty]
        public string ControlPanelPermutationCaption
        {
            get
            {
                return PermutationCaptions.ForControlPanel(Side);
            }
        }

        // A surface without a texture has no light, and a landscape doesn't use its light (so it can't be set, as
        // painting a light can't set it)
        [CreateProperty]
        public int PrimaryLightIndex
        {
            get
            {
                return Side.primary_texture.texture.IsEmptyShapeDescriptor() ? LightIndexField.NoLight : Side.primary_lightsource_index;
            }
            set
            {
                SetLight(value, (side, light) => side.SetLight(LevelEntity_Side.DataSources.Primary, light));
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
                SetLight(value, (side, light) => side.SetLight(LevelEntity_Side.DataSources.Secondary, light));
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
                SetLight(value, (side, light) => side.SetLight(LevelEntity_Side.DataSources.Transparent, light));
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

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Root.Q("control-panel-flags").BindEnabled(this, nameof(IsControlPanel));

            // From dark to full light
            var ambientDeltaSlider = Root.Q(nameof(AmbientDelta)).Q<SliderInt>();
            ambientDeltaSlider.lowValue = -cstypes.FIXED_ONE;
            ambientDeltaSlider.highValue = cstypes.FIXED_ONE;
        }

        private void ApplyPanelTexture()
        {
            if (Entity.PanelSetsPrimaryTexture)
            {
                Edit(side => side.ApplyPanelTexture());
            }
        }

        private void SetFlag(ushort flag, bool isSet)
        {
            Edit(side => side.NativeObject.flags = WithFlag(side.NativeObject.flags, flag, isSet));
        }
    }
}
