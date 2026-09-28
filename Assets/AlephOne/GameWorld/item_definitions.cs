// Port of Aleph One: Source_Files/GameWorld/item_definitions.h
using static AlephOne.cstypes;
using static AlephOne.map;
using static AlephOne.shape_descriptors;

namespace AlephOne
{
    /* ---------- structures */

    public class item_definition
    {
        public short item_kind;
        public short singular_name_id;
        public short plural_name_id;
        public ushort base_shape;
        public short maximum_count_per_player;
        public short invalid_environments;

        // extension to support per-difficulty maximums
        public short[] extended_maximum_count = new short[NUMBER_OF_GAME_DIFFICULTY_LEVELS] { NONE, NONE, NONE, NONE, NONE };

        // items.cpp
        public short get_maximum_count_per_player(bool is_m1, int difficulty_level)
        {
            if (extended_maximum_count[difficulty_level] != NONE)
            {
                return extended_maximum_count[difficulty_level];
            }
            else
            {
                if (difficulty_level == _total_carnage_level &&
                    (is_m1 || item_kind == items._ammunition))
                {
                    return INT16_MAX;
                }
                else
                {
                    return maximum_count_per_player;
                }
            }
        }
    }

    public static partial class items
    {
        /* ---------- globals */

        public static readonly item_definition[] item_definitions = new item_definition[]
        {
            /* Knife */
            new item_definition {item_kind = _weapon, singular_name_id = 0, plural_name_id = 0, base_shape = UNONE, maximum_count_per_player = 1, invalid_environments = 0},

            // pistol and ammo
            new item_definition {item_kind = _weapon, singular_name_id = 1, plural_name_id = 2, base_shape = BUILD_DESCRIPTOR(_collection_items, 0), maximum_count_per_player = 2, invalid_environments = 0},
            new item_definition {item_kind = _ammunition, singular_name_id = 3, plural_name_id = 4, base_shape = BUILD_DESCRIPTOR(_collection_items, 3), maximum_count_per_player = 50, invalid_environments = 0},

            // fusion pistol and ammo
            new item_definition {item_kind = _weapon, singular_name_id = 5, plural_name_id = 5, base_shape = BUILD_DESCRIPTOR(_collection_items, 1), maximum_count_per_player = 1, invalid_environments = 0},
            new item_definition {item_kind = _ammunition, singular_name_id = 6, plural_name_id = 7, base_shape = BUILD_DESCRIPTOR(_collection_items, 4), maximum_count_per_player = 25, invalid_environments = 0},

            // assault rifle, bullets and grenades
            new item_definition {item_kind = _weapon, singular_name_id = 8, plural_name_id = 8, base_shape = BUILD_DESCRIPTOR(_collection_items, 2), maximum_count_per_player = 1, invalid_environments = _environment_vacuum},
            new item_definition {item_kind = _ammunition, singular_name_id = 9, plural_name_id = 10, base_shape = BUILD_DESCRIPTOR(_collection_items, 5), maximum_count_per_player = 15, invalid_environments = _environment_vacuum},
            new item_definition {item_kind = _ammunition, singular_name_id = 11, plural_name_id = 12, base_shape = BUILD_DESCRIPTOR(_collection_items, 6), maximum_count_per_player = 8, invalid_environments = _environment_vacuum},

            // rocket launcher and ammo
            new item_definition {item_kind = _weapon, singular_name_id = 13, plural_name_id = 13, base_shape = BUILD_DESCRIPTOR(_collection_items, 12), maximum_count_per_player = 1, invalid_environments = _environment_vacuum},
            new item_definition {item_kind = _ammunition, singular_name_id = 14, plural_name_id = 15, base_shape = BUILD_DESCRIPTOR(_collection_items, 7), maximum_count_per_player = 4, invalid_environments = _environment_vacuum},

            // invisibility, invincibility, invfravision
            new item_definition {item_kind = _powerup, singular_name_id = NONE, plural_name_id = NONE, base_shape = BUILD_DESCRIPTOR(_collection_items, 8), maximum_count_per_player = 1, invalid_environments = 0},
            new item_definition {item_kind = _powerup, singular_name_id = NONE, plural_name_id = NONE, base_shape = BUILD_DESCRIPTOR(_collection_items, 9), maximum_count_per_player = 1, invalid_environments = 0},
            new item_definition {item_kind = _powerup, singular_name_id = NONE, plural_name_id = NONE, base_shape = BUILD_DESCRIPTOR(_collection_items, 14), maximum_count_per_player = 1, invalid_environments = 0},

            // alien weapon and ammunition
            new item_definition {item_kind = _weapon, singular_name_id = 16, plural_name_id = 16, base_shape = BUILD_DESCRIPTOR(_collection_items, 13), maximum_count_per_player = 1, invalid_environments = 0},
            new item_definition {item_kind = _ammunition, singular_name_id = 17, plural_name_id = 18, base_shape = UNONE, maximum_count_per_player = 999, invalid_environments = 0},

            // flamethrower and ammo
            new item_definition {item_kind = _weapon, singular_name_id = 19, plural_name_id = 19, base_shape = BUILD_DESCRIPTOR(_collection_items, 10), maximum_count_per_player = 1, invalid_environments = _environment_vacuum},
            new item_definition {item_kind = _ammunition, singular_name_id = 20, plural_name_id = 21, base_shape = BUILD_DESCRIPTOR(_collection_items, 11), maximum_count_per_player = 3, invalid_environments = _environment_vacuum},

            /* extravision powerup */
            new item_definition {item_kind = _powerup, singular_name_id = NONE, plural_name_id = NONE, base_shape = BUILD_DESCRIPTOR(_collection_items, 15), maximum_count_per_player = 1, invalid_environments = 0},

            // energy and oxygen recharges
            new item_definition {item_kind = _powerup, singular_name_id = NONE, plural_name_id = NONE, base_shape = BUILD_DESCRIPTOR(_collection_items, 23), maximum_count_per_player = 1, invalid_environments = 0}, /* oxygen recharge */
            new item_definition {item_kind = _powerup, singular_name_id = NONE, plural_name_id = NONE, base_shape = BUILD_DESCRIPTOR(_collection_items, 20), maximum_count_per_player = 1, invalid_environments = 0}, /* x1 recharge */
            new item_definition {item_kind = _powerup, singular_name_id = NONE, plural_name_id = NONE, base_shape = BUILD_DESCRIPTOR(_collection_items, 21), maximum_count_per_player = 1, invalid_environments = 0}, /* x2 recharge */
            new item_definition {item_kind = _powerup, singular_name_id = NONE, plural_name_id = NONE, base_shape = BUILD_DESCRIPTOR(_collection_items, 22), maximum_count_per_player = 1, invalid_environments = 0}, /* x3 recharge */

            // shotgun and ammo
            new item_definition {item_kind = _weapon, singular_name_id = 27, plural_name_id = 28, base_shape = BUILD_DESCRIPTOR(_collection_items, 18), maximum_count_per_player = 2, invalid_environments = 0},
            new item_definition {item_kind = _ammunition, singular_name_id = 17, plural_name_id = 18, base_shape = BUILD_DESCRIPTOR(_collection_items, 19), maximum_count_per_player = 80, invalid_environments = 0},

            // _i_spht_door_key, _i_uplink_chip
            new item_definition {item_kind = _item, singular_name_id = 29, plural_name_id = 30, base_shape = BUILD_DESCRIPTOR(_collection_items, 17), maximum_count_per_player = 8, invalid_environments = 0},
            new item_definition {item_kind = _item, singular_name_id = 31, plural_name_id = 32, base_shape = BUILD_DESCRIPTOR(_collection_items, 16), maximum_count_per_player = 1, invalid_environments = 0},

            // Net game balls.
            new item_definition {item_kind = _ball, singular_name_id = 33, plural_name_id = 33, base_shape = BUILD_DESCRIPTOR(BUILD_COLLECTION(_collection_player, 0), 29), maximum_count_per_player = 1, invalid_environments = _environment_single_player},
            new item_definition {item_kind = _ball, singular_name_id = 34, plural_name_id = 34, base_shape = BUILD_DESCRIPTOR(BUILD_COLLECTION(_collection_player, 1), 29), maximum_count_per_player = 1, invalid_environments = _environment_single_player},
            new item_definition {item_kind = _ball, singular_name_id = 35, plural_name_id = 35, base_shape = BUILD_DESCRIPTOR(BUILD_COLLECTION(_collection_player, 2), 29), maximum_count_per_player = 1, invalid_environments = _environment_single_player},
            new item_definition {item_kind = _ball, singular_name_id = 36, plural_name_id = 36, base_shape = BUILD_DESCRIPTOR(BUILD_COLLECTION(_collection_player, 3), 29), maximum_count_per_player = 1, invalid_environments = _environment_single_player},
            new item_definition {item_kind = _ball, singular_name_id = 37, plural_name_id = 37, base_shape = BUILD_DESCRIPTOR(BUILD_COLLECTION(_collection_player, 4), 29), maximum_count_per_player = 1, invalid_environments = _environment_single_player},
            new item_definition {item_kind = _ball, singular_name_id = 38, plural_name_id = 38, base_shape = BUILD_DESCRIPTOR(BUILD_COLLECTION(_collection_player, 5), 29), maximum_count_per_player = 1, invalid_environments = _environment_single_player},
            new item_definition {item_kind = _ball, singular_name_id = 39, plural_name_id = 39, base_shape = BUILD_DESCRIPTOR(BUILD_COLLECTION(_collection_player, 6), 29), maximum_count_per_player = 1, invalid_environments = _environment_single_player},
            new item_definition {item_kind = _ball, singular_name_id = 40, plural_name_id = 40, base_shape = BUILD_DESCRIPTOR(BUILD_COLLECTION(_collection_player, 7), 29), maximum_count_per_player = 1, invalid_environments = _environment_single_player},

            // LP addition: smg and ammo
            new item_definition {item_kind = _weapon, singular_name_id = 41, plural_name_id = 41, base_shape = BUILD_DESCRIPTOR(_collection_items, 25), maximum_count_per_player = 1, invalid_environments = 0},
            new item_definition {item_kind = _ammunition, singular_name_id = 42, plural_name_id = 43, base_shape = BUILD_DESCRIPTOR(_collection_items, 24), maximum_count_per_player = 8, invalid_environments = 0},
        };
    }
}
