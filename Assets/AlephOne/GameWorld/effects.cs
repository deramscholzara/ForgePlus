// Port of Aleph One: Source_Files/GameWorld/effects.h, effects.cpp (effect types and the packing of
// effect definitions)
//
// Not ported: effect_data and everything that runs effects in a game (new_effect, update_effects, ...).
using System.Collections.Generic;
using static AlephOne.csalerts;
using static AlephOne.cstypes;
using static AlephOne.effect_definitions;
using static AlephOne.Packing;

namespace AlephOne
{
    public static class effects
    {
        /* ---------- effect structure */

        /* effect types */
        public const short _effect_rocket_explosion = 0;
        public const short _effect_rocket_contrail = 1;
        public const short _effect_grenade_explosion = 2;
        public const short _effect_grenade_contrail = 3;
        public const short _effect_bullet_ricochet = 4;
        public const short _effect_alien_weapon_ricochet = 5;
        public const short _effect_flamethrower_burst = 6;
        public const short _effect_fighter_blood_splash = 7;
        public const short _effect_player_blood_splash = 8;
        public const short _effect_civilian_blood_splash = 9;
        public const short _effect_assimilated_civilian_blood_splash = 10;
        public const short _effect_enforcer_blood_splash = 11;
        public const short _effect_compiler_bolt_minor_detonation = 12;
        public const short _effect_compiler_bolt_major_detonation = 13;
        public const short _effect_compiler_bolt_major_contrail = 14;
        public const short _effect_fighter_projectile_detonation = 15;
        public const short _effect_fighter_melee_detonation = 16;
        public const short _effect_hunter_projectile_detonation = 17;
        public const short _effect_hunter_spark = 18;
        public const short _effect_minor_fusion_detonation = 19;
        public const short _effect_major_fusion_detonation = 20;
        public const short _effect_major_fusion_contrail = 21;
        public const short _effect_fist_detonation = 22;
        public const short _effect_minor_defender_detonation = 23;
        public const short _effect_major_defender_detonation = 24;
        public const short _effect_defender_spark = 25;
        public const short _effect_trooper_blood_splash = 26;
        public const short _effect_water_lamp_breaking = 27;
        public const short _effect_lava_lamp_breaking = 28;
        public const short _effect_sewage_lamp_breaking = 29;
        public const short _effect_alien_lamp_breaking = 30;
        public const short _effect_metallic_clang = 31;
        public const short _effect_teleport_object_in = 32;
        public const short _effect_teleport_object_out = 33;
        public const short _effect_small_water_splash = 34;
        public const short _effect_medium_water_splash = 35;
        public const short _effect_large_water_splash = 36;
        public const short _effect_large_water_emergence = 37;
        public const short _effect_small_lava_splash = 38;
        public const short _effect_medium_lava_splash = 39;
        public const short _effect_large_lava_splash = 40;
        public const short _effect_large_lava_emergence = 41;
        public const short _effect_small_sewage_splash = 42;
        public const short _effect_medium_sewage_splash = 43;
        public const short _effect_large_sewage_splash = 44;
        public const short _effect_large_sewage_emergence = 45;
        public const short _effect_small_goo_splash = 46;
        public const short _effect_medium_goo_splash = 47;
        public const short _effect_large_goo_splash = 48;
        public const short _effect_large_goo_emergence = 49;
        public const short _effect_minor_hummer_projectile_detonation = 50;
        public const short _effect_major_hummer_projectile_detonation = 51;
        public const short _effect_durandal_hummer_projectile_detonation = 52;
        public const short _effect_hummer_spark = 53;
        public const short _effect_cyborg_projectile_detonation = 54;
        public const short _effect_cyborg_blood_splash = 55;
        public const short _effect_minor_fusion_dispersal = 56;
        public const short _effect_major_fusion_dispersal = 57;
        public const short _effect_overloaded_fusion_dispersal = 58;
        public const short _effect_sewage_yeti_blood_splash = 59;
        public const short _effect_sewage_yeti_projectile_detonation = 60;
        public const short _effect_water_yeti_blood_splash = 61;
        public const short _effect_lava_yeti_blood_splash = 62;
        public const short _effect_lava_yeti_projectile_detonation = 63;
        public const short _effect_yeti_melee_detonation = 64;
        public const short _effect_juggernaut_spark = 65;
        public const short _effect_juggernaut_missile_contrail = 66;
        // LP addition: Jjaro stuff
        public const short _effect_small_jjaro_splash = 67;
        public const short _effect_medium_jjaro_splash = 68;
        public const short _effect_large_jjaro_splash = 69;
        public const short _effect_large_jjaro_emergence = 70;
        public const short _effect_civilian_fusion_blood_splash = 71;
        public const short _effect_assimilated_civilian_fusion_blood_splash = 72;
        public const short NUMBER_OF_EFFECT_TYPES = 73;

        public const int SIZEOF_effect_data = 32;

        public const int SIZEOF_effect_definition = 14;

        public static void unpack_effect_definition(StreamPointer S, IList<effect_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                effect_definition ObjPtr = Objects[k];

                StreamToValue(S, out ObjPtr.collection);
                StreamToValue(S, out ObjPtr.shape);

                StreamToValue(S, out ObjPtr.sound_pitch);

                StreamToValue(S, out ObjPtr.flags);
                StreamToValue(S, out ObjPtr.delay);
                StreamToValue(S, out ObjPtr.delay_sound);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_effect_definition));
        }

        public static void unpack_m1_effect_definition(StreamPointer S, IList<effect_definition> effect_definitions, int Count)
        {
            for (int k = 0; k < Count; k++)
            {
                effect_definition ObjPtr = effect_definitions[k];

                StreamToValue(S, out ObjPtr.collection);
                StreamToValue(S, out ObjPtr.shape);
                ObjPtr.sound_pitch = FIXED_ONE;
                StreamToValue(S, out ObjPtr.flags);
                ObjPtr.delay = 0;
                ObjPtr.delay_sound = NONE;
            }
        }

        public static void pack_effect_definition(StreamPointer S, IList<effect_definition> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                effect_definition ObjPtr = Objects[k];

                ValueToStream(S, ObjPtr.collection);
                ValueToStream(S, ObjPtr.shape);

                ValueToStream(S, ObjPtr.sound_pitch);

                ValueToStream(S, ObjPtr.flags);
                ValueToStream(S, ObjPtr.delay);
                ValueToStream(S, ObjPtr.delay_sound);
            }

            assert((S.Position - Stream) == (Count * SIZEOF_effect_definition));
        }

        public static void init_effect_definitions(effect_definition[] effect_definitions)
        {
            // memcpy(effect_definitions, original_effect_definitions, sizeof(effect_definitions));
            for (int k = 0; k < NUMBER_OF_EFFECT_TYPES; k++)
            {
                effect_definitions[k] = original_effect_definitions[k].Clone();
            }
        }
    }
}
