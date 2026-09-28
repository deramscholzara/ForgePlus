using AlephOne;
using UnityEngine;

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
        // Used for converting between world-unit (WU) "increments" and meters.
        // One WU is 1024 "increments", and we want a WU to convert to 2 meters, so we use 512 as the conversion ratio.
        public const float WorldUnitIncrementsPerMeter = 512f;
        public const float WorldUnitIncrementsPerWorldUnit = world.WORLD_ONE;
        public const float MeterToWorldUnit = WorldUnitIncrementsPerMeter / WorldUnitIncrementsPerWorldUnit;

        public const float UnitsPerTextureOffetNudge = WorldUnitIncrementsPerWorldUnit / 128f;

        private static readonly Material SelectionIndicatorMaterial = new Material(Shader.Find("ForgePlus/GeometrySelectionIndicator"));

        public static Vector3 GetMeshVertex(MapLevel level, int endpointIndex, short height = 0)
        {
            var endpoint = level.EndpointList[endpointIndex].vertex;

            // Convert from Marathon right-handed to Unity left-handed
            // by flipping Y-axis and assigning it to Z
            return new Vector3(endpoint.x, height, -endpoint.y) / WorldUnitIncrementsPerMeter;
        }

        public static GameObject CreateSurfaceSelectionIndicator(string name, Transform parent, Vector3 vertexWorldPosition, Vector3 nextVertexWorldPosition, Vector3 previousVertexWorldPosition)
        {
            var indicator = new GameObject($"Selection Indicators - {name}");
            indicator.transform.SetParent(parent, worldPositionStays: true);
            indicator.layer = SelectionManager.SelectionIndicatorLayer;

            indicator.AddComponent<MeshFilter>();
            indicator.AddComponent<MeshRenderer>().sharedMaterial = SelectionIndicatorMaterial;

            UpdateSurfaceSelectionIndicator(indicator, vertexWorldPosition, nextVertexWorldPosition, previousVertexWorldPosition);

            return indicator;
        }

        public static void UpdateSurfaceSelectionIndicator(GameObject indicator, Vector3 vertexWorldPosition, Vector3 nextVertexWorldPosition, Vector3 previousVertexWorldPosition)
        {
            var thickness = 0.04f;
            var length = 0.2f;
            var clockwiseDirection = (nextVertexWorldPosition - vertexWorldPosition).normalized;
            var counterclockwiseDirection = (previousVertexWorldPosition - vertexWorldPosition).normalized;
            var scale = Mathf.Min(1f, Vector3.Distance(vertexWorldPosition, nextVertexWorldPosition) / (length * 2f), Vector3.Distance(vertexWorldPosition, previousVertexWorldPosition) / (length * 2f));

            indicator.transform.position = vertexWorldPosition;

            var meshFilter = indicator.GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh)
            {
                UnityEngine.Object.Destroy(meshFilter.sharedMesh);
            }

            meshFilter.sharedMesh = CreateSurfaceSelectionIndicatorCornerMesh(clockwiseDirection, counterclockwiseDirection, length, thickness, scale);
        }

        // Fits an indicator to each corner of a surface, creating any that are missing (a ceiling's vertices wind the other way)
        public static GameObject[] FitSurfaceSelectionIndicators(string name, Transform surface, Vector3[] vertices, bool isCeiling, GameObject[] indicators = null)
        {
            if (indicators == null)
            {
                indicators = new GameObject[vertices.Length];
            }

            var localToWorldMatrix = surface.localToWorldMatrix;

            for (var i = 0; i < vertices.Length; i++)
            {
                var nextIndex = i < vertices.Length - 1 ? i + 1 : 0;
                var previousIndex = i >= 1 ? i - 1 : vertices.Length - 1;

                if (isCeiling)
                {
                    (nextIndex, previousIndex) = (previousIndex, nextIndex);
                }

                var vertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[i]);
                var nextVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[nextIndex]);
                var previousVertexWorldPosition = localToWorldMatrix.MultiplyPoint(vertices[previousIndex]);

                if (indicators[i])
                {
                    UpdateSurfaceSelectionIndicator(indicators[i], vertexWorldPosition, nextVertexWorldPosition, previousVertexWorldPosition);
                }
                else
                {
                    indicators[i] = CreateSurfaceSelectionIndicator($"{name} ({i})", surface, vertexWorldPosition, nextVertexWorldPosition, previousVertexWorldPosition);
                }
            }

            return indicators;
        }

        public static void DestroySurfaceSelectionIndicator(GameObject indicator)
        {
            if (!indicator)
            {
                return;
            }

            var meshFilter = indicator.GetComponent<MeshFilter>();
            if (meshFilter && meshFilter.sharedMesh)
            {
                UnityEngine.Object.Destroy(meshFilter.sharedMesh);
            }

            UnityEngine.Object.Destroy(indicator);
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
