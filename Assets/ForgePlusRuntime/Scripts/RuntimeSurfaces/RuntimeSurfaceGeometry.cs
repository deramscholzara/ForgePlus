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
