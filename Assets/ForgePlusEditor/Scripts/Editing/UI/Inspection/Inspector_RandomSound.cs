using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation;
using ForgePlus.UI;
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;

namespace ForgePlus.Inspection
{
    // An entry in the level's random sounds ('bonk'): a sound (a random sound code), played every period at a volume,
    // from a direction (unless it's non-directional) and at a pitch. Each of those varies by up to its delta (exclusive)
    // above it (map.cpp: handle_random_sound_image, which adds local_random() % delta), so each is shown as a range: from
    // the value to the highest it reaches (value + delta - 1), or just the value, for no delta.
    public class Inspector_RandomSound : Inspector_SoundImage
    {
        private const float AngleUnitsPerDegree = world.NUMBER_OF_ANGLES / 360f;
        private const float FixedOne = cstypes.FIXED_ONE;

        public Inspector_RandomSound(SoundImageEntry entry) : base(entry)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Random Sound";
            }
        }

        protected override string KindName
        {
            get
            {
                return "Random";
            }
        }

        private random_sound_image_data Image
        {
            get
            {
                return Entity.Random;
            }
        }

        [CreateProperty]
        public string Sound
        {
            get
            {
                return SoundImageDescriptions.RandomSoundName(Image.sound_index);
            }
            set
            {
                if (TryFindCode(value, SoundManagerEnums.NUMBER_OF_RANDOM_SOUND_DEFINITIONS, SoundImageDescriptions.RandomSoundName, out var code) && code != Image.sound_index)
                {
                    EditEntry(() => Image.sound_index = code);
                }
            }
        }

        [CreateProperty]
        public List<string> SoundChoices
        {
            get
            {
                return BuildSoundChoices(Image.sound_index, SoundManagerEnums.NUMBER_OF_RANDOM_SOUND_DEFINITIONS, SoundsLoading.Instance.HasRandomSound, SoundImageDescriptions.RandomSoundName);
            }
        }

        // ---------- Volume: from silent (0) to full (MAXIMUM_SOUND_VOLUME)

        [CreateProperty]
        public int Volume
        {
            get
            {
                return Image.volume;
            }
            set
            {
                var maximum = VolumeMaximum;
                SetRange(value, maximum, 0, SoundManagerEnums.MAXIMUM_SOUND_VOLUME, (minimum, delta) => { Image.volume = minimum; Image.delta_volume = delta; });
            }
        }

        [CreateProperty]
        public int VolumeMaximum
        {
            get
            {
                return Maximum(Image.volume, Image.delta_volume);
            }
            set
            {
                SetRange(Image.volume, value, 0, SoundManagerEnums.MAXIMUM_SOUND_VOLUME, (minimum, delta) => Image.delta_volume = delta);
            }
        }

        // ---------- Period: in ticks (30 a second)

        [CreateProperty]
        public int Period
        {
            get
            {
                return Image.period;
            }
            set
            {
                var maximum = PeriodMaximum;
                SetRange(value, maximum, 0, short.MaxValue, (minimum, delta) => { Image.period = minimum; Image.delta_period = delta; });
            }
        }

        [CreateProperty]
        public int PeriodMaximum
        {
            get
            {
                return Maximum(Image.period, Image.delta_period);
            }
            set
            {
                SetRange(Image.period, value, 0, short.MaxValue, (minimum, delta) => Image.delta_period = delta);
            }
        }

        // ---------- Direction: in degrees (saved as angle units, 512 a turn), which it comes from unless it's
        // non-directional

        [CreateProperty]
        public bool NonDirectional
        {
            get
            {
                return csmacros.TEST_FLAG(Image.flags, map._sound_image_is_non_directional);
            }
            set
            {
                EditEntry(() => Image.flags = value ? (ushort) (Image.flags | map._sound_image_is_non_directional) : (ushort) (Image.flags & ~map._sound_image_is_non_directional));
            }
        }

        [CreateProperty]
        public bool IsDirectionEditable
        {
            get
            {
                return !NonDirectional;
            }
        }

        [CreateProperty]
        public int Direction
        {
            get
            {
                return Degrees(Image.direction);
            }
            set
            {
                var maximum = Maximum(Image.direction, Image.delta_direction);
                SetRange(AngleUnits(value), maximum, short.MinValue, short.MaxValue, (minimum, delta) => { Image.direction = minimum; Image.delta_direction = delta; });
            }
        }

        [CreateProperty]
        public int DirectionMaximum
        {
            get
            {
                return Degrees((short) Maximum(Image.direction, Image.delta_direction));
            }
            set
            {
                SetRange(Image.direction, AngleUnits(value), short.MinValue, short.MaxValue, (minimum, delta) => Image.delta_direction = delta);
            }
        }

        // ---------- Pitch: a multiple of the sound's own (saved as a fixed-point number, whose delta is too fine to count
        // the highest it reaches as one less)

        [CreateProperty]
        public float Pitch
        {
            get
            {
                return (float) Math.Round(Image.pitch / FixedOne, 3);
            }
            set
            {
                var pitch = Math.Max(1, Mathf.RoundToInt(value * FixedOne));
                var maximum = Image.pitch + Image.delta_pitch;

                EditEntry(() =>
                {
                    Image.pitch = pitch;
                    Image.delta_pitch = Math.Max(0, maximum - pitch);
                });
            }
        }

        [CreateProperty]
        public float PitchMaximum
        {
            get
            {
                return (float) Math.Round((Image.pitch + Image.delta_pitch) / FixedOne, 3);
            }
            set
            {
                var maximum = Mathf.RoundToInt(value * FixedOne);

                EditEntry(() => Image.delta_pitch = Math.Max(0, maximum - Image.pitch));
            }
        }

        // The highest a value reaches with its delta
        private static int Maximum(short value, short delta)
        {
            return delta > 1 ? value + delta - 1 : value;
        }

        // Sets a range's value and delta from its first and second values, kept to the bounds (a second value below the
        // first means no variation)
        private void SetRange(int minimum, int maximum, int lowest, int highest, Action<short, short> set)
        {
            var clampedMinimum = (short) Math.Clamp(minimum, lowest, highest);
            var clampedMaximum = Math.Clamp(maximum, clampedMinimum, highest);
            var delta = (short) Math.Min(short.MaxValue, clampedMaximum > clampedMinimum ? clampedMaximum - clampedMinimum + 1 : 0);

            EditEntry(() => set(clampedMinimum, delta));
        }

        private static int Degrees(short angle)
        {
            return Mathf.RoundToInt(AlephOneExtensions.AngleToDegrees(angle));
        }

        private static int AngleUnits(int degrees)
        {
            return Mathf.RoundToInt(degrees * AngleUnitsPerDegree);
        }
    }
}
