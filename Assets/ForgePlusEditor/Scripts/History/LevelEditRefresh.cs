#if !NO_EDITING
using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.Palette;
using ForgePlus.PolygonContainment;
using ForgePlus.UI;
using RuntimeCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.History
{
    // Shows restored level data after undoing or redoing. Changed entities take their data again, as editing them
    // would, and changes to the level's structure (or to data not handled here) rebuild the level.
    [NoAutoStaticsCleanup]
    public static class LevelEditRefresh
    {
        // Data without entities of its own (inspectors, errors and the palette are refreshed for every change)
        private static readonly HashSet<string> dataOnly = new HashSet<string>
        {
            nameof(MapLevel.MapIndexList),
            nameof(MapLevel.object_placement_info),
            nameof(MapLevel.loaded_wad),
            LevelHistory.MapNameData,
            LevelHistory.RemovedResourcesData,
        };

        private static readonly HashSet<string> refreshablePolygonFields = new HashSet<string>
        {
            nameof(polygon_data.type),
            nameof(polygon_data.flags),
            nameof(polygon_data.permutation),
            nameof(polygon_data.floor_texture),
            nameof(polygon_data.ceiling_texture),
            nameof(polygon_data.floor_height),
            nameof(polygon_data.ceiling_height),
            nameof(polygon_data.floor_lightsource_index),
            nameof(polygon_data.ceiling_lightsource_index),
            nameof(polygon_data.floor_transfer_mode),
            nameof(polygon_data.ceiling_transfer_mode),
            nameof(polygon_data.floor_origin),
            nameof(polygon_data.ceiling_origin),
            nameof(polygon_data.media_index),
            nameof(polygon_data.media_lightsource_index),
            nameof(polygon_data.sound_source_indexes),
            nameof(polygon_data.ambient_sound_image_index),
            nameof(polygon_data.random_sound_image_index),

            // Worked out from its lines, which are rebuilt for their own changes
            nameof(polygon_data.side_indexes),

            // Worked out from where its corners are (which are refreshed for their own changes), and the map indexes
            nameof(polygon_data.area),
            nameof(polygon_data.center),
            nameof(polygon_data.first_exclusion_zone_index),
            nameof(polygon_data.line_exclusion_zone_count),
            nameof(polygon_data.point_exclusion_zone_count),
            nameof(polygon_data.first_neighbor_index),
            nameof(polygon_data.neighbor_count),
        };

        // Where its corners' polygons and lines are listed in the map indexes
        private static readonly HashSet<string> platformMapIndexFields = new HashSet<string>
        {
            nameof(platform_data.endpoint_owners),
        };

        private static readonly HashSet<string> polygonHeightFields = new HashSet<string>
        {
            nameof(polygon_data.floor_height),
            nameof(polygon_data.ceiling_height),
        };

        private static readonly HashSet<string> polygonSoundFields = new HashSet<string>
        {
            nameof(polygon_data.ambient_sound_image_index),
            nameof(polygon_data.random_sound_image_index),
        };

        // A changed type or place rebuilds the side's line (which surfaces it has depends on them), and the others are re-applied
        private static readonly HashSet<string> sideLineFields = new HashSet<string>
        {
            nameof(side_data.type),
            nameof(side_data.line_index),
            nameof(side_data.polygon_index),

            // Worked out from its line (map_constructors.cpp: recalculate_redundant_side_data)
            nameof(side_data.exclusion_zone),
        };

        private static readonly HashSet<string> refreshableSideFields = new HashSet<string>(sideLineFields)
        {
            nameof(side_data.flags),
            nameof(side_data.primary_texture),
            nameof(side_data.secondary_texture),
            nameof(side_data.transparent_texture),
            nameof(side_data.control_panel_type),
            nameof(side_data.control_panel_permutation),
            nameof(side_data.primary_transfer_mode),
            nameof(side_data.secondary_transfer_mode),
            nameof(side_data.transparent_transfer_mode),
            nameof(side_data.primary_lightsource_index),
            nameof(side_data.secondary_lightsource_index),
            nameof(side_data.transparent_lightsource_index),
            nameof(side_data.ambient_delta),
        };

        // A line's other fields (including which sides it has, and its length, which follows its points) only rebuild its
        // sides
        private static readonly HashSet<string> structuralLineFields = new HashSet<string>
        {
            nameof(line_data.endpoint_indexes),
            nameof(line_data.clockwise_polygon_owner),
            nameof(line_data.counterclockwise_polygon_owner),
        };

        // These load other textures
        private static readonly HashSet<string> collectionStaticFields = new HashSet<string>
        {
            nameof(static_data.environment_code),
            nameof(static_data.song_index),
        };

        public static void Refresh(IReadOnlyList<DataChange> changes)
        {
            var level = LevelEntity_Level.Instance;

            // The map's directory has the level's name and flags
            if (changes.Any(change => change.Data.Name == nameof(MapLevel.static_world) || change.Data.Name == LevelHistory.MapNameData))
            {
                MapsLoading.Instance.UpdateOpenLevelDirectory();
            }

            if (!level)
            {
                return;
            }

            if (NeedsRebuild(changes))
            {
                LevelEditing.RebuildLevelKeepingSelection();
                return;
            }

            var reselect = LevelEditing.RememberSelection();

            RefreshEntities(level, changes);

            PaletteManager.Instance.RefreshSwatches();

            var ui = ForgePlusUI.Instance;
            if (ui && ui.Errors != null)
            {
                ui.Errors.RequestRefresh();
            }

            // Selected again, so inspectors (whose rows can depend on the data) are made again, and a selected side
            // whose line was rebuilt is the new one
            if (SelectionManager.Instance.Selection.Count > 0)
            {
                SelectionManager.Instance.DeselectAll(inspectNothingSelected: false);
                reselect();
            }
            else
            {
                InspectorPanel.Instance.RefreshAllInspectors();
            }
        }

        private static bool NeedsRebuild(IReadOnlyList<DataChange> changes)
        {
            foreach (var change in changes)
            {
                var name = change.Data.Name;

                if (dataOnly.Contains(name))
                {
                    continue;
                }

                if (change is TrackedValue.ValueChange valueChange)
                {
                    if (name != nameof(MapLevel.static_world) ||
                        DataState.ChangedFields(valueChange.Before, valueChange.After).Overlaps(collectionStaticFields))
                    {
                        return true;
                    }

                    continue;
                }

                var listChange = (TrackedList.ListChange) change;

                switch (name)
                {
                    case nameof(MapLevel.AmbientSoundImageList):
                    case nameof(MapLevel.RandomSoundImageList):
                        continue;
                    case nameof(MapLevel.PolygonList):
                        if (listChange.CountChanged ||
                            AnyChangedFields(listChange, fields => !fields.IsSubsetOf(refreshablePolygonFields)) ||
                            AnyPlatformTypeChange(listChange))
                        {
                            return true;
                        }

                        continue;
                    case nameof(MapLevel.SideList):
                        // Sides added or removed (as heights reveal or hide faces) only rebuild their lines' sides
                        if (AnyChangedFields(listChange, fields => !fields.IsSubsetOf(refreshableSideFields), skipAddedOrRemoved: true))
                        {
                            return true;
                        }

                        continue;
                    case nameof(MapLevel.LineList):
                        if (listChange.CountChanged || AnyChangedFields(listChange, fields => fields.Overlaps(structuralLineFields)))
                        {
                            return true;
                        }

                        continue;
                    case nameof(MapLevel.EndpointList):
                        // Moved points are refreshed (the rest of a point's data is worked out from its polygons)
                        if (listChange.CountChanged)
                        {
                            return true;
                        }

                        continue;
                    case nameof(MapLevel.PlatformList):
                        // Map indexes worked out again (as moving points does) don't change the platform itself
                        if (listChange.CountChanged || AnyChangedFields(listChange, fields => !fields.IsSubsetOf(platformMapIndexFields)))
                        {
                            return true;
                        }

                        continue;
                    case nameof(MapLevel.SavedObjectList):
                    case nameof(MapLevel.MapAnnotationList):
                    case nameof(MapLevel.LightList):
                    case nameof(MapLevel.MediaList):
                    case nameof(MapLevel.map_terminal_text):
                        // Their entities are only made (one for each) by building the level
                        if (listChange.CountChanged)
                        {
                            return true;
                        }

                        continue;
                    default:
                        // Such as platforms, which change the sides around them
                        return true;
                }
            }

            return false;
        }

        private static void RefreshEntities(LevelEntity_Level level, IReadOnlyList<DataChange> changes)
        {
            var linesToRegenerate = new HashSet<short>();
            var polygonsWithChangedHeights = new HashSet<short>();
            var movedPoints = new HashSet<short>();
            var soundsChanged = false;

            SurfaceBatchingManager.Instance.DeferMerging(true);

            try
            {
                foreach (var change in changes)
                {
                    if (!(change is TrackedList.ListChange listChange))
                    {
                        continue;
                    }

                    for (var elementIndex = 0; elementIndex < listChange.Elements.Count; elementIndex++)
                    {
                        var index = (short) listChange.Elements[elementIndex].Index;
                        var changedFields = listChange.ChangedFields(elementIndex);

                        switch (change.Data.Name)
                        {
                            case nameof(MapLevel.PolygonList):
                                if (level.Polygons.TryGetValue(index, out var polygon))
                                {
                                    polygon.ApplyAllSurfaces();
                                }

                                // Its sides reach its floor and ceiling
                                if (changedFields.Overlaps(polygonHeightFields))
                                {
                                    polygonsWithChangedHeights.Add(index);

                                    var nativePolygon = level.Level.PolygonList[index];
                                    for (var i = 0; i < nativePolygon.vertex_count; i++)
                                    {
                                        linesToRegenerate.Add(nativePolygon.line_indexes[i]);
                                    }
                                }

                                soundsChanged |= changedFields.Overlaps(polygonSoundFields);
                                break;
                            case nameof(MapLevel.SideList):
                                var element = listChange.Elements[elementIndex];

                                if (element.IsAddedOrRemoved ||
                                    changedFields.Overlaps(sideLineFields) ||
                                    !level.Sides.TryGetValue(index, out var side) || !side)
                                {
                                    // Added, removed or moved between lines: on the line it was on, and the one it's on
                                    foreach (var state in new[] { element.Before, element.After })
                                    {
                                        if (state is side_data sideState)
                                        {
                                            linesToRegenerate.Add(sideState.line_index);
                                        }
                                    }
                                }
                                else
                                {
                                    side.ApplyAllSurfaces();
                                }

                                break;
                            case nameof(MapLevel.LineList):
                                linesToRegenerate.Add(index);
                                break;
                            case nameof(MapLevel.EndpointList):
                                if (changedFields.Contains(nameof(endpoint_data.vertex)))
                                {
                                    movedPoints.Add(index);
                                }

                                break;
                            case nameof(MapLevel.SavedObjectList):
                                if (level.MapObjects.TryGetValue(index, out var mapObject))
                                {
                                    mapObject.ApplyType();
                                    mapObject.ApplyPlacement();
                                }

                                break;
                            case nameof(MapLevel.MapAnnotationList):
                                if (level.Annotations.TryGetValue(index, out var annotation))
                                {
                                    annotation.RefreshLabel();
                                    annotation.RefreshPosition();
                                }

                                break;
                            case nameof(MapLevel.LightList):
                                if (level.Lights.TryGetValue(index, out var light))
                                {
                                    light.BeginRuntimeStyleBehavior();
                                }

                                break;
                            case nameof(MapLevel.MediaList):
                                if (level.Medias.TryGetValue(index, out var media))
                                {
                                    foreach (var mediaPolygon in level.Polygons.Values)
                                    {
                                        if (mediaPolygon.NativeObject.media_index == index)
                                        {
                                            mediaPolygon.ApplyMedia();
                                        }
                                    }

                                    media.ApplyMaterialProperties();
                                }

                                break;
                            case nameof(MapLevel.AmbientSoundImageList):
                            case nameof(MapLevel.RandomSoundImageList):
                                soundsChanged = true;
                                break;
                            case nameof(MapLevel.map_terminal_text):
                                var ui = ForgePlusUI.Instance;
                                if (ui && ui.Terminals != null)
                                {
                                    ui.Terminals.ReloadTerminal();
                                }

                                break;
                        }
                    }
                }

                if (movedPoints.Count > 0)
                {
                    PointEditing.ApplyShapes(level, movedPoints, linesToRegenerate);
                }

                foreach (var lineIndex in linesToRegenerate)
                {
                    if (level.Lines.TryGetValue(lineIndex, out var line))
                    {
                        line.RegenerateSurfaces();
                    }
                }

                // Objects and annotations are placed by their polygons' heights
                if (polygonsWithChangedHeights.Count > 0)
                {
                    foreach (var mapObject in level.MapObjects.Values)
                    {
                        if (polygonsWithChangedHeights.Contains(mapObject.NativeObject.polygon_index))
                        {
                            mapObject.ApplyPlacement();
                        }
                    }

                    foreach (var annotation in level.Annotations.Values)
                    {
                        if (polygonsWithChangedHeights.Contains(annotation.NativeObject.polygon_index))
                        {
                            annotation.RefreshPosition();
                        }
                    }
                }
            }
            finally
            {
                SurfaceBatchingManager.Instance.DeferMerging(false);
            }

            if (polygonsWithChangedHeights.Count > 0)
            {
                PolygonContainmentMap.MarkChanged();
                HeightsEditing.NotifyHeightsChanged();
            }

            if (soundsChanged)
            {
                SoundImageEditing.NotifyRestored();
            }
        }

        // Elements added or removed change the list's count, which is checked first (unless they're skipped here)
        private static bool AnyChangedFields(TrackedList.ListChange change, Func<HashSet<string>, bool> isMatch, bool skipAddedOrRemoved = false)
        {
            for (var elementIndex = 0; elementIndex < change.Elements.Count; elementIndex++)
            {
                var element = change.Elements[elementIndex];
                if (skipAddedOrRemoved && element.IsAddedOrRemoved)
                {
                    continue;
                }

                if (isMatch(change.ChangedFields(elementIndex)))
                {
                    return true;
                }
            }

            return false;
        }

        // Making a polygon a platform (or no longer one) adds (or removes) the platform's entities
        private static bool AnyPlatformTypeChange(TrackedList.ListChange change)
        {
            foreach (var element in change.Elements)
            {
                var before = (polygon_data) element.Before;
                var after = (polygon_data) element.After;

                if (before != null && after != null && before.type != after.type &&
                    (before.type == map._polygon_is_platform || after.type == map._polygon_is_platform))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
