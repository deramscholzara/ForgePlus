using AlephOne;
using ForgePlus.LevelManipulation.Utilities;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace ForgePlus.PolygonContainment
{
    // Which of the open level's polygons its transform is in (NONE for none), looked for again only when it moves or the
    // level's polygons change. It updates in LateUpdate, so readers there need an execution order after ExecutionOrder.
    [DefaultExecutionOrder(ExecutionOrder)]
    public class PolygonContainmentTracker : MonoBehaviour
    {
        public const int ExecutionOrder = 1000;

        // Whether a polygon has it only from its floor to its ceiling (rather than at any height over or under it)
        [SerializeField]
        private bool withHeight = true;

        private PolygonContainmentMap evaluatedMap;
        private int evaluatedVersion;
        private int3 evaluatedPoint;
        private bool hasEvaluated;

        public event Action<PolygonContainmentTracker> OnPolygonChanged;

        public short PolygonIndex { get; private set; } = cstypes.NONE;

        // Where it was when last looked for (in world units)
        public world_point3d Location
        {
            get
            {
                return new world_point3d((short) evaluatedPoint.x, (short) evaluatedPoint.y, (short) evaluatedPoint.z);
            }
        }

        public bool WithHeight
        {
            get
            {
                return withHeight;
            }
            set
            {
                if (withHeight != value)
                {
                    withHeight = value;
                    hasEvaluated = false;
                }
            }
        }

        // A position in the level (meters) as a point in world units (Marathon's x and y, and its height as z)
        public static int3 ToWorldPoint(Vector3 position)
        {
            var scaled = position * GeometryUtilities.WorldUnitIncrementsPerMeter;

            return math.clamp(
                new int3(Mathf.RoundToInt(scaled.x), Mathf.RoundToInt(-scaled.z), Mathf.RoundToInt(scaled.y)),
                short.MinValue,
                short.MaxValue);
        }

        // Looks again now (rather than waiting for LateUpdate), such as for something that just moved it
        public void Evaluate()
        {
            var map = PolygonContainmentMap.Current;
            if (map == null)
            {
                evaluatedMap = null;
                hasEvaluated = false;
                SetPolygon(cstypes.NONE);
                return;
            }

            var point = ToWorldPoint(transform.position);
            var isSameMap = hasEvaluated && map == evaluatedMap && map.Version == evaluatedVersion;

            if (isSameMap && point.Equals(evaluatedPoint))
            {
                // Hasn't moved, so it's where it was
                return;
            }

            short polygonIndex;
            if (!isSameMap || PolygonIndex == cstypes.NONE)
            {
                polygonIndex = map.FindPolygon(point, withHeight);
            }
            else if (map.Contains(PolygonIndex, point, withHeight))
            {
                polygonIndex = PolygonIndex;
            }
            else
            {
                polygonIndex = map.FindAdjacentPolygon(PolygonIndex, point, withHeight);

                if (polygonIndex == cstypes.NONE)
                {
                    // Left its polygon, but not for one next to it
                    polygonIndex = map.FindPolygon(point, withHeight);
                }
            }

            evaluatedMap = map;
            evaluatedVersion = map.Version;
            evaluatedPoint = point;
            hasEvaluated = true;

            SetPolygon(polygonIndex);
        }

        private void OnEnable()
        {
            hasEvaluated = false;
        }

        private void OnDisable()
        {
            SetPolygon(cstypes.NONE);
        }

        private void LateUpdate()
        {
            Evaluate();
        }

        private void SetPolygon(short polygonIndex)
        {
            if (PolygonIndex == polygonIndex)
            {
                return;
            }

            PolygonIndex = polygonIndex;

            OnPolygonChanged?.Invoke(this);
        }
    }
}
