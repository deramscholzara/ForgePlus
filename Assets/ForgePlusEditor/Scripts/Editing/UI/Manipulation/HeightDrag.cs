using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    // Follows the pointer up and down a vertical plane facing the camera, for raising or lowering a floor or ceiling. Grid
    // snapping snaps to the world's sixteenth-of-a-world-unit marks (wherever the drag started).
    public class HeightDrag
    {
        private readonly Plane plane;
        private readonly float pressElevation;
        private readonly int startingHeight;

        private float lastElevation;

        public HeightDrag(Vector3 pressWorldPosition, short startingHeight, Camera camera)
        {
            // Facing the camera, or (looking straight down or up) facing the way its top points
            var normal = camera.transform.forward;
            normal.y = 0f;

            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = camera.transform.up;
                normal.y = 0f;
            }

            plane = new Plane(normal.normalized, pressWorldPosition);

            pressElevation = pressWorldPosition.y;
            lastElevation = pressElevation;
            this.startingHeight = startingHeight;
        }

        // Where the pointer last met the plane (not yet clamped to the face's other height)
        public int DraggedHeight(Ray pointerRay)
        {
            if (plane.Raycast(pointerRay, out var distance))
            {
                lastElevation = pointerRay.GetPoint(distance).y;
            }

            var height = startingHeight + (lastElevation - pressElevation) * GeometryUtilities.WorldUnitIncrementsPerMeter;
            height = Mathf.Clamp(height, short.MinValue, short.MaxValue);

            if (AxisLocks.Instance.SnapToGrid)
            {
                return Mathf.RoundToInt(height / HeightsEditing.SnapIncrement) * HeightsEditing.SnapIncrement;
            }

            return Mathf.RoundToInt(height);
        }
    }
}
