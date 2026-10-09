#if !NO_EDITING
using AlephOne;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;

namespace ForgePlus.LevelManipulation
{
    // Edits a platform's static data (its settings, which the level saves), then initializes its running state
    // (platform_data) from it, as Aleph One does when loading the level
    public static class PlatformEditing
    {
        // Static data ('plat', as Bungie's editor saved it, which every engine works platforms out from) rather than
        // running state ('PLAT', as Bungie's shipped levels have, which lacks settings such as automatic heights)
        public static bool SavesStaticData(MapLevel level)
        {
            return !level.loaded_wad.modeled_chunks.Contains(tags.PLATFORM_STRUCTURE_TAG) &&
                   level.static_platforms.Count == level.PlatformList.Count;
        }

        // The level's own, or (for a level not saving static data) made from the platform
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

        // An edit that changes which way or how far the platform moves changes the sides around it, so rebuilds the level
        public static void Edit(short platformIndex, Action<static_platform_data> edit, bool changesShape)
        {
            var runtimeLevel = LevelEntity_Level.Instance;
            var level = runtimeLevel.Level;

            SaveStaticData(level);

            // Its travel reaches the faces around it, which gain or lose sides as it reveals or hides them
            SideExposure sideExposure = null;
            if (changesShape)
            {
                sideExposure = new SideExposure();
                sideExposure.RecordPolygon(level, level.PolygonList[level.PlatformList[platformIndex].polygon_index]);
            }

            var staticData = GetStaticData(level, platformIndex);
            edit(staticData);

            platforms.initialize_platform(level, platformIndex, staticData, level.PlatformList[platformIndex].polygon_index);

            if (sideExposure != null)
            {
                sideExposure.ApplyToSides(level);
                LevelEditing.RebuildLevelKeepingSelection();
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

        // With the defaults of a S'pht door (the most common platform)
        public static void MakePlatform(LevelEntity_Polygon polygon)
        {
            var level = polygon.ParentLevel.Level;
            SaveStaticData(level);

            var staticData = platforms.get_defaults_for_platform_type(platforms._platform_is_spht_door).Clone();
            staticData.polygon_index = polygon.NativeIndex;
            staticData.tag = 0;

            platforms.new_platform(level, staticData, polygon.NativeIndex, level.PlatformList.Count + 1);
            level.static_platforms.Add(staticData);
            LineFlagsEditing.UpdateForPlatformChange(level, polygon.NativeObject);

            LevelEditing.RebuildLevelKeepingSelection();
        }

        // Makes a platform polygon another type. Later platforms move down an index, as do their polygons' permutations.
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
            LineFlagsEditing.UpdateForPlatformChange(level, polygon.NativeObject);

            LevelEditing.RebuildLevelKeepingSelection();
        }

        // From the first platform edit on, the level saves static data (which every engine prefers), in the running
        // state's place; an unedited level saves its platforms as they were loaded
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

        // With its worked-out heights made explicit. Aleph One's export conversion takes a ceiling platform's lowest
        // height from its floor, which would change how far it moves, so that's corrected.
        private static static_platform_data StaticDataFromPlatform(platform_data platform)
        {
            var staticData = platforms.static_platform_data_from_platform(platform);

            if (platforms.PLATFORM_COMES_FROM_CEILING(platform) && !platforms.PLATFORM_COMES_FROM_FLOOR(platform))
            {
                staticData.minimum_height = platform.minimum_ceiling_height;
            }

            return staticData;
        }
    }
}
#endif
