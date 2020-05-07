using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public class UVPlanarDrag : PlanarDrag
    {
        private const float MaximumDistance = 16f;

        private readonly Vector2 startingUVs;

        public UVPlanarDrag(Vector2 startingUVs, Vector3 dragOriginPosition, Vector3 surfaceWorldNormal, Vector3 textureWorldUp)
            : base(dragOriginPosition, surfaceWorldNormal, textureWorldUp)
        {
            this.startingUVs = startingUVs;
        }

        public Vector2 UVDraggedPosition(Ray pointerRay)
        {
            var planarPosition = GetPlanarPosition(pointerRay);

            return UVDraggedPosition(planarPosition);
        }

        private Vector2 UVDraggedPosition(Vector3 currentPosition)
        {
            var uvDraggedPosition = UVDragVector(currentPosition);

            uvDraggedPosition += startingUVs;

            if (Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl))
            {
                uvDraggedPosition /= 32f;

                uvDraggedPosition.x = Mathf.Round(uvDraggedPosition.x) * 32;
                uvDraggedPosition.y = Mathf.Round(uvDraggedPosition.y) * 32;
            }

            uvDraggedPosition.x %= GeometryUtilities.WorldUnitIncrementsPerWorldUnit;
            uvDraggedPosition.y %= GeometryUtilities.WorldUnitIncrementsPerWorldUnit;

            return uvDraggedPosition;
        }

        private Vector2 UVDragVector(Vector3 currentPosition)
        {
            var uvDragVector = DragVector(currentPosition);
            uvDragVector.x = Mathf.Clamp(uvDragVector.x, -MaximumDistance, MaximumDistance);
            uvDragVector.y = Mathf.Clamp(uvDragVector.y, -MaximumDistance, MaximumDistance);

            var uvOffset = new Vector2(-uvDragVector.x * GeometryUtilities.WorldUnitIncrementsPerMeter,
                                       uvDragVector.y * GeometryUtilities.WorldUnitIncrementsPerMeter);

            return uvOffset;
        }
    }
}
