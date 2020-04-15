using ForgePlus.DataFileIO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weland;

namespace ForgePlus.ShapesCollections
{
    public class WallsCollection
    {
        public enum SurfaceTypes
        {
            Normal,
            Media,
        }

        // Normal
        private static readonly Shader OpaqueWithAlphaAlphaNormalShader = Shader.Find("ForgePlus/OpaqueWithAlphaNormal");
        private static readonly Shader TransparentNormalShader = Shader.Find("ForgePlus/TransparentNormal");

        // Landscape
        private static readonly Shader OpaqueLandscapeShader = Shader.Find("ForgePlus/OpaqueLandscape");

        // Media (could be Normal, but I like the added ripple effect)
        private static readonly Shader MediaShader = Shader.Find("ForgePlus/Media");

        // No assignment
        private static readonly Material UnassignedMaterial = new Material(Shader.Find("ForgePlus/Unassigned"));

        private static readonly Texture2D GridTexture = Resources.Load<Texture2D>("Walls/Grid");

        private static readonly Dictionary<ShapeDescriptor, Texture2D> Textures = new Dictionary<ShapeDescriptor, Texture2D>(255);

        private static readonly Dictionary<ShapeDescriptor, Material> Materials = new Dictionary<ShapeDescriptor, Material>(255);
        private static readonly Dictionary<ShapeDescriptor, Material> TransparentMaterials = new Dictionary<ShapeDescriptor, Material>(100);
        private static readonly Dictionary<ShapeDescriptor, Material> LandscapeMaterials = new Dictionary<ShapeDescriptor, Material>(1);
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

        public static Material GetMaterial(
            ShapeDescriptor shapeDescriptor,
            short transferMode,
            bool isOpaqueSurface,
            SurfaceTypes surfaceType)
        {
            if ((ushort)shapeDescriptor != (ushort)ShapeDescriptor.Empty)
            {
                var landscapeTransferMode = transferMode == 9 ||
                                            (shapeDescriptor.Collection >= 27 && shapeDescriptor.Collection <= 30);

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

        public static void ClearCollection()
        {
            ClearMaterials(Materials);
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
                    return GetTrackedMaterial(shapeDescriptor, textureToUse, TransparentNormalShader, TransparentMaterials);
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
