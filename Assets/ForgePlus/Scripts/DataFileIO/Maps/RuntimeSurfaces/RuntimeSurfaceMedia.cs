using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;
using Weland;


namespace ForgePlus.LevelManipulation
{
    public class RuntimeSurfaceMedia : RuntimeSurfaceLight
    {
        private const float MagnitudeToWorldUnit = 1f / 40f; // Note: Not sure why this isn't 1/30 to match the tick rate.

        private readonly int mediaDirectionPropertyId = Shader.PropertyToID("_MediaDirectionAngle");
        private readonly int mediaSpeedPropertyId = Shader.PropertyToID("_MediaFlowSpeed");
        private readonly int mediaDepthPropertyId = Shader.PropertyToID("_MediaDepth");

        private FPMedia fpMedia;

        public void InitializeRuntimeSurface(FPLight fpLight, FPMedia fpMedia)
        {
            this.fpMedia = fpMedia;

            runtimeSurfaceMaterialKey.sourceMedia = fpMedia;

            // RuntimeSurfaceLight.InitializeRuntimeSurface must not run before runtimeSurfaceMaterialKey is initialized
            base.InitializeRuntimeSurface(fpLight, isStaticBatchable: false);

            UpdateDirectionFlowAndDepth();
        }

        private void UpdateDirectionFlowAndDepth()
        {
            if (fpMedia.WelandObject.CurrentMagnitude != 0)
            {
                surfaceMaterial.SetFloat(mediaDirectionPropertyId, (float)fpMedia.WelandObject.Direction);
                surfaceMaterial.SetFloat(mediaSpeedPropertyId, (float)fpMedia.WelandObject.CurrentMagnitude * MagnitudeToWorldUnit);
            }
            else
            {
                surfaceMaterial.SetFloat(mediaDirectionPropertyId, 25f);
                surfaceMaterial.SetFloat(mediaSpeedPropertyId, 0f);
            }

            switch (fpMedia.WelandObject.Type)
            {
                case MediaType.Water:
                    surfaceMaterial.SetFloat(mediaDepthPropertyId, 6f);
                    break;
                case MediaType.Lava:
                    surfaceMaterial.SetFloat(mediaDepthPropertyId, 0.01f);
                    break;
                case MediaType.Goo:
                    surfaceMaterial.SetFloat(mediaDepthPropertyId, 1f);
                    break;
                case MediaType.Sewage:
                    surfaceMaterial.SetFloat(mediaDepthPropertyId, 1f);
                    break;
                case MediaType.Jjaro:
                    surfaceMaterial.SetFloat(mediaDepthPropertyId, 1.25f);
                    break;
            }
        }

        protected override void Update()
        {
            base.Update();

            transform.position = new Vector3(0f, fpMedia.CurrentHeight, 0f);
        }
    }
}
