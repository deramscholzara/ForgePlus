using ForgePlus.Entities.Geometry;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Constraints;
using RuntimeCore.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Rendering;
using AlephOne;
using ForgePlus.Extensions;

namespace RuntimeCore.Entities.Geometry
{
    [NoAutoStaticsCleanup]
    public class RuntimeSurfaceGeometryModule_Side : RuntimeSurfaceGeometryModule_Base
    {
        // Initial heights are those the level's texture offsets are relative to
        private enum PlatformHeights
        {
            Current,
            Initial,
            Open,
        }

        // In world unit increments
        private const float MinimumClippedHeight = 0.5f;

        // Null sides have no texture offsets
        private static readonly side_texture_definition NullSideTexture = new side_texture_definition();

        // Reused for every side's mesh updates, as the mesh copies them (so clipping every frame doesn't allocate)
        private static readonly Vector3[] ScratchPositions = new Vector3[4];
        private static readonly Vector2[] ScratchUVs = new Vector2[4];

        // Sides smaller than this (in square world units) are left out of Surface Cache Global Illumination
        private const float MinimumSurfaceCacheArea = 0.1f;

        private readonly LevelEntity_Side sideEntity;
        private readonly LevelEntity_Side.DataSources dataSource;
        private readonly LevelEntity_Side.Sections section;

        private PlatformConstraint platformConstraint;

        private PlatformSideClipping platformSideClipping;
        private MeshCollider surfaceCollider;
        private RuntimeSurfaceGeometry surface;
        private Vector3[] fullSizePositions;
        private bool platformClippingIsApplied;
        private bool lastClippedVisibility;
        private float lastClippedBottom;
        private float lastClippedTop;
        private float lastClippedTextureAnchor;

        public bool IsShownByPlatformClipping
        {
            get
            {
                return !platformClippingIsApplied || lastClippedVisibility;
            }
        }

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

        private line_data Line
        {
            get
            {
                return map.get_line_data(sideEntity.ParentLevel.Level, sideEntity.ParentLineIndex);
            }
        }

        // The polygon the side faces into, which it's drawn with
        public override short OwnerPolygonIndex
        {
            get
            {
                return Line.GetPolygonOwner(sideEntity.IsClockwise);
            }
        }

        private short FacingPolygonIndex
        {
            get
            {
                return Line.GetPolygonOwner(sideEntity.IsClockwise);
            }
        }

        private short OpposingPolygonIndex
        {
            get
            {
                return Line.GetPolygonOwner(!sideEntity.IsClockwise);
            }
        }

        private bool FacingPolygonIsPlatform
        {
            get
            {
                var level = sideEntity.ParentLevel.Level;
                return map.get_polygon_data(level, FacingPolygonIndex).GetPlatform(level) != null;
            }
        }

        // A middle surface is a full side when there's nothing on the other side,
        // otherwise it's the transparent texture across the opening
        private bool UsesFullSideHeights
        {
            get
            {
                return OpposingPolygonIndex < 0 ||
                       (sideEntity.NativeObject != null && sideEntity.NativeObject.type == map._full_side);
            }
        }

        public RuntimeSurfaceGeometryModule_Side(
            LevelEntity_Side sideEntity,
            LevelEntity_Side.DataSources dataSource,
            LevelEntity_Side.Sections section,
            Mesh surfaceMesh,
            MeshRenderer surfaceRenderer) : base()
        {
            this.sideEntity = sideEntity;
            this.dataSource = dataSource;
            this.section = section;
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
            ApplySourceMaterial(innerLayer: true);

            // Tangents follow the UVs, which didn't exist yet when ApplyPositionsAndTriangles calculated them
            SurfaceMesh.RecalculateTangents(MeshUpdateFlags.DontNotifyMeshUsers |
                                            MeshUpdateFlags.DontRecalculateBounds |
                                            MeshUpdateFlags.DontResetBoneBounds);

            if (sideEntity.NativeObject.HasLayeredTransparentSide(sideEntity.ParentLevel.Level))
            {
                ApplyTextureOffset(innerLayer: false);
                ApplyTransferMode(innerLayer: false);
                ApplyLight(innerLayer: false);
                ApplySourceMaterial(innerLayer: false);
            }

            ApplyAmbientDelta();

            ApplyRendererMaterials();

            ApplyInteractiveSurface();
        }

        public override void ApplyPositionsAndTriangles()
        {
            var line = Line;

            var endpointIndexA = sideEntity.IsClockwise ? line.endpoint_indexes[0] : line.endpoint_indexes[1];
            var endpointIndexB = sideEntity.IsClockwise ? line.endpoint_indexes[1] : line.endpoint_indexes[0];

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

            fullSizePositions = positions;

            // Clipping must be reapplied to the new geometry
            InvalidatePlatformClipping();
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
            platformConstraint = null;

            if (platformSideClipping)
            {
                UnityEngine.Object.Destroy(platformSideClipping);
                platformSideClipping = null;

                SetClippedAway(false);
            }

            if (sideEntity.NativeObject.HasLayeredTransparentSide(sideEntity.ParentLevel.Level))
            {
                // Note: Layered transparent sides have no opposing platform to
                //       attach to, because they have no opposing polygon.
                return;
            }

            var opposingPolygonIndex = OpposingPolygonIndex;
            var level = sideEntity.ParentLevel.Level;
            var opposingPolygon = opposingPolygonIndex >= 0 ? map.get_polygon_data(level, opposingPolygonIndex) : null;
            var opposingPolygonIsPlatform = opposingPolygon != null && opposingPolygon.GetPlatform(level) != null;

            if (FacingPolygonIsPlatform)
            {
                if (!opposingPolygonIsPlatform)
                {
                    // A platform's own sides (such as a door's frame) always cover its whole travel
                    return;
                }

                // Between two platforms, what's drawn depends on both of their heights (nothing while they're level)
                AddPlatformSideClipping();
                return;
            }

            if (!opposingPolygonIsPlatform)
            {
                return;
            }

            var opposingPlatformIndex = opposingPolygon.permutation;
            sideEntity.ParentLevel.CeilingPlatforms.TryGetValue(opposingPlatformIndex, out var opposingCeilingPlatform);
            sideEntity.ParentLevel.FloorPlatforms.TryGetValue(opposingPlatformIndex, out var opposingFloorPlatform);

            // Follow the platform surface that bounds this surface in Aleph One
            switch (section)
            {
                case LevelEntity_Side.Sections.Top:
                    if (!opposingCeilingPlatform)
                    {
                        return;
                    }

                    ConstrainSurfaceToPlatform(opposingCeilingPlatform, constrainAbovePlatform: true);
                    break;

                case LevelEntity_Side.Sections.Middle:
                    if (UsesFullSideHeights)
                    {
                        return;
                    }

                    if (opposingCeilingPlatform)
                    {
                        ConstrainSurfaceToPlatform(opposingCeilingPlatform, constrainAbovePlatform: false);
                    }
                    else if (!opposingFloorPlatform)
                    {
                        return;
                    }

                    break;

                case LevelEntity_Side.Sections.Bottom:
                    if (!opposingFloorPlatform)
                    {
                        return;
                    }

                    ConstrainSurfaceToPlatform(opposingFloorPlatform, constrainAbovePlatform: false);
                    break;

                default:
                    throw new NotImplementedException($"Section '{section}' is not implemented.");
            }

            AddPlatformSideClipping();
        }

        private void AddPlatformSideClipping()
        {
            platformSideClipping = SurfaceRenderer.gameObject.AddComponent<PlatformSideClipping>();
            platformSideClipping.Module = this;
            platformSideClipping.Watch(GetPlatforms(FacingPolygonIndex).Concat(GetPlatforms(OpposingPolygonIndex)));
            platformClippingIsApplied = false;
        }

        // The platforms of a polygon (one, the other, or both of its floor and ceiling), if it's a platform
        private IEnumerable<LevelEntity_Platform> GetPlatforms(short polygonIndex)
        {
            if (polygonIndex < 0)
            {
                yield break;
            }

            var level = sideEntity.ParentLevel;
            var polygon = map.get_polygon_data(level.Level, polygonIndex);
            if (polygon.GetPlatform(level.Level) == null)
            {
                yield break;
            }

            if (level.FloorPlatforms.TryGetValue(polygon.permutation, out var floorPlatform))
            {
                yield return floorPlatform;
            }

            if (level.CeilingPlatforms.TryGetValue(polygon.permutation, out var ceilingPlatform))
            {
                yield return ceilingPlatform;
            }
        }

        // Hidden while platforms leave none of it to draw (by the surface's visibility, alongside its kind and culling)
        private void SetClippedAway(bool isClippedAway)
        {
            if (!surface)
            {
                surface = SurfaceRenderer.GetComponent<RuntimeSurfaceGeometry>();
            }

            if (surface)
            {
                surface.SetClippedAway(isClippedAway);
            }
            else
            {
                // Without editing, the surface's component is gone once it's assembled, and nothing else hides it
                SurfaceRenderer.forceRenderingOff = isClippedAway;
            }
        }

        // Its geometry changed, so it's trimmed again this frame
        private void InvalidatePlatformClipping()
        {
            platformClippingIsApplied = false;

            if (platformSideClipping)
            {
                platformSideClipping.RequestUpdate();
            }
        }

        // Fits the surface to what Aleph One would draw for the platforms' current heights
        public void UpdatePlatformClipping(bool clippingEnabled)
        {
            if (fullSizePositions == null)
            {
                return;
            }

            GetAlephOneSurface(PlatformHeights.Current, PlatformHeights.Current, innerLayer: true, out var windowBottom, out var windowTop, out var textureAnchor);

            var transformHeight = SurfaceRenderer.transform.position.y * GeometryUtilities.WorldUnitIncrementsPerMeter;

            float bottom;
            float top;
            bool isVisible;

            if (clippingEnabled)
            {
                bottom = windowBottom - transformHeight;
                top = windowTop - transformHeight;
                isVisible = windowTop - windowBottom >= MinimumClippedHeight;
            }
            else
            {
                bottom = LowElevation - HighElevation;
                top = 0f;
                isVisible = true;
            }

            textureAnchor -= transformHeight;

            if (platformClippingIsApplied &&
                isVisible == lastClippedVisibility &&
                (!isVisible ||
                 (bottom == lastClippedBottom &&
                  top == lastClippedTop &&
                  textureAnchor == lastClippedTextureAnchor)))
            {
                return;
            }

            platformClippingIsApplied = true;
            lastClippedVisibility = isVisible;
            lastClippedBottom = bottom;
            lastClippedTop = top;
            lastClippedTextureAnchor = textureAnchor;

            if (!surfaceCollider)
            {
                surfaceCollider = SurfaceRenderer.GetComponent<MeshCollider>();
            }

            SetClippedAway(!isVisible);

            if (surfaceCollider)
            {
                surfaceCollider.enabled = isVisible;
            }

            if (!isVisible)
            {
                return;
            }

            var positions = ScratchPositions;
            for (var i = 0; i < positions.Length; i++)
            {
                positions[i] = fullSizePositions[i];
            }

            // Vertex order: bottom-left, top-left, top-right, bottom-right
            positions[0].y = positions[3].y = bottom / GeometryUtilities.WorldUnitIncrementsPerMeter;
            positions[1].y = positions[2].y = top / GeometryUtilities.WorldUnitIncrementsPerMeter;

            SurfaceMesh.SetVertices(positions);
            SurfaceMesh.SetUVs(channel: 0, BuildUVs(GetTexture(innerLayer: true).x0, textureAnchor, bottom, top));
            SurfaceMesh.RecalculateBounds();

            if (surfaceCollider)
            {
                PlatformSideColliders.Reshape(surfaceCollider, SurfaceMesh);
            }
        }

        public override void ApplyTextureOffset(bool innerLayer)
        {
            Vector2[] UVs;

            var bottom = (float)(LowElevation - HighElevation);

            if (sideEntity.NativeObject == null)
            {
                UVs = BuildUVs(0, 0f, bottom, 0f);

                SurfaceMesh.SetUVs(channel: 0, UVs);
            }
            else
            {
                // A platform's own sides are aligned as when it's open (when they can be seen), and other sides
                // as with the platforms where the level starts them (UpdatePlatformClipping keeps them aligned)
                GetAlephOneSurface(FacingPolygonIsPlatform ? PlatformHeights.Open : PlatformHeights.Initial,
                                   PlatformHeights.Initial,
                                   innerLayer,
                                   out _,
                                   out _,
                                   out var textureAnchor);

                // Relative to the top of the surface, where its transform is
                textureAnchor -= HighElevation;

                UVs = BuildUVs(GetTexture(innerLayer).x0, textureAnchor, bottom, 0f);

                SurfaceMesh.SetUVs(channel: innerLayer ? 0 : 1, UVs);
            }

            if (innerLayer)
            {
                InvalidatePlatformClipping();
            }
        }

        public override void ApplyTransferMode(bool innerLayer)
        {
            var modifiedMaterialKey = MaterialKey;

            if (sideEntity.NativeObject == null)
            {
                modifiedMaterialKey.TransferMode = default;
            }
            else if (innerLayer)
            {
                modifiedMaterialKey.TransferMode = TransferModeLook.For(sideEntity.NativeObject.GetTransferMode(dataSource));
            }
            else
            {
                modifiedMaterialKey.LayeredTransparentSideTransferMode = TransferModeLook.For(sideEntity.NativeObject.transparent_transfer_mode);
            }

            MaterialKey = modifiedMaterialKey;
        }

        // Its materials hold the light's intensity (a layered side's outer layer's material, its transparent light's)
        public override void ApplyLight(bool innerLayer)
        {
            if (sideEntity.NativeObject == null)
            {
                return;
            }

            var modifiedMaterialKey = MaterialKey;

            if (innerLayer)
            {
                modifiedMaterialKey.SourceLight = GetLight(sideEntity.ParentLevel, sideEntity.NativeObject.GetLightsourceIndex(dataSource));
            }
            else
            {
                modifiedMaterialKey.LayeredTransparentSideSourceLight = GetLight(sideEntity.ParentLevel, sideEntity.NativeObject.transparent_lightsource_index);
            }

            MaterialKey = modifiedMaterialKey;
        }

        // Applies to both layers of a layered transparent side. Aleph One pins the lit result to between dark and full
        // (render.c: render_node_side), so a delta past full light either way is clamped here.
        public void ApplyAmbientDelta()
        {
            var ambientDelta = sideEntity.NativeObject == null ?
                               0f :
                               Mathf.Clamp((float)sideEntity.NativeObject.ambient_delta / cstypes.FIXED_ONE, -1f, 1f);

            var uv4 = ScratchUVs;
            for (var i = 0; i < uv4.Length; i++)
            {
                uv4[i] = new Vector4(ambientDelta, 0f, 0f, 0f);
            }

            SurfaceMesh.SetUVs(AmbientDeltaUVChannel, uv4);
        }

        public override void ApplyMedia()
        {
            throw new NotImplementedException("Sides do not have media surfaces - this code should be unreachable.");
        }

        // Sides too small to matter (by their full size, before platforms clip them) are left out of Surface Cache Global
        // Illumination too
        protected override bool IsExcludedFromSurfaceCache()
        {
            return base.IsExcludedFromSurfaceCache() || GetFullSizeArea() < MinimumSurfaceCacheArea;
        }

        // In square world units
        private float GetFullSizeArea()
        {
            if (fullSizePositions == null)
            {
                return 0f;
            }

            // Vertex order: bottom-left, top-left, top-right, bottom-right
            var metersPerWorldUnit = GeometryUtilities.WorldUnitIncrementsPerWorldUnit / GeometryUtilities.WorldUnitIncrementsPerMeter;
            var width = Vector3.Distance(fullSizePositions[0], fullSizePositions[3]) / metersPerWorldUnit;
            var height = Vector3.Distance(fullSizePositions[0], fullSizePositions[1]) / metersPerWorldUnit;

            return width * height;
        }

        public override void ApplySourceMaterial(bool innerLayer)
        {
            DecrementTextureUsage();

            var modifiedMaterialKey = MaterialKey;

            if (sideEntity.NativeObject == null)
            {
                modifiedMaterialKey.SourceMaterial =
                    MaterialGeneration_Geometry.GetMaterial(cstypes.UNONE,
                                                            transferMode: 0,
                                                            isOpaqueSurface: true,
                                                            MaterialGeneration_Geometry.SurfaceTypes.Normal,
                                                            incrementUsageCounter: false);
            }
            else if (innerLayer)
            {
                var shapeDescriptor = sideEntity.NativeObject.GetTexture(dataSource).texture;
                var transferMode = sideEntity.NativeObject.GetTransferMode(dataSource);

                modifiedMaterialKey.SourceMaterial =
                    MaterialGeneration_Geometry.GetMaterial(shapeDescriptor,
                                                            transferMode,
                                                            sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                                                            MaterialGeneration_Geometry.SurfaceTypes.Normal,
                                                            incrementUsageCounter: true);

            }
            else
            {
                modifiedMaterialKey.LayeredTransparentSideSourceMaterial =
                    MaterialGeneration_Geometry.GetMaterial(sideEntity.NativeObject.transparent_texture.texture,
                                                            sideEntity.NativeObject.transparent_transfer_mode,
                                                            sideEntity.NativeObject.SurfaceShouldBeOpaque(dataSource, sideEntity.ParentLevel.Level),
                                                            MaterialGeneration_Geometry.SurfaceTypes.LayeredTransparentOuter,
                                                            incrementUsageCounter: true);

            }

            MaterialKey = modifiedMaterialKey;
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

            sideEntity.ParentLevel.EditableSurface_Sides.Add(sideSurface);

            // PhysX can't make a collider from a surface with no area ("cleaning the mesh failed"), as a side of a line with
            // no length (or no height) has
            if (HighElevation > LowElevation && RuntimeSurfaceGeometry.HasArea(SurfaceMesh))
            {
                SurfaceRenderer.gameObject.AddComponent<MeshCollider>();
            }
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
            constraint.Watch(platform);

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

        private void GetPolygonHeights(short polygonIndex, PlatformHeights heights, out float floorHeight, out float ceilingHeight)
        {
            var level = sideEntity.ParentLevel;
            var polygon = map.get_polygon_data(level.Level, polygonIndex);

            if (heights == PlatformHeights.Open)
            {
                polygon.GetHeightRange(level.Level, out var lowestFloor, out _, out _, out var highestCeiling);

                floorHeight = lowestFloor;
                ceilingHeight = highestCeiling;

                return;
            }

            floorHeight = polygon.floor_height;
            ceilingHeight = polygon.ceiling_height;

            var platform = polygon.GetPlatform(level.Level);
            if (platform == null)
            {
                return;
            }

            if (level.FloorPlatforms.TryGetValue(polygon.permutation, out var floorPlatform))
            {
                floorHeight = heights == PlatformHeights.Current ? floorPlatform.CurrentHeightInWorldUnitIncrements : platform.floor_height;
            }

            if (level.CeilingPlatforms.TryGetValue(polygon.permutation, out var ceilingPlatform))
            {
                ceilingHeight = heights == PlatformHeights.Current ? ceilingPlatform.CurrentHeightInWorldUnitIncrements : platform.ceiling_height;
            }
        }

        // The part of this surface Aleph One draws for the given heights (RenderRasterize.cpp: render_node),
        // and the height its texture's top edge is at (RenderRasterize.cpp: render_node_side)
        private void GetAlephOneSurface(
            PlatformHeights facingHeights,
            PlatformHeights opposingHeights,
            bool innerLayer,
            out float bottom,
            out float top,
            out float textureAnchor)
        {
            GetPolygonHeights(FacingPolygonIndex, facingHeights, out var facingFloor, out var facingCeiling);
            GetPolygonHeights(FacingPolygonIndex, PlatformHeights.Initial, out _, out var initialFacingCeiling);

            var opposingPolygonIndex = OpposingPolygonIndex;
            var hasOpposingPolygon = opposingPolygonIndex >= 0;

            var opposingFloor = facingFloor;
            var opposingCeiling = facingCeiling;
            var initialOpposingCeiling = initialFacingCeiling;

            if (hasOpposingPolygon)
            {
                GetPolygonHeights(opposingPolygonIndex, opposingHeights, out opposingFloor, out opposingCeiling);
                GetPolygonHeights(opposingPolygonIndex, PlatformHeights.Initial, out _, out initialOpposingCeiling);
            }

            var lowestAdjacentCeiling = Mathf.Min(facingCeiling, opposingCeiling);
            var highestAdjacentFloor = Mathf.Max(facingFloor, opposingFloor);

            // The unclipped top, which the texture hangs from (surface.h1)
            float textureTop;

            switch (section)
            {
                case LevelEntity_Side.Sections.Top:
                    bottom = Mathf.Min(lowestAdjacentCeiling, facingCeiling);
                    textureTop = facingCeiling;
                    break;

                case LevelEntity_Side.Sections.Middle:
                    if (UsesFullSideHeights)
                    {
                        bottom = facingFloor;
                        textureTop = facingCeiling;
                    }
                    else
                    {
                        bottom = Mathf.Max(highestAdjacentFloor, facingFloor);
                        textureTop = lowestAdjacentCeiling;
                    }

                    break;

                case LevelEntity_Side.Sections.Bottom:
                    bottom = facingFloor;
                    textureTop = Mathf.Max(highestAdjacentFloor, facingFloor);
                    break;

                default:
                    throw new NotImplementedException($"Section '{section}' is not implemented.");
            }

            // Drawn no higher than the ceiling (surface.hmax)
            top = Mathf.Min(textureTop, facingCeiling);

            float textureOffsetY = GetTexture(innerLayer).y0;

            // Primary textures of full, high and split sides move with ceiling platforms: with the platform on the
            // sides facing it, and against it on the platform's own sides (platforms.cpp: adjust_platform_sides)
            if (sideEntity.NativeObject != null &&
                innerLayer &&
                dataSource == LevelEntity_Side.DataSources.Primary &&
                (sideEntity.NativeObject.type == map._full_side ||
                 sideEntity.NativeObject.type == map._high_side ||
                 sideEntity.NativeObject.type == map._split_side))
            {
                if (hasOpposingPolygon)
                {
                    textureOffsetY += opposingCeiling - initialOpposingCeiling;
                }

                textureOffsetY -= facingCeiling - initialFacingCeiling;
            }

            textureAnchor = textureTop + textureOffsetY;
        }

        private side_texture_definition GetTexture(bool innerLayer)
        {
            if (sideEntity.NativeObject == null)
            {
                return NullSideTexture;
            }

            return innerLayer ? sideEntity.NativeObject.GetTexture(dataSource) : sideEntity.NativeObject.transparent_texture;
        }

        // Heights are relative to the surface's transform, with textureAnchor at the top of the texture.
        // Returns the shared scratch array, so the result must be applied before the next call.
        private Vector2[] BuildUVs(short textureOffsetX, float textureAnchor, float bottom, float top)
        {
            var meshUVs = ScratchUVs;

            if (sideEntity.NativeObject == null)
            {
                meshUVs[0] = Vector2.zero;
                meshUVs[1] = Vector2.zero;
                meshUVs[2] = Vector2.zero;
                meshUVs[3] = Vector2.zero;
            }
            else
            {
                var left = textureOffsetX / GeometryUtilities.WorldUnitIncrementsPerWorldUnit;
                var right = left + map.get_line_data(sideEntity.ParentLevel.Level, sideEntity.NativeObject.line_index).length / GeometryUtilities.WorldUnitIncrementsPerWorldUnit;

                // Aleph One's texture coordinate runs downward from the anchor, and ForgePlus's runs upward
                var bottomV = (bottom - textureAnchor) / GeometryUtilities.WorldUnitIncrementsPerWorldUnit;
                var topV = (top - textureAnchor) / GeometryUtilities.WorldUnitIncrementsPerWorldUnit;

                meshUVs[0] = new Vector2(left, bottomV);
                meshUVs[1] = new Vector2(left, topV);
                meshUVs[2] = new Vector2(right, topV);
                meshUVs[3] = new Vector2(right, bottomV);
            }

            return meshUVs;
        }

        private void DecrementTextureUsage()
        {
            if (sideEntity.NativeObject == null)
            {
                return;
            }

            MaterialGeneration_Geometry.DecrementTextureUsage(sideEntity.NativeObject.GetTexture(dataSource).texture);
        }
    }
}
