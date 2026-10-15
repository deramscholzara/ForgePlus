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
    // texture and shader) for each combination of source materials, lights and media, shared by every surface with that
    // combination. A light sets its intensity on its own copies, a media sets its flow on its own copy, and Simple
    // Visuals swaps all of their shaders (SimpleVisuals). They're forgotten as the level closes.
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

            // Keys are dictionary keys, so these avoid the reflection-based default struct equality
            public bool Equals(Key other)
            {
                return SourceMaterial == other.SourceMaterial &&
                       LayeredTransparentSideSourceMaterial == other.LayeredTransparentSideSourceMaterial &&
                       SourceLight == other.SourceLight &&
                       LayeredTransparentSideSourceLight == other.LayeredTransparentSideSourceLight &&
                       SourceMedia == other.SourceMedia;
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(SourceMaterial, LayeredTransparentSideSourceMaterial, SourceLight, LayeredTransparentSideSourceLight, SourceMedia);
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

        private static void Forget()
        {
            materials.Clear();
            SimpleVisuals.Forget();
            SurfaceCacheAdmission.Forget();
        }
    }
}
