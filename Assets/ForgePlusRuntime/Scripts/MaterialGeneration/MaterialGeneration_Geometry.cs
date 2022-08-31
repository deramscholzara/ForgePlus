using ForgePlus.DataFileIO;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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

#if USE_TEXTURE_ARRAYS
        public struct Color24
        {
            public byte r;
            public byte g;
            public byte b;
        }

        private readonly struct Texture2DArrayCollectionKey
        {
            public readonly int Width, Height;
            public readonly TextureFormat Format;
            public readonly bool HasMipMaps;
            public readonly uint ShapeCollection;
            public readonly bool UsedForMedia;
            public readonly bool IsForOpaqueSurface;

            public Texture2DArrayCollectionKey(ShapeDescriptor shapeDescriptor, bool usedForMedia, bool isForOpaqueSurface)
            {
                var texture = GetTexture(shapeDescriptor, returnPlaceholderIfNotFound: true);
                // TODO: should unload this texture when done adding it to the array, if NO_EDITING

                Width = texture.width;
                Height = texture.height;
                Format = texture.format;
                HasMipMaps = texture.mipmapCount > 1;

                // Treat all landscapes as part of the same collection,
                // since it's not common to have over 255 landscape bitmaps total
                ShapeCollection = shapeDescriptor.UsesLandscapeCollection() ? (uint) 27 : shapeDescriptor.Collection;

                // Treat all medias as part of the same collection,
                // since it's not common to have over 255 media bitmaps total,
                // and the "tint" effect for the shader is controlled by vertex color (TODO: actually set this up)
                UsedForMedia = usedForMedia;
                if (usedForMedia)
                {
                    ShapeCollection = byte.MaxValue + 1;
                }

                IsForOpaqueSurface = isForOpaqueSurface;
            }
        }

        private class Texture2DArrayCollection
        {
            public readonly Material SharedMaterial;
            private Texture2DArray textureArray;
            private int width, height;
            private TextureFormat format;
            private bool hasMipLevels;
            private Dictionary<ShapeDescriptor, int> indicesByShapeDescriptor;
            private List<Material> uniqueMaterials;

            public Texture2DArrayCollection(
                ShapeDescriptor firstShapeDescriptor,
                Material sharedMaterial,
                int width,
                int height,
                TextureFormat format,
                bool hasMipLevels)
            {
                this.SharedMaterial = sharedMaterial;
                this.width = width;
                this.height = height;
                this.format = format;
                this.hasMipLevels = hasMipLevels;
                indicesByShapeDescriptor = new Dictionary<ShapeDescriptor, int>();

                AddBitmap(firstShapeDescriptor);
            }

            public void SubscribeUniqueMaterial(Material material)
            {
                material.mainTexture = textureArray;

                if (uniqueMaterials == null)
                    uniqueMaterials = new List<Material>();

                if (!uniqueMaterials.Contains(material))
                    uniqueMaterials.Add(material);
            }

            public void UnsubscribeUniqueMaterial(Material material)
            {
                if (uniqueMaterials != null &&
                    uniqueMaterials.Contains(material))
                    uniqueMaterials.Remove(material);
            }

            public void AddBitmap(ShapeDescriptor shapeDescriptor)
            {
                if (!indicesByShapeDescriptor.ContainsKey(shapeDescriptor))
                {
                    indicesByShapeDescriptor[shapeDescriptor] = indicesByShapeDescriptor.Count;
                }
            }

            public int GetBitmapIndex(ShapeDescriptor shapeDescriptor)
            {
                return indicesByShapeDescriptor[shapeDescriptor];
            }

            public void Apply()
            {
                if (textureArray)
                {
                    Object.Destroy(textureArray);
                }

                // Build Expanded Texture Array
                textureArray = new Texture2DArray(
                    width: width,
                    height: height,
                    depth: indicesByShapeDescriptor.Count,
                    textureFormat: format,
                    mipChain: hasMipLevels,
                    linear: false);

                textureArray.filterMode = FilterMode.Point;
                textureArray.name = "Dynamic Texture Array";

                foreach (var descriptorIndexPair in indicesByShapeDescriptor)
                {
                    var texture = GetTexture(descriptorIndexPair.Key, returnPlaceholderIfNotFound: true);
                    textureArray.filterMode = texture.filterMode;

                    for (var mipIndex = 0; mipIndex < texture.mipmapCount; mipIndex++)
                    {
                        Graphics.CopyTexture(
                            texture, 0, mipIndex,
                            textureArray, descriptorIndexPair.Value, mipIndex);
                    }

#if NO_EDITING
                    // TODO: should unload "texture" when done adding it to the array, if NO_EDITING
#endif
                }

                SharedMaterial.mainTexture = textureArray;

                foreach (var uniqueMaterial in uniqueMaterials)
                {
                    uniqueMaterial.mainTexture = textureArray;
                }
            }
        }

        public static bool TextureArraysArePopulating = false;
#endif

        // Normal
#if USE_TEXTURE_ARRAYS
        private static readonly Shader OpaqueWithAlphaAlphaNormalShader = Shader.Find("ForgePlus/OpaqueWithAlphaNormal(Arrays)");
        private static readonly Shader TransparentNormalShader = Shader.Find("ForgePlus/TransparentNormal(Arrays)");
        private static readonly Shader TransparentNormalLayeredOuterShader = Shader.Find("ForgePlus/TransparentNormalLayeredOuter(Arrays)");
#else
        private static readonly Shader OpaqueWithAlphaAlphaNormalShader = Shader.Find("ForgePlus/OpaqueWithAlphaNormal");
        private static readonly Shader TransparentNormalShader = Shader.Find("ForgePlus/TransparentNormal");
        private static readonly Shader TransparentNormalLayeredOuterShader = Shader.Find("ForgePlus/TransparentNormalLayeredOuter");
#endif

        // Landscape
#if USE_TEXTURE_ARRAYS
        private static readonly Shader OpaqueLandscapeShader = Shader.Find("ForgePlus/OpaqueLandscape(Arrays)");
#else
        private static readonly Shader OpaqueLandscapeShader = Shader.Find("ForgePlus/OpaqueLandscape");
#endif

        // Media (could be Normal, but I like the added ripple effect)
#if USE_TEXTURE_ARRAYS
        private static readonly Shader MediaShader = Shader.Find("ForgePlus/Media(Arrays)");
#else
        private static readonly Shader MediaShader = Shader.Find("ForgePlus/Media");
#endif

        // No assignment
        private static readonly Material UnassignedMaterial = new Material(Shader.Find("ForgePlus/Unassigned"));

        private static readonly Texture2D GridTexture = Resources.Load<Texture2D>("Walls/Grid");

        private static readonly int mediaSubColorPropertyId = Shader.PropertyToID("_SubMediaColor");

#if USE_TEXTURE_ARRAYS
        private static readonly int textureArrayIndexPropertyId = Shader.PropertyToID("_TextureArrayIndex");
#endif

        // TODO: Convert Textures to use TextureSet (renamed from PluginTextureSet)
        //       and make PluginLoading_Texture use this.Textures instead of its "TextureLookup"
        private static readonly Dictionary<ShapeDescriptor, Texture2D> Textures = new Dictionary<ShapeDescriptor, Texture2D>(255);
        private static readonly Dictionary<ShapeDescriptor, int> TextureUsageCounter = new Dictionary<ShapeDescriptor, int>();

#if USE_TEXTURE_ARRAYS
        private static readonly Dictionary<Texture2DArrayCollectionKey, Texture2DArrayCollection> Texture2DArrays = new Dictionary<Texture2DArrayCollectionKey, Texture2DArrayCollection>(14);

        private static readonly Dictionary<ShapeDescriptor, Texture2DArrayCollectionKey> Texture2DArrayKeys = new Dictionary<ShapeDescriptor, Texture2DArrayCollectionKey>(255);
        private static readonly Dictionary<ShapeDescriptor, Texture2DArrayCollectionKey> TransparentTexture2DArrayKeys = new Dictionary<ShapeDescriptor, Texture2DArrayCollectionKey>(100);
        private static readonly Dictionary<ShapeDescriptor, Texture2DArrayCollectionKey> TransparentLayeredOuterTexture2DArrayKeys = new Dictionary<ShapeDescriptor, Texture2DArrayCollectionKey>(100);
        private static readonly Dictionary<ShapeDescriptor, Texture2DArrayCollectionKey> LandscapeTexture2DArrayKeys = new Dictionary<ShapeDescriptor, Texture2DArrayCollectionKey>(4);
        private static readonly Dictionary<ShapeDescriptor, Texture2DArrayCollectionKey> MediaTexture2DArrayKeys = new Dictionary<ShapeDescriptor, Texture2DArrayCollectionKey>(5);
#else
        private static readonly Dictionary<ShapeDescriptor, Material> Materials = new Dictionary<ShapeDescriptor, Material>(255);
        private static readonly Dictionary<ShapeDescriptor, Material> TransparentMaterials = new Dictionary<ShapeDescriptor, Material>(100);
        private static readonly Dictionary<ShapeDescriptor, Material> TransparentLayeredOuterMaterials = new Dictionary<ShapeDescriptor, Material>(100);
        private static readonly Dictionary<ShapeDescriptor, Material> LandscapeMaterials = new Dictionary<ShapeDescriptor, Material>(4);
        private static readonly Dictionary<ShapeDescriptor, Material> MediaMaterials = new Dictionary<ShapeDescriptor, Material>(5);
#endif

        public static IDictionary<ShapeDescriptor, Texture2D> GetAllLoadedTextures()
        {
            return Textures;
        }

        public static Texture2D GetTexture(ShapeDescriptor shapeDescriptor, bool returnPlaceholderIfNotFound = false)
        {
            if (PluginLoading_Texture.Instance.PluginSupportEnabled &&
                PluginLoading_Texture.Instance.TextureLookup.ContainsKey(shapeDescriptor))
            {
                if (PluginLoading_Texture.Instance.TextureLookup[shapeDescriptor].MainTexture)
                    return PluginLoading_Texture.Instance.TextureLookup[shapeDescriptor].MainTexture;
            }
            else if (Textures.ContainsKey(shapeDescriptor))
            {
                return Textures[shapeDescriptor];
            }
            else
            {
                var textureToUse = ShapesLoading.Instance.GetShape(shapeDescriptor);

                if (textureToUse)
                {
                    textureToUse.name = $"Collection({shapeDescriptor.Collection}) Bitmap({shapeDescriptor.Bitmap})";
                    Textures[shapeDescriptor] = textureToUse;
                }
                else if (returnPlaceholderIfNotFound)
                {
                    textureToUse = GridTexture;
                }

                return textureToUse;
            }

            return null;
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

                var landscapeTransferMode = transferMode == 9 || shapeDescriptor.UsesLandscapeCollection();

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

#if USE_TEXTURE_ARRAYS
            Texture2DArrays.Clear();

            Texture2DArrayKeys.Clear();
            TransparentTexture2DArrayKeys.Clear();
            TransparentLayeredOuterTexture2DArrayKeys.Clear();
            MediaTexture2DArrayKeys.Clear();
            LandscapeTexture2DArrayKeys.Clear();
#else
            ClearMaterials(Materials);
            ClearMaterials(TransparentMaterials);
            ClearMaterials(TransparentLayeredOuterMaterials);
            ClearMaterials(MediaMaterials);
            ClearMaterials(LandscapeMaterials);
#endif

            foreach (var texturesKey in Textures.Keys)
            {
                Object.Destroy(Textures[texturesKey]);
            }

            // TODO: Make plugin texture loading on-demand based on loaded plugin data,
            //       similar to ShapesLoading.Instance.GetShape (see PluginLoading_Textures notes),
            //       then TextureLookup, or Textures (using TextureSet) can safely be destroyed and cleared here.

            Textures.Clear();
        }

#if USE_TEXTURE_ARRAYS
        public static void ApplyTextureArrays()
        {
            foreach (var array in Texture2DArrays.Values)
            {
                array.Apply();
            }
        }

        public static int GetTextureArrayIndex(
            ShapeDescriptor shapeDescriptor,
            short transferMode,
            bool isOpaqueSurface,
            SurfaceTypes surfaceType)
        {
            if (shapeDescriptor.IsEmpty())
            {
                return 0;
            }

            var landscapeTransferMode = transferMode == 9 || shapeDescriptor.UsesLandscapeCollection();

            var collectionKey =
                GetTexture2DArrayKeyDictionary(
                        shapeDescriptor,
                        landscapeTransferMode: landscapeTransferMode,
                        isOpaqueSurface: isOpaqueSurface,
                        surfaceType)
                    [shapeDescriptor];

            var collection = Texture2DArrays[collectionKey];

            return collection.GetBitmapIndex(shapeDescriptor);
        }

        public static void SubscribeUniqueMaterial(
            Material uniqueMaterial,
            Material sharedMaterial)
        {
            var matchingCollections =
                Texture2DArrays.Values.Where(
                    collection => collection.SharedMaterial == sharedMaterial);

            foreach (var collection in matchingCollections)
            {
                collection.SubscribeUniqueMaterial(uniqueMaterial);
            }
        }

        public static void UnsubscribeUniqueMaterial(Material uniqueMaterial)
        {
            foreach (var texture2DArray in Texture2DArrays.Values)
            {
                texture2DArray.UnsubscribeUniqueMaterial(uniqueMaterial);
            }
        }
#else
        private static void ClearMaterials(IDictionary<ShapeDescriptor, Material> materials)
        {
            // Don't actually clear the Materials list,
            // just clear their textures so the Materials can be reused
            foreach (var material in materials.Values)
            {
                material.mainTexture = null;
            }
        }
#endif

#if USE_TEXTURE_ARRAYS
        private static Dictionary<ShapeDescriptor, Texture2DArrayCollectionKey> GetTexture2DArrayKeyDictionary(
            ShapeDescriptor shapeDescriptor,
            bool landscapeTransferMode,
            bool isOpaqueSurface,
            SurfaceTypes surfaceType)
        {
            // TODO: where textures are -first- loaded, populate a Dictionary<ShapeDescriptor, TextureFormat>
            //    so this can be looked up without loading a texture (important for NO_EDITING)
            var textureToUse = GetTexture(shapeDescriptor, returnPlaceholderIfNotFound: true);

            if (surfaceType == SurfaceTypes.Media)
            {
                return MediaTexture2DArrayKeys;
            }
            else if (landscapeTransferMode)
            {
                return LandscapeTexture2DArrayKeys;
            }
            else
            {
                if (isOpaqueSurface ||
                    (textureToUse.format != TextureFormat.ARGB32 &&
                     textureToUse.format != TextureFormat.DXT5))
                {
                    return Texture2DArrayKeys;
                }
                else
                {
                    if (surfaceType == SurfaceTypes.LayeredTransparentOuter)
                    {
                        return TransparentLayeredOuterTexture2DArrayKeys;
                    }
                    else
                    {
                        return TransparentTexture2DArrayKeys;
                    }
                }
            }
        }
#endif

        private static Material GetTrackedMaterial(
            ShapeDescriptor shapeDescriptor,
            bool landscapeTransferMode,
            bool isOpaqueSurface,
            SurfaceTypes surfaceType)
        {
#if USE_TEXTURE_ARRAYS
            var collectionKeyDictionary = GetTexture2DArrayKeyDictionary(
                shapeDescriptor,
                landscapeTransferMode: landscapeTransferMode,
                isOpaqueSurface: isOpaqueSurface,
                surfaceType);

            if (collectionKeyDictionary.ContainsKey(shapeDescriptor))
            {
                var collectionKey = collectionKeyDictionary[shapeDescriptor];
                var texture2DArrayCollection = Texture2DArrays[collectionKey];
                return texture2DArrayCollection.SharedMaterial;
            }
#endif

            var textureToUse = GetTexture(shapeDescriptor, returnPlaceholderIfNotFound: true);

#if USE_TEXTURE_ARRAYS
            // TODO: Figure out why transparent surfaces are using the normal opaque shader
            //       load Poor Yorick and look at the waterfall near the start... 

            var texture2DArrayCollectionKey = new Texture2DArrayCollectionKey(
                shapeDescriptor,
                surfaceType == SurfaceTypes.Media,
                isOpaqueSurface ||
                (textureToUse.format != TextureFormat.ARGB32 && textureToUse.format != TextureFormat.DXT5));
            collectionKeyDictionary[shapeDescriptor] = texture2DArrayCollectionKey;

            if (Texture2DArrays.ContainsKey(texture2DArrayCollectionKey))
            {
                var texture2DArrayCollection = Texture2DArrays[texture2DArrayCollectionKey];
                texture2DArrayCollection.AddBitmap(shapeDescriptor);
                return texture2DArrayCollection.SharedMaterial;
            }
#endif

            Material material;

            if (surfaceType == SurfaceTypes.Media)
            {
#if USE_TEXTURE_ARRAYS
                material = new Material(MediaShader);
#else
                material = GetTrackedMaterial(shapeDescriptor, textureToUse, MediaShader, MediaMaterials);
#endif
            }
            else if (landscapeTransferMode)
            {
#if USE_TEXTURE_ARRAYS
                material = new Material(OpaqueLandscapeShader);
#else
                material = GetTrackedMaterial(shapeDescriptor, textureToUse, OpaqueLandscapeShader, LandscapeMaterials);
#endif
            }
            else
            {
                if (isOpaqueSurface ||
                    (textureToUse.format != TextureFormat.ARGB32 &&
                     textureToUse.format != TextureFormat.DXT5))
                {
#if USE_TEXTURE_ARRAYS
                    material = new Material(OpaqueWithAlphaAlphaNormalShader);
#else
                    material = GetTrackedMaterial(shapeDescriptor, textureToUse, OpaqueWithAlphaAlphaNormalShader, Materials);
#endif
                }
                else
                {
                    if (surfaceType == SurfaceTypes.LayeredTransparentOuter)
                    {
#if USE_TEXTURE_ARRAYS
                        material = new Material(TransparentNormalLayeredOuterShader);
#else
                        material = GetTrackedMaterial(shapeDescriptor, textureToUse, TransparentNormalLayeredOuterShader, TransparentLayeredOuterMaterials);
#endif
                    }
                    else
                    {
#if USE_TEXTURE_ARRAYS
                        material = new Material(TransparentNormalShader);
#else
                        material = GetTrackedMaterial(shapeDescriptor, textureToUse, TransparentNormalShader, TransparentMaterials);
#endif
                    }
                }
            }

#if USE_TEXTURE_ARRAYS
            material.name = $"Collection({shapeDescriptor.Collection}) Bitmap({shapeDescriptor.Bitmap})";
            material.enableInstancing = true;

            var newTexture2DArrayCollection = new Texture2DArrayCollection(
                shapeDescriptor,
                material,
                textureToUse.width,
                textureToUse.height,
                textureToUse.format,
                textureToUse.mipmapCount > 0);

            Texture2DArrays[texture2DArrayCollectionKey] = newTexture2DArrayCollection;
#endif

            return material;
        }

#if !USE_TEXTURE_ARRAYS
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
                material.name = $"Collection({shapeDescriptor.Collection}) Bitmap({shapeDescriptor.Bitmap})";
            }

            return material;
        }
#endif
    }
}