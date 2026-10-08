#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.Inspection;
using ForgePlus.PolygonContainment;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using static AlephOne.map;

namespace ForgePlus.LevelManipulation
{
    // Floor and ceiling heights, with the level's derived data (side types, line and endpoint heights and flags, and
    // automatic platform travel) kept as Bungie's editor kept it, and only the affected faces, sides and objects rebuilt
    [AutoStaticsCleanup]
    public static partial class HeightsEditing
    {
        // Snapped heights are multiples of a sixteenth of a world unit
        public const int SnapIncrement = world.WORLD_ONE / 16;

        // After a change is committed (as a drag ends, rather than while it moves)
        public static event Action OnHeightsChanged;

        private static readonly HashSet<LevelEntity_Polygon> draggedPolygons = new HashSet<LevelEntity_Polygon>();

        // A platform's heights are its platform's
        public static bool CanEdit(LevelEntity_Polygon polygon)
        {
            return polygon && polygon.NativeObject.type != _polygon_is_platform;
        }

        public static short GetHeight(LevelEntity_Polygon polygon, LevelEntity_Polygon.DataSources dataSource)
        {
            return dataSource == LevelEntity_Polygon.DataSources.Ceiling ? polygon.NativeObject.ceiling_height : polygon.NativeObject.floor_height;
        }

        // A floor can meet its ceiling, but not pass it
        public static bool IsAllowed(LevelEntity_Polygon polygon, LevelEntity_Polygon.DataSources dataSource, int height)
        {
            return height == ClampHeight(polygon, dataSource, height);
        }

        public static short ClampHeight(LevelEntity_Polygon polygon, LevelEntity_Polygon.DataSources dataSource, int height)
        {
            return dataSource == LevelEntity_Polygon.DataSources.Ceiling ?
                   (short) Mathf.Clamp(height, polygon.NativeObject.floor_height, short.MaxValue) :
                   (short) Mathf.Clamp(height, short.MinValue, polygon.NativeObject.ceiling_height);
        }

        // Faces that can't take the height are left as they are
        public static void SetHeights(IEnumerable<LevelEntity_Polygon> polygons, LevelEntity_Polygon.DataSources dataSource, short height)
        {
            var changedPolygons = new List<LevelEntity_Polygon>();
            SurfaceBatchingManager.Instance.DeferMerging(true);

            try
            {
                changedPolygons = Apply(polygons, dataSource, height);
            }
            finally
            {
                Commit(changedPolygons);
            }
        }

        public static void BeginDrag()
        {
            draggedPolygons.Clear();
            SurfaceBatchingManager.Instance.DeferMerging(true);
        }

        public static void DragHeight(LevelEntity_Polygon polygon, LevelEntity_Polygon.DataSources dataSource, short height)
        {
            draggedPolygons.UnionWith(Apply(new[] { polygon }, dataSource, height));
        }

        public static void EndDrag()
        {
            Commit(draggedPolygons.ToList());
            draggedPolygons.Clear();
        }

        // Returns the polygons whose height changed
        private static List<LevelEntity_Polygon> Apply(IEnumerable<LevelEntity_Polygon> polygons, LevelEntity_Polygon.DataSources dataSource, short height)
        {
            var level = LevelEntity_Level.Instance;
            var data = level.Level;

            var changedPolygons = polygons.Where(polygon => CanEdit(polygon) &&
                                                            IsAllowed(polygon, dataSource, height) &&
                                                            GetHeight(polygon, dataSource) != height)
                                          .ToList();

            // Whether each line was closed off by its polygons' heights before any of them changed
            var lineClosures = new Dictionary<short, bool>();
            var endpointIndexes = new HashSet<short>();

            foreach (var polygon in changedPolygons)
            {
                var nativePolygon = polygon.NativeObject;

                for (var i = 0; i < nativePolygon.vertex_count; i++)
                {
                    var lineIndex = nativePolygon.line_indexes[i];
                    if (!lineClosures.ContainsKey(lineIndex))
                    {
                        lineClosures[lineIndex] = LineFlagsEditing.IsClosedByHeights(data, lineIndex);
                    }

                    endpointIndexes.Add(nativePolygon.endpoint_indexes[i]);
                }

                if (dataSource == LevelEntity_Polygon.DataSources.Ceiling)
                {
                    nativePolygon.ceiling_height = height;
                }
                else
                {
                    nativePolygon.floor_height = height;
                }
            }

            foreach (var lineClosure in lineClosures)
            {
                LineFlagsEditing.UpdateForHeightChange(data, lineClosure.Key, lineClosure.Value);
                RecalculateSideTypes(data, data.LineList[lineClosure.Key]);
            }

            // After the lines, whose solidity they take
            foreach (var endpointIndex in endpointIndexes)
            {
                map_constructors.recalculate_redundant_endpoint_data(data, endpointIndex);
            }

            foreach (var polygon in changedPolygons)
            {
                var surface = dataSource == LevelEntity_Polygon.DataSources.Ceiling ? polygon.CeilingSurface : polygon.FloorSurface;
                surface.ApplyHeight(rebatchImmediately: false);
            }

            foreach (var lineIndex in lineClosures.Keys)
            {
                level.Lines[lineIndex].RegenerateSurfaces();
            }

            // Objects (on the floor, or hanging from the ceiling) and annotations (halfway up) follow their polygons
            var polygonIndexes = new HashSet<short>(changedPolygons.Select(polygon => polygon.NativeIndex));

            foreach (var mapObject in level.MapObjects.Values)
            {
                if (polygonIndexes.Contains(mapObject.NativeObject.polygon_index))
                {
                    mapObject.ApplyPlacement();
                }
            }

            foreach (var annotation in level.Annotations.Values)
            {
                if (polygonIndexes.Contains(annotation.NativeObject.polygon_index))
                {
                    annotation.RefreshPosition();
                }
            }

            return changedPolygons;
        }

        // A platform whose travel changed changes the sides around it, so the level is rebuilt
        private static void Commit(List<LevelEntity_Polygon> changedPolygons)
        {
            var platformsChanged = changedPolygons.Count > 0 && RefreshAdjacentPlatforms(changedPolygons);

            SurfaceBatchingManager.Instance.DeferMerging(false);

            if (changedPolygons.Count == 0)
            {
                return;
            }

            PolygonContainmentMap.MarkChanged();

            OnHeightsChanged?.Invoke();
            InspectorPanel.Instance.RefreshAllInspectors();

            if (platformsChanged)
            {
                var selectedPolygon = SelectionManager.Instance.SelectedObject as LevelEntity_Polygon;
                var selectedPolygonIndex = selectedPolygon ? selectedPolygon.NativeIndex : cstypes.NONE;

                LevelEditing.RebuildLevel(() =>
                {
                    if (LevelEntity_Level.Instance.Polygons.TryGetValue(selectedPolygonIndex, out var polygon))
                    {
                        SelectionManager.Instance.SelectObject(polygon, multiSelect: false);
                    }
                });
            }
        }

        // As recalculate_side_type (map_constructors.cpp) works them out
        private static void RecalculateSideTypes(MapLevel data, line_data line)
        {
            if (line.clockwise_polygon_side_index != cstypes.NONE)
            {
                map_constructors.recalculate_side_type(data, line.clockwise_polygon_side_index);
            }

            if (line.counterclockwise_polygon_side_index != cstypes.NONE)
            {
                map_constructors.recalculate_side_type(data, line.counterclockwise_polygon_side_index);
            }
        }

        // Neighboring platforms that work out their travel from the polygons around them (platforms.cpp:
        // calculate_platform_extrema) are initialized again, as loading the level would. Returns whether any travel changed.
        private static bool RefreshAdjacentPlatforms(List<LevelEntity_Polygon> changedPolygons)
        {
            var data = LevelEntity_Level.Instance.Level;

            // Running state ('PLAT') keeps the travel it was saved with
            if (!PlatformEditing.SavesStaticData(data))
            {
                return false;
            }

            var platformIndexes = new HashSet<short>();
            foreach (var polygon in changedPolygons)
            {
                var nativePolygon = polygon.NativeObject;

                for (var i = 0; i < nativePolygon.vertex_count; i++)
                {
                    var adjacentPolygonIndex = nativePolygon.adjacent_polygon_indexes[i];
                    if (adjacentPolygonIndex != cstypes.NONE && data.PolygonList[adjacentPolygonIndex].type == _polygon_is_platform)
                    {
                        platformIndexes.Add(data.PolygonList[adjacentPolygonIndex].permutation);
                    }
                }
            }

            var anyChanged = false;

            foreach (var platformIndex in platformIndexes)
            {
                var staticData = PlatformEditing.GetStaticData(data, platformIndex);
                if (staticData.minimum_height != cstypes.NONE && staticData.maximum_height != cstypes.NONE)
                {
                    continue;
                }

                // Initialized as a copy, which replaces the platform only if its travel changed
                var original = data.PlatformList[platformIndex];
                var candidate = original.Clone();
                data.PlatformList[platformIndex] = candidate;

                var mapIndexCount = data.MapIndexList.Count;
                platforms.initialize_platform(data, platformIndex, staticData, original.polygon_index);

                // Its endpoints' polygons and lines are unchanged, so their map indexes still are too
                data.MapIndexList.RemoveRange(mapIndexCount, data.MapIndexList.Count - mapIndexCount);
                for (var i = 0; i < original.endpoint_owners.Length; i++)
                {
                    candidate.endpoint_owners[i] = original.endpoint_owners[i].Clone();
                }

                if (candidate.minimum_floor_height == original.minimum_floor_height &&
                    candidate.maximum_floor_height == original.maximum_floor_height &&
                    candidate.minimum_ceiling_height == original.minimum_ceiling_height &&
                    candidate.maximum_ceiling_height == original.maximum_ceiling_height)
                {
                    data.PlatformList[platformIndex] = original;
                    continue;
                }

                anyChanged = true;

                var platformPolygon = data.PolygonList[original.polygon_index];
                for (var i = 0; i < platformPolygon.vertex_count; i++)
                {
                    RecalculateSideTypes(data, data.LineList[platformPolygon.line_indexes[i]]);
                }
            }

            return anyChanged;
        }
    }
}
#endif
