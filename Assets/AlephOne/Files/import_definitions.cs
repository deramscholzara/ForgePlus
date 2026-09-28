// Port of Aleph One: Source_Files/Files/import_definitions.cpp and its header, Files/extensions.h
//
// Not ported: network physics (get_network_physics_buffer, process_network_physics_model,
// import_m1_physics_data_from_network) and set_to_default_physics_file().
using static AlephOne.csalerts;
using static AlephOne.effects;
using static AlephOne.game_errors;
using static AlephOne.monsters;
using static AlephOne.Packing;
using static AlephOne.physics;
using static AlephOne.physics_models;
using static AlephOne.player;
using static AlephOne.projectiles;
using static AlephOne.SDL_rwops;
using static AlephOne.tags;
using static AlephOne.wad;
using static AlephOne.weapon_definitions;
using static AlephOne.weapons;

namespace AlephOne
{
    // ForgePlus: Aleph One's physics globals, monster_definitions[], effect_definitions[],
    // projectile_definitions[], physics_models[] and weapon_definitions[]
    public class PhysicsModel
    {
        public readonly monster_definition[] monster_definitions = new monster_definition[NUMBER_OF_MONSTER_TYPES];
        public readonly effect_definition[] effect_definitions = new effect_definition[NUMBER_OF_EFFECT_TYPES];
        public readonly projectile_definition[] projectile_definitions = new projectile_definition[NUMBER_OF_PROJECTILE_TYPES];
        public readonly physics_constants[] physics_models = new physics_constants[NUMBER_OF_PHYSICS_MODELS];
        public readonly weapon_definition[] weapon_definitions = new weapon_definition[NUMBER_OF_WEAPONS];
    }

    public static class import_definitions
    {
        // extensions.h
        public const short BUNGIE_PHYSICS_DATA_VERSION = 0;
        public const short PHYSICS_DATA_VERSION = 1;

        /* ---------- code */

        public static void init_physics_wad_data(PhysicsModel model)
        {
            init_monster_definitions(model.monster_definitions);
            init_effect_definitions(model.effect_definitions);
            init_projectile_definitions(model.projectile_definitions);
            init_physics_constants(model.physics_models);
            init_weapon_definitions(model.weapon_definitions);
        }

        public static bool physics_file_is_m1(FileSpecifier PhysicsFileSpec)
        {
            bool m1_physics = false;

            // check for M1 physics
            var PhysicsFile = new OpenedFile();
            if (PhysicsFileSpec.Open(PhysicsFile))
            {
                uint tag = SDL_ReadBE32(PhysicsFile.GetRWops());
                switch (tag)
                {
                    case M1_MONSTER_PHYSICS_TAG:
                    case M1_EFFECTS_PHYSICS_TAG:
                    case M1_PROJECTILE_PHYSICS_TAG:
                    case M1_PHYSICS_PHYSICS_TAG:
                    case M1_WEAPONS_PHYSICS_TAG:
                        m1_physics = true;
                        break;
                    default:
                        break;
                }

                PhysicsFile.Close();
            }
            return m1_physics;
        }

        public static void import_definition_structures(PhysicsModel model, FileSpecifier PhysicsFileSpec)
        {
            init_physics_wad_data(model);

            if (physics_file_is_m1(PhysicsFileSpec))
            {
                import_m1_physics_data(model, PhysicsFileSpec);
            }
            else
            {
                wad_data wad;
                bool bungie_physics;

                wad = get_physics_wad_data(PhysicsFileSpec, out bungie_physics);
                if (wad != null)
                {
                    /* Actually load it in.. */
                    import_physics_wad_data(model, wad);

                    free_wad(wad);
                }
            }
        }

        public static uint get_physics_file_checksum(FileSpecifier PhysicsFileSpec)
        {
            return crc.calculate_crc_for_file(PhysicsFileSpec);
        }

        /* --------- local code */
        public static wad_data get_physics_wad_data(FileSpecifier PhysicsFileSpec, out bool bungie_physics)
        {
            wad_data wad = null;
            bungie_physics = false;

            var PhysicsFile = new OpenedFile();
            if (open_wad_file_for_reading(PhysicsFileSpec, PhysicsFile))
            {
                var header = new wad_header();

                if (read_wad_header(PhysicsFile, header))
                {
                    if (header.data_version == BUNGIE_PHYSICS_DATA_VERSION || header.data_version == PHYSICS_DATA_VERSION)
                    {
                        wad = read_indexed_wad_from_file(PhysicsFile, header, 0, true);
                        if (header.data_version == BUNGIE_PHYSICS_DATA_VERSION)
                        {
                            bungie_physics = true;
                        }
                        else
                        {
                            bungie_physics = false;
                        }
                    }
                }

                close_wad_file(PhysicsFile);
            }

            /* Reset any errors that might have occurred.. */
            set_game_error(systemError, errNone);

            return wad;
        }

        public static void import_physics_wad_data(PhysicsModel model, wad_data wad)
        {
            // LP: this code is copied out of game_wad.c
            int data_length;
            byte[] data;
            int count;

            data = extract_type_from_wad(wad, MONSTER_PHYSICS_TAG, out data_length);
            count = data_length / SIZEOF_monster_definition;
            assert(count * SIZEOF_monster_definition == data_length);
            assert(count <= NUMBER_OF_MONSTER_TYPES);
            if (data_length > 0)
            {
                unpack_monster_definition(new StreamPointer(data), model.monster_definitions, count);
            }

            data = extract_type_from_wad(wad, EFFECTS_PHYSICS_TAG, out data_length);
            count = data_length / SIZEOF_effect_definition;
            assert(count * SIZEOF_effect_definition == data_length);
            assert(count <= NUMBER_OF_EFFECT_TYPES);
            if (data_length > 0)
            {
                unpack_effect_definition(new StreamPointer(data), model.effect_definitions, count);
            }

            data = extract_type_from_wad(wad, PROJECTILE_PHYSICS_TAG, out data_length);
            count = data_length / SIZEOF_projectile_definition;
            assert(count * SIZEOF_projectile_definition == data_length);
            assert(count <= NUMBER_OF_PROJECTILE_TYPES);
            if (data_length > 0)
            {
                unpack_projectile_definition(new StreamPointer(data), model.projectile_definitions, count);
            }

            data = extract_type_from_wad(wad, PHYSICS_PHYSICS_TAG, out data_length);
            count = data_length / SIZEOF_physics_constants;
            assert(count * SIZEOF_physics_constants == data_length);
            assert(count <= get_number_of_physics_models());
            if (data_length > 0)
            {
                unpack_physics_constants(new StreamPointer(data), model.physics_models, count);
            }

            data = extract_type_from_wad(wad, WEAPONS_PHYSICS_TAG, out data_length);
            count = data_length / SIZEOF_weapon_definition;
            assert(count * SIZEOF_weapon_definition == data_length);
            assert(count <= get_number_of_weapon_types());
            if (data_length > 0)
            {
                unpack_weapon_definition(new StreamPointer(data), model.weapon_definitions, count);
            }
        }

        private static void import_m1_physics_data(PhysicsModel model, FileSpecifier PhysicsFileSpec)
        {
            var PhysicsFile = new OpenedFile();
            if (!PhysicsFileSpec.Open(PhysicsFile))
            {
                return;
            }

            int position = 0;
            int length;
            PhysicsFile.GetLength(out length);

            while (position < length)
            {
                var header = new byte[12];
                PhysicsFile.Read(header.Length, header);
                var header_stream = new StreamPointer(header);

                uint tag;
                ushort count;
                ushort size;

                StreamToValue(header_stream, out tag);
                header_stream.Skip(4); // unused
                StreamToValue(header_stream, out count);
                StreamToValue(header_stream, out size);

                var data = new byte[count * size];
                PhysicsFile.Read(data.Length, data);
                switch (tag)
                {
                    case M1_MONSTER_PHYSICS_TAG:
                        unpack_m1_monster_definition(new StreamPointer(data), model.monster_definitions, count);
                        break;
                    case M1_EFFECTS_PHYSICS_TAG:
                        unpack_m1_effect_definition(new StreamPointer(data), model.effect_definitions, count);
                        break;
                    case M1_PROJECTILE_PHYSICS_TAG:
                        unpack_m1_projectile_definition(new StreamPointer(data), model.projectile_definitions, count);
                        break;
                    case M1_PHYSICS_PHYSICS_TAG:
                        unpack_m1_physics_constants(new StreamPointer(data), model.physics_models, count);
                        break;
                    case M1_WEAPONS_PHYSICS_TAG:
                        unpack_m1_weapon_definition(new StreamPointer(data), model.weapon_definitions, count);
                        break;
                }

                PhysicsFile.GetPosition(out position);
            }

            PhysicsFile.Close();
        }
    }
}
