using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;
using Weland;


namespace ForgePlus.LevelManipulation
{
    public class SurfaceMedia : MonoBehaviour
    {
        private const float MagnitudeToWorldUnit = 1f / 40f; // Note: Not sure why this isn't 1/30 to match the tick rate.

        private readonly int mediaDirectionPropertyId = Shader.PropertyToID("_MediaDirectionAngle");
        private readonly int mediaSpeedPropertyId = Shader.PropertyToID("_MediaFlowSpeed");
        private readonly int mediaDepthPropertyId = Shader.PropertyToID("_MediaDepth");

#pragma warning disable IDE0044
        // This member is purely here so it's exposed in the inspector
        // TODO: Get rid of this once there's a proper inspector implemented.
        //       - Note: Can then use "IndexOf" to get the index of the FPLight for the inspector
        private short mediaIndex = -1;
#pragma warning restore IDE0044
        private FPMedia fpMedia;

        private Material surfaceMaterial;
        
        public void AssignFPMedia(short mediaIndex, FPMedia fpMedia)
        {
            this.mediaIndex = mediaIndex;
            this.fpMedia = fpMedia;

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
            if (fpMedia != null)
            {
                transform.position = new Vector3(0f, fpMedia.CurrentHeight, 0f);
            }
        }
    }
}
