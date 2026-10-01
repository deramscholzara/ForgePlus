using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using ForgePlus.UI;
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;

namespace ForgePlus.Inspection
{
    // An entry in the level's ambient sounds ('ambi'): a sound (an ambient sound code) and its volume
    public class Inspector_AmbientSound : Inspector_SoundImage
    {
        public Inspector_AmbientSound(SoundImageEntry entry) : base(entry)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Ambient Sound";
            }
        }

        protected override string KindName
        {
            get
            {
                return "Ambient";
            }
        }

        private ambient_sound_image_data Image
        {
            get
            {
                return Entity.Ambient;
            }
        }

        [CreateProperty]
        public string Sound
        {
            get
            {
                return SoundImageDescriptions.AmbientSoundName(Image.sound_index);
            }
            set
            {
                if (TryFindCode(value, SoundManagerEnums.NUMBER_OF_AMBIENT_SOUND_DEFINITIONS, SoundImageDescriptions.AmbientSoundName, out var code) && code != Image.sound_index)
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
                return BuildSoundChoices(Image.sound_index, SoundManagerEnums.NUMBER_OF_AMBIENT_SOUND_DEFINITIONS, SoundsLoading.Instance.HasAmbientSound, SoundImageDescriptions.AmbientSoundName);
            }
        }

        // From silent (0) to full (MAXIMUM_SOUND_VOLUME)
        [CreateProperty]
        public int Volume
        {
            get
            {
                return Image.volume;
            }
            set
            {
                var volume = (short) Math.Clamp(value, 0, SoundManagerEnums.MAXIMUM_SOUND_VOLUME);
                if (volume != Image.volume)
                {
                    EditEntry(() => Image.volume = volume);
                }
                else
                {
                    RefreshValuesInInspector();
                }
            }
        }

        [CreateProperty]
        public int VolumeSlider
        {
            get
            {
                return Volume;
            }
            set
            {
                Volume = value;
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            Root.Q(nameof(Volume)).Q<SliderInt>().highValue = SoundManagerEnums.MAXIMUM_SOUND_VOLUME;
        }
    }
}
