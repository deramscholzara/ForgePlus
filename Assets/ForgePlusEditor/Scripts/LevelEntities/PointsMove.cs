#if !NO_EDITING
using AlephOne;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Entities.MapObjects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Dragging points together across the level: what's selected (points, lines' ends, sides' lines' ends and polygons'
    // corners, all moving by the same amount) along with what was grabbed. They move through PointEditing, which stretches
    // the polygons around them that share them, and holds them back from making polygons invalid unless Allow Invalid
    // Move is held. What's in a moved polygon (its objects and annotations) moves along with it. Recorded as one action
    // when it ends.
    //
    // Grid snapping snaps a grabbed point to the grid (and the rest move as far as it does); otherwise, it snaps how far
    // they move, so points off the grid stay as far off it.
    public sealed class PointsMove
    {
        private readonly LevelEntity_Level level;
        private readonly LevelPlaneDrag drag;
        private readonly short[] pointIndexes;
        private readonly short referenceIndex;
        private readonly world_point2d referenceStart;
        private readonly bool snapsMovement;

        // What's in the polygons, and where each began
        private readonly List<(LevelEntity_MapObject MapObject, world_point3d Start)> objects = new List<(LevelEntity_MapObject, world_point3d)>();
        private readonly List<(LevelEntity_Annotation Annotation, world_point2d Start)> annotations = new List<(LevelEntity_Annotation, world_point2d)>();

        // What was grabbed (one of the selection's, or what's grabbed alone), at the place the drag began
        public static PointsMove ForSelection(LevelEntity_Level level, ISelectable grabbed, Vector3 pressWorldPosition)
        {
            var selection = SelectionManager.Instance.Selection;
            var moved = selection.Contains(grabbed) ? selection.ToList() : new List<ISelectable> { grabbed };

            var points = new List<short>();
            var polygons = new HashSet<short>();
            var data = level.Level;

            // The grabbed thing's own points first, the first of which the rest follow
            foreach (var selectable in new[] { grabbed }.Concat(moved.Where(selectable => selectable != grabbed)))
            {
                switch (selectable)
                {
                    case LevelEntity_Point point:
                        points.Add(point.NativeIndex);
                        break;
                    case LevelEntity_Line line:
                        points.AddRange(line.NativeObject.endpoint_indexes);
                        break;
                    case LevelEntity_Side side when side.NativeIndex >= 0 && side.ParentLineIndex >= 0:
                        points.AddRange(data.LineList[side.ParentLineIndex].endpoint_indexes);
                        break;
                    case LevelEntity_Polygon polygon:
                        var polygonData = polygon.NativeObject;
                        points.AddRange(polygonData.endpoint_indexes.Take(polygonData.vertex_count));
                        polygons.Add(polygon.NativeIndex);
                        break;
                }
            }

            return new PointsMove(level, points, pressWorldPosition, polygons, snapsMovement: !(grabbed is LevelEntity_Point));
        }

        private PointsMove(LevelEntity_Level level, IEnumerable<short> points, Vector3 pressWorldPosition, ICollection<short> carriedPolygonIndexes, bool snapsMovement)
        {
            this.level = level;
            this.snapsMovement = snapsMovement;

            var data = level.Level;
            pointIndexes = points.Distinct().ToArray();
            referenceIndex = pointIndexes[0];
            referenceStart = data.EndpointList[referenceIndex].vertex;

            // Dragged as a movement (from nothing), so it's the movement that snaps, or from the grabbed point
            drag = new LevelPlaneDrag(pressWorldPosition, snapsMovement ? new world_point2d(0, 0) : referenceStart);

            if (carriedPolygonIndexes.Count > 0)
            {
                foreach (var mapObject in level.MapObjects.Values)
                {
                    if (mapObject && carriedPolygonIndexes.Contains(mapObject.NativeObject.polygon_index))
                    {
                        var location = mapObject.NativeObject.location;
                        objects.Add((mapObject, new world_point3d(location.x, location.y, location.z)));
                    }
                }

                foreach (var annotation in level.Annotations.Values)
                {
                    if (annotation && carriedPolygonIndexes.Contains(annotation.NativeObject.polygon_index))
                    {
                        annotations.Add((annotation, annotation.NativeObject.location));
                    }
                }
            }

            PointEditing.BeginDrag();
            PointEditing.DragHeight = pressWorldPosition.y;
        }

        public void Drag(Ray pointerRay)
        {
            var dragged = drag.DraggedLocation(pointerRay);
            var target = snapsMovement ?
                         new world_point2d(Moved(referenceStart.x, dragged.x), Moved(referenceStart.y, dragged.y)) :
                         dragged;

            var movement = PointEditing.DragPoints(level, pointIndexes, referenceIndex, target, snapsMovement, canBeAllowedInvalid: true);

            foreach (var (mapObject, start) in objects)
            {
                var location = mapObject.NativeObject.location;
                location.x = Moved(start.x, movement.x);
                location.y = Moved(start.y, movement.y);
                mapObject.NativeObject.location = location;
                mapObject.ApplyPlacement();
            }

            foreach (var (annotation, start) in annotations)
            {
                annotation.SetLocation(new world_point2d(Moved(start.x, movement.x), Moved(start.y, movement.y)));
            }
        }

        public void End()
        {
            PointEditing.EndDrag();
        }

        private static short Moved(short start, int movement)
        {
            return (short) Mathf.Clamp(start + movement, short.MinValue, short.MaxValue);
        }
    }
}
#endif
