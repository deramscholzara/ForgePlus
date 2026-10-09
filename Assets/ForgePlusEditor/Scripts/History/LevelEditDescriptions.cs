#if !NO_EDITING
using AlephOne;
using ForgePlus.Localization;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.History
{
    // Names an edit for the Undo and Redo tooltips (such as "Polygon 12" or "3 sides") after the first of these it changed,
    // since edits also change data worked out from what was edited (a polygon's height changes its sides and lines)
    [NoAutoStaticsCleanup]
    public static class LevelEditDescriptions
    {
        // Each tracked list's entries, as "History.{Name}" (one) and "History.{Name}.Many" (several)
        private static readonly (string Data, string Name)[] listNames =
        {
            (nameof(MapLevel.PlatformList), "Platform"),
            (nameof(MapLevel.static_platforms), "Platform"),
            (nameof(MapLevel.PolygonList), "Polygon"),
            (nameof(MapLevel.SideList), "Side"),
            (nameof(MapLevel.LineList), "Line"),
            (nameof(MapLevel.SavedObjectList), "Object"),
            (nameof(MapLevel.MapAnnotationList), "Annotation"),
            (nameof(MapLevel.LightList), "Light"),
            (nameof(MapLevel.MediaList), "Media"),
            (nameof(MapLevel.AmbientSoundImageList), "AmbientSound"),
            (nameof(MapLevel.RandomSoundImageList), "RandomSound"),
            (nameof(MapLevel.map_terminal_text), "Terminal"),
            (nameof(MapLevel.EndpointList), "Endpoint"),
        };

        // Single values, as "History.{Name}"
        private static readonly (string Data, string Name)[] valueNames =
        {
            (nameof(MapLevel.object_placement_info), "Placements"),
            (nameof(MapLevel.static_world), "Level"),
            (LevelHistory.RemovedResourcesData, "Level"),
            (LevelHistory.MapNameData, "Map"),
        };

        // Polygon fields worked out from other data, whose changes alone don't make a polygon one the edit was of (such
        // as the polygon facing a side that a height edit added)
        private static readonly HashSet<string> derivedPolygonFields = new HashSet<string>
        {
            nameof(polygon_data.side_indexes),
        };

        public static string Describe(IReadOnlyList<DataChange> changes)
        {
            foreach (var (data, name) in listNames)
            {
                foreach (var change in changes)
                {
                    if (change.Data.Name == data && change is TrackedList.ListChange listChange)
                    {
                        var editedIndexes = EditedIndexes(listChange);
                        if (editedIndexes.Count == 0)
                        {
                            continue;
                        }

                        return editedIndexes.Count == 1 ?
                               Strings.Get(Strings.Common, $"History.{name}", editedIndexes[0]) :
                               Strings.Get(Strings.Common, $"History.{name}.Many", editedIndexes.Count);
                    }
                }
            }

            foreach (var (data, name) in valueNames)
            {
                foreach (var change in changes)
                {
                    if (change.Data.Name == data)
                    {
                        return Strings.Get(Strings.Common, $"History.{name}");
                    }
                }
            }

            // Anything else in the level
            return Strings.Get(Strings.Common, "History.Level");
        }

        private static List<int> EditedIndexes(TrackedList.ListChange change)
        {
            var indexes = new List<int>();
            var isPolygons = change.Data.Name == nameof(MapLevel.PolygonList);

            for (var elementIndex = 0; elementIndex < change.Elements.Count; elementIndex++)
            {
                var element = change.Elements[elementIndex];

                if (isPolygons && !element.IsAddedOrRemoved && change.ChangedFields(elementIndex).IsSubsetOf(derivedPolygonFields))
                {
                    continue;
                }

                indexes.Add(element.Index);
            }

            return indexes;
        }
    }
}
#endif
