using ForgePlus.ShapesCollections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Weland;

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

        public static void BuildRendererObject(
            Level level,
            GameObject rendererHost,
            Vector3[] vertices,
            int[] triangles,
            Vector2[] uvs,
            ShapeDescriptor shapeDescriptor,
            FPLight fpLight,
            short lightIndex,
            short transferMode,
            Color[] transferModesVertexColors,
            bool isOpaqueSurface,
            WallsCollection.SurfaceTypes surfaceType = WallsCollection.SurfaceTypes.Normal,
            Media media = null)
        {
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
            var material = WallsCollection.GetMaterial(shapeDescriptor, transferMode, isOpaqueSurface, surfaceType);
            rendererHost.AddComponent<MeshRenderer>().sharedMaterial = material;

            // Assign Light
            if (fpLight != null)
            {
                var surfaceLight = rendererHost.AddComponent<SurfaceLight>();
                surfaceLight.AssignFPLight(fpLight, lightIndex, surfaceType != WallsCollection.SurfaceTypes.Media ? 0f : (float)media.MinimumLightIntensity);
            }

            if (surfaceType == WallsCollection.SurfaceTypes.Media)
            {
                var surfaceMedia = rendererHost.AddComponent<FPMedia>();
                surfaceMedia.AssignMedia(media);
            }
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
    }
}
