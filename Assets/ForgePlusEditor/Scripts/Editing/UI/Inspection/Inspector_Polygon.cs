using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_Polygon : Inspector_Base<LevelEntity_Polygon>
    {
        public Inspector_Polygon(LevelEntity_Polygon polygon) : base(polygon)
        {
        }

        protected override string LayoutPath
        {
            get
            {
                return "UI/Inspectors/Inspector - Polygon";
            }
        }

        private polygon_data Polygon
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

        // Choosing Platform makes the polygon a platform (with a S'pht door's settings, which Platforms mode edits), and
        // choosing another type for a platform removes its platform
        [CreateProperty]
        public string Type
        {
            get
            {
                return TypeChoice(Polygon.type);
            }
            set
            {
                for (short type = 0; type < NumberOfPolygonTypes; type++)
                {
                    if (TypeChoice(type) == value && type != Polygon.type)
                    {
                        SetType(type);
                        return;
                    }
                }
            }
        }

        [CreateProperty]
        public List<string> TypeChoices
        {
            get
            {
                var choices = new List<string>();
                for (short type = 0; type < NumberOfPolygonTypes; type++)
                {
                    choices.Add(TypeChoice(type));
                }

                return choices;
            }
        }

        [CreateProperty]
        public string Permutation
        {
            get
            {
                return Polygon.permutation.ToString();
            }
        }

        [CreateProperty]
        public string MediaIndex
        {
            get
            {
                return Polygon.media_index.ToString();
            }
        }

        [CreateProperty]
        public string MediaLight
        {
            get
            {
                return Polygon.media_lightsource_index.ToString();
            }
        }

        [CreateProperty]
        public string AmbientSound
        {
            get
            {
                return Polygon.ambient_sound_image_index.ToString();
            }
        }

        [CreateProperty]
        public string RandomSound
        {
            get
            {
                return Polygon.random_sound_image_index.ToString();
            }
        }

        [CreateProperty]
        public string FloorHeight
        {
            get
            {
                return Polygon.floor_height.ToString();
            }
        }

        [CreateProperty]
        public string FloorLightIndex
        {
            get
            {
                return Polygon.floor_lightsource_index.ToString();
            }
        }

        [CreateProperty]
        public string CeilingHeight
        {
            get
            {
                return Polygon.ceiling_height.ToString();
            }
        }

        [CreateProperty]
        public string CeilingLightIndex
        {
            get
            {
                return Polygon.ceiling_lightsource_index.ToString();
            }
        }

        // Aleph One's polygon types run from _polygon_is_normal to _polygon_is_superglue
        private const short NumberOfPolygonTypes = map._polygon_is_superglue + 1;

        // Making or removing a platform rebuilds the level (the sides around it change), and reselects the polygon
        private void SetType(short type)
        {
            if (type == map._polygon_is_platform)
            {
                PlatformEditing.MakePlatform(Entity);
            }
            else if (Polygon.type == map._polygon_is_platform)
            {
                PlatformEditing.RemovePlatform(Entity, type);
            }
            else
            {
                Edit(polygon => polygon.NativeObject.type = type);
            }
        }

        private static string TypeChoice(short type)
        {
            return $"{AlephOneNames.PolygonType(type)} ({type})";
        }
    }
}
