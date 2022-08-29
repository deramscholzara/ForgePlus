using ForgePlus.Entities.Geometry;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Constraints;
using RuntimeCore.Materials;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Weland;
using Weland.Extensions;

namespace RuntimeCore.Entities.Geometry
{
    public class RuntimeSurfaceGeometryModule_Side : RuntimeSurfaceGeometryModule_Base
    {
        private int lastLayeredTransparentSideTextureIndex;
        private int lastLayeredTransparentSideLightIndex;
        
        private readonly LevelEntity_Side sideEntity;
        private readonly LevelEntity_Side.DataSources dataSource;

        private PlatformConstraint platformConstraint;
        private short textureOffsetFromFacingCeilingPlatform = 0;

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
            UnityEngine.Object.Destroy(platformConstraint);

            IsStaticBatchable = true;

            if (sideEntity.NativeObject.HasLayeredTransparentSide(sideEntity.ParentLevel.Level))
            {
                // Note: Layered transparent sides have no opposing platform to
                //       attach to, because they have no opposing polygon.
                return;
            }

            var line = sideEntity.ParentLevel.Level.Lines[sideEntity.ParentLineIndex];

            var facingPolygonIndex = sideEntity.IsClockwise ? line.ClockwisePolygonOwner : line.CounterclockwisePolygonOwner;
            var facingPolygon = sideEntity.ParentLevel.Level.Polygons[facingPolygonIndex];

            if (facingPolygon.Type == PolygonType.Platform)
            {
                var facingPlatformIndex = facingPolygon.Permutation;
                if (facingPlatformIndex >= 0 &&
                    sideEntity.ParentLevel.CeilingPlatforms.ContainsKey(facingPlatformIndex))
                {
                    var facingPlatform = sideEntity.ParentLevel.CeilingPlatforms[facingPlatformIndex];
                    var facingPlatformLowHeight = facingPlatform.NativeObject.RuntimeMinimumHeight(sideEntity.ParentLevel.Level);

                    // If the top of this Side Surface extends higher than the initial state of the Platform it is facing,
                    // then the UVs need to be shifted down, as they should aligh to top-left of the initial visible area.
                    // So this offset is determined here, then used later in the ApplyTextureOffset method.
                    if (facingPlatform.NativeObject.InitiallyExtended &&
                        HighElevation > facingPlatformLowHeight)
                    {
                        textureOffsetFromFacingCeilingPlatform = (short)(HighElevation - facingPlatformLowHeight);

                        if (textureOffsetFromFacingCeilingPlatform < 0)
                        {
                            textureOffsetFromFacingCeilingPlatform = 0;
                        }
                    }
                }
            }

            var opposingPolygonIndex = sideEntity.IsClockwise ? line.CounterclockwisePolygonOwner : line.ClockwisePolygonOwner;
            if (opposingPolygonIndex < 0)
            {
                return;
            }

            var opposingPolygon = sideEntity.ParentLevel.Level.Polygons[opposingPolygonIndex];
            if (opposingPolygon.Type != PolygonType.Platform)
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
                     (dataSource == LevelEntity_Side.DataSources.Secondary ||
                       (dataSource == LevelEntity_Side.DataSources.Primary &&
                        opposingPolygon.CeilingHeight >= facingPolygon.CeilingHeight)))
            {
                var opposingFloorPlatform = sideEntity.ParentLevel.FloorPlatforms[opposingPlatformIndex];
                ConstrainSurfaceToPlatform(opposingFloorPlatform, constrainAbovePlatform: false);

                IsStaticBatchable = false;
            }
        }

        public override void ApplyTextureOffset(bool innerLayer)
        {
            Vector4[] UVs;

            var lastLight = innerLayer ? lastLightIndex : lastLayeredTransparentSideLightIndex;
            var lastTexture = innerLayer ? lastTextureIndex : lastLayeredTransparentSideTextureIndex;

            if (sideEntity.NativeObject == null)
            {
                UVs = BuildUVs(0, 0, lastLight, lastTexture);

                SurfaceMesh.SetUVs(channel: 0, UVs);
            }
            else if (innerLayer)
            {
                switch (dataSource)
                {
                    case LevelEntity_Side.DataSources.Primary:
                        UVs = BuildUVs(sideEntity.NativeObject.Primary.X, (short)(sideEntity.NativeObject.Primary.Y - textureOffsetFromFacingCeilingPlatform), lastLight, lastTexture);
                        break;

                    case LevelEntity_Side.DataSources.Secondary:
                        UVs = BuildUVs(sideEntity.NativeObject.Secondary.X, (short)(sideEntity.NativeObject.Secondary.Y - textureOffsetFromFacingCeilingPlatform), lastLight, lastTexture);
                        break;

                    case LevelEntity_Side.DataSources.Transparent:
                        UVs = BuildUVs(sideEntity.NativeObject.Transparent.X, (short)(sideEntity.NativeObject.Transparent.Y - textureOffsetFromFacingCeilingPlatform), lastLight, lastTexture);
                        break;

                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }

                SurfaceMesh.SetUVs(channel: 0, UVs);
            }
            else
            {
                UVs = BuildUVs(sideEntity.NativeObject.Transparent.X, (short)(sideEntity.NativeObject.Transparent.Y + textureOffsetFromFacingCeilingPlatform), lastLight, lastTexture);

                SurfaceMesh.SetUVs(channel: 1, UVs);
            }
        }

        public override void ApplyTransferMode(bool innerLayer)
        {
            if (sideEntity.NativeObject == null)
            {
                var vertexColors = new Color[4];
                for (var i = 0; i < 4; i++)
                {
                    vertexColors[i] = Color.black;
                }

                SurfaceMesh.SetColors(vertexColors);
                
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

                var uv2 = new Vector4[4];

                for (var i = 0; i < 4; i++)
                {
                    uv2[i].x = vertexColor.r;
                    uv2[i].y = vertexColor.g;
                    uv2[i].z = vertexColor.b;
                    uv2[i].w = vertexColor.a;
                }

                SurfaceMesh.SetUVs(channel: 2, uvs: uv2);
            }
        }

        public override void ApplyLight(bool innerLayer)
        {
            var modifiedBatchKey = BatchKey;

            if (sideEntity.NativeObject == null)
            {
                modifiedBatchKey.SourceLight = null;
                modifiedBatchKey.LayeredTransparentSideSourceLight = null;
            }
            else if (innerLayer)
            {
                switch (dataSource)
                {
                    case LevelEntity_Side.DataSources.Primary:
                        modifiedBatchKey.SourceLight = sideEntity.ParentLevel.Lights[sideEntity.NativeObject.PrimaryLightsourceIndex];
                        lastLightIndex = sideEntity.NativeObject.PrimaryLightsourceIndex;
                        break;

                    case LevelEntity_Side.DataSources.Secondary:
                        modifiedBatchKey.SourceLight = sideEntity.ParentLevel.Lights[sideEntity.NativeObject.SecondaryLightsourceIndex];
                        lastLightIndex = sideEntity.NativeObject.SecondaryLightsourceIndex;
                        break;

                    case LevelEntity_Side.DataSources.Transparent:
                        modifiedBatchKey.SourceLight = sideEntity.ParentLevel.Lights[sideEntity.NativeObject.TransparentLightsourceIndex];
                        lastLightIndex = sideEntity.NativeObject.TransparentLightsourceIndex;
                        break;

                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }
            }
            else
            {
                modifiedBatchKey.LayeredTransparentSideSourceLight = sideEntity.ParentLevel.Lights[sideEntity.NativeObject.TransparentLightsourceIndex];
                lastLayeredTransparentSideLightIndex = sideEntity.NativeObject.TransparentLightsourceIndex;
            }
            
            if (innerLayer)
            {
                var UVs = SurfaceMesh.uv.Select(uv => new Vector4(uv.x, uv.y, lastLightIndex, lastTextureIndex)).ToArray();
                SurfaceMesh.SetUVs(0, UVs);
            }
            else
            {
                var UVs = SurfaceMesh.uv.Select(uv => new Vector4(uv.x, uv.y, lastLayeredTransparentSideLightIndex, lastLayeredTransparentSideTextureIndex)).ToArray();
                SurfaceMesh.SetUVs(1, UVs);
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
                modifiedBatchKey.SourceMaterial =
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
#if USE_TEXTURE_ARRAYS
                        modifiedBatchKey.SourceShapeDescriptor = sideEntity.NativeObject.Primary.Texture;
#endif
                        modifiedBatchKey.SourceMaterial =
                            MaterialGeneration_Geometry.GetMaterial(sideEntity.NativeObject.Primary.Texture,
                                                                    sideEntity.NativeObject.PrimaryTransferMode,
                                                                    sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                                                                    MaterialGeneration_Geometry.SurfaceTypes.Normal,
                                                                    incrementUsageCounter: true);
            
#if USE_TEXTURE_ARRAYS
                        lastTextureIndex = MaterialGeneration_Geometry.GetTextureArrayIndex(
                            sideEntity.NativeObject.Primary.Texture,
                            sideEntity.NativeObject.PrimaryTransferMode,
                            sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                            MaterialGeneration_Geometry.SurfaceTypes.Normal);
#endif

                        break;

                    case LevelEntity_Side.DataSources.Secondary:
#if USE_TEXTURE_ARRAYS
                        modifiedBatchKey.SourceShapeDescriptor = sideEntity.NativeObject.Secondary.Texture;
#endif
                        modifiedBatchKey.SourceMaterial =
                            MaterialGeneration_Geometry.GetMaterial(sideEntity.NativeObject.Secondary.Texture,
                                                                    sideEntity.NativeObject.SecondaryTransferMode,
                                                                    sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                                                                    MaterialGeneration_Geometry.SurfaceTypes.Normal,
                                                                    incrementUsageCounter: true);
            
#if USE_TEXTURE_ARRAYS
                        lastTextureIndex = MaterialGeneration_Geometry.GetTextureArrayIndex(
                            sideEntity.NativeObject.Secondary.Texture,
                            sideEntity.NativeObject.SecondaryTransferMode,
                            sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                            MaterialGeneration_Geometry.SurfaceTypes.Normal);
#endif

                        break;

                    case LevelEntity_Side.DataSources.Transparent:
                        
#if USE_TEXTURE_ARRAYS
                        modifiedBatchKey.SourceShapeDescriptor = sideEntity.NativeObject.Transparent.Texture;
#endif
                        modifiedBatchKey.SourceMaterial =
                            MaterialGeneration_Geometry.GetMaterial(sideEntity.NativeObject.Transparent.Texture,
                                                                    sideEntity.NativeObject.TransparentTransferMode,
                                                                    sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                                                                    MaterialGeneration_Geometry.SurfaceTypes.Normal,
                                                                    incrementUsageCounter: true);
            
#if USE_TEXTURE_ARRAYS
                        lastTextureIndex = MaterialGeneration_Geometry.GetTextureArrayIndex(
                            sideEntity.NativeObject.Transparent.Texture,
                            sideEntity.NativeObject.TransparentTransferMode,
                            sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                            MaterialGeneration_Geometry.SurfaceTypes.Normal);
#endif

                        break;

                    default:
                        throw new NotImplementedException($"DataSource '{dataSource}' is not implemented.");
                }
            }
            else
            {
#if USE_TEXTURE_ARRAYS
                modifiedBatchKey.LayeredTransparentSideShapeDescriptor = sideEntity.NativeObject.Transparent.Texture;
#endif
                modifiedBatchKey.LayeredTransparentSideSourceMaterial =
                    MaterialGeneration_Geometry.GetMaterial(sideEntity.NativeObject.Transparent.Texture,
                                                            sideEntity.NativeObject.TransparentTransferMode,
                                                            sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                                                            MaterialGeneration_Geometry.SurfaceTypes.LayeredTransparentOuter,
                                                            incrementUsageCounter: true);
            
#if USE_TEXTURE_ARRAYS
                lastLayeredTransparentSideTextureIndex = MaterialGeneration_Geometry.GetTextureArrayIndex(
                    sideEntity.NativeObject.Transparent.Texture,
                    sideEntity.NativeObject.TransparentTransferMode,
                    sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                    MaterialGeneration_Geometry.SurfaceTypes.LayeredTransparentOuter);
#endif
            }
            
            if (innerLayer)
            {
                var UVs = SurfaceMesh.uv.Select(uv => new Vector4(uv.x, uv.y, lastLightIndex, lastTextureIndex)).ToArray();
                SurfaceMesh.SetUVs(0, UVs);
            }
            else
            {
                var UVs = SurfaceMesh.uv.Select(uv => new Vector4(uv.x, uv.y, lastLayeredTransparentSideLightIndex, lastLayeredTransparentSideTextureIndex)).ToArray();
                SurfaceMesh.SetUVs(1, UVs);
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
#endif
        }

        private Vector4[] BuildUVs(short textureOffsetX, short textureOffsetY, int lastLight, int lastTexture)
        {
            var meshUVs = new Vector4[4];

            if (sideEntity.NativeObject == null)
            {
                meshUVs[0] = new Vector4(0f, 0f, lastLight, lastTexture);
                meshUVs[1] = new Vector4(0f, 0f, lastLight, lastTexture);
                meshUVs[2] = new Vector4(0f, 0f, lastLight, lastTexture);
                meshUVs[3] = new Vector4(0f, 0f, lastLight, lastTexture);
            }
            else
            {
                var lineLength = sideEntity.ParentLevel.Level.Lines[sideEntity.NativeObject.LineIndex].Length / GeometryUtilities.WorldUnitIncrementsPerWorldUnit;
                var bottomPosition = (LowElevation - HighElevation) / GeometryUtilities.WorldUnitIncrementsPerWorldUnit;

                var offset = new Vector4(textureOffsetX, -textureOffsetY, 0f, 0f) / GeometryUtilities.WorldUnitIncrementsPerWorldUnit;

                meshUVs[0] = new Vector4(0f, bottomPosition, lastLight, lastTexture) + offset;
                meshUVs[1] = new Vector4(0f, 0f, lastLight, lastTexture) + offset;
                meshUVs[2] = new Vector4(lineLength, 0f, lastLight, lastTexture) + offset;
                meshUVs[3] = new Vector4(lineLength, bottomPosition, lastLight, lastTexture) + offset;
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
