using ForgePlus.ApplicationGeneral;
using ForgePlus.DataFileIO;
using System.Collections.Generic;
using UnityEngine;


namespace ForgePlus.LevelManipulation
{
    public class RuntimeSurfaceLight : MonoBehaviour
    {
        protected SurfaceBatchingManager.RuntimeSurfaceMaterialKey runtimeSurfaceMaterialInstanceKey = new SurfaceBatchingManager.RuntimeSurfaceMaterialKey();

        public void InitializeRuntimeSurface(FPLight fpLight, bool isStaticBatchable)
        {
            var renderer = GetComponent<MeshRenderer>();

            runtimeSurfaceMaterialInstanceKey.sourceMaterial = renderer.sharedMaterials[0];
            runtimeSurfaceMaterialInstanceKey.sourceLight = fpLight;

            // RuntimeSurfaceLight.InitializeRuntimeSurface must not run before runtimeSurfaceMaterialKey is initialized
            renderer.sharedMaterials = SurfaceBatchingManager.Instance.GetUniqueMaterials(runtimeSurfaceMaterialInstanceKey);

            if (isStaticBatchable)
            {
                SurfaceBatchingManager.Instance.AddToBatches(runtimeSurfaceMaterialInstanceKey, gameObject);
            }
        }

        public void InitializeRuntimeSurface(FPLight fpLight, FPLight layeredTransparentSideFPLight, bool isStaticBatchable)
        {
            var renderer = GetComponent<MeshRenderer>();

            runtimeSurfaceMaterialInstanceKey.layeredTransparentSideSourceMaterial = renderer.sharedMaterials[1];
            runtimeSurfaceMaterialInstanceKey.layeredTransparentSideSourceLight = layeredTransparentSideFPLight;

            InitializeRuntimeSurface(fpLight, isStaticBatchable);
            ////Debug.Log($"A: {renderer.sharedMaterials.Length}");
            ////Debug.Log($"B: {renderer.sharedMaterials.Length}");
        }

        // TODO: use this class to control material/mesh batching stuff for geometry/texture/light/media editing
        //       - expose merge and unmerge methods
        //           - can be used when geometry is modified (unmerge on edit-start, merge on edit-complete)
        //           - use the key-specific methods in SurfaceBatchingManager
        //       - convert InitializeRuntimeSurface into an UpdateMaterialKey method
        //           - which would also appropriately unmerge/remerge and remove/add to batches as-appropriate if the key or isStaticMatchable actually changed
    }
}
