using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Palette;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    // A media's data (media.h: media_data). Its height follows its light between its low and high heights.
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

        // Its kind of liquid, which gives it its texture, sounds and effects
        [CreateProperty]
        public string Type
        {
            get
            {
                return MediaTypeChoices.Choice(Media.type);
            }
            set
            {
                if (MediaTypeChoices.TryParse(Media.type, value, out var type) && type != Media.type)
                {
                    Edit(media => media.NativeObject.type = type);
                    ApplyType();
                }
            }
        }

        [CreateProperty]
        public List<string> TypeChoices
        {
            get
            {
                return MediaTypeChoices.All(Media.type);
            }
        }

        [CreateProperty]
        public List<string> TypeAlephOneOnlyChoices
        {
            get
            {
                return MediaTypeChoices.AlephOneOnly(Media.type);
            }
        }

        [CreateProperty]
        public string TypeNote
        {
            get
            {
                return MediaTypeChoices.Note(Media.type);
            }
        }

        // Its height while its light is off (world units, 1024 to a meter)
        [CreateProperty]
        public int LowHeight
        {
            get
            {
                return Media.low;
            }
            set
            {
                Edit(media => media.NativeObject.low = ClampToShort(value));
            }
        }

        // Its height while its light is fully on
        [CreateProperty]
        public int HighHeight
        {
            get
            {
                return Media.high;
            }
            set
            {
                Edit(media => media.NativeObject.high = ClampToShort(value));
            }
        }

        // In degrees
        [CreateProperty]
        public float FlowDirection
        {
            get
            {
                return (float) Math.Round(AlephOneExtensions.AngleToDegrees(Media.current_direction), 2);
            }
            set
            {
                var angle = AlephOneExtensions.DegreesToAngle(value);

                Edit(media => media.NativeObject.current_direction = angle);
                Entity.ApplyMaterialProperties();
            }
        }

        // World units per tick, which pushes what's in it
        [CreateProperty]
        public int FlowMagnitude
        {
            get
            {
                return Media.current_magnitude;
            }
            set
            {
                Edit(media => media.NativeObject.current_magnitude = ClampToNonNegativeShort(value));
                Entity.ApplyMaterialProperties();
            }
        }

        // The light whose intensity sets its height (and lights its surfaces)
        [CreateProperty]
        public int LightIndex
        {
            get
            {
                return Media.light_index;
            }
            set
            {
                SetLight(value, (media, light) => media.NativeObject.light_index = light);
            }
        }

        // The least of its light's intensity it rises to
        [CreateProperty]
        public float MinimumLightIntensity
        {
            get
            {
                return DisplayedIntensity(Media.minimum_light_intensity);
            }
            set
            {
                Edit(media => media.NativeObject.minimum_light_intensity = FixedIntensity(value));
            }
        }

        // It makes no sound while it's below the floor
        [CreateProperty]
        public bool FloorObstructsSound
        {
            get
            {
                return AlephOne.media.MEDIA_SOUND_OBSTRUCTED_BY_FLOOR(Media);
            }
            set
            {
                Edit(media => AlephOne.media.SET_MEDIA_SOUND_OBSTRUCTED_BY_FLOOR(media.NativeObject, value));
            }
        }

        // Its surfaces take its new type's texture and effects, and the palette its new type's name
        private void ApplyType()
        {
            var level = LevelEntity_Level.Instance;
            foreach (var polygon in level.Polygons.Values)
            {
                if (polygon.NativeObject.media_index == Entity.NativeIndex && polygon.MediaSurface)
                {
                    polygon.MediaSurface.ApplyMedia();
                }
            }

            Entity.ApplyMaterialProperties();
            PaletteManager.Instance.RefreshSwatches();
        }
    }
}
