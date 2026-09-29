using AlephOne;
using ForgePlus.Extensions;
using RuntimeCore.Entities.Geometry;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_Media : Inspector_Base<LevelEntity_Media>
    {
        public Inspector_Media(LevelEntity_Media media) : base(media)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Media";
            }
        }

        private media_data Media
        {
            get
            {
                return Entity.NativeObject;
            }
        }

        [CreateProperty]
        public string Id
        {
            get
            {
                return Entity.NativeIndex.ToString();
            }
        }

        [CreateProperty]
        public string Type
        {
            get
            {
                return AlephOneNames.MediaType(Media.type);
            }
        }

        [CreateProperty]
        public string LowHeight
        {
            get
            {
                return Media.low.ToString();
            }
        }

        [CreateProperty]
        public string HighHeight
        {
            get
            {
                return Media.high.ToString();
            }
        }

        [CreateProperty]
        public string FlowDirection
        {
            get
            {
                return AlephOneExtensions.AngleToDegrees(Media.current_direction).ToString();
            }
        }

        [CreateProperty]
        public string FlowMagnitude
        {
            get
            {
                return Media.current_magnitude.ToString();
            }
        }

        [CreateProperty]
        public string LightIndex
        {
            get
            {
                return Media.light_index.ToString();
            }
        }

        [CreateProperty]
        public string MinimumLightIntensity
        {
            get
            {
                return AlephOneExtensions.FixedToFloat(Media.minimum_light_intensity).ToString();
            }
        }

        [CreateProperty]
        public bool FloorObstructsSound
        {
            get
            {
                return AlephOne.media.MEDIA_SOUND_OBSTRUCTED_BY_FLOOR(Media);
            }
        }
    }
}
