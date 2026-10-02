using AlephOne;
using ForgePlus.Sound;
using RuntimeCore.Entities;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Plays one of the level's sounds on its own, from the sounds file (LevelSoundPlayback): an ambient or random sound
    // entry's (at its volume, and a random one's pitch), or an ambient sound (such as a sound source's) at a volume
    public static class SoundPreviews
    {
        public static bool CanPlay(SoundImageKinds kind, short index)
        {
            var level = SoundImageEditing.Level;
            if (!LevelSoundPlayback.Instance || level == null || index < 0 || index >= SoundImageEditing.Count(kind))
            {
                return false;
            }

            return kind == SoundImageKinds.Ambient ?
                LevelSoundPlayback.CanPlayAmbientSound(level.AmbientSoundImageList[index].sound_index) :
                LevelSoundPlayback.CanPlayRandomSound(level.RandomSoundImageList[index].sound_index);
        }

        public static void Play(SoundImageKinds kind, short index)
        {
            if (!CanPlay(kind, index))
            {
                return;
            }

            var level = SoundImageEditing.Level;
            if (kind == SoundImageKinds.Ambient)
            {
                var entry = level.AmbientSoundImageList[index];
                LevelSoundPlayback.Instance.PreviewAmbientSound(entry.sound_index, entry.volume);
            }
            else
            {
                LevelSoundPlayback.Instance.PreviewRandomSound(level.RandomSoundImageList[index]);
            }
        }

        public static bool CanPlayAmbientSound(short ambientSound)
        {
            return LevelSoundPlayback.Instance && ambientSound != cstypes.NONE && LevelSoundPlayback.CanPlayAmbientSound(ambientSound);
        }

        public static void PlayAmbientSound(short ambientSound, short volume)
        {
            if (CanPlayAmbientSound(ambientSound))
            {
                LevelSoundPlayback.Instance.PreviewAmbientSound(ambientSound, volume);
            }
        }

        // A sound source's volume: its facing, or, when that's negative, the intensity of the light whose index it is,
        // negated (as the light is now), as in _sound_add_ambient_sources_proc (map.cpp)
        public static short SoundSourceVolume(map_object soundSource)
        {
            if (soundSource.facing >= 0)
            {
                return soundSource.facing;
            }

            var level = LevelEntity_Level.Instance;
            var intensity = level && level.Lights.TryGetValue((short) -soundSource.facing, out var light) ? Mathf.Clamp01(light.CurrentLinearIntensity) : 0f;

            return (short) (Mathf.RoundToInt(intensity * cstypes.FIXED_ONE) >> 8);
        }

        // The ambient sound a platform sound source plays instead of its own (while its platform moves): its platform's
        // moving sound, as in _sound_add_ambient_sources_proc (map.cpp), or NONE if its polygon isn't a platform (or the
        // platform makes no moving sound)
        public static short PlatformMovingSound(MapLevel level, map_object soundSource)
        {
            var polygon = map.get_polygon_data(level, soundSource.polygon_index);
            if (polygon == null || polygon.type != map._polygon_is_platform || platforms.get_platform_data(level, polygon.permutation) == null)
            {
                return cstypes.NONE;
            }

            return platforms.get_platform_moving_sound(level, polygon.permutation);
        }
    }
}
