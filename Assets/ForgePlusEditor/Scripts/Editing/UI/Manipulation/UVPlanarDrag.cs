using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public class UVPlanarDrag : PlanarDrag
    {
        private const float MaximumDistance = 16f;

        private readonly Vector2 startingUVs;

        // A floor's or ceiling's texture runs across the level the other way to the level's own axes, so its axis locks
        // are swapped to lock the same way they do when moving geometry (a side's aren't)
        public UVPlanarDrag(Vector2 startingUVs, Vector3 dragOriginPosition, Vector3 surfaceWorldNormal, Vector3 textureWorldUp, bool swapsAxisLocks = false)
            : base(dragOriginPosition, surfaceWorldNormal, textureWorldUp, swapsAxisLocks)
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

            if (AxisLocks.Instance.SnapToGrid)
            {
                uvDraggedPosition /= GeometryUtilities.UnitsPerTextureOffsetSnap;

                uvDraggedPosition.x = Mathf.Round(uvDraggedPosition.x) * GeometryUtilities.UnitsPerTextureOffsetSnap;
                uvDraggedPosition.y = Mathf.Round(uvDraggedPosition.y) * GeometryUtilities.UnitsPerTextureOffsetSnap;
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
