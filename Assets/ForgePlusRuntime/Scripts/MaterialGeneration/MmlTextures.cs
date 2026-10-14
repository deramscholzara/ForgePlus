using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RuntimeCore.Materials
{
    // Substitute images for walls and sprites, which MML gives (<opengl><texture>), as Aleph One's OpenGL renderer uses
    // them (OGL_Textures.cpp): for a shape's bitmap in a collection and CLUT, its normal image (with its normal mask as
    // its opacity, if it has one). Glow images, bump maps (offset images) and the opacity and blending options aren't
    // shown.
    [NoAutoStaticsCleanup]
    public static class MmlTextures
    {
        // Wall textures, by their normal image and mask (null for images that couldn't be loaded)
        private static readonly Dictionary<(string Image, string Mask, bool IsLandscape), Texture2D> wallTextures = new Dictionary<(string Image, string Mask, bool IsLandscape), Texture2D>();

        // A wall, floor, ceiling, media or landscape texture's substitute, if MML gives one
        public static bool TryGetWallTexture(ushort shapeDescriptor, out Texture2D texture)
        {
            texture = null;

            if (!TryGetOptions(shapeDescriptor, out var options))
            {
                return false;
            }

            var isLandscape = shapeDescriptor.UsesLandscapeCollection();
            var key = (options.NormalColors.GetPath(), options.NormalMask.GetPath(), isLandscape);
            if (!wallTextures.TryGetValue(key, out texture))
            {
                texture = LoadWallTexture(options, isLandscape);
                wallTextures[key] = texture;
            }

            return texture;
        }

        // A sprite frame's substitute, bottom row first, mirrored as the shape is (flags are its low-level shape's)
        public static bool TryGetSpritePixels(byte collection, byte clut, short bitmapIndex, ushort flags, out Color32[] pixels, out int width, out int height)
        {
            pixels = null;
            width = 0;
            height = 0;

            if (!TryGetOptions(collection, clut, bitmapIndex, out var options))
            {
                return false;
            }

            var image = LoadImage(options);
            if (!image)
            {
                return false;
            }

            try
            {
                width = image.width;
                height = image.height;
                pixels = image.GetPixels32();
            }
            finally
            {
                Object.Destroy(image);
            }

            var xMirrored = (flags & collection_definition._X_MIRRORED_BIT) != 0;
            var yMirrored = (flags & collection_definition._Y_MIRRORED_BIT) != 0;
            if (xMirrored || yMirrored)
            {
                var mirrored = new Color32[pixels.Length];
                for (var y = 0; y < height; y++)
                {
                    var sourceY = yMirrored ? height - 1 - y : y;
                    for (var x = 0; x < width; x++)
                    {
                        var sourceX = xMirrored ? width - 1 - x : x;
                        mirrored[y * width + x] = pixels[sourceY * width + sourceX];
                    }
                }

                pixels = mirrored;
            }

            return true;
        }

        public static bool IsSubstitute(Texture2D texture)
        {
            return texture && wallTextures.ContainsValue(texture);
        }

        // When the substitutions change (as other MML applies)
        public static void Clear()
        {
            foreach (var texture in wallTextures.Values)
            {
                if (texture)
                {
                    Object.Destroy(texture);
                }
            }

            wallTextures.Clear();
        }

        // For Fast Enter Play Mode, where statics survive between Play sessions
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            wallTextures.Clear();
        }

        private static bool TryGetOptions(ushort shapeDescriptor, out OGL_TextureOptions options)
        {
            options = null;

            var collection = (short) shapeDescriptor.GetCollection();
            var lowLevelShape = shapes.get_low_level_shape_definition(collection, (short) shapeDescriptor.GetShape());

            return lowLevelShape != null &&
                   TryGetOptions((byte) collection, (byte) shapeDescriptor.GetCLUT(), lowLevelShape.bitmap_index, out options);
        }

        private static bool TryGetOptions(byte collection, byte clut, short bitmapIndex, out OGL_TextureOptions options)
        {
            options = null;

            if (collection >= shape_descriptors.NUMBER_OF_COLLECTIONS || bitmapIndex < 0)
            {
                return false;
            }

            options = OGL_Subst_Texture_Def.OGL_GetTextureOptions(collection, clut, bitmapIndex);

            // A texture must have a normal colored part (OGL_TextureOptionsBase::Load)
            return !string.IsNullOrEmpty(options.NormalColors.GetPath()) && options.NormalColors.Exists();
        }

        private static Texture2D LoadWallTexture(OGL_TextureOptions options, bool isLandscape)
        {
            var image = LoadImage(options);
            if (!image)
            {
                return null;
            }

            try
            {
                var texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, mipChain: true)
                {
                    name = Path.GetFileName(options.NormalColors.GetPath()),
                    filterMode = FilterMode.Trilinear,
                };

                texture.SetPixels32(image.GetPixels32());

                if (isLandscape)
                {
                    texture.wrapModeV = TextureWrapMode.Clamp;
                }

                if (MathUtilities.IsPowerOfTwo(texture.width) && MathUtilities.IsPowerOfTwo(texture.height))
                {
                    texture.Apply(updateMipmaps: true, makeNoLongerReadable: false);
                    texture.Compress(highQuality: true);
                    texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                }
                else
                {
                    texture.Apply(updateMipmaps: true, makeNoLongerReadable: true);
                }

                return texture;
            }
            finally
            {
                Object.Destroy(image);
            }
        }

        // The normal image, readable, with its mask as its opacity (ImageLoader_SDL.cpp: a mask of another size isn't
        // used, and its opacity is its red, green and blue's average). Null if it can't be loaded.
        private static Texture2D LoadImage(OGL_TextureOptions options)
        {
            var image = LoadImageFile(options.NormalColors.GetPath());
            if (!image)
            {
                Debug.LogWarning($"MML: the substitute image \"{options.NormalColors.GetPath()}\" couldn't be loaded.");
                return null;
            }

            var maskPath = options.NormalMask.GetPath();
            if (!string.IsNullOrEmpty(maskPath) && options.NormalMask.Exists())
            {
                var mask = LoadImageFile(maskPath);
                if (mask && mask.width == image.width && mask.height == image.height)
                {
                    var pixels = image.GetPixels32();
                    var maskPixels = mask.GetPixels32();
                    for (var i = 0; i < pixels.Length; i++)
                    {
                        var opacity = (maskPixels[i].r + maskPixels[i].g + maskPixels[i].b) / 3f;
                        pixels[i].a = (byte) Mathf.Clamp((int) (opacity + 0.5f), 0, 255);
                    }

                    image.SetPixels32(pixels);
                    image.Apply(updateMipmaps: false);
                }

                if (mask)
                {
                    Object.Destroy(mask);
                }
            }

            return image;
        }

        // A readable RGBA image, bottom row first (as Unity textures are)
        private static Texture2D LoadImageFile(string path)
        {
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return null;
            }

            if (DdsImage.IsDds(bytes))
            {
                return DdsImage.Load(bytes);
            }

            var image = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            if (!image.LoadImage(bytes, markNonReadable: false))
            {
                Object.Destroy(image);
                return null;
            }

            if (image.format != TextureFormat.RGBA32)
            {
                var converted = new Texture2D(image.width, image.height, TextureFormat.RGBA32, mipChain: false);
                converted.SetPixels32(image.GetPixels32());
                converted.Apply(updateMipmaps: false);
                Object.Destroy(image);
                image = converted;
            }

            return image;
        }
    }
}
