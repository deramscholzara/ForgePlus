using AlephOne;
using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Follows the pointer across the level plane (horizontal) the drag started on, as a level location: its X along the
    // world's X, and its Y along the world's Z (reversed). Grid snapping (which alt/option inverts) snaps each axis to the
    // world's sixteenth-of-a-world-unit marks, and a locked axis stays where it started.
    public class LevelPlaneDrag
    {
        private readonly PlanarDrag planarDrag;
        private readonly world_point2d startingLocation;

        public LevelPlaneDrag(Vector3 pressWorldPosition, world_point2d startingLocation)
        {
            planarDrag = new PlanarDrag(pressWorldPosition, Vector3.up, Vector3.forward);
            this.startingLocation = startingLocation;
        }

        public world_point2d DraggedLocation(Ray pointerRay)
        {
            var dragVector = planarDrag.DragVector(pointerRay);

            var snaps = AxisLocks.Instance.SnapToGrid;
            var x = DraggedCoordinate(startingLocation.x, dragVector.x, snaps && !AxisLocks.Instance.XLocked);
            var y = DraggedCoordinate(startingLocation.y, -dragVector.y, snaps && !AxisLocks.Instance.YLocked);

            return new world_point2d(x, y);
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
    }
}
