using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation;
using ForgePlus.UI;
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

        // The light of the polygon's media's surface, which only a polygon with media uses
        [CreateProperty]
        public int MediaLight
        {
            get
            {
                return Polygon.media_index < 0 ? LightIndexField.NoLight : Polygon.media_lightsource_index;
            }
            set
            {
                SetLight(LevelEntity_Polygon.DataSources.Media, value);
            }
        }

        [CreateProperty]
        public bool IsMediaLightEditable
        {
            get
            {
                return Polygon.media_index >= 0;
            }
        }

        // Its entry in the level's ambient sounds, which it loops while the player is in it (Sounds mode lists them)
        [CreateProperty]
        public string AmbientSound
        {
            get
            {
                return SoundImageDescriptions.AmbientSoundOfPolygon(Entity.ParentLevel.Level, Polygon.ambient_sound_image_index);
            }
            set
            {
                SetSound(SoundImageKinds.Ambient, value);
            }
        }

        [CreateProperty]
        public List<string> AmbientSoundChoices
        {
            get
            {
                return SoundChoices(SoundImageKinds.Ambient);
            }
        }

        [CreateProperty]
        public bool IsAmbientSoundPlayable
        {
            get
            {
                return SoundPreviews.CanPlay(SoundImageKinds.Ambient, SoundImageEditing.IndexOf(Polygon, SoundImageKinds.Ambient));
            }
        }

        // Its entry, at the entry's volume
        public void PlayAmbientSound()
        {
            SoundPreviews.Play(SoundImageKinds.Ambient, SoundImageEditing.IndexOf(Polygon, SoundImageKinds.Ambient));
        }

        // Its entry in the level's random sounds, which it plays now and then while the player is in it
        [CreateProperty]
        public string RandomSound
        {
            get
            {
                return SoundImageDescriptions.RandomSoundOfPolygon(Entity.ParentLevel.Level, Polygon.random_sound_image_index);
            }
            set
            {
                SetSound(SoundImageKinds.Random, value);
            }
        }

        [CreateProperty]
        public List<string> RandomSoundChoices
        {
            get
            {
                return SoundChoices(SoundImageKinds.Random);
            }
        }

        [CreateProperty]
        public bool IsRandomSoundPlayable
        {
            get
            {
                return SoundPreviews.CanPlay(SoundImageKinds.Random, SoundImageEditing.IndexOf(Polygon, SoundImageKinds.Random));
            }
        }

        // Its entry, at a volume and pitch from the entry's ranges
        public void PlayRandomSound()
        {
            SoundPreviews.Play(SoundImageKinds.Random, SoundImageEditing.IndexOf(Polygon, SoundImageKinds.Random));
        }

        // The sound sources it hears (those near enough, as the level's map indexes list them for it: map_constructors.cpp,
        // precalculate_polygon_sound_sources)
        [CreateProperty]
        public string HeardSoundSources
        {
            get
            {
                var level = Entity.ParentLevel.Level;
                var sources = new List<string>();

                for (var i = (int) Polygon.sound_source_indexes; i >= 0 && i < level.MapIndexList.Count; i++)
                {
                    var objectIndex = level.MapIndexList[i];
                    if (objectIndex == cstypes.NONE)
                    {
                        break;
                    }

                    sources.Add(objectIndex.ToString());
                }

                return sources.Count == 0 ? "None" : $"Objects {string.Join(", ", sources)}";
            }
        }

        // "None", then each entry of the list
        private List<string> SoundChoices(SoundImageKinds kind)
        {
            var level = Entity.ParentLevel.Level;
            var choices = new List<string>();
            var count = SoundImageEditing.Count(kind);

            for (short index = cstypes.NONE; index < count; index++)
            {
                choices.Add(SoundChoice(level, kind, index));
            }

            // One that's not in the list is still shown
            var current = SoundChoice(level, kind, SoundImageEditing.IndexOf(Polygon, kind));
            if (!choices.Contains(current))
            {
                choices.Insert(0, current);
            }

            return choices;
        }

        private static string SoundChoice(MapLevel level, SoundImageKinds kind, short index)
        {
            return kind == SoundImageKinds.Ambient ?
                   SoundImageDescriptions.AmbientSoundOfPolygon(level, index) :
                   SoundImageDescriptions.RandomSoundOfPolygon(level, index);
        }

        private void SetSound(SoundImageKinds kind, string choice)
        {
            var level = Entity.ParentLevel.Level;
            var count = SoundImageEditing.Count(kind);

            for (short index = cstypes.NONE; index < count; index++)
            {
                if (SoundChoice(level, kind, index) == choice)
                {
                    SoundImageEditing.Assign(Entity, kind, index);
                    return;
                }
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

        // A landscape surface doesn't use its light (so it can't be set, as painting a light can't set it)
        [CreateProperty]
        public int FloorLightIndex
        {
            get
            {
                return Polygon.floor_lightsource_index;
            }
            set
            {
                SetLight(LevelEntity_Polygon.DataSources.Floor, value);
            }
        }

        [CreateProperty]
        public bool IsFloorLightIndexEditable
        {
            get
            {
                return !Polygon.floor_texture.UsesLandscapeCollection();
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
        public int CeilingLightIndex
        {
            get
            {
                return Polygon.ceiling_lightsource_index;
            }
            set
            {
                SetLight(LevelEntity_Polygon.DataSources.Ceiling, value);
            }
        }

        [CreateProperty]
        public bool IsCeilingLightIndexEditable
        {
            get
            {
                return !Polygon.ceiling_texture.UsesLandscapeCollection();
            }
        }

        private void SetLight(LevelEntity_Polygon.DataSources dataSource, int lightIndex)
        {
            if (lightIndex >= 0)
            {
                Edit(polygon => polygon.SetLight(dataSource, (short) lightIndex));
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
