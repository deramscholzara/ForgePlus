using AlephOne;
using ForgePlus.ApplicationGeneral;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Entities.MapObjects;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Moves an object with the pointer across the level plane the drag started on, into whichever polygon it's over (it
    // stays where it was while the pointer is over no polygon). Grid snapping snaps it to the world's sixteenth-of-a-world-
    // unit marks along X and Y. In another polygon, its height keeps its distance from that polygon's floor (or ceiling,
    // for one hanging from it) while grid snapping is on or ctrl is held, and otherwise keeps its height in the world,
    // within the polygon's floor and ceiling.
    public class ObjectDrag
    {
        private readonly LevelEntity_MapObject mapObject;
        private readonly PlanarDrag planarDrag;

        private readonly world_point2d startingLocation;
        private readonly short startingPolygonIndex;
        private readonly short startingZ;
        private readonly int startingElevation;

        public ObjectDrag(LevelEntity_MapObject mapObject, Vector3 pressWorldPosition)
        {
            this.mapObject = mapObject;

            // Its X follows the world's X (the level's X), and its Y the world's Z (the level's Y, reversed)
            planarDrag = new PlanarDrag(pressWorldPosition, Vector3.up, Vector3.forward);

            var data = mapObject.NativeObject;
            startingLocation = new world_point2d(data.location.x, data.location.y);
            startingPolygonIndex = data.polygon_index;
            startingZ = data.location.z;
            startingElevation = SurfaceHeight(mapObject.Level, startingPolygonIndex) + startingZ;
        }

        // The grid snapping setting itself (which alt/option only inverts for snapping), or ctrl
        private static bool KeepsHeightFromSurface
        {
            get
            {
                return AxisLocks.Instance.SnapToGridEnabled || ForgePlusInput.Editing.KeepRelativeHeight.IsPressed();
            }
        }

        public void Drag(Ray pointerRay)
        {
            var dragVector = planarDrag.DragVector(pointerRay);

            var snaps = AxisLocks.Instance.SnapToGrid;
            var x = DraggedCoordinate(startingLocation.x, dragVector.x, snaps && !AxisLocks.Instance.XLocked);
            var y = DraggedCoordinate(startingLocation.y, -dragVector.y, snaps && !AxisLocks.Instance.YLocked);
            var location = new world_point2d(x, y);

            var level = mapObject.Level;
            var polygonIndex = map.world_point_to_polygon_index(level, location);
            if (polygonIndex == cstypes.NONE)
            {
                return;
            }

            mapObject.MoveTo(location, polygonIndex, DraggedZ(level, polygonIndex));
        }

        // A locked axis isn't snapped, so it stays where it was
        private static short DraggedCoordinate(short start, float meters, bool snaps)
        {
            float coordinate = start + Mathf.RoundToInt(meters * GeometryUtilities.WorldUnitIncrementsPerMeter);

            if (snaps)
            {
                coordinate = Mathf.Round(coordinate / HeightsEditing.SnapIncrement) * HeightsEditing.SnapIncrement;
            }

            return (short) Mathf.Clamp(coordinate, short.MinValue, short.MaxValue);
        }

        // Its height in its own polygon is left as it is
        private short DraggedZ(MapLevel level, short polygonIndex)
        {
            if (polygonIndex == startingPolygonIndex || KeepsHeightFromSurface)
            {
                return startingZ;
            }

            var polygon = level.PolygonList[polygonIndex];
            var elevation = Mathf.Clamp(startingElevation, polygon.floor_height, polygon.ceiling_height);

            return (short) Mathf.Clamp(elevation - SurfaceHeight(level, polygonIndex), short.MinValue, short.MaxValue);
        }

        // Its height is measured from this (map.cpp: new_map_object)
        private int SurfaceHeight(MapLevel level, short polygonIndex)
        {
            var polygon = level.PolygonList[polygonIndex];

            return csmacros.TEST_FLAG(mapObject.NativeObject.flags, map._map_object_hanging_from_ceiling) ? polygon.ceiling_height : polygon.floor_height;
        }
    }
}
