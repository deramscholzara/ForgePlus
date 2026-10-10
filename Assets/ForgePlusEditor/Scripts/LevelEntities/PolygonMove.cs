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
    // Dragging a polygon whole across the level: its corners move together (PointEditing, which stretches the polygons
    // around it that share them, and holds them back from making polygons invalid unless Allow Invalid Move is held), and
    // what's in it (its objects and annotations) moves along with it. Grid snapping snaps how far it moves, so a polygon
    // off the grid stays as far off it. Recorded as one action when it ends.
    public sealed class PolygonMove
    {
        private readonly LevelEntity_Level level;
        private readonly LevelPlaneDrag drag;
        private readonly short[] pointIndexes;
        private readonly short referenceIndex;
        private readonly world_point2d referenceStart;

        // What's in it, and where each began
        private readonly List<(LevelEntity_MapObject MapObject, world_point3d Start)> objects = new List<(LevelEntity_MapObject, world_point3d)>();
        private readonly List<(LevelEntity_Annotation Annotation, world_point2d Start)> annotations = new List<(LevelEntity_Annotation, world_point2d)>();

        public PolygonMove(LevelEntity_Polygon polygon, Vector3 pressWorldPosition)
        {
            level = polygon.ParentLevel;

            var data = level.Level;
            var polygonData = polygon.NativeObject;
            pointIndexes = polygonData.endpoint_indexes.Take(polygonData.vertex_count).Distinct().ToArray();
            referenceIndex = pointIndexes[0];
            referenceStart = data.EndpointList[referenceIndex].vertex;

            // Dragged as a movement (from nothing), so it's the movement that snaps
            drag = new LevelPlaneDrag(pressWorldPosition, new world_point2d(0, 0));

            foreach (var mapObject in level.MapObjects.Values)
            {
                if (mapObject && mapObject.NativeObject.polygon_index == polygon.NativeIndex)
                {
                    var location = mapObject.NativeObject.location;
                    objects.Add((mapObject, new world_point3d(location.x, location.y, location.z)));
                }
            }

            foreach (var annotation in level.Annotations.Values)
            {
                if (annotation && annotation.NativeObject.polygon_index == polygon.NativeIndex)
                {
                    annotations.Add((annotation, annotation.NativeObject.location));
                }
            }

            PointEditing.BeginDrag();
            PointEditing.DragHeight = pressWorldPosition.y;
        }

        public void Drag(Ray pointerRay)
        {
            var dragged = drag.DraggedLocation(pointerRay);
            var target = new world_point2d((short) Mathf.Clamp(referenceStart.x + dragged.x, short.MinValue, short.MaxValue),
                                           (short) Mathf.Clamp(referenceStart.y + dragged.y, short.MinValue, short.MaxValue));

            var movement = PointEditing.DragPoints(level, pointIndexes, referenceIndex, target, snapsMovement: true, canBeAllowedInvalid: true);

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
