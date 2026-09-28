// Port of Aleph One: Source_Files/GameWorld/items.h, items.cpp (item types and definitions)
//
// Not ported: everything that places, picks up or animates items in a game, and MML parsing of items.
using static AlephOne.csmacros;
using static AlephOne.cstypes;

namespace AlephOne
{
    public static partial class items
    {
        /* ---------- constants */

        /* item types (class) */
        public const short _weapon = 0;
        public const short _ammunition = 1;
        public const short _powerup = 2;
        public const short _item = 3;
        public const short _weapon_powerup = 4;
        public const short _ball = 5;

        public const short NUMBER_OF_ITEM_TYPES = 6;
        public const short _network_statistics = NUMBER_OF_ITEM_TYPES; // Used in game_window.c

        /* item types */
        public const short _i_knife = 0;
        public const short _i_magnum = 1;
        public const short _i_magnum_magazine = 2;
        public const short _i_plasma_pistol = 3;
        public const short _i_plasma_magazine = 4;
        public const short _i_assault_rifle = 5;
        public const short _i_assault_rifle_magazine = 6;
        public const short _i_assault_grenade_magazine = 7;
        public const short _i_missile_launcher = 8;
        public const short _i_missile_launcher_magazine = 9;
        public const short _i_invisibility_powerup = 10;
        public const short _i_invincibility_powerup = 11;
        public const short _i_infravision_powerup = 12;
        public const short _i_alien_shotgun = 13;
        public const short _i_alien_shotgun_magazine = 14;
        public const short _i_flamethrower = 15;
        public const short _i_flamethrower_canister = 16;
        public const short _i_extravision_powerup = 17;
        public const short _i_oxygen_powerup = 18;
        public const short _i_energy_powerup = 19;
        public const short _i_double_energy_powerup = 20;
        public const short _i_triple_energy_powerup = 21;
        public const short _i_shotgun = 22;
        public const short _i_shotgun_magazine = 23;
        public const short _i_spht_door_key = 24;
        public const short _i_uplink_chip = 25;

        public const short BALL_ITEM_BASE = 26;
        public const short _i_light_blue_ball = BALL_ITEM_BASE;
        public const short _i_red_ball = 27;
        public const short _i_violet_ball = 28;
        public const short _i_yellow_ball = 29;
        public const short _i_brown_ball = 30;
        public const short _i_orange_ball = 31;
        public const short _i_blue_ball = 32; // heh heh
        public const short _i_green_ball = 33;

        // LP addition:
        public const short _i_smg = 34;
        public const short _i_smg_ammo = 35;

        public const short NUMBER_OF_DEFINED_ITEMS = 36;

        /* ---------- code */

        // Item-definition accessor
        public static item_definition get_item_definition(
            short type)
        {
            return GetMemberWithBounds(item_definitions, type, NUMBER_OF_DEFINED_ITEMS);
        }

        //a non-inlined version for external use
        public static item_definition get_item_definition_external(
            short type)
        {
            return get_item_definition(type);
        }

        public static short get_item_kind(
            short item_id)
        {
            item_definition definition = get_item_definition(item_id);
            // LP change: added idiot-proofing
            if (definition == null) return NONE;

            return definition.item_kind;
        }

        public static short get_item_shape(
            short item_id)
        {
            item_definition definition = get_item_definition(item_id);
            // LP change: added idiot-proofing
            if (definition == null) return NONE;

            return (short) definition.base_shape;
        }
    }
}
