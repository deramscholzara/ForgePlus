#if !NO_EDITING
using AlephOne;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Keeps objects and annotations in their polygons as the geometry changes: one left outside its polygon goes into
    // whichever polygon it's in, or else back into its own at the nearest place inside its edge. An object's height (from
    // its polygon's floor, or ceiling for one hanging from it) is kept within its polygon's floor and ceiling.
    public static class ObjectPlacement
    {
        // How far in from the polygon's edge (in world units) it's put, at most, looking for its inside
        private const int MaximumInset = world.WORLD_ONE / 4;

        // Whether each object and annotation in the polygons is in its polygon, and its height within it, before an edit
        // (as each was first recorded, for an edit made over many steps)
        public sealed class Snapshot
        {
            public readonly Dictionary<map_object, (bool IsInside, bool IsWithinHeights)> Objects = new Dictionary<map_object, (bool, bool)>();
            public readonly Dictionary<map_annotation, bool> Annotations = new Dictionary<map_annotation, bool>();

            public void Record(MapLevel level, ICollection<short> polygonIndexes)
            {
                foreach (var mapObject in level.SavedObjectList)
                {
                    if (polygonIndexes.Contains(mapObject.polygon_index) && !Objects.ContainsKey(mapObject))
                    {
                        Objects[mapObject] = (IsInside(level, mapObject.polygon_index, Location(mapObject)), IsWithinHeights(level, mapObject, mapObject.polygon_index, mapObject.location.z));
                    }
                }

                foreach (var annotation in level.MapAnnotationList)
                {
                    if (polygonIndexes.Contains(annotation.polygon_index) && !Annotations.ContainsKey(annotation))
                    {
                        Annotations[annotation] = IsInside(level, annotation.polygon_index, annotation.location);
                    }
                }
            }
        }

        public static Snapshot Capture(MapLevel level, ICollection<short> polygonIndexes)
        {
            var snapshot = new Snapshot();
            snapshot.Record(level, polygonIndexes);

            return snapshot;
        }
        // After the edit, those the edit left outside their polygons (or heights) are put back in. Returns the changed
        // objects' and annotations' indexes.
        public static (List<short> Objects, List<short> Annotations) Apply(MapLevel level, Snapshot snapshot)
        {
            var changedObjects = new List<short>();
            var changedAnnotations = new List<short>();

            foreach (var entry in snapshot.Objects)
            {
                var mapObject = entry.Key;
                var index = (short) level.SavedObjectList.IndexOf(mapObject);
                if (index < 0)
                {
                    continue;
                }

                var changed = false;

                if (entry.Value.IsInside && !IsInside(level, mapObject.polygon_index, Location(mapObject)))
                {
                    var location = Location(mapObject);
                    var polygonIndex = mapObject.polygon_index;
                    PutInPolygon(level, ref location, ref polygonIndex);

                    mapObject.location.x = location.x;
                    mapObject.location.y = location.y;
                    mapObject.polygon_index = polygonIndex;
                    changed = true;
                }

                if (entry.Value.IsWithinHeights && !IsWithinHeights(level, mapObject, mapObject.polygon_index, mapObject.location.z))
                {
                    mapObject.location.z = ClampedZ(level, mapObject, mapObject.polygon_index, mapObject.location.z);
                    changed = true;
                }

                if (changed)
                {
                    changedObjects.Add(index);
                }
            }

            foreach (var entry in snapshot.Annotations)
            {
                var annotation = entry.Key;
                var index = (short) level.MapAnnotationList.IndexOf(annotation);
                if (index < 0 || !entry.Value || IsInside(level, annotation.polygon_index, annotation.location))
                {
                    continue;
                }

                var location = annotation.location;
                var polygonIndex = annotation.polygon_index;
                PutInPolygon(level, ref location, ref polygonIndex);

                annotation.location = location;
                annotation.polygon_index = polygonIndex;
                changedAnnotations.Add(index);
            }

            return (changedObjects, changedAnnotations);
        }

        // Into whichever polygon it's in, or else into its own, at the nearest place inside its edge
        public static void PutInPolygon(MapLevel level, ref world_point2d location, ref short polygonIndex)
        {
            if (IsInside(level, polygonIndex, location))
            {
                return;
            }

            var containingIndex = map.world_point_to_polygon_index(level, location);
            if (containingIndex != cstypes.NONE)
            {
                polygonIndex = containingIndex;
                return;
            }

            if (polygonIndex >= 0 && polygonIndex < level.PolygonList.Count)
            {
                location = NearestInside(level, polygonIndex, location);
            }
        }

        // Its height (from its surface) kept between its polygon's floor and ceiling
        public static short ClampedZ(MapLevel level, map_object mapObject, short polygonIndex, short z)
        {
            var polygon = level.PolygonList[polygonIndex];
            var surface = SurfaceHeight(polygon, mapObject);
            var elevation = Mathf.Clamp(surface + z, polygon.floor_height, polygon.ceiling_height);

            return (short) Mathf.Clamp(elevation - surface, short.MinValue, short.MaxValue);
        }

        public static bool IsWithinHeights(MapLevel level, map_object mapObject, short polygonIndex, short z)
        {
            if (polygonIndex < 0 || polygonIndex >= level.PolygonList.Count)
            {
                return false;
            }

            var polygon = level.PolygonList[polygonIndex];
            var elevation = SurfaceHeight(polygon, mapObject) + z;

            return elevation >= polygon.floor_height && elevation <= polygon.ceiling_height;
        }

        public static bool IsInside(MapLevel level, short polygonIndex, world_point2d location)
        {
            return polygonIndex >= 0 && polygonIndex < level.PolygonList.Count && map.point_in_polygon(level, polygonIndex, location);
        }

        // Everything in a polygon that's being removed goes into the polygon it's in, or else into the given neighbor
        public static void MoveOutOfPolygon(MapLevel level, short polygonIndex, short neighborIndex)
        {
            foreach (var mapObject in level.SavedObjectList)
            {
                if (mapObject.polygon_index == polygonIndex)
                {
                    var location = Location(mapObject);
                    var newIndex = Containing(level, location, polygonIndex, neighborIndex);
                    if (newIndex != cstypes.NONE)
                    {
                        location = IsInside(level, newIndex, location) ? location : NearestInside(level, newIndex, location);
                        mapObject.location.x = location.x;
                        mapObject.location.y = location.y;
                        mapObject.polygon_index = newIndex;
                        mapObject.location.z = ClampedZ(level, mapObject, newIndex, mapObject.location.z);
                    }
                }
            }

            foreach (var annotation in level.MapAnnotationList)
            {
                if (annotation.polygon_index == polygonIndex)
                {
                    var newIndex = Containing(level, annotation.location, polygonIndex, neighborIndex);
                    if (newIndex != cstypes.NONE)
                    {
                        annotation.location = IsInside(level, newIndex, annotation.location) ? annotation.location : NearestInside(level, newIndex, annotation.location);
                        annotation.polygon_index = newIndex;
                    }
                }
            }
        }

        private static short Containing(MapLevel level, world_point2d location, short excludedIndex, short fallbackIndex)
        {
            for (short i = 0; i < level.PolygonList.Count; i++)
            {
                if (i != excludedIndex && map.point_in_polygon(level, i, location))
                {
                    return i;
                }
            }

            return fallbackIndex;
        }

        // The nearest place on the polygon's edge, moved in toward its center until it's inside
        private static world_point2d NearestInside(MapLevel level, short polygonIndex, world_point2d location)
        {
            var polygon = level.PolygonList[polygonIndex];
            var point = new Vector2(location.x, location.y);
            var nearest = point;
            var nearestDistance = float.MaxValue;

            for (var i = 0; i < polygon.vertex_count; i++)
            {
                var a = Vertex(level, polygon.endpoint_indexes[i]);
                var b = Vertex(level, polygon.endpoint_indexes[(i + 1) % polygon.vertex_count]);
                var onEdge = NearestOnSegment(point, a, b);
                var distance = (onEdge - point).sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = onEdge;
                }
            }

            map.find_center_of_polygon(level, polygonIndex, out var centerPoint);
            var center = new Vector2(centerPoint.x, centerPoint.y);
            var inward = (center - nearest).normalized;

            for (var inset = 1; inset <= MaximumInset; inset++)
            {
                var candidate = nearest + inward * inset;
                var candidateLocation = new world_point2d((short) Mathf.RoundToInt(candidate.x), (short) Mathf.RoundToInt(candidate.y));

                if (map.point_in_polygon(level, polygonIndex, candidateLocation))
                {
                    return candidateLocation;
                }
            }

            return centerPoint;
        }

        private static Vector2 NearestOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var segment = b - a;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0f)
            {
                return a;
            }

            return a + segment * Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
        }

        private static Vector2 Vertex(MapLevel level, short endpointIndex)
        {
            var vertex = level.EndpointList[endpointIndex].vertex;

            return new Vector2(vertex.x, vertex.y);
        }

        private static world_point2d Location(map_object mapObject)
        {
            return new world_point2d(mapObject.location.x, mapObject.location.y);
        }

        // Its height is measured from this (map.cpp: new_map_object)
        private static int SurfaceHeight(polygon_data polygon, map_object mapObject)
        {
            return csmacros.TEST_FLAG(mapObject.flags, map._map_object_hanging_from_ceiling) ? polygon.ceiling_height : polygon.floor_height;
        }
    }
}
#endif
