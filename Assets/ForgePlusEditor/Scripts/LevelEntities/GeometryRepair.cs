#if !NO_EDITING
using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.History;
using ForgePlus.PolygonContainment;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace ForgePlus.LevelManipulation
{
    // Collapses lines of no length into single points (LineCollapse), keeping what's in the polygons around them inside
    // them, then rebuilds the level (its structure has changed), selecting the kept point again if one was asked for
    public static class GeometryRepair
    {
        // Lines of no length at the points (as a move leaves them), each keeping its other point (the one that stayed
        // put). Returns whether any collapsed.
        public static bool CollapseAtPoints(LevelEntity_Level level, ICollection<short> pointIndexes, ObjectPlacement.Snapshot placements, bool selectKeptPoint)
        {
            var data = level.Level;
            var points = pointIndexes.Select(index => data.EndpointList[index]).ToList();
            var affectedPolygons = new HashSet<polygon_data>();
            endpoint_data keptPoint = null;

            foreach (var point in points)
            {
                // Until none is left at it (the point it collapses into may have more)
                for (var guard = 0; guard < 64; guard++)
                {
                    var pointIndex = (short) data.EndpointList.IndexOf(point);
                    if (pointIndex < 0)
                    {
                        break;
                    }

                    var lineIndex = FindCollapsibleLine(data, pointIndex);
                    if (lineIndex == cstypes.NONE)
                    {
                        break;
                    }

                    var line = data.LineList[lineIndex];
                    var otherIndex = line.endpoint_indexes[0] == pointIndex ? line.endpoint_indexes[1] : line.endpoint_indexes[0];

                    BeginCollapse(level, affectedPolygons, pointIndex, otherIndex, placements);
                    keptPoint = LineCollapse.Collapse(data, lineIndex, otherIndex, affectedPolygons) ?? keptPoint;
                }
            }

            if (affectedPolygons.Count == 0)
            {
                return false;
            }

            FinishCollapse(level, affectedPolygons, placements, selectKeptPoint ? keptPoint : null);
            return true;
        }

        // A line the Errors panel lists, keeping its first point
        public static void CollapseLine(LevelEntity_Level level, short lineIndex)
        {
            var data = level.Level;
            if (!LineCollapse.CanCollapse(data, lineIndex))
            {
                return;
            }

            var line = data.LineList[lineIndex];
            var affectedPolygons = new HashSet<polygon_data>();
            var placements = new ObjectPlacement.Snapshot();

            BeginCollapse(level, affectedPolygons, line.endpoint_indexes[1], line.endpoint_indexes[0], placements);
            LineCollapse.Collapse(data, lineIndex, line.endpoint_indexes[0], affectedPolygons);
            FinishCollapse(level, affectedPolygons, placements, keptPoint: null);
        }

        private static short FindCollapsibleLine(MapLevel data, short pointIndex)
        {
            for (short lineIndex = 0; lineIndex < data.LineList.Count; lineIndex++)
            {
                var line = data.LineList[lineIndex];
                if ((line.endpoint_indexes[0] == pointIndex || line.endpoint_indexes[1] == pointIndex) && LineCollapse.CanCollapse(data, lineIndex))
                {
                    return lineIndex;
                }
            }

            return cstypes.NONE;
        }

        // The selection refers to entities that are about to be rebuilt; what's in the polygons around both points is
        // recorded (where it wasn't already, as by a move)
        private static void BeginCollapse(LevelEntity_Level level, HashSet<polygon_data> affectedPolygons, short pointIndex, short otherIndex, ObjectPlacement.Snapshot placements)
        {
            SelectionManager.Instance.DeselectAll();

            var data = level.Level;
            var polygonIndexes = new HashSet<short>();
            for (short polygonIndex = 0; polygonIndex < data.PolygonList.Count; polygonIndex++)
            {
                var polygon = data.PolygonList[polygonIndex];
                for (var i = 0; i < polygon.vertex_count; i++)
                {
                    if (polygon.endpoint_indexes[i] == pointIndex || polygon.endpoint_indexes[i] == otherIndex)
                    {
                        polygonIndexes.Add(polygonIndex);
                        affectedPolygons.Add(polygon);
                        break;
                    }
                }
            }

            placements.Record(data, polygonIndexes);
        }

        private static void FinishCollapse(LevelEntity_Level level, HashSet<polygon_data> affectedPolygons, ObjectPlacement.Snapshot placements, endpoint_data keptPoint)
        {
            var data = level.Level;

            LineCollapse.RecalculateAround(data, affectedPolygons);
            ObjectPlacement.Apply(data, placements);

            // Until the level is rebuilt, which works these out for its entities
            level.EndpointLines = data.BuildEndpointLines();
            PolygonContainmentMap.MarkChanged();

            LevelEditing.RebuildLevel(() =>
            {
                var rebuiltLevel = LevelEntity_Level.Instance;
                if (keptPoint == null || !rebuiltLevel || rebuiltLevel.Points == null)
                {
                    return;
                }

                var keptIndex = (short) rebuiltLevel.Level.EndpointList.IndexOf(keptPoint);
                if (rebuiltLevel.Points.TryGetValue(keptIndex, out var point))
                {
                    SelectionManager.Instance.SelectObject(point, multiSelect: false);
                }
            });
        }
    }
}
#endif
