﻿using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using ForgePlus.ShapesCollections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
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

        public const int GeometryBuildChunkSize = 256;

        public static Action<string> OnLevelOpened;
        public static Action OnLevelClosed;

        public readonly int LevelIndex;

        private readonly MapFile mapsFile;

        private Level level;
        private FPLevel FPLevel;

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

            OnLevelOpened(level.Name);

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

            OnLevelClosed();
        }

        private async Task BuildLevel()
        {
            var initializeFPLevelStartTime = DateTime.Now;

            FPLevel = new GameObject($"Level ({LevelName})").AddComponent<FPLevel>();
            FPLevel.Level = level;
            FPLevel.Index = (short)LevelIndex;

            FPLevel.FPPolygons = new Dictionary<short, FPPolygon>();
            FPLevel.FPLines = new Dictionary<short, FPLine>();
            FPLevel.FPSides = new Dictionary<short, FPSide>();
            FPLevel.FPLights = new Dictionary<short, FPLight>();
            FPLevel.FPMedias = new Dictionary<short, FPMedia>();
            FPLevel.FPCeilingFpPlatforms = new Dictionary<short, FPPlatform>();
            FPLevel.FPFloorFpPlatforms = new Dictionary<short, FPPlatform>();
            FPLevel.FPMapObjects = new Dictionary<short, FPMapObject>();
            FPLevel.FPAnnotations = new Dictionary<short, FPAnnotation>();

            FPLevel.FPInteractiveSurfacePolygons = new List<FPInteractiveSurfacePolygon>();
            FPLevel.FPInteractiveSurfaceSides = new List<FPInteractiveSurfaceSide>();
            FPLevel.FPInteractiveSurfaceMedias = new List<FPInteractiveSurfaceMedia>();

            // Clear out Walls Materials so it can be repopulated with the correct set
            WallsCollection.ClearCollection();

            Debug.Log($"--- LevelBuild: Initialized FPLevel in timespan: {DateTime.Now - initializeFPLevelStartTime}");

            await Task.Yield();

            #region Initialization_Textures
            var buildTexturesStartTime = DateTime.Now;

            // Initialize Textures here so they in proper index order
            var landscapeShapeDescriptor = new ShapeDescriptor();
            // Note: Landscape collections in Shapes are respectively sequential to Landscape map info starting at 27
            landscapeShapeDescriptor.Collection = (byte)(level.Landscape + 27);
            WallsCollection.GetTexture(landscapeShapeDescriptor, returnPlaceholderIfNotFound: false);

            var wallShapeDescriptor = new ShapeDescriptor();
            // Note: Walls collections in Shapes are respectively sequential to Environment map info starting at 17
            wallShapeDescriptor.Collection = (byte)(level.Environment + 17);
            for (var i = 0; i < 256; i++)
            {
                wallShapeDescriptor.Bitmap = (byte)i;
                if (!WallsCollection.GetTexture(wallShapeDescriptor, returnPlaceholderIfNotFound: false))
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
                FPLevel.FPLights[(short)i] = new FPLight((short)i, level.Lights[i], FPLevel);
            }

            Debug.Log($"--- LevelBuild: Built & started FPLights in timespan: {DateTime.Now - buildFPLightsStartTime}");
            #endregion Initialization_Lights

            await Task.Yield();

            #region Initialization_Medias
            var buildFPMediasStartTime = DateTime.Now;

            // Initialize Medias here so they are in proper index order
            for (var i = 0; i < level.Medias.Count; i++)
            {
                FPLevel.FPMedias[(short)i] = new FPMedia((short)i, level.Medias[i], FPLevel);
            }

            Debug.Log($"--- LevelBuild: Built & started FPMedias in timespan: {DateTime.Now - buildFPMediasStartTime}");
            #endregion Initialization_Medias

            await Task.Yield();

            #region Polygons_And_Media
            var buildPolygonsStartTime = DateTime.Now;

            var polygonsGroupGO = new GameObject("Polygons");
            polygonsGroupGO.transform.SetParent(FPLevel.transform);

            for (var polygonIndex = 0; polygonIndex < level.Polygons.Count; polygonIndex++)
            {
                var polygon = level.Polygons[polygonIndex];

                var polygonRootGO = new GameObject($"Polygon ({polygonIndex})");
                polygonRootGO.transform.SetParent(polygonsGroupGO.transform);

                var fpPolygon = polygonRootGO.AddComponent<FPPolygon>();
                FPLevel.FPPolygons[(short)polygonIndex] = fpPolygon;
                fpPolygon.Index = (short)polygonIndex;
                fpPolygon.WelandObject = polygon;
                fpPolygon.FPLevel = FPLevel;

                fpPolygon.GenerateSurfaces(polygon, (short)polygonIndex);

                if (polygonIndex > 0 && polygonIndex % GeometryBuildChunkSize == 0)
                {
                    await Task.Yield();
                }
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

                var fpLine = lineRootGO.AddComponent<FPLine>();
                FPLevel.FPLines[lineIndex] = fpLine;
                fpLine.Index = lineIndex;
                fpLine.WelandObject = line;
                fpLine.FPLevel = FPLevel;

                fpLine.GenerateSurfaces();

                if (lineIndex > 0 && lineIndex % GeometryBuildChunkSize == 0)
                {
                    await Task.Yield();
                }
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

                var fpMapObject = mapObjectRootGO.AddComponent<FPMapObject>();
                FPLevel.FPMapObjects[(short)objectIndex] = fpMapObject;
                fpMapObject.Index = (short)objectIndex;
                fpMapObject.WelandObject = mapObject;
                fpMapObject.FPLevel = FPLevel;

                fpMapObject.GenerateObject();

                if (objectIndex > 0 && objectIndex % GeometryBuildChunkSize == 0)
                {
                    await Task.Yield();
                }
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
                var annotationInstance = UnityEngine.Object.Instantiate(FPAnnotation.Prefab);
                annotationInstance.Index = (short)i;
                annotationInstance.WelandObject = annotation;
                annotationInstance.FPLevel = FPLevel;

                annotationInstance.RefreshLabel();

                var positionalHeight = (FPLevel.FPPolygons[annotation.PolygonIndex].WelandObject.FloorHeight + FPLevel.FPPolygons[annotation.PolygonIndex].WelandObject.CeilingHeight) / 2f / GeometryUtilities.WorldUnitIncrementsPerMeter;
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
