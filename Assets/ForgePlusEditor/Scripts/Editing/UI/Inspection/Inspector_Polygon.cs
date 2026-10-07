using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation;
using ForgePlus.Localization;
using ForgePlus.UI;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;

namespace ForgePlus.Inspection
{
    public class Inspector_Polygon : Inspector_Base<LevelEntity_Polygon>
    {
        // Aleph One's polygon types run from _polygon_is_normal to _polygon_is_superglue
        private const short NumberOfPolygonTypes = map._polygon_is_superglue + 1;

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

        // Choosing Platform makes the polygon a platform (with a S'pht door's settings), and choosing another type for a
        // platform removes its platform
        [CreateProperty]
        public string Type
        {
            get
            {
                return TypeChoice(Polygon.type);
            }
            set
            {
                if (TryFindChoice(ShortRange(0, NumberOfPolygonTypes), TypeChoice, value, out var type) && type != Polygon.type)
                {
                    SetType(type);
                }
            }
        }

        [CreateProperty]
        public List<string> TypeChoices
        {
            get
            {
                return ChoicesOf(ShortRange(0, NumberOfPolygonTypes), TypeChoice);
            }
        }

        // What it means depends on the type (see PermutationCaption)
        [CreateProperty]
        public int Permutation
        {
            get
            {
                return Polygon.permutation;
            }
            set
            {
                Edit(polygon => polygon.NativeObject.permutation = ClampToShort(value));
            }
        }

        // A platform's is its platform's index, which making and removing platforms keeps
        [CreateProperty]
        public bool IsPermutationEditable
        {
            get
            {
                return Polygon.type != map._polygon_is_platform;
            }
        }

        [CreateProperty]
        public string PermutationCaption
        {
            get
            {
                return PermutationCaptions.ForPolygon(Polygon);
            }
        }

        // As painting it in Media mode sets it
        [CreateProperty]
        public string MediaIndex
        {
            get
            {
                return MediaChoice(Polygon.media_index);
            }
            set
            {
                if (TryFindChoice(MediaIndexes(), MediaChoice, value, out var mediaIndex) && mediaIndex != Polygon.media_index)
                {
                    Entity.ParentLevel.Medias.TryGetValue(mediaIndex, out var media);
                    Edit(polygon => polygon.SetMedia(media));
                }
            }
        }

        [CreateProperty]
        public List<string> MediaIndexChoices
        {
            get
            {
                return ChoicesOf(MediaIndexes(), MediaChoice);
            }
        }

        // It's edited in Geometry mode (Sounds mode shows it too)
        [CreateProperty]
        public bool IsMediaIndexEditable
        {
            get
            {
                return ModeManager.Instance.PrimaryMode == ModeManager.PrimaryModes.Geometry;
            }
        }

        // Only a polygon with media uses it
        [CreateProperty]
        public int MediaLight
        {
            get
            {
                return Polygon.media_index < 0 ? LightIndexField.NoLight : Polygon.media_lightsource_index;
            }
            set
            {
                SetLight(value, (polygon, light) => polygon.SetLight(LevelEntity_Polygon.DataSources.Media, light));
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

        // Its entry in the level's ambient sounds, which it loops while the player is in it
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
        public List<string> AmbientSoundAlephOneOnlyChoices
        {
            get
            {
                return AlephOneOnlySoundChoices(SoundImageKinds.Ambient);
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

        // At the entry's volume
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
        public List<string> RandomSoundAlephOneOnlyChoices
        {
            get
            {
                return AlephOneOnlySoundChoices(SoundImageKinds.Random);
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

        // At a volume and pitch from the entry's ranges
        public void PlayRandomSound()
        {
            SoundPreviews.Play(SoundImageKinds.Random, SoundImageEditing.IndexOf(Polygon, SoundImageKinds.Random));
        }

        // The sound sources near enough to hear, as the level's map indexes list them (map_constructors.cpp:
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

                return sources.Count == 0 ?
                       Strings.Get(Strings.Common, "Inspector.Polygon.HeardSoundSources.None") :
                       Strings.Get(Strings.Common, "Inspector.Polygon.HeardSoundSources.Objects", string.Join(", ", sources));
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

        // A landscape doesn't use its light (so it can't be set, as painting a light can't set it)
        [CreateProperty]
        public int FloorLightIndex
        {
            get
            {
                return Polygon.floor_lightsource_index;
            }
            set
            {
                SetLight(value, (polygon, light) => polygon.SetLight(LevelEntity_Polygon.DataSources.Floor, light));
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
                SetLight(value, (polygon, light) => polygon.SetLight(LevelEntity_Polygon.DataSources.Ceiling, light));
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

        // "None", then each entry of the list
        private List<string> SoundChoices(SoundImageKinds kind)
        {
            var level = Entity.ParentLevel.Level;

            return ChoicesOf(ShortRange(cstypes.NONE, SoundImageEditing.Count(kind) + 1), index => SoundChoice(level, kind, index), SoundImageEditing.IndexOf(Polygon, kind));
        }

        // The entries past the original games' limit, which only Aleph One holds
        private List<string> AlephOneOnlySoundChoices(SoundImageKinds kind)
        {
            var level = Entity.ParentLevel.Level;
            var count = SoundImageEditing.Count(kind) - SoundImageEditing.MaximumOriginalEntries;

            return ChoicesOf(ShortRange(SoundImageEditing.MaximumOriginalEntries, count), index => SoundChoice(level, kind, index));
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

            if (TryFindChoice(ShortRange(cstypes.NONE, SoundImageEditing.Count(kind) + 1), entry => SoundChoice(level, kind, entry), choice, out var index))
            {
                SoundImageEditing.Assign(Entity, kind, index);
            }
        }

        // "None" (NONE), then the level's media, and the polygon's own index if it's no media's
        private List<short> MediaIndexes()
        {
            var indexes = new List<short> { cstypes.NONE };
            indexes.AddRange(Entity.ParentLevel.Medias.Keys.OrderBy(index => index));

            if (!indexes.Contains(Polygon.media_index))
            {
                indexes.Add(Polygon.media_index);
            }

            return indexes;
        }

        // "2 - lava", or "None"
        private string MediaChoice(short mediaIndex)
        {
            if (mediaIndex == cstypes.NONE)
            {
                return Strings.Get(Strings.Common, "Inspector.Polygon.MediaIndex.None");
            }

            return Entity.ParentLevel.Medias.TryGetValue(mediaIndex, out var media) ?
                   Strings.Get(Strings.Common, "Inspector.Polygon.MediaIndex.Choice", mediaIndex, AlephOneNames.MediaType(media.NativeObject.type)) :
                   mediaIndex.ToString();
        }

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
            return NameAndNumber(AlephOneNames.PolygonType(type), type);
        }
    }
}
