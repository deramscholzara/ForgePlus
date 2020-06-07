using ForgePlus.Entities.Geometry;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Materials;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Weland;

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

        private readonly TimeSpan chunkLoadMaxTime = TimeSpan.FromSeconds(1.0 / 20); // aim for ~20 fps

        public readonly int LevelIndex;

        private readonly MapFile mapsFile;

        private Level level;
        private LevelEntity_Level FPLevel;

        public string LevelName { get; private set; }

        public LevelData(int levelIndex, MapFile mapsFile)
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

            level = new Level();
            level.Load(mapsFile.Directory[LevelIndex]);

            LevelName = level.Name;

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

        public Wadfile.DirectoryEntry GetSaveWad()
        {
            level.AssurePlayerStart();
            // TODO: Need to re-add Player MapObjects if there were none

            return level.Save();
        }

        public async Task OpenLevel()
        {
            if (FPLevel)
            {
                // Already open, so exit
                return;
            }

            if (level == null)
            {
                var loadDataStartTime = DateTime.Now;

                LoadData();

                Debug.Log($"--- LevelLoad: Loaded level data in timespan: {DateTime.Now - loadDataStartTime}");
            }

            var buildStartTime = DateTime.Now;

            await BuildLevel();

            Debug.Log($"--- LevelBuild: Built full level from data in total timespan: {DateTime.Now - buildStartTime}");

            var initializationStartTime = DateTime.Now;

            LevelInitializationDebugTimer(initializationStartTime);
        }

        public void CloseLevel()
        {
            if (!FPLevel)
            {
                // Not open, so exit
                return;
            }

            FPLevel.PrepareForDestruction();

            UnityEngine.Object.Destroy(FPLevel.gameObject);
        }

        private async Task<DateTime> ChunkLoadYield(DateTime chunkLoadStartTime)
        {
            if (DateTime.Now - chunkLoadStartTime >= chunkLoadMaxTime)
            {
                await Task.Yield();

                chunkLoadStartTime = DateTime.Now;
            }

            return chunkLoadStartTime;
        }

        private async Task BuildLevel()
        {
            var initializeFPLevelStartTime = DateTime.Now;

            FPLevel = new GameObject($"Level ({LevelName})").AddComponent<LevelEntity_Level>();
            FPLevel.Level = level;
            FPLevel.Index = (short)LevelIndex;

            FPLevel.FPPolygons = new Dictionary<short, LevelEntity_Polygon>();
            FPLevel.FPLines = new Dictionary<short, LevelEntity_Line>();
            FPLevel.FPSides = new Dictionary<short, LevelEntity_Side>();
            FPLevel.FPLights = new Dictionary<short, LevelEntity_Light>();
            FPLevel.FPMedias = new Dictionary<short, LevelEntity_Media>();
            FPLevel.FPCeilingFpPlatforms = new Dictionary<short, LevelEntity_Platform>();
            FPLevel.FPFloorFpPlatforms = new Dictionary<short, LevelEntity_Platform>();
            FPLevel.FPMapObjects = new Dictionary<short, LevelEntity_MapObject>();
            FPLevel.FPAnnotations = new Dictionary<short, LevelEntity_Annotation>();

            FPLevel.FPInteractiveSurfacePolygons = new List<EditableSurface_Polygon>();
            FPLevel.FPInteractiveSurfaceSides = new List<EditableSurface_Side>();
            FPLevel.FPInteractiveSurfaceMedias = new List<EditableSurface_Media>();

            // Clear out Walls Materials so it can be repopulated with the correct set
            MaterialGeneration_Geometry.ClearCollection();

            Debug.Log($"--- LevelBuild: Initialized FPLevel in timespan: {DateTime.Now - initializeFPLevelStartTime}");

            await Task.Yield();

            #region Initialization_Textures
            var buildTexturesStartTime = DateTime.Now;

            // Initialize Textures here so they in proper index order
            var landscapeShapeDescriptor = new ShapeDescriptor();
            // Note: Landscape collections in Shapes are respectively sequential to Landscape map info starting at 27
            landscapeShapeDescriptor.Collection = (byte)(level.Landscape + 27);
            MaterialGeneration_Geometry.GetTexture(landscapeShapeDescriptor, returnPlaceholderIfNotFound: false);

            var wallShapeDescriptor = new ShapeDescriptor();
            // Note: Walls collections in Shapes are respectively sequential to Environment map info starting at 17
            wallShapeDescriptor.Collection = (byte)(level.Environment + 17);
            for (var i = 0; i < 256; i++)
            {
                wallShapeDescriptor.Bitmap = (byte)i;
                if (!MaterialGeneration_Geometry.GetTexture(wallShapeDescriptor, returnPlaceholderIfNotFound: false))
                {
                    break;
                }
            }

            Debug.Log($"--- LevelBuild: Built Textures in timespan: {DateTime.Now - buildTexturesStartTime}");
            #endregion Initialization_Textures

            await Task.Yield();

            #region Initialization_Lights
            var buildFPLightsStartTime = DateTime.Now;

            // Initialize Lights here so they are in proper index order
            for (var i = 0; i < level.Lights.Count; i++)
            {
                FPLevel.FPLights[(short)i] = new LevelEntity_Light((short)i, level.Lights[i], FPLevel);
            }

            Debug.Log($"--- LevelBuild: Built & started FPLights in timespan: {DateTime.Now - buildFPLightsStartTime}");
            #endregion Initialization_Lights

            await Task.Yield();

            #region Initialization_Medias
            var buildFPMediasStartTime = DateTime.Now;

            // Initialize Medias here so they are in proper index order
            for (var i = 0; i < level.Medias.Count; i++)
            {
                FPLevel.FPMedias[(short)i] = new LevelEntity_Media((short)i, level.Medias[i], FPLevel);
            }

            Debug.Log($"--- LevelBuild: Built & started FPMedias in timespan: {DateTime.Now - buildFPMediasStartTime}");
            #endregion Initialization_Medias

            await Task.Yield();

            var chunkLoadStartTime = DateTime.Now;

            #region Polygons_And_Media
            var buildPolygonsStartTime = DateTime.Now;

            var polygonsGroupGO = new GameObject("Polygons");
            polygonsGroupGO.transform.SetParent(FPLevel.transform);

            for (short polygonIndex = 0; polygonIndex < level.Polygons.Count; polygonIndex++)
            {
                var polygon = level.Polygons[polygonIndex];

                var polygonRootGO = new GameObject($"Polygon ({polygonIndex})");
                polygonRootGO.transform.SetParent(polygonsGroupGO.transform);

                var fpPolygon = polygonRootGO.AddComponent<LevelEntity_Polygon>();
                FPLevel.FPPolygons[polygonIndex] = fpPolygon;
                fpPolygon.InitializeEntity(FPLevel, polygonIndex, polygon);

                chunkLoadStartTime = await ChunkLoadYield(chunkLoadStartTime);
            }

            Debug.Log($"--- LevelBuild: Built Polygons, Medias, & Platforms in timespan: {DateTime.Now - buildPolygonsStartTime}");
            #endregion Polygons_And_Media

            await Task.Yield();

            #region Lines_And_Sides
            var buildSidesStartTime = DateTime.Now;

            var linesGroupGO = new GameObject("Lines");
            linesGroupGO.transform.SetParent(FPLevel.transform);

            for (short lineIndex = 0; lineIndex < level.Lines.Count; lineIndex++)
            {
                GameObject lineRootGO = new GameObject($"Line ({lineIndex})");
                lineRootGO.transform.SetParent(linesGroupGO.transform);

                var line = level.Lines[lineIndex];

                var fpLine = lineRootGO.AddComponent<LevelEntity_Line>();
                FPLevel.FPLines[lineIndex] = fpLine;
                fpLine.NativeIndex = lineIndex;
                fpLine.NativeObject = line;
                fpLine.FPLevel = FPLevel;

                fpLine.GenerateSurfaces();

                chunkLoadStartTime = await ChunkLoadYield(chunkLoadStartTime);
            }

            Debug.Log($"--- LevelBuild: Built Lines & Sides in timespan: {DateTime.Now - buildSidesStartTime}");
            #endregion Lines_And_Sides

            await Task.Yield();

            #region Objects_And_Placements
            var buildObjectsStartTime = DateTime.Now;

            var mapObjectsGroupGO = new GameObject("MapObjects");
            mapObjectsGroupGO.transform.SetParent(FPLevel.transform);

            for (short objectIndex = 0; objectIndex < level.Objects.Count; objectIndex++)
            {
                var mapObject = level.Objects[objectIndex];

                var mapObjectRootGO = new GameObject($"MapObject: {mapObject.Type} ({objectIndex})");
                mapObjectRootGO.transform.SetParent(mapObjectsGroupGO.transform);

                var fpMapObject = mapObjectRootGO.AddComponent<LevelEntity_MapObject>();
                FPLevel.FPMapObjects[(short)objectIndex] = fpMapObject;
                fpMapObject.NativeIndex = (short)objectIndex;
                fpMapObject.NativeObject = mapObject;
                fpMapObject.FPLevel = FPLevel;

                fpMapObject.GenerateObject();

                chunkLoadStartTime = await ChunkLoadYield(chunkLoadStartTime);
            }

            Debug.Log($"--- LevelBuild: Built Objects in timespan: {DateTime.Now - buildObjectsStartTime}");
            #endregion Objects_And_Placements

            await Task.Yield();

            #region Annotations
            var annotationsGroupGO = new GameObject("Annotations");
            annotationsGroupGO.transform.SetParent(FPLevel.transform);

            for (var i = 0; i < level.Annotations.Count; i++)
            {
                var annotation = level.Annotations[i];
                var annotationInstance = UnityEngine.Object.Instantiate(LevelEntity_Annotation.Prefab);
                annotationInstance.NativeIndex = (short)i;
                annotationInstance.NativeObject = annotation;
                annotationInstance.FPLevel = FPLevel;

                annotationInstance.RefreshLabel();

                var positionalHeight = (FPLevel.FPPolygons[annotation.PolygonIndex].NativeObject.FloorHeight + FPLevel.FPPolygons[annotation.PolygonIndex].NativeObject.CeilingHeight) / 2f / GeometryUtilities.WorldUnitIncrementsPerMeter;
                annotationInstance.transform.position = new Vector3(annotation.X / GeometryUtilities.WorldUnitIncrementsPerMeter, positionalHeight, -annotation.Y / GeometryUtilities.WorldUnitIncrementsPerMeter);

                annotationInstance.transform.SetParent(annotationsGroupGO.transform, worldPositionStays: true);

                FPLevel.FPAnnotations[(short)i] = annotationInstance;
            }
            #endregion Annotations
        }

        private async void LevelInitializationDebugTimer(DateTime startTime)
        {
            // Yield 2 times, to ensure we hit the frame after initialization ocurred
            // (meaning Awake(), Start(), OnEnabled(), etc. all ran)
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();

            Debug.Log($"--- LevelLoad: Initialized level in timespan: {DateTime.Now - startTime}");
        }
    }
}
