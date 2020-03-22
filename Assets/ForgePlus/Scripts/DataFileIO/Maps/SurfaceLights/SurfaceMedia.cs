using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;
using Weland;


namespace ForgePlus.LevelManipulation
{
    // TODO: Convert to FPMedia
    public class SurfaceMedia : SurfaceLightBase
    {
        private const float MagnitudeToWorldUnit = 1f / 40f;

        private readonly int mediaDirectionPropertyId = Shader.PropertyToID("_MediaDirectionAngle");
        private readonly int mediaSpeedPropertyId = Shader.PropertyToID("_MediaFlowSpeed");
        private readonly int mediaDepthPropertyId = Shader.PropertyToID("_MediaDepth");

        [SerializeField]
        private Media media;

        public void AssignMedia(Media media)
        {
            this.media = media;

            UpdateDirectionFlowAndDepth();
        }

        private void UpdateDirectionFlowAndDepth()
        {
            if (media != null)
            {
                if (media.CurrentMagnitude != 0)
                {
                    surfaceMaterial.SetFloat(mediaDirectionPropertyId, (float)media.Direction);
                    surfaceMaterial.SetFloat(mediaSpeedPropertyId, (float)media.CurrentMagnitude * MagnitudeToWorldUnit);
                }
                else
                {
                    surfaceMaterial.SetFloat(mediaDirectionPropertyId, 25f);
                    surfaceMaterial.SetFloat(mediaSpeedPropertyId, 0f);
                }

                switch (media.Type)
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
        }

        protected override void SetDisplayValue(float intensity)
        {
            if (media != null)
            {
                var currentHeight = Mathf.Lerp((float)media.Low / GeometryUtilities.WorldUnitIncrementsPerMeter, (float)media.High / GeometryUtilities.WorldUnitIncrementsPerMeter, intensity);
                transform.position = new Vector3(0f, currentHeight, 0f);
            }
        }
    }
}
