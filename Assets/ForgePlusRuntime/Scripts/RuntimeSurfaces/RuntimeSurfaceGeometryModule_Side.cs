using ForgePlus.Entities.Geometry;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Constraints;
using RuntimeCore.Materials;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using Weland;
using Weland.Extensions;

namespace RuntimeCore.Entities.Geometry
{
    public class RuntimeSurfaceGeometryModule_Side : RuntimeSurfaceGeometryModule_Base
    {
        private readonly LevelEntity_Side sideEntity;
        private readonly LevelEntity_Side.DataSources dataSource;

        private PlatformConstraint platformConstraint;

        private short LowElevation
        {
            get
            {
                switch (dataSource)
                {
                    case LevelEntity_Side.DataSources.Primary:
                        return sideEntity.PrimaryLowElevation;
                    case LevelEntity_Side.DataSources.Secondary:
                        return sideEntity.SecondaryLowElevation;
                    case LevelEntity_Side.DataSources.Transparent:
                        return sideEntity.TransparentLowElevation;
                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }
            }
        }

        private short HighElevation
        {
            get
            {
                switch (dataSource)
                {
                    case LevelEntity_Side.DataSources.Primary:
                        return sideEntity.PrimaryHighElevation;
                    case LevelEntity_Side.DataSources.Secondary:
                        return sideEntity.SecondaryHighElevation;
                    case LevelEntity_Side.DataSources.Transparent:
                        return sideEntity.TransparentHighElevation;
                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }
            }
        }

        public RuntimeSurfaceGeometryModule_Side(
            LevelEntity_Side sideEntity,
            LevelEntity_Side.DataSources dataSource,
            Mesh surfaceMesh,
            MeshRenderer surfaceRenderer) : base()
        {
            this.sideEntity = sideEntity;
            this.dataSource = dataSource;
            SurfaceMesh = surfaceMesh;
            SurfaceRenderer = surfaceRenderer;
        }

        public override void AssembleSurface()
        {
            if (sideEntity == null)
            {
                Debug.Log($"Null Side: {sideEntity.name}", sideEntity);
            }

            ApplyPositionsAndTriangles();
            ApplyTransformPosition();

            ApplyPlatform();

            ApplyTextureOffset(innerLayer: true);
            ApplyTransferMode(innerLayer: true);
            ApplyLight(innerLayer: true);
            ApplyBatchKeyMaterial(innerLayer: true);

            if (sideEntity.NativeObject.HasLayeredTransparentSide(sideEntity.ParentLevel.Level))
            {
                ApplyTextureOffset(innerLayer: false);
                ApplyTransferMode(innerLayer: false);
                ApplyLight(innerLayer: false);
                ApplyBatchKeyMaterial(innerLayer: false);
            }

            ApplyRendererMaterials();

            ApplyInteractiveSurface();
        }

        public override void ApplyPositionsAndTriangles()
        {
            var line = sideEntity.ParentLevel.Level.Lines[sideEntity.ParentLineIndex];

            var endpointIndexA = sideEntity.IsClockwise ? line.EndpointIndexes[0] : line.EndpointIndexes[1];
            var endpointIndexB = sideEntity.IsClockwise ? line.EndpointIndexes[1] : line.EndpointIndexes[0];

            var bottomPosition = (short)(LowElevation - HighElevation);

            var positions = new Vector3[]
            {
                GeometryUtilities.GetMeshVertex(sideEntity.ParentLevel.Level, endpointIndexA, bottomPosition),
                GeometryUtilities.GetMeshVertex(sideEntity.ParentLevel.Level, endpointIndexA),
                GeometryUtilities.GetMeshVertex(sideEntity.ParentLevel.Level, endpointIndexB),
                GeometryUtilities.GetMeshVertex(sideEntity.ParentLevel.Level, endpointIndexB, bottomPosition)
            };

            var triangles = new int[6];

            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 2;
            triangles[4] = 3;
            triangles[5] = 0;

            SurfaceMesh.SetVertices(positions);
            SurfaceMesh.SetTriangles(triangles, submesh: 0);

            SurfaceMesh.RecalculateNormals(MeshUpdateFlags.DontNotifyMeshUsers |
                                           MeshUpdateFlags.DontRecalculateBounds |
                                           MeshUpdateFlags.DontResetBoneBounds);

            SurfaceMesh.RecalculateTangents(MeshUpdateFlags.DontNotifyMeshUsers |
                                            MeshUpdateFlags.DontRecalculateBounds |
                                            MeshUpdateFlags.DontResetBoneBounds);
        }

        public override void ApplyTransformPosition()
        {
            SurfaceRenderer.transform.position = new Vector3(
                0f,
                HighElevation / GeometryUtilities.WorldUnitIncrementsPerMeter,
                0f);
        }

        public override void ApplyPlatform()
        {
            if (sideEntity.NativeObject.HasLayeredTransparentSide(sideEntity.ParentLevel.Level))
            {
                // Note: Layered transparent sides have no opposing platform to
                //       attach to, because they have no opposing polygon.
                return;
            }

            UnityEngine.Object.Destroy(platformConstraint);

            IsStaticBatchable = true;

            var line = sideEntity.ParentLevel.Level.Lines[sideEntity.ParentLineIndex];

            var opposingPolygonIndex = sideEntity.IsClockwise ? line.ClockwisePolygonOwner : line.CounterclockwisePolygonOwner;
            if (opposingPolygonIndex < 0)
            {
                return;
            }

            var opposingPolygon = sideEntity.ParentLevel.Level.Polygons[opposingPolygonIndex];
            if (opposingPolygon.Type != Weland.PolygonType.Platform)
            {
                return;
            }

            var opposingPlatformIndex = opposingPolygon.Permutation;
            if (opposingPlatformIndex < 0)
            {
                return;
            }

            if (sideEntity.ParentLevel.CeilingPlatforms.ContainsKey(opposingPlatformIndex) &&
                (dataSource == LevelEntity_Side.DataSources.Primary ||
                 dataSource == LevelEntity_Side.DataSources.Transparent))
            {
                var opposingCeilingPlatform = sideEntity.ParentLevel.CeilingPlatforms[opposingPlatformIndex];
                ConstrainSurfaceToPlatform(opposingCeilingPlatform,
                                           constrainAbovePlatform: dataSource == LevelEntity_Side.DataSources.Primary);

                IsStaticBatchable = false;
            }
            else if (sideEntity.ParentLevel.FloorPlatforms.ContainsKey(opposingPlatformIndex) &&
                     dataSource == LevelEntity_Side.DataSources.Secondary)
            {
                var opposingFloorPlatform = sideEntity.ParentLevel.FloorPlatforms[opposingPlatformIndex];
                ConstrainSurfaceToPlatform(opposingFloorPlatform, constrainAbovePlatform: false);

                IsStaticBatchable = false;
            }
        }

        public override void ApplyTextureOffset(bool innerLayer)
        {
            Vector2[] UVs;

            if (sideEntity.NativeObject == null)
            {
                UVs = BuildUVs(0, 0);
            }
            else if (innerLayer)
            {
                switch (dataSource)
                {
                    case LevelEntity_Side.DataSources.Primary:
                        UVs = BuildUVs(sideEntity.NativeObject.Primary.X, sideEntity.NativeObject.Primary.Y);
                        break;

                    case LevelEntity_Side.DataSources.Secondary:
                        UVs = BuildUVs(sideEntity.NativeObject.Secondary.X, sideEntity.NativeObject.Secondary.Y);
                        break;

                    case LevelEntity_Side.DataSources.Transparent:
                        UVs = BuildUVs(sideEntity.NativeObject.Transparent.X, sideEntity.NativeObject.Transparent.Y);
                        break;

                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }

                SurfaceMesh.SetUVs(channel: 0, UVs);
            }
            else
            {
                UVs = BuildUVs(sideEntity.NativeObject.Transparent.X, sideEntity.NativeObject.Transparent.Y);

                SurfaceMesh.SetUVs(channel: 1, UVs);
            }
        }

        public override void ApplyTransferMode(bool innerLayer)
        {
            if (sideEntity.NativeObject == null)
            {
                return;
            }

            if (innerLayer)
            {
                Color vertexColor;

                switch (dataSource)
                {
                    case LevelEntity_Side.DataSources.Primary:
                        vertexColor = GetTransferModeVertexColor(sideEntity.NativeObject.PrimaryTransferMode);
                        break;

                    case LevelEntity_Side.DataSources.Secondary:
                        vertexColor = GetTransferModeVertexColor(sideEntity.NativeObject.SecondaryTransferMode);
                        break;

                    case LevelEntity_Side.DataSources.Transparent:
                        vertexColor = GetTransferModeVertexColor(sideEntity.NativeObject.TransparentTransferMode);
                        break;

                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }

                var vertexColors = new Color[4];
                for (var i = 0; i < 4; i++)
                {
                    vertexColors[i] = vertexColor;
                }

                SurfaceMesh.SetColors(vertexColors);
            }
            else
            {
                var vertexColor = GetTransferModeVertexColor(sideEntity.NativeObject.TransparentTransferMode);

                var uv2 = new Vector2[4];
                var uv3 = new Vector2[4];

                for (var i = 0; i < 4; i++)
                {
                    uv2[i].x = vertexColor.r;
                    uv2[i].y = vertexColor.g;
                    uv3[i].x = vertexColor.b;
                    uv3[i].y = vertexColor.a;
                }

                SurfaceMesh.SetUVs(channel: 2, uvs: uv2);
                SurfaceMesh.SetUVs(channel: 3, uvs: uv3);
            }
        }

        public override void ApplyLight(bool innerLayer)
        {
            var modifiedBatchKey = BatchKey;

            if (sideEntity.NativeObject == null)
            {
                modifiedBatchKey.sourceLight = null;
                modifiedBatchKey.layeredTransparentSideSourceLight = null;
            }
            else if (innerLayer)
            {
                switch (dataSource)
                {
                    case LevelEntity_Side.DataSources.Primary:
                        modifiedBatchKey.sourceLight = sideEntity.ParentLevel.Lights[sideEntity.NativeObject.PrimaryLightsourceIndex];
                        break;

                    case LevelEntity_Side.DataSources.Secondary:
                        modifiedBatchKey.sourceLight = sideEntity.ParentLevel.Lights[sideEntity.NativeObject.SecondaryLightsourceIndex];
                        break;

                    case LevelEntity_Side.DataSources.Transparent:
                        modifiedBatchKey.sourceLight = sideEntity.ParentLevel.Lights[sideEntity.NativeObject.TransparentLightsourceIndex];
                        break;

                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }
            }
            else
            {
                modifiedBatchKey.layeredTransparentSideSourceLight = sideEntity.ParentLevel.Lights[sideEntity.NativeObject.TransparentLightsourceIndex];
            }

            BatchKey = modifiedBatchKey;
        }

        public override void ApplyMedia()
        {
            throw new NotImplementedException("Sides do not have media surfaces - this code should be unreachable.");
        }

        public override void ApplyBatchKeyMaterial(bool innerLayer)
        {
            DecrementTextureUsage();

            var modifiedBatchKey = BatchKey;

            if (sideEntity.NativeObject == null)
            {
                modifiedBatchKey.sourceMaterial =
                    MaterialGeneration_Geometry.GetMaterial(ShapeDescriptor.Empty,
                                                            transferMode: 0,
                                                            isOpaqueSurface: true,
                                                            MaterialGeneration_Geometry.SurfaceTypes.Normal,
                                                            incrementUsageCounter: false);
            }
            else if (innerLayer)
            {
                switch (dataSource)
                {
                    case LevelEntity_Side.DataSources.Primary:
                        modifiedBatchKey.sourceMaterial =
                            MaterialGeneration_Geometry.GetMaterial(sideEntity.NativeObject.Primary.Texture,
                                                                    sideEntity.NativeObject.PrimaryTransferMode,
                                                                    sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                                                                    MaterialGeneration_Geometry.SurfaceTypes.Normal,
                                                                    incrementUsageCounter: true);

                        break;

                    case LevelEntity_Side.DataSources.Secondary:
                        modifiedBatchKey.sourceMaterial =
                            MaterialGeneration_Geometry.GetMaterial(sideEntity.NativeObject.Secondary.Texture,
                                                                    sideEntity.NativeObject.SecondaryTransferMode,
                                                                    sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                                                                    MaterialGeneration_Geometry.SurfaceTypes.Normal,
                                                                    incrementUsageCounter: true);

                        break;

                    case LevelEntity_Side.DataSources.Transparent:
                        modifiedBatchKey.sourceMaterial =
                            MaterialGeneration_Geometry.GetMaterial(sideEntity.NativeObject.Transparent.Texture,
                                                                    sideEntity.NativeObject.TransparentTransferMode,
                                                                    sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                                                                    MaterialGeneration_Geometry.SurfaceTypes.Normal,
                                                                    incrementUsageCounter: true);

                        break;

                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }
            }
            else
            {
                modifiedBatchKey.layeredTransparentSideSourceMaterial =
                    MaterialGeneration_Geometry.GetMaterial(sideEntity.NativeObject.Transparent.Texture,
                                                            sideEntity.NativeObject.TransparentTransferMode,
                                                            sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                                                            MaterialGeneration_Geometry.SurfaceTypes.LayeredTransparentOuter,
                                                            incrementUsageCounter: true);
            }

            BatchKey = modifiedBatchKey;
        }

        public override void PrepareForDestruction()
        {
            DecrementTextureUsage();
        }

        protected override void ApplyInteractiveSurface()
        {
            var sideSurface = SurfaceRenderer.gameObject.AddComponent<EditableSurface_Side>();
            sideSurface.ParentSide = sideEntity;
            sideSurface.DataSource = dataSource;
            sideSurface.Platform = platformConstraint != null ? platformConstraint.Parent.GetComponent<LevelEntity_Platform>() : null;

            var line = sideEntity.ParentLevel.Level.Lines[sideEntity.ParentLineIndex];

            var facingPolygonIndex = sideEntity.IsClockwise ? line.ClockwisePolygonOwner : line.CounterclockwisePolygonOwner;

            var mediaIndex = sideEntity.ParentLevel.Level.Polygons[facingPolygonIndex].MediaIndex;
            sideSurface.Media = mediaIndex >= 0 ? sideEntity.ParentLevel.Medias[mediaIndex] : null;

            if (sideEntity.NativeObject == null)
            {
                sideSurface.surfaceShapeDescriptor = ShapeDescriptor.Empty;
                sideSurface.RuntimeLight = null;
            }
            else
            {
                switch (dataSource)
                {
                    case LevelEntity_Side.DataSources.Primary:
                        sideSurface.surfaceShapeDescriptor = sideEntity.NativeObject.Primary.Texture;
                        sideSurface.RuntimeLight = sideEntity.ParentLevel.Lights[sideEntity.NativeObject.PrimaryLightsourceIndex];
                        break;

                    case LevelEntity_Side.DataSources.Secondary:
                        sideSurface.surfaceShapeDescriptor = sideEntity.NativeObject.Secondary.Texture;
                        sideSurface.RuntimeLight = sideEntity.ParentLevel.Lights[sideEntity.NativeObject.SecondaryLightsourceIndex];
                        break;

                    case LevelEntity_Side.DataSources.Transparent:
                        sideSurface.surfaceShapeDescriptor = sideEntity.NativeObject.Transparent.Texture;
                        sideSurface.RuntimeLight = sideEntity.ParentLevel.Lights[sideEntity.NativeObject.TransparentLightsourceIndex];
                        break;

                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }
            }

            sideEntity.ParentLevel.EditableSurface_Sides.Add(sideSurface);

            SurfaceRenderer.gameObject.AddComponent<MeshCollider>();
        }

        private void ConstrainSurfaceToPlatform(LevelEntity_Platform platform, bool constrainAbovePlatform)
        {
            // If editing isn't possible, then the surface can just be hierarchically contstrained
#if NO_EDITING
            SurfaceRenderer.transform.SetParent(platform.transform);

            if (constrainAbovePlatform)
            {
                SurfaceRenderer.transform.localPosition = new Vector3(
                    0f,
                    (HighElevation - LowElevation) / GeometryUtilities.WorldUnitIncrementsPerMeter,
                    0f);
            }
            else
            {
                SurfaceRenderer.transform.localPosition = Vector3.zero;
            }
#else
            var constraint = SurfaceRenderer.gameObject.AddComponent<PlatformConstraint>();
            constraint.Parent = platform.transform;

            if (constrainAbovePlatform)
            {
                constraint.WorldOffsetFromParent = new Vector3(
                    0f,
                    (HighElevation - LowElevation) / GeometryUtilities.WorldUnitIncrementsPerMeter,
                    0f);
            }
            else
            {
                constraint.WorldOffsetFromParent = Vector3.zero;
            }

            constraint.ApplyConstraint();

            platformConstraint = constraint;

            platform.BeginRuntimeStyleBehavior();
#endif
        }

        private Vector2[] BuildUVs(short textureOffsetX, short textureOffsetY)
        {
            var uvs = new Vector2[4];

            if (sideEntity.NativeObject == null)
            {
                uvs[0] = Vector2.zero;
                uvs[1] = Vector2.zero;
                uvs[2] = Vector2.zero;
                uvs[3] = Vector2.zero;
            }
            else
            {
                var lineLength = sideEntity.ParentLevel.Level.Lines[sideEntity.NativeObject.LineIndex].Length / GeometryUtilities.WorldUnitIncrementsPerWorldUnit;
                var bottomPosition = (LowElevation - HighElevation) / GeometryUtilities.WorldUnitIncrementsPerWorldUnit;

                var offset = new Vector2(textureOffsetX, -textureOffsetY) / GeometryUtilities.WorldUnitIncrementsPerWorldUnit;

                uvs[0] = new Vector2(0f, bottomPosition) + offset;
                uvs[1] = Vector2.zero + offset;
                uvs[2] = new Vector2(lineLength, 0f) + offset;
                uvs[3] = new Vector2(lineLength, bottomPosition) + offset;
            }

            return uvs;
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
                    return new Color(-1f / 8f, 0f, 0f, 0f);
                case TransferModes.HorizontalSlideFast: // Horizontal Slide Fast
                    return new Color(-2f / 8f, 0f, 0f, 0f);
                case TransferModes.VerticalSlide: // Vertical Slide
                    return new Color(0f, 1f / 8f, 0f, 0f);
                case TransferModes.VerticalSlideFast: // Vertical Slide Fast
                    return new Color(0f, 2f / 8f, 0f, 0f);
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
            if (sideEntity.NativeObject == null)
            {
                return;
            }

            switch (dataSource)
            {
                case LevelEntity_Side.DataSources.Primary:
                    MaterialGeneration_Geometry.DecrementTextureUsage(sideEntity.NativeObject.Primary.Texture);
                    break;
                case LevelEntity_Side.DataSources.Secondary:
                    MaterialGeneration_Geometry.DecrementTextureUsage(sideEntity.NativeObject.Secondary.Texture);
                    break;
                case LevelEntity_Side.DataSources.Transparent:
                    MaterialGeneration_Geometry.DecrementTextureUsage(sideEntity.NativeObject.Transparent.Texture);
                    break;
                default:
                    throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
            }
        }
    }
}
