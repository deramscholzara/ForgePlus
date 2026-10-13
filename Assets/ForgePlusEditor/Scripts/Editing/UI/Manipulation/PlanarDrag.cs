using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public class PlanarDrag
    {
        protected readonly Matrix4x4 originLocalToWorldMatrix;
        protected readonly Plane intersectionPlane;

        private Vector3 lastValidPlanarPosition;

        // Whether the X lock holds the drag's Y still (and the Y lock its X), for a plane whose axes run the other way to
        // the level's (so locking an axis locks the same one it does when moving geometry)
        private readonly bool swapsAxisLocks;

        public PlanarDrag(Vector3 dragOriginPosition, Vector3 planarNormal, Vector3 planarUp, bool swapsAxisLocks = false)
        {
            this.swapsAxisLocks = swapsAxisLocks;

            originLocalToWorldMatrix = Matrix4x4.TRS(dragOriginPosition,
                                         Quaternion.LookRotation(-planarNormal, planarUp),
                                         Vector3.one);

            intersectionPlane = new Plane(planarNormal, dragOriginPosition);

            lastValidPlanarPosition = dragOriginPosition;
        }

        public Vector3 DragVector(Ray pointerRay)
        {
            var planarPosition = GetPlanarPosition(pointerRay);

            return DragVector(planarPosition);
        }

        protected Vector3 DragVector(Vector3 currentPosition)
        {
            var dragVector = originLocalToWorldMatrix.inverse.MultiplyPoint(currentPosition);

            // Force exact snap to plane - in case there are any precision issues.
            dragVector.z = 0f;

#if UNITY_EDITOR
            Debug.DrawLine(originLocalToWorldMatrix.MultiplyPoint(Vector3.zero), originLocalToWorldMatrix.MultiplyPoint(dragVector));
#endif

            var locksX = swapsAxisLocks ? AxisLocks.Instance.YLocked : AxisLocks.Instance.XLocked;
            var locksY = swapsAxisLocks ? AxisLocks.Instance.XLocked : AxisLocks.Instance.YLocked;

            if (locksX)
            {
                dragVector.x = 0f;
            }
            else if (locksY)
            {
                dragVector.y = 0f;
            }

            return dragVector;
        }

        protected Vector3 GetPlanarPosition(Ray pointerRay)
        {
            if (intersectionPlane.Raycast(pointerRay, out float distanceToPlane))
            {
                lastValidPlanarPosition = pointerRay.GetPoint(distanceToPlane);
            }

            return lastValidPlanarPosition;
        }
    }
}
