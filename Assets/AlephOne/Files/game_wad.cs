// Port of Aleph One: Source_Files/Files/game_wad.h, game_wad.cpp (maps)
//
// Not ported (the running game, saved games, networking, Lua, MML and level scripts, the UI):
// set_map_file/use_map_file, new_game, goto_level, load_game_from_file, revert_game, save_game_file,
// build_save_game_wad, build_meta_game_wad, get_dynamic_data_from_save/_wad, get_player_data_from_wad,
// the net functions, and in process_map_wad the restoring_game path, scenery, shapes and sounds patches,
// MMLS/LUAS/Lua state, music and ephemera. Chunks that aren't loaded are kept with the level
// (MapLevel.loaded_wad) and saved as they were.
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csalerts;
using static AlephOne.cstypes;
using static AlephOne.editor;
using static AlephOne.game_errors;
using static AlephOne.import_definitions;
using static AlephOne.lightsource;
using static AlephOne.map;
using static AlephOne.map_constructors;
using static AlephOne.media;
using static AlephOne.Packing;
using static AlephOne.placement;
using static AlephOne.platforms;
using static AlephOne.tags;
using static AlephOne.wad;

namespace AlephOne
{
    [NoAutoStaticsCleanup]
    public static class game_wad
    {
        // game_wad.h
        public const int SAVE_GAME_METADATA_INDEX = 1000;

        /* -------- level loading */

        public static bool load_level_from_map(FileSpecifier MapFileSpec, short level_index, out MapLevel level)
        {
            var header = new wad_header();
            wad_data wad;
            short index_to_load;

            level = null;
            clear_game_error();

            /* Determine what we are trying to do.. */
            vassert(level_index != NONE, "restoring saved games is not ported");
            index_to_load = level_index;

            var MapFile = new OpenedFile();
            if (open_wad_file_for_reading(MapFileSpec, MapFile))
            {
                /* Read the file */
                if (read_wad_header(MapFile, header))
                {
                    if (index_to_load >= 0 && index_to_load < header.wad_count)
                    {
                        wad = read_indexed_wad_from_file(MapFile, header, index_to_load, true);
                        if (wad != null)
                        {
                            /* Process everything... */
                            level = new MapLevel();
                            process_map_wad(level, wad, header.data_version);

                            // ForgePlus: keep the level's directory data
                            if (header.application_specific_directory_data_size == SIZEOF_directory_data)
                            {
                                byte[] directories = read_directory_data(MapFile, header);
                                level.loaded_wad.directory_data = new byte[SIZEOF_directory_data];
                                Array.Copy(directories, get_indexed_directory_data(header, index_to_load, directories), level.loaded_wad.directory_data, 0, SIZEOF_directory_data);
                            }

                            /* Nuke our memory... */
                            free_wad(wad);
                        }
                        else
                        {
                            // error code has been set...
                        }
                    }
                    else
                    {
                        set_game_error(gameError, errWadIndexOutOfRange);
                    }
                }
                else
                {
                    // error code has been set...
                }

                /* Close the file.. */
                close_wad_file(MapFile);
            }
            else
            {
                // error code has been set..
            }

            /* ... and bail */
            if (error_pending()) level = null;
            return (!error_pending());
        }

        /* Hopefully this is in the correct order of initialization... */
        /* This sucks, beavis. */
        private static void complete_loading_level(MapLevel level, byte[] _map_indexes, int map_index_count,
            byte[] _platform_data, int platform_data_count,
            byte[] actual_platform_data, int actual_platform_data_count, short version)
        {
            /* Scan, add the doors, recalculate, and generally tie up all loose ends */
            /* Recalculate the redundant data.. */
            load_redundant_map_data(level, _map_indexes, map_index_count);

            level.static_platforms.Clear();

            /* Add the platforms. */
            if (_platform_data != null || (_platform_data == null && actual_platform_data == null))
            {
                scan_and_add_platforms(level, _platform_data, platform_data_count, version);
            }
            else
            {
                assert(actual_platform_data != null);
                level.PlatformList.Clear();
                for (int i = 0; i < actual_platform_data_count; i++) level.PlatformList.Add(new platform_data());
                unpack_platform_data(new StreamPointer(actual_platform_data), level.PlatformList, actual_platform_data_count);
                assert(actual_platform_data_count == (short) actual_platform_data_count);
                assert(0 <= (short) actual_platform_data_count);
                // dynamic_world->platform_count= actual_platform_data_count;
            }

            // scan_and_add_scenery(); ok_to_reset_scenery_solidity = true;

            /* Gotta do this after recalculate redundant.. */
            if (version == MARATHON_ONE_DATA_VERSION)
            {
                short loop;

                for (loop = 0; loop < level.SideList.Count; ++loop)
                {
                    guess_side_lightsource_indexes(level, loop);
                    if ((level.static_world.environment_flags & _environment_vacuum) != 0)
                    {
                        side_data side = get_side_data(level, loop);
                        if ((side.flags & _side_is_control_panel) != 0)
                            side.flags |= _side_is_m1_lighted_switch;
                    }
                }
            }
        }

        /* Call with location of NULL to get the number of start locations for a */
        /* given team or player */
        public static short get_player_starting_location_and_facing(MapLevel level, short team, short index, object_location location)
        {
            short ii;
            short count = 0;
            bool done = false;

            for (ii = 0; !done && ii < level.SavedObjectList.Count; ++ii)
            {
                map_object saved_object = level.SavedObjectList[ii];
                if (saved_object.type == _saved_player)
                {
                    /* index=NONE means use any starting location */
                    if (saved_object.index == team || team == NONE)
                    {
                        if (location != null && count == index)
                        {
                            location.p = saved_object.location;
                            location.polygon_index = saved_object.polygon_index;
                            location.yaw = saved_object.facing;
                            location.pitch = 0;
                            location.flags = saved_object.flags;
                            done = true;
                        }
                        count++;
                    }
                }
            }

            /* If they asked for a valid location, make sure that we gave them one */
            if (location != null) vassert(done, "Tried to place: {0} only {1} starting pts.", index, count);

            return count;
        }

        public static uint get_current_map_checksum(FileSpecifier MapFileSpec)
        {
            var header = new wad_header();

            var MapFile = new OpenedFile();
            open_wad_file_for_reading(MapFileSpec, MapFile);
            assert(MapFile.IsOpen());

            /* Read the file */
            read_wad_header(MapFile, header);

            /* Close the file.. */
            close_wad_file(MapFile);

            return header.checksum;
        }

        // ForgePlus: from the old-style branches of get_indexed_entry_point() and get_entry_points(), which
        // repeat it; get_level_directory() uses it too
        private static void fix_m1_entry_point_flags(wad_header header, static_data map_info)
        {
            // single-player Marathon 1 levels aren't always marked
            if (header.data_version == MARATHON_ONE_DATA_VERSION &&
                map_info.entry_point_flags == 0)
                map_info.entry_point_flags = _single_player_entry_point;

            // Marathon 1 handled (then-unused) coop flag differently
            if (header.data_version == MARATHON_ONE_DATA_VERSION)
            {
                if ((map_info.entry_point_flags & _single_player_entry_point) != 0)
                    map_info.entry_point_flags |= _multiplayer_cooperative_entry_point;
                if ((map_info.entry_point_flags & _multiplayer_carnage_entry_point) != 0)
                    map_info.entry_point_flags &= unchecked((uint) ~_multiplayer_cooperative_entry_point);
            }
        }

        private static static_data read_map_info(wad_data wad)
        {
            int length;
            byte[] p = extract_type_from_wad(wad, MAP_INFO_TAG, out length);
            assert(length == SIZEOF_static_data);
            var map_info = new static_data();
            unpack_static_data(new StreamPointer(p), new[] { map_info }, 1);
            return map_info;
        }

        public static bool get_indexed_entry_point(FileSpecifier MapFileSpec, entry_point entry_point, ref short index, int type)
        {
            short actual_index;

            // Open map file
            var MapFile = new OpenedFile();
            if (!open_wad_file_for_reading(MapFileSpec, MapFile))
                return false;

            // Read header
            var header = new wad_header();
            if (!read_wad_header(MapFile, header))
            {
                close_wad_file(MapFile);
                return false;
            }

            bool success = false;
            if (header.application_specific_directory_data_size == SIZEOF_directory_data)
            {
                // New style wad
                byte[] total_directory_data = read_directory_data(MapFile, header);

                assert(total_directory_data != null);
                for (actual_index = index; actual_index < header.wad_count; ++actual_index)
                {
                    int p = get_indexed_directory_data(header, actual_index, total_directory_data);
                    var directory = new directory_data();
                    unpack_directory_data(new StreamPointer(total_directory_data, p), new[] { directory }, 1);

                    /* Find the flags that match.. */
                    if ((directory.entry_point_flags & type) != 0)
                    {
                        /* This one is valid! */
                        entry_point.level_number = actual_index;
                        strncpy(entry_point.level_name, directory.level_name, 66);

                        index = (short) (actual_index + 1);
                        success = true;
                        break; /* Out of the for loop */
                    }
                }
            }
            else
            {
                // Old style wad, find the index
                for (actual_index = index; !success && actual_index < header.wad_count; ++actual_index)
                {
                    wad_data wad;

                    /* Read the file */
                    wad = read_indexed_wad_from_file(MapFile, header, actual_index, true);
                    if (wad != null)
                    {
                        /* IF this has the proper type.. */
                        static_data map_info = read_map_info(wad);

                        fix_m1_entry_point_flags(header, map_info);

                        if ((map_info.entry_point_flags & type) != 0)
                        {
                            /* This one is valid! */
                            entry_point.level_number = actual_index;
                            assert(strlen(map_info.level_name) < LEVEL_NAME_LENGTH);
                            strncpy(entry_point.level_name, map_info.level_name, 66);

                            index = (short) (actual_index + 1);
                            success = true;
                        }

                        free_wad(wad);
                    }
                }
            }

            close_wad_file(MapFile);

            return success;
        }

        // Get vector of map entry points matching given type
        public static bool get_entry_points(FileSpecifier MapFileSpec, List<entry_point> vec, int type)
        {
            vec.Clear();

            // Open map file
            var MapFile = new OpenedFile();
            if (!open_wad_file_for_reading(MapFileSpec, MapFile))
                return false;

            // Read header
            var header = new wad_header();
            if (!read_wad_header(MapFile, header))
            {
                close_wad_file(MapFile);
                return false;
            }

            bool success = false;
            if (header.application_specific_directory_data_size == SIZEOF_directory_data)
            {
                // New style wad, read directory data
                byte[] total_directory_data = read_directory_data(MapFile, header);
                assert(total_directory_data != null);

                // Push matching directory entries into vector
                for (short i = 0; i < header.wad_count; i++)
                {
                    int p = get_indexed_directory_data(header, i, total_directory_data);
                    var directory = new directory_data();
                    unpack_directory_data(new StreamPointer(total_directory_data, p), new[] { directory }, 1);

                    if ((directory.entry_point_flags & type) != 0)
                    {
                        // This one is valid
                        var point = new entry_point();
                        point.level_number = i;
                        strncpy(point.level_name, directory.level_name, 66);
                        vec.Add(point);
                        success = true;
                    }
                }
            }
            else
            {
                // Old style wad
                for (short i = 0; i < header.wad_count; i++)
                {
                    wad_data wad = read_indexed_wad_from_file(MapFile, header, i, true);
                    if (wad == null)
                        continue;

                    // Read map_info data
                    static_data map_info = read_map_info(wad);

                    fix_m1_entry_point_flags(header, map_info);

                    if ((map_info.entry_point_flags & type) != 0)
                    {
                        // This one is valid
                        var point = new entry_point();
                        point.level_number = i;
                        assert(strlen(map_info.level_name) < LEVEL_NAME_LENGTH);
                        strncpy(point.level_name, map_info.level_name, 66);
                        vec.Add(point);
                        success = true;
                    }

                    free_wad(wad);
                }
            }

            close_wad_file(MapFile);

            return success;
        }

        // ForgePlus: every level's directory data, read as get_entry_points() reads it
        public static bool get_level_directory(FileSpecifier MapFileSpec, List<directory_data> levels)
        {
            levels.Clear();

            var MapFile = new OpenedFile();
            if (!open_wad_file_for_reading(MapFileSpec, MapFile))
                return false;

            var header = new wad_header();
            if (!read_wad_header(MapFile, header))
            {
                close_wad_file(MapFile);
                return false;
            }

            bool success = true;
            if (header.application_specific_directory_data_size == SIZEOF_directory_data)
            {
                byte[] total_directory_data = read_directory_data(MapFile, header);
                for (short i = 0; i < header.wad_count; i++)
                {
                    var directory = new directory_data();
                    unpack_directory_data(new StreamPointer(total_directory_data, get_indexed_directory_data(header, i, total_directory_data)), new[] { directory }, 1);
                    levels.Add(directory);
                }
            }
            else
            {
                for (short i = 0; i < header.wad_count && success; i++)
                {
                    wad_data wad = read_indexed_wad_from_file(MapFile, header, i, true);
                    if (wad == null)
                    {
                        success = false;
                        continue;
                    }

                    static_data map_info = read_map_info(wad);
                    fix_m1_entry_point_flags(header, map_info);
                    levels.Add(build_directory_data(map_info));

                    free_wad(wad);
                }
            }

            close_wad_file(MapFile);
            return success;
        }

        /* -------------------- Private or map editor functions */

        private static void initialize_map_for_new_level(MapLevel level)
        {
            level.static_world = new static_data();
            level.loaded_wad = new LoadedWad();
        }

        private static void allocate_map_for_counts(MapLevel level, int polygon_count, int side_count, int endpoint_count, int line_count)
        {
            // Most of the other stuff: reallocate here
            resize(level.EndpointList, endpoint_count, () => new endpoint_data());
            resize(level.LineList, line_count, () => new line_data());
            resize(level.SideList, side_count, () => new side_data());
            resize(level.PolygonList, polygon_count, () => new polygon_data());

            // Map indexes: start off with none of them (of course),
            // but reserve a size equal to the map index length
            level.MapIndexList.Clear();
            // dynamic_world->map_index_count= 0;
        }

        // std::vector::resize() with value-initialized (zeroed) new elements
        private static void resize<T>(List<T> list, int count, Func<T> make)
        {
            list.Clear();
            for (int i = 0; i < count; i++) list.Add(make());
        }

        private static void load_points(MapLevel level, byte[] points, int count)
        {
            int loop;

            // OK to modify input-data pointer since it's called by value
            var S = new StreamPointer(points);
            for (loop = 0; loop < count; ++loop)
            {
                endpoint_data endpoint = level.EndpointList[loop];
                StreamToValue(S, out endpoint.vertex.x);
                StreamToValue(S, out endpoint.vertex.y);
            }
            assert(count == (short) count);
            assert(0 <= (short) count);
            // dynamic_world->endpoint_count= count;
        }

        private static void load_lines(MapLevel level, byte[] lines, int count)
        {
            // assert(count>=0 && count<=MAXIMUM_LINES_PER_MAP);
            unpack_line_data(new StreamPointer(lines ?? new byte[0]), level.LineList, count);
            assert(count == (short) count);
            assert(0 <= (short) count);
            // dynamic_world->line_count= count;
        }

        private static void load_sides(MapLevel level, byte[] sides, int count, short version)
        {
            int loop;

            // assert(count>=0 && count<=MAXIMUM_SIDES_PER_MAP);

            unpack_side_data(new StreamPointer(sides ?? new byte[0]), level.SideList, count);

            if (version == MARATHON_ONE_DATA_VERSION)
            {
                for (loop = 0; loop < count; ++loop)
                {
                    // some editors set unused flags; clear them out
                    const int m1_side_flags_mask = 0x0007;

                    level.SideList[loop].transparent_texture.texture = UNONE;
                    level.SideList[loop].ambient_delta = 0;
                    level.SideList[loop].flags &= m1_side_flags_mask;
                    level.SideList[loop].flags |= _side_item_is_optional;
                }
            }
            else
            {
                bool editor_set_unused_flags = false;
                for (loop = 0; loop < count; ++loop)
                {
                    // some editors set unused flags; clear them out
                    if ((level.SideList[loop].flags & _reserved_side_flag) != 0)
                    {
                        editor_set_unused_flags = true;
                        break;
                    }
                }

                if (editor_set_unused_flags)
                {
                    for (loop = 0; loop < count; ++loop)
                    {
                        const int m2_side_flags_mask = 0x007f;
                        level.SideList[loop].flags &= m2_side_flags_mask;
                    }
                }
            }

            assert(count == (short) count);
            assert(0 <= (short) count);
            // dynamic_world->side_count= count;
        }

        private static void load_polygons(MapLevel level, byte[] polys, int count, short version)
        {
            int loop;

            // assert(count>=0 && count<=MAXIMUM_POLYGONS_PER_MAP);

            unpack_polygon_data(new StreamPointer(polys ?? new byte[0]), level.PolygonList, count);
            assert(count == (short) count);
            assert(0 <= (short) count);
            // dynamic_world->polygon_count= count;

            /* Allow for backward compatibility! */
            switch (version)
            {
                case MARATHON_ONE_DATA_VERSION:
                    for (loop = 0; loop < count; ++loop)
                    {
                        polygon_data polygon = level.PolygonList[loop];
                        polygon.media_index = NONE;
                        polygon.floor_origin.x = polygon.floor_origin.y = 0;
                        polygon.ceiling_origin.x = polygon.ceiling_origin.y = 0;

                        switch (polygon.type)
                        {
                            case _polygon_is_hill:
                                polygon.type = _polygon_is_minor_ouch;
                                break;
                            case _polygon_is_base:
                                polygon.type = _polygon_is_major_ouch;
                                break;
                            case _polygon_is_zone_border:
                                polygon.type = _polygon_is_glue;
                                break;
                            case _polygon_is_goal:
                                polygon.type = _polygon_is_glue_trigger;
                                break;
                            case _polygon_is_visible_monster_trigger:
                                polygon.type = _polygon_is_superglue;
                                break;
                            case _polygon_is_invisible_monster_trigger:
                                polygon.type = _polygon_must_be_explored;
                                break;
                            case _polygon_is_dual_monster_trigger:
                                polygon.type = _polygon_is_automatic_exit;
                                break;
                        }

                        // this is set on some m1 maps, but it's unknown what the flag
                        // does. Operating on the assumption that old m1 editors didn't
                        // clear out flags, just unset the flag. Otherwise the map will
                        // assert out later
                        polygon.flags &= unchecked((ushort) ~POLYGON_IS_DETACHED_BIT);
                    }
                    break;

                case MARATHON_TWO_DATA_VERSION:
                // LP addition:
                case MARATHON_INFINITY_DATA_VERSION:
                    break;

                default:
                    assert(false);
                    break;
            }
        }

        private static void load_lights(MapLevel level, byte[] _lights, int count, short version)
        {
            short loop, new_index;

            resize(level.LightList, count, () => new light_data()); // LightList.resize(count); objlist_clear(lights,count);
            // vassert(count>=0 && count<=MAXIMUM_LIGHTS_PER_MAP, csprintf(temporary, "Light count: %d vers: %d",
            // count, version));

            old_light_data[] OldLights;
            var S = new StreamPointer(_lights ?? new byte[0]);

            switch (version)
            {
                case MARATHON_ONE_DATA_VERSION:
                    {
                        // Unpack the old lights into a temporary array
                        OldLights = new old_light_data[count];
                        for (int i = 0; i < count; i++) OldLights[i] = new old_light_data();
                        unpack_old_light_data(S, OldLights, count);

                        for (loop = 0; loop < count; ++loop)
                        {
                            var TempLight = new static_light_data[1];
                            convert_old_light_data_to_new(TempLight, new[] { OldLights[loop] }, 1);

                            new_index = new_light(level, TempLight[0]);
                            assert(new_index == loop);
                        }
                        break;
                    }

                case MARATHON_TWO_DATA_VERSION:
                case MARATHON_INFINITY_DATA_VERSION:
                    // OK to modify the data pointer since it was passed by value
                    for (loop = 0; loop < count; ++loop)
                    {
                        var TempLight = new static_light_data();
                        unpack_static_light_data(S, new[] { TempLight }, 1);

                        new_index = new_light(level, TempLight);
                        assert(new_index == loop);
                    }
                    break;

                default:
                    assert(false);
                    break;
            }
        }

        private static void load_annotations(MapLevel level, byte[] annotations, int count)
        {
            // assert(count>=0 && count<=MAXIMUM_ANNOTATIONS_PER_MAP);
            resize(level.MapAnnotationList, count, () => new map_annotation());
            unpack_map_annotation(new StreamPointer(annotations ?? new byte[0]), level.MapAnnotationList, count);
            assert(count == (short) count);
            assert(0 <= (short) count);
            // dynamic_world->default_annotation_count= count;
        }

        private static void load_objects(MapLevel level, byte[] map_objects, int count, short version)
        {
            // assert(count>=0 && count<=MAXIMUM_SAVED_OBJECTS);
            resize(level.SavedObjectList, count, () => new map_object());
            unpack_map_object(new StreamPointer(map_objects ?? new byte[0]), level.SavedObjectList, count, version);
            assert(count == (short) count);
            assert(0 <= (short) count);
            // dynamic_world->initial_objects_count= count;
        }

        private static void load_map_info(MapLevel level, byte[] map_info)
        {
            unpack_static_data(new StreamPointer(map_info), new[] { level.static_world }, 1);
            level.static_world.ball_in_play = false;
        }

        private static void load_media(MapLevel level, byte[] _medias, int count)
        {
            int ii;

            level.MediaList.Clear(); // MediaList.resize(count); objlist_clear(medias,count);
            // assert(count>=0 && count<=MAXIMUM_MEDIAS_PER_MAP);

            var S = new StreamPointer(_medias ?? new byte[0]);
            for (ii = 0; ii < count; ++ii)
            {
                var TempMedia = new media_data();
                unpack_media_data(S, new[] { TempMedia }, 1);

                int new_index = new_media(level, TempMedia);
                assert(new_index == ii);
            }
        }

        private static void load_ambient_sound_images(MapLevel level, byte[] data, int count)
        {
            // assert(count>=0 &&count<=MAXIMUM_AMBIENT_SOUND_IMAGES_PER_MAP);
            resize(level.AmbientSoundImageList, count, () => new ambient_sound_image_data());
            unpack_ambient_sound_image_data(new StreamPointer(data ?? new byte[0]), level.AmbientSoundImageList, count);
            assert(count == (short) count);
            assert(0 <= (short) count);
            // dynamic_world->ambient_sound_image_count= count;
        }

        private static void load_terminal_data(MapLevel level, byte[] data, int length)
        {
            /* I would really like it if I could get these into computer_interface.c statically */
            computer_interface.unpack_map_terminal_data(level, new StreamPointer(data ?? new byte[0]), length);
        }

        private static void load_random_sound_images(MapLevel level, byte[] data, int count)
        {
            // assert(count>=0 &&count<=MAXIMUM_RANDOM_SOUND_IMAGES_PER_MAP);
            resize(level.RandomSoundImageList, count, () => new random_sound_image_data());
            unpack_random_sound_image_data(new StreamPointer(data ?? new byte[0]), level.RandomSoundImageList, count);
            assert(count == (short) count);
            assert(0 <= (short) count);
            // dynamic_world->random_sound_image_count= count;
        }

        /* Recalculate all the redundant crap- must be done before platforms/doors/etc.. */
        private static void recalculate_redundant_map(MapLevel level)
        {
            short loop;

            for (loop = 0; loop < level.PolygonList.Count; ++loop) recalculate_redundant_polygon_data(level, loop);
            for (loop = 0; loop < level.LineList.Count; ++loop) recalculate_redundant_line_data(level, loop);
            for (loop = 0; loop < level.EndpointList.Count; ++loop) recalculate_redundant_endpoint_data(level, loop);
        }

        /* -------- static functions */

        private static void scan_and_add_platforms(MapLevel level, byte[] platform_static_data, int count, short version)
        {
            short loop;

            level.PlatformList.Clear(); // PlatformList.resize(count); objlist_clear(platforms,count);

            resize(level.static_platforms, count, () => new static_platform_data());
            unpack_static_platform_data(new StreamPointer(platform_static_data ?? new byte[0]), level.static_platforms, count, version);

            for (loop = 0; loop < level.PolygonList.Count; ++loop)
            {
                polygon_data polygon = level.PolygonList[loop];
                if (polygon.type == _polygon_is_platform)
                {
                    /* Search and find the extra data.  If it is not there, use the permutation for */
                    /* backwards compatibility! */

                    int platform_static_data_index;
                    for (platform_static_data_index = 0; platform_static_data_index < count; ++platform_static_data_index)
                    {
                        if (level.static_platforms[platform_static_data_index].polygon_index == loop)
                        {
                            new_platform(level, level.static_platforms[platform_static_data_index], loop, count);
                            break;
                        }
                    }

                    /* DIdn't find it- use a standard platform */
                    if (platform_static_data_index == count)
                    {
                        polygon.permutation = 1;
                        new_platform(level, get_defaults_for_platform_type(polygon.permutation), loop, count);
                    }
                }
            }
        }

        // ForgePlus: extract_type_from_wad(), noting that the level models the chunk
        private static byte[] extract_modeled_type_from_wad(MapLevel level, wad_data wad, uint type, out int length)
        {
            byte[] data = extract_type_from_wad(wad, type, out length);
            if (data != null) level.loaded_wad.modeled_chunks.Add(type);
            return data;
        }

        /* Load a level from a wad-> mainly used by the net stuff. */
        public static bool process_map_wad(MapLevel level, wad_data wad, short version)
        {
            int data_length;
            byte[] data;
            int count;
            bool is_preprocessed_map = false;

            assert(version == MARATHON_INFINITY_DATA_VERSION || version == MARATHON_TWO_DATA_VERSION || version == MARATHON_ONE_DATA_VERSION);

            /* zero everything so no slots are used */
            initialize_map_for_new_level(level);
            level.loaded_wad.data_version = version;

            /* Calculate the length (for reallocate map) */
            allocate_map_structure_for_map(level, wad);

            /* Extract points */
            data = extract_type_from_wad(wad, POINT_TAG, out data_length);
            count = data_length / SIZEOF_world_point2d;
            assert(data_length == count * SIZEOF_world_point2d);

            if (count != 0)
            {
                level.loaded_wad.modeled_chunks.Add(POINT_TAG);
                load_points(level, data, count);
            }
            else
            {
                data = extract_modeled_type_from_wad(level, wad, ENDPOINT_DATA_TAG, out data_length);
                count = data_length / SIZEOF_endpoint_data;
                assert(data_length == count * SIZEOF_endpoint_data);
                // assert(count>=0 && count<MAXIMUM_ENDPOINTS_PER_MAP);

                /* Slam! */
                unpack_endpoint_data(new StreamPointer(data ?? new byte[0]), level.EndpointList, count);
                assert(count == (short) count);
                assert(0 <= (short) count);
                // dynamic_world->endpoint_count= count;

                if (version > MARATHON_ONE_DATA_VERSION)
                    is_preprocessed_map = true;
            }

            /* Extract lines */
            data = extract_modeled_type_from_wad(level, wad, LINE_TAG, out data_length);
            count = data_length / SIZEOF_line_data;
            assert(data_length == count * SIZEOF_line_data);
            load_lines(level, data, count);

            /* Order is important! */
            data = extract_modeled_type_from_wad(level, wad, SIDE_TAG, out data_length);
            count = data_length / SIZEOF_side_data;
            assert(data_length == count * SIZEOF_side_data);
            load_sides(level, data, count, version);

            /* Extract polygons */
            data = extract_modeled_type_from_wad(level, wad, POLYGON_TAG, out data_length);
            count = data_length / SIZEOF_polygon_data;
            assert(data_length == count * SIZEOF_polygon_data);
            load_polygons(level, data, count, version);

            /* Extract the lightsources */
            {
                /* When you are restoring a game, the actual light structure is set. */
                data = extract_modeled_type_from_wad(level, wad, LIGHTSOURCE_TAG, out data_length);
                if (version == MARATHON_ONE_DATA_VERSION)
                {
                    /* We have an old style light */
                    count = data_length / SIZEOF_old_light_data;
                    assert(count * SIZEOF_old_light_data == data_length);
                    load_lights(level, data, count, version);
                }
                else
                {
                    count = data_length / SIZEOF_static_light_data;
                    assert(count * SIZEOF_static_light_data == data_length);
                    load_lights(level, data, count, version);
                }

                // HACK!!!!!!!!!!!!!!! vulcan doesn't NONE .first_object field after adding scenery
                {
                    for (count = 0; count < level.PolygonList.Count; ++count)
                    {
                        level.PolygonList[count].first_object = NONE;
                    }
                }
            }

            /* Extract the annotations */
            data = extract_modeled_type_from_wad(level, wad, ANNOTATION_TAG, out data_length);
            count = data_length / SIZEOF_map_annotation;
            assert(data_length == count * SIZEOF_map_annotation);
            load_annotations(level, data, count);

            /* Extract the objects */
            data = extract_modeled_type_from_wad(level, wad, OBJECT_TAG, out data_length);
            count = data_length / SIZEOF_map_object;
            assert(data_length == count * SIZEOF_map_object);
            load_objects(level, data, count, version);

            /* Extract the map info data */
            data = extract_modeled_type_from_wad(level, wad, MAP_INFO_TAG, out data_length);
            // LP change: made this more Pfhorte-friendly
            assert(SIZEOF_static_data == data_length
                || SIZEOF_static_data - 2 == data_length);
            if (data_length < SIZEOF_static_data)
            {
                // ForgePlus: Aleph One reads the missing 2 bytes from past the end of the chunk
                var padded = new byte[SIZEOF_static_data];
                Array.Copy(data, padded, data_length);
                data = padded;
            }
            load_map_info(level, data);
            if (version == MARATHON_ONE_DATA_VERSION)
            {
                static_data static_world = level.static_world;
                if ((static_world.mission_flags & _mission_exploration) != 0)
                {
                    static_world.mission_flags &= ~_mission_exploration;
                    static_world.mission_flags |= _mission_exploration_m1;
                }
                if ((static_world.mission_flags & _mission_rescue) != 0)
                {
                    static_world.mission_flags &= ~_mission_rescue;
                    static_world.mission_flags |= _mission_rescue_m1;
                }
                if ((static_world.mission_flags & _mission_repair) != 0)
                {
                    static_world.mission_flags &= ~_mission_repair;
                    static_world.mission_flags |= _mission_repair_m1;
                }
                if ((static_world.environment_flags & _environment_rebellion) != 0)
                {
                    static_world.environment_flags &= ~_environment_rebellion;
                    static_world.environment_flags |= _environment_rebellion_m1;
                }
                static_world.environment_flags |= _environment_glue_m1 | _environment_ouch_m1 | _environment_song_index_m1 | _environment_terminals_stop_time | _environment_activation_ranges | _environment_m1_weapons;
            }

            // if (static_world->environment_flags & _environment_song_index_m1) Music::instance()->SetClassicLevelMusic(static_world->song_index);

            /* Extract the game difficulty info.. */
            data = extract_modeled_type_from_wad(level, wad, ITEM_PLACEMENT_STRUCTURE_TAG, out data_length);
            // In case of an absent placement chunk...
            if (data_length == 0)
            {
                data = new byte[2 * MAXIMUM_OBJECT_TYPES * SIZEOF_object_frequency_definition];
            }
            else
                assert(data_length == 2 * MAXIMUM_OBJECT_TYPES * SIZEOF_object_frequency_definition);
            load_placement_data(level, new StreamPointer(data, MAXIMUM_OBJECT_TYPES * SIZEOF_object_frequency_definition), new StreamPointer(data));

            /* Extract the terminal data. */
            data = extract_modeled_type_from_wad(level, wad, TERMINAL_DATA_TAG, out data_length);
            load_terminal_data(level, data, data_length);

            /* Extract the media definitions */
            {
                data = extract_modeled_type_from_wad(level, wad, MEDIA_TAG, out data_length);
                count = data_length / SIZEOF_media_data;
                assert(count * SIZEOF_media_data == data_length);
                load_media(level, data, count);
            }

            /* Extract the ambient sound images */
            data = extract_modeled_type_from_wad(level, wad, AMBIENT_SOUND_TAG, out data_length);
            count = data_length / SIZEOF_ambient_sound_image_data;
            assert(data_length == count * SIZEOF_ambient_sound_image_data);
            load_ambient_sound_images(level, data, count);
            load_ambient_sound_images(level, data, data_length / SIZEOF_ambient_sound_image_data);

            /* Extract the random sound images */
            data = extract_modeled_type_from_wad(level, wad, RANDOM_SOUND_TAG, out data_length);
            count = data_length / SIZEOF_random_sound_image_data;
            assert(data_length == count * SIZEOF_random_sound_image_data);
            load_random_sound_images(level, data, count);

            // Not ported: extracting the embedded shapes and sounds, MMLS, LUAS and saved Lua state

            // LP addition: load the physics-model chunks (all fixed-size): see process_map_wad_physics()

            // RunScriptChunks();

            // init_ephemera(dynamic_world->polygon_count);

            // PolygonListCopy = PolygonList; // must be done before polygons heights are modified below

            /* If we are restoring the game, then we need to add the dynamic data */
            {
                byte[] map_index_data;
                int map_index_count;
                byte[] platform_structures;
                int platform_structure_count;

                if (version == MARATHON_ONE_DATA_VERSION)
                {
                    /* Force precalculation */
                    map_index_data = null;
                    map_index_count = 0;
                }
                else
                {
                    map_index_data = extract_modeled_type_from_wad(level, wad, MAP_INDEXES_TAG, out data_length);
                    map_index_count = data_length / sizeof(short);
                    assert(map_index_count * sizeof(short) == data_length);
                }

                assert((is_preprocessed_map && map_index_count != 0) || (!is_preprocessed_map && map_index_count == 0));

                data = extract_modeled_type_from_wad(level, wad, PLATFORM_STATIC_DATA_TAG, out data_length);
                count = data_length / SIZEOF_static_platform_data;
                assert(count * SIZEOF_static_platform_data == data_length);

                platform_structures = extract_type_from_wad(wad, PLATFORM_STRUCTURE_TAG, out data_length);
                platform_structure_count = data_length / SIZEOF_platform_data;
                assert(platform_structure_count * SIZEOF_platform_data == data_length);
                // ForgePlus: complete_loading_level() only loads 'PLAT' when there's no 'plat'
                if (data == null && platform_structures != null) level.loaded_wad.modeled_chunks.Add(PLATFORM_STRUCTURE_TAG);

                complete_loading_level(level, map_index_data, map_index_count,
                    data, count, platform_structures,
                    platform_structure_count, version);
            }

            // PlatformListCopy = PlatformList;

            // ForgePlus: record the wad for save_level()
            for (short index = 0; index < wad.tag_count; ++index)
            {
                tag_data tag = wad.tag_data[index];
                level.loaded_wad.chunk_order.Add(tag.tag);
                if (level.loaded_wad.preserved_chunks.ContainsKey(tag.tag) || level.loaded_wad.chunk_bytes.ContainsKey(tag.tag)) continue;

                var bytes = new byte[tag.length];
                Array.Copy(tag.data, bytes, tag.length);
                if (level.loaded_wad.modeled_chunks.Contains(tag.tag))
                {
                    level.loaded_wad.chunk_bytes[tag.tag] = bytes;
                    if (writes_as_modeled(level, tag.tag))
                    {
                        byte[] packing = level_chunk_data(level, tag.tag, out int size);
                        level.loaded_wad.chunk_packing[tag.tag] = packing ?? new byte[0];
                    }
                }
                else
                {
                    level.loaded_wad.preserved_chunks[tag.tag] = bytes;
                }
            }

            /* ... and bail */
            return true;
        }

        // ForgePlus: the physics section of process_map_wad(), loading into model. Returns false, leaving model
        // untouched, if the level has no physics: Aleph One then calls import_definition_structures(), whose
        // result for the physics file the caller already has.
        public static bool process_map_wad_physics(wad_data wad, PhysicsModel model)
        {
            int data_length;

            // LP addition: load the physics-model chunks (all fixed-size)
            bool PhysicsModelLoaded = false;

            // Aleph One repeats import_physics_wad_data() here, calling init_physics_wad_data() at the first chunk
            foreach (uint tag in new uint[] { MONSTER_PHYSICS_TAG, EFFECTS_PHYSICS_TAG, PROJECTILE_PHYSICS_TAG, PHYSICS_PHYSICS_TAG, WEAPONS_PHYSICS_TAG })
            {
                extract_type_from_wad(wad, tag, out data_length);
                if (data_length > 0) PhysicsModelLoaded = true;
            }

            if (PhysicsModelLoaded)
            {
                init_physics_wad_data(model);
                import_physics_wad_data(model, wad);
            }

            // ghs: always reload the physics model if there isn't one merged
            // if (!PhysicsModelLoaded && !game_is_networked) import_definition_structures();

            return PhysicsModelLoaded;
        }

        private static void allocate_map_structure_for_map(MapLevel level, wad_data wad)
        {
            int data_length;
            int line_count, polygon_count, side_count, endpoint_count;

            /* Extract points */
            extract_type_from_wad(wad, POINT_TAG, out data_length);
            endpoint_count = data_length / SIZEOF_world_point2d;
            if (endpoint_count * SIZEOF_world_point2d != data_length) alert_corrupted_map(0x7074); // 'pt'

            if (endpoint_count == 0)
            {
                extract_type_from_wad(wad, ENDPOINT_DATA_TAG, out data_length);
                endpoint_count = data_length / SIZEOF_endpoint_data;
                if (endpoint_count * SIZEOF_endpoint_data != data_length) alert_corrupted_map(0x6570); // 'ep'
            }

            /* Extract lines */
            extract_type_from_wad(wad, LINE_TAG, out data_length);
            line_count = data_length / SIZEOF_line_data;
            if (line_count * SIZEOF_line_data != data_length) alert_corrupted_map(0x6c69); // 'li'

            /* Sides.. */
            extract_type_from_wad(wad, SIDE_TAG, out data_length);
            side_count = data_length / SIZEOF_side_data;
            if (side_count * SIZEOF_side_data != data_length) alert_corrupted_map(0x7369); // 'si'

            /* Extract polygons */
            extract_type_from_wad(wad, POLYGON_TAG, out data_length);
            polygon_count = data_length / SIZEOF_polygon_data;
            if (polygon_count * SIZEOF_polygon_data != data_length) alert_corrupted_map(0x7369); // 'si'

            allocate_map_for_counts(level, polygon_count, side_count, endpoint_count, line_count);
        }

        /* Note that we assume the redundant data has already been recalculated... */
        private static void load_redundant_map_data(MapLevel level, byte[] redundant_data, int count)
        {
            if (redundant_data != null)
            {
                // assert(redundant_data && map_indexes);
                var Stream = new StreamPointer(redundant_data);
                var map_indexes = new short[count];
                StreamToList(Stream, map_indexes, count);
                level.MapIndexList.Clear();
                level.MapIndexList.AddRange(map_indexes);
                assert(count == (short) count);
                assert(0 <= (short) count);
                // dynamic_world->map_index_count= count;
            }
            else
            {
                recalculate_redundant_map(level);
                precalculate_map_indexes(level);
            }
        }

        /* -------- saving */

        private struct save_game_data
        {
            public uint tag;
            public short unit_size;
            public bool loaded_by_level;

            public save_game_data(uint tag, int unit_size, bool loaded_by_level)
            {
                this.tag = tag;
                this.unit_size = (short) unit_size;
                this.loaded_by_level = loaded_by_level;
            }
        }

        private static readonly save_game_data[] export_data = new save_game_data[]
        {
            new save_game_data(POINT_TAG, SIZEOF_world_point2d, true),
            new save_game_data(LINE_TAG, SIZEOF_line_data, true),
            new save_game_data(POLYGON_TAG, SIZEOF_polygon_data, true),
            new save_game_data(SIDE_TAG, SIZEOF_side_data, true),
            new save_game_data(LIGHTSOURCE_TAG, SIZEOF_static_light_data, true),
            new save_game_data(ANNOTATION_TAG, SIZEOF_map_annotation, true),
            new save_game_data(OBJECT_TAG, SIZEOF_map_object, true),
            new save_game_data(MAP_INFO_TAG, SIZEOF_static_data, true),
            new save_game_data(ITEM_PLACEMENT_STRUCTURE_TAG, SIZEOF_object_frequency_definition, true),
            new save_game_data(PLATFORM_STATIC_DATA_TAG, SIZEOF_static_platform_data, true),
            new save_game_data(TERMINAL_DATA_TAG, sizeof(byte), true),
            new save_game_data(MEDIA_TAG, SIZEOF_media_data, true), // false },
            new save_game_data(AMBIENT_SOUND_TAG, SIZEOF_ambient_sound_image_data, true),
            new save_game_data(RANDOM_SOUND_TAG, SIZEOF_random_sound_image_data, true),
            new save_game_data(SHAPE_PATCH_TAG, sizeof(byte), true),
            new save_game_data(SOUND_PATCH_TAG, sizeof(byte), true),
            // { PLATFORM_STRUCTURE_TAG, SIZEOF_platform_data, true },
        };

        // Without the running game's chunks; the physics chunks are copied as bytes
        private static readonly save_game_data[] save_data = new save_game_data[]
        {
            new save_game_data(ENDPOINT_DATA_TAG, SIZEOF_endpoint_data, true),
            new save_game_data(LINE_TAG, SIZEOF_line_data, true),
            new save_game_data(SIDE_TAG, SIZEOF_side_data, true),
            new save_game_data(POLYGON_TAG, SIZEOF_polygon_data, true),
            new save_game_data(LIGHTSOURCE_TAG, SIZEOF_light_data, true), // false },
            new save_game_data(ANNOTATION_TAG, SIZEOF_map_annotation, true),
            new save_game_data(OBJECT_TAG, SIZEOF_map_object, true),
            new save_game_data(MAP_INFO_TAG, SIZEOF_static_data, true),
            new save_game_data(ITEM_PLACEMENT_STRUCTURE_TAG, SIZEOF_object_frequency_definition, true),
            new save_game_data(MEDIA_TAG, SIZEOF_media_data, true), // false },
            new save_game_data(AMBIENT_SOUND_TAG, SIZEOF_ambient_sound_image_data, true),
            new save_game_data(RANDOM_SOUND_TAG, SIZEOF_random_sound_image_data, true),
            new save_game_data(TERMINAL_DATA_TAG, sizeof(byte), true),

            // LP addition: handling of physics models
            new save_game_data(MONSTER_PHYSICS_TAG, sizeof(byte), true),
            new save_game_data(EFFECTS_PHYSICS_TAG, sizeof(byte), true),
            new save_game_data(PROJECTILE_PHYSICS_TAG, sizeof(byte), true),
            new save_game_data(PHYSICS_PHYSICS_TAG, sizeof(byte), true),
            new save_game_data(WEAPONS_PHYSICS_TAG, sizeof(byte), true),

            // GHS: save the new embedded shapes
            new save_game_data(SHAPE_PATCH_TAG, sizeof(byte), true),
            new save_game_data(SOUND_PATCH_TAG, sizeof(byte), true),

            new save_game_data(MMLS_TAG, sizeof(byte), true),
            new save_game_data(LUAS_TAG, sizeof(byte), true),

            new save_game_data(MAP_INDEXES_TAG, sizeof(short), true), // false },
            new save_game_data(PLATFORM_STRUCTURE_TAG, SIZEOF_platform_data, true), // false },
        };

        private static byte[] export_tag_to_global_array_and_size(MapLevel level, uint tag, out int size)
        {
            byte[] array = null;
            int unit_size = 0;
            int count = 0;
            int index;

            for (index = 0; index < export_data.Length; ++index)
            {
                if (export_data[index].tag == tag)
                {
                    unit_size = export_data[index].unit_size;
                    break;
                }
            }
            assert(index != export_data.Length);

            switch (tag)
            {
                case POINT_TAG:
                    count = level.EndpointList.Count;
                    break;

                case LIGHTSOURCE_TAG:
                    count = recalculate_map_counts(level);
                    break;

                case PLATFORM_STATIC_DATA_TAG:
                    count = level.PlatformList.Count;
                    break;

                case POLYGON_TAG:
                    count = level.PolygonList.Count;
                    break;

                default:
                    assert(false);
                    break;
            }

            // Allocate a temporary packed-data chunk;
            // indicate if there is nothing to be written
            size = count * unit_size;
            if (size > 0)
                array = new byte[size];
            else
                return null;

            // objlist_clear(array, *size);

            // An OK-to-alter version of that array pointer
            var temp_array = new StreamPointer(array);

            switch (tag)
            {
                case POINT_TAG:
                    for (int loop = 0; loop < count; ++loop)
                    {
                        world_point2d vertex = level.EndpointList[loop].vertex;
                        ValueToStream(temp_array, vertex.x);
                        ValueToStream(temp_array, vertex.y);
                    }
                    break;

                case LIGHTSOURCE_TAG:
                    for (int loop = 0; loop < count; ++loop)
                    {
                        pack_static_light_data(temp_array, new[] { level.LightList[loop].static_data }, 1);
                    }
                    break;

                case PLATFORM_STATIC_DATA_TAG:
                    if (level.static_platforms.Count == count)
                    {
                        // export them directly as they came in
                        pack_static_platform_data(new StreamPointer(array), level.static_platforms, count);
                    }
                    else
                    {
                        for (int loop = 0; loop < count; ++loop)
                        {
                            platform_data p = level.PlatformList[loop];

                            // ForgePlus: shared with editing
                            static_platform_data platform = static_platform_data_from_platform(p);

                            pack_static_platform_data(temp_array, new[] { platform }, 1);
                        }
                    }
                    break;

                case POLYGON_TAG:
                    for (int loop = 0; loop < count; ++loop)
                    {
                        // Forge visual mode crashes if we don't do this
                        polygon_data polygon = level.PolygonList[loop].Clone();
                        polygon.first_object = NONE;
                        pack_polygon_data(temp_array, new[] { polygon }, 1);
                    }
                    break;

                default:
                    assert(false);
                    break;
            }

            return array;
        }

        /* the sizes are the sizes to save in the file, be aware! */
        private static byte[] tag_to_global_array_and_size(MapLevel level, uint tag, out int size)
        {
            byte[] array = null;
            int unit_size = 0;
            int count = 0;
            int index;
            byte[] preserved = null;

            for (index = 0; index < save_data.Length; ++index)
            {
                if (save_data[index].tag == tag)
                {
                    unit_size = save_data[index].unit_size;
                    break;
                }
            }
            assert(index != save_data.Length);

            // LP: had fixed off-by-one error in medias saving,
            // and had added physics-model saving

            switch (tag)
            {
                case ENDPOINT_DATA_TAG:
                    count = level.EndpointList.Count;
                    break;
                case LINE_TAG:
                    count = level.LineList.Count;
                    break;
                case SIDE_TAG:
                    count = level.SideList.Count;
                    break;
                case POLYGON_TAG:
                    count = level.PolygonList.Count;
                    break;
                case LIGHTSOURCE_TAG:
                    count = recalculate_map_counts(level);
                    break;
                case ANNOTATION_TAG:
                    count = level.MapAnnotationList.Count;
                    break;
                case OBJECT_TAG:
                    count = level.SavedObjectList.Count;
                    break;
                case MAP_INFO_TAG:
                    count = 1;
                    break;
                case MAP_INDEXES_TAG:
                    count = unchecked((ushort) level.MapIndexList.Count);
                    break;
                case MEDIA_TAG:
                    count = count_number_of_medias_used(level);
                    break;
                case ITEM_PLACEMENT_STRUCTURE_TAG:
                    count = 2 * MAXIMUM_OBJECT_TYPES;
                    break;
                case PLATFORM_STRUCTURE_TAG:
                    count = level.PlatformList.Count;
                    break;
                case AMBIENT_SOUND_TAG:
                    count = level.AmbientSoundImageList.Count;
                    break;
                case RANDOM_SOUND_TAG:
                    count = level.RandomSoundImageList.Count;
                    break;
                case TERMINAL_DATA_TAG:
                    count = computer_interface.calculate_packed_terminal_data_length(level);
                    break;
                case MONSTER_PHYSICS_TAG:
                case EFFECTS_PHYSICS_TAG:
                case PROJECTILE_PHYSICS_TAG:
                case PHYSICS_PHYSICS_TAG:
                case WEAPONS_PHYSICS_TAG:
                case SHAPE_PATCH_TAG:
                case SOUND_PATCH_TAG:
                case MMLS_TAG:
                case LUAS_TAG:
                    // ForgePlus: the chunk as it was loaded
                    level.loaded_wad.preserved_chunks.TryGetValue(tag, out preserved);
                    count = preserved != null ? preserved.Length / unit_size : 0;
                    break;
                default:
                    assert(false);
                    break;
            }

            // Allocate a temporary packed-data chunk;
            // indicate if there is nothing to be written
            size = count * unit_size;
            if (size > 0)
                array = new byte[size];
            else
                return null;

            // objlist_clear(array, *size);

            // An OK-to-alter version of that array pointer
            var temp_array = new StreamPointer(array);

            switch (tag)
            {
                case ENDPOINT_DATA_TAG:
                    pack_endpoint_data(temp_array, level.EndpointList, count);
                    break;
                case LINE_TAG:
                    pack_line_data(temp_array, level.LineList, count);
                    break;
                case SIDE_TAG:
                    pack_side_data(temp_array, level.SideList, count);
                    break;
                case POLYGON_TAG:
                    pack_polygon_data(temp_array, level.PolygonList, count);
                    break;
                case LIGHTSOURCE_TAG:
                    pack_light_data(temp_array, level.LightList, count);
                    break;
                case ANNOTATION_TAG:
                    pack_map_annotation(temp_array, level.MapAnnotationList, count);
                    break;
                case OBJECT_TAG:
                    pack_map_object(temp_array, level.SavedObjectList, count);
                    break;
                case MAP_INFO_TAG:
                    pack_static_data(temp_array, new[] { level.static_world }, count);
                    break;
                case MAP_INDEXES_TAG:
                    ListToStream(temp_array, level.MapIndexList.ToArray(), count); // E-Z packing here...
                    break;
                case MEDIA_TAG:
                    pack_media_data(temp_array, level.MediaList, count);
                    break;
                case ITEM_PLACEMENT_STRUCTURE_TAG:
                    pack_object_frequency_definition(temp_array, get_placement_info(level), count);
                    break;
                case PLATFORM_STRUCTURE_TAG:
                    pack_platform_data(temp_array, level.PlatformList, count);
                    break;
                case AMBIENT_SOUND_TAG:
                    pack_ambient_sound_image_data(temp_array, level.AmbientSoundImageList, count);
                    break;
                case RANDOM_SOUND_TAG:
                    pack_random_sound_image_data(temp_array, level.RandomSoundImageList, count);
                    break;
                case TERMINAL_DATA_TAG:
                    computer_interface.pack_map_terminal_data(level, temp_array, count);
                    break;
                case MONSTER_PHYSICS_TAG:
                case EFFECTS_PHYSICS_TAG:
                case PROJECTILE_PHYSICS_TAG:
                case PHYSICS_PHYSICS_TAG:
                case WEAPONS_PHYSICS_TAG:
                case SHAPE_PATCH_TAG:
                case SOUND_PATCH_TAG:
                case MMLS_TAG:
                case LUAS_TAG:
                    Array.Copy(preserved, array, size);
                    break;
                default:
                    assert(false);
                    break;
            }

            return array;
        }

        private static wad_data build_export_wad(MapLevel level, wad_header header, out int length)
        {
            wad_data wad = null;
            byte[] array_to_slam;
            int size;

            length = 0;

            wad = create_empty_wad();
            if (wad != null)
            {
                // recalculate_map_counts();

                // try to divine initial platform/polygon states
                var SavedPlatforms = level.PlatformList.ConvertAll(p => p.Clone());
                var SavedPolygons = level.PolygonList.ConvertAll(p => p.Clone());
                var SavedLines = level.LineList.ConvertAll(l => l.Clone());
                var SavedSides = level.SideList.ConvertAll(s => s.Clone());

                for (int loop = 0; loop < level.PlatformList.Count; ++loop)
                {
                    platform_data platform = level.PlatformList[loop];

                    if (PLATFORM_COMES_FROM_CEILING(platform))
                    {
                        short new_ceiling_height = PLATFORM_IS_INITIALLY_EXTENDED(platform) ? platform.minimum_ceiling_height : platform.maximum_ceiling_height;
                        adjust_platform_sides(level, platform, platform.ceiling_height, new_ceiling_height);
                    }

                    // platform->floor_height = original_platform->floor_height; ...
                    // PolygonList[platform->polygon_index].floor_height = PolygonListCopy[platform->polygon_index].floor_height; ...
                }

                for (int loop = 0; loop < level.LineList.Count; ++loop)
                {
                    line_data line = level.LineList[loop];
                    if (LINE_IS_VARIABLE_ELEVATION(line))
                    {
                        SET_LINE_VARIABLE_ELEVATION(line, false);
                        SET_LINE_SOLIDITY(line, false);
                        SET_LINE_TRANSPARENCY(line, true);
                    }
                }

                for (int loop = 0; loop < export_data.Length; ++loop)
                {
                    /* If there is a conversion function, let it handle it */
                    switch (export_data[loop].tag)
                    {
                        case POINT_TAG:
                        case LIGHTSOURCE_TAG:
                        case PLATFORM_STATIC_DATA_TAG:
                        case POLYGON_TAG:
                            array_to_slam = export_tag_to_global_array_and_size(level, export_data[loop].tag, out size);
                            break;
                        default:
                            array_to_slam = tag_to_global_array_and_size(level, export_data[loop].tag, out size);
                            break;
                    }

                    /* Add it to the wad.. */
                    if (size != 0)
                    {
                        wad = append_data_to_wad(wad, export_data[loop].tag, array_to_slam, size, 0);
                    }
                }

                level.PlatformList = SavedPlatforms;
                level.PolygonList = SavedPolygons;
                level.LineList = SavedLines;
                level.SideList = SavedSides;

                if (wad != null) length = calculate_wad_length(header, wad);
            }

            return wad;
        }

        public static bool export_level(FileSpecifier File, MapLevel level)
        {
            var header = new wad_header();
            short err = 0;
            bool success = false;
            int offset, wad_length;
            wad_data wad;

            clear_game_error();

            var TempFile = new FileSpecifier();
            TempFile.SetTempName(File);

            /* Fill in the default wad header (we are using File instead of TempFile to get the name right in the header) */
            fill_default_wad_header(File, CURRENT_WADFILE_VERSION, MARATHON_TWO_DATA_VERSION, 1, 0, header);

            if (create_wadfile(TempFile))
            {
                var SaveFile = new OpenedFile();
                if (open_wad_file_for_writing(TempFile, SaveFile))
                {
                    /* Write out the new header */
                    if (write_wad_header(SaveFile, header))
                    {
                        offset = SIZEOF_wad_header;

                        wad = build_export_wad(level, header, out wad_length);
                        if (wad != null)
                        {
                            var entry = new byte[get_size_of_directory_data(header)];
                            set_indexed_directory_offset_and_length(header, entry, 0, offset, wad_length, 0);

                            if (write_wad(SaveFile, header, wad, offset))
                            {
                                /* Update the new header */
                                offset += wad_length;
                                header.directory_offset = offset;
                                if (write_wad_header(SaveFile, header) && write_directorys(SaveFile, header, entry))
                                {
                                    /* We win. */
                                    success = true;
                                }
                            }

                            free_wad(wad);
                        }
                    }

                    err = (short) SaveFile.GetError();
                    calculate_and_store_wadfile_checksum(SaveFile);
                    close_wad_file(SaveFile);
                }

                if (err == 0)
                {
                    if (!TempFile.Rename(File))
                    {
                        err = 1;
                    }
                }
            }

            if (err != 0 || error_pending())
            {
                success = false;
            }

            return success;
        }

        public static void level_has_embedded_physics_lua(FileSpecifier MapFileSpec, int Level, out bool HasPhysics, out bool HasLua)
        {
            HasPhysics = false;
            HasLua = false;

            // load the wad file and look for chunks !!??
            var header = new wad_header();
            wad_data wad;
            var MapFile = new OpenedFile();
            if (open_wad_file_for_reading(MapFileSpec, MapFile))
            {
                if (read_wad_header(MapFile, header))
                {
                    wad = read_indexed_wad_from_file(MapFile, header, (short) Level, true);
                    if (wad != null)
                    {
                        int data_length;
                        extract_type_from_wad(wad, PHYSICS_PHYSICS_TAG, out data_length);
                        HasPhysics = data_length > 0;

                        extract_type_from_wad(wad, LUAS_TAG, out data_length);
                        HasLua = data_length > 0;
                        free_wad(wad);
                    }
                }
                close_wad_file(MapFile);
            }
        }

        /* -------- ForgePlus: saving a level whole */

        // Whether save_level() packs the chunk from the level. Marathon 1 levels are saved as Marathon 2 data,
        // as export_level() saves them: with 'PNTS', and without the Marathon 1 'iidx', which Marathon 2 data
        // would take for precalculated map indexes.
        private static bool writes_as_modeled(MapLevel level, uint tag)
        {
            if (level.loaded_wad.data_version == MARATHON_ONE_DATA_VERSION)
            {
                if (tag == ENDPOINT_DATA_TAG || tag == MAP_INDEXES_TAG) return false;
                if (tag == POINT_TAG) return true;
            }
            return level.loaded_wad.modeled_chunks.Contains(tag);
        }

        // A chunk's data as build_export_wad() gets it
        private static byte[] level_chunk_data(MapLevel level, uint tag, out int size)
        {
            switch (tag)
            {
                case POINT_TAG:
                case LIGHTSOURCE_TAG:
                case PLATFORM_STATIC_DATA_TAG:
                case POLYGON_TAG:
                    return export_tag_to_global_array_and_size(level, tag, out size);
                default:
                    return tag_to_global_array_and_size(level, tag, out size);
            }
        }

        // The level's chunks in their loaded order, those it models packed from it and the rest as loaded,
        // then any new modeled chunks in export order. With keep_unchanged_chunks, a chunk that packs as it
        // did after loading is written as loaded, so an unedited level is saved byte for byte (packing zeroes
        // unused fields, and loading applies fixes that Aleph One applies again on every load). Chunks with an
        // excluded tag (such as the physics chunks) are left out.
        public static wad_data build_level_wad(MapLevel level, wad_header header, out int length, bool keep_unchanged_chunks = true,
            ICollection<uint> excluded_tags = null)
        {
            // Marathon 1 chunks aren't Marathon 2 data
            if (level.loaded_wad.data_version == MARATHON_ONE_DATA_VERSION) keep_unchanged_chunks = false;

            wad_data wad = create_empty_wad();
            var written = new HashSet<uint>();

            void add(uint tag, byte[] array, int size)
            {
                written.Add(tag);
                if (size != 0) wad = append_data_to_wad(wad, tag, array, size, 0);
            }

            foreach (uint tag in level.loaded_wad.chunk_order)
            {
                if (written.Contains(tag)) continue; // a wad can't hold a tag twice
                if (excluded_tags != null && excluded_tags.Contains(tag)) continue;

                if (writes_as_modeled(level, tag))
                {
                    byte[] array = level_chunk_data(level, tag, out int size);
                    if (keep_unchanged_chunks && level.loaded_wad.chunk_packing.TryGetValue(tag, out byte[] packing) &&
                        (array ?? new byte[0]).SequenceEqual(packing))
                    {
                        byte[] original = level.loaded_wad.chunk_bytes[tag];
                        add(tag, original, original.Length);
                    }
                    else
                    {
                        add(tag, array, size);
                    }
                }
                else if (level.loaded_wad.preserved_chunks.TryGetValue(tag, out byte[] preserved) &&
                    !(level.loaded_wad.data_version == MARATHON_ONE_DATA_VERSION && (tag == ENDPOINT_DATA_TAG || tag == MAP_INDEXES_TAG)))
                {
                    add(tag, preserved, preserved.Length);
                }
            }

            uint endpoint_tag = writes_as_modeled(level, ENDPOINT_DATA_TAG) ? ENDPOINT_DATA_TAG : POINT_TAG;
            uint platform_tag = writes_as_modeled(level, PLATFORM_STRUCTURE_TAG) ? PLATFORM_STRUCTURE_TAG : PLATFORM_STATIC_DATA_TAG;
            uint[] new_tags = new uint[]
            {
                endpoint_tag, LINE_TAG, POLYGON_TAG, SIDE_TAG, LIGHTSOURCE_TAG, ANNOTATION_TAG, OBJECT_TAG, MAP_INFO_TAG,
                ITEM_PLACEMENT_STRUCTURE_TAG, platform_tag, MEDIA_TAG, AMBIENT_SOUND_TAG, RANDOM_SOUND_TAG
            };
            foreach (uint tag in new_tags)
            {
                if (written.Contains(tag)) continue;
                if (excluded_tags != null && excluded_tags.Contains(tag)) continue;
                byte[] array = level_chunk_data(level, tag, out int size);
                add(tag, array, size);
            }

            length = calculate_wad_length(header, wad);
            return wad;
        }

        // The directory data Marathon 2 and Infinity map files have for each level, which Aleph One only reads
        public static directory_data build_directory_data(static_data static_world)
        {
            var directory = new directory_data();
            directory.mission_flags = static_world.mission_flags;
            directory.environment_flags = static_world.environment_flags;
            directory.entry_point_flags = unchecked((int) static_world.entry_point_flags);
            Array.Copy(static_world.level_name, directory.level_name, LEVEL_NAME_LENGTH);
            return directory;
        }

        // The packed directory data to save: existing while it matches the level's map info (so stray bytes
        // after the name are kept), else build_directory_data()
        public static byte[] directory_data_bytes(MapLevel level, byte[] existing)
        {
            var directory = build_directory_data(level.static_world);
            if (existing != null)
            {
                var old = new directory_data();
                unpack_directory_data(new StreamPointer(existing), new[] { old }, 1);
                int n = strlen(directory.level_name);
                bool same_name = n == strlen(old.level_name);
                for (int i = 0; same_name && i < n; i++) same_name = directory.level_name[i] == old.level_name[i];
                if (same_name && old.mission_flags == directory.mission_flags && old.environment_flags == directory.environment_flags &&
                    old.entry_point_flags == directory.entry_point_flags)
                {
                    return (byte[]) existing.Clone();
                }
            }

            var bytes = new byte[SIZEOF_directory_data];
            pack_directory_data(new StreamPointer(bytes), new[] { directory }, 1);
            return bytes;
        }

        // Marathon 1 levels are converted to Marathon 2 data on loading, and saved as such
        public static short level_data_version_for_saving(MapLevel level)
        {
            return level.loaded_wad.data_version == MARATHON_ONE_DATA_VERSION ? MARATHON_TWO_DATA_VERSION : level.loaded_wad.data_version;
        }

        // export_level(), writing build_level_wad() and, with with_directory_data, the level's directory data
        // ForgePlus: excluded_tags, as for build_level_wad()
        public static bool save_level(FileSpecifier File, MapLevel level, bool with_directory_data = true,
            ICollection<uint> excluded_tags = null)
        {
            return save_levels(File, new[] { level }, with_directory_data, excluded_tags: excluded_tags);
        }

        // ForgePlus: save_level() for several levels, in order (the data version is the first's). A given file_name or
        // checksum replaces File's name or the calculated one (so saved games and films still find the map).
        public static bool save_levels(FileSpecifier File, IList<MapLevel> levels, bool with_directory_data = true,
            byte[] file_name = null, uint? checksum = null, ICollection<uint> excluded_tags = null)
        {
            var header = new wad_header();
            short err = 0;
            bool success = false;
            int offset, wad_length;
            wad_data wad;

            clear_game_error();

            var TempFile = new FileSpecifier();
            TempFile.SetTempName(File);

            /* Fill in the default wad header (we are using File instead of TempFile to get the name right in the header) */
            fill_default_wad_header(File, CURRENT_WADFILE_VERSION, level_data_version_for_saving(levels[0]), (short) levels.Count,
                (short) (with_directory_data ? SIZEOF_directory_data : 0), header);

            // ForgePlus: the given name, if there is one (keeping its terminating NUL)
            if (file_name != null)
            {
                Array.Clear(header.file_name, 0, header.file_name.Length);
                Array.Copy(file_name, header.file_name, Math.Min(file_name.Length, MAXIMUM_WADFILE_NAME_LENGTH - 1));
            }

            if (create_wadfile(TempFile))
            {
                var SaveFile = new OpenedFile();
                if (open_wad_file_for_writing(TempFile, SaveFile))
                {
                    /* Write out the new header */
                    if (write_wad_header(SaveFile, header))
                    {
                        offset = SIZEOF_wad_header;

                        var entries = new byte[get_size_of_directory_data(header)];
                        var wrote_all = true;

                        for (short index = 0; wrote_all && index < levels.Count; ++index)
                        {
                            MapLevel level = levels[index];

                            wad = build_level_wad(level, header, out wad_length, excluded_tags: excluded_tags);
                            if (wad == null)
                            {
                                wrote_all = false;
                                break;
                            }

                            set_indexed_directory_offset_and_length(header, entries, index, offset, wad_length, index);
                            if (with_directory_data)
                            {
                                Array.Copy(directory_data_bytes(level, level.loaded_wad.directory_data), 0, entries,
                                    get_indexed_directory_data(header, index, entries), SIZEOF_directory_data);
                            }

                            wrote_all = write_wad(SaveFile, header, wad, offset);
                            offset += wad_length;

                            free_wad(wad);
                        }

                        if (wrote_all)
                        {
                            /* Update the new header */
                            header.directory_offset = offset;
                            if (write_wad_header(SaveFile, header) && write_directorys(SaveFile, header, entries))
                            {
                                /* We win. */
                                success = true;
                            }
                        }
                    }

                    err = (short) SaveFile.GetError();
                    calculate_and_store_wadfile_checksum(SaveFile);

                    // ForgePlus: the given checksum, in place of the calculated one
                    if (checksum.HasValue)
                    {
                        read_wad_header(SaveFile, header);
                        header.checksum = checksum.Value;
                        write_wad_header(SaveFile, header);
                    }

                    close_wad_file(SaveFile);
                }

                if (err == 0)
                {
                    if (!TempFile.Rename(File))
                    {
                        err = 1;
                        set_game_error(systemError, (short) TempFile.GetError());
                    }
                }
            }

            if (err != 0 || error_pending())
            {
                success = false;
            }

            return success;
        }

        /*
         *  Unpacking/packing functions
         */

        public static void unpack_directory_data(StreamPointer S, IList<directory_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                directory_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.mission_flags);
                StreamToValue(S, out ObjPtr.environment_flags);
                StreamToValue(S, out ObjPtr.entry_point_flags);
                StreamToBytes(S, ObjPtr.level_name, LEVEL_NAME_LENGTH);
            }

            assert((S.Position - Stream) == SIZEOF_directory_data);
        }

        // ZZZ: gnu cc swears this is currently unused, and I don't see any sneaky #includes that might need it...
        // ForgePlus: used by directory_data_bytes()
        public static void pack_directory_data(StreamPointer S, IList<directory_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                directory_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.mission_flags);
                ValueToStream(S, ObjPtr.environment_flags);
                ValueToStream(S, ObjPtr.entry_point_flags);
                BytesToStream(S, ObjPtr.level_name, LEVEL_NAME_LENGTH);
            }

            assert((S.Position - Stream) == SIZEOF_directory_data);
        }

        /* ---------- string.h */

        private static void strncpy(byte[] dest, byte[] src, int n)
        {
            int i = 0;
            for (; i < n && i < src.Length && src[i] != 0; i++) dest[i] = src[i];
            for (; i < n && i < dest.Length; i++) dest[i] = 0;
        }

        private static int strlen(byte[] s)
        {
            int i = 0;
            while (i < s.Length && s[i] != 0) i++;
            return i;
        }
    }
}
