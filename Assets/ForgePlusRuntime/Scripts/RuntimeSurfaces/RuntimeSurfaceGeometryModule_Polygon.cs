using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Materials;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using AlephOne;
using ForgePlus.Extensions;

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
            var positions = new Vector3[polygonEntity.NativeObject.vertex_count];
            var triangles = new int[(polygonEntity.NativeObject.vertex_count - 2) * (dataSource == LevelEntity_Polygon.DataSources.Media ? 6 : 3)];

            // Using Collapsing Convex-Polygon Traversal for speediness reasons
            for (int earlyVertexIndex = 0, lateVertexIndex = polygonEntity.NativeObject.vertex_count - 1, currentTriangleIndex = 0;
                 earlyVertexIndex <= lateVertexIndex;
                 earlyVertexIndex++, lateVertexIndex--)
            {
                AssignVertexPosition(earlyVertexIndex, polygonEntity.NativeObject, positions);

                if (earlyVertexIndex < lateVertexIndex)
                {
                    // Vertex-traversal has not intersected, so continue
                    AssignVertexPosition(lateVertexIndex, polygonEntity.NativeObject, positions);

                    // Note: only need to rebuild triangles if the vertex count changed
                    if (polygonEntity.NativeObject.vertex_count != SurfaceMesh.vertexCount &&
                        earlyVertexIndex + 1 < lateVertexIndex)
                    {
                        // Vertex-traversal is not on the final vertices, so continue
                        AssignTriangle(earlyVertexIndex,
                                       lateVertexIndex,
                                       currentTriangleIndex,
                                       triangles,
                                       reverseOrder: dataSource == LevelEntity_Polygon.DataSources.Ceiling);

                        currentTriangleIndex += 3;

                        if (dataSource == LevelEntity_Polygon.DataSources.Media)
                        {
                            // Backside triangles for media
                            AssignTriangle(earlyVertexIndex,
                                           lateVertexIndex,
                                           currentTriangleIndex,
                                           triangles,
                                           reverseOrder: true);

                            currentTriangleIndex += 3;
                        }

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

                            if (dataSource == LevelEntity_Polygon.DataSources.Media)
                            {
                                // Backside triangles for media
                                AssignTriangle(earlyVertexIndex,
                                               lateVertexIndex,
                                               currentTriangleIndex,
                                               triangles,
                                               isLateTriangle: true,
                                               reverseOrder: true);

                                currentTriangleIndex += 3;
                            }
                        }
                    }
                }
            }

            var vertexCountChanged = polygonEntity.NativeObject.vertex_count != SurfaceMesh.vertexCount;

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
                        polygonEntity.NativeObject.floor_height / GeometryUtilities.WorldUnitIncrementsPerMeter,
                        0f);
                    break;

                case LevelEntity_Polygon.DataSources.Ceiling:
                    SurfaceRenderer.transform.position = new Vector3(
                        0f,
                        polygonEntity.NativeObject.ceiling_height / GeometryUtilities.WorldUnitIncrementsPerMeter,
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

            IsStaticBatchable = true;

            var platform = polygonEntity.NativeObject.GetPlatform(polygonEntity.ParentLevel.Level);

            if (platform != null)
            {
                switch (dataSource)
                {
                    case LevelEntity_Polygon.DataSources.Floor:
                        if (platforms.PLATFORM_COMES_FROM_FLOOR(platform.static_flags))
                        {
                            var runtimePlatform = SurfaceRenderer.gameObject.AddComponent<LevelEntity_Platform>();

                            runtimePlatform.InitializeEntity(
                                polygonEntity.ParentLevel,
                                polygonEntity.NativeObject.permutation,
                                platform);
                            runtimePlatform.UpdatePlatformValues(LevelEntity_Platform.LinkedSurfaces.Floor);

                            polygonEntity.ParentLevel.FloorPlatforms[polygonEntity.NativeObject.permutation] = runtimePlatform;

                            platformComponent = runtimePlatform;

                            IsStaticBatchable = false;

                            runtimePlatform.BeginRuntimeStyleBehavior();
                        }
                        break;
                    case LevelEntity_Polygon.DataSources.Ceiling:
                        if (platforms.PLATFORM_COMES_FROM_CEILING(platform.static_flags))
                        {
                            var runtimePlatform = SurfaceRenderer.gameObject.AddComponent<LevelEntity_Platform>();

                            runtimePlatform.InitializeEntity(
                                polygonEntity.ParentLevel,
                                polygonEntity.NativeObject.permutation,
                                platform);
                            runtimePlatform.UpdatePlatformValues(LevelEntity_Platform.LinkedSurfaces.Ceiling);

                            polygonEntity.ParentLevel.CeilingPlatforms[polygonEntity.NativeObject.permutation] = runtimePlatform;

                            platformComponent = runtimePlatform;

                            IsStaticBatchable = false;

                            runtimePlatform.BeginRuntimeStyleBehavior();
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
            Vector4[] UVs;

            switch (dataSource)
            {
                case LevelEntity_Polygon.DataSources.Floor:
                    UVs = BuildUVs(polygonEntity.NativeObject.floor_origin.x, polygonEntity.NativeObject.floor_origin.y);
                    break;

                case LevelEntity_Polygon.DataSources.Ceiling:
                    UVs = BuildUVs(polygonEntity.NativeObject.ceiling_origin.x, polygonEntity.NativeObject.ceiling_origin.y);
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
            short transferMode;

            switch (dataSource)
            {
                case LevelEntity_Polygon.DataSources.Floor:
                    transferMode = polygonEntity.NativeObject.floor_transfer_mode;
                    break;

                case LevelEntity_Polygon.DataSources.Ceiling:
                    transferMode = polygonEntity.NativeObject.ceiling_transfer_mode;
                    break;

                case LevelEntity_Polygon.DataSources.Media:
                    // Medias don't have transfer modes
                    return;

                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }

            var vertexColor = GetTransferModeVertexColor(transferMode);
            var vertexColors = new Color[polygonEntity.NativeObject.vertex_count];
            for (var i = 0; i < polygonEntity.NativeObject.vertex_count; i++)
            {
                vertexColors[i] = vertexColor;
            }

            SurfaceMesh.SetColors(vertexColors);

            ApplyTransferModeEffects(transferMode, innerLayer: true);
        }

        public override void ApplyLight(bool innerLayer = true)
        {
            var modifiedBatchKey = BatchKey;

            switch (dataSource)
            {
                case LevelEntity_Polygon.DataSources.Floor:
                    modifiedBatchKey.SourceLight = polygonEntity.ParentLevel.Lights[polygonEntity.NativeObject.floor_lightsource_index];
                    lastLightIndex = polygonEntity.NativeObject.floor_lightsource_index;
                    break;

                case LevelEntity_Polygon.DataSources.Ceiling:
                    modifiedBatchKey.SourceLight = polygonEntity.ParentLevel.Lights[polygonEntity.NativeObject.ceiling_lightsource_index];
                    lastLightIndex = polygonEntity.NativeObject.ceiling_lightsource_index;
                    break;

                case LevelEntity_Polygon.DataSources.Media:
                    modifiedBatchKey.SourceLight = polygonEntity.ParentLevel.Lights[polygonEntity.NativeObject.media_lightsource_index];
                    lastLightIndex = polygonEntity.NativeObject.media_lightsource_index;
                    break;

                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }
            
            var UVs = SurfaceMesh.uv.Select(uv => new Vector4(uv.x, uv.y, lastLightIndex, lastTextureIndex)).ToArray();
            SurfaceMesh.SetUVs(0, UVs);

            BatchKey = modifiedBatchKey;
        }

        public override void ApplyMedia()
        {
            if (dataSource == LevelEntity_Polygon.DataSources.Media)
            {
                var modifiedBatchKey = BatchKey;

                var mediaIndex = polygonEntity.NativeObject.media_index;

                if (mediaIndex >= 0)
                {
                    modifiedBatchKey.SourceMedia = polygonEntity.ParentLevel.Medias[mediaIndex];
                }
                else
                {
                    modifiedBatchKey.SourceMedia = null;
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
#if USE_TEXTURE_ARRAYS
                    modifiedBatchKey.SourceShapeDescriptor = polygonEntity.NativeObject.floor_texture;
#endif
                    modifiedBatchKey.SourceMaterial =
                        MaterialGeneration_Geometry.GetMaterial(
                            polygonEntity.NativeObject.floor_texture,
                            polygonEntity.NativeObject.floor_transfer_mode,
                            isOpaqueSurface: true,
                            MaterialGeneration_Geometry.SurfaceTypes.Normal,
                            incrementUsageCounter: true);
            
#if USE_TEXTURE_ARRAYS
                    lastTextureIndex = MaterialGeneration_Geometry.GetTextureArrayIndex(
                        polygonEntity.NativeObject.floor_texture,
                        polygonEntity.NativeObject.floor_transfer_mode,
                        isOpaqueSurface: true,
                        MaterialGeneration_Geometry.SurfaceTypes.Normal);
#endif

                    break;

                case LevelEntity_Polygon.DataSources.Ceiling:
#if USE_TEXTURE_ARRAYS
                    modifiedBatchKey.SourceShapeDescriptor = polygonEntity.NativeObject.ceiling_texture;
#endif
                    modifiedBatchKey.SourceMaterial =
                        MaterialGeneration_Geometry.GetMaterial(
                            polygonEntity.NativeObject.ceiling_texture,
                            polygonEntity.NativeObject.ceiling_transfer_mode,
                            isOpaqueSurface: true,
                            MaterialGeneration_Geometry.SurfaceTypes.Normal,
                            incrementUsageCounter: true);
            
#if USE_TEXTURE_ARRAYS
                    lastTextureIndex = MaterialGeneration_Geometry.GetTextureArrayIndex(
                        polygonEntity.NativeObject.ceiling_texture,
                        polygonEntity.NativeObject.ceiling_transfer_mode,
                        isOpaqueSurface: true,
                        MaterialGeneration_Geometry.SurfaceTypes.Normal);
#endif

                    break;

                case LevelEntity_Polygon.DataSources.Media:
                    // A media's texture comes from its type's definition (media.cpp: new_media / update_medias)
                    var mediaDefinition = AlephOne.media.get_media_definition(BatchKey.SourceMedia.NativeObject.type);
                    var mediaShapeDescriptor = mediaDefinition != null ?
                                               shape_descriptors.BUILD_DESCRIPTOR(mediaDefinition.collection, mediaDefinition.shape) :
                                               cstypes.UNONE;
                    
#if USE_TEXTURE_ARRAYS
                    modifiedBatchKey.SourceShapeDescriptor = mediaShapeDescriptor;
#endif
                    modifiedBatchKey.SourceMaterial =
                        MaterialGeneration_Geometry.GetMaterial(
                            mediaShapeDescriptor,
                            (short)TransferModes.Normal,
                            isOpaqueSurface: true,
                            MaterialGeneration_Geometry.SurfaceTypes.Media,
                            incrementUsageCounter: false);
            
#if USE_TEXTURE_ARRAYS
                    lastTextureIndex = MaterialGeneration_Geometry.GetTextureArrayIndex(
                        mediaShapeDescriptor,
                        (short) TransferModes.Normal,
                        isOpaqueSurface: true,
                        MaterialGeneration_Geometry.SurfaceTypes.Media);
#endif

                    break;

                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }
            
            var UVs = SurfaceMesh.uv.Select(uv => new Vector4(uv.x, uv.y, lastLightIndex, lastTextureIndex)).ToArray();
            SurfaceMesh.SetUVs(0, UVs);

            BatchKey = modifiedBatchKey;
        }

        public override void PrepareForDestruction()
        {
            DecrementTextureUsage();
        }

        protected override void ApplyInteractiveSurface()
        {
            var nativeObject = polygonEntity.NativeObject;
            var platformIndex = nativeObject.type == map._polygon_is_platform ? nativeObject.permutation : (short)-1;

            switch (dataSource)
            {
                case LevelEntity_Polygon.DataSources.Ceiling:
                    var ceilingPlatform = polygonEntity.ParentLevel.CeilingPlatforms.FirstOrDefault(entry => entry.Key == platformIndex).Value;

                    var ceilingInteractiveSurface = SurfaceRenderer.gameObject.AddComponent<EditableSurface_Polygon>();
                    ceilingInteractiveSurface.ParentPolygon = polygonEntity;
                    ceilingInteractiveSurface.DataSource = dataSource;
                    ceilingInteractiveSurface.Platform = ceilingPlatform;

                    polygonEntity.ParentLevel.EditableSurface_Polygons.Add(ceilingInteractiveSurface);
                    break;

                case LevelEntity_Polygon.DataSources.Floor:
                    var floorPlatform = polygonEntity.ParentLevel.FloorPlatforms.FirstOrDefault(entry => entry.Key == platformIndex).Value;

                    var floorInteractiveSurface = SurfaceRenderer.gameObject.AddComponent<EditableSurface_Polygon>();
                    floorInteractiveSurface.ParentPolygon = polygonEntity;
                    floorInteractiveSurface.DataSource = dataSource;
                    floorInteractiveSurface.Platform = floorPlatform;

                    polygonEntity.ParentLevel.EditableSurface_Polygons.Add(floorInteractiveSurface);
                    break;

                case LevelEntity_Polygon.DataSources.Media:
                    var mediaInteractiveSurface = SurfaceRenderer.gameObject.AddComponent<EditableSurface_Media>();
                    mediaInteractiveSurface.Polygon = polygonEntity;

                    polygonEntity.ParentLevel.EditableSurface_Medias.Add(mediaInteractiveSurface);
                    break;

                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }

            // PhysX can't make a collider from a surface with no area ("cleaning the mesh failed"); one is added if it
            // gets some (RuntimeSurfaceGeometry.ApplyShape)
            if (RuntimeSurfaceGeometry.HasArea(SurfaceMesh))
            {
                SurfaceRenderer.gameObject.AddComponent<MeshCollider>();
            }
        }

        private void AssignVertexPosition(int vertexIndex, polygon_data polygon, Vector3[] vertexPositions)
        {
            var endpointIndex = polygon.endpoint_indexes[vertexIndex];

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

        private Vector4[] BuildUVs(short textureOffsetX, short textureOffsetY)
        {
            var meshUVs = new Vector4[polygonEntity.NativeObject.vertex_count];

            for (var i = 0; i < polygonEntity.NativeObject.vertex_count; i++)
            {
                var vertexPosition = GeometryUtilities.GetMeshVertex(polygonEntity.ParentLevel.Level, polygonEntity.NativeObject.endpoint_indexes[i]);

                var u = -(vertexPosition.z * GeometryUtilities.MeterToWorldUnit);
                var v = -(vertexPosition.x * GeometryUtilities.MeterToWorldUnit);
                var floorOffset = new Vector4(textureOffsetY / GeometryUtilities.WorldUnitIncrementsPerWorldUnit,
                                              -textureOffsetX / GeometryUtilities.WorldUnitIncrementsPerWorldUnit,
                                              0f,
                                              0f);
                meshUVs[i] = new Vector4(u, v, lastLightIndex, lastTextureIndex) + floorOffset;
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
                    MaterialGeneration_Geometry.DecrementTextureUsage(polygonEntity.NativeObject.floor_texture);
                    break;
                case LevelEntity_Polygon.DataSources.Ceiling:
                    MaterialGeneration_Geometry.DecrementTextureUsage(polygonEntity.NativeObject.ceiling_texture);
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
