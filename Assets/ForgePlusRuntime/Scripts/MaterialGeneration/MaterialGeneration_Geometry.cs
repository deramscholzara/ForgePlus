using ForgePlus.DataFileIO;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using AlephOne;
using ForgePlus.Extensions;

namespace RuntimeCore.Materials
{
    [AutoStaticsCleanup]
    public partial class MaterialGeneration_Geometry
    {
        public enum SurfaceTypes
        {
            Normal,
            Media,
            LayeredTransparentOuter,
        }

        // Normal
        [NoAutoStaticsCleanup] private static readonly Shader OpaqueWithAlphaAlphaNormalShader = Shader.Find("ForgePlus/OpaqueWithAlphaNormal");
        [NoAutoStaticsCleanup] private static readonly Shader TransparentNormalShader = Shader.Find("ForgePlus/TransparentNormal");
        [NoAutoStaticsCleanup] private static readonly Shader TransparentNormalLayeredOuterShader = Shader.Find("ForgePlus/TransparentNormalLayeredOuter");

        // Landscape
        [NoAutoStaticsCleanup] private static readonly Shader OpaqueLandscapeShader = Shader.Find("ForgePlus/OpaqueLandscape");

        // Media (could be Normal, but I like the added ripple effect)
        [NoAutoStaticsCleanup] private static readonly Shader MediaShader = Shader.Find("ForgePlus/Media");

        // No assignment (recreated when destroyed, such as at the end of a Play session)
        private static Material unassignedMaterial;

        private static Material UnassignedMaterial
        {
            get
            {
                if (!unassignedMaterial)
                {
                    unassignedMaterial = new Material(Shader.Find("ForgePlus/Unassigned"));
                }

                return unassignedMaterial;
            }
        }

        [NoAutoStaticsCleanup] private static readonly Texture2D GridTexture = Resources.Load<Texture2D>("Walls/Grid");

        // Landscapes aren't lit by the level's lights (so they're drawn the same whichever light their surface has)
        public static bool IsLandscape(Material sourceMaterial)
        {
            return sourceMaterial && sourceMaterial.shader == OpaqueLandscapeShader;
        }

        // The unassigned material, or one drawn with the grid in place of a texture that's missing
        public static bool IsPlaceholder(Material sourceMaterial)
        {
            return sourceMaterial &&
                   (sourceMaterial == unassignedMaterial ||
                    (sourceMaterial.HasTexture(mainTexturePropertyId) && sourceMaterial.GetTexture(mainTexturePropertyId) == GridTexture));
        }

        [NoAutoStaticsCleanup] private static readonly int mainTexturePropertyId = Shader.PropertyToID("_MainTex");

        [NoAutoStaticsCleanup] private static readonly int mediaSubColorPropertyId = Shader.PropertyToID("_SubMediaColor");

        // MML's bump maps and glow maps for substitute textures (MmlTextures), on the shaders that draw them
        [NoAutoStaticsCleanup] private static readonly int bumpMapPropertyId = Shader.PropertyToID("_BumpMap");
        [NoAutoStaticsCleanup] private static readonly int emissionMapPropertyId = Shader.PropertyToID("_EmissionMap");

        private static readonly Dictionary<ushort, Texture2D> Textures = new Dictionary<ushort, Texture2D>(255);
        private static readonly Dictionary<ushort, int> TextureUsageCounter = new Dictionary<ushort, int>();

        private static readonly Dictionary<ushort, Material> Materials = new Dictionary<ushort, Material>(255);
        private static readonly Dictionary<ushort, Material> TransparentMaterials = new Dictionary<ushort, Material>(100);
        private static readonly Dictionary<ushort, Material> TransparentLayeredOuterMaterials = new Dictionary<ushort, Material>(100);
        private static readonly Dictionary<ushort, Material> LandscapeMaterials = new Dictionary<ushort, Material>(4);
        private static readonly Dictionary<ushort, Material> MediaMaterials = new Dictionary<ushort, Material>(5);

        public static IDictionary<ushort, Texture2D> GetAllLoadedTextures()
        {
            return Textures;
        }

        public static Texture2D GetTexture(ushort shapeDescriptor, bool returnPlaceholderIfNotFound = false)
        {
            if (TryGetLoadedTexture(shapeDescriptor, out var loadedTexture))
            {
                return loadedTexture;
            }

            var textureToUse = ShapesLoading.Instance.GetShape(shapeDescriptor);

            if (textureToUse)
            {
                AddTexture(shapeDescriptor, textureToUse);
            }
            else if (returnPlaceholderIfNotFound)
            {
                textureToUse = GridTexture;
            }

            return textureToUse;
        }

        // Loads these textures in order, as GetTexture would one at a time, but decodes their shapes in parallel and
        // converts their pixels together. A collection's shapes are numbered from 0, so it stops at its first missing texture.
        public static void LoadTextures(IReadOnlyList<ushort> shapeDescriptors)
        {
            if (!ShapesLoading.Instance.TryLoadFile())
            {
                return;
            }

            // Parallel to shapeDescriptors (null for textures that are already loaded, and for missing shapes)
            var preparedShapes = new ShapesFile.PreparedShape[shapeDescriptors.Count];
            var indexesToPrepare = Enumerable.Range(0, shapeDescriptors.Count).Where(i => !TryGetLoadedTexture(shapeDescriptors[i], out _)).ToList();
            var prepared = ShapesFile.PrepareShapes(indexesToPrepare.Select(i => shapeDescriptors[i]).ToList());
            for (var i = 0; i < prepared.Length; i++)
            {
                preparedShapes[indexesToPrepare[i]] = prepared[i];
            }

            try
            {
                var shapesToCreate = new List<ShapesFile.PreparedShape>();
                var descriptorsToCreate = new HashSet<ushort>();
                var stoppedCollections = new HashSet<int>();

                for (var i = 0; i < shapeDescriptors.Count; i++)
                {
                    var shapeDescriptor = shapeDescriptors[i];
                    var collection = shapeDescriptor.GetCollection();
                    if (stoppedCollections.Contains(collection))
                    {
                        continue;
                    }

                    bool found;
                    if (TryGetLoadedTexture(shapeDescriptor, out var loadedTexture))
                    {
                        found = loadedTexture;
                    }
                    else
                    {
                        var preparedShape = preparedShapes[i];
                        found = preparedShape != null;

                        if (found && descriptorsToCreate.Add(shapeDescriptor))
                        {
                            shapesToCreate.Add(preparedShape);
                        }
                    }

                    if (!found)
                    {
                        stoppedCollections.Add(collection);
                    }
                }

                var textures = ShapesFile.CreateTextures(shapesToCreate);
                for (var i = 0; i < textures.Length; i++)
                {
                    AddTexture(shapesToCreate[i].ShapeDescriptor, textures[i]);
                }
            }
            finally
            {
                foreach (var preparedShape in preparedShapes)
                {
                    preparedShape?.Dispose();
                }
            }
        }

        // MML's substitute textures (from the scripts and plugins) are used in place of the shapes' (and listed with them)
        private static bool TryGetLoadedTexture(ushort shapeDescriptor, out Texture2D texture)
        {
            if (Textures.TryGetValue(shapeDescriptor, out texture))
            {
                return true;
            }

            if (MmlTextures.TryGetWallTexture(shapeDescriptor, out texture))
            {
                Textures[shapeDescriptor] = texture;
                return true;
            }

            return false;
        }

        private static void AddTexture(ushort shapeDescriptor, Texture2D texture)
        {
            texture.name = $"Collection({shapeDescriptor.GetCollection()}) Bitmap({shapeDescriptor.GetShape()})";
            Textures[shapeDescriptor] = texture;
        }

        public static bool GetTextureIsInUse(ushort shapeDescriptor)
        {
            return TextureUsageCounter.ContainsKey(shapeDescriptor);
        }

        public static Material GetMaterial(
            ushort shapeDescriptor,
            short transferMode,
            bool isOpaqueSurface,
            SurfaceTypes surfaceType,
            bool incrementUsageCounter)
        {
            if (!shapeDescriptor.IsEmptyShapeDescriptor())
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

                var landscapeTransferMode = AlephOneExtensions.IsLandscapeTransferMode(transferMode) || shapeDescriptor.UsesLandscapeCollection();

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

        public static void DecrementTextureUsage(ushort shapeDescriptor)
        {
            if (shapeDescriptor.IsEmptyShapeDescriptor())
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

            // MML's substitute textures are kept (by MmlTextures) while the MML still gives them
            foreach (var texture in Textures.Values)
            {
                if (!MmlTextures.IsSubstitute(texture))
                {
                    Object.Destroy(texture);
                }
            }

            Textures.Clear();
        }

        private static void ClearMaterials(IDictionary<ushort, Material> materials)
        {
            // Don't actually clear the Materials list,
            // just clear their textures so the Materials can be reused
            foreach (var material in materials.Values)
            {
                material.mainTexture = null;
                ApplyMmlMaps(material, bumpMap: null, glowMap: null);
            }
        }

        // Shaders without the maps are left as they are, and no map gives the shader's default (no bump or glow)
        private static void ApplyMmlMaps(Material material, Texture2D bumpMap, Texture2D glowMap)
        {
            if (material.HasTexture(bumpMapPropertyId))
            {
                material.SetTexture(bumpMapPropertyId, bumpMap);
            }

            if (material.HasTexture(emissionMapPropertyId))
            {
                material.SetTexture(emissionMapPropertyId, glowMap);
            }
        }

        private static Material GetTrackedMaterial(
            ushort shapeDescriptor,
            bool landscapeTransferMode,
            bool isOpaqueSurface,
            SurfaceTypes surfaceType)
        {

            var textureToUse = GetTexture(shapeDescriptor, returnPlaceholderIfNotFound: true);

            Material material;

            var drawsLandscape = landscapeTransferMode;

            if (surfaceType == SurfaceTypes.Media)
            {
                material = GetTrackedMaterial(shapeDescriptor, textureToUse, MediaShader, MediaMaterials);
            }
            else if (drawsLandscape)
            {
                material = GetTrackedMaterial(shapeDescriptor, textureToUse, OpaqueLandscapeShader, LandscapeMaterials);
            }
            else
            {
                if ((isOpaqueSurface &&
                     surfaceType != SurfaceTypes.LayeredTransparentOuter) ||
                    (textureToUse.format != TextureFormat.ARGB32 &&
                     textureToUse.format != TextureFormat.DXT5))
                {
                    material = GetTrackedMaterial(shapeDescriptor, textureToUse, OpaqueWithAlphaAlphaNormalShader, Materials);
                }
                else
                {
                    if (surfaceType == SurfaceTypes.LayeredTransparentOuter)
                    {
                        material = GetTrackedMaterial(shapeDescriptor, textureToUse, TransparentNormalLayeredOuterShader, TransparentLayeredOuterMaterials);
                    }
                    else
                    {
                        material = GetTrackedMaterial(shapeDescriptor, textureToUse, TransparentNormalShader, TransparentMaterials);
                    }
                }
            }

            return material;
        }

        // Every surface's emission (its Meta pass's) is light Surface Cache Global Illumination spreads to other surfaces (it
        // only reads emission from materials marked for realtime indirect emission): lit surfaces' bounced light, and
        // landscapes' ambient sky light. The surface materials copied from it (SurfaceMaterials) take this setting from it.
        private static void SetSurfaceCacheEmission(Material material)
        {
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeIndirectEmission;
        }

        private static Material GetTrackedMaterial(ushort shapeDescriptor, Texture2D textureToUse, Shader shaderToUse, IDictionary<ushort, Material> trackedMaterials)
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

                SetSurfaceCacheEmission(material);

                if (shaderToUse == MediaShader)
                {
                    var readableTexture = GetReadableTexture(textureToUse);
                    var fastAverageTextureColor = readableTexture.GetPixelBilinear(0f, 0f);
                    fastAverageTextureColor += readableTexture.GetPixelBilinear(0.25f, 0.75f);
                    fastAverageTextureColor += readableTexture.GetPixelBilinear(0.66f, 0.33f);
                    fastAverageTextureColor *= 1f / 3f;

                    if (readableTexture != textureToUse)
                    {
                        Object.Destroy(readableTexture);
                    }

                    Color.RGBToHSV(fastAverageTextureColor, out float hue, out float saturation, out _);
                    fastAverageTextureColor = Color.HSVToRGB(hue, saturation, 1f);

                    material.SetColor(mediaSubColorPropertyId, fastAverageTextureColor);
                }
            }

            if (material.mainTexture != textureToUse)
            {
                material.mainTexture = textureToUse;
                material.name = $"Collection({shapeDescriptor.GetCollection()}) Bitmap({shapeDescriptor.GetShape()})";

                ApplyMmlMaps(material,
                             MmlTextures.TryGetWallBumpMap(shapeDescriptor, out var bumpMap) ? bumpMap : null,
                             MmlTextures.TryGetWallGlowMap(shapeDescriptor, out var glowMap) ? glowMap : null);
            }

            return material;
        }

        // The texture, or (as textures are made unreadable once uploaded) a small readable copy of it, read back from the GPU
        private static Texture2D GetReadableTexture(Texture2D texture)
        {
            if (texture.isReadable)
            {
                return texture;
            }

            const int CopySize = 64;
            var copyTarget = RenderTexture.GetTemporary(CopySize, CopySize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;

            try
            {
                Graphics.Blit(texture, copyTarget);
                RenderTexture.active = copyTarget;

                var copy = new Texture2D(CopySize, CopySize, TextureFormat.RGBA32, mipChain: false, linear: false);
                copy.ReadPixels(new UnityEngine.Rect(0, 0, CopySize, CopySize), 0, 0);
                copy.Apply(updateMipmaps: false);

                return copy;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(copyTarget);
            }
        }
    }
}