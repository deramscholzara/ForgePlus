using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public class PlanarDrag
    {
        protected readonly Matrix4x4 originLocalToWorldMatrix;
        protected readonly Plane intersectionPlane;

        private Vector3 lastValidPlanarPosition;

        public PlanarDrag(Vector3 dragOriginPosition, Vector3 planarNormal, Vector3 planarUp)
        {
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

            if (AxisLocks.Instance.XLocked)
            {
                dragVector.x = 0f;
            }
            else if (AxisLocks.Instance.YLocked)
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
