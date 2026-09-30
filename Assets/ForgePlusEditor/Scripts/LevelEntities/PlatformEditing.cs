#if !NO_EDITING
using AlephOne;
using ForgePlus.DataFileIO;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Edits a level's platforms as saving and loading see them: a platform's settings are the static data the level saves
    // for it, and its running state (a platform_data) is initialized from those, as Aleph One does when loading the level
    public static class PlatformEditing
    {
        // Whether the level's platforms are saved as static data ('plat': their settings, as Bungie's editor saved them,
        // from which Marathon 2, Infinity and Aleph One all work out the platforms when loading the level), which saving
        // packs as it is while there's an entry for each platform. Otherwise (as in the levels Bungie shipped), they're
        // saved in their running state ('PLAT'), which has only the heights worked out, not the settings they came from
        // (such as automatic heights).
        public static bool SavesStaticData(MapLevel level)
        {
            return !level.loaded_wad.modeled_chunks.Contains(tags.PLATFORM_STRUCTURE_TAG) &&
                   level.static_platforms.Count == level.PlatformList.Count;
        }

        // The platform's static data: the level's own, or (for a level whose platforms haven't been edited, and aren't
        // saved as static data) made from the platform
        public static static_platform_data GetStaticData(MapLevel level, short platformIndex)
        {
            var platform = level.PlatformList[platformIndex];

            if (SavesStaticData(level))
            {
                foreach (var staticData in level.static_platforms)
                {
                    if (staticData.polygon_index == platform.polygon_index)
                    {
                        return staticData;
                    }
                }
            }

            return StaticDataFromPlatform(platform);
        }

        // Changes the platform's static data, then initializes the platform from it again. An edit that changes the
        // platform's shape (which way it moves, or how far) changes the sides around it too, so the level is rebuilt.
        public static void Edit(short platformIndex, Action<static_platform_data> edit, bool changesShape)
        {
            var runtimeLevel = LevelEntity_Level.Instance;
            var level = runtimeLevel.Level;

            SaveStaticData(level);

            var staticData = GetStaticData(level, platformIndex);
            edit(staticData);

            platforms.initialize_platform(level, platformIndex, staticData, level.PlatformList[platformIndex].polygon_index);

            if (changesShape)
            {
                RebuildLevel(() => SelectPlatform(platformIndex));
                return;
            }

            // Both halves of a platform that goes both ways start over with the new settings
            if (runtimeLevel.FloorPlatforms.TryGetValue(platformIndex, out var floorPlatform))
            {
                floorPlatform.UpdatePlatformValues(LevelEntity_Platform.LinkedSurfaces.Floor);
                floorPlatform.BeginRuntimeStyleBehavior();
            }

            if (runtimeLevel.CeilingPlatforms.TryGetValue(platformIndex, out var ceilingPlatform))
            {
                ceilingPlatform.UpdatePlatformValues(LevelEntity_Platform.LinkedSurfaces.Ceiling);
                ceilingPlatform.BeginRuntimeStyleBehavior();
            }
        }

        // Makes the polygon a platform, with the defaults of a S'pht door (the most common platform), as a new platform
        // gets when Aleph One loads a level
        public static void MakePlatform(LevelEntity_Polygon polygon)
        {
            var level = polygon.ParentLevel.Level;
            SaveStaticData(level);

            var staticData = platforms.get_defaults_for_platform_type(platforms._platform_is_spht_door).Clone();
            staticData.polygon_index = polygon.NativeIndex;
            staticData.tag = 0;

            platforms.new_platform(level, staticData, polygon.NativeIndex, level.PlatformList.Count + 1);
            level.static_platforms.Add(staticData);

            var polygonIndex = polygon.NativeIndex;
            RebuildLevel(() => SelectPolygon(polygonIndex));
        }

        // Makes a platform polygon another type, removing its platform. Later platforms move down an index, and the
        // polygons of each remember their platform's index (in their permutation).
        public static void RemovePlatform(LevelEntity_Polygon polygon, short newType)
        {
            var level = polygon.ParentLevel.Level;
            SaveStaticData(level);

            var platformIndex = polygon.NativeObject.permutation;
            var platform = level.PlatformList[platformIndex];

            level.static_platforms.RemoveAll(staticData => staticData.polygon_index == platform.polygon_index);
            level.PlatformList.RemoveAt(platformIndex);

            foreach (var polygonData in level.PolygonList)
            {
                if (polygonData.type == map._polygon_is_platform && polygonData.permutation > platformIndex)
                {
                    polygonData.permutation--;
                }
            }

            // Its permutation was its platform's index, which means nothing to other types
            polygon.NativeObject.type = newType;
            polygon.NativeObject.permutation = 0;

            var polygonIndex = polygon.NativeIndex;
            RebuildLevel(() => SelectPolygon(polygonIndex));
        }

        // From the first edit of its platforms, a level saves them as static data, so it saves their settings as they're
        // set (the original engine's form, which it and Aleph One load in preference to the running state). Each platform
        // keeps the static data it was loaded with, or gets it from its running state, and the static data is saved where
        // the running state was. An unedited level still saves its platforms as they were loaded.
        private static void SaveStaticData(MapLevel level)
        {
            if (SavesStaticData(level))
            {
                // Already saved as static data, so exit
                return;
            }

            var loadedStaticData = level.static_platforms.ToArray();
            level.static_platforms.Clear();

            foreach (var platform in level.PlatformList)
            {
                var staticData = Array.Find(loadedStaticData, candidate => candidate.polygon_index == platform.polygon_index);

                // A platform polygon without static data got the defaults for its type when loaded (as in Aleph One)
                if (staticData == null && !level.loaded_wad.modeled_chunks.Contains(tags.PLATFORM_STRUCTURE_TAG))
                {
                    staticData = platforms.get_defaults_for_platform_type(platform.type).Clone();
                    staticData.polygon_index = platform.polygon_index;
                }

                level.static_platforms.Add(staticData ?? StaticDataFromPlatform(platform));
            }

            var chunks = level.loaded_wad;
            if (chunks.modeled_chunks.Remove(tags.PLATFORM_STRUCTURE_TAG))
            {
                chunks.modeled_chunks.Add(tags.PLATFORM_STATIC_DATA_TAG);

                var runningStateOrder = chunks.chunk_order.IndexOf(tags.PLATFORM_STRUCTURE_TAG);
                if (runningStateOrder >= 0 && !chunks.chunk_order.Contains(tags.PLATFORM_STATIC_DATA_TAG))
                {
                    chunks.chunk_order[runningStateOrder] = tags.PLATFORM_STATIC_DATA_TAG;
                }
            }
        }

        // The static data a platform's running state comes from, with the heights it's worked out (explicit, so the
        // platform loads the same). The conversion Aleph One uses when exporting a level takes a ceiling platform's lowest
        // height from its floor rather than its ceiling, which would change how far it moves, so that's corrected here.
        private static static_platform_data StaticDataFromPlatform(platform_data platform)
        {
            var staticData = platforms.static_platform_data_from_platform(platform);

            if (platforms.PLATFORM_COMES_FROM_CEILING(platform) && !platforms.PLATFORM_COMES_FROM_FLOOR(platform))
            {
                staticData.minimum_height = platform.minimum_ceiling_height;
            }

            return staticData;
        }

        // A frame later, so the inspector whose edit this is has finished with its (soon destroyed) entities
        private static async void RebuildLevel(Action afterRebuilding)
        {
            await Awaitable.NextFrameAsync();

            MapsLoading.Instance.RebuildLevel();

            afterRebuilding();
        }

        private static void SelectPlatform(short platformIndex)
        {
            var platform = LevelEntity_Platform.GetSelectablePlatform(LevelEntity_Level.Instance, platformIndex);

            if (platform)
            {
                SelectionManager.Instance.SelectObject(platform, multiSelect: false);
            }
        }

        private static void SelectPolygon(short polygonIndex)
        {
            if (LevelEntity_Level.Instance.Polygons.TryGetValue(polygonIndex, out var polygon))
            {
                SelectionManager.Instance.SelectObject(polygon, multiSelect: false);
            }
        }
    }
}
#endif
