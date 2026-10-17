#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.PolygonContainment;
using RuntimeCore.Entities;
using System.Collections.Generic;

namespace ForgePlus.LevelManipulation
{
    // Points with no lines (and so no polygon has them as a corner). An edit that leaves a point that way removes it, but
    // a level can come with some (which old maps may have for their own reasons), so those are only warned about
    // (GeometryValidation), with a fix that removes them.
    public static class OrphanedPoints
    {
        public static bool IsOrphaned(MapLevel data, short pointIndex)
        {
            foreach (var line in data.LineList)
            {
                if (line.endpoint_indexes[0] == pointIndex || line.endpoint_indexes[1] == pointIndex)
                {
                    return false;
                }
            }

            foreach (var polygon in data.PolygonList)
            {
                if (System.Array.IndexOf(polygon.endpoint_indexes, pointIndex, 0, polygon.vertex_count) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        // Those of the points an edit may have left with no lines (such as those at the far ends of removed lines)
        public static void RemoveNewlyOrphaned(MapLevel data, IEnumerable<endpoint_data> candidates)
        {
            foreach (var point in candidates)
            {
                var pointIndex = (short) data.EndpointList.IndexOf(point);
                if (pointIndex >= 0 && IsOrphaned(data, pointIndex))
                {
                    LevelTopology.RemoveEndpoint(data, point);
                }
            }
        }

        // The Errors panel's fix for one the level came with, rebuilding the level (its structure has changed)
        public static void Remove(LevelEntity_Level level, short pointIndex)
        {
            var data = level.Level;
            if (pointIndex < 0 || pointIndex >= data.EndpointList.Count || !IsOrphaned(data, pointIndex))
            {
                return;
            }

            SelectionManager.Instance.DeselectAll();

            LevelTopology.RemoveEndpoint(data, data.EndpointList[pointIndex]);

            level.EndpointLines = data.BuildEndpointLines();
            PolygonContainmentMap.MarkChanged();
            LevelEditing.RebuildLevel();
        }
    }
}
#endif
