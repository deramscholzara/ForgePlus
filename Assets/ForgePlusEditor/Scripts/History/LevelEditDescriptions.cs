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
            // Moving points changes much of the data around them
            (nameof(MapLevel.EndpointList), "Endpoint"),
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
        };

        // Single values, as "History.{Name}"
        private static readonly (string Data, string Name)[] valueNames =
        {
            (nameof(MapLevel.object_placement_info), "Placements"),
            (nameof(MapLevel.static_world), "Level"),
            (LevelHistory.RemovedResourcesData, "Level"),
            (LevelHistory.MapNameData, "Map"),
        };

        // Fields worked out from other data, whose changes alone don't make an entry one the edit was of (such as the
        // polygon facing a side that a height edit added, or the points around a polygon whose height changed)
        private static readonly Dictionary<string, HashSet<string>> derivedFields = new Dictionary<string, HashSet<string>>
        {
            {
                nameof(MapLevel.PolygonList), new HashSet<string>
                {
                    nameof(polygon_data.side_indexes),
                    nameof(polygon_data.area),
                    nameof(polygon_data.center),
                    nameof(polygon_data.first_exclusion_zone_index),
                    nameof(polygon_data.line_exclusion_zone_count),
                    nameof(polygon_data.point_exclusion_zone_count),
                    nameof(polygon_data.first_neighbor_index),
                    nameof(polygon_data.neighbor_count),
                    nameof(polygon_data.sound_source_indexes),
                }
            },
            {
                nameof(MapLevel.EndpointList), new HashSet<string>
                {
                    nameof(endpoint_data.flags),
                    nameof(endpoint_data.highest_adjacent_floor_height),
                    nameof(endpoint_data.lowest_adjacent_ceiling_height),
                    nameof(endpoint_data.transformed),
                    nameof(endpoint_data.supporting_polygon_index),
                }
            },
            {
                nameof(MapLevel.PlatformList), new HashSet<string>
                {
                    nameof(platform_data.endpoint_owners),
                }
            },
            {
                nameof(MapLevel.LineList), new HashSet<string>
                {
                    nameof(line_data.length),
                }
            },
            {
                nameof(MapLevel.SideList), new HashSet<string>
                {
                    nameof(side_data.exclusion_zone),
                }
            },
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
            derivedFields.TryGetValue(change.Data.Name, out var derived);

            for (var elementIndex = 0; elementIndex < change.Elements.Count; elementIndex++)
            {
                var element = change.Elements[elementIndex];

                if (derived != null && !element.IsAddedOrRemoved && change.ChangedFields(elementIndex).IsSubsetOf(derived))
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
