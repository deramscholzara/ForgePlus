// Port of Aleph One: Source_Files/Sound/sound_definitions.h
//
// Not ported: the commented-out sound_definitions table.
// sound_definition is ported as SoundDefinition (SoundFile.cs), which is what Aleph One reads a sounds file into.
// The ambient and random sound tables are the originals: MML can change them (parse_mml_sounds in SoundManager.cpp),
// which isn't ported.
using Unity.Scripting.LifecycleManagement;
using static AlephOne.SoundManagerEnums;

namespace AlephOne
{
    public class ambient_sound_definition
    {
        public short sound_index;

        public ambient_sound_definition(short sound_index)
        {
            this.sound_index = sound_index;
        }
    }

    public class depth_curve_definition
    {
        public short maximum_volume, maximum_volume_distance;
        public short minimum_volume, minimum_volume_distance;

        public depth_curve_definition(int maximum_volume, int maximum_volume_distance, int minimum_volume, int minimum_volume_distance)
        {
            this.maximum_volume = (short) maximum_volume;
            this.maximum_volume_distance = (short) maximum_volume_distance;
            this.minimum_volume = (short) minimum_volume;
            this.minimum_volume_distance = (short) minimum_volume_distance;
        }
    }

    public class sound_behavior_definition
    {
        public depth_curve_definition obstructed_curve, unobstructed_curve;

        public sound_behavior_definition(depth_curve_definition obstructed_curve, depth_curve_definition unobstructed_curve)
        {
            this.obstructed_curve = obstructed_curve;
            this.unobstructed_curve = unobstructed_curve;
        }
    }

    public class random_sound_definition
    {
        public short sound_index;

        public random_sound_definition(short sound_index)
        {
            this.sound_index = sound_index;
        }
    }

    [NoAutoStaticsCleanup]
    public static class sound_definitions
    {
        public const int MAXIMUM_PERMUTATIONS_PER_SOUND = 5;

        /* sound behaviors */
        public const short _sound_is_quiet = 0;
        public const short _sound_is_normal = 1;
        public const short _sound_is_loud = 2;
        public const short NUMBER_OF_SOUND_BEHAVIOR_DEFINITIONS = 3;

        /* flags */
        public const ushort _sound_cannot_be_restarted = 0x0001;
        public const ushort _sound_does_not_self_abort = 0x0002;
        public const ushort _sound_resists_pitch_changes = 0x0004; // 0.5 external pitch changes
        public const ushort _sound_cannot_change_pitch = 0x0008; // no external pitch changes
        public const ushort _sound_cannot_be_obstructed = 0x0010; // ignore obstructions
        public const ushort _sound_cannot_be_media_obstructed = 0x0020; // ignore media obstructions
        public const ushort _sound_is_ambient = 0x0040; // will not be loaded unless _ambient_sound_flag is asserted

        /* sound chances */
        public const ushort _ten_percent = 32768 * 9 / 10;
        public const ushort _twenty_percent = 32768 * 8 / 10;
        public const ushort _thirty_percent = 32768 * 7 / 10;
        public const ushort _fourty_percent = 32768 * 6 / 10;
        public const ushort _fifty_percent = 32768 * 5 / 10;
        public const ushort _sixty_percent = 32768 * 4 / 10;
        public const ushort _seventy_percent = 32768 * 3 / 10;
        public const ushort _eighty_percent = 32768 * 2 / 10;
        public const ushort _ninty_percent = 32768 * 1 / 10;
        public const ushort _always = 0;

        public const int SOUND_FILE_VERSION = 1;
        public static readonly uint SOUND_FILE_TAG = cstypes.FOUR_CHARS_TO_INT('s', 'n', 'd', '2');

        // struct sound_file_header: int32 version, int32 tag, int16 source_count (usually 2: 8-bit, 16-bit),
        // int16 sound_count, int16 unused[124], immediately followed by source_count*sound_count sound_definition
        // structures (read in place by M2SoundFile::Open)
        public const int SIZEOF_sound_file_header = 260;

        public const int SIZEOF_sound_definition = 64;

        /* ---------- sound behavior structures */

        public static readonly sound_behavior_definition[] sound_behavior_definitions = new sound_behavior_definition[NUMBER_OF_SOUND_BEHAVIOR_DEFINITIONS]
        {
            /* _sound_is_quiet */
            new sound_behavior_definition(
                new depth_curve_definition(0, 0, 0, 0), /* obstructed quiet sounds make no sound */
                new depth_curve_definition(MAXIMUM_SOUND_VOLUME, 0, 0, 5 * world.WORLD_ONE)),

            /* _sound_is_normal */
            new sound_behavior_definition(
                new depth_curve_definition(MAXIMUM_SOUND_VOLUME / 2, 0, 0, 7 * world.WORLD_ONE),
                new depth_curve_definition(MAXIMUM_SOUND_VOLUME, world.WORLD_ONE, 0, 10 * world.WORLD_ONE)),

            /* _sound_is_loud */
            new sound_behavior_definition(
                new depth_curve_definition((3 * MAXIMUM_SOUND_VOLUME) / 4, 0, 0, 10 * world.WORLD_ONE),
                new depth_curve_definition(MAXIMUM_SOUND_VOLUME, 2 * world.WORLD_ONE, MAXIMUM_SOUND_VOLUME / 8, 15 * world.WORLD_ONE)),
        };

        /* ---------- ambient sound definition structures */

        public static readonly ambient_sound_definition[] ambient_sound_definitions = new ambient_sound_definition[NUMBER_OF_AMBIENT_SOUND_DEFINITIONS]
        {
            new ambient_sound_definition(_snd_water),
            new ambient_sound_definition(_snd_sewage),
            new ambient_sound_definition(_snd_lava),
            new ambient_sound_definition(_snd_goo),
            new ambient_sound_definition(_snd_under_media),
            new ambient_sound_definition(_snd_wind),
            new ambient_sound_definition(_snd_waterfall),
            new ambient_sound_definition(_snd_siren),
            new ambient_sound_definition(_snd_fan),
            new ambient_sound_definition(_snd_spht_door),
            new ambient_sound_definition(_snd_spht_platform),
            new ambient_sound_definition(_snd_heavy_spht_door),
            new ambient_sound_definition(_snd_heavy_spht_platform),
            new ambient_sound_definition(_snd_light_machinery),
            new ambient_sound_definition(_snd_heavy_machinery),
            new ambient_sound_definition(_snd_transformer),
            new ambient_sound_definition(_snd_sparking_transformer),
            new ambient_sound_definition(_snd_machine_binder),
            new ambient_sound_definition(_snd_machine_bookpress),
            new ambient_sound_definition(_snd_machine_puncher),
            new ambient_sound_definition(_snd_electric),
            new ambient_sound_definition(_snd_alarm),
            new ambient_sound_definition(_snd_night_wind),
            new ambient_sound_definition(_snd_pfhor_door),
            new ambient_sound_definition(_snd_pfhor_platform),
            new ambient_sound_definition(_snd_alien_noise1),
            new ambient_sound_definition(_snd_alien_noise2),
            // LP addition: Marathon Infinity ambient sound
            new ambient_sound_definition(_snd_alien_harmonics),
        };

        /* ---------- random sound definition structures */

        public static readonly random_sound_definition[] random_sound_definitions = new random_sound_definition[NUMBER_OF_RANDOM_SOUND_DEFINITIONS]
        {
            new random_sound_definition(_snd_water_drip),
            new random_sound_definition(_snd_surface_explosion),
            new random_sound_definition(_snd_underground_explosion),
            new random_sound_definition(_snd_owl),
            // LP addition: Marathon Infinity random sound
            new random_sound_definition(_snd_creak),
        };
    }
}
