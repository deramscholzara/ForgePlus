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
    // its opacity, if it has one). Walls also have their bump map (offset image, with its height as its opacity) and
    // glow image (with its glow mask as its opacity), which, as in Aleph One (OGL_Setup.cpp:
    // OGL_TextureOptionsBase::Load), are only used with a normal image, and a glow image only at the normal image's size.
    // The opacity and blending options aren't shown.
    [NoAutoStaticsCleanup]
    public static class MmlTextures
    {
        // Wall textures, by their normal image and mask (null for images that couldn't be loaded)
        private static readonly Dictionary<(string Image, string Mask, bool IsLandscape), Texture2D> wallTextures = new Dictionary<(string Image, string Mask, bool IsLandscape), Texture2D>();

        // Wall bump maps, by their offset image (null for images that couldn't be loaded)
        private static readonly Dictionary<string, Texture2D> bumpMaps = new Dictionary<string, Texture2D>();

        // Wall glow maps, by their glow image, mask and whether they're premultiplied (null for images that couldn't be
        // loaded)
        private static readonly Dictionary<(string Image, string Mask, bool IsPremultiplied), Texture2D> glowMaps = new Dictionary<(string Image, string Mask, bool IsPremultiplied), Texture2D>();

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

        // A wall texture's substitute's bump map (normal in its color, height in its opacity), if MML gives one
        public static bool TryGetWallBumpMap(ushort shapeDescriptor, out Texture2D bumpMap)
        {
            bumpMap = null;

            if (!TryGetOptions(shapeDescriptor, out var options) || !TryGetWallTexture(shapeDescriptor, out _))
            {
                return false;
            }

            var path = options.OffsetMap.GetPath();
            if (string.IsNullOrEmpty(path) || !options.OffsetMap.Exists())
            {
                return false;
            }

            if (!bumpMaps.TryGetValue(path, out bumpMap))
            {
                bumpMap = LoadBumpMap(path);
                bumpMaps[path] = bumpMap;
            }

            return bumpMap;
        }

        // A wall texture's substitute's glow map (its color times its opacity glows), if MML gives one at the substitute's
        // size
        public static bool TryGetWallGlowMap(ushort shapeDescriptor, out Texture2D glowMap)
        {
            glowMap = null;

            if (!TryGetOptions(shapeDescriptor, out var options) || !TryGetWallTexture(shapeDescriptor, out var texture))
            {
                return false;
            }

            var path = options.GlowColors.GetPath();
            if (string.IsNullOrEmpty(path) || !options.GlowColors.Exists())
            {
                return false;
            }

            var key = (path, options.GlowMask.GetPath(), options.GlowIsPremultiplied);
            if (!glowMaps.TryGetValue(key, out glowMap))
            {
                glowMap = LoadGlowMap(options);
                glowMaps[key] = glowMap;
            }

            if (glowMap && (glowMap.width != texture.width || glowMap.height != texture.height))
            {
                return false;
            }

            return glowMap;
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
            DestroyAll(wallTextures.Values);
            DestroyAll(bumpMaps.Values);
            DestroyAll(glowMaps.Values);

            wallTextures.Clear();
            bumpMaps.Clear();
            glowMaps.Clear();
        }

        // For Fast Enter Play Mode, where statics survive between Play sessions
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            wallTextures.Clear();
            bumpMaps.Clear();
            glowMaps.Clear();
        }

        private static void DestroyAll(IEnumerable<Texture2D> textures)
        {
            foreach (var texture in textures)
            {
                if (texture)
                {
                    Object.Destroy(texture);
                }
            }
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
                return CreateWallTexture(image, options.NormalColors.GetPath(), isLinear: false, isLandscape);
            }
            finally
            {
                Object.Destroy(image);
            }
        }

        // Linear, as its color is a direction rather than a color, and its opacity (height) is kept
        private static Texture2D LoadBumpMap(string path)
        {
            var image = LoadImageFile(path);
            if (!image)
            {
                Debug.LogWarning($"MML: the offset image \"{path}\" couldn't be loaded.");
                return null;
            }

            try
            {
                return CreateWallTexture(image, path, isLinear: true, isLandscape: false);
            }
            finally
            {
                Object.Destroy(image);
            }
        }

        private static Texture2D LoadGlowMap(OGL_TextureOptions options)
        {
            var image = LoadImage(options.GlowColors, options.GlowMask, "glow image");
            if (!image)
            {
                return null;
            }

            try
            {
                // A premultiplied image's color already has its opacity applied, so it glows at full opacity
                if (options.GlowIsPremultiplied)
                {
                    var pixels = image.GetPixels32();
                    for (var i = 0; i < pixels.Length; i++)
                    {
                        pixels[i].a = byte.MaxValue;
                    }

                    image.SetPixels32(pixels);
                }

                return CreateWallTexture(image, options.GlowColors.GetPath(), isLinear: false, isLandscape: false);
            }
            finally
            {
                Object.Destroy(image);
            }
        }

        // Mipmapped, and compressed where its size allows
        private static Texture2D CreateWallTexture(Texture2D image, string path, bool isLinear, bool isLandscape)
        {
            var texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, mipChain: true, linear: isLinear)
            {
                name = Path.GetFileName(path),
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

        // The normal image, with its mask as its opacity. Null if it can't be loaded.
        private static Texture2D LoadImage(OGL_TextureOptions options)
        {
            return LoadImage(options.NormalColors, options.NormalMask, "substitute image");
        }

        // The image, readable, with its mask as its opacity (ImageLoader_SDL.cpp: a mask of another size isn't used, and
        // its opacity is its red, green and blue's average). Null if it can't be loaded.
        private static Texture2D LoadImage(FileSpecifier colors, FileSpecifier maskFile, string description)
        {
            var image = LoadImageFile(colors.GetPath());
            if (!image)
            {
                Debug.LogWarning($"MML: the {description} \"{colors.GetPath()}\" couldn't be loaded.");
                return null;
            }

            var maskPath = maskFile.GetPath();
            if (!string.IsNullOrEmpty(maskPath) && maskFile.Exists())
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
