// Port of Aleph One: Source_Files/GameWorld/platforms.h, platforms.cpp, platform_definitions.h (map data)
//
// Not ported (the running platform state machine): update_platforms, platform_was_entered,
// try_and_change_platform_state, try_and_change_tagged_platform_states, set_platform_state,
// monster_can_enter/leave_platform, player_touch_platform_state, platform_is_legal_player_target,
// platform_is_at_initial_state, adjust_platform_for_media, and MML parsing. Which sound a platform plays
// (play_platform_sound) is ported, as get_platform_sound, but not playing it.
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csalerts;
using static AlephOne.csmacros;
using static AlephOne.cstypes;
using static AlephOne.editor;
using static AlephOne.map;
using static AlephOne.map_constructors;
using static AlephOne.Packing;
using static AlephOne.SoundManagerEnums;
using static AlephOne.world;

namespace AlephOne
{
    public class endpoint_owner_data
    {
        public short first_polygon_index, polygon_index_count;
        public short first_line_index, line_index_count;

        public endpoint_owner_data Clone() { return (endpoint_owner_data) MemberwiseClone(); }
    }

    public class static_platform_data /* size platform-dependant */
    {
        public short type;
        public short speed, delay;
        public short maximum_height, minimum_height; /* if NONE then calculated in some reasonable way */ // world_distance

        public uint static_flags;

        public short polygon_index;

        public short tag;

        public short[] unused = new short[7];

        public static_platform_data() { }

        // Aggregate initializer, as used by the platform definitions
        public static_platform_data(short type, short speed, short delay, short maximum_height, short minimum_height, uint static_flags)
        {
            this.type = type;
            this.speed = speed;
            this.delay = delay;
            this.maximum_height = maximum_height;
            this.minimum_height = minimum_height;
            this.static_flags = static_flags;
        }

        public static_platform_data Clone()
        {
            var copy = (static_platform_data) MemberwiseClone();
            copy.unused = (short[]) unused.Clone();
            return copy;
        }
    }

    public class platform_data /* 140 bytes */
    {
        public short type;
        public uint static_flags;
        public short speed, delay;
        public short minimum_floor_height, maximum_floor_height; // world_distance
        public short minimum_ceiling_height, maximum_ceiling_height; // world_distance

        public short polygon_index;

        public ushort dynamic_flags;
        public short floor_height, ceiling_height; // world_distance
        public short ticks_until_restart; /* if we're not moving but are active, this is our delay until we move again */

        public endpoint_owner_data[] endpoint_owners = platforms.new_endpoint_owners();

        public short parent_platform_index; /* the platform_index which activated us, if any */

        public short tag;

        public short[] unused = new short[22];

        public platform_data Clone()
        {
            var copy = (platform_data) MemberwiseClone();
            copy.endpoint_owners = new endpoint_owner_data[endpoint_owners.Length];
            for (int i = 0; i < endpoint_owners.Length; i++) copy.endpoint_owners[i] = endpoint_owners[i].Clone();
            copy.unused = (short[]) unused.Clone();
            return copy;
        }
    }

    [NoAutoStaticsCleanup]
    public static class platforms
    {
        /* ---------- constants */

        // #define MAXIMUM_PLATFORMS_PER_MAP 64

        /* platform types */
        public const short _platform_is_spht_door = 0;
        public const short _platform_is_spht_split_door = 1;
        public const short _platform_is_locked_spht_door = 2;
        public const short _platform_is_spht_platform = 3;
        public const short _platform_is_noisy_spht_platform = 4;
        public const short _platform_is_heavy_spht_door = 5;
        public const short _platform_is_pfhor_door = 6;
        public const short _platform_is_heavy_spht_platform = 7;
        public const short _platform_is_pfhor_platform = 8;
        public const short NUMBER_OF_PLATFORM_TYPES = 9;

        /* platform speeds */
        public const short _very_slow_platform = WORLD_ONE / (4 * TICKS_PER_SECOND);
        public const short _slow_platform = WORLD_ONE / (2 * TICKS_PER_SECOND);
        public const short _fast_platform = 2 * _slow_platform;
        public const short _very_fast_platform = 3 * _slow_platform;
        public const short _blindingly_fast_platform = 4 * _slow_platform;

        /* platform delays */
        public const short _no_delay_platform = 0; /* use carefully; difficult to reincarnate on */
        public const short _short_delay_platform = TICKS_PER_SECOND;
        public const short _long_delay_platform = 2 * TICKS_PER_SECOND;
        public const short _very_long_delay_platform = 4 * TICKS_PER_SECOND;
        public const short _extremely_long_delay_platform = 8 * TICKS_PER_SECOND;

        /* static platform flags */
        public const int _platform_is_initially_active = 0; /* otherwise inactive */
        public const int _platform_is_initially_extended = 1; /* high for floor platforms, low for ceiling platforms, closed for two-way platforms */
        public const int _platform_deactivates_at_each_level = 2; /* this platform will deactivate each time it reaches a discrete level */
        public const int _platform_deactivates_at_initial_level = 3; /* this platform will deactivate upon returning to its original position */
        public const int _platform_activates_adjacent_platforms_when_deactivating = 4; /* when deactivating, this platform activates adjacent platforms */
        public const int _platform_extends_floor_to_ceiling = 5; /* i.e., there is no empty space when the platform is fully extended */
        public const int _platform_comes_from_floor = 6; /* platform rises from floor */
        public const int _platform_comes_from_ceiling = 7; /* platform lowers from ceiling */
        public const int _platform_causes_damage = 8; /* when obstructed by monsters, this platform causes damage */
        public const int _platform_does_not_activate_parent = 9; /* does not reactive it's parent (i.e., that platform which activated it) */
        public const int _platform_activates_only_once = 10; /* cannot be activated a second time */
        public const int _platform_activates_light = 11; /* activates floor and ceiling lightsources while activating */
        public const int _platform_deactivates_light = 12; /* deactivates floor and ceiling lightsources while deactivating */
        public const int _platform_is_player_controllable = 13; /* i.e., door: players can use action key to change the state and/or direction of this platform */
        public const int _platform_is_monster_controllable = 14; /* i.e., door: monsters can expect to be able to move this platform even if inactive */
        public const int _platform_reverses_direction_when_obstructed = 15;
        public const int _platform_cannot_be_externally_deactivated = 16; /* when active, can only be deactivated by itself */
        public const int _platform_uses_native_polygon_heights = 17; /* complicated interpretation; uses native polygon heights during automatic min,max calculation */
        public const int _platform_delays_before_activation = 18; /* whether or not the platform begins with the maximum delay before moving */
        public const int _platform_activates_adjacent_platforms_when_activating = 19;
        public const int _platform_deactivates_adjacent_platforms_when_activating = 20;
        public const int _platform_deactivates_adjacent_platforms_when_deactivating = 21;
        public const int _platform_contracts_slower = 22;
        public const int _platform_activates_adjacent_platforms_at_each_level = 23;
        public const int _platform_is_locked = 24;
        public const int _platform_is_secret = 25;
        public const int _platform_is_door = 26;
        public const int _platform_floods_m1 = 27;
        public const int NUMBER_OF_STATIC_PLATFORM_FLAGS = 28; /* <=32 */

        // The macros take anything with static_flags (static_platform_data or platform_data)
        public static bool PLATFORM_IS_INITIALLY_ACTIVE(uint static_flags) { return TEST_FLAG32(static_flags, _platform_is_initially_active); }
        public static bool PLATFORM_IS_INITIALLY_EXTENDED(uint static_flags) { return TEST_FLAG32(static_flags, _platform_is_initially_extended); }
        public static bool PLATFORM_IS_INITIALLY_CONTRACTED(uint static_flags) { return (!PLATFORM_IS_INITIALLY_EXTENDED(static_flags)); }
        public static bool PLATFORM_EXTENDS_FLOOR_TO_CEILING(uint static_flags) { return TEST_FLAG32(static_flags, _platform_extends_floor_to_ceiling); }
        public static bool PLATFORM_COMES_FROM_FLOOR(uint static_flags) { return TEST_FLAG32(static_flags, _platform_comes_from_floor); }
        public static bool PLATFORM_COMES_FROM_CEILING(uint static_flags) { return TEST_FLAG32(static_flags, _platform_comes_from_ceiling); }
        public static bool PLATFORM_GOES_BOTH_WAYS(uint static_flags) { return (PLATFORM_COMES_FROM_FLOOR(static_flags) && PLATFORM_COMES_FROM_CEILING(static_flags)); }
        public static bool PLATFORM_CAUSES_DAMAGE(uint static_flags) { return TEST_FLAG32(static_flags, _platform_causes_damage); }
        public static bool PLATFORM_REVERSES_DIRECTION_WHEN_OBSTRUCTED(uint static_flags) { return TEST_FLAG32(static_flags, _platform_reverses_direction_when_obstructed); }
        public static bool PLATFORM_ACTIVATES_ONLY_ONCE(uint static_flags) { return TEST_FLAG32(static_flags, _platform_activates_only_once); }
        public static bool PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_WHEN_DEACTIVATING(uint static_flags) { return TEST_FLAG32(static_flags, _platform_activates_adjacent_platforms_when_deactivating); }
        public static bool PLATFORM_ACTIVATES_LIGHT(uint static_flags) { return TEST_FLAG32(static_flags, _platform_activates_light); }
        public static bool PLATFORM_DEACTIVATES_LIGHT(uint static_flags) { return TEST_FLAG32(static_flags, _platform_deactivates_light); }
        public static bool PLATFORM_DEACTIVATES_AT_EACH_LEVEL(uint static_flags) { return TEST_FLAG32(static_flags, _platform_deactivates_at_each_level); }
        public static bool PLATFORM_DEACTIVATES_AT_INITIAL_LEVEL(uint static_flags) { return TEST_FLAG32(static_flags, _platform_deactivates_at_initial_level); }
        public static bool PLATFORM_DOES_NOT_ACTIVATE_PARENT(uint static_flags) { return TEST_FLAG32(static_flags, _platform_does_not_activate_parent); }
        public static bool PLATFORM_IS_PLAYER_CONTROLLABLE(uint static_flags) { return TEST_FLAG32(static_flags, _platform_is_player_controllable); }
        public static bool PLATFORM_IS_MONSTER_CONTROLLABLE(uint static_flags) { return TEST_FLAG32(static_flags, _platform_is_monster_controllable); }
        public static bool PLATFORM_CANNOT_BE_EXTERNALLY_DEACTIVATED(uint static_flags) { return TEST_FLAG32(static_flags, _platform_cannot_be_externally_deactivated); }
        public static bool PLATFORM_USES_NATIVE_POLYGON_HEIGHTS(uint static_flags) { return TEST_FLAG32(static_flags, _platform_uses_native_polygon_heights); }
        public static bool PLATFORM_DELAYS_BEFORE_ACTIVATION(uint static_flags) { return TEST_FLAG32(static_flags, _platform_delays_before_activation); }
        public static bool PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_WHEN_ACTIVATING(uint static_flags) { return TEST_FLAG32(static_flags, _platform_activates_adjacent_platforms_when_activating); }
        public static bool PLATFORM_DEACTIVATES_ADJACENT_PLATFORMS_WHEN_ACTIVATING(uint static_flags) { return TEST_FLAG32(static_flags, _platform_deactivates_adjacent_platforms_when_activating); }
        public static bool PLATFORM_DEACTIVATES_ADJACENT_PLATFORMS_WHEN_DEACTIVATING(uint static_flags) { return TEST_FLAG32(static_flags, _platform_deactivates_adjacent_platforms_when_deactivating); }
        public static bool PLATFORM_CONTRACTS_SLOWER(uint static_flags) { return TEST_FLAG32(static_flags, _platform_contracts_slower); }
        public static bool PLATFORM_ACTIVATES_ADJACENT_PLATFORMS_AT_EACH_LEVEL(uint static_flags) { return TEST_FLAG32(static_flags, _platform_activates_adjacent_platforms_at_each_level); }
        public static bool PLATFORM_IS_LOCKED(uint static_flags) { return TEST_FLAG32(static_flags, _platform_is_locked); }
        public static bool PLATFORM_IS_SECRET(uint static_flags) { return TEST_FLAG32(static_flags, _platform_is_secret); }
        public static bool PLATFORM_IS_DOOR(uint static_flags) { return TEST_FLAG32(static_flags, _platform_is_door); }
        public static bool PLATFORM_FLOODS_M1(uint static_flags) { return TEST_FLAG32(static_flags, _platform_floods_m1); }

        public static bool PLATFORM_IS_INITIALLY_ACTIVE(static_platform_data p) { return PLATFORM_IS_INITIALLY_ACTIVE(p.static_flags); }
        public static bool PLATFORM_IS_INITIALLY_EXTENDED(static_platform_data p) { return PLATFORM_IS_INITIALLY_EXTENDED(p.static_flags); }
        public static bool PLATFORM_COMES_FROM_FLOOR(static_platform_data p) { return PLATFORM_COMES_FROM_FLOOR(p.static_flags); }
        public static bool PLATFORM_COMES_FROM_CEILING(static_platform_data p) { return PLATFORM_COMES_FROM_CEILING(p.static_flags); }
        public static bool PLATFORM_GOES_BOTH_WAYS(static_platform_data p) { return PLATFORM_GOES_BOTH_WAYS(p.static_flags); }
        public static bool PLATFORM_IS_LOCKED(static_platform_data p) { return PLATFORM_IS_LOCKED(p.static_flags); }
        public static bool PLATFORM_IS_DOOR(static_platform_data p) { return PLATFORM_IS_DOOR(p.static_flags); }
        public static bool PLATFORM_FLOODS_M1(static_platform_data p) { return PLATFORM_FLOODS_M1(p.static_flags); }

        public static bool PLATFORM_IS_INITIALLY_ACTIVE(platform_data p) { return PLATFORM_IS_INITIALLY_ACTIVE(p.static_flags); }
        public static bool PLATFORM_IS_INITIALLY_EXTENDED(platform_data p) { return PLATFORM_IS_INITIALLY_EXTENDED(p.static_flags); }
        public static bool PLATFORM_EXTENDS_FLOOR_TO_CEILING(platform_data p) { return PLATFORM_EXTENDS_FLOOR_TO_CEILING(p.static_flags); }
        public static bool PLATFORM_COMES_FROM_FLOOR(platform_data p) { return PLATFORM_COMES_FROM_FLOOR(p.static_flags); }
        public static bool PLATFORM_COMES_FROM_CEILING(platform_data p) { return PLATFORM_COMES_FROM_CEILING(p.static_flags); }
        public static bool PLATFORM_GOES_BOTH_WAYS(platform_data p) { return PLATFORM_GOES_BOTH_WAYS(p.static_flags); }
        public static bool PLATFORM_USES_NATIVE_POLYGON_HEIGHTS(platform_data p) { return PLATFORM_USES_NATIVE_POLYGON_HEIGHTS(p.static_flags); }
        public static bool PLATFORM_FLOODS_M1(platform_data p) { return PLATFORM_FLOODS_M1(p.static_flags); }

        public static void SET_PLATFORM_IS_INITIALLY_ACTIVE(static_platform_data p, bool v) { p.static_flags = SET_FLAG32(p.static_flags, _platform_is_initially_active, v); }
        public static void SET_PLATFORM_IS_INITIALLY_EXTENDED(static_platform_data p, bool v) { p.static_flags = SET_FLAG32(p.static_flags, _platform_is_initially_extended, v); }
        public static void SET_PLATFORM_EXTENDS_FLOOR_TO_CEILING(static_platform_data p, bool v) { p.static_flags = SET_FLAG32(p.static_flags, _platform_extends_floor_to_ceiling, v); }
        public static void SET_PLATFORM_COMES_FROM_FLOOR(static_platform_data p, bool v) { p.static_flags = SET_FLAG32(p.static_flags, _platform_comes_from_floor, v); }
        public static void SET_PLATFORM_COMES_FROM_CEILING(static_platform_data p, bool v) { p.static_flags = SET_FLAG32(p.static_flags, _platform_comes_from_ceiling, v); }
        public static void SET_PLATFORM_USES_NATIVE_POLYGON_HEIGHTS(static_platform_data p, bool v) { p.static_flags = SET_FLAG32(p.static_flags, _platform_uses_native_polygon_heights, v); }
        public static void SET_PLATFORM_IS_LOCKED(static_platform_data p, bool v) { p.static_flags = SET_FLAG32(p.static_flags, _platform_is_locked, v); }
        public static void SET_PLATFORM_IS_SECRET(static_platform_data p, bool v) { p.static_flags = SET_FLAG32(p.static_flags, _platform_is_secret, v); }
        public static void SET_PLATFORM_IS_DOOR(static_platform_data p, bool v) { p.static_flags = SET_FLAG32(p.static_flags, _platform_is_door, v); }
        public static void SET_PLATFORM_FLOODS_M1(static_platform_data p, bool v) { p.static_flags = SET_FLAG32(p.static_flags, _platform_floods_m1, v); }

        /* dynamic platform flags */
        public const int _platform_is_active = 0; /* otherwise inactive */
        public const int _platform_is_extending = 1; /* otherwise contracting; could be waiting between levels */
        public const int _platform_is_moving = 2; /* otherwise at rest (waiting between levels) */
        public const int _platform_has_been_activated = 3; /* in case we can only be activated once */
        public const int _platform_was_moving = 4; /* the platform moved unobstructed last tick */
        public const int _platform_is_fully_extended = 5;
        public const int _platform_is_fully_contracted = 6;
        public const int _platform_was_just_activated_or_deactivated = 7;
        public const int _platform_floor_below_media = 8;
        public const int _platform_ceiling_below_media = 9;
        public const int NUMBER_OF_DYNAMIC_PLATFORM_FLAGS = 10; /* <=16 */

        public static bool PLATFORM_IS_ACTIVE(platform_data p) { return TEST_FLAG16((p).dynamic_flags, _platform_is_active); }
        public static bool PLATFORM_IS_EXTENDING(platform_data p) { return TEST_FLAG16((p).dynamic_flags, _platform_is_extending); }
        public static bool PLATFORM_IS_CONTRACTING(platform_data p) { return (!PLATFORM_IS_EXTENDING(p)); }
        public static bool PLATFORM_IS_MOVING(platform_data p) { return TEST_FLAG16((p).dynamic_flags, _platform_is_moving); }
        public static bool PLATFORM_HAS_BEEN_ACTIVATED(platform_data p) { return TEST_FLAG16((p).dynamic_flags, _platform_has_been_activated); }
        public static bool PLATFORM_WAS_MOVING(platform_data p) { return TEST_FLAG16((p).dynamic_flags, _platform_was_moving); }
        public static bool PLATFORM_IS_FULLY_EXTENDED(platform_data p) { return TEST_FLAG16((p).dynamic_flags, _platform_is_fully_extended); }
        public static bool PLATFORM_IS_FULLY_CONTRACTED(platform_data p) { return TEST_FLAG16((p).dynamic_flags, _platform_is_fully_contracted); }
        public static bool PLATFORM_WAS_JUST_ACTIVATED_OR_DEACTIVATED(platform_data p) { return TEST_FLAG16((p).dynamic_flags, _platform_was_just_activated_or_deactivated); }
        public static bool PLATFORM_FLOOR_BELOW_MEDIA(platform_data p) { return TEST_FLAG16((p).dynamic_flags, _platform_floor_below_media); }
        public static bool PLATFORM_CEILING_BELOW_MEDIA(platform_data p) { return TEST_FLAG16((p).dynamic_flags, _platform_ceiling_below_media); }

        public static void SET_PLATFORM_IS_ACTIVE(platform_data p, bool v) { p.dynamic_flags = SET_FLAG16((p).dynamic_flags, _platform_is_active, (v)); }
        public static void SET_PLATFORM_IS_EXTENDING(platform_data p) { p.dynamic_flags = SET_FLAG16((p).dynamic_flags, _platform_is_extending, true); }
        public static void SET_PLATFORM_IS_CONTRACTING(platform_data p) { p.dynamic_flags = SET_FLAG16((p).dynamic_flags, _platform_is_extending, false); }
        public static void SET_PLATFORM_IS_MOVING(platform_data p, bool v) { p.dynamic_flags = SET_FLAG16((p).dynamic_flags, _platform_is_moving, (v)); }
        public static void SET_PLATFORM_HAS_BEEN_ACTIVATED(platform_data p) { p.dynamic_flags = SET_FLAG16((p).dynamic_flags, _platform_has_been_activated, true); }
        public static void SET_PLATFORM_IS_FULLY_EXTENDED(platform_data p) { p.dynamic_flags = SET_FLAG16((p).dynamic_flags, _platform_is_fully_extended, true); }
        public static void SET_PLATFORM_IS_FULLY_CONTRACTED(platform_data p) { p.dynamic_flags = SET_FLAG16((p).dynamic_flags, _platform_is_fully_contracted, true); }

        // using "fully contracted" is close enough to act like Marathon... I hope
        public static bool PLATFORM_IS_FLOODED(platform_data p) { return (PLATFORM_FLOODS_M1(p) && PLATFORM_IS_FULLY_CONTRACTED(p)); }

        public const int SIZEOF_static_platform_data = 32;

        public const int SIZEOF_platform_data = 140;

        internal static endpoint_owner_data[] new_endpoint_owners()
        {
            var owners = new endpoint_owner_data[MAXIMUM_VERTICES_PER_POLYGON];
            for (int i = 0; i < owners.Length; i++) owners[i] = new endpoint_owner_data();
            return owners;
        }

        /* ---------- platform_definitions.h */

        /* sound codes for play_platform_sound() */
        public const short _stopping_sound = 0;
        public const short _starting_sound = 1;
        public const short _obstructed_sound = 2;
        public const short _uncontrollable_sound = 3;

        // Not ported: key_item_index and damage (the running game's)
        public class platform_definition
        {
            /* sounds; specific sounds are played if they can be (i.e., ...at_bottom) otherwise the
                general sound is played */
            public short starting_extension, starting_contraction;
            public short stopping_extension, stopping_contraction;
            public short obstructed_sound, uncontrollable_sound;
            public short moving_sound;

            public static_platform_data defaults;
        }

        private static readonly platform_definition[] platform_definitions = new platform_definition[NUMBER_OF_PLATFORM_TYPES]
        {
            new platform_definition // _platform_is_spht_door
            {
                starting_extension = _snd_spht_door_opening, starting_contraction = _snd_spht_door_closing,
                stopping_extension = NONE, stopping_contraction = NONE,
                obstructed_sound = _snd_spht_door_obstructed, uncontrollable_sound = _snd_spht_door_obstructed,
                moving_sound = _ambient_snd_spht_door,
                defaults = new static_platform_data(
                    _platform_is_spht_door, _fast_platform, _very_long_delay_platform, NONE, NONE,
                    FLAG(_platform_deactivates_at_initial_level) | FLAG(_platform_extends_floor_to_ceiling) |
                    FLAG(_platform_is_player_controllable) | FLAG(_platform_is_monster_controllable) |
                    FLAG(_platform_reverses_direction_when_obstructed) | FLAG(_platform_is_initially_extended) |
                    FLAG(_platform_comes_from_ceiling) | FLAG(_platform_is_door))
            },
            new platform_definition // _platform_is_split_spht_door
            {
                starting_extension = _snd_spht_door_opening, starting_contraction = _snd_spht_door_closing,
                stopping_extension = NONE, stopping_contraction = NONE,
                obstructed_sound = _snd_spht_door_obstructed, uncontrollable_sound = _snd_spht_door_obstructed,
                moving_sound = _ambient_snd_spht_door,
                defaults = new static_platform_data(
                    _platform_is_spht_split_door, _slow_platform, _very_long_delay_platform, NONE, NONE,
                    FLAG(_platform_deactivates_at_initial_level) | FLAG(_platform_extends_floor_to_ceiling) |
                    FLAG(_platform_is_player_controllable) | FLAG(_platform_is_monster_controllable) |
                    FLAG(_platform_reverses_direction_when_obstructed) | FLAG(_platform_comes_from_floor) |
                    FLAG(_platform_comes_from_ceiling) | FLAG(_platform_is_initially_extended) |
                    FLAG(_platform_is_door))
            },
            new platform_definition // _platform_is_locked_spht_door
            {
                starting_extension = _snd_spht_door_opening, starting_contraction = _snd_spht_door_closing,
                stopping_extension = NONE, stopping_contraction = NONE,
                obstructed_sound = _snd_spht_door_obstructed, uncontrollable_sound = _snd_spht_door_obstructed,
                moving_sound = _ambient_snd_spht_door,
                defaults = new static_platform_data(
                    _platform_is_locked_spht_door, _slow_platform, _very_long_delay_platform, NONE, NONE,
                    FLAG(_platform_deactivates_at_initial_level) | FLAG(_platform_extends_floor_to_ceiling) |
                    FLAG(_platform_is_player_controllable) | FLAG(_platform_is_monster_controllable) |
                    FLAG(_platform_reverses_direction_when_obstructed) | FLAG(_platform_comes_from_floor) |
                    FLAG(_platform_comes_from_ceiling) | FLAG(_platform_is_initially_extended) |
                    FLAG(_platform_is_door))
            },
            new platform_definition // _platform_is_spht_platform
            {
                starting_extension = NONE, starting_contraction = NONE,
                stopping_extension = NONE, stopping_contraction = NONE,
                obstructed_sound = NONE, uncontrollable_sound = NONE,
                moving_sound = NONE,
                defaults = new static_platform_data(
                    _platform_is_spht_platform, _slow_platform, _long_delay_platform, NONE, NONE,
                    FLAG(_platform_is_initially_active) | FLAG(_platform_is_initially_extended) | FLAG(_platform_comes_from_floor) |
                    FLAG(_platform_reverses_direction_when_obstructed))
            },
            new platform_definition // _platform_is_noisy_spht_platform
            {
                starting_extension = _snd_spht_platform_starting, starting_contraction = _snd_spht_platform_starting,
                stopping_extension = _snd_spht_platform_stopping, stopping_contraction = _snd_spht_platform_stopping,
                obstructed_sound = _snd_spht_platform_stopping, uncontrollable_sound = NONE,
                moving_sound = _ambient_snd_spht_platform,
                defaults = new static_platform_data(
                    _platform_is_noisy_spht_platform, _slow_platform, _long_delay_platform, NONE, NONE,
                    FLAG(_platform_is_initially_active) | FLAG(_platform_is_initially_extended) | FLAG(_platform_comes_from_floor) |
                    FLAG(_platform_reverses_direction_when_obstructed))
            },
            new platform_definition // _platform_is_heavy_spht_door
            {
                starting_extension = _snd_heavy_spht_door_closing, starting_contraction = _snd_heavy_spht_door_opening,
                stopping_extension = _snd_heavy_spht_door_closed, stopping_contraction = _snd_heavy_spht_door_open,
                obstructed_sound = _snd_heavy_spht_door_obstructed, uncontrollable_sound = _snd_heavy_spht_door_obstructed,
                moving_sound = _ambient_snd_heavy_spht_door,
                defaults = new static_platform_data(
                    _platform_is_heavy_spht_door, _slow_platform, _very_long_delay_platform, NONE, NONE,
                    FLAG(_platform_deactivates_at_initial_level) | FLAG(_platform_extends_floor_to_ceiling) |
                    FLAG(_platform_is_player_controllable) | FLAG(_platform_is_monster_controllable) |
                    FLAG(_platform_reverses_direction_when_obstructed) | FLAG(_platform_comes_from_ceiling) |
                    FLAG(_platform_is_initially_extended) | FLAG(_platform_is_door))
            },
            new platform_definition // pfhor door
            {
                starting_extension = _snd_pfhor_door_opening, starting_contraction = _snd_pfhor_door_closing,
                stopping_extension = NONE, stopping_contraction = NONE,
                obstructed_sound = _snd_pfhor_door_obstructed, uncontrollable_sound = _snd_pfhor_door_obstructed,
                moving_sound = _ambient_snd_pfhor_door,
                defaults = new static_platform_data(
                    _platform_is_pfhor_door, _fast_platform, _very_long_delay_platform, NONE, NONE,
                    FLAG(_platform_deactivates_at_initial_level) | FLAG(_platform_extends_floor_to_ceiling) |
                    FLAG(_platform_is_player_controllable) | FLAG(_platform_is_monster_controllable) |
                    FLAG(_platform_reverses_direction_when_obstructed) | FLAG(_platform_is_initially_extended) |
                    FLAG(_platform_comes_from_ceiling) | FLAG(_platform_is_door))
            },
            new platform_definition // _platform_is_heavy_spht_platform
            {
                starting_extension = _snd_heavy_spht_platform_starting, starting_contraction = _snd_heavy_spht_platform_starting,
                stopping_extension = _snd_heavy_spht_platform_stopping, stopping_contraction = _snd_heavy_spht_platform_stopping,
                obstructed_sound = _snd_heavy_spht_platform_stopping, uncontrollable_sound = NONE,
                moving_sound = _ambient_snd_heavy_spht_platform,
                defaults = new static_platform_data(
                    _platform_is_heavy_spht_platform, _slow_platform, _long_delay_platform, NONE, NONE,
                    FLAG(_platform_is_initially_active) | FLAG(_platform_is_initially_extended) |
                    FLAG(_platform_comes_from_floor) | FLAG(_platform_reverses_direction_when_obstructed))
            },
            new platform_definition // pfhor platform
            {
                starting_extension = _snd_pfhor_platform_starting, starting_contraction = _snd_pfhor_platform_starting,
                stopping_extension = _snd_pfhor_platform_stopping, stopping_contraction = _snd_pfhor_platform_stopping,
                obstructed_sound = _snd_pfhor_platform_stopping, uncontrollable_sound = NONE,
                moving_sound = _ambient_snd_pfhor_platform,
                defaults = new static_platform_data(
                    _platform_is_pfhor_platform, _slow_platform, _long_delay_platform, NONE, NONE,
                    FLAG(_platform_is_initially_active) | FLAG(_platform_is_initially_extended) | FLAG(_platform_comes_from_floor) |
                    FLAG(_platform_reverses_direction_when_obstructed))
            },
        };

        /* ---------- code */

        public static platform_data get_platform_data(MapLevel level, short platform_index)
        {
            platform_data platform = GetMemberWithBounds(level.PlatformList, platform_index, level.PlatformList.Count);

            vassert(platform != null, "platform index #{0} is out of range", platform_index);

            return platform;
        }

        private static platform_definition get_platform_definition(short type)
        {
            return GetMemberWithBounds(platform_definitions, type, NUMBER_OF_PLATFORM_TYPES);
        }

        public static short get_platform_moving_sound(MapLevel level, short platform_index)
        {
            platform_data platform = get_platform_data(level, platform_index);
            platform_definition definition = get_platform_definition(platform.type);
            if (definition == null) return NONE;

            return definition.moving_sound;
        }

        // play_platform_sound's choice of sound. ForgePlus: with PLATFORM_IS_EXTENDING and PLATFORM_IS_FULLY_CONTRACTED
        // passed in, as the running game's dynamic flags aren't kept
        public static short get_platform_sound(MapLevel level, short platform_index, short type, bool is_extending, bool is_fully_contracted)
        {
            platform_data platform = get_platform_data(level, platform_index);
            platform_definition definition = get_platform_definition(platform.type);
            if (definition == null) return NONE;
            short sound_code;

            switch (type)
            {
                case _obstructed_sound:
                    sound_code = definition.obstructed_sound;
                    break;

                case _uncontrollable_sound:
                    sound_code = definition.uncontrollable_sound;
                    break;

                case _starting_sound:
                    sound_code = is_extending ? definition.starting_extension : definition.starting_contraction;
                    break;
                case _stopping_sound:
                    sound_code = is_fully_contracted ? definition.stopping_contraction : definition.stopping_extension;
                    break;

                default:
                    assert(false);
                    sound_code = NONE;
                    break;
            }

            return sound_code;
        }

        // maximum_platforms_per_map is MAXIMUM_PLATFORMS_PER_MAP, which scan_and_add_platforms() sets to the
        // number of static platforms
        public static short new_platform(MapLevel level, static_platform_data data, short polygon_index, int maximum_platforms_per_map)
        {
            short platform_index = NONE;
            platform_data platform;

            assert(NUMBER_OF_DYNAMIC_PLATFORM_FLAGS <= 16);
            assert(NUMBER_OF_STATIC_PLATFORM_FLAGS <= 32);
            // LP: OK for a platform to be a do-nothing platform
            // assert(data->static_flags&(FLAG(_platform_comes_from_floor)|FLAG(_platform_comes_from_ceiling)));

            if (level.PlatformList.Count < maximum_platforms_per_map)
            {
                platform_index = (short) level.PlatformList.Count;
                platform = new platform_data();
                level.PlatformList.Add(platform);

                initialize_platform(level, platform_index, data, polygon_index);
            }

            return platform_index;
        }

        // ForgePlus: the rest of new_platform(), so an edited platform can be initialized again from its static data
        public static void initialize_platform(MapLevel level, short platform_index, static_platform_data data, short polygon_index)
        {
            platform_data platform = level.PlatformList[platform_index];
            polygon_data polygon = get_polygon_data(level, polygon_index);
            short i;

            /* remember the platform_index in the polygon's .permutation field */
            polygon.permutation = platform_index;
            polygon.type = _polygon_is_platform;

            /* initialize the platform */
            platform.type = data.type;
            platform.static_flags = data.static_flags;
            platform.tag = data.tag;
            platform.speed = data.speed;
            platform.delay = data.delay;
            platform.polygon_index = polygon_index;
            platform.parent_platform_index = NONE;
            calculate_platform_extrema(level, platform_index, data.minimum_height, data.maximum_height);

            /* stuff in the correct defaults; if the platform is initially active it begins moving
                immediately */
            platform.dynamic_flags = 0;
            platform.floor_height = polygon.floor_height;
            platform.ceiling_height = polygon.ceiling_height;
            if (PLATFORM_IS_INITIALLY_ACTIVE(platform))
            {
                SET_PLATFORM_IS_ACTIVE(platform, true);
                SET_PLATFORM_HAS_BEEN_ACTIVATED(platform);
                SET_PLATFORM_IS_MOVING(platform, true);
            }
            if (PLATFORM_IS_INITIALLY_EXTENDED(platform))
            {
                if (PLATFORM_COMES_FROM_FLOOR(platform)) platform.floor_height = platform.maximum_floor_height;
                if (PLATFORM_COMES_FROM_CEILING(platform)) platform.ceiling_height = platform.minimum_ceiling_height;
                SET_PLATFORM_IS_CONTRACTING(platform);
                SET_PLATFORM_IS_FULLY_EXTENDED(platform);
            }
            else
            {
                if (PLATFORM_COMES_FROM_FLOOR(platform)) platform.floor_height = platform.minimum_floor_height;
                if (PLATFORM_COMES_FROM_CEILING(platform)) platform.ceiling_height = platform.maximum_ceiling_height;
                SET_PLATFORM_IS_EXTENDING(platform);
                SET_PLATFORM_IS_FULLY_CONTRACTED(platform);
            }

            /* remember what polygons and lines are adjacent to the endpoints of the platform
                polygon so we can quickly recalculate heights later */
            for (i = 0; i < polygon.vertex_count; ++i)
            {
                calculate_endpoint_polygon_owners(level, polygon.endpoint_indexes[i], out platform.endpoint_owners[i].first_polygon_index,
                    out platform.endpoint_owners[i].polygon_index_count);
                calculate_endpoint_line_owners(level, polygon.endpoint_indexes[i], out platform.endpoint_owners[i].first_line_index,
                    out platform.endpoint_owners[i].line_index_count);
            }

            // ForgePlus: the polygon keeps its native heights, which export_level() would restore
            // polygon->floor_height= platform->floor_height;
            // polygon->ceiling_height= platform->ceiling_height;
            // adjust_platform_endpoint_and_line_heights(platform_index);
            // adjust_platform_for_media(platform_index, true);
        }

        // ForgePlus: the static data export_tag_to_global_array_and_size() (game_wad.cpp) saves for a platform, when the
        // level has no static data of its own for its platforms
        public static static_platform_data static_platform_data_from_platform(platform_data p)
        {
            // ghs: this belongs somewhere else
            var platform = new static_platform_data(); // obj_clear(platform);
            platform.type = p.type;
            platform.speed = p.speed;
            platform.delay = p.delay;
            if (PLATFORM_GOES_BOTH_WAYS(p))
            {
                platform.maximum_height = p.maximum_ceiling_height;
                platform.minimum_height = p.minimum_floor_height;
            }
            else if (PLATFORM_COMES_FROM_FLOOR(p))
            {
                platform.maximum_height = p.maximum_floor_height;
                platform.minimum_height = p.minimum_floor_height;
            }
            else
            {
                platform.maximum_height = p.maximum_ceiling_height;
                platform.minimum_height = p.minimum_floor_height;
            }
            platform.static_flags = p.static_flags;
            platform.polygon_index = p.polygon_index;
            platform.tag = p.tag;

            return platform;
        }

        public static static_platform_data get_defaults_for_platform_type(short type)
        {
            platform_definition definition = get_platform_definition(type);
            // Fallback for out-of-range type
            if (definition == null) definition = get_platform_definition(0);

            return definition.defaults;
        }

        /* ---------- private code */

        private static short polygon_index_to_platform_index(MapLevel level, short polygon_index)
        {
            short platform_index;

            for (platform_index = 0; platform_index < level.PlatformList.Count; ++platform_index)
            {
                if (level.PlatformList[platform_index].polygon_index == polygon_index) break;
            }
            if (platform_index == level.PlatformList.Count) platform_index = NONE;

            return platform_index;
        }

        /* rules for using native polygon heights: a) if this is a floor platform, then take the polygon's
            native floor height to be the maximum height if it is greater than the minimum height, otherwise
            use it as the minimum height; b) if this is a ceiling platform, then take the polygon's native
            ceiling height to be the minimum height if it is less than the maximum height, otherwise use it
            as the maximum height; c) native polygon height is not used for floor/ceiling platforms */
        private static void calculate_platform_extrema(MapLevel level, short platform_index, short lowest_level, short highest_level)
        {
            short i;
            platform_data platform = get_platform_data(level, platform_index);
            polygon_data polygon = get_polygon_data(level, platform.polygon_index);
            short lowest_adjacent_floor, highest_adjacent_ceiling;
            short highest_adjacent_floor, lowest_adjacent_ceiling;

            // LP change: no need for this test
            // assert(lowest_level==NONE||highest_level==NONE||lowest_level<highest_level);

            /* calculate lowest and highest adjacent floors and ceilings */
            lowest_adjacent_floor = highest_adjacent_floor = polygon.floor_height;
            lowest_adjacent_ceiling = highest_adjacent_ceiling = polygon.ceiling_height;
            for (i = 0; i < polygon.vertex_count; ++i)
            {
                if (polygon.adjacent_polygon_indexes[i] != NONE)
                {
                    polygon_data adjacent_polygon = get_polygon_data(level, polygon.adjacent_polygon_indexes[i]);

                    if (adjacent_polygon.floor_height < lowest_adjacent_floor) lowest_adjacent_floor = adjacent_polygon.floor_height;
                    if (adjacent_polygon.floor_height > highest_adjacent_floor) highest_adjacent_floor = adjacent_polygon.floor_height;
                    if (adjacent_polygon.ceiling_height < lowest_adjacent_ceiling) lowest_adjacent_ceiling = adjacent_polygon.ceiling_height;
                    if (adjacent_polygon.ceiling_height > highest_adjacent_ceiling) highest_adjacent_ceiling = adjacent_polygon.ceiling_height;
                }
            }

            /* take into account the EXTENDS_FLOOR_TO_CEILING flag */
            if (PLATFORM_EXTENDS_FLOOR_TO_CEILING(platform))
            {
                if (polygon.ceiling_height > highest_adjacent_floor) highest_adjacent_floor = polygon.ceiling_height;
                if (polygon.floor_height < lowest_adjacent_ceiling) lowest_adjacent_ceiling = polygon.floor_height;
            }

            /* calculate floor and ceiling min, max values as appropriate for the platform direction */
            if (PLATFORM_GOES_BOTH_WAYS(platform))
            {
                /* split platforms always meet in the center */
                platform.minimum_floor_height = lowest_level == NONE ? lowest_adjacent_floor : lowest_level;
                platform.maximum_ceiling_height = highest_level == NONE ? highest_adjacent_ceiling : highest_level;
                platform.maximum_floor_height = platform.minimum_ceiling_height =
                    (short) ((platform.minimum_floor_height + platform.maximum_ceiling_height) / 2);
            }
            else
            {
                if (PLATFORM_COMES_FROM_FLOOR(platform))
                {
                    if (PLATFORM_USES_NATIVE_POLYGON_HEIGHTS(platform))
                    {
                        if (polygon.floor_height < lowest_adjacent_floor || PLATFORM_EXTENDS_FLOOR_TO_CEILING(platform))
                        {
                            lowest_adjacent_floor = polygon.floor_height;
                        }
                        else
                        {
                            highest_adjacent_floor = polygon.floor_height;
                        }
                    }

                    platform.minimum_floor_height = lowest_level == NONE ? lowest_adjacent_floor : lowest_level;
                    platform.maximum_floor_height = highest_level == NONE ? highest_adjacent_floor : highest_level;
                    platform.minimum_ceiling_height = platform.maximum_ceiling_height = polygon.ceiling_height;
                }
                else if (PLATFORM_COMES_FROM_CEILING(platform))
                {

                    if (PLATFORM_USES_NATIVE_POLYGON_HEIGHTS(platform))
                    {
                        if (polygon.ceiling_height > highest_adjacent_ceiling || PLATFORM_EXTENDS_FLOOR_TO_CEILING(platform))
                        {
                            highest_adjacent_ceiling = polygon.ceiling_height;
                        }
                        else
                        {
                            lowest_adjacent_ceiling = polygon.ceiling_height;
                        }
                    }

                    platform.minimum_ceiling_height = lowest_level == NONE ? lowest_adjacent_ceiling : lowest_level;
                    platform.maximum_ceiling_height = highest_level == NONE ? highest_adjacent_ceiling : highest_level;
                    platform.minimum_floor_height = platform.maximum_floor_height = polygon.floor_height;
                }
            }
        }

        public static void adjust_platform_sides(MapLevel level, platform_data platform, short old_ceiling_height, short new_ceiling_height)
        {
            polygon_data polygon = get_polygon_data(level, platform.polygon_index);
            short delta_height = (short) (new_ceiling_height - old_ceiling_height);

            for (short i = 0; i < polygon.vertex_count; ++i)
            {
                short side_index;
                side_data side;
                line_data line = get_line_data(level, polygon.line_indexes[i]);
                short adjacent_polygon_index = polygon.adjacent_polygon_indexes[i];

                /* adjust the platform side (i.e., the texture on the side of the platform) */
                if (adjacent_polygon_index != NONE)
                {
                    side_index = adjacent_polygon_index == line.clockwise_polygon_owner ? line.clockwise_polygon_side_index : line.counterclockwise_polygon_side_index;
                    if (side_index != NONE)
                    {
                        side = get_side_data(level, side_index);
                        switch (side.type)
                        {
                            case _full_side:
                            case _high_side:
                            case _split_side:
                                side.primary_texture.y0 += delta_height;
                                break;
                        }
                    }
                }

                /* adjust the shaft side (i.e., the texture the platform slides against) */
                side_index = polygon.side_indexes[i];
                if (side_index != NONE)
                {
                    side = get_side_data(level, side_index);
                    switch (side.type)
                    {
                        case _split_side: /* secondary */
                        case _high_side: /* primary */
                        case _full_side: /* primary */
                            side.primary_texture.y0 -= delta_height;
                            break;
                        case _low_side: /* primary */
                            // ghs: the following doesn't appear to be necessary at all!
                            break;

                        default:
                            vhalt($"wasn't expecting side #{side_index} to have type #{side.type}");
                            break;
                    }
                }
            }
        }

        public static void unpack_static_platform_data(StreamPointer S, IList<static_platform_data> Objects, int Count, short version)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                static_platform_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.type);
                StreamToValue(S, out ObjPtr.speed);
                StreamToValue(S, out ObjPtr.delay);
                StreamToValue(S, out ObjPtr.maximum_height);
                StreamToValue(S, out ObjPtr.minimum_height);

                StreamToValue(S, out ObjPtr.static_flags);

                StreamToValue(S, out ObjPtr.polygon_index);

                StreamToValue(S, out ObjPtr.tag);

                S.Skip(7 * 2);

                if (version == MARATHON_ONE_DATA_VERSION)
                {
                    switch (ObjPtr.type)
                    {
                        case 0: // marathon door
                        case 3: // pfhor door
                            SET_PLATFORM_IS_DOOR(ObjPtr, true);
                            break;
                    }

                    if (PLATFORM_IS_LOCKED(ObjPtr))
                    {
                        SET_PLATFORM_IS_LOCKED(ObjPtr, false);
                        SET_PLATFORM_FLOODS_M1(ObjPtr, true);
                    }
                }
            }

            assert((S.Position - Stream) == Count * SIZEOF_static_platform_data);
        }

        public static void pack_static_platform_data(StreamPointer S, IList<static_platform_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                static_platform_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.type);
                ValueToStream(S, ObjPtr.speed);
                ValueToStream(S, ObjPtr.delay);
                ValueToStream(S, ObjPtr.maximum_height);
                ValueToStream(S, ObjPtr.minimum_height);

                ValueToStream(S, ObjPtr.static_flags);

                ValueToStream(S, ObjPtr.polygon_index);

                ValueToStream(S, ObjPtr.tag);

                S.Skip(7 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_static_platform_data);
        }

        private static void StreamToEndpointOwner(StreamPointer S, endpoint_owner_data Object)
        {
            StreamToValue(S, out Object.first_polygon_index);
            StreamToValue(S, out Object.polygon_index_count);
            StreamToValue(S, out Object.first_line_index);
            StreamToValue(S, out Object.line_index_count);
        }

        private static void EndpointOwnerToStream(StreamPointer S, endpoint_owner_data Object)
        {
            ValueToStream(S, Object.first_polygon_index);
            ValueToStream(S, Object.polygon_index_count);
            ValueToStream(S, Object.first_line_index);
            ValueToStream(S, Object.line_index_count);
        }

        public static void unpack_platform_data(StreamPointer S, IList<platform_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                platform_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.type);
                StreamToValue(S, out ObjPtr.static_flags);
                StreamToValue(S, out ObjPtr.speed);
                StreamToValue(S, out ObjPtr.delay);
                StreamToValue(S, out ObjPtr.minimum_floor_height);
                StreamToValue(S, out ObjPtr.maximum_floor_height);
                StreamToValue(S, out ObjPtr.minimum_ceiling_height);
                StreamToValue(S, out ObjPtr.maximum_ceiling_height);

                StreamToValue(S, out ObjPtr.polygon_index);
                StreamToValue(S, out ObjPtr.dynamic_flags);
                StreamToValue(S, out ObjPtr.floor_height);
                StreamToValue(S, out ObjPtr.ceiling_height);
                StreamToValue(S, out ObjPtr.ticks_until_restart);

                for (int j = 0; j < MAXIMUM_VERTICES_PER_POLYGON; j++)
                    StreamToEndpointOwner(S, ObjPtr.endpoint_owners[j]);

                StreamToValue(S, out ObjPtr.parent_platform_index);

                StreamToValue(S, out ObjPtr.tag);

                S.Skip(22 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_platform_data);
        }

        public static void pack_platform_data(StreamPointer S, IList<platform_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                platform_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.type);
                ValueToStream(S, ObjPtr.static_flags);
                ValueToStream(S, ObjPtr.speed);
                ValueToStream(S, ObjPtr.delay);
                ValueToStream(S, ObjPtr.minimum_floor_height);
                ValueToStream(S, ObjPtr.maximum_floor_height);
                ValueToStream(S, ObjPtr.minimum_ceiling_height);
                ValueToStream(S, ObjPtr.maximum_ceiling_height);

                ValueToStream(S, ObjPtr.polygon_index);
                ValueToStream(S, ObjPtr.dynamic_flags);
                ValueToStream(S, ObjPtr.floor_height);
                ValueToStream(S, ObjPtr.ceiling_height);
                ValueToStream(S, ObjPtr.ticks_until_restart);

                for (int j = 0; j < MAXIMUM_VERTICES_PER_POLYGON; j++)
                    EndpointOwnerToStream(S, ObjPtr.endpoint_owners[j]);

                ValueToStream(S, ObjPtr.parent_platform_index);

                ValueToStream(S, ObjPtr.tag);

                S.Skip(22 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_platform_data);
        }
    }
}
