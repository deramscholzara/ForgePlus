// Port of Aleph One: Source_Files/GameWorld/projectiles.h, projectiles.cpp (projectile types and the
// packing of projectile definitions)
//
// Not ported: projectile_data and everything that moves projectiles in a game.
using System.Collections.Generic;
using static AlephOne.csalerts;
using static AlephOne.cstypes;
using static AlephOne.map;
using static AlephOne.map_constructors;
using static AlephOne.Packing;
using static AlephOne.projectile_definitions;

namespace AlephOne
{
    public static class projectiles
    {
        /* ---------- projectile structure */

        /* projectile types */
        public const short _projectile_rocket = 0;
        public const short _projectile_grenade = 1;
        public const short _projectile_pistol_bullet = 2;
        public const short _projectile_rifle_bullet = 3;
        public const short _projectile_shotgun_bullet = 4;
        public const short _projectile_staff = 5;
        public const short _projectile_staff_bolt = 6;
        public const short _projectile_flamethrower_burst = 7;
        public const short _projectile_compiler_bolt_minor = 8;
        public const short _projectile_compiler_bolt_major = 9;
        public const short _projectile_alien_weapon = 10;
        public const short _projectile_fusion_bolt_minor = 11;
        public const short _projectile_fusion_bolt_major = 12;
        public const short _projectile_hunter = 13;
        public const short _projectile_fist = 14;
        public const short _projectile_armageddon_sphere = 15;
        public const short _projectile_armageddon_electricity = 16;
        public const short _projectile_juggernaut_rocket = 17;
        public const short _projectile_trooper_bullet = 18;
        public const short _projectile_trooper_grenade = 19;
        public const short _projectile_minor_defender = 20;
        public const short _projectile_major_defender = 21;
        public const short _projectile_juggernaut_missile = 22;
        public const short _projectile_minor_energy_drain = 23;
        public const short _projectile_major_energy_drain = 24;
        public const short _projectile_oxygen_drain = 25;
        public const short _projectile_minor_hummer = 26;
        public const short _projectile_major_hummer = 27;
        public const short _projectile_durandal_hummer = 28;
        public const short _projectile_minor_cyborg_ball = 29;
        public const short _projectile_major_cyborg_ball = 30;
        public const short _projectile_ball = 31;
        public const short _projectile_minor_fusion_dispersal = 32;
        public const short _projectile_major_fusion_dispersal = 33;
        public const short _projectile_overloaded_fusion_dispersal = 34;
        public const short _projectile_yeti = 35;
        public const short _projectile_sewage_yeti = 36;
        public const short _projectile_lava_yeti = 37;
        // LP additions:
        public const short _projectile_smg_bullet = 38;
        public const short NUMBER_OF_PROJECTILE_TYPES = 39;

        public const int SIZEOF_projectile_data = 32;

        public const int SIZEOF_projectile_definition = 48;

        /* translate_projectile() flags */
        public const short _flyby_of_current_player = 0x0001;
        public const short _projectile_hit = 0x0002;
        public const short _projectile_hit_monster = 0x0004; // monster_index in *obstruction_index
        public const short _projectile_hit_floor = 0x0008; // polygon_index in *obstruction_index
        public const short _projectile_hit_media = 0x0010; // polygon_index in *obstruction_index
        public const short _projectile_hit_landscape = 0x0020;
        public const short _projectile_hit_scenery = 0x0040;

        public static void unpack_projectile_definition(StreamPointer S, IList<projectile_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                projectile_definition ObjPtr = Objects[k];

                StreamToValue(S, out ObjPtr.collection);
                StreamToValue(S, out ObjPtr.shape);
                StreamToValue(S, out ObjPtr.detonation_effect);
                StreamToValue(S, out ObjPtr.media_detonation_effect);
                StreamToValue(S, out ObjPtr.contrail_effect);
                StreamToValue(S, out ObjPtr.ticks_between_contrails);
                StreamToValue(S, out ObjPtr.maximum_contrails);
                StreamToValue(S, out ObjPtr.media_projectile_promotion);

                StreamToValue(S, out ObjPtr.radius);
                StreamToValue(S, out ObjPtr.area_of_effect);
                unpack_damage_definition(S, new[] { ObjPtr.damage }, 1);

                StreamToValue(S, out ObjPtr.flags);

                StreamToValue(S, out ObjPtr.speed);
                StreamToValue(S, out ObjPtr.maximum_range);

                StreamToValue(S, out ObjPtr.sound_pitch);
                StreamToValue(S, out ObjPtr.flyby_sound);
                StreamToValue(S, out ObjPtr.rebound_sound);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_projectile_definition));
        }


        public static void unpack_m1_projectile_definition(StreamPointer S, IList<projectile_definition> projectile_definitions, int Count)
        {
            for (int k = 0; k < Count; k++)
            {
                projectile_definition ObjPtr = projectile_definitions[k];

                StreamToValue(S, out ObjPtr.collection);
                StreamToValue(S, out ObjPtr.shape);
                StreamToValue(S, out ObjPtr.detonation_effect);
                ObjPtr.media_detonation_effect = NONE;
                StreamToValue(S, out ObjPtr.contrail_effect);
                StreamToValue(S, out ObjPtr.ticks_between_contrails);
                StreamToValue(S, out ObjPtr.maximum_contrails);
                ObjPtr.media_projectile_promotion = 0;

                StreamToValue(S, out ObjPtr.radius);
                StreamToValue(S, out ObjPtr.area_of_effect);
                unpack_damage_definition(S, new[] { ObjPtr.damage }, 1);

                ushort flags;
                StreamToValue(S, out flags);
                ObjPtr.flags = flags;

                StreamToValue(S, out ObjPtr.speed);
                StreamToValue(S, out ObjPtr.maximum_range);

                ObjPtr.sound_pitch = FIXED_ONE;
                StreamToValue(S, out ObjPtr.flyby_sound);
                ObjPtr.rebound_sound = NONE;

                if (ObjPtr.damage.type == _damage_projectile)
                {
                    ObjPtr.flags |= _bleeding_projectile;
                }
            }
        }

        public static void pack_projectile_definition(StreamPointer S, IList<projectile_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                projectile_definition ObjPtr = Objects[k];

                ValueToStream(S, ObjPtr.collection);
                ValueToStream(S, ObjPtr.shape);
                ValueToStream(S, ObjPtr.detonation_effect);
                ValueToStream(S, ObjPtr.media_detonation_effect);
                ValueToStream(S, ObjPtr.contrail_effect);
                ValueToStream(S, ObjPtr.ticks_between_contrails);
                ValueToStream(S, ObjPtr.maximum_contrails);
                ValueToStream(S, ObjPtr.media_projectile_promotion);

                ValueToStream(S, ObjPtr.radius);
                ValueToStream(S, ObjPtr.area_of_effect);
                pack_damage_definition(S, new[] { ObjPtr.damage }, 1);

                ValueToStream(S, ObjPtr.flags);

                ValueToStream(S, ObjPtr.speed);
                ValueToStream(S, ObjPtr.maximum_range);

                ValueToStream(S, ObjPtr.sound_pitch);
                ValueToStream(S, ObjPtr.flyby_sound);
                ValueToStream(S, ObjPtr.rebound_sound);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_projectile_definition));
        }

        public static void init_projectile_definitions(projectile_definition[] projectile_definitions)
        {
            // memcpy(projectile_definitions, original_projectile_definitions, sizeof(projectile_definitions));
            for (int k = 0; k < NUMBER_OF_PROJECTILE_TYPES; k++)
            {
                projectile_definitions[k] = original_projectile_definitions[k].Clone();
            }
        }
    }
}
