using System;
using System.Collections.Generic;
using System.Linq;
using ForgePlus.DataFileIO;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Materials;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace ForgePlus.ApplicationGeneral
{
    // The materials level surfaces are drawn with: a copy of the source material (MaterialGeneration_Geometry's, for a
    // texture and shader) for each combination of source materials, lights, media and transfer mode looks, shared by every
    // surface with that combination. A light sets its intensity on its own copies, a media sets its flow on its own copy,
    // a transfer mode sets its keywords and speeds on its own copies (TransferModeLook), and Simple Visuals swaps all of
    // their shaders (SimpleVisuals). They're forgotten as the level closes.
    [AutoStaticsCleanup]
    public static partial class SurfaceMaterials
    {
        public struct Key : IEquatable<Key>
        {
            // A material, rather than just a texture, as the same texture can be drawn with different shaders (such as a
            // media texture on a wall, and on media)
            public Material SourceMaterial;

            // A layered transparent side's outer layer's
            public Material LayeredTransparentSideSourceMaterial;

            // Lights with no entity (such as a null side's) are drawn at full intensity
            public LevelEntity_Light SourceLight;

            public LevelEntity_Light LayeredTransparentSideSourceLight;

            public LevelEntity_Media SourceMedia;

            // How the surface's transfer mode draws it (and a layered transparent side's outer layer's); the default is Normal
            public TransferModeLook TransferMode;

            public TransferModeLook LayeredTransparentSideTransferMode;

            // Keys are dictionary keys, so these avoid the reflection-based default struct equality
            public bool Equals(Key other)
            {
                return SourceMaterial == other.SourceMaterial &&
                       LayeredTransparentSideSourceMaterial == other.LayeredTransparentSideSourceMaterial &&
                       SourceLight == other.SourceLight &&
                       LayeredTransparentSideSourceLight == other.LayeredTransparentSideSourceLight &&
                       SourceMedia == other.SourceMedia &&
                       TransferMode.Equals(other.TransferMode) &&
                       LayeredTransparentSideTransferMode.Equals(other.LayeredTransparentSideTransferMode);
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(SourceMaterial, LayeredTransparentSideSourceMaterial, SourceLight, LayeredTransparentSideSourceLight, SourceMedia, TransferMode, LayeredTransparentSideTransferMode);
            }

            public static bool operator ==(Key a, Key b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(Key a, Key b)
            {
                return !a.Equals(b);
            }
        }

        private static readonly Dictionary<Key, Material[]> materials = new Dictionary<Key, Material[]>();

        // Whether the materials are forgotten as this Play session's levels close
        private static bool isForgettingOnLevelClose;

        // Surface Cache Global Illumination renders each material's Meta pass once, and again only when the material
        // changes, so a change to what every Meta pass reads (such as the Light Multiplier setting's global) reaches it as
        // each material is touched again: a few a frame, as each one is rendered again (as lights pace theirs)
        private const int SurfaceCacheRefreshesPerFrame = 4;
        private static readonly Queue<Material> surfaceCacheRefreshQueue = new Queue<Material>();
        private static readonly HashSet<Material> surfaceCacheRefreshPending = new HashSet<Material>();
        private static bool isRefreshingEachFrame;
        private static int lastRefreshFrame = -1;
        private static readonly int lightIntensityPropertyId = Shader.PropertyToID("_LightIntensity");
        private static readonly int mainTexturePropertyId = Shader.PropertyToID("_MainTex");

        // Every material surfaces are drawn with
        public static IEnumerable<Material> InUse => materials.Values.SelectMany(keyMaterials => keyMaterials);

        // The surface's materials: its own, then a layered transparent side's outer layer's
        public static Material[] Get(Key key)
        {
            // Landscapes aren't lit by lights, so every one with a texture shares a material that no light changes (which
            // would have Surface Cache Global Illumination render it again for nothing)
            if (MaterialGeneration_Geometry.IsLandscape(key.SourceMaterial))
            {
                key.SourceLight = null;
            }

            if (MaterialGeneration_Geometry.IsLandscape(key.LayeredTransparentSideSourceMaterial))
            {
                key.LayeredTransparentSideSourceLight = null;
            }

            // Static draws neither the texture nor the light, so every static surface drawn with a shader shares a material
            if (key.TransferMode.IsStatic)
            {
                key.SourceMaterial = MaterialGeneration_Geometry.GetStaticMaterial(key.SourceMaterial);
                key.SourceLight = null;
            }

            if (key.LayeredTransparentSideTransferMode.IsStatic)
            {
                key.LayeredTransparentSideSourceMaterial = MaterialGeneration_Geometry.GetStaticMaterial(key.LayeredTransparentSideSourceMaterial);
                key.LayeredTransparentSideSourceLight = null;
            }

            if (materials.TryGetValue(key, out var keyMaterials))
            {
                return keyMaterials;
            }

            if (!isForgettingOnLevelClose)
            {
                isForgettingOnLevelClose = true;
                MapsLoading.Instance.OnLevelClosed += Forget;
            }

            keyMaterials = key.LayeredTransparentSideSourceMaterial ?
                           new[] { new Material(key.SourceMaterial), new Material(key.LayeredTransparentSideSourceMaterial) } :
                           new[] { new Material(key.SourceMaterial) };

            materials[key] = keyMaterials;

            key.TransferMode.ApplyTo(keyMaterials[0]);

            if (keyMaterials.Length > 1)
            {
                key.LayeredTransparentSideTransferMode.ApplyTo(keyMaterials[1]);
            }

            key.SourceMedia?.SubscribeMaterial(keyMaterials[0]);

            // Drawn with the grid while Simple Visuals is on
            foreach (var material in keyMaterials)
            {
                SimpleVisuals.Apply(material);
            }

            // After any shader swap, as a shader without the light's property doesn't keep it
            SubscribeToLight(key.SourceLight, keyMaterials[0]);

            if (keyMaterials.Length > 1)
            {
                SubscribeToLight(key.LayeredTransparentSideSourceLight, keyMaterials[1]);
            }

            return keyMaterials;
        }

        public static void SetSimpleVisualsEnabled(bool enabled)
        {
            SimpleVisuals.SetEnabled(enabled, InUse);

            // The grid shader lights every material, including those whose own shader didn't keep their light's index
            if (enabled)
            {
                foreach (var keyMaterials in materials)
                {
                    ApplyLight(keyMaterials.Key.SourceLight, keyMaterials.Value[0]);

                    if (keyMaterials.Value.Length > 1)
                    {
                        ApplyLight(keyMaterials.Key.LayeredTransparentSideSourceLight, keyMaterials.Value[1]);
                    }
                }
            }
        }

        private static void SubscribeToLight(LevelEntity_Light light, Material material)
        {
            if (light != null)
            {
                light.SubscribeMaterial(material);
            }
            else
            {
                ApplyLight(null, material);
            }
        }

        private static void ApplyLight(LevelEntity_Light light, Material material)
        {
            if (light != null)
            {
                light.ApplyIntensity(material);
            }
            else
            {
                material.SetFloat(LevelEntity_Light.lightIndexPropertyId, LevelEntity_Light.FullIntensityLightIndex);
                material.SetFloat(LevelEntity_Light.lightIntensityPropertyId, 1f);
            }
        }

        // For Fast Enter Play Mode, where statics survive between Play sessions but the materials they hold were destroyed
        // as the last session ended, and the level-close subscription was to the last session's MapsLoading
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            materials.Clear();
            isForgettingOnLevelClose = false;
            SurfaceCacheAdmission.Forget();
            StopRefreshing();
        }

        // Every surface material's Meta pass is rendered again for Surface Cache Global Illumination, a few a frame
        public static void RefreshSurfaceCache()
        {
            foreach (var material in InUse)
            {
                if (material && surfaceCacheRefreshPending.Add(material))
                {
                    surfaceCacheRefreshQueue.Enqueue(material);
                }
            }

            if (!isRefreshingEachFrame && surfaceCacheRefreshQueue.Count > 0)
            {
                isRefreshingEachFrame = true;
                UnityEngine.Rendering.RenderPipelineManager.beginContextRendering += RefreshQueuedMaterials;
            }
        }

        // Once a frame (rendering begins for each view, but they share a frame's refreshes)
        private static void RefreshQueuedMaterials(UnityEngine.Rendering.ScriptableRenderContext context, List<Camera> cameras)
        {
            if (Time.frameCount == lastRefreshFrame)
            {
                return;
            }

            lastRefreshFrame = Time.frameCount;

            for (var i = 0; i < SurfaceCacheRefreshesPerFrame && surfaceCacheRefreshQueue.Count > 0; i++)
            {
                var material = surfaceCacheRefreshQueue.Dequeue();
                surfaceCacheRefreshPending.Remove(material);

                if (material)
                {
                    TouchMaterial(material);
                }
            }

            if (surfaceCacheRefreshQueue.Count == 0)
            {
                StopRefreshing();
            }
        }

        // Setting a property to the value it already has still counts as a change to the material
        private static void TouchMaterial(Material material)
        {
            if (material.HasFloat(lightIntensityPropertyId))
            {
                material.SetFloat(lightIntensityPropertyId, material.GetFloat(lightIntensityPropertyId));
            }
            else if (material.HasTexture(mainTexturePropertyId))
            {
                material.SetTexture(mainTexturePropertyId, material.GetTexture(mainTexturePropertyId));
            }
        }

        private static void StopRefreshing()
        {
            surfaceCacheRefreshQueue.Clear();
            surfaceCacheRefreshPending.Clear();

            if (isRefreshingEachFrame)
            {
                isRefreshingEachFrame = false;
                UnityEngine.Rendering.RenderPipelineManager.beginContextRendering -= RefreshQueuedMaterials;
            }
        }

        private static void Forget()
        {
            materials.Clear();
            StopRefreshing();
            SimpleVisuals.Forget();
            SurfaceCacheAdmission.Forget();
        }
    }
}
