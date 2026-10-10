using AlephOne;
using ForgePlus.Extensions;
using ForgePlus.Jobs;
using ForgePlus.LevelManipulation.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    // Aleph One keeps its shapes in global state, so only one shapes file can be loaded at a time
    public class ShapesFile : IFileLoadable
    {
        private const int MinimumShapesFileLength = shape_descriptors.MAXIMUM_COLLECTIONS * shape_definitions.SIZEOF_collection_header;

        public void Load(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new IOException($"\"{fileName}\" is not a readable Marathon shapes file.");
            }

            Close();

            // Use the same color depth as modern Aleph One, which loads 16-bit data where a collection has it
            screen.bit_depth = 32;
            shapes.open_shapes_file(new FileSpecifier(fileName));

            // Marathon 2 and Infinity shapes files start with their collection headers, and Aleph One keeps the previous
            // file's when it can't read these ones. (Marathon 1's keep their collections in their resource forks.)
            if (!shapes.shapes_file_is_m1() && new FileInfo(fileName).Length < MinimumShapesFileLength)
            {
                Close();
                throw new IOException($"\"{fileName}\" is not a readable Marathon shapes file.");
            }

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

        // A wall, floor, ceiling, media or landscape texture's bitmap and colors, decoded and ready to become a texture
        public sealed class PreparedShape : IDisposable
        {
            public ushort ShapeDescriptor;
            public IndexedShapeBitmap Bitmap;
            public Color32[] Palette;
            public bool IsLandscape;
            public TextureFormat Format;
            public bool HasMipmaps;

            public void Dispose()
            {
                Bitmap.Dispose();
            }
        }

        // A wall, floor, ceiling, media or landscape texture, or null if the collection, CLUT or shape doesn't exist
        public Texture2D GetShape(ushort shapeDescriptor)
        {
            using (var shape = PrepareShape(shapeDescriptor))
            {
                return shape == null ? null : CreateTextures(new[] { shape })[0];
            }
        }

        // Decodes these shapes on worker threads (they only read the loaded shapes), with null for each that doesn't exist.
        // The caller disposes them.
        public static PreparedShape[] PrepareShapes(IReadOnlyList<ushort> shapeDescriptors)
        {
            var preparedShapes = new PreparedShape[shapeDescriptors.Count];

            Parallel.For(0, shapeDescriptors.Count, new ParallelOptions { MaxDegreeOfParallelism = JobsUtility.JobWorkerCount }, i =>
            {
                preparedShapes[i] = PrepareShape(shapeDescriptors[i]);
            });

            return preparedShapes;
        }

        // Converts all of their pixels in one batch of jobs (null for null shapes)
        public static Texture2D[] CreateTextures(IReadOnlyList<PreparedShape> preparedShapes)
        {
            var textures = new Texture2D[preparedShapes.Count];

            using (var batch = new PixelJobs.PixelBatch())
            {
                var pixels = new NativeArray<byte>[preparedShapes.Count];
                for (var i = 0; i < preparedShapes.Count; i++)
                {
                    var shape = preparedShapes[i];
                    if (shape != null)
                    {
                        pixels[i] = batch.ScheduleConversion(shape.Bitmap, shape.Palette, shape.IsLandscape, shape.Format);
                    }
                }

                batch.Complete();

                for (var i = 0; i < preparedShapes.Count; i++)
                {
                    if (preparedShapes[i] != null)
                    {
                        textures[i] = CreateTexture(preparedShapes[i], pixels[i]);
                    }
                }
            }

            return textures;
        }

        private static PreparedShape PrepareShape(ushort shapeDescriptor)
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
            for (var i = 0; i < bitmap.PixelCount && !hasAlpha; i++)
            {
                hasAlpha = palette[bitmap.Indexes[i]].a == 0;
            }

            return new PreparedShape
            {
                ShapeDescriptor = shapeDescriptor,
                Bitmap = bitmap,
                Palette = palette,
                IsLandscape = isLandscape,
                Format = hasAlpha ? TextureFormat.ARGB32 : TextureFormat.RGB24,
                HasMipmaps = hasAlpha || !isLandscape,
            };
        }

        private static Texture2D CreateTexture(PreparedShape shape, NativeArray<byte> pixels)
        {
            var bitmap = shape.Bitmap;

            // Walls are stored as runs (columns); landscapes' runs are lines of sky, so they're rotated into rows
            var textureWidth = shape.IsLandscape ? bitmap.Height : bitmap.Width;
            var textureHeight = shape.IsLandscape ? bitmap.Width : bitmap.Height;

            var result = new Texture2D(textureWidth, textureHeight, shape.Format, shape.HasMipmaps);
            result.SetPixelData(pixels, mipLevel: 0);

            if (shape.IsLandscape)
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
    }
}
