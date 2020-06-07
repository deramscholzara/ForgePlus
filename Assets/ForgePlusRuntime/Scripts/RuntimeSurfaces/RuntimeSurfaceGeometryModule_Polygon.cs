using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Materials;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Weland;

namespace RuntimeCore.Entities.Geometry
{
    public class RuntimeSurfaceGeometryModule_Polygon : RuntimeSurfaceGeometryModule_Base
    {
        private readonly LevelEntity_Polygon polygonEntity;
        private readonly LevelEntity_Polygon.DataSources dataSource;

        private LevelEntity_Platform platformComponent;

        public RuntimeSurfaceGeometryModule_Polygon(
            LevelEntity_Polygon polygonEntity,
            LevelEntity_Polygon.DataSources dataSource,
            Mesh surfaceMesh,
            MeshRenderer surfaceRenderer) : base()
        {
            this.polygonEntity = polygonEntity;
            this.dataSource = dataSource;
            SurfaceMesh = surfaceMesh;
            SurfaceRenderer = surfaceRenderer;
        }

        public override void AssembleSurface()
        {
            // Note: ApplyTextureOffset() & ApplyTransferMode() will be run due to vertex
            //       count change being detected during ApplyPositionsAndTriangles()
            ApplyPositionsAndTriangles();
            ApplyTransformPosition();

            ApplyPlatform();
            ApplyLight();
            ApplyMedia();
            ApplyBatchKeyMaterial();

            ApplyRendererMaterials();

            ApplyInteractiveSurface();
        }

        public override void ApplyPositionsAndTriangles()
        {
            var positions = new Vector3[polygonEntity.NativeObject.VertexCount];
            var triangles = new int[(polygonEntity.NativeObject.VertexCount - 2) * 3];

            // Using Collapsing Convex-Polygon Traversal for speediness reasons
            for (int earlyVertexIndex = 0, lateVertexIndex = polygonEntity.NativeObject.VertexCount - 1, currentTriangleIndex = 0;
                 earlyVertexIndex <= lateVertexIndex;
                 earlyVertexIndex++, lateVertexIndex--)
            {
                AssignVertexPosition(earlyVertexIndex, polygonEntity.NativeObject, positions);

                if (earlyVertexIndex < lateVertexIndex)
                {
                    // Vertex-traversal has not intersected, so continue
                    AssignVertexPosition(lateVertexIndex, polygonEntity.NativeObject, positions);

                    // Note: only need to rebuild triangles if the vertex count changed
                    if (polygonEntity.NativeObject.VertexCount != SurfaceMesh.vertexCount &&
                        earlyVertexIndex + 1 < lateVertexIndex)
                    {
                        // Vertex-traversal is not on the final vertices, so continue
                        AssignTriangle(earlyVertexIndex,
                                       lateVertexIndex,
                                       currentTriangleIndex,
                                       triangles,
                                       reverseOrder: dataSource == LevelEntity_Polygon.DataSources.Ceiling);

                        currentTriangleIndex += 3;

                        if (earlyVertexIndex + 1 < lateVertexIndex - 1)
                        {
                            // Vertex traversal is not about to intersect, so continue
                            AssignTriangle(earlyVertexIndex,
                                           lateVertexIndex,
                                           currentTriangleIndex,
                                           triangles,
                                           isLateTriangle: true,
                                           reverseOrder: dataSource == LevelEntity_Polygon.DataSources.Ceiling);

                            currentTriangleIndex += 3;
                        }
                    }
                }
            }

            var vertexCountChanged = polygonEntity.NativeObject.VertexCount != SurfaceMesh.vertexCount;

            SurfaceMesh.SetVertices(positions);

            if (vertexCountChanged)
            {
                SurfaceMesh.SetTriangles(triangles, submesh: 0);

                ApplyTextureOffset();
                ApplyTransferMode();
            }

            SurfaceMesh.RecalculateNormals(MeshUpdateFlags.DontNotifyMeshUsers |
                                           MeshUpdateFlags.DontRecalculateBounds |
                                           MeshUpdateFlags.DontResetBoneBounds);

            SurfaceMesh.RecalculateTangents(MeshUpdateFlags.DontNotifyMeshUsers |
                                            MeshUpdateFlags.DontRecalculateBounds |
                                            MeshUpdateFlags.DontResetBoneBounds);
        }

        public override void ApplyTransformPosition()
        {

            switch (dataSource)
            {
                case LevelEntity_Polygon.DataSources.Floor:
                    SurfaceRenderer.transform.position = new Vector3(
                        0f,
                        polygonEntity.NativeObject.FloorHeight / GeometryUtilities.WorldUnitIncrementsPerMeter,
                        0f);
                    break;

                case LevelEntity_Polygon.DataSources.Ceiling:
                    SurfaceRenderer.transform.position = new Vector3(
                        0f,
                        polygonEntity.NativeObject.CeilingHeight / GeometryUtilities.WorldUnitIncrementsPerMeter,
                        0f);
                    break;

                case LevelEntity_Polygon.DataSources.Media:
                    // Media height is set by the the media subscription in
                    // RuntimeSurfaceGeometry.ApplyMedia and in its constructor
                    return;

                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }
        }

        public override void ApplyPlatform()
        {
            if (platformComponent)
            {
                switch (dataSource)
                {
                    case LevelEntity_Polygon.DataSources.Floor:
                        if (polygonEntity.ParentLevel.FloorPlatforms.ContainsKey(platformComponent.NativeIndex))
                        {
                            polygonEntity.ParentLevel.FloorPlatforms.Remove(platformComponent.NativeIndex);
                        }
                        break;

                    case LevelEntity_Polygon.DataSources.Ceiling:
                        if (polygonEntity.ParentLevel.CeilingPlatforms.ContainsKey(platformComponent.NativeIndex))
                        {
                            polygonEntity.ParentLevel.CeilingPlatforms.Remove(platformComponent.NativeIndex);
                        }
                        break;

                    case LevelEntity_Polygon.DataSources.Media:
                        // Media surfaces don't have platforms
                        return;

                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }

                platformComponent.PrepareForDestruction();
                UnityEngine.Object.Destroy(platformComponent);
            }

            var platform = GeometryUtilities.GetPlatformForPolygon(polygonEntity.ParentLevel.Level, polygonEntity.NativeObject);

            if (platform != null)
            {
                switch (dataSource)
                {
                    case LevelEntity_Polygon.DataSources.Floor:
                        if (platform.ComesFromFloor)
                        {
                            var runtimePlatform = SurfaceRenderer.gameObject.AddComponent<LevelEntity_Platform>();

                            runtimePlatform.InitializeEntity(
                                polygonEntity.ParentLevel,
                                polygonEntity.NativeObject.Permutation,
                                platform);
                            runtimePlatform.UpdatePlatformValues(LevelEntity_Platform.LinkedSurfaces.Floor);

                            polygonEntity.ParentLevel.FloorPlatforms[polygonEntity.NativeObject.Permutation] = runtimePlatform;

                            platformComponent = runtimePlatform;
                        }
                        break;
                    case LevelEntity_Polygon.DataSources.Ceiling:
                        if (platform.ComesFromCeiling)
                        {
                            var runtimePlatform = SurfaceRenderer.gameObject.AddComponent<LevelEntity_Platform>();

                            runtimePlatform.InitializeEntity(
                                polygonEntity.ParentLevel,
                                polygonEntity.NativeObject.Permutation,
                                platform);
                            runtimePlatform.UpdatePlatformValues(LevelEntity_Platform.LinkedSurfaces.Ceiling);

                            polygonEntity.ParentLevel.CeilingPlatforms[polygonEntity.NativeObject.Permutation] = runtimePlatform;

                            platformComponent = runtimePlatform;
                        }
                        break;

                    case LevelEntity_Polygon.DataSources.Media:
                        // Media surfaces don't have platforms
                        return;

                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }
            }

            if (platformComponent)
            {
                ApplyTransformPosition();
            }
        }

        public override void ApplyTextureOffset(bool innerLayer = true)
        {
            Vector2[] UVs;

            switch (dataSource)
            {
                case LevelEntity_Polygon.DataSources.Floor:
                    UVs = BuildUVs(polygonEntity.NativeObject.FloorOrigin.X, polygonEntity.NativeObject.FloorOrigin.Y);
                    break;

                case LevelEntity_Polygon.DataSources.Ceiling:
                    UVs = BuildUVs(polygonEntity.NativeObject.CeilingOrigin.X, polygonEntity.NativeObject.CeilingOrigin.Y);
                    break;

                case LevelEntity_Polygon.DataSources.Media:
                    UVs = BuildUVs(0, 0);
                    break;

                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }

            SurfaceMesh.SetUVs(channel: 0, UVs);
        }

        public override void ApplyTransferMode(bool innerLayer = true)
        {
            Color vertexColor;

            switch (dataSource)
            {
                case LevelEntity_Polygon.DataSources.Floor:
                    vertexColor = GetTransferModeVertexColor(polygonEntity.NativeObject.FloorTransferMode);
                    break;

                case LevelEntity_Polygon.DataSources.Ceiling:
                    vertexColor = GetTransferModeVertexColor(polygonEntity.NativeObject.FloorTransferMode);
                    break;

                case LevelEntity_Polygon.DataSources.Media:
                    // Medias don't have transfer modes
                    return;

                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }

            var vertexColors = new Color[polygonEntity.NativeObject.VertexCount];
            for (var i = 0; i < polygonEntity.NativeObject.VertexCount; i++)
            {
                vertexColors[i] = vertexColor;
            }

            SurfaceMesh.SetColors(vertexColors);
        }

        public override void ApplyLight(bool innerLayer = true)
        {
            var modifiedBatchKey = BatchKey;

            switch (dataSource)
            {
                case LevelEntity_Polygon.DataSources.Floor:
                    modifiedBatchKey.sourceLight = polygonEntity.ParentLevel.Lights[polygonEntity.NativeObject.FloorLight];
                    break;

                case LevelEntity_Polygon.DataSources.Ceiling:
                    modifiedBatchKey.sourceLight = polygonEntity.ParentLevel.Lights[polygonEntity.NativeObject.CeilingLight];
                    break;

                case LevelEntity_Polygon.DataSources.Media:
                    modifiedBatchKey.sourceLight = polygonEntity.ParentLevel.Lights[polygonEntity.NativeObject.MediaLight];
                    return;

                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }

            BatchKey = modifiedBatchKey;
        }

        public override void ApplyMedia()
        {
            if (dataSource == LevelEntity_Polygon.DataSources.Media)
            {
                var modifiedBatchKey = BatchKey;

                var mediaIndex = polygonEntity.NativeObject.MediaIndex;

                if (mediaIndex >= 0)
                {
                    modifiedBatchKey.sourceMedia = polygonEntity.ParentLevel.Medias[mediaIndex];
                }
                else
                {
                    modifiedBatchKey.sourceMedia = null;
                }

                BatchKey = modifiedBatchKey;
            }
        }

        public override void ApplyBatchKeyMaterial(bool innerLayer = true)
        {
            DecrementTextureUsage();

            var modifiedBatchKey = BatchKey;

            switch (dataSource)
            {
                case LevelEntity_Polygon.DataSources.Floor:
                    modifiedBatchKey.sourceMaterial =
                        MaterialGeneration_Geometry.GetMaterial(polygonEntity.NativeObject.FloorTexture,
                                                    polygonEntity.NativeObject.FloorTransferMode,
                                                    isOpaqueSurface: true,
                                                    MaterialGeneration_Geometry.SurfaceTypes.Normal,
                                                    incrementUsageCounter: true);

                    break;

                case LevelEntity_Polygon.DataSources.Ceiling:
                    modifiedBatchKey.sourceMaterial =
                        MaterialGeneration_Geometry.GetMaterial(polygonEntity.NativeObject.CeilingTexture,
                                                    polygonEntity.NativeObject.CeilingTransferMode,
                                                    isOpaqueSurface: true,
                                                    MaterialGeneration_Geometry.SurfaceTypes.Normal,
                                                    incrementUsageCounter: true);

                    break;

                case LevelEntity_Polygon.DataSources.Media:
                    var mediaShapeDescriptor = new ShapeDescriptor((ushort)polygonEntity.NativeObject.FloorTexture);

                    switch (BatchKey.sourceMedia.NativeObject.Type)
                    {
                        case MediaType.Water:
                            mediaShapeDescriptor.Collection = 17;
                            mediaShapeDescriptor.Bitmap = 19;
                            break;
                        case MediaType.Lava:
                            mediaShapeDescriptor.Collection = 18;
                            mediaShapeDescriptor.Bitmap = 12;
                            break;
                        case MediaType.Goo:
                            mediaShapeDescriptor.Collection = 21;
                            mediaShapeDescriptor.Bitmap = 5;
                            break;
                        case MediaType.Sewage:
                            mediaShapeDescriptor.Collection = 19;
                            mediaShapeDescriptor.Bitmap = 13;
                            break;
                        case MediaType.Jjaro:
                            mediaShapeDescriptor.Collection = 20;
                            mediaShapeDescriptor.Bitmap = 13;
                            break;
                    }

                    modifiedBatchKey.sourceMaterial =
                        MaterialGeneration_Geometry.GetMaterial(mediaShapeDescriptor,
                                                    (short)TransferModes.Normal,
                                                    isOpaqueSurface: true,
                                                    MaterialGeneration_Geometry.SurfaceTypes.Media,
                                                    incrementUsageCounter: false);

                    break;

                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }

            BatchKey = modifiedBatchKey;
        }

        public override void PrepareForDestruction()
        {
            DecrementTextureUsage();
        }

        protected override void ApplyInteractiveSurface()
        {
            var nativeObject = polygonEntity.NativeObject;
            var platformIndex = nativeObject.Type == PolygonType.Platform ? nativeObject.Permutation : (short)-1;
            var media = (nativeObject.MediaIndex >= 0) ? polygonEntity.ParentLevel.Medias[nativeObject.MediaIndex] : null;

            switch (dataSource)
            {
                case LevelEntity_Polygon.DataSources.Ceiling:
                    var ceilingPlatform = polygonEntity.ParentLevel.CeilingPlatforms.FirstOrDefault(entry => entry.Key == platformIndex).Value;

                    var ceilingInteractiveSurface = SurfaceRenderer.gameObject.AddComponent<EditableSurface_Polygon>();
                    ceilingInteractiveSurface.ParentPolygon = polygonEntity;
                    ceilingInteractiveSurface.DataSource = dataSource;
                    ceilingInteractiveSurface.surfaceShapeDescriptor = nativeObject.CeilingTexture;
                    ceilingInteractiveSurface.RuntimeLight = polygonEntity.ParentLevel.Lights[nativeObject.CeilingLight];
                    ceilingInteractiveSurface.Media = media;
                    ceilingInteractiveSurface.Platform = ceilingPlatform;

                    polygonEntity.ParentLevel.EditableSurface_Polygons.Add(ceilingInteractiveSurface);
                    break;

                case LevelEntity_Polygon.DataSources.Floor:
                    var floorPlatform = polygonEntity.ParentLevel.FloorPlatforms.FirstOrDefault(entry => entry.Key == platformIndex).Value;

                    var floorInteractiveSurface = SurfaceRenderer.gameObject.AddComponent<EditableSurface_Polygon>();
                    floorInteractiveSurface.ParentPolygon = polygonEntity;
                    floorInteractiveSurface.DataSource = dataSource;
                    floorInteractiveSurface.surfaceShapeDescriptor = nativeObject.FloorTexture;
                    floorInteractiveSurface.RuntimeLight = polygonEntity.ParentLevel.Lights[nativeObject.FloorLight];
                    floorInteractiveSurface.Media = media;
                    floorInteractiveSurface.Platform = floorPlatform;

                    polygonEntity.ParentLevel.EditableSurface_Polygons.Add(floorInteractiveSurface);
                    break;

                case LevelEntity_Polygon.DataSources.Media:
                    var mediaInteractiveSurface = SurfaceRenderer.gameObject.AddComponent<EditableSurface_Media>();
                    mediaInteractiveSurface.Polygon = polygonEntity;
                    mediaInteractiveSurface.RuntimeLight = polygonEntity.ParentLevel.Lights[nativeObject.MediaLight];
                    mediaInteractiveSurface.Media = media;

                    polygonEntity.ParentLevel.EditableSurface_Medias.Add(mediaInteractiveSurface);
                    break;

                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }

            SurfaceRenderer.gameObject.AddComponent<MeshCollider>();
        }

        private void AssignVertexPosition(int vertexIndex, Polygon polygon, Vector3[] vertexPositions)
        {
            var endpointIndex = polygon.EndpointIndexes[vertexIndex];

            vertexPositions[vertexIndex] = GeometryUtilities.GetMeshVertex(polygonEntity.ParentLevel.Level, endpointIndex);
        }

        private void AssignTriangle(int earlyVertexIndex, int lateVertexIndex, int currentTriangleIndex, int[] triangles, bool reverseOrder = false, bool isLateTriangle = false)
        {
            if (isLateTriangle)
            {
                triangles[currentTriangleIndex] = earlyVertexIndex + 1;
                triangles[currentTriangleIndex + 1] = lateVertexIndex - 1;
                triangles[currentTriangleIndex + 2] = lateVertexIndex;
            }
            else
            {
                triangles[currentTriangleIndex] = earlyVertexIndex;
                triangles[currentTriangleIndex + 1] = earlyVertexIndex + 1;
                triangles[currentTriangleIndex + 2] = lateVertexIndex;
            }

            if (reverseOrder)
            {
                var firstIndex = triangles[currentTriangleIndex];
                triangles[currentTriangleIndex] = triangles[currentTriangleIndex + 2];
                triangles[currentTriangleIndex + 2] = firstIndex;
            }
        }

        private Vector2[] BuildUVs(short textureOffsetX, short textureOffsetY)
        {
            var meshUVs = new Vector2[polygonEntity.NativeObject.VertexCount];

            for (var i = 0; i < polygonEntity.NativeObject.VertexCount; i++)
            {
                var vertexPosition = GeometryUtilities.GetMeshVertex(polygonEntity.ParentLevel.Level, polygonEntity.NativeObject.EndpointIndexes[i]);

                var u = -(vertexPosition.z * GeometryUtilities.MeterToWorldUnit);
                var v = -(vertexPosition.x * GeometryUtilities.MeterToWorldUnit);
                var floorOffset = new Vector2(textureOffsetY / GeometryUtilities.WorldUnitIncrementsPerWorldUnit,
                                              -textureOffsetX / GeometryUtilities.WorldUnitIncrementsPerWorldUnit);
                meshUVs[i] = new Vector2(u, v) + floorOffset;
            }

            return meshUVs;
        }

        private Color GetTransferModeVertexColor(short transferMode)
        {
            var mode = (TransferModes)transferMode;

            switch (mode)
            {
                case TransferModes.Pulsate: // Pulsate
                case TransferModes.Wobble: // Wobble
                    return new Color(0f, 0f, 2f, 0f);
                case TransferModes.WobbleFast: // Wobble Fast
                    return new Color(0f, 0f, 20f, 0f);
                case TransferModes.HorizontalSlide: // Horizontal Slide
                    return new Color(0f, -1f / 8f, 0f, 0f);
                case TransferModes.HorizontalSlideFast: // Horizontal Slide Fast
                    return new Color(0f, -2f / 8f, 0f, 0f);
                case TransferModes.VerticalSlide: // Vertical Slide
                    return new Color(1f / 8f, 0f, 0f, 0f);
                case TransferModes.VerticalSlideFast: // Vertical Slide Fast
                    return new Color(2f / 8f, 0f, 0f, 0f);
                case TransferModes.Wander: // Wander
                    return new Color(0f, 0f, 0f, 1f);
                case TransferModes.WanderFast: // Wander Fast
                    return new Color(0f, 0f, 0f, 2f);
                default: // Normal
                    return Color.clear;
            }
        }

        private void DecrementTextureUsage()
        {
            switch (dataSource)
            {
                case LevelEntity_Polygon.DataSources.Floor:
                    MaterialGeneration_Geometry.DecrementTextureUsage(polygonEntity.NativeObject.FloorTexture);
                    break;
                case LevelEntity_Polygon.DataSources.Ceiling:
                    MaterialGeneration_Geometry.DecrementTextureUsage(polygonEntity.NativeObject.CeilingTexture);
                    break;
                case LevelEntity_Polygon.DataSources.Media:
                    // Media surfaces do not increment texture usage when calling WallsCollection.GetMaterial()
                    break;
                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }
        }
    }
}
