using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;
using Weland;


namespace ForgePlus.LevelManipulation
{
    public class RuntimeSurfaceMedia : RuntimeSurfaceLight
    {
        private FPMedia fpMedia;

        public void InitializeRuntimeSurface(FPLight fpLight, FPMedia fpMedia)
        {
            this.fpMedia = fpMedia;

            batchKey.sourceMedia = fpMedia;

            // RuntimeSurfaceLight.InitializeRuntimeSurface must not run before runtimeSurfaceMaterialKey is initialized
            base.InitializeRuntimeSurface(fpLight, isStaticBatchable: false);

            fpMedia.SubscribeSurface(transform);
        }

        private void OnDestroy()
        {
            if (fpMedia != null)
            {
                fpMedia.UnsubscribeSurface(transform);
            }
        }
    }
}
