// Port of Aleph One: Source_Files/RenderMain/textures.h, textures.cpp (MARATHON2)
//
// Aleph One keeps a bitmap in one block: the bitmap_definition, the row pointers, then the pixels. Here
// the pixels are bitmap_definition.pixels, and the row addresses are offsets into it.
//
// Not ported: bitmap_definition_buffer.
using static AlephOne.cstypes;

namespace AlephOne
{
    /* ---------- structures */

    public class bitmap_definition
    {
        public short width, height; /* in pixels */
        public short bytes_per_row; /* if ==NONE this is a transparent RLE shape */

        public short flags; /* [column_order.1] [unused.15] */
        public short bit_depth; /* should always be ==8 */

        public short[] unused = new short[8];

        public int[] row_addresses = new int[1]; // pixel8 *row_addresses[1];

        // ForgePlus: what follows the row pointers
        public byte[] pixels = new byte[0];
    }

    public static class textures
    {
        /* bitmap flags */
        public const int _COLUMN_ORDER_BIT = 0x8000;
        public const int _TRANSPARENT_BIT = 0x4000;
        public const int _PATCHED_BIT = 0x2000; // the bitmap should take precedent over MML

        public const int SIZEOF_bitmap_definition = 30;

        /* ---------- code */

        /* assumes pixel data follows bitmap_definition structure immediately */
        public static int calculate_bitmap_origin(
            bitmap_definition bitmap)
        {
            int origin;

            origin = 0;
            // origin+= (bitmap->flags&_COLUMN_ORDER_BIT ? bitmap->width : bitmap->height)*sizeof(pixel8 *);

            return origin;
        }

        public static void remap_bitmap(
            bitmap_definition bitmap,
            byte[] table)
        {
            short row, rows, columns;

            rows = (bitmap.flags & _COLUMN_ORDER_BIT) != 0 ? bitmap.width : bitmap.height;
            columns = (bitmap.flags & _COLUMN_ORDER_BIT) != 0 ? bitmap.height : bitmap.width;

            if (bitmap.bytes_per_row != NONE)
            {
                for (row = 0; row < rows; ++row)
                {
                    map_bytes(bitmap.pixels, bitmap.row_addresses[row], table, sizeof(byte) * columns);
                }
            }
            else
            {
                // MARATHON2
                byte[] p = bitmap.pixels;
                int pixels;

                pixels = bitmap.row_addresses[0];
                for (row = 0; row < rows; ++row)
                {
                    // CB: first/last are stored in big-endian order
                    ushort first = (ushort) (p[pixels++] << 8);
                    first |= p[pixels++];
                    ushort last = (ushort) (p[pixels++] << 8);
                    last |= p[pixels++];
                    map_bytes(p, pixels, table, last - first);
                    pixels += last - first;
                }
            }
        }

        /* must initialize bytes_per_row, height and row_address[0] */
        public static void precalculate_bitmap_row_addresses(
            bitmap_definition bitmap)
        {
            short row, rows, bytes_per_row;
            int row_address;
            int table;

            rows = (bitmap.flags & _COLUMN_ORDER_BIT) != 0 ? bitmap.width : bitmap.height;

            row_address = bitmap.row_addresses[0];
            table = 0;
            if ((bytes_per_row = bitmap.bytes_per_row) != NONE)
            {
                for (row = 0; row < rows; ++row)
                {
                    bitmap.row_addresses[table++] = row_address;
                    row_address += bytes_per_row;
                }
            }
            else
            {
                // MARATHON2
                byte[] p = bitmap.pixels;
                for (row = 0; row < rows; ++row)
                {
                    bitmap.row_addresses[table++] = row_address;

                    // CB: first/last are stored in big-endian order
                    ushort first = (ushort) (p[row_address++] << 8);
                    first |= p[row_address++];
                    ushort last = (ushort) (p[row_address++] << 8);
                    last |= p[row_address++];
                    row_address += last - first;
                }
            }
        }

        public static void map_bytes(
            byte[] buffer,
            int buffer_offset,
            byte[] table,
            int size)
        {
            while ((size -= 1) >= 0)
            {
                buffer[buffer_offset] = table[buffer[buffer_offset]];
                buffer_offset += 1;
            }
        }
    }
}
