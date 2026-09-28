using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.LevelManipulation.Utilities;
using System.IO;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    // Aleph One keeps its shapes in global state, so only one shapes file can be loaded at a time
    public class ShapesFile : IFileLoadable
    {
        private const int MinimumShapesFileLength = shape_descriptors.MAXIMUM_COLLECTIONS * shape_definitions.SIZEOF_collection_header;

        public void Load(string fileName)
        {
            // Aleph One keeps the previous file's collection headers when it can't read these ones
            if (!File.Exists(fileName) || new FileInfo(fileName).Length < MinimumShapesFileLength)
            {
                throw new IOException($"\"{fileName}\" is not a readable Marathon shapes file.");
            }

            Close();

            // Use the same color depth as modern Aleph One, which loads 16-bit data where a collection has it
            screen.bit_depth = 32;
            shapes.open_shapes_file(new FileSpecifier(fileName));

            var loadableCollections = 0;
            for (short collection = 0; collection < shape_descriptors.MAXIMUM_COLLECTIONS; collection++)
            {
                if (shapes.can_load_collection(collection))
                {
                    @interface.mark_collection_for_loading(collection);
                    loadableCollections++;
                }
            }

            if (loadableCollections == 0)
            {
                Close();
                throw new IOException($"\"{fileName}\" contains no shape collections.");
            }

            shapes.load_collections(with_progress_bar: false, is_opengl: false);
        }

        public void Close()
        {
            shapes.unload_all_collections();
            shapes.close_shapes_file();
        }

        public bool IsWallCollection(short collection)
        {
            var definition = shapes.get_collection_definition(collection);
            return definition != null && definition.type == collection_definition._wall_collection;
        }

        // A wall, floor, ceiling, media or landscape texture, or null if the collection, CLUT or shape doesn't exist
        public Texture2D GetShape(ushort shapeDescriptor)
        {
            var collection = (short) shapeDescriptor.GetCollection();
            var clut = (short) shapeDescriptor.GetCLUT();
            var shape = (short) shapeDescriptor.GetShape();

            var definition = shapes.get_collection_definition(collection);
            if (definition == null || clut >= definition.clut_count)
            {
                return null;
            }

            var lowLevelShape = shapes.get_low_level_shape_definition(collection, shape);
            if (lowLevelShape == null)
            {
                return null;
            }

            var bitmap = IndexedShapeBitmap.Decode(collection, clut, lowLevelShape.bitmap_index);
            if (bitmap == null)
            {
                return null;
            }

            var isLandscape = shapeDescriptor.UsesLandscapeCollection();

            var palette = bitmap.GetPalette32();
            if (isLandscape && bitmap.Colors.Length > 0)
            {
                // Landscapes draw color 0 opaque (OGL_Textures.cpp: FindColorTables), so resolve it to the CLUT's color 0
                var opaqueColor = bitmap.Colors[0].ToColor32();
                for (var i = 0; i < palette.Length; i++)
                {
                    if (palette[i].a == 0)
                    {
                        palette[i] = opaqueColor;
                    }
                }
            }

            var hasAlpha = false;
            for (var i = 0; i < bitmap.Indexes.Length && !hasAlpha; i++)
            {
                hasAlpha = palette[bitmap.Indexes[i]].a == 0;
            }

            // Walls are stored as runs (columns); landscapes' runs are lines of sky, so they're rotated into rows
            var textureWidth = isLandscape ? bitmap.Height : bitmap.Width;
            var textureHeight = isLandscape ? bitmap.Width : bitmap.Height;

            Texture2D result;
            if (hasAlpha)
            {
                result = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, mipChain: true);
            }
            else if (isLandscape)
            {
                result = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, mipChain: false);
            }
            else
            {
                result = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, mipChain: true);
            }

            result.name = $"CLUT({clut}) Bitmap({shape}) Collection({collection})";

            result.SetPixels32(isLandscape ? GetLandscapePixels(bitmap, palette) : bitmap.GetPixels32(palette));


            if (isLandscape)
            {
                result.wrapModeV = TextureWrapMode.Clamp;
            }

            result.filterMode = FilterMode.Point;

            if (MathUtilities.IsPowerOfTwo(result.width) &&
                MathUtilities.IsPowerOfTwo(result.height))
            {
                result.Apply(updateMipmaps: true, makeNoLongerReadable: false);
                result.Compress(highQuality: true);
                result.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            }
            else
            {
                result.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            }

            return result;
        }

        private static Color32[] GetLandscapePixels(IndexedShapeBitmap bitmap, Color32[] palette)
        {
            // Run x becomes texture row x, and element y becomes column (width - 1 - y)
            var textureWidth = bitmap.Height;
            var pixels = new Color32[bitmap.Width * bitmap.Height];
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    pixels[x * textureWidth + (textureWidth - 1 - y)] = palette[bitmap.Indexes[y * bitmap.Width + x]];
                }
            }

            return pixels;
        }
    }
}
