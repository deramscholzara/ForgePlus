using ForgePlus.Inspection;
using ForgePlus.LevelManipulation.Utilities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Weland;


namespace ForgePlus.LevelManipulation
{
    public class FPMedia : IFPManipulatable<Media>, IFPDestructionPreparable, IFPSelectable, IFPInspectable
    {
        private const float MagnitudeToWorldUnit = 1f / 40f; // Note: Not sure why this isn't 1/30 to match the tick rate.

        private static readonly int mediaDirectionPropertyId = Shader.PropertyToID("_MediaDirectionAngle");
        private static readonly int mediaSpeedPropertyId = Shader.PropertyToID("_MediaFlowSpeed");
        private static readonly int mediaDepthPropertyId = Shader.PropertyToID("_MediaDepth");

        public short? Index { get; set; }
        public Media WelandObject { get; set; }

        public FPLevel FPLevel { private get; set; }

        public float CurrentHeight
        {
            get
            {
                return currentHeight;
            }
            private set
            {
                currentHeight = value;

                foreach (var transform in subscribedSurfaces)
                {
                    transform.position = new Vector3(0f, currentHeight, 0f);
                }
            }
        }

        private float currentHeight = 0f;

        private List<Material> subscribedMaterials = new List<Material>();
        private List<Transform> subscribedSurfaces = new List<Transform>();

        private CancellationTokenSource synchronizationLoopCTS;

        public FPMedia(short index, Media media, FPLevel fpLevel)
        {
            Index = index;
            WelandObject = media;
            FPLevel = fpLevel;

            BeginRuntimeStyleBehavior();
        }

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank - no current reason to toggle this, as its selection comes from the palette or already-gated FPInteractiveSurface components
        }

        public void Inspect()
        {
            var inspectorPrefab = Resources.Load<InspectorFPMedia>("Inspectors/Inspector - FPMedia");
            var inspector = Object.Instantiate(inspectorPrefab);
            inspector.PopulateValues(this);
            InspectorPanel.Instance.AddInspector(inspector);
        }

        public void PrepareForDestruction()
        {
            synchronizationLoopCTS?.Cancel();
            synchronizationLoopCTS = null;
        }

        public void SubscribeMaterial(Material material)
        {
            subscribedMaterials.Add(material);

            ApplyDirectionFlowAndDepthPropertiesToMaterial(material);
        }

        public void UnsubscribeMaterial(Material material)
        {
            subscribedMaterials.Remove(material);
        }

        public void SubscribeSurface(Transform surface)
        {
            subscribedSurfaces.Add(surface);

            surface.position = new Vector3(0f, CurrentHeight, 0f);
        }

        public void UnsubscribeSurface(Transform surface)
        {
            subscribedSurfaces.Remove(surface);
        }

        public async void BeginRuntimeStyleBehavior()
        {
            synchronizationLoopCTS?.Cancel();

            synchronizationLoopCTS = new CancellationTokenSource();
            var cancellationToken = synchronizationLoopCTS.Token;

            while (!cancellationToken.IsCancellationRequested && Application.isPlaying)
            {
                var lowHeight = (float)WelandObject.Low / GeometryUtilities.WorldUnitIncrementsPerMeter;
                var highHeight = (float)WelandObject.High / GeometryUtilities.WorldUnitIncrementsPerMeter;

                var intensity = (float)FPLevel.FPLights[WelandObject.LightIndex].CurrentIntensity;
                intensity = Mathf.Max(intensity, (float)WelandObject.MinimumLightIntensity);

                var currentHeight = Mathf.Lerp(lowHeight, highHeight, intensity);

                CurrentHeight = currentHeight;

                await Task.Yield();
            }
        }

        private void ApplyDirectionFlowAndDepthPropertiesToMaterial(Material material)
        {
            if (material)
            {
                if (WelandObject.CurrentMagnitude != 0)
                {
                    material.SetFloat(mediaDirectionPropertyId, (float)WelandObject.Direction);
                    material.SetFloat(mediaSpeedPropertyId, (float)WelandObject.CurrentMagnitude * MagnitudeToWorldUnit);
                }
                else
                {
                    material.SetFloat(mediaDirectionPropertyId, 25f);
                    material.SetFloat(mediaSpeedPropertyId, 0f);
                }

                switch (WelandObject.Type)
                {
                    case MediaType.Water:
                        material.SetFloat(mediaDepthPropertyId, 6f);
                        break;
                    case MediaType.Lava:
                        material.SetFloat(mediaDepthPropertyId, 0.01f);
                        break;
                    case MediaType.Goo:
                        material.SetFloat(mediaDepthPropertyId, 1f);
                        break;
                    case MediaType.Sewage:
                        material.SetFloat(mediaDepthPropertyId, 1f);
                        break;
                    case MediaType.Jjaro:
                        material.SetFloat(mediaDepthPropertyId, 1.25f);
                        break;
                }
            }
        }
    }
}
