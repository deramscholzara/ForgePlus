// Port of Aleph One: Source_Files/Sound/SoundManager.h, SoundManager.cpp (sound definition lookups)
//
// Not ported: everything that plays sound (the SoundManager class, ambient and random sound playback, sound
// channels, listeners), and MML parsing (parse_mml_sounds, reset_mml_sounds), so the ambient and random tables are
// always the originals.
using Unity.Scripting.LifecycleManagement;
using static AlephOne.sound_definitions;
using static AlephOne.SoundManagerEnums;

namespace AlephOne
{
    [NoAutoStaticsCleanup]
    public static class SoundManager
    {
        public static ambient_sound_definition get_ambient_sound_definition(short ambient_sound_index)
        {
            return csmacros.GetMemberWithBounds(ambient_sound_definitions, ambient_sound_index, NUMBER_OF_AMBIENT_SOUND_DEFINITIONS);
        }

        public static random_sound_definition get_random_sound_definition(short random_sound_index)
        {
            return csmacros.GetMemberWithBounds(random_sound_definitions, random_sound_index, NUMBER_OF_RANDOM_SOUND_DEFINITIONS);
        }

        // short SoundManager::RandomSoundIndexToSoundIndex(short random_sound_index)
        public static short RandomSoundIndexToSoundIndex(short random_sound_index)
        {
            random_sound_definition definition = get_random_sound_definition(random_sound_index);
            return definition != null ? definition.sound_index : cstypes.NONE;
        }
    }
}
