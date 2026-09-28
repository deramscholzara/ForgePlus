using AlephOne;

namespace ForgePlus.DataFileIO
{
    // A loaded collection's bitmap, decoded as Aleph One draws it (get_shape_surface), top row first.
    // Palette maps raw indexes to CLUT colors, as update_color_environment does (null where there's none).
    public sealed class IndexedShapeBitmap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        // [y * Width + x]
        public byte[] Indexes { get; private set; }

        public rgb_color_value[] Colors { get; private set; }

        public rgb_color_value[] Palette { get; private set; }

        // Null if the collection isn't loaded or the bitmap or CLUT doesn't exist
        public static IndexedShapeBitmap Decode(short collectionIndex, short clutIndex, short bitmapIndex, ushort mirrorFlags = 0)
        {
            var collection = shapes.get_collection_definition(collectionIndex);
            if (collection == null)
            {
                return null;
            }

            var bitmap = shapes.get_bitmap_definition(collectionIndex, bitmapIndex);
            if (bitmap == null)
            {
                return null;
            }

            var colors = shapes.get_collection_colors(collectionIndex, clutIndex);
            var primaryColors = shapes.get_collection_colors(collectionIndex, 0);
            if (colors == null || primaryColors == null)
            {
                return null;
            }

            var width = (int) bitmap.width;
            var height = (int) bitmap.height;
            var indexes = new byte[width * height];

            var columnOrder = (bitmap.flags & textures._COLUMN_ORDER_BIT) != 0;
            var xMirrored = (mirrorFlags & collection_definition._X_MIRRORED_BIT) != 0;
            var yMirrored = (mirrorFlags & collection_definition._Y_MIRRORED_BIT) != 0;

            var runs = columnOrder ? width : height;
            var runLength = columnOrder ? height : width;
            var pixels = bitmap.pixels;

            for (var run = 0; run < runs; run++)
            {
                var read = bitmap.row_addresses[run];
                int first;
                int last;
                if (bitmap.bytes_per_row == cstypes.NONE)
                {
                    // Big-endian first and last
                    first = (pixels[read] << 8) | pixels[read + 1];
                    last = (pixels[read + 2] << 8) | pixels[read + 3];
                    read += 4;
                }
                else
                {
                    first = 0;
                    last = runLength;
                }

                for (var element = first; element < last; element++)
                {
                    var x = columnOrder ? run : element;
                    var y = columnOrder ? element : run;

                    if (xMirrored)
                    {
                        x = width - 1 - x;
                    }

                    if (yMirrored)
                    {
                        y = height - 1 - y;
                    }

                    indexes[y * width + x] = pixels[read++];
                }
            }

            var palette = new rgb_color_value[256];
            for (var i = collection_definition.NUMBER_OF_PRIVATE_COLORS; i < collection.color_count; i++)
            {
                palette[primaryColors[i].value] = colors[i];
            }

            return new IndexedShapeBitmap
            {
                Width = width,
                Height = height,
                Indexes = indexes,
                Colors = colors,
                Palette = palette,
            };
        }
    }
}
