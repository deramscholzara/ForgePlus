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
    // Dragging points together across the level, such as a polygon's corners or a line's ends: they move by the same amount
    // (PointEditing, which stretches the polygons around them that share them, and holds them back from making polygons
    // invalid unless Allow Invalid Move is held), and what's in a dragged polygon (its objects and annotations) moves along
    // with it. Grid snapping snaps how far they move, so points off the grid stay as far off it. Recorded as one action
    // when it ends.
    public sealed class PointsMove
    {
        private readonly LevelEntity_Level level;
        private readonly LevelPlaneDrag drag;
        private readonly short[] pointIndexes;
        private readonly short referenceIndex;
        private readonly world_point2d referenceStart;

        // What's in the polygon, and where each began
        private readonly List<(LevelEntity_MapObject MapObject, world_point3d Start)> objects = new List<(LevelEntity_MapObject, world_point3d)>();
        private readonly List<(LevelEntity_Annotation Annotation, world_point2d Start)> annotations = new List<(LevelEntity_Annotation, world_point2d)>();

        // Its corners, with what's in it
        public static PointsMove ForPolygon(LevelEntity_Polygon polygon, Vector3 pressWorldPosition)
        {
            var polygonData = polygon.NativeObject;
            return new PointsMove(polygon.ParentLevel, polygonData.endpoint_indexes.Take(polygonData.vertex_count), pressWorldPosition, polygon.NativeIndex);
        }

        // Its ends
        public static PointsMove ForLine(LevelEntity_Level level, LevelEntity_Line line, Vector3 pressWorldPosition)
        {
            return new PointsMove(level, line.NativeObject.endpoint_indexes, pressWorldPosition, cstypes.NONE);
        }

        private PointsMove(LevelEntity_Level level, IEnumerable<short> points, Vector3 pressWorldPosition, short carriedPolygonIndex)
        {
            this.level = level;

            var data = level.Level;
            pointIndexes = points.Distinct().ToArray();
            referenceIndex = pointIndexes[0];
            referenceStart = data.EndpointList[referenceIndex].vertex;

            // Dragged as a movement (from nothing), so it's the movement that snaps
            drag = new LevelPlaneDrag(pressWorldPosition, new world_point2d(0, 0));

            if (carriedPolygonIndex != cstypes.NONE)
            {
                foreach (var mapObject in level.MapObjects.Values)
                {
                    if (mapObject && mapObject.NativeObject.polygon_index == carriedPolygonIndex)
                    {
                        var location = mapObject.NativeObject.location;
                        objects.Add((mapObject, new world_point3d(location.x, location.y, location.z)));
                    }
                }

                foreach (var annotation in level.Annotations.Values)
                {
                    if (annotation && annotation.NativeObject.polygon_index == carriedPolygonIndex)
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
