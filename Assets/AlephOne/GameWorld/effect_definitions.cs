// Port of Aleph One: Source_Files/GameWorld/effect_definitions.h
using static AlephOne.cstypes;
using static AlephOne.effects;
using static AlephOne.map;
using static AlephOne.shape_descriptors;
using static AlephOne.SoundManagerEnums;

namespace AlephOne
{
    /* ---------- structures */

    public class effect_definition
    {
        public short collection, shape;

        public int sound_pitch;

        public ushort flags;
        public short delay, delay_sound;

        public effect_definition Clone()
        {
            return (effect_definition) MemberwiseClone();
        }
    }

    public static class effect_definitions
    {
        /* ---------- constants */

        /* flags */
        public const short _end_when_animation_loops = 0x0001;
        public const short _end_when_transfer_animation_loops = 0x0002;
        public const short _sound_only = 0x0004; /* play the animation’s initial sound and nothing else */
        public const short _make_twin_visible = 0x0008;
        public const short _media_effect = 0x0010;

        /* ---------- effect definitions */

        public static readonly effect_definition[] original_effect_definitions = new effect_definition[NUMBER_OF_EFFECT_TYPES]
        {
            /* rocket explosion, contrail */
            new effect_definition {collection = _collection_rocket, shape = 1, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_rocket, shape = 2, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* grenade explosion, contrail */
            new effect_definition {collection = _collection_rocket, shape = 9, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_rocket, shape = 4, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* bullet ricochet */
            new effect_definition {collection = _collection_rocket, shape = 13, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_alien_weapon_ricochet */
            new effect_definition {collection = _collection_rocket, shape = 5, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* flame thrower burst */
            new effect_definition {collection = _collection_rocket, shape = 6, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* fighter blood splash */
            new effect_definition {collection = _collection_fighter, shape = 8, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* player blood splash */
            new effect_definition {collection = _collection_rocket, shape = 10, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* civilian blood splash, assimilated civilian blood splash */
            new effect_definition {collection = _collection_civilian, shape = 7, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_civilian, 3), shape = 12, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* enforcer blood splash */
            new effect_definition {collection = _collection_enforcer, shape = 5, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_compiler_bolt_minor_detonation, _effect_compiler_bolt_major_detonation,
                _effect_compiler_bolt_major_contrail */
            new effect_definition {collection = _collection_compiler, shape = 6, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_compiler, 1), shape = 6, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_compiler, 1), shape = 5, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_fighter_projectile_detonation, _effect_fighter_melee_detonation */
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_fighter, 0), shape = 10, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_fighter, 0), shape = 11, sound_pitch = _normal_frequency, flags = (ushort) _sound_only, delay = 0, delay_sound = NONE},

            /* _effect_hunter_projectile_detonation, _effect_hunter_spark */
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_hunter, 0), shape = 4, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_hunter, 0), shape = 8, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_minor_fusion_detonation, _effect_major_fusion_detonation, _effect_major_fusion_contrail */
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_rocket, 0), shape = 14, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_rocket, 0), shape = 15, sound_pitch = _higher_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_rocket, 0), shape = 16, sound_pitch = _higher_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_fist_detonation */
            new effect_definition {collection = _collection_rocket, shape = 17, sound_pitch = _normal_frequency, flags = (ushort) _sound_only, delay = 0, delay_sound = NONE},

            /* _effect_minor_defender_detonation, _effect_major_defender_detonation, _effect_defender_spark */
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_defender, 0), shape = 5, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_defender, 1), shape = 5, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_defender, 0), shape = 7, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_trooper_blood_splash */
            new effect_definition {collection = _collection_trooper, shape = 8, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_lamp_breaking */
            new effect_definition {collection = _collection_scenery1, shape = 22, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery2, shape = 18, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery3, shape = 16, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery5, shape = 8, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_metallic_clang */
            new effect_definition {collection = _collection_rocket, shape = 23, sound_pitch = _normal_frequency, flags = (ushort) _sound_only, delay = 0, delay_sound = NONE},

            /* _effect_teleport_in, _effect_teleport_out */
            new effect_definition {collection = _collection_items, shape = 0, sound_pitch = _normal_frequency, flags = _end_when_transfer_animation_loops|_make_twin_visible, delay = TICKS_PER_SECOND, delay_sound = _snd_teleport_in},
            new effect_definition {collection = _collection_items, shape = 0, sound_pitch = _normal_frequency, flags = (ushort) _end_when_transfer_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_small_water_splash, _effect_medium_water_splash, _effect_large_water_splash, _effect_large_water_emergence */
            new effect_definition {collection = _collection_scenery1, shape = 0, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_media_effect, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery1, shape = 1, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_media_effect, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery1, shape = 2, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_sound_only, delay = NONE},
            new effect_definition {collection = _collection_scenery1, shape = 3, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_sound_only, delay = NONE},

            /* _effect_small_lava_splash, _effect_medium_lava_splash, _effect_large_lava_splash, _effect_large_lava_emergence */
            new effect_definition {collection = _collection_scenery2, shape = 0, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_media_effect, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery2, shape = 1, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_media_effect, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery2, shape = 2, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_sound_only, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery2, shape = 17, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_sound_only, delay = 0, delay_sound = NONE},

            /* _effect_small_sewage_splash, _effect_medium_sewage_splash, _effect_large_sewage_splash, _effect_large_sewage_emergence */
            new effect_definition {collection = _collection_scenery3, shape = 0, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_media_effect, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery3, shape = 1, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_media_effect, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery3, shape = 2, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_sound_only, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery3, shape = 3, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_sound_only, delay = 0, delay_sound = NONE},

            /* _effect_small_goo_splash, _effect_medium_goo_splash, _effect_large_goo_splash, _effect_large_goo_emergence */
            new effect_definition {collection = _collection_scenery5, shape = 0, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_media_effect, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery5, shape = 1, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_media_effect, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery5, shape = 2, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_sound_only, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery5, shape = 3, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_sound_only, delay = 0, delay_sound = NONE},

            /* _effect_minor_hummer_projectile_detonation, _effect_major_hummer_projectile_detonation,
                _effect_durandal_hummer_projectile_detonation, _effect_hummer_spark */
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_hummer, 0), shape = 6, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_hummer, 1), shape = 6, sound_pitch = _higher_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_hummer, 4), shape = 6, sound_pitch = _lower_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_hummer, 0), shape = 7, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_cyborg_projectile_detonation, _effect_cyborg_blood_splash */
            new effect_definition {collection = _collection_cyborg, shape = 7, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_cyborg, shape = 8, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /*  _effect_minor_fusion_dispersal, _effect_major_fusion_dispersal, _effect_overloaded_fusion_dispersal */
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_rocket, 0), shape = 19, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_sound_only, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_rocket, 0), shape = 20, sound_pitch = _higher_frequency, flags = _end_when_animation_loops|_sound_only, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_rocket, 0), shape = 21, sound_pitch = _lower_frequency, flags = _end_when_animation_loops|_sound_only, delay = 0, delay_sound = NONE},

            /* _effect_sewage_yeti_blood_splash, _effect_sewage_yeti_projectile_detonation */
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_yeti, 0), shape = 5, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_yeti, 0), shape = 11, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_water_yeti_blood_splash */
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_yeti, 1), shape = 5, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_lava_yeti_blood_splash, _effect_lava_yeti_projectile_detonation */
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_yeti, 2), shape = 5, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_yeti, 2), shape = 7, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            /* _effect_yeti_melee_detonation */
            new effect_definition {collection = _collection_yeti, shape = 8, sound_pitch = _normal_frequency, flags = (ushort) _sound_only, delay = 0, delay_sound = NONE},

            /* _effect_juggernaut_spark, _effect_juggernaut_missile_contrail */
            new effect_definition {collection = _collection_juggernaut, shape = 3, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_rocket, shape = 24, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},

            // LP addition: Jjaro stuff and VacBobs
            /* _effect_small_jjaro_splash, _effect_medium_jjaro_splash, _effect_large_jjaro_splash, _effect_large_jjaro_emergence */
            new effect_definition {collection = _collection_scenery4, shape = 0, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_media_effect, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery4, shape = 1, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_media_effect, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery4, shape = 2, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_sound_only, delay = 0, delay_sound = NONE},
            new effect_definition {collection = _collection_scenery4, shape = 3, sound_pitch = _normal_frequency, flags = _end_when_animation_loops|_sound_only, delay = 0, delay_sound = NONE},

            /* _effect_civilian_fusion_blood_splash, _effect_assimilated_civilian_fusion_blood_splash */
            new effect_definition {collection = _collection_civilian_fusion, shape = 7, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
            new effect_definition {collection = (short) BUILD_COLLECTION(_collection_civilian_fusion, 3), shape = 12, sound_pitch = _normal_frequency, flags = (ushort) _end_when_animation_loops, delay = 0, delay_sound = NONE},
        };
    }
}
