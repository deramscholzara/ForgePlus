using ForgePlus.ShapesCollections;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Weland;
using Weland.Extensions;

namespace ForgePlus.LevelManipulation.Utilities
{
    public static class GeometryUtilities
    {
        /// <summary>
        /// Used for converting between world-unit (WU) "increments" and meters.
        /// One WU is 1024 "increments", and we want a WU to convert to 2 meters, so we use 512 as the conversion ratio.
        /// </summary>
        public const float WorldUnitIncrementsPerMeter = 512f;
        public const float WorldUnitIncrementsPerWorldUnit = 1024f;
        public const float MeterToWorldUnit = WorldUnitIncrementsPerMeter / WorldUnitIncrementsPerWorldUnit;

        private static readonly Material SelectionIndicatorMaterial = new Material(Shader.Find("ForgePlus/GeometrySelectionIndicator"));

        public static Vector3 GetMeshVertex(Level level, int endpointIndex, short height = 0)
        {
            var endpoint = level.Endpoints[endpointIndex];

            // Convert from Marathon right-handed to Unity left-handed
            // by flipping Y-axis and assigning it to Z
            return new Vector3(endpoint.X, height, -endpoint.Y) / WorldUnitIncrementsPerMeter;
        }

        public static Platform GetPlatformForPolygonIndex(Level level, short polygonIndex)
        {
            return level.Platforms.First(platform => platform.PolygonIndex == polygonIndex);
        }

        public static short GetPlatformIndexForPolygonIndex(Level level, short polygonIndex)
        {
            for (short i = 0; i < level.Platforms.Count; i++)
            {
                if (level.Platforms[i].PolygonIndex == polygonIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        public static void BuildRendererObject(
            GameObject rendererHost,
            Vector3[] vertices,
            int[] triangles,
            Vector2[] uvs,
            ShapeDescriptor shapeDescriptor,
            FPLight fpLight,
            short transferMode,
            Color[] transferModesVertexColors,
            bool isOpaqueSurface,
            bool isStaticBatchable)
        {
            BuildRendererObject(rendererHost,
                                vertices,
                                triangles,
                                uvs,
                                shapeDescriptor,
                                fpLight,
                                transferMode,
                                transferModesVertexColors,
                                isOpaqueSurface,
                                isStaticBatchable,
                                fpMedia: null);
        }

        public static void BuildRendererObject(
            GameObject rendererHost,
            Vector3[] vertices,
            int[] triangles,
            Vector2[] uvs,
            ShapeDescriptor shapeDescriptor,
            FPLight fpLight,
            short transferMode,
            Color[] transferModesVertexColors,
            bool isOpaqueSurface,
            bool isStaticBatchable,
            FPMedia fpMedia)
        {
            if (fpMedia != null)
            {
                // Add backside geometry for the underside of medias
                // - supports seeing the media from below
                // - supports collider selection from both sides
                var mediaTriangles = new int[triangles.Length * 2];

                for (var i = 0; i < triangles.Length; i++)
                {
                    mediaTriangles[i] = triangles[i];
                    mediaTriangles[mediaTriangles.Length - 1 - i] = triangles[i];
                }

                triangles = mediaTriangles;
            }

            var mesh = new Mesh();
            mesh.name = rendererHost.name;

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, submesh: 0);
            mesh.SetUVs(channel: 0, uvs: uvs);
            mesh.SetColors(transferModesVertexColors);
            mesh.RecalculateNormals(MeshUpdateFlags.DontNotifyMeshUsers |
                                    MeshUpdateFlags.DontRecalculateBounds |
                                    MeshUpdateFlags.DontResetBoneBounds);
            mesh.RecalculateTangents(MeshUpdateFlags.DontNotifyMeshUsers |
                                     MeshUpdateFlags.DontRecalculateBounds |
                                     MeshUpdateFlags.DontResetBoneBounds);

            rendererHost.AddComponent<MeshFilter>().sharedMesh = mesh;

            // Assign Common Wall Material
            var surfaceType = fpMedia == null ? WallsCollection.SurfaceTypes.Normal : WallsCollection.SurfaceTypes.Media;
            var material = WallsCollection.GetMaterial(shapeDescriptor, transferMode, isOpaqueSurface, surfaceType, incrementUsageCounter: true);
            rendererHost.AddComponent<MeshRenderer>().sharedMaterial = material;

            // Assign Appropriate Runtime Surface Component
            if (fpMedia != null)
            {
                var surfaceMedia = rendererHost.AddComponent<RuntimeSurfaceMedia>();
                surfaceMedia.InitializeRuntimeSurface(fpLight, fpMedia);
            }
            else if (fpLight != null)
            {
                var surfaceLight = rendererHost.AddComponent<RuntimeSurfaceLight>();
                surfaceLight.InitializeRuntimeSurface(shapeDescriptor.UsesLandscapeCollection() ? null : fpLight, isStaticBatchable);
            }

            rendererHost.AddComponent<MeshCollider>();
        }

        public static Color GetTransferModeVertexColor(short transferMode, bool isSideSurface)
        {
            switch (transferMode)
            {
                case 4: // Pulsate
                case 5: // Wobble
                    return new Color(0f, 0f, 2f, 0f);
                case 6: // Wobble Fast
                    return new Color(0f, 0f, 20f, 0f);
                case 15: // Horizontal Slide
                    return isSideSurface ? new Color(-1f / 8f, 0f, 0f, 0f) : new Color(0f, -1f / 8f, 0f, 0f);
                case 16: // Horizontal Slide Fast
                    return isSideSurface ? new Color(-2f / 8f, 0f, 0f, 0f) : new Color(0f, -2f / 8f, 0f, 0f);
                case 17: // Vertical Slide
                    return isSideSurface ? new Color(0f, 1f / 8f, 0f, 0f) : new Color(1f / 8f, 0f, 0f, 0f);
                case 18: // Vertical Slide Fast
                    return isSideSurface ? new Color(0f, 2f / 8f, 0f, 0f) : new Color(2f / 8f, 0f, 0f, 0f);
                case 19: // Wander
                    return new Color(0f, 0f, 0f, 1f);
                case 20: // Wander Fast
                    return new Color(0f, 0f, 0f, 2f);
                default: // Normal
                    return Color.clear;
            }
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
