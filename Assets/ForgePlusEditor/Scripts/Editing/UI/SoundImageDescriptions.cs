using AlephOne;
using ForgePlus.Extensions;
using System.Text;

namespace ForgePlus.UI
{
    // The level's ambient sounds ('ambi', which a polygon loops while the player is in it) and random sounds ('bonk',
    // which it plays now and then), as map.cpp plays them: each varies by up to its delta (exclusive) above its value
    public static class SoundImageDescriptions
    {
        // Ticks per second (Aleph One's TICKS_PER_SECOND)
        private const float TicksPerSecond = 30f;

        // "Waterfall (6)" (a sound's code, with its name if Aleph One's table of them names it)
        public static string AmbientSoundName(short sound)
        {
            var name = AlephOneNames.AmbientSound(sound);

            return name == sound.ToString() ? $"({sound})" : $"{name} ({sound})";
        }

        public static string RandomSoundName(short sound)
        {
            var name = AlephOneNames.RandomSound(sound);

            return name == sound.ToString() ? $"({sound})" : $"{name} ({sound})";
        }

        // The polygon's ambient sound, as its index in the list and its sound
        public static string AmbientSoundOfPolygon(MapLevel level, short index)
        {
            if (index < 0)
            {
                return "None";
            }

            return index < level.AmbientSoundImageList.Count ?
                   $"{index}: {AmbientSoundName(level.AmbientSoundImageList[index].sound_index)}" :
                   $"{index} (not in the level)";
        }

        public static string RandomSoundOfPolygon(MapLevel level, short index)
        {
            if (index < 0)
            {
                return "None";
            }

            return index < level.RandomSoundImageList.Count ?
                   $"{index}: {RandomSoundName(level.RandomSoundImageList[index].sound_index)}" :
                   $"{index} (not in the level)";
        }

        public static string Describe(ambient_sound_image_data sound)
        {
            return $"{AmbientSoundName(sound.sound_index)}\n" +
                   $"Volume: {Volume(sound.volume)}";
        }

        public static string Describe(random_sound_image_data sound)
        {
            var description = new StringBuilder();
            description.Append($"{RandomSoundName(sound.sound_index)}\n");
            description.Append($"Volume: {Volume(sound.volume)}{Delta(sound.delta_volume, sound.delta_volume.ToString())}\n");
            description.Append($"Every: {Seconds(sound.period)}{Delta(sound.delta_period, Seconds(sound.delta_period))}\n");

            if (csmacros.TEST_FLAG(sound.flags, map._sound_image_is_non_directional))
            {
                description.Append("Direction: None (all around)\n");
            }
            else
            {
                description.Append($"Direction: {Degrees(sound.direction)}{Delta(sound.delta_direction, Degrees(sound.delta_direction))}\n");
            }

            description.Append($"Pitch: {Pitch(sound.pitch)}{Delta(sound.delta_pitch, Pitch(sound.delta_pitch))}");

            return description.ToString();
        }

        // Out of MAXIMUM_SOUND_VOLUME (full volume)
        private static string Volume(short volume)
        {
            return $"{volume} ({100f * volume / SoundManagerEnums.MAXIMUM_SOUND_VOLUME:0}%)";
        }

        private static string Seconds(short ticks)
        {
            return $"{ticks / TicksPerSecond:0.##} s";
        }

        private static string Degrees(short angle)
        {
            return $"{AlephOneExtensions.AngleToDegrees(angle):0.#}°";
        }

        // A fixed-point multiple of the sound's pitch
        private static string Pitch(int pitch)
        {
            return $"{(float) pitch / (1 << 16):0.##}×";
        }

        private static string Delta(int delta, string amount)
        {
            return delta != 0 ? $" (plus up to {amount})" : string.Empty;
        }
    }
}
