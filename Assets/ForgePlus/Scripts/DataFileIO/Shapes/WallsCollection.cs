using ForgePlus.DataFileIO;
using System.Collections.Generic;
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
        private static readonly Shader OpaqueNormalShader = Shader.Find("ForgePlus/OpaqueNormal");
        private static readonly Shader OpaqueWithAlphaAlphaNormalShader = Shader.Find("ForgePlus/OpaqueWithAlphaNormal");
        private static readonly Shader TransparentNormalShader = Shader.Find("ForgePlus/TransparentNormal");

        // Landscape
        private static readonly Shader OpaqueLandscapeShader = Shader.Find("ForgePlus/OpaqueLandscape");

        // Media (could be Normal, but I like the added ripple effect)
        private static readonly Shader MediaShader = Shader.Find("ForgePlus/Media");

        // Missing bitmap
        private static readonly Shader OpaqueTriplanarShader = Shader.Find("ForgePlus/OpaqueTriplanar");

        // No assignment
        private static readonly Material UnassignedMaterial = new Material(Shader.Find("ForgePlus/Unassigned"));

        private static readonly Texture2D GridTexture = Resources.Load<Texture2D>("Walls/Grid");

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(200);
        private static readonly Dictionary<string, Material> MediaMaterials = new Dictionary<string, Material>(5);

        public static Material GetMaterial(
            ShapeDescriptor shapeDescriptor,
            short transferMode,
            bool isOpaqueSurface,
            SurfaceTypes surfaceType)
        {
            if ((ushort)shapeDescriptor != (ushort)ShapeDescriptor.Empty)
            {
                var trackedMaterial = GetTrackedMaterial(shapeDescriptor, transferMode, isOpaqueSurface);

                if (surfaceType == SurfaceTypes.Media)
                {
                    Material mediaMaterial;
                    if (MediaMaterials.ContainsKey(GetMaterialKey(shapeDescriptor, transferMode, isOpaqueSurface)))
                    {
                        mediaMaterial = MediaMaterials[GetMaterialKey(shapeDescriptor, transferMode, isOpaqueSurface)];
                    }
                    else
                    {
                        mediaMaterial = new Material(MediaShader);
                    }

                    if (mediaMaterial.mainTexture != trackedMaterial.mainTexture)
                    {
                        mediaMaterial.mainTexture = trackedMaterial.mainTexture;
                    }

                    return mediaMaterial;
                }

                return trackedMaterial;
            }
            else
            {
                return UnassignedMaterial;
            }
        }

        public static void ClearMaterials()
        {
            // Don't actually clear the Materials list,
            // just clear their textures so the Materials can be reused
            foreach (var material in Materials.Values)
            {
                var textureOnMaterial = material.mainTexture;
                if (textureOnMaterial != GridTexture)
                {
                    Object.Destroy(material.mainTexture);
                }

                material.mainTexture = null;
            }
        }

        private static Material GetTrackedMaterial(ShapeDescriptor shapeDescriptor, short transferMode, bool isOpaqueSurface)
        {
            var collectionIndex = shapeDescriptor.Collection;
            var bitmapIndex = shapeDescriptor.Bitmap;

            if (collectionIndex >= 27 && collectionIndex <= 30)
            {
                // If this is a landscape bitmap, then force landscape transfer mode
                transferMode = 9;
            }

            var material = Materials.ContainsKey(GetMaterialKey(shapeDescriptor, transferMode, isOpaqueSurface)) ? Materials[GetMaterialKey(shapeDescriptor, transferMode, isOpaqueSurface)] : null;

            if (!material || !material.mainTexture)
            {
                Texture2D textureToUse = ShapesLoading.Instance.GetShape(shapeDescriptor);
                Shader shaderToUse;
                if (transferMode == 9)
                {
                    shaderToUse = OpaqueLandscapeShader;
                }
                else if (textureToUse)
                {
                    shaderToUse = textureToUse.format == TextureFormat.ARGB32 ? (isOpaqueSurface ? OpaqueWithAlphaAlphaNormalShader : TransparentNormalShader) : OpaqueNormalShader;
                }
                else
                {
                    shaderToUse = OpaqueTriplanarShader;
                }


                if (!textureToUse)
                {
                    textureToUse = GridTexture;
                }

                if (material)
                {
                    if (material.shader != shaderToUse)
                    {
                        material.shader = shaderToUse;
                    }
                }
                else
                {
                    material = new Material(shaderToUse);
                    Materials[GetMaterialKey(shapeDescriptor, transferMode, isOpaqueSurface)] = material;
                }

                if (!material.mainTexture)
                {
                    material.mainTexture = textureToUse;
                }

                material.name = $"Collection({collectionIndex}) Bitmap({bitmapIndex})";
            }

            return material;
        }

        private static string GetMaterialKey(ShapeDescriptor shapeDescriptor, short transferMode, bool isOpaqueWithAlphaVariant)
        {
            // TODO: Make the Key in track bitmap, collection, transfer mode, and light index,
            //       and set up SurfaceLight always get its materials from this class.
            //       This should reduce material count, and thus also draw calls.
            return $"{shapeDescriptor.Collection},{shapeDescriptor.Bitmap},{transferMode},{isOpaqueWithAlphaVariant}";
        }
    }
}
