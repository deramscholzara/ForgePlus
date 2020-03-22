using UnityEngine;


namespace ForgePlus.LevelManipulation
{
    public class SurfaceLight : SurfaceLightBase
    {
        private readonly int lightIntensityPropertyId = Shader.PropertyToID("_LightIntensity");

        protected override void SetDisplayValue(float intensity)
        {
            surfaceMaterial.SetFloat(lightIntensityPropertyId, intensity);
        }
    }
}
