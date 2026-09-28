// Port of Aleph One: Source_Files/GameWorld/devices.cpp (control panel definitions)
//
// Not ported (the running game): initializing, updating and toggling control panels
// (initialize_control_panels_for_level, update_control_panels, change_device_state, update_action_key,
// untoggled_repair_switches_on_level, assume_correct_switch_position, try_and_toggle_control_panel,
// line_side_has_control_panel, find_action_key_target, ...), control_panel_settings, and MML parsing.
using static AlephOne.csmacros;
using static AlephOne.cstypes;
using static AlephOne.items;
using static AlephOne.map;
using static AlephOne.shape_descriptors;
using static AlephOne.SoundManagerEnums;
using static AlephOne.world;

namespace AlephOne
{
    public class control_panel_definition
    {
        public short _class;
        public ushort flags;

        public short collection;
        public short active_shape, inactive_shape;

        public short[] sounds = new short[devices.NUMBER_OF_CONTROL_PANEL_SOUNDS];
        public int sound_frequency; // _fixed

        public short item;

        public control_panel_definition(short _class, ushort flags, short collection, short active_shape, short inactive_shape,
            short[] sounds, int sound_frequency, short item)
        {
            this._class = _class;
            this.flags = flags;
            this.collection = collection;
            this.active_shape = active_shape;
            this.inactive_shape = inactive_shape;
            this.sounds = sounds;
            this.sound_frequency = sound_frequency;
            this.item = item;
        }
    }

    public static class devices
    {
        /* ---------- constants */

        public const int OXYGEN_RECHARGE_FREQUENCY = 0;
        public const int ENERGY_RECHARGE_FREQUENCY = 0;

        public const int MAXIMUM_PLATFORM_ACTIVATION_RANGE = (3 * WORLD_ONE);
        public const int MAXIMUM_CONTROL_ACTIVATION_RANGE = (WORLD_ONE + WORLD_ONE_HALF);
        public const int OBJECT_RADIUS = 50;

        public const int MINIMUM_RESAVE_TICKS = (2 * TICKS_PER_SECOND);

        /* ---------- structures */

        // control panel sounds
        public const short _activating_sound = 0;
        public const short _deactivating_sound = 1;
        public const short _unusuable_sound = 2;
        public const short NUMBER_OF_CONTROL_PANEL_SOUNDS = 3;

        /* ---------- globals */

        private static readonly control_panel_definition[] control_panel_definitions = new control_panel_definition[]
        {
            // _collection_walls1 -- LP: water
            new control_panel_definition(_panel_is_oxygen_refuel, 0, _collection_walls1, 2, 3, new short[] { _snd_oxygen_refuel, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_shield_refuel, 0, _collection_walls1, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_double_shield_refuel, 0, _collection_walls1, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE + FIXED_ONE / 8, NONE),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls1, 0, 1, new short[] { _snd_chip_insertion, NONE, NONE }, FIXED_ONE, _i_uplink_chip),
            new control_panel_definition(_panel_is_light_switch, 0, _collection_walls1, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_platform_switch, 0, _collection_walls1, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls1, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_pattern_buffer, 0, _collection_walls1, 4, 4, new short[] { _snd_pattern_buffer, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_computer_terminal, 0, _collection_walls1, 4, 4, new short[] { NONE, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls1, 1, 0, new short[] { _snd_destroy_control_panel, NONE, NONE }, FIXED_ONE, NONE),

            // _collection_walls2 -- LP: lava
            new control_panel_definition(_panel_is_shield_refuel, 0, _collection_walls2, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_double_shield_refuel, 0, _collection_walls2, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE + FIXED_ONE / 8, NONE),
            new control_panel_definition(_panel_is_triple_shield_refuel, 0, _collection_walls2, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE + FIXED_ONE / 4, NONE),
            new control_panel_definition(_panel_is_light_switch, 0, _collection_walls2, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_platform_switch, 0, _collection_walls2, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls2, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_pattern_buffer, 0, _collection_walls2, 4, 4, new short[] { _snd_pattern_buffer, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_computer_terminal, 0, _collection_walls2, 4, 4, new short[] { NONE, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_oxygen_refuel, 0, _collection_walls2, 2, 3, new short[] { _snd_oxygen_refuel, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls2, 0, 1, new short[] { _snd_chip_insertion, NONE, NONE }, FIXED_ONE, _i_uplink_chip),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls2, 1, 0, new short[] { _snd_destroy_control_panel, NONE, NONE }, FIXED_ONE, NONE),

            // _collection_walls3 -- LP: sewage
            new control_panel_definition(_panel_is_shield_refuel, 0, _collection_walls3, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_double_shield_refuel, 0, _collection_walls3, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE + FIXED_ONE / 8, NONE),
            new control_panel_definition(_panel_is_triple_shield_refuel, 0, _collection_walls3, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE + FIXED_ONE / 4, NONE),
            new control_panel_definition(_panel_is_light_switch, 0, _collection_walls3, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_platform_switch, 0, _collection_walls3, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls3, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_pattern_buffer, 0, _collection_walls3, 4, 4, new short[] { _snd_pattern_buffer, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_computer_terminal, 0, _collection_walls3, 4, 4, new short[] { NONE, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_oxygen_refuel, 0, _collection_walls3, 2, 3, new short[] { _snd_oxygen_refuel, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls3, 0, 1, new short[] { _snd_chip_insertion, NONE, NONE }, FIXED_ONE, _i_uplink_chip),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls3, 1, 0, new short[] { _snd_destroy_control_panel, NONE, NONE }, FIXED_ONE, NONE),

            // _collection_walls4 -- LP: really _collection_walls5 -- pfhor
            new control_panel_definition(_panel_is_shield_refuel, 0, _collection_walls5, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_double_shield_refuel, 0, _collection_walls5, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE + FIXED_ONE / 8, NONE),
            new control_panel_definition(_panel_is_triple_shield_refuel, 0, _collection_walls5, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE + FIXED_ONE / 4, NONE),
            new control_panel_definition(_panel_is_light_switch, 0, _collection_walls5, 0, 1, new short[] { _snd_pfhor_switch_on, _snd_pfhor_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_platform_switch, 0, _collection_walls5, 0, 1, new short[] { _snd_pfhor_switch_on, _snd_pfhor_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls5, 0, 1, new short[] { _snd_pfhor_switch_on, _snd_pfhor_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_pattern_buffer, 0, _collection_walls5, 4, 4, new short[] { _snd_pattern_buffer, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_computer_terminal, 0, _collection_walls5, 4, 4, new short[] { NONE, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_oxygen_refuel, 0, _collection_walls5, 2, 3, new short[] { _snd_oxygen_refuel, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls5, 0, 1, new short[] { _snd_chip_insertion, NONE, NONE }, FIXED_ONE, _i_uplink_chip),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls5, 1, 0, new short[] { _snd_destroy_control_panel, NONE, NONE }, FIXED_ONE, NONE),

            // LP addition: _collection_walls4 -- jjaro
            new control_panel_definition(_panel_is_shield_refuel, 0, _collection_walls4, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_double_shield_refuel, 0, _collection_walls4, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE + FIXED_ONE / 8, NONE),
            new control_panel_definition(_panel_is_triple_shield_refuel, 0, _collection_walls4, 2, 3, new short[] { _snd_energy_refuel, NONE, NONE }, FIXED_ONE + FIXED_ONE / 4, NONE),
            new control_panel_definition(_panel_is_light_switch, 0, _collection_walls4, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_platform_switch, 0, _collection_walls4, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls4, 0, 1, new short[] { _snd_switch_on, _snd_switch_off, _snd_cant_toggle_switch }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_pattern_buffer, 0, _collection_walls4, 4, 4, new short[] { _snd_pattern_buffer, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_computer_terminal, 0, _collection_walls4, 4, 4, new short[] { NONE, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_oxygen_refuel, 0, _collection_walls4, 2, 3, new short[] { _snd_oxygen_refuel, NONE, NONE }, FIXED_ONE, NONE),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls4, 0, 1, new short[] { _snd_chip_insertion, NONE, NONE }, FIXED_ONE, _i_uplink_chip),
            new control_panel_definition(_panel_is_tag_switch, 0, _collection_walls4, 1, 0, new short[] { _snd_destroy_control_panel, NONE, NONE }, FIXED_ONE, NONE),
        };

        public static readonly int NUMBER_OF_CONTROL_PANEL_DEFINITIONS = control_panel_definitions.Length;

        /* ---------- code */

        public static control_panel_definition get_control_panel_definition(short control_panel_type)
        {
            return GetMemberWithBounds(control_panel_definitions, control_panel_type, NUMBER_OF_CONTROL_PANEL_DEFINITIONS);
        }

        public static short get_panel_class(short panel_type)
        {
            control_panel_definition definition = get_control_panel_definition(panel_type);

            return definition._class;
        }

        public static void set_control_panel_texture(side_data side)
        {
            control_panel_definition definition = get_control_panel_definition(side.control_panel_type);
            // LP change: idiot-proofing
            if (definition == null) return;

            side.primary_texture.texture = BUILD_DESCRIPTOR(definition.collection,
                GET_CONTROL_PANEL_STATUS(side) ? definition.active_shape : definition.inactive_shape);
        }
    }
}
