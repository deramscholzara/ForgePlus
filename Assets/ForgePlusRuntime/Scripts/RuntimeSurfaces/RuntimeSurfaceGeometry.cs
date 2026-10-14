using System;
using UnityEngine;
using UnityEngine.Rendering;
using ForgePlus.LevelManipulation.Utilities;

namespace RuntimeCore.Entities.Geometry
{
    public class RuntimeSurfaceGeometry : MonoBehaviour
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
            renderer.allowOcclusionWhenDynamic = false;
        }

        public void PrepareForDestruction()
        {
            SubscribeToMedia(null);

            geometryModule.PrepareForDestruction();
        }

        public void ApplyPositions()
        {
            geometryModule.ApplyPositionsAndTriangles();
        }

        public void ApplyPlatform()
        {
            geometryModule.ApplyPositionsAndTriangles();
        }

        // For a floor's, ceiling's or media's corners that moved: its positions, the texture coordinates that follow them,
        // and its collider
        public void ApplyShape()
        {
            geometryModule.ApplyPositionsAndTriangles();
            geometryModule.ApplyTextureOffset(innerLayer: true);

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

        private void AssembleSurface()
        {
            geometryModule.AssembleSurface();

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
