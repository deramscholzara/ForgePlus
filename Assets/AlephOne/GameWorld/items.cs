// Port of Aleph One: Source_Files/GameWorld/items.h, items.cpp (item types and definitions)
//
// Not ported: everything that places, picks up or animates items in a game.
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

        // The definitions as they were before MML changed them (null while MML hasn't)
        private static item_definition[] original_item_definitions;

        // ForgePlus: whether MML has changed the definitions since they were last reset
        public static bool item_definitions_changed_by_mml()
        {
            return original_item_definitions != null;
        }

        public static void reset_mml_items()
        {
            if (original_item_definitions != null)
            {
                for (int i = 0; i < NUMBER_OF_DEFINED_ITEMS; i++)
                    item_definitions[i] = original_item_definitions[i];
                original_item_definitions = null;
            }
        }

        public static void parse_mml_items(InfoTree root)
        {
            // back up old values first
            if (original_item_definitions == null)
            {
                original_item_definitions = new item_definition[NUMBER_OF_DEFINED_ITEMS];
                for (int i = 0; i < NUMBER_OF_DEFINED_ITEMS; i++)
                {
                    original_item_definitions[i] = item_definitions[i];
                    item_definitions[i] = item_definitions[i].Clone();
                }
            }

            foreach (var itree in root.children_named("item"))
            {
                short index = 0;
                if (!itree.read_indexed("index", ref index, NUMBER_OF_DEFINED_ITEMS))
                    continue;

                item_definition def = item_definitions[index];
                itree.read_attr("singular", ref def.singular_name_id);
                itree.read_attr("plural", ref def.plural_name_id);
                itree.read_indexed("maximum", ref def.maximum_count_per_player, short.MaxValue + 1);
                itree.read_attr("invalid", ref def.invalid_environments);
                itree.read_indexed("type", ref def.item_kind, NUMBER_OF_ITEM_TYPES);

                foreach (var max in itree.children_named("difficulty"))
                {
                    short difficulty = 0;
                    if (!max.read_indexed("index", ref difficulty, map.NUMBER_OF_GAME_DIFFICULTY_LEVELS))
                        continue;

                    max.read_indexed("maximum", ref def.extended_maximum_count[difficulty], short.MaxValue + 1);
                }

                foreach (var shape in itree.children_named("shape"))
                    shape.read_shape(ref def.base_shape);
            }
        }
    }
}
