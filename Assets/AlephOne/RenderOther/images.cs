// Port of Aleph One: Source_Files/RenderOther/images.h, images.cpp (the scenario's and the shapes file's pictures)
//
// Surfaces are the SDL/SDL_surface.cs stand-in, their pixels little-endian as on the platforms Aleph One runs on.
// C++'s template unpack_bits<T> is unpack_bits with the size of T as a parameter.
//
// Not ported: the Images file, the external resources file and the sounds file (the interface's pictures and
// sounds), plugin resources (Plugins::get_resource), the QuickTime JPEG opcode (0x8200, which Aleph One only
// decodes with SDL_image), drawing (draw_picture, draw_full_screen_pict_resource_*, scroll_full_screen_*,
// rescale_surface, tile_surface), CLUTs for the screen (calculate_picture_clut, build_8bit_system_color_table),
// Marathon 1's composited menu, and the title screens.
//
// ForgePlus: picture_to_surface stops at the end of the picture's data.
using Unity.Scripting.LifecycleManagement;
using static AlephOne.cstypes;
using static AlephOne.SDL_rwops;
using static AlephOne.SDL_surface;
using static AlephOne.wad;

namespace AlephOne
{
    // Structure for open image file
    public class image_file_t
    {
        private readonly OpenedResourceFile rsrc_file = new OpenedResourceFile();
        private readonly OpenedFile wad_file = new OpenedFile();
        private readonly wad_header wad_hdr = new wad_header();

        /*
         *  Open/close image file
         */
        public bool open_file(FileSpecifier file)
        {
            close_file();

            // Try to open as a resource file
            if (!file.Open(rsrc_file))
            {
                // This failed, maybe it's a wad file (M2 Win95 style)
                if (!open_wad_file_for_reading(file, wad_file)
                 || !read_wad_header(wad_file, wad_hdr))
                {
                    // This also failed, bail out
                    wad_file.Close();
                    return false;
                }
            } // Try to open wad file, too
            else if (!wad_file.IsOpen())
            {
                if (open_wad_file_for_reading(file, wad_file))
                {
                    if (!read_wad_header(wad_file, wad_hdr))
                    {
                        wad_file.Close();
                    }
                }
            }

            return true;
        }

        public void close_file()
        {
            rsrc_file.Close();
            wad_file.Close();
        }

        public bool is_open()
        {
            return rsrc_file.IsOpen() || wad_file.IsOpen();
        }

        /*
         *  Determine ID for picture resource
         */
        public int determine_pict_resource_id(int base_id, int delta16, int delta32)
        {
            int actual_id = base_id;
            bool done = false;
            int bit_depth = screen.interface_bit_depth;

            while (!done)
            {
                int next_bit_depth = 0;

                actual_id = base_id;
                switch (bit_depth)
                {
                    case 8:
                        next_bit_depth = 0;
                        break;

                    case 16:
                        next_bit_depth = 8;
                        actual_id += delta16;
                        break;

                    case 32:
                        next_bit_depth = 16;
                        actual_id += delta32;
                        break;

                    default:
                        csalerts.assert(false);
                        break;
                }

                if (has_pict(actual_id))
                    done = true;

                if (!done)
                {
                    if (next_bit_depth != 0)
                        bit_depth = next_bit_depth;
                    else
                    {
                        // Didn't find it. Return the 8 bit version and bail..
                        done = true;
                    }
                }
            }
            return actual_id;
        }

        /*
         *  Get resource from file
         */
        private bool has_rsrc(uint rsrc_type, uint wad_type, int id)
        {
            // Check for resource in resource file
            if (rsrc_file.IsOpen())
            {
                if (rsrc_file.Check(rsrc_type, (short) id))
                    return true;
            }

            // Check for resource in wad file
            if (wad_file.IsOpen())
            {
                wad_data d = read_indexed_wad_from_file(wad_file, wad_hdr, (short) id, true);
                if (d != null)
                {
                    bool success = false;
                    if (extract_type_from_wad(d, wad_type, out _) != null)
                        success = true;
                    free_wad(d);
                    return success;
                }
            }

            return false;
        }

        public bool has_pict(int id)
        {
            return has_rsrc(FOUR_CHARS_TO_INT('P', 'I', 'C', 'T'), FOUR_CHARS_TO_INT('P', 'I', 'C', 'T'), id) || has_rsrc(FOUR_CHARS_TO_INT('P', 'I', 'C', 'T'), FOUR_CHARS_TO_INT('p', 'i', 'c', 't'), id);
        }

        public bool has_clut(int id)
        {
            return has_rsrc(FOUR_CHARS_TO_INT('c', 'l', 'u', 't'), FOUR_CHARS_TO_INT('c', 'l', 'u', 't'), id);
        }

        private bool get_rsrc(uint rsrc_type, uint wad_type, int id, LoadedResource rsrc)
        {
            // Get resource from resource file
            if (rsrc_file.IsOpen())
            {
                if (rsrc_file.Get(rsrc_type, (short) id, rsrc))
                    return true;
            }

            // Get resource from wad file
            if (wad_file.IsOpen())
            {
                wad_data d = read_indexed_wad_from_file(wad_file, wad_hdr, (short) id, true);
                if (d != null)
                {
                    bool success = false;
                    byte[] raw = extract_type_from_wad(d, wad_type, out int raw_length);
                    if (raw != null)
                    {
                        if (rsrc_type == FOUR_CHARS_TO_INT('P', 'I', 'C', 'T'))
                        {
                            if (wad_type == FOUR_CHARS_TO_INT('P', 'I', 'C', 'T'))
                            {
                                var pict_data = new byte[raw_length];
                                System.Array.Copy(raw, pict_data, raw_length);
                                rsrc.SetData(pict_data, raw_length);
                                success = true;
                            }
                            else
                            {
                                byte[] clut_data = extract_type_from_wad(d, FOUR_CHARS_TO_INT('c', 'l', 'u', 't'), out int clut_length);
                                success = make_rsrc_from_pict(raw, raw_length, rsrc, clut_data, clut_length);
                            }
                        }
                        else if (rsrc_type == FOUR_CHARS_TO_INT('c', 'l', 'u', 't'))
                            success = make_rsrc_from_clut(raw, raw_length, rsrc);
                        else if (rsrc_type == FOUR_CHARS_TO_INT('s', 'n', 'd', ' '))
                        {
                            var snd_data = new byte[raw_length];
                            System.Array.Copy(raw, snd_data, raw_length);
                            rsrc.SetData(snd_data, raw_length);
                            success = true;
                        }
                        else if (rsrc_type == FOUR_CHARS_TO_INT('T', 'E', 'X', 'T'))
                        {
                            var text_data = new byte[raw_length];
                            System.Array.Copy(raw, text_data, raw_length);
                            rsrc.SetData(text_data, raw_length);
                            success = true;
                        }
                    }
                    free_wad(d);
                    return success;
                }
            }

            return false;
        }

        public bool get_pict(int id, LoadedResource rsrc)
        {
            return get_rsrc(FOUR_CHARS_TO_INT('P', 'I', 'C', 'T'), FOUR_CHARS_TO_INT('P', 'I', 'C', 'T'), id, rsrc) || get_rsrc(FOUR_CHARS_TO_INT('P', 'I', 'C', 'T'), FOUR_CHARS_TO_INT('p', 'i', 'c', 't'), id, rsrc);
        }

        public bool get_clut(int id, LoadedResource rsrc)
        {
            return get_rsrc(FOUR_CHARS_TO_INT('c', 'l', 'u', 't'), FOUR_CHARS_TO_INT('c', 'l', 'u', 't'), id, rsrc);
        }

        public bool get_snd(int id, LoadedResource rsrc)
        {
            return get_rsrc(FOUR_CHARS_TO_INT('s', 'n', 'd', ' '), FOUR_CHARS_TO_INT('s', 'n', 'd', ' '), id, rsrc);
        }

        public bool get_text(int id, LoadedResource rsrc)
        {
            return get_rsrc(FOUR_CHARS_TO_INT('T', 'E', 'X', 'T'), FOUR_CHARS_TO_INT('t', 'e', 'x', 't'), id, rsrc);
        }

        /*
         *  Convert picture and CLUT data from wad file to PICT resource
         */
        private bool make_rsrc_from_pict(byte[] data, int length, LoadedResource rsrc, byte[] clut_data, int clut_length)
        {
            if (length < 10)
                return false;

            // Extract size and depth
            byte[] p = data;
            int height = (p[4] << 8) + p[5];
            int width = (p[6] << 8) + p[7];
            int depth = (p[8] << 8) + p[9];
            if (depth != 8 && depth != 16)
                return false;

            // 8-bit depth requires CLUT
            if (depth == 8)
            {
                if (clut_data == null || clut_length != 6 + 256 * 6)
                    return false;
            }

            // size(2), rect(8), versionOp(2), version(2), headerOp(26)
            int output_length = 2 + 8 + 2 + 2 + 26;
            int row_bytes;
            if (depth == 8)
            {
                // opcode(2), pixMap(46), colorTable(8+256*8), srcRect/dstRect/mode(18), data(variable)
                row_bytes = width;
                output_length += 2 + 46 + 8 + 256 * 8 + 18;
            }
            else
            {
                // opcode(2), pixMap(50), srcRect/dstRect/mode(18), data(variable)
                row_bytes = width * 2;
                output_length += 2 + 50 + 18;
            }
            // data(variable), opEndPic(2)
            output_length += row_bytes * height + 2;

            // Allocate memory for Mac PICT resource
            var pict_rsrc = new byte[output_length];

            // Convert pict tag to Mac PICT resource
            byte[] q = pict_rsrc;
            int qi = 0;

            // 1. PICT header
            q[0] = (byte) (output_length >> 8);
            q[1] = (byte) output_length;
            System.Array.Copy(p, 0, q, 2, 8);
            qi += 10;

            // 2. VersionOp/Version/HeaderOp
            q[qi + 0] = 0x00; q[qi + 1] = 0x11; // versionOp
            q[qi + 2] = 0x02; q[qi + 3] = 0xff; // version
            q[qi + 4] = 0x0c; q[qi + 5] = 0x00; // headerOp
            q[qi + 6] = 0xff; q[qi + 7] = 0xfe; // header version
            q[qi + 11] = 0x48; // hRes
            q[qi + 15] = 0x48; // vRes
            System.Array.Copy(p, 0, q, qi + 18, 8);
            qi += 30;

            // 3. opcode
            if (depth == 8)
            {
                q[qi + 0] = 0x00; q[qi + 1] = 0x98; // PackBitsRect
                qi += 2;
            }
            else
            {
                q[qi + 0] = 0x00; q[qi + 1] = 0x9a; // DirectBitsRect
                qi += 6; // skip pmBaseAddr
            }

            // 4. PixMap
            q[qi + 0] = (byte) ((row_bytes >> 8) | 0x80);
            q[qi + 1] = (byte) row_bytes;
            System.Array.Copy(p, 0, q, qi + 2, 8);
            q[qi + 13] = 0x01; // packType = unpacked
            q[qi + 19] = 0x48; // hRes
            q[qi + 23] = 0x48; // vRes
            q[qi + 27] = (byte) (depth == 8 ? 0 : 0x10); // pixelType
            q[qi + 29] = (byte) depth; // pixelSize
            q[qi + 31] = (byte) (depth == 8 ? 1 : 3); // cmpCount
            q[qi + 33] = (byte) (depth == 8 ? 8 : 5); // cmpSize
            qi += 46;

            // 5. ColorTable
            if (depth == 8)
            {
                q[qi + 7] = 0xff; // ctSize
                qi += 8;
                int pi = 6;
                for (int i = 0; i < 256; i++)
                {
                    qi++;
                    q[qi++] = (byte) i; // value
                    q[qi++] = clut_data[pi++]; // red
                    q[qi++] = clut_data[pi++];
                    q[qi++] = clut_data[pi++]; // green
                    q[qi++] = clut_data[pi++];
                    q[qi++] = clut_data[pi++]; // blue
                    q[qi++] = clut_data[pi++];
                }
            }

            // 6. source/destination Rect and transfer mode
            System.Array.Copy(p, 0, q, qi, 8);
            System.Array.Copy(p, 0, q, qi + 8, 8);
            qi += 18;

            // 7. graphics data
            System.Array.Copy(p, 10, q, qi, row_bytes * height);
            qi += row_bytes * height;

            // 8. OpEndPic
            q[qi + 0] = 0x00;
            q[qi + 1] = 0xff;

            rsrc.SetData(pict_rsrc, output_length);
            return true;
        }

        private bool make_rsrc_from_clut(byte[] data, int length, LoadedResource rsrc)
        {
            const int input_length = 6 + 256 * 6; // 6 bytes header, 256 entries with 6 bytes each
            const int output_length = 8 + 256 * 8; // 8 bytes header, 256 entries with 8 bytes each

            if (length != input_length)
                return false;

            // Allocate memory for Mac CLUT resource
            var clut_rsrc = new byte[output_length];

            // Convert clut tag to Mac CLUT resource
            byte[] p = data;
            byte[] q = clut_rsrc;
            int pi = 0, qi = 0;

            // 1. Header
            q[6] = p[0]; // color count
            q[7] = p[1];
            pi += 6;
            qi += 8;

            // 2. Color table
            for (int i = 0; i < 256; i++)
            {
                qi++;
                q[qi++] = (byte) i; // value
                q[qi++] = p[pi++]; // red
                q[qi++] = p[pi++];
                q[qi++] = p[pi++]; // green
                q[qi++] = p[pi++];
                q[qi++] = p[pi++]; // blue
                q[qi++] = p[pi++];
            }

            rsrc.SetData(clut_rsrc, output_length);
            return true;
        }
    }

    [NoAutoStaticsCleanup]
    public static class images
    {
        // Constants
        public const int _images_file_delta16 = 1000;
        public const int _images_file_delta32 = 2000;
        public const int _scenario_file_delta16 = 10000;
        public const int _scenario_file_delta32 = 20000;

        // Global variables
        private static readonly image_file_t ScenarioFile = new image_file_t();
        private static readonly image_file_t ShapesImagesFile = new image_file_t();

        /*
         *  Uncompress picture data, returns size of compressed image data that was read
         */

        // Uncompress (and endian-correct) scan line compressed by PackBits RLE algorithm
        private static int unpack_bits(byte[] src, int src_offset, int row_bytes, byte[] dst, int dst_offset, int sizeof_T)
        {
            // Read source count
            int src_count;
            if (row_bytes > 250)
            {
                src_count = (src[src_offset] << 8) | src[src_offset + 1];
                src_offset += 2;
            }
            else
                src_count = src[src_offset++];

            while (src_count > 0)
            {
                // Read flag/count byte
                int c = (sbyte) src[src_offset++];
                src_count--;
                if (c < 0)
                {
                    // RLE compressed run
                    int size = -c + 1;
                    int data;
                    if (sizeof_T == 1)
                    {
                        data = src[src_offset++];
                        src_count--;
                    }
                    else
                    {
                        data = (src[src_offset] << 8) | src[src_offset + 1];
                        src_offset += 2;
                        src_count -= 2;
                    }
                    for (int i = 0; i < size; i++)
                        dst_offset = store(dst, dst_offset, data, sizeof_T);
                }
                else
                {
                    // Uncompressed run
                    int size = c + 1;
                    for (int i = 0; i < size; i++)
                    {
                        int data;
                        if (sizeof_T == 1)
                        {
                            data = src[src_offset++];
                            src_count--;
                        }
                        else
                        {
                            data = (src[src_offset] << 8) | src[src_offset + 1];
                            src_offset += 2;
                            src_count -= 2;
                        }
                        dst_offset = store(dst, dst_offset, data, sizeof_T);
                    }
                }
            }
            return src_offset;
        }

        // *dst++ = data, for a T of sizeof_T bytes
        private static int store(byte[] dst, int dst_offset, int data, int sizeof_T)
        {
            if (sizeof_T == 1)
            {
                dst[dst_offset] = (byte) data;
            }
            else
            {
                dst[dst_offset] = (byte) data;
                dst[dst_offset + 1] = (byte) (data >> 8);
            }
            return dst_offset + sizeof_T;
        }

        // 8-bit picture, one scan line at a time
        private static int uncompress_rle8(byte[] src, int src_offset, int row_bytes, byte[] dst, int dst_offset, int dst_pitch, int height)
        {
            int start = src_offset;
            for (int y = 0; y < height; y++)
            {
                src_offset = unpack_bits(src, src_offset, row_bytes, dst, dst_offset, 1);
                dst_offset += dst_pitch;
            }
            return src_offset - start;
        }

        // 16-bit picture, one scan line at a time, 16-bit chunks
        private static int uncompress_rle16(byte[] src, int src_offset, int row_bytes, byte[] dst, int dst_offset, int dst_pitch, int height)
        {
            int start = src_offset;
            for (int y = 0; y < height; y++)
            {
                src_offset = unpack_bits(src, src_offset, row_bytes, dst, dst_offset, 2);
                dst_offset += dst_pitch;
            }
            return src_offset - start;
        }

        private static void copy_component_into_surface(byte[] src, int src_offset, byte[] dst, int dst_offset, int count, int component)
        {
            // PlatformIsLittleEndian()
            dst_offset += 2 - component;
            while (count-- != 0)
            {
                dst[dst_offset] = src[src_offset++];
                dst_offset += 4;
            }
        }

        // 32-bit picture, one scan line, one component at a time
        private static int uncompress_rle32(byte[] src, int src_offset, int row_bytes, byte[] dst, int dst_offset, int dst_pitch, int height)
        {
            var tmp = new byte[row_bytes];

            int start = src_offset;

            int width = row_bytes / 4;
            for (int y = 0; y < height; y++)
            {
                src_offset = unpack_bits(src, src_offset, row_bytes, tmp, 0, 1);

                // "tmp" now contains "width" bytes of red, followed by "width"
                // bytes of green and "width" bytes of blue, so we have to copy them
                // into the surface in the right order
                copy_component_into_surface(tmp, 0, dst, dst_offset, width, 0);
                copy_component_into_surface(tmp, width, dst, dst_offset, width, 1);
                copy_component_into_surface(tmp, width * 2, dst, dst_offset, width, 2);

                dst_offset += dst_pitch;
            }

            return src_offset - start;
        }

        // byte_swapping.h: byte_swap_memory(data, _2byte or _4byte, count)
        private static void byte_swap_memory(byte[] data, int data_offset, int field_size, int count)
        {
            for (int i = 0; i < count; i++, data_offset += field_size)
            {
                System.Array.Reverse(data, data_offset, field_size);
            }
        }

        private static int uncompress_picture(byte[] src, int src_offset, int row_bytes, byte[] dst, int dst_pitch, int depth, int height, int pack_type)
        {
            // Depths <8 have to be color expanded to depth 8 after uncompressing,
            // so we uncompress into a temporary buffer
            byte[] orig_dst = dst;
            int orig_dst_pitch = dst_pitch;
            if (depth < 8)
            {
                dst = new byte[row_bytes * height];
                dst_pitch = row_bytes;
            }

            int data_size = 0;

            if (row_bytes < 8)
            {
                // Uncompressed data
                copy_rows(src, src_offset, row_bytes, dst, dst_pitch, height);
                data_size = row_bytes * height;
            }
            else
            {
                // Compressed data
                if (depth <= 8)
                {
                    // Indexed color
                    if (pack_type == 1)
                    {
                        // goto no_packing (depth <= 8, so nothing to byte swap)
                        copy_rows(src, src_offset, row_bytes, dst, dst_pitch, height);
                        data_size = row_bytes * height;
                    }
                    else
                        data_size = uncompress_rle8(src, src_offset, row_bytes, dst, 0, dst_pitch, height);
                }
                else
                {
                    // Direct color
                    if (pack_type == 0)
                    {
                        if (depth == 16)
                            pack_type = 3;
                        else if (depth == 32)
                            pack_type = 4;
                    }
                    switch (pack_type)
                    {
                        case 1: // No packing
                            copy_rows(src, src_offset, row_bytes, dst, dst_pitch, height);
                            data_size = row_bytes * height;
                            if (depth == 16)
                                byte_swap_memory(dst, 0, 2, dst_pitch * height / 2);
                            else if (depth == 32)
                                byte_swap_memory(dst, 0, 4, dst_pitch * height / 4);
                            break;
                        case 3: // Run-length encoding by 16-bit chunks
                            data_size = uncompress_rle16(src, src_offset, row_bytes, dst, 0, dst_pitch, height);
                            break;
                        case 4: // Run-length encoding one component at a time
                            data_size = uncompress_rle32(src, src_offset, row_bytes, dst, 0, dst_pitch, height);
                            break;
                        default:
                            // fprintf(stderr, "Unimplemented packing type %d (depth %d) in PICT resource\n", pack_type, depth);
                            data_size = -1;
                            break;
                    }
                }
            }

            // Color expansion 1/2/4->8 bits
            if (depth < 8)
            {
                byte[] p = dst;
                byte[] q = orig_dst;
                int pi = 0, qi = 0;

                // Source and destination may have different alignment restrictions,
                // don't run off the right of either
                int x_max = row_bytes;
                while (x_max * 8 / depth > orig_dst_pitch)
                    x_max--;

                switch (depth)
                {
                    case 1:
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < x_max; x++)
                            {
                                byte b = p[pi + x];
                                q[qi + x * 8 + 0] = (byte) ((b & 0x80) != 0 ? 0x01 : 0x00);
                                q[qi + x * 8 + 1] = (byte) ((b & 0x40) != 0 ? 0x01 : 0x00);
                                q[qi + x * 8 + 2] = (byte) ((b & 0x20) != 0 ? 0x01 : 0x00);
                                q[qi + x * 8 + 3] = (byte) ((b & 0x10) != 0 ? 0x01 : 0x00);
                                q[qi + x * 8 + 4] = (byte) ((b & 0x08) != 0 ? 0x01 : 0x00);
                                q[qi + x * 8 + 5] = (byte) ((b & 0x04) != 0 ? 0x01 : 0x00);
                                q[qi + x * 8 + 6] = (byte) ((b & 0x02) != 0 ? 0x01 : 0x00);
                                q[qi + x * 8 + 7] = (byte) ((b & 0x01) != 0 ? 0x01 : 0x00);
                            }
                            pi += row_bytes;
                            qi += orig_dst_pitch;
                        }
                        break;
                    case 2:
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < x_max; x++)
                            {
                                byte b = p[pi + x];
                                q[qi + x * 4 + 0] = (byte) ((b >> 6) & 0x03);
                                q[qi + x * 4 + 1] = (byte) ((b >> 4) & 0x03);
                                q[qi + x * 4 + 2] = (byte) ((b >> 2) & 0x03);
                                q[qi + x * 4 + 3] = (byte) (b & 0x03);
                            }
                            pi += row_bytes;
                            qi += orig_dst_pitch;
                        }
                        break;
                    case 4:
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < x_max; x++)
                            {
                                byte b = p[pi + x];
                                q[qi + x * 2 + 0] = (byte) ((b >> 4) & 0x0f);
                                q[qi + x * 2 + 1] = (byte) (b & 0x0f);
                            }
                            pi += row_bytes;
                            qi += orig_dst_pitch;
                        }
                        break;
                }
            }

            return data_size;
        }

        // for (y...) { memcpy(q, p, MIN(row_bytes, dst_pitch)); p += row_bytes; q += dst_pitch; }
        private static void copy_rows(byte[] src, int src_offset, int row_bytes, byte[] dst, int dst_pitch, int height)
        {
            int count = csmacros.MIN(row_bytes, dst_pitch);
            for (int y = 0; y < height; y++)
            {
                System.Array.Copy(src, src_offset + y * row_bytes, dst, y * dst_pitch, count);
            }
        }

        public static int get_pict_header_width(LoadedResource rsrc)
        {
            SDL_RWops p = SDL_RWFromConstMem(rsrc.GetPointer(), rsrc.GetLength());
            if (p != null)
            {
                SDL_RWseek(p, 8, SEEK_CUR);
                int width = SDL_ReadBE16(p);
                SDL_RWclose(p);
                return width;
            }
            return -1;
        }

        /*
         *  Convert picture resource to SDL surface
         */
        public static SDL_Surface picture_to_surface(LoadedResource rsrc)
        {
            SDL_Surface s = null;

            if (!rsrc.IsLoaded())
                return s;

            // Open stream to picture resource
            SDL_RWops p = SDL_RWFromConstMem(rsrc.GetPointer(), rsrc.GetLength());
            if (p == null)
                return s;
            SDL_RWseek(p, 6, SEEK_CUR); // picSize/top/left
            int pic_height = SDL_ReadBE16(p);
            int pic_width = SDL_ReadBE16(p);

            // Read and parse picture opcodes
            bool done = false;
            while (!done)
            {
                // ForgePlus: stop at the end of a truncated picture, whose zeros past the end would be NOPs forever
                if (SDL_RWtell(p) >= rsrc.GetLength())
                    break;

                ushort opcode = SDL_ReadBE16(p);
                switch (opcode)
                {
                    case 0x0000: // NOP
                    case 0x0011: // VersionOp
                    case 0x001c: // HiliteMode
                    case 0x001e: // DefHilite
                    case 0x0038: // FrameSameRect
                    case 0x0039: // PaintSameRect
                    case 0x003a: // EraseSameRect
                    case 0x003b: // InvertSameRect
                    case 0x003c: // FillSameRect
                    case 0x02ff: // Version
                        break;

                    case 0x00ff: // OpEndPic
                        done = true;
                        break;

                    case 0x0001: // Clipping region
                    {
                        ushort size = SDL_ReadBE16(p);
                        if ((size & 1) != 0)
                            size++;
                        SDL_RWseek(p, size - 2, SEEK_CUR);
                        break;
                    }

                    case 0x0003: // TxFont
                    case 0x0004: // TxFace
                    case 0x0005: // TxMode
                    case 0x0008: // PnMode
                    case 0x000d: // TxSize
                    case 0x0015: // PnLocHFrac
                    case 0x0016: // ChExtra
                    case 0x0023: // ShortLineFrom
                    case 0x00a0: // ShortComment
                        SDL_RWseek(p, 2, SEEK_CUR);
                        break;

                    case 0x0006: // SpExtra
                    case 0x0007: // PnSize
                    case 0x000b: // OvSize
                    case 0x000c: // Origin
                    case 0x000e: // FgColor
                    case 0x000f: // BgColor
                    case 0x0021: // LineFrom
                        SDL_RWseek(p, 4, SEEK_CUR);
                        break;

                    case 0x001a: // RGBFgCol
                    case 0x001b: // RGBBkCol
                    case 0x001d: // HiliteColor
                    case 0x001f: // OpColor
                    case 0x0022: // ShortLine
                        SDL_RWseek(p, 6, SEEK_CUR);
                        break;

                    case 0x0002: // BkPat
                    case 0x0009: // PnPat
                    case 0x000a: // FillPat
                    case 0x0010: // TxRatio
                    case 0x0020: // Line
                    case 0x0030: // FrameRect
                    case 0x0031: // PaintRect
                    case 0x0032: // EraseRect
                    case 0x0033: // InvertRect
                    case 0x0034: // FillRect
                        SDL_RWseek(p, 8, SEEK_CUR);
                        break;

                    case 0x0c00: // HeaderOp
                        SDL_RWseek(p, 24, SEEK_CUR);
                        break;

                    case 0x00a1: // LongComment
                    {
                        SDL_RWseek(p, 2, SEEK_CUR);
                        int size = SDL_ReadBE16(p);
                        if ((size & 1) != 0)
                            size++;
                        SDL_RWseek(p, size, SEEK_CUR);
                        break;
                    }

                    case 0x0098: // Packed CopyBits
                    case 0x0099: // Packed CopyBits with clipping region
                    case 0x009a: // Direct CopyBits
                    case 0x009b: // Direct CopyBits with clipping region
                    {
                        // 1. PixMap
                        if (opcode == 0x009a || opcode == 0x009b)
                            SDL_RWseek(p, 4, SEEK_CUR); // pmBaseAddr
                        ushort row_bytes = SDL_ReadBE16(p); // the upper 2 bits are flags
                        bool is_pixmap = ((row_bytes & 0x8000) != 0);
                        row_bytes &= 0x3fff;
                        ushort top = SDL_ReadBE16(p);
                        ushort left = SDL_ReadBE16(p);
                        ushort height = (ushort) (SDL_ReadBE16(p) - top);
                        ushort width = (ushort) (SDL_ReadBE16(p) - left);
                        ushort pack_type, pixel_size;
                        if (is_pixmap)
                        {
                            SDL_RWseek(p, 2, SEEK_CUR); // pmVersion
                            pack_type = SDL_ReadBE16(p);
                            SDL_RWseek(p, 14, SEEK_CUR); // packSize/hRes/vRes/pixelType
                            pixel_size = SDL_ReadBE16(p);
                            SDL_RWseek(p, 16, SEEK_CUR); // cmpCount/cmpSize/planeBytes/pmTable/pmReserved
                        }
                        else
                        {
                            pack_type = 0;
                            pixel_size = 1;
                        }

                        // Allocate surface for picture
                        uint Rmask = 0, Gmask = 0, Bmask = 0;
                        int surface_depth = 8;
                        switch (pixel_size)
                        {
                            case 1:
                            case 2:
                            case 4:
                            case 8:
                                Rmask = Gmask = Bmask = 0;
                                surface_depth = 8; // SDL surfaces must be at least 8 bits depth, so we expand 1/2/4-bit pictures to 8-bit
                                break;
                            case 16:
                                Rmask = 0x7c00;
                                Gmask = 0x03e0;
                                Bmask = 0x001f;
                                surface_depth = 16;
                                break;
                            case 32:
                                Rmask = 0x00ff0000;
                                Gmask = 0x0000ff00;
                                Bmask = 0x000000ff;
                                surface_depth = 32;
                                break;
                            default:
                                // fprintf(stderr, "Unsupported PICT depth %d\n", pixel_size);
                                done = true;
                                break;
                        }
                        if (done)
                            break;
                        SDL_Surface bm = SDL_CreateRGBSurface(SDL_SWSURFACE, width, height, surface_depth, Rmask, Gmask, Bmask, 0);
                        if (bm == null)
                        {
                            done = true;
                            break;
                        }

                        // 2. ColorTable
                        if (is_pixmap && (opcode == 0x0098 || opcode == 0x0099))
                        {
                            var colors = new SDL_Color[256];
                            SDL_RWseek(p, 4, SEEK_CUR); // ctSeed
                            ushort flags = SDL_ReadBE16(p);
                            int num_colors = SDL_ReadBE16(p) + 1;
                            for (int i = 0; i < num_colors; i++)
                            {
                                byte value = (byte) (SDL_ReadBE16(p) & 0xff);
                                if ((flags & 0x8000) != 0)
                                    value = (byte) i;
                                colors[value] = new SDL_Color
                                {
                                    r = (byte) (SDL_ReadBE16(p) >> 8),
                                    g = (byte) (SDL_ReadBE16(p) >> 8),
                                    b = (byte) (SDL_ReadBE16(p) >> 8),
                                    a = 0xff,
                                };
                            }
                            SDL_SetPaletteColors(bm.format.palette, colors, 0, 256);
                        }

                        // 3. source/destination Rect and transfer mode
                        SDL_RWseek(p, 18, SEEK_CUR);

                        // 4. clipping region
                        if (opcode == 0x0099 || opcode == 0x009b)
                        {
                            ushort rgn_size = SDL_ReadBE16(p);
                            SDL_RWseek(p, rgn_size - 2, SEEK_CUR);
                        }

                        // 5. graphics data
                        int data_size = uncompress_picture(rsrc.GetPointer(), (int) SDL_RWtell(p), row_bytes, bm.pixels, bm.pitch, pixel_size, height, pack_type);
                        if (data_size < 0)
                        {
                            done = true;
                            break;
                        }
                        if ((data_size & 1) != 0)
                            data_size++;
                        SDL_RWseek(p, data_size, SEEK_CUR);

                        // If there's already a surface, throw away the decoded image
                        // (actually, we could have skipped this entire opcode, but the
                        // only way to do this is to decode the image data).
                        // So we only draw the first image we encounter.
                        if (s != null)
                        {
                            SDL_FreeSurface(bm);
                        }
                        else
                        {
                            s = bm;
                        }

                        break;
                    }

                    // Not ported: case 0x8200 (Compressed QuickTime image), #ifdef HAVE_SDL_IMAGE

                    default:
                        if (opcode >= 0x0300 && opcode < 0x8000)
                            SDL_RWseek(p, (opcode >> 8) * 2, SEEK_CUR);
                        else if (opcode >= 0x8000 && opcode < 0x8100)
                            break;
                        else
                        {
                            // fprintf(stderr, "Unimplemented opcode %04x in PICT resource\n", opcode);
                            done = true;
                        }
                        break;
                }
            }

            // Close stream, return surface
            SDL_RWclose(p);
            return s;
        }

        /*
         *  Shutdown image manager
         */
        public static void shutdown_images_handler()
        {
            ShapesImagesFile.close_file();
            ScenarioFile.close_file();
        }

        /*
         *  Set map file to load images from
         */
        public static void set_scenario_images_file(FileSpecifier file)
        {
            ScenarioFile.open_file(file);
        }

        public static void unset_scenario_images_file()
        {
            ScenarioFile.close_file();
        }

        public static void set_shapes_images_file(FileSpecifier file)
        {
            ShapesImagesFile.open_file(file);
        }

        /*
         *  Get/draw image from scenario
         */
        public static bool get_picture_resource_from_scenario(int base_resource, LoadedResource PictRsrc)
        {
            bool found = false;

            if (!found && ScenarioFile.is_open())
            {
                // Not ported: found = Plugins::instance()->get_resource(FOUR_CHARS_TO_INT('P','I','C','T'), id, PictRsrc);
                if (!found)
                {
                    found = ScenarioFile.get_pict(ScenarioFile.determine_pict_resource_id(base_resource, _scenario_file_delta16, _scenario_file_delta32), PictRsrc);
                }
            }

            if (!found && ShapesImagesFile.is_open())
            {
                // Not ported: found = Plugins::instance()->get_resource(FOUR_CHARS_TO_INT('P','I','C','T'), base_resource, PictRsrc);
                if (!found)
                {
                    found = ShapesImagesFile.get_pict(base_resource, PictRsrc);
                }
            }

            return found;
        }

        public static bool scenario_picture_exists(int base_resource)
        {
            var PictRsrc = new LoadedResource();
            return get_picture_resource_from_scenario(base_resource, PictRsrc);
        }
    }
}
