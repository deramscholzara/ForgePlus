using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using RuntimeCore.Entities;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.Inspection
{
    // The shapes collections a surface's texture can be from (the shapes file's wall collections). The original games
    // only load the level's walls and landscape (map.cpp: mark_environment_collections); Aleph One loads every
    // collection the level's surfaces use (map.cpp: mark_map_collections).
    [NoAutoStaticsCleanup]
    public static class CollectionChoices
    {
        // Includes the surface's own collection, even if it isn't a wall collection
        public static List<string> All(ushort shapeDescriptor)
        {
            return Inspector_Base.ChoicesOf(Collections(shapeDescriptor), Choice);
        }

        public static List<string> AlephOneOnly(ushort shapeDescriptor)
        {
            var alephOneOnly = new List<string>();
            foreach (var collection in Collections(shapeDescriptor))
            {
                if (!IsLevelCollection(collection))
                {
                    alephOneOnly.Add(Choice(collection));
                }
            }

            return alephOneOnly;
        }

        public static string Choice(int collection)
        {
            var name = AlephOneNames.Collection(collection);
            var choiceKey = collection == LevelWallCollection() ? "Collection.Choice.LevelWalls" :
                            collection == LevelLandscapeCollection() ? "Collection.Choice.LevelLandscape" :
                            "Collection.Choice.AlephOneOnly";

            return Strings.Get(Strings.Textures, choiceKey, collection, name);
        }

        public static bool TryParse(ushort shapeDescriptor, string choice, out int collection)
        {
            return Inspector_Base.TryFindChoice(Collections(shapeDescriptor), Choice, choice, out collection);
        }

        // Whether the original games load the collection for the level
        public static bool IsLevelCollection(int collection)
        {
            return collection == LevelWallCollection() || collection == LevelLandscapeCollection();
        }

        // Empty unless the texture is from a collection only Aleph One loads
        public static string Note(ushort shapeDescriptor)
        {
            if (shapeDescriptor.IsEmptyShapeDescriptor() || IsLevelCollection(shapeDescriptor.GetCollection()))
            {
                return string.Empty;
            }

            return Strings.Get(Strings.Textures, "Collection.Note.AlephOneOnly",
                               Name(LevelWallCollection()), Name(LevelLandscapeCollection()));
        }

        public static int LevelWallCollection()
        {
            var level = LevelEntity_Level.Instance;

            return WallCollectionOf(level ? level.Level.static_world.environment_code : (short) 0);
        }

        // The environment's first wall collection (map.cpp: mark_environment_collections), or walls1 if it has none
        public static int WallCollectionOf(short environmentCode)
        {
            if (environmentCode >= 0 && environmentCode < map.NUMBER_OF_ENVIRONMENTS)
            {
                for (var i = 0; i < map.NUMBER_OF_ENV_COLLECTIONS; i++)
                {
                    var collection = map.Environments[environmentCode, i];
                    if (collection != cstypes.NONE && ShapesLoading.Instance.IsWallCollection(collection))
                    {
                        return collection;
                    }
                }
            }

            return shape_descriptors._collection_walls1;
        }

        public static int LevelLandscapeCollection()
        {
            var level = LevelEntity_Level.Instance;

            return LandscapeCollectionOf(level ? level.Level.static_world.song_index : (short) 0);
        }

        // As LevelData loads it
        public static int LandscapeCollectionOf(short songIndex)
        {
            return shape_descriptors._collection_landscape1 + songIndex;
        }

        // "18 (walls2)"
        private static string Name(int collection)
        {
            return Strings.Get(Strings.Textures, "Collection.Name", collection, AlephOneNames.Collection(collection));
        }

        private static List<int> Collections(ushort shapeDescriptor)
        {
            var collections = new List<int>();
            for (short collection = 0; collection < shape_descriptors.MAXIMUM_COLLECTIONS; collection++)
            {
                if (ShapesLoading.Instance.IsWallCollection(collection))
                {
                    collections.Add(collection);
                }
            }

            if (!shapeDescriptor.IsEmptyShapeDescriptor() && !collections.Contains(shapeDescriptor.GetCollection()))
            {
                collections.Add(shapeDescriptor.GetCollection());
                collections.Sort();
            }

            return collections;
        }
    }
}
