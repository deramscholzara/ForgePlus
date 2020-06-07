using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Weland;


namespace RuntimeCore.Entities.Geometry
{
    public class LevelEntity_Media : IManipulatable<Media>, IDestructionPreparable, ISelectable, IInspectable
    {
        private const float MagnitudeToWorldUnit = 1f / 40f; // Note: Not sure why this isn't 1/30 to match the tick rate.

        private static readonly int mediaDirectionPropertyId = Shader.PropertyToID("_MediaDirectionAngle");
        private static readonly int mediaSpeedPropertyId = Shader.PropertyToID("_MediaFlowSpeed");
        private static readonly int mediaDepthPropertyId = Shader.PropertyToID("_MediaDepth");

        public short NativeIndex { get; set; }
        public Media NativeObject { get; set; }

        public LevelEntity_Level FPLevel { private get; set; }

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

        public LevelEntity_Media(short index, Media media, LevelEntity_Level fpLevel)
        {
            NativeIndex = index;
            NativeObject = media;
            FPLevel = fpLevel;

            BeginRuntimeStyleBehavior();
        }

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank - no current reason to toggle this, as its selection comes from the palette or already-gated FPInteractiveSurface components
        }

        public void Inspect()
        {
            var inspectorPrefab = Resources.Load<Inspector_Media>("Inspectors/Inspector - Media");
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
            if (!subscribedSurfaces.Contains(surface))
            {
                subscribedSurfaces.Add(surface);

                surface.position = new Vector3(0f, CurrentHeight, 0f);
            }
        }

        public void UnsubscribeSurface(Transform surface)
        {
            if (subscribedSurfaces.Contains(surface))
            {
                subscribedSurfaces.Remove(surface);
            }
        }

        public async void BeginRuntimeStyleBehavior()
        {
            synchronizationLoopCTS?.Cancel();

            synchronizationLoopCTS = new CancellationTokenSource();
            var cancellationToken = synchronizationLoopCTS.Token;

            while (!cancellationToken.IsCancellationRequested && Application.isPlaying)
            {
                var lowHeight = (float)NativeObject.Low / GeometryUtilities.WorldUnitIncrementsPerMeter;
                var highHeight = (float)NativeObject.High / GeometryUtilities.WorldUnitIncrementsPerMeter;

                var intensity = FPLevel.FPLights[NativeObject.LightIndex].CurrentLinearIntensity;
                intensity = Mathf.Max(intensity, (float)NativeObject.MinimumLightIntensity);

                var currentHeight = Mathf.Lerp(lowHeight, highHeight, intensity);

                CurrentHeight = currentHeight;

                await Task.Yield();
            }
        }

        private void ApplyDirectionFlowAndDepthPropertiesToMaterial(Material material)
        {
            if (material)
            {
                if (NativeObject.CurrentMagnitude != 0)
                {
                    material.SetFloat(mediaDirectionPropertyId, (float)NativeObject.Direction);
                    material.SetFloat(mediaSpeedPropertyId, (float)NativeObject.CurrentMagnitude * MagnitudeToWorldUnit);
                }
                else
                {
                    material.SetFloat(mediaDirectionPropertyId, 25f);
                    material.SetFloat(mediaSpeedPropertyId, 0f);
                }

                switch (NativeObject.Type)
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
