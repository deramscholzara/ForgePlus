using ForgePlus.Entities.Geometry;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using RuntimeCore.Materials;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
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
        private LevelEntity_Level runtimeLevel;

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
            if (runtimeLevel)
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
            if (!runtimeLevel)
            {
                // Not open, so exit
                return;
            }

            runtimeLevel.PrepareForDestruction();

            UnityEngine.Object.Destroy(runtimeLevel.gameObject);
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
            GarbageCollector.GCMode = GarbageCollector.Mode.Disabled;
            
            var initializeLevelStartTime = DateTime.Now;

            runtimeLevel = new GameObject($"Level ({LevelName})").AddComponent<LevelEntity_Level>();
            runtimeLevel.Level = level;
            runtimeLevel.Index = (short)LevelIndex;

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

            await Task.Yield();

            #region Initialization_Textures
            var buildTexturesStartTime = DateTime.Now;
            
#if !NO_EDITING
            // Initialize Textures here so they in proper index order for the texturing interface
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
#endif
            
            Debug.Log($"--- LevelBuild: Built Textures in timespan: {DateTime.Now - buildTexturesStartTime}");
            #endregion Initialization_Textures

            await Task.Yield();

            #region Initialization_Lights
            var buildLightsStartTime = DateTime.Now;

            // Initialize Lights here so they are in proper index order
            for (var i = 0; i < level.Lights.Count; i++)
            {
                runtimeLevel.Lights[(short)i] = new LevelEntity_Light((short)i, level.Lights[i], runtimeLevel);
            }

            Debug.Log($"--- LevelBuild: Built & started Lights in timespan: {DateTime.Now - buildLightsStartTime}");
            #endregion Initialization_Lights

            await Task.Yield();

            #region Initialization_Medias
            var buildMediasStartTime = DateTime.Now;

            // Initialize Medias here so they are in proper index order
            for (var i = 0; i < level.Medias.Count; i++)
            {
                runtimeLevel.Medias[(short)i] = new LevelEntity_Media((short)i, level.Medias[i], runtimeLevel);
            }

            Debug.Log($"--- LevelBuild: Built & started Medias in timespan: {DateTime.Now - buildMediasStartTime}");
            #endregion Initialization_Medias

            await Task.Yield();

            var chunkLoadStartTime = DateTime.Now;

            #region Polygons_And_Media
            var buildPolygonsStartTime = DateTime.Now;

            var polygonsGroupGO = new GameObject("Polygons");
            polygonsGroupGO.transform.SetParent(runtimeLevel.transform);

            for (short polygonIndex = 0; polygonIndex < level.Polygons.Count; polygonIndex++)
            {
                var polygon = level.Polygons[polygonIndex];

                var polygonRootGO = new GameObject($"Polygon ({polygonIndex})");
                polygonRootGO.transform.SetParent(polygonsGroupGO.transform);

                var runtimePolygon = polygonRootGO.AddComponent<LevelEntity_Polygon>();
                runtimeLevel.Polygons[polygonIndex] = runtimePolygon;
                runtimePolygon.InitializeEntity(runtimeLevel, polygonIndex, polygon);

                chunkLoadStartTime = await ChunkLoadYield(chunkLoadStartTime);
            }

            Debug.Log($"--- LevelBuild: Built Polygons, Medias, & Platforms in timespan: {DateTime.Now - buildPolygonsStartTime}");
            #endregion Polygons_And_Media

            await Task.Yield();

            #region Lines_And_Sides
            var buildSidesStartTime = DateTime.Now;

            var linesGroupGO = new GameObject("Lines");
            linesGroupGO.transform.SetParent(runtimeLevel.transform);

            for (short lineIndex = 0; lineIndex < level.Lines.Count; lineIndex++)
            {
                GameObject lineRootGO = new GameObject($"Line ({lineIndex})");
                lineRootGO.transform.SetParent(linesGroupGO.transform);

                var line = level.Lines[lineIndex];

                var runtimeLine = lineRootGO.AddComponent<LevelEntity_Line>();
                runtimeLevel.Lines[lineIndex] = runtimeLine;
                runtimeLine.NativeIndex = lineIndex;
                runtimeLine.NativeObject = line;
                runtimeLine.ParentLevel = runtimeLevel;

                runtimeLine.GenerateSurfaces();

                chunkLoadStartTime = await ChunkLoadYield(chunkLoadStartTime);
            }

            Debug.Log($"--- LevelBuild: Built Lines & Sides in timespan: {DateTime.Now - buildSidesStartTime}");
            #endregion Lines_And_Sides

            await Task.Yield();

            #region Objects_And_Placements
            var buildObjectsStartTime = DateTime.Now;

            var mapObjectsGroupGO = new GameObject("MapObjects");
            mapObjectsGroupGO.transform.SetParent(runtimeLevel.transform);

            for (short objectIndex = 0; objectIndex < level.Objects.Count; objectIndex++)
            {
                var mapObject = level.Objects[objectIndex];

                var mapObjectRootGO = new GameObject($"MapObject: {mapObject.Type} ({objectIndex})");
                mapObjectRootGO.transform.SetParent(mapObjectsGroupGO.transform);

                var runtimeMapObject = mapObjectRootGO.AddComponent<LevelEntity_MapObject>();
                runtimeLevel.MapObjects[(short)objectIndex] = runtimeMapObject;
                runtimeMapObject.NativeIndex = (short)objectIndex;
                runtimeMapObject.NativeObject = mapObject;
                runtimeMapObject.ParentLevel = runtimeLevel;

                runtimeMapObject.GenerateObject();

                chunkLoadStartTime = await ChunkLoadYield(chunkLoadStartTime);
            }

            Debug.Log($"--- LevelBuild: Built Objects in timespan: {DateTime.Now - buildObjectsStartTime}");
            #endregion Objects_And_Placements

            await Task.Yield();

            #region Annotations
            var annotationsGroupGO = new GameObject("Annotations");
            annotationsGroupGO.transform.SetParent(runtimeLevel.transform);

            for (var i = 0; i < level.Annotations.Count; i++)
            {
                var annotation = level.Annotations[i];
                var annotationInstance = UnityEngine.Object.Instantiate(LevelEntity_Annotation.Prefab);
                annotationInstance.NativeIndex = (short)i;
                annotationInstance.NativeObject = annotation;
                annotationInstance.ParentLevel = runtimeLevel;

                annotationInstance.RefreshLabel();

                var positionalHeight = (runtimeLevel.Polygons[annotation.PolygonIndex].NativeObject.FloorHeight + runtimeLevel.Polygons[annotation.PolygonIndex].NativeObject.CeilingHeight) / 2f / GeometryUtilities.WorldUnitIncrementsPerMeter;
                annotationInstance.transform.position = new Vector3(annotation.X / GeometryUtilities.WorldUnitIncrementsPerMeter, positionalHeight, -annotation.Y / GeometryUtilities.WorldUnitIncrementsPerMeter);

                annotationInstance.transform.SetParent(annotationsGroupGO.transform, worldPositionStays: true);

                runtimeLevel.Annotations[(short)i] = annotationInstance;
            }
            #endregion Annotations

            GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
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
