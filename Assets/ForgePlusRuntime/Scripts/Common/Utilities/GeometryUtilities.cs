using RuntimeCore.Materials;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Weland;
using Weland.Extensions;

namespace ForgePlus.LevelManipulation.Utilities
{
    public enum TransferModes
    {
        Normal = 0,
        Pulsate = 4,
        Wobble = 5,
        WobbleFast = 6,
        Landscape = 9,
        HorizontalSlide = 15,
        HorizontalSlideFast = 16,
        VerticalSlide = 17,
        VerticalSlideFast = 18,
        Wander = 19,
        WanderFast = 20,
    }

    public static class GeometryUtilities
    {
        /// <summary>
        /// Used for converting between world-unit (WU) "increments" and meters.
        /// One WU is 1024 "increments", and we want a WU to convert to 2 meters, so we use 512 as the conversion ratio.
        /// </summary>
        public const float WorldUnitIncrementsPerMeter = 512f;
        public const float WorldUnitIncrementsPerWorldUnit = 1024f;
        public const float MeterToWorldUnit = WorldUnitIncrementsPerMeter / WorldUnitIncrementsPerWorldUnit;

        public const float UnitsPerTextureOffetNudge = WorldUnitIncrementsPerWorldUnit / 128f;

        private static readonly Material SelectionIndicatorMaterial = new Material(Shader.Find("ForgePlus/GeometrySelectionIndicator"));

        public static Vector3 GetMeshVertex(Level level, int endpointIndex, short height = 0)
        {
            var endpoint = level.Endpoints[endpointIndex];

            // Convert from Marathon right-handed to Unity left-handed
            // by flipping Y-axis and assigning it to Z
            return new Vector3(endpoint.X, height, -endpoint.Y) / WorldUnitIncrementsPerMeter;
        }

        public static Platform GetPlatformForPolygon(Level level, Polygon polygon)
        {
            return polygon.Type == PolygonType.Platform ? level.Platforms[polygon.Permutation] : null;
        }

        public static GameObject CreateSurfaceSelectionIndicator(string name, Transform parent, Vector3 vertexWorldPosition, Vector3 nextVertexWorldPosition, Vector3 previousVertexWorldPosition)
        {
            var thickness = 0.04f;
            var length = 0.2f;
            var clockwiseDirection = (nextVertexWorldPosition - vertexWorldPosition).normalized;
            var counterclockwiseDirection = (previousVertexWorldPosition - vertexWorldPosition).normalized;
            var scale = Mathf.Min(1f, Vector3.Distance(vertexWorldPosition, nextVertexWorldPosition) / (length * 2f), Vector3.Distance(vertexWorldPosition, previousVertexWorldPosition) / (length * 2f));

            var indicator = new GameObject($"Selection Indicators - {name}");
            indicator.transform.position = vertexWorldPosition;
            indicator.transform.SetParent(parent, worldPositionStays: true);
            indicator.layer = SelectionManager.SelectionIndicatorLayer;

            indicator.AddComponent<MeshFilter>().sharedMesh = CreateSurfaceSelectionIndicatorCornerMesh(clockwiseDirection, counterclockwiseDirection, length, thickness, scale);
            indicator.AddComponent<MeshRenderer>().sharedMaterial = SelectionIndicatorMaterial;

            return indicator;
        }

        private static Mesh CreateSurfaceSelectionIndicatorCornerMesh(Vector3 clockwiseDirection, Vector3 counterclockwiseDirection, float length, float thickness, float scale)
        {
            var mesh = new Mesh();

            var facingVector = Vector3.Cross(clockwiseDirection, counterclockwiseDirection);
            var clockwiseThicknessDirection = Vector3.Cross(facingVector, clockwiseDirection).normalized;
            var counterclockwiseThicknessDirection = Vector3.Cross(counterclockwiseDirection, facingVector).normalized;
            var insetCornerPosition = thickness / Mathf.Abs(Mathf.Sin(Vector3.Angle(clockwiseDirection, counterclockwiseDirection) * 0.5f * Mathf.Deg2Rad)) * (clockwiseThicknessDirection + counterclockwiseThicknessDirection).normalized;

            mesh.vertices = new Vector3[]
            {
                Vector3.zero,
                (clockwiseDirection * length) * scale,
                (clockwiseDirection * length + clockwiseThicknessDirection * thickness) * scale,
                insetCornerPosition * scale,
                (counterclockwiseThicknessDirection * thickness + counterclockwiseDirection * length) * scale,
                (counterclockwiseDirection * length) * scale
            };

            mesh.triangles = new int[]
            {
                0, 1, 2,
                2, 3, 0,
                3, 4, 0,
                4, 5, 0
            };

            mesh.colors = new Color[]
            {
                new Color(1f, 1f, 1f, 0.75f),
                new Color(1f, 1f, 1f, 0.75f),
                new Color(1f, 1f, 1f, 0f),
                new Color(1f, 1f, 1f, 0f),
                new Color(1f, 1f, 1f, 0f),
                new Color(1f, 1f, 1f, 0.75f),
            };

            return mesh;
        }
    }
}
