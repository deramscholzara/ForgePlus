// Port of Aleph One: Source_Files/GameWorld/placement.cpp (map data: loading the placement chunk)
//
// Not ported (runtime only): placing, recreating and counting objects during a game
// (place_initial_objects, recreate_objects, object_was_just_added/destroyed,
// get_random_player_starting_location_and_facing, ...).
using System;
using static AlephOne.csalerts;
using static AlephOne.map;
using static AlephOne.map_constructors;

namespace AlephOne
{
    public static class placement
    {
        /* Constants */
        public const int NUMBER_OF_TICKS_BETWEEN_RECREATION = (15 * TICKS_PER_SECOND);
        public const int INVISIBLE_RANDOM_POINT_RETRIES = 10;

        /* Global variables */
        // object_placement_info is a field of MapLevel; item_placement_info and monster_placement_info
        // point into it:
        public const int item_placement_info = 0;
        public const int monster_placement_info = MAXIMUM_OBJECT_TYPES;

        public static object_frequency_definition[] new_object_placement_info()
        {
            var info = new object_frequency_definition[2 * MAXIMUM_OBJECT_TYPES];
            for (int i = 0; i < info.Length; i++) info[i] = new object_frequency_definition();
            return info;
        }

        // The MAXIMUM_OBJECT_TYPES definitions starting at object_placement_info[first]
        private static object_frequency_definition[] placement_info_at(MapLevel level, int first)
        {
            var info = new object_frequency_definition[MAXIMUM_OBJECT_TYPES];
            Array.Copy(level.object_placement_info, first, info, 0, MAXIMUM_OBJECT_TYPES);
            return info;
        }

        /*************************************************************************************************
         *
         * Function: load_placement_data
         * Purpose:  called by game_wad.c to get the placement information for the map.
         *
         * LP: changed to unpack the placement data from a stream of bytes
         *
         *************************************************************************************************/
        public static void load_placement_data(MapLevel level, StreamPointer _monsters, StreamPointer _items)
        {
            assert(_monsters != null && _items != null);
            // assert(NUMBER_OF_MONSTER_TYPES<=MAXIMUM_OBJECT_TYPES);
            // assert(NUMBER_OF_DEFINED_ITEMS<=MAXIMUM_OBJECT_TYPES);

            /* Clear the arrays */
            level.object_placement_info = new_object_placement_info();

            /* Copy them in */
            unpack_object_frequency_definition(_monsters, placement_info_at(level, monster_placement_info), MAXIMUM_OBJECT_TYPES);
            unpack_object_frequency_definition(_items, placement_info_at(level, item_placement_info), MAXIMUM_OBJECT_TYPES);

            // Clears the data for monster #0, the Marine
            level.object_placement_info[monster_placement_info] = new object_frequency_definition(); // obj_clear(*monster_placement_info);

            // Not ported: the DEBUG checks, and the #if 0 network fixup
        }

        public static object_frequency_definition[] get_placement_info(MapLevel level)
        {
            return level.object_placement_info;
        }
    }
}
