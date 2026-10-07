using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using System.Text;

namespace ForgePlus.UI
{
    // The level's ambient sounds ('ambi', which a polygon loops while the player is in it) and random sounds ('bonk',
    // which it plays now and then), as map.cpp plays them: each varies by up to its delta (exclusive) above its value
    public static class SoundImageDescriptions
    {
        // Aleph One's TICKS_PER_SECOND
        private const float TicksPerSecond = 30f;

        public static string AmbientSoundName(short sound)
        {
            return NameAndCode(AlephOneNames.AmbientSound(sound), sound);
        }

        public static string RandomSoundName(short sound)
        {
            return NameAndCode(AlephOneNames.RandomSound(sound), sound);
        }

        // The polygon's ambient sound, as its index in the list and its sound
        public static string AmbientSoundOfPolygon(MapLevel level, short index)
        {
            var list = level.AmbientSoundImageList;

            return SoundOfPolygon(index, index >= 0 && index < list.Count ? AmbientSoundName(list[index].sound_index) : null);
        }

        public static string RandomSoundOfPolygon(MapLevel level, short index)
        {
            var list = level.RandomSoundImageList;

            return SoundOfPolygon(index, index >= 0 && index < list.Count ? RandomSoundName(list[index].sound_index) : null);
        }

        public static string Describe(ambient_sound_image_data sound)
        {
            return $"{AmbientSoundName(sound.sound_index)}\n" +
                   Strings.Get(Strings.Common, "SoundImage.Describe.Volume", Volume(sound.volume));
        }

        public static string Describe(random_sound_image_data sound)
        {
            var description = new StringBuilder();
            description.Append($"{RandomSoundName(sound.sound_index)}\n");
            description.Append(Strings.Get(Strings.Common, "SoundImage.Describe.Volume", WithDelta(Volume(sound.volume), sound.delta_volume, sound.delta_volume.ToString())) + "\n");
            description.Append(Strings.Get(Strings.Common, "SoundImage.Describe.Every", WithDelta(Seconds(sound.period), sound.delta_period, Seconds(sound.delta_period))) + "\n");

            if (csmacros.TEST_FLAG(sound.flags, map._sound_image_is_non_directional))
            {
                description.Append(Strings.Get(Strings.Common, "SoundImage.Describe.Direction.None") + "\n");
            }
            else
            {
                description.Append(Strings.Get(Strings.Common, "SoundImage.Describe.Direction", WithDelta(Degrees(sound.direction), sound.delta_direction, Degrees(sound.delta_direction))) + "\n");
            }

            description.Append(Strings.Get(Strings.Common, "SoundImage.Describe.Pitch", WithDelta(Pitch(sound.pitch), sound.delta_pitch, Pitch(sound.delta_pitch))));

            return description.ToString();
        }

        // "Waterfall (6)", or "(6)" where Aleph One's table of sounds doesn't name it
        private static string NameAndCode(string name, short sound)
        {
            return name == sound.ToString() ? $"({sound})" : Strings.Get(Strings.Common, "Inspector.Base.NameAndNumber", name, sound);
        }

        // The sound is null for an index past the end of its list
        private static string SoundOfPolygon(short index, string sound)
        {
            if (index < 0)
            {
                return Strings.Get(Strings.Common, "SoundImage.None");
            }

            return sound != null ? Strings.Get(Strings.Common, "SoundImage.OfPolygon", index, sound) : Strings.Get(Strings.Common, "SoundImage.OfPolygon.Missing", index);
        }

        // Out of MAXIMUM_SOUND_VOLUME (full volume)
        private static string Volume(short volume)
        {
            return Strings.Get(Strings.Common, "SoundImage.Volume", volume, (100f * volume / SoundManagerEnums.MAXIMUM_SOUND_VOLUME).ToString("0"));
        }

        private static string Seconds(short ticks)
        {
            return Strings.Get(Strings.Common, "SoundImage.Seconds", (ticks / TicksPerSecond).ToString("0.##"));
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

        // The value, and how much more it can be (if it varies)
        private static string WithDelta(string value, int delta, string amount)
        {
            return delta != 0 ? Strings.Get(Strings.Common, "SoundImage.WithDelta", value, amount) : value;
        }
    }
}
