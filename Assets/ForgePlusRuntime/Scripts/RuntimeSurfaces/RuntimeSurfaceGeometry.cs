using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ForgePlus.LevelManipulation.Utilities;

namespace RuntimeCore.Entities.Geometry
{
    [AutoStaticsCleanup]
    public partial class RuntimeSurfaceGeometry : MonoBehaviour
    {
        public Mesh SurfaceMesh => geometryModule.SurfaceMesh;
        
        public MeshRenderer SurfaceRenderer => geometryModule.SurfaceRenderer;

        protected RuntimeSurfaceGeometryModule_Base geometryModule;

        // The media whose height it follows (a media surface's)
        private LevelEntity_Media subscribedMedia;

        public virtual void InitializeRuntimeSurface(
            LevelEntity_Polygon entity,
            LevelEntity_Polygon.DataSources dataSource)
        {
            if (geometryModule != null)
            {
                throw new Exception("Cannot initialize surface more than once.");
            }

            var mesh = CreateMesh();

            geometryModule = new RuntimeSurfaceGeometryModule_Polygon(
                entity,
                dataSource,
                mesh,
                CreateRenderer());

            kind = dataSource == LevelEntity_Polygon.DataSources.Media ? Kind.Other : Kind.Polygon;

            AssembleSurface();
        }

        public virtual void InitializeRuntimeSurface(
            LevelEntity_Side entity,
            LevelEntity_Side.DataSources dataSource,
            LevelEntity_Side.Sections section)
        {
            if (geometryModule != null)
            {
                throw new Exception("Cannot initialize surface more than once.");
            }

            var mesh = CreateMesh();

            geometryModule = new RuntimeSurfaceGeometryModule_Side(
                entity,
                dataSource,
                section,
                mesh,
                CreateRenderer());

            kind = Kind.Side;

            AssembleSurface();
        }

        // Surfaces are lit by the level's own light-intensity texture rather than by Unity's lighting,
        // so Unity's per-renderer lighting features would only add culling and draw costs
        public static void ConfigureRenderer(MeshRenderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        public void PrepareForDestruction()
        {
            SubscribeToMedia(null);

            geometryModule.PrepareForDestruction();
        }

        public void ApplyPositions()
        {
            geometryModule.ApplyPositionsAndTriangles();
            geometryModule.ApplySurfaceCacheAdmission();
        }

        public void ApplyPlatform()
        {
            geometryModule.ApplyPositionsAndTriangles();
            geometryModule.ApplySurfaceCacheAdmission();
        }

        // For a floor's, ceiling's or media's corners that moved: its positions, the texture coordinates that follow them,
        // and its collider
        public void ApplyShape()
        {
            geometryModule.ApplyPositionsAndTriangles();
            geometryModule.ApplyTextureOffset(innerLayer: true);
            geometryModule.ApplySurfaceCacheAdmission();

            if (!TryGetComponent<MeshCollider>(out var meshCollider) && HasArea(SurfaceMesh))
            {
                gameObject.AddComponent<MeshCollider>();
            }
            else if (meshCollider)
            {
                // The collider only takes a mesh's new shape as it's assigned, and PhysX can't make one with no area (as a
                // polygon whose corners are in a line has, while it's being moved)
                meshCollider.sharedMesh = null;
                if (HasArea(SurfaceMesh))
                {
                    meshCollider.sharedMesh = SurfaceMesh;
                }
            }
        }
        // Whether any of the mesh's triangles has area
        public static bool HasArea(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            for (var i = 0; i + 2 < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                if (Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a).sqrMagnitude > 1e-10f)
                {
                    return true;
                }
            }

            return false;
        }

        // For a floor's or ceiling's height
        public void ApplyHeight()
        {
            geometryModule.ApplyTransformPosition();
        }

        public void ApplyTextureOffset(bool innerLayer = true)
        {
            geometryModule.ApplyTextureOffset(innerLayer);
        }

        public void ApplyTransferMode(bool innerLayer = true)
        {
            geometryModule.ApplyTransferMode(innerLayer);
        }

        public void ApplyTexture(bool innerLayer = true)
        {
            geometryModule.ApplySourceMaterial(innerLayer);
            geometryModule.ApplyRendererMaterials();
        }

        public void ApplyLight(bool innerLayer = true)
        {
            geometryModule.ApplyLight(innerLayer);
            geometryModule.ApplyRendererMaterials();
        }

        public void ApplyAmbientDelta()
        {
            if (geometryModule is RuntimeSurfaceGeometryModule_Side sideModule)
            {
                sideModule.ApplyAmbientDelta();
            }
        }

        public void ApplyMedia()
        {
            if (geometryModule is RuntimeSurfaceGeometryModule_Polygon &&
                geometryModule.MaterialKey.SourceMedia != null)
            {
                geometryModule.ApplyMedia();

                if (geometryModule.MaterialKey.SourceMedia == null)
                {
                    PrepareForDestruction();
                    Destroy(gameObject);
                    return;
                }

                // Its media (or its media's type) may have changed its texture
                geometryModule.ApplySourceMaterial(innerLayer: true);
                geometryModule.ApplyRendererMaterials();

                SubscribeToMedia(geometryModule.MaterialKey.SourceMedia);
            }
        }

        // Destroyed with the surface (DestroysMeshOnDestroy)
        private Mesh CreateMesh()
        {
            var mesh = new Mesh();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<DestroysMeshOnDestroy>().Mesh = mesh;

            return mesh;
        }

        private MeshRenderer CreateRenderer()
        {
            var surfaceRenderer = gameObject.AddComponent<MeshRenderer>();
            ConfigureRenderer(surfaceRenderer);

            return surfaceRenderer;
        }

        // Which of the visualization options (Polygons, Sides) shows and hides it (a media surface is Media's)
        private enum Kind
        {
            Other,
            Polygon,
            Side,
        }

        private Kind kind;

        // Clicks pass through what's hidden (the pointer doesn't raycast this layer)
        private const int HiddenLayer = 2;
        private const int ShownLayer = 0;

        private static bool polygonsAreVisible = true;
        private static bool sidesAreVisible = true;

        // Every floor's, ceiling's and side's surface, for showing and hiding them
        // Not readonly, so AutoStaticsCleanup resets it each Play session
        private static HashSet<RuntimeSurfaceGeometry> kindSurfaces = new HashSet<RuntimeSurfaceGeometry>();

        // Floors and ceilings (Polygons) and sides (Sides) that are hidden aren't drawn, and clicks pass through them,
        // though they're still there
        public static void SetKindVisibility(bool polygonsVisible, bool sidesVisible)
        {
            if (polygonsVisible == polygonsAreVisible && sidesVisible == sidesAreVisible)
            {
                return;
            }

            polygonsAreVisible = polygonsVisible;
            sidesAreVisible = sidesVisible;

            kindSurfaces.RemoveWhere(surface => !surface);
            foreach (var surface in kindSurfaces)
            {
                surface.ApplyKindVisibility();
            }
        }

        private void ApplyKindVisibility()
        {
            var isShown = kind == Kind.Polygon ? polygonsAreVisible : kind != Kind.Side || sidesAreVisible;
            var surfaceRenderer = SurfaceRenderer;

            if (surfaceRenderer)
            {
                surfaceRenderer.forceRenderingOff = !isShown;
            }

            gameObject.layer = isShown ? ShownLayer : HiddenLayer;
        }

        private void OnDestroy()
        {
            kindSurfaces.Remove(this);
        }

        private void AssembleSurface()
        {
            geometryModule.AssembleSurface();

            if (kind != Kind.Other)
            {
                kindSurfaces.Add(this);
                ApplyKindVisibility();
            }

            // A media's surfaces follow its height
            SubscribeToMedia(geometryModule.MaterialKey.SourceMedia);

            // If editing isn't possible, then the surface should never need to be updated or rebuilt
#if NO_EDITING
            Destroy(this);
#endif
        }

        private void SubscribeToMedia(LevelEntity_Media media)
        {
            if (media == subscribedMedia)
            {
                return;
            }

            subscribedMedia?.UnsubscribeSurface(transform);
            subscribedMedia = media;
            subscribedMedia?.SubscribeSurface(transform);
        }    }
}
