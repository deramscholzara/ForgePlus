﻿using ForgePlus.DataFileIO;
using System.Collections.Generic;
using UnityEngine;
using Weland;
using Weland.Extensions;

namespace RuntimeCore.Materials
{
    public class MaterialGeneration_Geometry
    {
        public enum SurfaceTypes
        {
            Normal,
            Media,
            LayeredTransparentOuter,
        }

        // Normal
        private static readonly Shader OpaqueWithAlphaAlphaNormalShader = Shader.Find("ForgePlus/OpaqueWithAlphaNormal");
        private static readonly Shader TransparentNormalShader = Shader.Find("ForgePlus/TransparentNormal");
        private static readonly Shader TransparentNormalLayeredOuterShader = Shader.Find("ForgePlus/TransparentNormalLayeredOuter");

        // Landscape
        private static readonly Shader OpaqueLandscapeShader = Shader.Find("ForgePlus/OpaqueLandscape");

        // Media (could be Normal, but I like the added ripple effect)
        private static readonly Shader MediaShader = Shader.Find("ForgePlus/Media");

        // No assignment
        private static readonly Material UnassignedMaterial = new Material(Shader.Find("ForgePlus/Unassigned"));

        private static readonly Texture2D GridTexture = Resources.Load<Texture2D>("Walls/Grid");

        private static readonly int mediaSubColorPropertyId = Shader.PropertyToID("_SubMediaColor");

        private static readonly Dictionary<ShapeDescriptor, Texture2D> Textures = new Dictionary<ShapeDescriptor, Texture2D>(255);

        private static readonly Dictionary<ShapeDescriptor, int> TextureUsageCounter = new Dictionary<ShapeDescriptor, int>();

        private static readonly Dictionary<ShapeDescriptor, Material> Materials = new Dictionary<ShapeDescriptor, Material>(255);
        private static readonly Dictionary<ShapeDescriptor, Material> TransparentMaterials = new Dictionary<ShapeDescriptor, Material>(100);
        private static readonly Dictionary<ShapeDescriptor, Material> TransparentLayeredOuterMaterials = new Dictionary<ShapeDescriptor, Material>(100);
        private static readonly Dictionary<ShapeDescriptor, Material> LandscapeMaterials = new Dictionary<ShapeDescriptor, Material>(4);
        private static readonly Dictionary<ShapeDescriptor, Material> MediaMaterials = new Dictionary<ShapeDescriptor, Material>(5);

        public static IDictionary<ShapeDescriptor, Texture2D> GetAllLoadedTextures()
        {
            return Textures;
        }

        public static Texture2D GetTexture(ShapeDescriptor shapeDescriptor, bool returnPlaceholderIfNotFound = false)
        {
            Texture2D textureToUse;
            if (Textures.ContainsKey(shapeDescriptor))
            {
                textureToUse = Textures[shapeDescriptor];
            }
            else
            {
                textureToUse = ShapesLoading.Instance.GetShape(shapeDescriptor);

                if (textureToUse)
                {
                    textureToUse.name = $"Collection({shapeDescriptor.Collection}) Bitmap({shapeDescriptor.Bitmap})";
                    Textures[shapeDescriptor] = textureToUse;
                }
            }

            if (textureToUse == null && returnPlaceholderIfNotFound)
            {
                textureToUse = GridTexture;
            }

            return textureToUse;
        }

        public static bool GetTextureIsInUse(ShapeDescriptor shapeDescriptor)
        {
            return TextureUsageCounter.ContainsKey(shapeDescriptor);
        }

        public static Material GetMaterial(
            ShapeDescriptor shapeDescriptor,
            short transferMode,
            bool isOpaqueSurface,
            SurfaceTypes surfaceType,
            bool incrementUsageCounter)
        {
            if (!shapeDescriptor.IsEmpty())
            {
                var landscapeTransferMode = transferMode == 9 || shapeDescriptor.UsesLandscapeCollection();

                if (TextureUsageCounter.ContainsKey(shapeDescriptor))
                {
                    if (incrementUsageCounter)
                    {
                        TextureUsageCounter[shapeDescriptor]++;
                    }
                }
                else
                {
                    TextureUsageCounter[shapeDescriptor] = 1;
                }

                return GetTrackedMaterial(shapeDescriptor,
                                          landscapeTransferMode,
                                          isOpaqueSurface,
                                          surfaceType);
            }
            else
            {
                return UnassignedMaterial;
            }
        }

        public static void DecrementTextureUsage(ShapeDescriptor shapeDescriptor)
        {
            if (shapeDescriptor.IsEmpty())
            {
                return;
            }

            if (TextureUsageCounter.ContainsKey(shapeDescriptor))
            {
                TextureUsageCounter[shapeDescriptor]--;

                if (TextureUsageCounter[shapeDescriptor] <= 0)
                {
                    TextureUsageCounter.Remove(shapeDescriptor);
                }
            }
        }

        public static void ClearCollection()
        {
            TextureUsageCounter.Clear();

            ClearMaterials(Materials);
            ClearMaterials(TransparentMaterials);
            ClearMaterials(TransparentLayeredOuterMaterials);
            ClearMaterials(MediaMaterials);
            ClearMaterials(LandscapeMaterials);

            foreach (var texturesKey in Textures.Keys)
            {
                Object.Destroy(Textures[texturesKey]);
            }

            Textures.Clear();
        }

        private static void ClearMaterials(IDictionary<ShapeDescriptor, Material> materials)
        {
            // Don't actually clear the Materials list,
            // just clear their textures so the Materials can be reused
            foreach (var material in materials.Values)
            {
                material.mainTexture = null;
            }
        }

        private static Material GetTrackedMaterial(
            ShapeDescriptor shapeDescriptor,
            bool landscapeTransferMode,
            bool isOpaqueSurface,
            SurfaceTypes surfaceType)
        {
            var textureToUse = GetTexture(shapeDescriptor, returnPlaceholderIfNotFound: true);

            if (surfaceType == SurfaceTypes.Media)
            {
                return GetTrackedMaterial(shapeDescriptor, textureToUse, MediaShader, MediaMaterials);
            }
            else if (landscapeTransferMode)
            {
                return GetTrackedMaterial(shapeDescriptor, textureToUse, OpaqueLandscapeShader, LandscapeMaterials);
            }
            else
            {
                if (isOpaqueSurface ||
                    textureToUse.format != TextureFormat.ARGB32)
                {
                    return GetTrackedMaterial(shapeDescriptor, textureToUse, OpaqueWithAlphaAlphaNormalShader, Materials);
                }
                else
                {
                    if (surfaceType == SurfaceTypes.LayeredTransparentOuter)
                    {
                        return GetTrackedMaterial(shapeDescriptor, textureToUse, TransparentNormalLayeredOuterShader, TransparentLayeredOuterMaterials);
                    }
                    else
                    {
                        return GetTrackedMaterial(shapeDescriptor, textureToUse, TransparentNormalShader, TransparentMaterials);
                    }
                }
            }
        }

        private static Material GetTrackedMaterial(ShapeDescriptor shapeDescriptor, Texture2D textureToUse, Shader shaderToUse, IDictionary<ShapeDescriptor, Material> trackedMaterials)
        {
            Material material;
            if (trackedMaterials.ContainsKey(shapeDescriptor))
            {
                material = trackedMaterials[shapeDescriptor];
            }
            else
            {
                material = new Material(shaderToUse);
                trackedMaterials[shapeDescriptor] = material;

                if (shaderToUse == MediaShader)
                {
                    var fastAverageTextureColor = textureToUse.GetPixelBilinear(0f, 0f);
                    fastAverageTextureColor += textureToUse.GetPixelBilinear(0.25f, 0.75f);
                    fastAverageTextureColor += textureToUse.GetPixelBilinear(0.66f, 0.33f);
                    fastAverageTextureColor *= 1f / 3f;

                    Color.RGBToHSV(fastAverageTextureColor, out float hue, out float saturation, out _);
                    fastAverageTextureColor = Color.HSVToRGB(hue, saturation, 1f);

                    material.SetColor(mediaSubColorPropertyId, fastAverageTextureColor);
                }
            }

            if (material.mainTexture != textureToUse)
            {
                material.mainTexture = textureToUse;

                material.name = textureToUse.name;
            }

            return material;
        }
    }
}