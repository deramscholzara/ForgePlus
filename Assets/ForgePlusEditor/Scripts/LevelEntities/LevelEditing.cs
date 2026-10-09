#if !NO_EDITING
using AlephOne;
using ForgePlus.DataFileIO;
using ForgePlus.Extensions;
using ForgePlus.History;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Steps shared by several kinds of level edits
    public static class LevelEditing
    {
        private static readonly List<Action> afterPendingRebuild = new List<Action>();

        // While the level's data has changed in ways its entities (soon rebuilt) don't match, such as removed geometry
        public static bool IsRebuildPending { get; private set; }

        // A frame later, so the inspector whose edit this is has finished with its (soon destroyed) entities. Asking again
        // before then rebuilds it once.
        public static async void RebuildLevel(Action afterRebuilding = null)
        {
            LevelHistory.MarkEdited();

            if (afterRebuilding != null)
            {
                afterPendingRebuild.Add(afterRebuilding);
            }

            if (IsRebuildPending)
            {
                return;
            }

            IsRebuildPending = true;

            try
            {
                await Awaitable.NextFrameAsync();

                MapsLoading.Instance.RebuildLevel();
            }
            finally
            {
                IsRebuildPending = false;
            }

            var actions = afterPendingRebuild.ToList();
            afterPendingRebuild.Clear();

            foreach (var action in actions)
            {
                action();
            }
        }

        // Rebuilds the level, then selects again what was selected (as far as it's still there)
        public static void RebuildLevelKeepingSelection()
        {
            var reselect = RememberSelection();

            RebuildLevel(reselect);
        }

        // Returns what selects the same things again (by their indexes) once the level's entities are made again
        public static Action RememberSelection()
        {
            var finders = new List<Func<ISelectable>>();

            foreach (var selected in SelectionManager.Instance.Selection)
            {
                var finder = FinderOf(selected);
                if (finder != null)
                {
                    finders.Add(finder);
                }
            }

            return () =>
            {
                var multiSelect = false;
                foreach (var finder in finders)
                {
                    var selectable = finder();
                    if (selectable != null && !SelectionManager.Instance.GetIsSelected(selectable))
                    {
                        SelectionManager.Instance.SelectObject(selectable, multiSelect);
                        multiSelect = true;
                    }
                }
            };
        }

        // Null for what can't be found again (such as a placeholder side, which has no index)
        private static Func<ISelectable> FinderOf(ISelectable selected)
        {
            switch (selected)
            {
                case LevelEntity_Polygon polygon:
                    return Find(polygon.NativeIndex, level => level.Polygons);
                case LevelEntity_Line line:
                    return Find(line.NativeIndex, level => level.Lines);
                case LevelEntity_Point point:
                    return Find(point.NativeIndex, level => level.Points);
                case LevelEntity_Side side:
                    return side.NativeIndex >= 0 ? Find(side.NativeIndex, level => level.Sides) : null;
                case LevelEntity_Light light:
                    return Find(light.NativeIndex, level => level.Lights);
                case LevelEntity_Media media:
                    return Find(media.NativeIndex, level => level.Medias);
                case LevelEntity_Platform platform:
                    var platformIndex = platform.NativeIndex;
                    return () => LevelEntity_Level.Instance ? LevelEntity_Platform.GetSelectablePlatform(LevelEntity_Level.Instance, platformIndex) : null;
                case LevelEntity_MapObject mapObject:
                    return Find(mapObject.NativeIndex, level => level.MapObjects);
                case LevelEntity_Annotation annotation:
                    return Find(annotation.NativeIndex, level => level.Annotations);
                case LevelEntity_Level _:
                    return () => LevelEntity_Level.Instance;
                case SoundImageEntry entry:
                    var kind = entry.Kind;
                    var index = entry.Index;
                    return () => SoundImageEditing.GetEntry(kind, index);
                default:
                    return null;
            }
        }

        private static Func<ISelectable> Find<TEntity>(short index, Func<LevelEntity_Level, Dictionary<short, TEntity>> entities) where TEntity : class, ISelectable
        {
            return () =>
            {
                var level = LevelEntity_Level.Instance;
                if (!level)
                {
                    return null;
                }

                // A destroyed MonoBehaviour entity isn't found
                return entities(level).TryGetValue(index, out var entity) && !(entity is UnityEngine.Object unityObject && !unityObject) ? entity : null;
            };
        }

        // The level's map indexes, worked out again as loading a level without them works them out: what's near each
        // polygon and the sound sources it hears (map_constructors.cpp: precalculate_map_indexes), then the polygons and
        // lines around each platform's corners (platforms.cpp: new_platform), which come after them in the list
        public static void RecalculateMapIndexes(MapLevel level)
        {
            level.MapIndexList.Clear();
            map_constructors.precalculate_map_indexes(level);

            foreach (var platform in level.PlatformList)
            {
                if (platform.polygon_index < 0 || platform.polygon_index >= level.PolygonList.Count)
                {
                    continue;
                }

                var polygon = level.PolygonList[platform.polygon_index];
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    var owners = platform.endpoint_owners[i];
                    map_constructors.calculate_endpoint_polygon_owners(level, polygon.endpoint_indexes[i], out owners.first_polygon_index, out owners.polygon_index_count);
                    map_constructors.calculate_endpoint_line_owners(level, polygon.endpoint_indexes[i], out owners.first_line_index, out owners.line_index_count);
                }
            }
        }

        // A landscape texture is drawn as a landscape (keeping a big landscape's mode), and other textures aren't
        public static short TransferModeForTexture(ushort shapeDescriptor, short transferMode)
        {
            if (shapeDescriptor.UsesLandscapeCollection())
            {
                return AlephOneExtensions.IsLandscapeTransferMode(transferMode) ? transferMode : map._xfer_landscape;
            }

            return AlephOneExtensions.IsLandscapeTransferMode(transferMode) ? map._xfer_normal : transferMode;
        }
    }
}
#endif
