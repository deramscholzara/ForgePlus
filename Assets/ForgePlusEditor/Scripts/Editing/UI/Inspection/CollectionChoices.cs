using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.Localization;
using RuntimeCore.Entities;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.Inspection
{
    // The shapes collections a surface's texture can be from (the shapes file's wall collections, which landscapes are),
    // as "index - name". The original Marathon 2 and Infinity only load the level's walls (its environment's: map.cpp,
    // mark_environment_collections) and its landscape (picked by its song index), so those are marked as the level's,
    // and the rest as Aleph One only (which loads every collection the level's surfaces use: map.cpp,
    // mark_map_collections).
    [NoAutoStaticsCleanup]
    public static class CollectionChoices
    {
        // The choices for a surface, which include its own collection (even if it isn't a wall collection)
        public static List<string> All(ushort shapeDescriptor)
        {
            var all = new List<string>();
            foreach (var collection in Collections(shapeDescriptor))
            {
                all.Add(Choice(collection));
            }

            return all;
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
            foreach (var candidate in Collections(shapeDescriptor))
            {
                if (Choice(candidate) == choice)
                {
                    collection = candidate;
                    return true;
                }
            }

            collection = cstypes.NONE;
            return false;
        }

        // Whether the original games load the collection for the level
        public static bool IsLevelCollection(int collection)
        {
            return collection == LevelWallCollection() || collection == LevelLandscapeCollection();
        }

        // For a texture from a collection only Aleph One loads, a note saying so (and which are the level's), or else none
        public static string Note(ushort shapeDescriptor)
        {
            if (shapeDescriptor.IsEmptyShapeDescriptor() || IsLevelCollection(shapeDescriptor.GetCollection()))
            {
                return string.Empty;
            }

            return Strings.Get(Strings.Textures, "Collection.Note.AlephOneOnly",
                               Name(LevelWallCollection()), Name(LevelLandscapeCollection()));
        }

        // The first wall collection of the level's environment (as the game loads them: map.cpp,
        // mark_environment_collections), or the first walls collection if it has none
        public static int LevelWallCollection()
        {
            var level = LevelEntity_Level.Instance;
            var environmentCode = level ? level.Level.static_world.environment_code : (short) 0;

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

        // The landscape collection the level's song index picks (as LevelData loads it)
        public static int LevelLandscapeCollection()
        {
            var level = LevelEntity_Level.Instance;

            return shape_descriptors._collection_landscape1 + (level ? level.Level.static_world.song_index : 0);
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
