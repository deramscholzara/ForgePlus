using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using System.Collections.Generic;
using UnityEngine;


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

        public void UnmergeBatch()
        {
            if (lastUnmergeBatchKey.HasValue)
            {
                Debug.LogError($"Surface \"{name}\" is already unmerged and will not make a repeat UnmergeBatch call.", this);
                return;
            }

            SurfaceBatchingManager.Instance.UnmergeBatch(batchKey);
            lastUnmergeBatchKey = batchKey;
        }

        public void MergeBatch(bool remergeFormerBatchIfDifferent = true)
        {
            if (remergeFormerBatchIfDifferent &&
                lastUnmergeBatchKey.HasValue &&
                lastUnmergeBatchKey.Value != batchKey)
            {
                SurfaceBatchingManager.Instance.MergeBatch(lastUnmergeBatchKey.Value);
            }

            SurfaceBatchingManager.Instance.MergeBatch(batchKey);
            lastUnmergeBatchKey = null;
        }

        // TODO: convert InitializeRuntimeSurface into an UpdateMaterialKey method
        //      - should UnmergeBatch/MergeBatch if the key actually changed
        //      - should UnmergeBatch if isStaticBatchable actually changed from true to false
    }
}
