using ForgePlus.ApplicationGeneral;
using System;
using UnityEngine;

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
                gameObject.AddComponent<MeshRenderer>());

            AssembleSurface();
        }

        public virtual void InitializeRuntimeSurface(
            LevelEntity_Side entity,
            LevelEntity_Side.DataSources dataSource)
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
                mesh,
                gameObject.AddComponent<MeshRenderer>());

            AssembleSurface();
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
                geometryModule.BatchKey.sourceMedia != null)
            {
                ApplyChange(rebatchImmediately: false, () =>
                {
                    geometryModule.ApplyMedia();

                    if (geometryModule.BatchKey.sourceMedia == null)
                    {
                        return;
                    }
                    else
                    {
                        geometryModule.ApplyBatchKeyMaterial(innerLayer: true);
                    }
                });

                if (geometryModule.BatchKey.sourceMedia == null)
                {
                    PrepareForDestruction();
                    Destroy(gameObject);
                }
                else if (rebatchImmediately)
                {
                    SurfaceBatchingManager.Instance.MergeAllBatches();
                }
            }
        }

        protected void AssembleSurface(bool rebatchImmediately = true)
        {
            ApplyChange(rebatchImmediately, () => geometryModule.AssembleSurface());

            // If editing isn't possible, then the surface should never need to be updated or rebuilt
#if NO_EDITING
            Destroy(this);
#endif
        }

        protected void ApplyChange(bool rebatchImmediately, Action changeAction)
        {
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
