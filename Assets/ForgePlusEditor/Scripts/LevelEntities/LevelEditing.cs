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
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Steps shared by several kinds of level edits
    public static class LevelEditing
    {
        // A frame later, so the inspector whose edit this is has finished with its (soon destroyed) entities
        public static async void RebuildLevel(Action afterRebuilding = null)
        {
            LevelHistory.MarkEdited();

            await Awaitable.NextFrameAsync();

            MapsLoading.Instance.RebuildLevel();

            afterRebuilding?.Invoke();
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
