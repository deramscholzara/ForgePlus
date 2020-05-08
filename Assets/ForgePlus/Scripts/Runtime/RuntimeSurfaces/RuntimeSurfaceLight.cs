using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using ForgePlus.ShapesCollections;
using System.Collections.Generic;
using UnityEngine;
using Weland;

namespace ForgePlus.LevelManipulation
{
    public class RuntimeSurfaceLight : MonoBehaviour
    {
        protected SurfaceBatchingManager.BatchKey batchKey = new SurfaceBatchingManager.BatchKey();

        private SurfaceBatchingManager.BatchKey? lastUnmergeBatchKey = null;

        public void InitializeRuntimeSurface(FPLight fpLight, bool isStaticBatchable)
        {
            var renderer = GetComponent<MeshRenderer>();

            batchKey.sourceMaterial = renderer.sharedMaterials[0];
            batchKey.sourceLight = fpLight;

            // RuntimeSurfaceLight.InitializeRuntimeSurface must not run before runtimeSurfaceMaterialKey is initialized
            renderer.sharedMaterials = SurfaceBatchingManager.Instance.GetUniqueMaterials(batchKey);

            if (isStaticBatchable)
            {
                SurfaceBatchingManager.Instance.AddToBatches(batchKey, gameObject);
            }
        }

        public void InitializeRuntimeSurface(FPLight fpLight, FPLight layeredTransparentSideFPLight, bool isStaticBatchable)
        {
            var renderer = GetComponent<MeshRenderer>();

            batchKey.layeredTransparentSideSourceMaterial = renderer.sharedMaterials[1];
            batchKey.layeredTransparentSideSourceLight = layeredTransparentSideFPLight;

            InitializeRuntimeSurface(fpLight, isStaticBatchable);
        }

        public void SetShapeDescriptor(ShapeDescriptor shapeDescriptor, short transferMode, bool isOpaqueSurface, WallsCollection.SurfaceTypes wallsCollectionType)
        {
            // TODO: Sides need the option to set the Transparent side on Full-TransparentSide surfaces,
            //       and should call the appropriate version of InitializeRuntimeSurface in those cases.

            var newMaterial = WallsCollection.GetMaterial(shapeDescriptor,
                                                          transferMode,
                                                          isOpaqueSurface,
                                                          wallsCollectionType,
                                                          incrementUsageCounter: true);

            if (newMaterial == batchKey.sourceMaterial)
            {
                return;
            }

            var wasMerged = SurfaceBatchingManager.Instance.GetBatchIsMerged(batchKey);

            if (wasMerged)
            {
                UnmergeBatch();

                SurfaceBatchingManager.Instance.RemoveFromBatches(batchKey, gameObject);
            }

            GetComponent<Renderer>().sharedMaterial = newMaterial;

            batchKey.sourceMaterial = newMaterial;

            if (wasMerged)
            {
                SurfaceBatchingManager.Instance.AddToBatches(batchKey, gameObject);

                MergeBatch();
            }
        }

        public void UnmergeBatch()
        {
            if (lastUnmergeBatchKey.HasValue)
            {
                Debug.LogError($"Surface \"{name}\" is already unmerged and will not make a repeat UnmergeBatch call.", this);
                return;
            }

            lastUnmergeBatchKey = batchKey;

            SurfaceBatchingManager.Instance.UnmergeBatch(batchKey);
        }

        public void MergeBatch(bool remergeFormerBatchIfDifferent = true)
        {
            if (remergeFormerBatchIfDifferent &&
                lastUnmergeBatchKey.HasValue &&
                lastUnmergeBatchKey.Value != batchKey &&
                SurfaceBatchingManager.Instance.GetBatchExists(lastUnmergeBatchKey.Value))
            {
                SurfaceBatchingManager.Instance.MergeBatch(lastUnmergeBatchKey.Value);
            }

            lastUnmergeBatchKey = null;

            SurfaceBatchingManager.Instance.MergeBatch(batchKey);
        }
    }
}
