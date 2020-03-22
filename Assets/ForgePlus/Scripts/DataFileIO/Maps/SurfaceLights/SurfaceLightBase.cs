using UnityEngine;

namespace ForgePlus.LevelManipulation
{
    public abstract class SurfaceLightBase : MonoBehaviour
    {
        protected Material surfaceMaterial;

#pragma warning disable IDE0044
        // This member is purely here so it's exposed in the inspector
        // TODO: Get rid of this once there's a proper inspector implemented.
        //       - Note: Can then use "IndexOf" to get the index of the FPLight for the inspector
        private int lightIndex = -1;
#pragma warning restore IDE0044
        private FPLight fPLight;
        private float minimumIntensity = 0f; // For Media

        public void AssignFPLight(FPLight fpLight, int lightIndex, float minimumIntensity = 0f)
        {
            this.lightIndex = lightIndex;
            fPLight = fpLight;
            this.minimumIntensity = minimumIntensity;
        }

        protected abstract void SetDisplayValue(float intensity);

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
                SetDisplayValue(Mathf.Max(fPLight.CurrentIntensity, minimumIntensity));
            }
        }
    }
}
