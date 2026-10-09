using ForgePlus.Inspection;
using ForgePlus.LevelManipulation;
using ForgePlus.LevelManipulation.Utilities;
using RuntimeCore.Common;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using AlephOne;
using ForgePlus.Extensions;


namespace RuntimeCore.Entities.Geometry
{
    // TODO: Should inherit from LevelEntity_Base, and should have a representative GameObject in the scene
    public class LevelEntity_Media : IDestructionPreparable, ISelectable, IInspectable
    {
        private const float MagnitudeToWorldUnit = 1f / 40f; // Note: Not sure why this isn't 1/30 to match the tick rate.

        private static readonly int mediaDirectionPropertyId = Shader.PropertyToID("_MediaDirectionAngle");
        private static readonly int mediaSpeedPropertyId = Shader.PropertyToID("_MediaFlowSpeed");
        private static readonly int mediaDepthPropertyId = Shader.PropertyToID("_MediaDepth");

        public short NativeIndex { get; set; }
        public media_data NativeObject { get; set; }

        public LevelEntity_Level ParentLevel { private get; set; }

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

        private static bool surfacesAreVisible = true;

        // Hidden media surfaces aren't drawn (whether batched or not), though they're still there
        public static bool SurfacesAreVisible
        {
            get
            {
                return surfacesAreVisible;
            }
            set
            {
                if (surfacesAreVisible == value)
                {
                    return;
                }

                surfacesAreVisible = value;

                var level = LevelEntity_Level.Instance;
                if (!level || level.Medias == null)
                {
                    return;
                }

                foreach (var media in level.Medias.Values)
                {
                    foreach (var surface in media.subscribedSurfaces)
                    {
                        ApplyVisibility(surface);
                    }
                }
            }
        }

        public LevelEntity_Media(short index, media_data media, LevelEntity_Level level)
        {
            NativeIndex = index;
            NativeObject = media;
            ParentLevel = level;

            BeginRuntimeStyleBehavior();
        }

        public void SetSelectability(bool enabled)
        {
            // Intentionally blank - no current reason to toggle this, as its selection comes from the palette or already-gated EditableSurface components
        }

        public void Inspect()
        {
            var inspector = new Inspector_Media(this);
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
                ApplyVisibility(surface);
            }
        }

        public void UnsubscribeSurface(Transform surface)
        {
            if (subscribedSurfaces.Contains(surface))
            {
                subscribedSurfaces.Remove(surface);
            }
        }

        // After its flow or type changes (its height follows its data each frame)
        public void ApplyMaterialProperties()
        {
            foreach (var material in subscribedMaterials)
            {
                ApplyDirectionFlowAndDepthPropertiesToMaterial(material);
            }
        }

        public async void BeginRuntimeStyleBehavior()
        {
            synchronizationLoopCTS?.Cancel();

            synchronizationLoopCTS = new CancellationTokenSource();
            var cancellationToken = synchronizationLoopCTS.Token;

            while (!cancellationToken.IsCancellationRequested && Application.isPlaying)
            {
                var lowHeight = (float)NativeObject.low / GeometryUtilities.WorldUnitIncrementsPerMeter;
                var highHeight = (float)NativeObject.high / GeometryUtilities.WorldUnitIncrementsPerMeter;

                var intensity = ParentLevel.Lights[NativeObject.light_index].CurrentLinearIntensity;
                intensity = Mathf.Max(intensity, AlephOneExtensions.FixedToFloat(NativeObject.minimum_light_intensity));

                var currentHeight = Mathf.Lerp(lowHeight, highHeight, intensity);

                CurrentHeight = currentHeight;

                await Awaitable.NextFrameAsync();
            }
        }

        // A surface, or the merged batch it's drawn in
        private static void ApplyVisibility(Transform surface)
        {
            if (surface && surface.TryGetComponent<MeshRenderer>(out var surfaceRenderer))
            {
                surfaceRenderer.forceRenderingOff = !surfacesAreVisible;
            }
        }

        private void ApplyDirectionFlowAndDepthPropertiesToMaterial(Material material)
        {
            if (material)
            {
                if (NativeObject.current_magnitude != 0)
                {
                    material.SetFloat(mediaDirectionPropertyId, AlephOneExtensions.AngleToDegrees(NativeObject.current_direction));
                    material.SetFloat(mediaSpeedPropertyId, (float)NativeObject.current_magnitude * MagnitudeToWorldUnit);
                }
                else
                {
                    material.SetFloat(mediaDirectionPropertyId, 25f);
                    material.SetFloat(mediaSpeedPropertyId, 0f);
                }

                switch (NativeObject.type)
                {
                    case media._media_water:
                        material.SetFloat(mediaDepthPropertyId, 6f);
                        break;
                    case media._media_lava:
                        material.SetFloat(mediaDepthPropertyId, 0.01f);
                        break;
                    case media._media_goo:
                        material.SetFloat(mediaDepthPropertyId, 1f);
                        break;
                    case media._media_sewage:
                        material.SetFloat(mediaDepthPropertyId, 1f);
                        break;
                    case media._media_jjaro:
                        material.SetFloat(mediaDepthPropertyId, 1.25f);
                        break;
                }
            }
        }
    }
}
