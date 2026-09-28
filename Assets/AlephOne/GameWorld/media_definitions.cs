// Port of Aleph One: Source_Files/GameWorld/media_definitions.h
using static AlephOne.cstypes;
using static AlephOne.effects;
using static AlephOne.fades;
using static AlephOne.map;
using static AlephOne.media;
using static AlephOne.shape_descriptors;
using static AlephOne.SoundManagerEnums;

namespace AlephOne
{
    /* ---------- structures */

    public class media_definition
    {
        public short collection, shape, shape_count, shape_frequency;
        public short transfer_mode;

        public short damage_frequency; // mask&ticks
        public damage_definition damage;

        public short[] detonation_effects = new short[NUMBER_OF_MEDIA_DETONATION_TYPES];
        public short[] sounds = new short[NUMBER_OF_MEDIA_SOUNDS];

        public short submerged_fade_effect;
    }

    public static partial class media
    {
        /* ---------- globals */

        // LP addition: added JjaroGoo support (copy of sewage)
        public static readonly media_definition[] media_definitions = new media_definition[NUMBER_OF_MEDIA_TYPES]
        {
            /* _media_water */
            new media_definition
            {
                collection = _collection_walls1, shape = 19, shape_count = 1, shape_frequency = 0, /* collection, shape, shape_count, frequency */
                transfer_mode = _xfer_normal, /* transfer mode */

                damage_frequency = 0, damage = new damage_definition { type = NONE, flags = 0, @base = 0, random = 0, scale = FIXED_ONE }, /* damage frequency and definition */

                detonation_effects = new short[] { _effect_small_water_splash, _effect_medium_water_splash, _effect_large_water_splash, _effect_large_water_emergence }, /* small, medium, large detonation effects */
                sounds = new short[] { NONE, NONE, _snd_enter_water, _snd_exit_water,
                    _snd_walking_in_water, _ambient_snd_water, _ambient_snd_under_media,
                    _snd_enter_water, _snd_exit_water },

                submerged_fade_effect = _effect_under_water, /* submerged fade effect */
            },

            /* _media_lava */
            new media_definition
            {
                collection = _collection_walls2, shape = 12, shape_count = 1, shape_frequency = 0, /* collection, shape, shape_count, frequency */
                transfer_mode = _xfer_normal, /* transfer mode */

                damage_frequency = 0xf, damage = new damage_definition { type = _damage_lava, flags = _alien_damage, @base = 16, random = 0, scale = FIXED_ONE }, /* damage frequency and definition */

                detonation_effects = new short[] { _effect_small_lava_splash, _effect_medium_lava_splash, _effect_large_lava_splash, _effect_large_lava_emergence }, /* small, medium, large detonation effects */
                sounds = new short[] { NONE, NONE, _snd_enter_lava, _snd_exit_lava,
                    _snd_walking_in_lava, _ambient_snd_lava, _ambient_snd_under_media,
                    _snd_enter_lava, _snd_exit_lava },

                submerged_fade_effect = _effect_under_lava, /* submerged fade effect */
            },

            /* _media_goo */
            new media_definition
            {
                collection = _collection_walls5, shape = 5, shape_count = 1, shape_frequency = 0, /* collection, shape, shape_count, frequency */
                transfer_mode = _xfer_normal, /* transfer mode */

                damage_frequency = 0x7, damage = new damage_definition { type = _damage_goo, flags = _alien_damage, @base = 8, random = 0, scale = FIXED_ONE }, /* damage frequency and definition */

                detonation_effects = new short[] { _effect_small_goo_splash, _effect_medium_goo_splash, _effect_large_goo_splash, _effect_large_goo_emergence }, /* small, medium, large detonation effects */
                sounds = new short[] { NONE, NONE, _snd_enter_lava, _snd_exit_lava,
                    _snd_walking_in_lava, _ambient_snd_goo, _ambient_snd_under_media,
                    _snd_enter_lava, _snd_exit_lava },

                submerged_fade_effect = _effect_under_goo, /* submerged fade effect */
            },

            /* _media_sewage */
            new media_definition
            {
                collection = _collection_walls3, shape = 13, shape_count = 1, shape_frequency = 0, /* collection, shape, shape_count, frequency */
                transfer_mode = _xfer_normal, /* transfer mode */

                damage_frequency = 0, damage = new damage_definition { type = NONE, flags = 0, @base = 0, random = 0, scale = FIXED_ONE }, /* damage frequency and definition */

                detonation_effects = new short[] { _effect_small_sewage_splash, _effect_medium_sewage_splash, _effect_large_sewage_splash, _effect_large_sewage_emergence }, /* small, medium, large detonation effects */
                sounds = new short[] { NONE, NONE, _snd_enter_sewage, _snd_exit_sewage,
                    NONE, _ambient_snd_sewage, _ambient_snd_under_media,
                    _snd_enter_sewage, _snd_exit_sewage },

                submerged_fade_effect = _effect_under_sewage, /* submerged fade effect */
            },

            /* _media_jjaro */
            new media_definition
            {
                collection = _collection_walls4, shape = 13, shape_count = 1, shape_frequency = 0, /* collection, shape, shape_count, frequency */
                transfer_mode = _xfer_normal, /* transfer mode */

                damage_frequency = 0, damage = new damage_definition { type = NONE, flags = 0, @base = 0, random = 0, scale = FIXED_ONE }, /* damage frequency and definition */

                detonation_effects = new short[] { _effect_small_jjaro_splash, _effect_medium_jjaro_splash, _effect_large_jjaro_splash, _effect_large_jjaro_emergence }, /* small, medium, large detonation effects */
                sounds = new short[] { NONE, NONE, _snd_enter_sewage, _snd_exit_sewage,
                    NONE, _ambient_snd_sewage, _ambient_snd_under_media,
                    _snd_enter_sewage, _snd_exit_sewage },

                submerged_fade_effect = _effect_under_jjaro, /* submerged fade effect */
            },
        };
    }
}
