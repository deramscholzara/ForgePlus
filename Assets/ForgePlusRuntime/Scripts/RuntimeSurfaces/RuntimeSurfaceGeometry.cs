using ForgePlus.ApplicationGeneral;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RuntimeCore.Entities.Geometry
{
    public class RuntimeSurfaceGeometry : MonoBehaviour
    {
        public Mesh SurfaceMesh => geometryModule.SurfaceMesh;
        
        public MeshRenderer SurfaceRenderer => geometryModule.SurfaceRenderer;

        protected RuntimeSurfaceGeometryModule_Base geometryModule;

        public virtual void InitializeRuntimeSurface(
            LevelEntity_Polygon entity,
            LevelEntity_Polygon.DataSources dataSource)
        {
            if (geometryModule != null)
            {
                throw new Exception("Cannot initialize surface more than once.");
            }

            var mesh = new Mesh();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;

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

            var mesh = new Mesh();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;

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
            SurfaceBatchingManager.Instance.UnmergeBatch(geometryModule.BatchKey);
            SurfaceBatchingManager.Instance.RemoveFromBatches(geometryModule.BatchKey, this);

            geometryModule.PrepareForDestruction();
        }

        public void ApplyPositions(bool rebatchImmediately = true)
        {
            ApplyChange(rebatchImmediately, () => geometryModule.ApplyPositionsAndTriangles());
        }

        public void ApplyPlatform(bool rebatchImmediately = true)
        {
            ApplyChange(rebatchImmediately, () => geometryModule.ApplyPositionsAndTriangles());
        }

        // For a floor's, ceiling's or media's corners that moved: its positions, the texture coordinates that follow them,
        // and its collider
        public void ApplyShape(bool rebatchImmediately = true)
        {
            ApplyChange(rebatchImmediately, () =>
            {
                geometryModule.ApplyPositionsAndTriangles();
                geometryModule.ApplyTextureOffset(innerLayer: true);
            });

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
        public void ApplyHeight(bool rebatchImmediately = true)
        {
            ApplyChange(rebatchImmediately, () => geometryModule.ApplyTransformPosition());
        }

        public void ApplyTextureOffset(bool innerLayer = true, bool rebatchImmediately = true)
        {
            ApplyChange(rebatchImmediately, () => geometryModule.ApplyTextureOffset(innerLayer));
        }

        public void ApplyTransferMode(bool innerLayer = true, bool rebatchImmediately = true)
        {
            ApplyChange(rebatchImmediately, () => geometryModule.ApplyTransferMode(innerLayer));
        }

        public void ApplyTexture(bool innerLayer = true, bool rebatchImmediately = true)
        {
            ApplyChange(rebatchImmediately, () =>
            {
                geometryModule.ApplyBatchKeyMaterial(innerLayer);
                geometryModule.ApplyRendererMaterials();
            });
        }

        public void ApplyLight(bool innerLayer = true, bool rebatchImmediately = true)
        {
            ApplyChange(rebatchImmediately, () =>
            {
                geometryModule.ApplyLight(innerLayer);
                geometryModule.ApplyRendererMaterials();
            });
        }

        public void ApplyAmbientDelta(bool rebatchImmediately = true)
        {
            if (geometryModule is RuntimeSurfaceGeometryModule_Side sideModule)
            {
                ApplyChange(rebatchImmediately, () => sideModule.ApplyAmbientDelta());
            }
        }

        public void ApplyMedia(bool rebatchImmediately = true)
        {
            if (geometryModule is RuntimeSurfaceGeometryModule_Polygon &&
                geometryModule.BatchKey.SourceMedia != null)
            {
                ApplyChange(rebatchImmediately: false, () =>
                {
                    geometryModule.ApplyMedia();

                    if (geometryModule.BatchKey.SourceMedia == null)
                    {
                        return;
                    }
                    else
                    {
                        // Its media (or its media's type) may have changed its texture
                        geometryModule.ApplyBatchKeyMaterial(innerLayer: true);
                        geometryModule.ApplyRendererMaterials();
                    }
                });

                if (geometryModule.BatchKey.SourceMedia == null)
                {
                    PrepareForDestruction();
                    Destroy(gameObject);
                }
                else if (rebatchImmediately && SurfaceBatchingManager.Instance.GetBatchIsMerged(geometryModule.BatchKey))
                {
                    SurfaceBatchingManager.Instance.MergeAllBatches();
                }
            }
        }

        private MeshRenderer CreateRenderer()
        {
            var surfaceRenderer = gameObject.AddComponent<MeshRenderer>();
            ConfigureRenderer(surfaceRenderer);

            return surfaceRenderer;
        }

        private void AssembleSurface()
        {
            ApplyChange(rebatchImmediately: false,
                        changeAction: () => geometryModule.AssembleSurface());

            // If editing isn't possible, then the surface should never need to be updated or rebuilt
#if NO_EDITING
            Destroy(this);
#endif
        }
        
        private void ApplyChange(bool rebatchImmediately, Action changeAction)
        {
            rebatchImmediately &= SurfaceBatchingManager.Instance.GetBatchIsMerged(geometryModule.BatchKey);

            SurfaceBatchingManager.Instance.UnmergeBatch(geometryModule.BatchKey);
            SurfaceBatchingManager.Instance.RemoveFromBatches(geometryModule.BatchKey, this);

            changeAction.Invoke();

            if (geometryModule.IsStaticBatchable)
            {
                SurfaceBatchingManager.Instance.AddToBatches(geometryModule.BatchKey, this);
                if (rebatchImmediately)
                {
                    SurfaceBatchingManager.Instance.MergeAllBatches();
                }
            }
        }
    }
}
