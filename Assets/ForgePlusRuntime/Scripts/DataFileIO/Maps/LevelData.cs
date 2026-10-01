using ForgePlus.Entities.Geometry;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using RuntimeCore.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using AlephOne;
using ForgePlus.Extensions;

namespace ForgePlus.DataFileIO
{
    public class LevelData
    {
        private enum SideDataSources
        {
            Primary,
            Secondary,
            Transparent,
        }

        public int LevelIndex { get; private set; }

        private readonly MapsFile mapsFile;

        private MapLevel level;
        private LevelEntity_Level runtimeLevel;

        public string LevelName { get; private set; }

        public LevelData(int levelIndex, MapsFile mapsFile)
        {
            LevelIndex = levelIndex;
            this.mapsFile = mapsFile;
        }

        public void LoadData()
        {
            if (level != null)
            {
                // Already loaded, so exit
                return;
            }

            UnloadData();

            level = mapsFile.LoadLevel(LevelIndex);

            LevelName = level.GetLevelName();

            return;
        }

        public void UnloadData()
        {
            if (level == null)
            {
                // Not loaded, so exit
                return;
            }

            CloseLevel();

            level = null;
        }

        public void SaveAsSingleLevelFile(string savePath)
        {
            mapsFile.SaveAsSingleLevelFile(level, savePath);

            // The saved file holds only this level, and it's now the loaded map file
            LevelIndex = 0;
        }

        // The saved file holds every level (this one where it was), and it's now the loaded map file
        public void SaveMerged(string savePath, bool keepChecksum)
        {
            mapsFile.SaveMerged(level, LevelIndex, savePath, keepChecksum);
        }

        public void OpenLevel()
        {
            if (runtimeLevel)
            {
                // Already open, so exit
                return;
            }

            if (level == null)
            {
                var loadDataStartTime = DateTime.Now;

                LoadData();

                var levelName = level.GetLevelName();
                if (string.IsNullOrWhiteSpace(levelName))
                {
                    levelName = "[unnamed]";
                }

                Debug.Log($"--- LevelLoad: Loaded level data in timespan: {DateTime.Now - loadDataStartTime}\n--- Level Name: {levelName}");
            }

            // Pure data work that the build doesn't need right away runs on worker threads alongside it
            var levelPhysics = PhysicsLoading.Instance.BuildLevelModelAsync(level);
            var endpointLines = Task.Run(() => level.BuildEndpointLines());

            var buildStartTime = DateTime.Now;

            BuildLevel(levelPhysics, endpointLines);

            Debug.Log($"--- LevelBuild: Built full level from data in total timespan: {DateTime.Now - buildStartTime}");

            var initializationStartTime = DateTime.Now;

            LevelInitializationDebugTimer(initializationStartTime);
        }

        public void CloseLevel()
        {
            if (!runtimeLevel)
            {
                // Not open, so exit
                return;
            }

            runtimeLevel.PrepareForDestruction();

            UnityEngine.Object.Destroy(runtimeLevel.gameObject);

            // Destroy waits for the end of the frame, so the level could otherwise still seem open to OpenLevel
            runtimeLevel = null;

            PhysicsLoading.Instance.ClearLevel();
        }

        // Results are waited for directly (rather than awaited), so the build never gives up a frame
        private void BuildLevel(Task<LoadedPhysicsModel> levelPhysics, Task<List<short>[]> endpointLines)
        {
            // CoreCLR players can't change the garbage collector's mode
#if !UNITY_EDITOR && !ENABLE_CORECLR
            GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
#endif

            var initializeLevelStartTime = DateTime.Now;

            runtimeLevel = new GameObject($"Level ({LevelName})").AddComponent<LevelEntity_Level>();
            runtimeLevel.Level = level;
            runtimeLevel.Index = (short) LevelIndex;

            runtimeLevel.Polygons = new Dictionary<short, LevelEntity_Polygon>();
            runtimeLevel.Lines = new Dictionary<short, LevelEntity_Line>();
            runtimeLevel.Sides = new Dictionary<short, LevelEntity_Side>();
            runtimeLevel.Lights = new Dictionary<short, LevelEntity_Light>();
            runtimeLevel.Medias = new Dictionary<short, LevelEntity_Media>();
            runtimeLevel.CeilingPlatforms = new Dictionary<short, LevelEntity_Platform>();
            runtimeLevel.FloorPlatforms = new Dictionary<short, LevelEntity_Platform>();
            runtimeLevel.MapObjects = new Dictionary<short, LevelEntity_MapObject>();
            runtimeLevel.Annotations = new Dictionary<short, LevelEntity_Annotation>();

            runtimeLevel.EditableSurface_Polygons = new List<EditableSurface_Polygon>();
            runtimeLevel.EditableSurface_Sides = new List<EditableSurface_Side>();
            runtimeLevel.EditableSurface_Medias = new List<EditableSurface_Media>();

            // Clear out Walls Materials so it can be repopulated with the correct set
            MaterialGeneration_Geometry.ClearCollection();

            Debug.Log($"--- LevelBuild: Initialized Level in timespan: {DateTime.Now - initializeLevelStartTime}");

            #region Initialization_Textures

            var buildTexturesStartTime = DateTime.Now;

#if !NO_EDITING
            // Initialize Textures here so they in proper index order for the texturing interface
            // Aleph One uses the landscape collection selected by the level's song index (map.cpp: mark_map_collections)
            var shapeDescriptors = new List<ushort>
            {
                AlephOneExtensions.BuildShapeDescriptor(shape_descriptors._collection_landscape1 + level.static_world.song_index, shape: 0),
            };

            // ...and the wall collections of its environment (map.cpp: mark_environment_collections),
            // each up to its first missing shape
            var environmentCode = level.static_world.environment_code;
            if (environmentCode >= 0 && environmentCode < map.NUMBER_OF_ENVIRONMENTS)
            {
                for (var environmentCollectionIndex = 0; environmentCollectionIndex < map.NUMBER_OF_ENV_COLLECTIONS; environmentCollectionIndex++)
                {
                    var collection = map.Environments[environmentCode, environmentCollectionIndex];
                    if (collection == cstypes.NONE || !ShapesLoading.Instance.IsWallCollection(collection))
                    {
                        continue;
                    }

                    for (var shape = 0; shape < shape_descriptors.MAXIMUM_SHAPES_PER_COLLECTION; shape++)
                    {
                        shapeDescriptors.Add(AlephOneExtensions.BuildShapeDescriptor(collection, shape));
                    }
                }
            }

            MaterialGeneration_Geometry.LoadTextures(shapeDescriptors);
#endif

            Debug.Log($"--- LevelBuild: Built Textures in timespan: {DateTime.Now - buildTexturesStartTime}");

            #endregion Initialization_Textures

            // The objects' sprites need the level's physics, and their pixel work then runs on worker threads
            // while the lights, media, polygons and sides are built
            PhysicsLoading.Instance.ApplyLevelModel(level, levelPhysics.GetAwaiter().GetResult());
            MaterialGeneration_Sprites.PrepareSprites(level.SavedObjectList.Select(MapObjectSpriteDefinitions.GetDefinition));

            #region Initialization_Lights

            var buildLightsStartTime = DateTime.Now;

            // Initialize Lights here so they are in proper index order
            for (var i = 0; i < level.LightList.Count; i++)
            {
                runtimeLevel.Lights[(short) i] = new LevelEntity_Light((short) i, level.LightList[i].static_data, runtimeLevel);
            }

            Debug.Log($"--- LevelBuild: Built & started Lights in timespan: {DateTime.Now - buildLightsStartTime}");

            #endregion Initialization_Lights

            #region Initialization_Medias

            var buildMediasStartTime = DateTime.Now;

            // Initialize Medias here so they are in proper index order
            for (var i = 0; i < level.MediaList.Count; i++)
            {
                runtimeLevel.Medias[(short) i] = new LevelEntity_Media((short) i, level.MediaList[i], runtimeLevel);
            }

            Debug.Log($"--- LevelBuild: Built & started Medias in timespan: {DateTime.Now - buildMediasStartTime}");

            #endregion Initialization_Medias

#if USE_TEXTURE_ARRAYS
            MaterialGeneration_Geometry.TextureArraysArePopulating = true;
#endif

            #region Polygons_And_Media

            var buildPolygonsStartTime = DateTime.Now;

            var polygonsGroupGO = new GameObject("Polygons");
            polygonsGroupGO.transform.SetParent(runtimeLevel.transform);

            for (short polygonIndex = 0; polygonIndex < level.PolygonList.Count; polygonIndex++)
            {
                var polygon = level.PolygonList[polygonIndex];

                var polygonRootGO = new GameObject($"Polygon ({polygonIndex})");
                polygonRootGO.transform.SetParent(polygonsGroupGO.transform);

                var runtimePolygon = polygonRootGO.AddComponent<LevelEntity_Polygon>();
                runtimeLevel.Polygons[polygonIndex] = runtimePolygon;
                runtimePolygon.InitializeEntity(runtimeLevel, polygonIndex, polygon);
            }

            Debug.Log($"--- LevelBuild: Built Polygons, Medias, & Platforms in timespan: {DateTime.Now - buildPolygonsStartTime}");

            #endregion Polygons_And_Media

            #region Lines_And_Sides

            var buildSidesStartTime = DateTime.Now;

            var linesGroupGO = new GameObject("Lines");
            linesGroupGO.transform.SetParent(runtimeLevel.transform);

            for (short lineIndex = 0; lineIndex < level.LineList.Count; lineIndex++)
            {
                GameObject lineRootGO = new GameObject($"Line ({lineIndex})");
                lineRootGO.transform.SetParent(linesGroupGO.transform);

                var line = level.LineList[lineIndex];

                var runtimeLine = lineRootGO.AddComponent<LevelEntity_Line>();
                runtimeLevel.Lines[lineIndex] = runtimeLine;
                runtimeLine.NativeIndex = lineIndex;
                runtimeLine.NativeObject = line;
                runtimeLine.ParentLevel = runtimeLevel;

                runtimeLine.GenerateSurfaces();
            }

            Debug.Log($"--- LevelBuild: Built Lines & Sides in timespan: {DateTime.Now - buildSidesStartTime}");

            #endregion Lines_And_Sides

#if USE_TEXTURE_ARRAYS
            MaterialGeneration_Geometry.ApplyTextureArrays();

            MaterialGeneration_Geometry.TextureArraysArePopulating = false;
#endif

            #region Objects_And_Placements

            var buildObjectsStartTime = DateTime.Now;

            var mapObjectsGroupGO = new GameObject("MapObjects");
            mapObjectsGroupGO.transform.SetParent(runtimeLevel.transform);

            for (short objectIndex = 0; objectIndex < level.SavedObjectList.Count; objectIndex++)
            {
                var mapObject = level.SavedObjectList[objectIndex];

                var mapObjectRootGO = new GameObject($"MapObject: {mapObject.GetTypeName()} ({objectIndex})");
                mapObjectRootGO.transform.SetParent(mapObjectsGroupGO.transform);

                var runtimeMapObject = mapObjectRootGO.AddComponent<LevelEntity_MapObject>();
                runtimeLevel.MapObjects[(short) objectIndex] = runtimeMapObject;
                runtimeMapObject.NativeIndex = (short) objectIndex;
                runtimeMapObject.NativeObject = mapObject;
                runtimeMapObject.ParentLevel = runtimeLevel;

                runtimeMapObject.GenerateObject();
            }

            Debug.Log($"--- LevelBuild: Built Objects in timespan: {DateTime.Now - buildObjectsStartTime}");

            #endregion Objects_And_Placements

            #region Annotations

            var annotationsGroupGO = new GameObject("Annotations");
            annotationsGroupGO.transform.SetParent(runtimeLevel.transform);

            for (var i = 0; i < level.MapAnnotationList.Count; i++)
            {
                var annotation = level.MapAnnotationList[i];
                var annotationInstance = new GameObject($"Annotation ({i})").AddComponent<LevelEntity_Annotation>();
                annotationInstance.NativeIndex = (short) i;
                annotationInstance.NativeObject = annotation;
                annotationInstance.ParentLevel = runtimeLevel;

                annotationInstance.RefreshLabel();
                annotationInstance.RefreshPosition();

                annotationInstance.transform.SetParent(annotationsGroupGO.transform, worldPositionStays: true);

                runtimeLevel.Annotations[(short) i] = annotationInstance;
            }

            #endregion Annotations

            // Only editing uses these, so they're the last thing the build waits for
            runtimeLevel.EndpointLines = endpointLines.GetAwaiter().GetResult();

#if !UNITY_EDITOR && !ENABLE_CORECLR
            GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
#endif
        }

        private async void LevelInitializationDebugTimer(DateTime startTime)
        {
            // Wait a few frames, to ensure we hit the frame after initialization occurred
            // (meaning Awake(), Start(), OnEnabled(), etc. all ran)
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();

            Debug.Log($"--- LevelLoad: Initialized level in timespan: {DateTime.Now - startTime}");
        }
    }
}