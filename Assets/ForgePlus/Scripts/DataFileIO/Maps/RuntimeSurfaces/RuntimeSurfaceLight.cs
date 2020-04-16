using UnityEngine;


namespace ForgePlus.LevelManipulation
{
    public class RuntimeSurfaceLight : MonoBehaviour
    {
        private readonly int lightIntensityPropertyId = Shader.PropertyToID("_LightIntensity");

        protected Material surfaceMaterial;

#pragma warning disable IDE0044
        // This member is purely here so it's exposed in the inspector
        // TODO: Get rid of this once there's a proper inspector implemented.
        //       - Note: Can then use "IndexOf" to get the index of the FPLight for the inspector
        private short lightIndex = -1;
#pragma warning restore IDE0044
        private FPLight fPLight;

        public void AssignFPLight(short lightIndex, FPLight fpLight)
        {
            this.lightIndex = lightIndex;
            fPLight = fpLight;
        }

        private void Awake()
        {
            // TODO: There must be a more efficient way to do this, than to make
            //       a unique material for each renderer.
            //       Does Unity's instancing system handle this automatically?
            //       Can DOTS in shaders/materials help with this?
            surfaceMaterial = GetComponent<MeshRenderer>().material;
        }

        private void Update()
        {
            if (fPLight != null)
            {
                surfaceMaterial.SetFloat(lightIntensityPropertyId, fPLight.CurrentIntensity);
            }
        }
    }
}
