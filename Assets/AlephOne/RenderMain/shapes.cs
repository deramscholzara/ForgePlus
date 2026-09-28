// Port of Aleph One: Source_Files/RenderMain/shapes.cpp (loading shapes files, and the accessors)
//
// Not ported (rendering): shading and tinting tables, the screen color table and the remapping of
// bitmaps into it (find_or_add_color, remap_bitmap, build_shading_tables*, build_*tinting_table*,
// _change_clut, get_global_shading_table), infravision MML, get_shape_surface(), OpenGL and software
// renderer extras, and plugins' shapes patches. So bitmaps keep the shapes file's palette indexes and
// the color tables their .value fields.
//
// Not ported: opening Marathon 1 shapes files, whose collections are '.256' resources (FileHandler.cs
// has no resource forks). The Marathon 1 RLE conversion is ported.
using System;
using System.Collections.Generic;
using static AlephOne.collection_definition;
using static AlephOne.csalerts;
using static AlephOne.cstypes;
using static AlephOne.@interface;
using static AlephOne.Packing;
using static AlephOne.render;
using static AlephOne.screen;
using static AlephOne.SDL_rwops;
using static AlephOne.shape_definitions;
using static AlephOne.shape_descriptors;
using static AlephOne.textures;

namespace AlephOne
{
    public static class shapes
    {
        /* ---------- constants */

        // Not ported: iWHITE, iBLACK, NUMBER_OF_TINT_TABLES (shading/tinting tables).

        /* collection status */
        public const short markNONE = 0;
        public const short markLOAD = 1;
        public const short markUNLOAD = 2;
        public const short markSTRIP = 4; /* we don't want bitmaps, just high/low-level shape data */
        public const short markPATCHED = 8; /* force re-load */

        /* flags */
        public const ushort _collection_is_stripped = 0x0001;

        /* ---------- globals */

        // LP addition: opened-shapes-file object
        private static readonly OpenedFile ShapesFile = new OpenedFile();
        // Not ported: OpenedResourceFile M1ShapesFile;

        public const int M1_SHAPES_VERSION = 1;
        public const int M2_SHAPES_VERSION = 2;
        private static int shapes_file_version;

        public static bool shapes_file_is_m1() { return shapes_file_version == M1_SHAPES_VERSION; }

        private static void load_collection_definition(collection_definition cd, SDL_RWops p)
        {
            cd.version = (short) SDL_ReadBE16(p);
            cd.type = (short) SDL_ReadBE16(p);
            cd.flags = SDL_ReadBE16(p);
            cd.color_count = (short) SDL_ReadBE16(p);
            cd.clut_count = (short) SDL_ReadBE16(p);
            cd.color_table_offset = (int) SDL_ReadBE32(p);
            cd.high_level_shape_count = (short) SDL_ReadBE16(p);
            cd.high_level_shape_offset_table_offset = (int) SDL_ReadBE32(p);
            cd.low_level_shape_count = (short) SDL_ReadBE16(p);
            cd.low_level_shape_offset_table_offset = (int) SDL_ReadBE32(p);
            cd.bitmap_count = (short) SDL_ReadBE16(p);
            cd.bitmap_offset_table_offset = (int) SDL_ReadBE32(p);
            cd.pixels_to_world = (short) SDL_ReadBE16(p);
            SDL_ReadBE32(p); // skip size
            SDL_RWseek(p, 253 * sizeof(short), SEEK_CUR); // unused

            // resize members
            cd.color_tables = resize(cd.color_tables, cd.clut_count * cd.color_count, true);
            cd.high_level_shapes = resize(cd.high_level_shapes, cd.high_level_shape_count, false);
            cd.low_level_shapes = resize(cd.low_level_shapes, cd.low_level_shape_count, true);
            cd.bitmaps = resize(cd.bitmaps, cd.bitmap_count, false);
        }

        // std::vector::resize(); a null element is an empty std::vector<uint8>
        private static T[] resize<T>(T[] array, int count, bool construct) where T : class, new()
        {
            var resized = new T[count];
            Array.Copy(array, resized, Math.Min(array.Length, count));
            if (construct)
            {
                for (int i = array.Length; i < count; i++)
                {
                    resized[i] = new T();
                }
            }

            return resized;
        }

        private static void load_clut(rgb_color_value[] r, int r_index, int count, SDL_RWops p)
        {
            var flags_and_value = new byte[2];
            for (int i = 0; i < count; i++, r_index++)
            {
                SDL_RWread(p, flags_and_value, 0, 1, 2);
                r[r_index].flags = flags_and_value[0];
                r[r_index].value = flags_and_value[1];
                r[r_index].red = SDL_ReadBE16(p);
                r[r_index].green = SDL_ReadBE16(p);
                r[r_index].blue = SDL_ReadBE16(p);
            }
        }

        private static void load_high_level_shape(ref high_level_shape_definition shape, SDL_RWops p)
        {
            short type = (short) SDL_ReadBE16(p);
            short flags = (short) SDL_ReadBE16(p);
            var name = new byte[HIGH_LEVEL_SHAPE_NAME_LENGTH + 2];
            SDL_RWread(p, name, 0, 1, HIGH_LEVEL_SHAPE_NAME_LENGTH + 2);
            short number_of_views = (short) SDL_ReadBE16(p);
            short frames_per_view = (short) SDL_ReadBE16(p);

            // Convert low-level shape index list
            int num_views;
            switch (number_of_views)
            {
                case _unanimated:
                case _animated1:
                    num_views = 1;
                    break;
                case _animated3to4:
                case _animated4:
                    num_views = 4;
                    break;
                case _animated3to5:
                case _animated5:
                    num_views = 5;
                    break;
                case _animated2to8:
                case _animated5to8:
                case _animated8:
                    num_views = 8;
                    break;
                default:
                    num_views = number_of_views;
                    break;
            }

            // shape.resize(sizeof(high_level_shape_definition) + num_views * frames_per_view * sizeof(int16));
            if (shape == null) shape = new high_level_shape_definition();

            high_level_shape_definition d = shape;

            d.type = type;
            d.flags = unchecked((ushort) flags);
            Buffer.BlockCopy(name, 0, d.name, 0, HIGH_LEVEL_SHAPE_NAME_LENGTH + 2);
            d.number_of_views = number_of_views;
            d.frames_per_view = frames_per_view;
            d.ticks_per_frame = (short) SDL_ReadBE16(p);
            d.key_frame = (short) SDL_ReadBE16(p);
            d.transfer_mode = (short) SDL_ReadBE16(p);
            d.transfer_mode_period = (short) SDL_ReadBE16(p);
            d.first_frame_sound = (short) SDL_ReadBE16(p);
            d.key_frame_sound = (short) SDL_ReadBE16(p);
            d.last_frame_sound = (short) SDL_ReadBE16(p);
            d.pixels_to_world = (short) SDL_ReadBE16(p);
            d.loop_frame = (short) SDL_ReadBE16(p);
            SDL_RWseek(p, 14 * sizeof(short), SEEK_CUR);

            // Convert low-level shape index list
            var low_level_shape_indexes = new short[num_views * d.frames_per_view];
            Array.Copy(d.low_level_shape_indexes, low_level_shape_indexes, Math.Min(d.low_level_shape_indexes.Length, low_level_shape_indexes.Length));
            d.low_level_shape_indexes = low_level_shape_indexes;
            for (int j = 0; j < num_views * d.frames_per_view; j++)
            {
                d.low_level_shape_indexes[j] = (short) SDL_ReadBE16(p);
            }
        }

        private static void load_low_level_shape(low_level_shape_definition d, SDL_RWops p)
        {
            d.flags = SDL_ReadBE16(p);
            d.minimum_light_intensity = (int) SDL_ReadBE32(p);
            d.bitmap_index = (short) SDL_ReadBE16(p);
            d.origin_x = (short) SDL_ReadBE16(p);
            d.origin_y = (short) SDL_ReadBE16(p);
            d.key_x = (short) SDL_ReadBE16(p);
            d.key_y = (short) SDL_ReadBE16(p);
            d.world_left = (short) SDL_ReadBE16(p);
            d.world_right = (short) SDL_ReadBE16(p);
            d.world_top = (short) SDL_ReadBE16(p);
            d.world_bottom = (short) SDL_ReadBE16(p);
            d.world_x0 = (short) SDL_ReadBE16(p);
            d.world_y0 = (short) SDL_ReadBE16(p);
            SDL_RWseek(p, 4 * sizeof(short), SEEK_CUR);
        }

        // bitmap is the part of the std::vector<uint8> after the row pointers
        private static void convert_m1_rle(List<byte> bitmap, int scanlines, int scanline_length, SDL_RWops p)
        {
            // std::vector<uint8> bitmap;
            for (int scanline = 0; scanline < scanlines; ++scanline)
            {
                var scanline_data = new byte[scanline_length + 1];
                int dst = 0;
                int sentry = scanline_length;

                while (true)
                {
                    short opcode = (short) SDL_ReadBE16(p);
                    if (opcode > 0)
                    {
                        assert(dst + opcode <= sentry);
                        SDL_RWread(p, scanline_data, dst, opcode, 1);
                        dst += opcode;
                    }
                    else if (opcode < 0)
                    {
                        assert(dst - opcode <= sentry);
                        dst -= opcode;
                    }
                    else
                        break;
                }

                assert(dst == sentry);

                // Find M2/oo-format RLE compression;
                // it needs the first nonblank pixel and the last nonblank one + 1
                short first = 0;
                short last = 0;
                for (int i = 0; i < scanline_length; ++i)
                {
                    if (scanline_data[i] != 0)
                    {
                        first = (short) i;
                        break;
                    }
                }

                for (int i = scanline_length - 1; i >= 0; --i)
                {
                    if (scanline_data[i] != 0)
                    {
                        last = (short) (i + 1);
                        break;
                    }
                }

                if (last < first) last = first;

                bitmap.Add((byte) (first >> 8));
                bitmap.Add((byte) (first & 0xff));
                bitmap.Add((byte) (last >> 8));
                bitmap.Add((byte) (last & 0xff));
                for (int i = first; i < last; i++) bitmap.Add(scanline_data[i]);
            }
        }

        private static void load_bitmap(ref bitmap_definition bitmap, SDL_RWops p, int version)
        {
            var b = new bitmap_definition();

            // Convert bitmap definition
            b.width = (short) SDL_ReadBE16(p);
            b.height = (short) SDL_ReadBE16(p);
            b.bytes_per_row = (short) SDL_ReadBE16(p);
            b.flags = (short) SDL_ReadBE16(p);
            b.bit_depth = (short) SDL_ReadBE16(p);

            // guess how big to make it
            int rows = (b.flags & _COLUMN_ORDER_BIT) != 0 ? b.width : b.height;
            int row_len = (b.flags & _COLUMN_ORDER_BIT) != 0 ? b.height : b.width;

            SDL_RWseek(p, 16, SEEK_CUR);

            // Skip row address pointers
            SDL_RWseek(p, (rows + 1) * sizeof(uint), SEEK_CUR);

            // the size of what follows the row pointers
            int pixel_data_size;
            if (b.bytes_per_row == NONE)
            {
                if (version == M1_SHAPES_VERSION)
                {
                    // make enough room for the definition, then append as we convert RLE
                    pixel_data_size = 0;
                }
                else
                {
                    // ugly--figure out how big it's going to be

                    int size = 0;
                    for (int j = 0; j < rows; j++)
                    {
                        short first = (short) SDL_ReadBE16(p);
                        short last = (short) SDL_ReadBE16(p);
                        size += 4;
                        SDL_RWseek(p, last - first, SEEK_CUR);
                        size += last - first;
                    }

                    pixel_data_size = size;

                    // Now, seek back
                    SDL_RWseek(p, -size, SEEK_CUR);
                }
            }
            else
            {
                pixel_data_size = rows * b.bytes_per_row;
            }

            if (bitmap == null) bitmap = new bitmap_definition();
            bitmap_definition d = bitmap;
            d.width = b.width;
            d.height = b.height;
            d.bytes_per_row = b.bytes_per_row;
            d.flags = b.flags;
            d.flags = (short) (d.flags & ~_PATCHED_BIT); // Anvil sets unused flags :( we'll set it later
            d.bit_depth = b.bit_depth;

            // Skip row address pointers
            d.row_addresses = new int[Math.Max(rows, 1)];
            d.pixels = new byte[pixel_data_size];
            int c = 0;

            // Copy bitmap data
            if (d.bytes_per_row == NONE)
            {
                // RLE format

                if (version == M1_SHAPES_VERSION)
                {
                    var converted = new List<byte>();
                    convert_m1_rle(converted, rows, row_len, p);
                    d.pixels = converted.ToArray();
                }
                else
                {
                    for (int j = 0; j < rows; j++)
                    {
                        short first = (short) SDL_ReadBE16(p);
                        short last = (short) SDL_ReadBE16(p);
                        d.pixels[c++] = (byte) (first >> 8);
                        d.pixels[c++] = (byte) (first);
                        d.pixels[c++] = (byte) (last >> 8);
                        d.pixels[c++] = (byte) (last);
                        SDL_RWread(p, d.pixels, c, 1, last - first);
                        c += last - first;
                    }
                }
            }
            else
            {
                SDL_RWread(p, d.pixels, c, d.bytes_per_row, rows);
                c += rows * d.bytes_per_row;
            }
        }

        private static void allocate_shading_tables(short collection_index, bool strip)
        {
            // Not ported: the shading tables
        }

        /*
         *  Load collection
         */

        private static bool load_collection(short collection_index, bool strip)
        {
            SDL_RWops p;
            int src_offset;

            collection_header header = get_collection_header(collection_index);

            if (shapes_file_version == M1_SHAPES_VERSION)
            {
                // Collections are stored in .256 resources
                // Not ported: reading resource forks (M1ShapesFile.Get('.', '2', '5', '6', 128 + collection_index, r))
                return false;
            }
            else
            {
                // Get offset and length of data in source file from header

                if (bit_depth == 8 || header.offset16 == -1)
                {
                    if (header.offset == -1)
                    {
                        return false;
                    }

                    src_offset = header.offset;
                }
                else
                {
                    src_offset = header.offset16;
                }

                p = ShapesFile.GetRWops();
                ShapesFile.SetPosition(0);
                src_offset += (int) SDL_RWtell(p);
            }

            // Read collection definition
            var cd = new collection_definition();
            SDL_RWseek(p, src_offset, RW_SEEK_SET);
            load_collection_definition(cd, p);
            header.status = (short) (header.status & ~markPATCHED);

            // Convert CLUTS
            if (cd.clut_count != 0 && cd.color_count != 0)
            {
                SDL_RWseek(p, src_offset + cd.color_table_offset, RW_SEEK_SET);
                load_clut(cd.color_tables, 0, cd.clut_count * cd.color_count, p);
            }

            // Convert high-level shape definitions
            if (cd.high_level_shape_count != 0)
            {
                SDL_RWseek(p, src_offset + cd.high_level_shape_offset_table_offset, RW_SEEK_SET);
                uint[] t = read_offset_table(p, cd.high_level_shape_count);

                for (int i = 0; i < cd.high_level_shape_count; i++)
                {
                    SDL_RWseek(p, src_offset + t[i], RW_SEEK_SET);
                    load_high_level_shape(ref cd.high_level_shapes[i], p);
                }
            }

            // Convert low-level shape definitions
            if (cd.low_level_shape_count != 0)
            {
                SDL_RWseek(p, src_offset + cd.low_level_shape_offset_table_offset, RW_SEEK_SET);
                uint[] t = read_offset_table(p, cd.low_level_shape_count);

                for (int i = 0; i < cd.low_level_shape_count; i++)
                {
                    SDL_RWseek(p, src_offset + t[i], RW_SEEK_SET);
                    load_low_level_shape(cd.low_level_shapes[i], p);
                }
            }

            // Convert bitmap definitions
            if (cd.bitmap_count != 0)
            {
                SDL_RWseek(p, src_offset + cd.bitmap_offset_table_offset, RW_SEEK_SET);
                uint[] t = read_offset_table(p, cd.bitmap_count);

                for (int i = 0; i < cd.bitmap_count; i++)
                {
                    SDL_RWseek(p, src_offset + t[i], RW_SEEK_SET);
                    load_bitmap(ref cd.bitmaps[i], p, shapes_file_version);
                }
            }

            header.collection = cd;

            if (strip)
            {
                //!! don't know what to do
                vhalt("Stripped shapes not implemented");
            }

            allocate_shading_tables(collection_index, strip);

            // if (header->shading_tables.empty()) { ... return false; }

            // Everything OK
            return true;
        }

        // std::vector<uint32> t(count); SDL_RWread(p, &t[0], sizeof(uint32), count); byte_swap_memory(&t[0], _4byte, count);
        private static uint[] read_offset_table(SDL_RWops p, int count)
        {
            var raw = new byte[count * sizeof(uint)];
            SDL_RWread(p, raw, 0, sizeof(uint), count);
            var t = new uint[count];
            StreamToList(new StreamPointer(raw), t, count);
            return t;
        }

        /*
         *  Unload collection
         */

        private static void unload_collection(collection_header header)
        {
            assert(header.collection != null);
            header.collection = null;
            header.shading_tables = new byte[0];
        }

        public const uint ENDC_TAG = 0x656E6463; // 'endc'
        public const uint CLDF_TAG = 0x636C6466; // 'cldf'
        public const uint HLSH_TAG = 0x686C7368; // 'hlsh'
        public const uint LLSH_TAG = 0x6C6C7368; // 'llsh'
        public const uint BMAP_TAG = 0x626D6170; // 'bmap'
        public const uint CTAB_TAG = 0x63746162; // 'ctab'

        private static byte[] shapes_patch = new byte[0];

        public static void set_shapes_patch_data(byte[] data, int length)
        {
            if (length == 0)
            {
                shapes_patch = new byte[0];
            }
            else
            {
                shapes_patch = new byte[length];
                Buffer.BlockCopy(data, 0, shapes_patch, 0, length);
            }
        }

        public static byte[] get_shapes_patch_data(out int length)
        {
            length = shapes_patch.Length;
            return length != 0 ? shapes_patch : null;
        }

        public static void load_shapes_patch(SDL_RWops p, bool override_replacements)
        {
            var color_counts = new short[MAXIMUM_COLLECTIONS];
            int start = (int) SDL_RWtell(p);
            SDL_RWseek(p, 0, SEEK_END);
            int end = (int) SDL_RWtell(p);

            SDL_RWseek(p, start, SEEK_SET);

            bool done = false;
            while (!done)
            {
                // is there more data to read?
                if (SDL_RWtell(p) < end)
                {
                    int collection_index = (int) SDL_ReadBE32(p);
                    int patch_bit_depth = (int) SDL_ReadBE32(p);

                    bool collection_end = false;
                    while (!collection_end)
                    {
                        // ForgePlus: Aleph One loops forever at the end of a truncated patch
                        if (SDL_RWtell(p) >= end)
                        {
                            break;
                        }

                        // read a tag
                        uint tag = SDL_ReadBE32(p);
                        if (tag == ENDC_TAG)
                        {
                            collection_end = true;
                        }
                        else if (tag == CLDF_TAG)
                        {
                            // a collection follows directly
                            collection_header header = get_collection_header((short) collection_index);
                            if (collection_loaded(header) && patch_bit_depth == 8)
                            {
                                load_collection_definition(header.collection, p);
                                color_counts[collection_index] = header.collection.color_count;
                                allocate_shading_tables((short) collection_index, false);
                                header.status |= markPATCHED;
                            }
                            else
                            {
                                // get the color count (it's the only way to skip the CTAB_TAG
                                SDL_RWseek(p, 6, SEEK_CUR);
                                color_counts[collection_index] = (short) SDL_ReadBE16(p);
                                SDL_RWseek(p, 544 - 8, SEEK_CUR);
                            }
                        }
                        else if (tag == HLSH_TAG)
                        {
                            collection_definition cd = get_collection_definition((short) collection_index);
                            int high_level_shape_index = (int) SDL_ReadBE32(p);
                            int size = (int) SDL_ReadBE32(p);
                            int pos = (int) SDL_RWtell(p);
                            if (cd != null && patch_bit_depth == 8 && (uint) high_level_shape_index < (uint) cd.high_level_shapes.Length)
                            {
                                load_high_level_shape(ref cd.high_level_shapes[high_level_shape_index], p);
                                SDL_RWseek(p, pos + size, SEEK_SET);
                            }
                            else
                            {
                                SDL_RWseek(p, size, SEEK_CUR);
                            }
                        }
                        else if (tag == LLSH_TAG)
                        {
                            collection_definition cd = get_collection_definition((short) collection_index);
                            int low_level_shape_index = (int) SDL_ReadBE32(p);
                            if (cd != null && patch_bit_depth == 8 && (uint) low_level_shape_index < (uint) cd.low_level_shapes.Length)
                            {
                                load_low_level_shape(cd.low_level_shapes[low_level_shape_index], p);
                            }
                            else
                            {
                                SDL_RWseek(p, 36, SEEK_CUR);
                            }
                        }
                        else if (tag == BMAP_TAG)
                        {
                            collection_definition cd = get_collection_definition((short) collection_index);
                            int bitmap_index = (int) SDL_ReadBE32(p);
                            int size = (int) SDL_ReadBE32(p);
                            if (cd != null && patch_bit_depth == 8 && (uint) bitmap_index < (uint) cd.bitmaps.Length)
                            {
                                load_bitmap(ref cd.bitmaps[bitmap_index], p, M2_SHAPES_VERSION);
                                if (override_replacements)
                                {
                                    bitmap_definition bitmap = get_bitmap_definition((short) collection_index, (short) bitmap_index);
                                    bitmap.flags = (short) (bitmap.flags | _PATCHED_BIT);
                                }
                            }
                            else
                            {
                                SDL_RWseek(p, size, SEEK_CUR);
                            }
                        }
                        else if (tag == CTAB_TAG)
                        {
                            collection_definition cd = get_collection_definition((short) collection_index);
                            int color_table_index = (int) SDL_ReadBE32(p);
                            if (cd != null && patch_bit_depth == 8 && ((uint) (color_table_index * cd.color_count) < (uint) cd.color_tables.Length))
                            {
                                // sic: &color_tables[color_table_index]
                                load_clut(cd.color_tables, color_table_index, cd.color_count, p);
                            }
                            else
                            {
                                SDL_RWseek(p, color_counts[collection_index] * SIZEOF_rgb_color_value, SEEK_CUR);
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine(string.Format("Unrecognized tag in patch file '{0}{1}{2}{3}'\n {4:x}",
                                (char) (byte) (tag >> 24), (char) (byte) (tag >> 16), (char) (byte) (tag >> 8), (char) (byte) tag, tag));
                        }
                    }
                }
                else
                {
                    done = true;
                }
            }
        }

        /* ---------- code */

        /* --------- private code */

        // Not ported: initialize_shape_handler()

        public static void open_shapes_file(FileSpecifier File)
        {
            bool m1_loaded = false;
            // Not ported: if (File.Open(M1ShapesFile) && M1ShapesFile.Check('.','2','5','6',128)) ...

            if (!m1_loaded && File.Open(ShapesFile))
            {
                shapes_file_version = M2_SHAPES_VERSION;
                // Load the collection headers;
                // need a buffer for the packed data
                int Size = MAXIMUM_COLLECTIONS * SIZEOF_collection_header;
                var CollHdrStream = new byte[Size];
                if (!ShapesFile.Read(Size, CollHdrStream))
                {
                    ShapesFile.Close();
                    return;
                }

                // Unpack them
                var S = new StreamPointer(CollHdrStream);
                int Count = MAXIMUM_COLLECTIONS;

                for (int k = 0; k < Count; k++)
                {
                    collection_header ObjPtr = collection_headers[k];

                    StreamToValue(S, out ObjPtr.status);
                    StreamToValue(S, out ObjPtr.flags);

                    StreamToValue(S, out ObjPtr.offset);
                    StreamToValue(S, out ObjPtr.length);
                    StreamToValue(S, out ObjPtr.offset16);
                    StreamToValue(S, out ObjPtr.length16);

                    S.Skip(6 * 2);

                    ObjPtr.collection = null; // so unloading can work properly
                    ObjPtr.shading_tables = new byte[0]; // so unloading can work properly
                }

                assert(S.Position == Count * SIZEOF_collection_header);
            }

            // Not ported: set_shapes_images_file(File);
        }

        public static void close_shapes_file()
        {
            if (shapes_file_version == M1_SHAPES_VERSION)
            {
                // Not ported: M1ShapesFile.Close();
            }
            else
            {
                ShapesFile.Close();
            }
        }

        private static bool collection_loaded(
            collection_header header)
        {
            return header.collection != null ? true : false;
        }

        public static bool collection_loaded(short collection_index)
        {
            collection_header header = get_collection_header(collection_index);
            return collection_loaded(header);
        }

        public static bool can_load_collection(short collection_index)
        {
            if (collection_index >= 0 && collection_index < NUMBER_OF_COLLECTIONS)
            {
                collection_header header = get_collection_header(collection_index);
                if (header != null)
                {
                    return (header.offset != -1 || header.offset16 != -1);
                }
            }

            return false;
        }

        private static void lock_collection(
            collection_header header)
        {
            // nothing to do
        }

        private static void unlock_collection(
            collection_header header)
        {
            // nothing to do
        }

        public static void unload_all_collections()
        {
            collection_header header;
            short collection_index;

            for (collection_index = 0; collection_index < MAXIMUM_COLLECTIONS; ++collection_index)
            {
                header = collection_headers[collection_index];
                if (collection_loaded(header))
                {
                    unload_collection(header);
                }
                // Not ported: OGL_UnloadModelsImages(collection_index);
            }
        }

        public static void mark_collection(
            short collection_code,
            bool loading)
        {
            if (collection_code != NONE)
            {
                short collection_index = (short) GET_COLLECTION(collection_code);

                assert(collection_index >= 0 && collection_index < MAXIMUM_COLLECTIONS);
                collection_headers[collection_index].status |= loading ? markLOAD : markUNLOAD;
            }
        }

        public static void strip_collection(
            short collection_code)
        {
            if (collection_code != NONE)
            {
                short collection_index = (short) GET_COLLECTION(collection_code);

                assert(collection_index >= 0 && collection_index < MAXIMUM_COLLECTIONS);
                collection_headers[collection_index].status |= markSTRIP;
            }
        }

        /* returns count, doesn't fill NULL buffer */
        public static short get_shape_descriptors(
            short shape_type,
            ushort[] buffer)
        {
            short collection_index, low_level_shape_index;
            short appropriate_type = 0;
            short count;
            int buffer_index = 0;

            switch (shape_type)
            {
                case _wall_shape: appropriate_type = _wall_collection; break;
                case _floor_or_ceiling_shape: appropriate_type = _wall_collection; break;
                default:
                    assert(false);
                    break;
            }

            count = 0;
            for (collection_index = 0; collection_index < MAXIMUM_COLLECTIONS; ++collection_index)
            {
                collection_definition collection = get_collection_definition(collection_index);
                // Skip over nonexistent collections, frames, and bitmaps.
                if (collection == null) continue;

                if (collection != null && collection.type == appropriate_type)
                {
                    for (low_level_shape_index = 0; low_level_shape_index < collection.low_level_shape_count; ++low_level_shape_index)
                    {
                        low_level_shape_definition low_level_shape = get_low_level_shape_definition(collection_index, low_level_shape_index);
                        if (low_level_shape == null) continue;
                        bitmap_definition bitmap = get_bitmap_definition(collection_index, low_level_shape.bitmap_index);
                        if (bitmap == null) continue;

                        count += collection.clut_count;
                        if (buffer != null)
                        {
                            short clut;

                            for (clut = 0; clut < collection.clut_count; ++clut)
                            {
                                buffer[buffer_index++] = BUILD_DESCRIPTOR(BUILD_COLLECTION(collection_index, clut), low_level_shape_index);
                            }
                        }
                    }
                }
            }

            return count;
        }

        // Not ported: the shading_tables argument
        public static void extended_get_shape_bitmap_and_shading_table(
            short collection_code,
            short low_level_shape_index,
            out bitmap_definition bitmap,
            short shading_mode)
        {
            // if (collection_code==_collection_marathon_control_panels) collection_code= 30, low_level_shape_index= 0;
            short collection_index = (short) GET_COLLECTION(collection_code);
            short clut_index = (short) GET_COLLECTION_CLUT(collection_code);

            // Forget about it if some one managed to call us with the NONE value
            assert(!(clut_index + 1 == MAXIMUM_CLUTS_PER_COLLECTION &&
                     collection_index + 1 == MAXIMUM_COLLECTIONS &&
                     low_level_shape_index + 1 == MAXIMUM_SHAPES_PER_COLLECTION));

            low_level_shape_definition low_level_shape = get_low_level_shape_definition(collection_index, low_level_shape_index);
            // Return NULL pointers for bitmap and shading table if the frame does not exist
            if (low_level_shape == null)
            {
                bitmap = null;
                return;
            }

            bitmap = get_bitmap_definition(collection_index, low_level_shape.bitmap_index);

            switch (shading_mode)
            {
                case _shading_normal:
                    // *shading_tables= get_collection_shading_tables(collection_index, clut_index);
                    break;
                case _shading_infravision:
                    // *shading_tables= get_collection_tint_tables(collection_index, 0);
                    break;

                default:
                    assert(false);
                    break;
            }
        }

        // Aleph One returns the low-level shape cast to a shape_information_data; this returns a copy
        public static shape_information_data extended_get_shape_information(
            short collection_code,
            short low_level_shape_index)
        {
            if ((GET_COLLECTION(collection_code) < 0) || (GET_COLLECTION(collection_code) >= NUMBER_OF_COLLECTIONS)) return null;
            if (low_level_shape_index < 0) return null;
            short collection_index = (short) GET_COLLECTION(collection_code);
            low_level_shape_definition low_level_shape;

            low_level_shape = get_low_level_shape_definition(collection_index, low_level_shape_index);
            if (low_level_shape == null) return null;

            var shape_information = new shape_information_data();
            shape_information.flags = low_level_shape.flags;
            shape_information.minimum_light_intensity = low_level_shape.minimum_light_intensity;
            shape_information.unused[0] = low_level_shape.bitmap_index;
            shape_information.unused[1] = low_level_shape.origin_x;
            shape_information.unused[2] = low_level_shape.origin_y;
            shape_information.unused[3] = low_level_shape.key_x;
            shape_information.unused[4] = low_level_shape.key_y;
            shape_information.world_left = low_level_shape.world_left;
            shape_information.world_right = low_level_shape.world_right;
            shape_information.world_top = low_level_shape.world_top;
            shape_information.world_bottom = low_level_shape.world_bottom;
            shape_information.world_x0 = low_level_shape.world_x0;
            shape_information.world_y0 = low_level_shape.world_y0;
            return shape_information;
        }

        public static void process_collection_sounds(
            short collection_code,
            Action<short> process_sound)
        {
            short collection_index = (short) GET_COLLECTION(collection_code);
            collection_definition collection = get_collection_definition(collection_index);
            // Skip over processing unloaded collections and sequences
            if (collection == null) return;

            short high_level_shape_index;

            for (high_level_shape_index = 0; high_level_shape_index < collection.high_level_shape_count; ++high_level_shape_index)
            {
                high_level_shape_definition high_level_shape = get_high_level_shape_definition(collection_index, high_level_shape_index);
                if (high_level_shape == null) return;

                process_sound(high_level_shape.first_frame_sound);
                process_sound(high_level_shape.key_frame_sound);
                process_sound(high_level_shape.last_frame_sound);
            }
        }

        // Aleph One returns &high_level_shape->number_of_views cast to a shape_animation_data; this returns a
        // copy, sharing the high-level shape's arrays
        public static shape_animation_data get_shape_animation_data(
            ushort shape)
        {
            short collection_index, high_level_shape_index;
            high_level_shape_definition high_level_shape;

            collection_index = (short) GET_COLLECTION(GET_DESCRIPTOR_COLLECTION(shape));
            high_level_shape_index = (short) GET_DESCRIPTOR_SHAPE(shape);
            high_level_shape = get_high_level_shape_definition(collection_index, high_level_shape_index);
            if (high_level_shape == null) return null;

            var animation = new shape_animation_data();
            animation.number_of_views = high_level_shape.number_of_views;
            animation.frames_per_view = high_level_shape.frames_per_view;
            animation.ticks_per_frame = high_level_shape.ticks_per_frame;
            animation.key_frame = high_level_shape.key_frame;
            animation.transfer_mode = high_level_shape.transfer_mode;
            animation.transfer_mode_period = high_level_shape.transfer_mode_period;
            animation.first_frame_sound = high_level_shape.first_frame_sound;
            animation.key_frame_sound = high_level_shape.key_frame_sound;
            animation.last_frame_sound = high_level_shape.last_frame_sound;
            animation.pixels_to_world = high_level_shape.pixels_to_world;
            animation.loop_frame = high_level_shape.loop_frame;
            animation.unused = high_level_shape.unused;
            animation.low_level_shape_indexes = high_level_shape.low_level_shape_indexes;
            return animation;
        }

        // Not ported: get_global_shading_table()

        public static void load_collections(
            bool with_progress_bar,
            bool is_opengl)
        {
            collection_header header;
            short collection_index;

            // Not ported: precalculate_bit_depth_constants();

            /* first go through our list of shape collections and dispose of any collections which
                were marked for unloading.  at the same time, unlock all those collections which
                will be staying (so the heap can move around) */
            for (collection_index = 0; collection_index < MAXIMUM_COLLECTIONS; ++collection_index)
            {
                header = collection_headers[collection_index];
                if (true)
                {
                    if (collection_loaded(header))
                    {
                        unload_collection(header);
                    }
                    // Not ported: OGL_UnloadModelsImages(collection_index); SW_Texture_Extras::instance()->Unload(collection_index);
                }
            }

            /* ... then go back through the list of collections and load any that we were asked to */
            for (collection_index = 0; collection_index < MAXIMUM_COLLECTIONS; ++collection_index)
            {
                header = collection_headers[collection_index];
                /* don't reload collections which are already in memory, but do lock them */
                if (collection_loaded(header))
                {
                    // In case the substitute images had been changed by some level-specific MML...
                    // OGL_LoadModelsImages(collection_index);
                    lock_collection(header);
                }
                else
                {
                    if ((header.status & markLOAD) != 0)
                    {
                        /* load and decompress collection */
                        if (!load_collection(collection_index, (header.status & markSTRIP) != 0 ? true : false))
                        {
                            if (shapes_file_version != M1_SHAPES_VERSION)
                            {
                                // alert_out_of_memory();
                                vhalt(string.Format("alert_out_of_memory: couldn't load collection {0}", collection_index));
                            }
                        }
                        // OGL_LoadModelsImages(collection_index);
                    }
                }

                /* clear action flags */
                header.status = markNONE;
                header.flags = 0;
            }

            // Not ported: Plugins::instance()->load_shapes_patches(is_opengl);

            if (shapes_patch.Length != 0)
            {
                SDL_RWops f = SDL_RWFromMem(shapes_patch, shapes_patch.Length);
                load_shapes_patch(f, true);
                SDL_RWclose(f);
            }

            /* remap the shapes, recalculate row base addresses, build our new world color table and
                (finally) update the screen to reflect our changes */
            update_color_environment(is_opengl);

            // Not ported: SW_Texture_Extras::instance()->Load(collection_index);
        }

        // Not ported: count_replacement_collections(), load_replacement_collections()

        /* ---------- private code */

        // Only the row addresses; the rest builds the screen color table and remaps bitmaps into it
        private static void update_color_environment(
            bool is_opengl)
        {
            short collection_index;
            short bitmap_index;

            /* loop through all collections, only paying attention to the loaded ones. */
            for (collection_index = 0; collection_index < MAXIMUM_COLLECTIONS; ++collection_index)
            {
                collection_definition collection = get_collection_definition(collection_index);

                if (collection != null && collection.bitmap_count != 0)
                {
                    // Not ported: adding the primary colors to the remapping table

                    /* then remap the collection and recalculate the base addresses of each bitmap */
                    for (bitmap_index = 0; bitmap_index < collection.bitmap_count; ++bitmap_index)
                    {
                        bitmap_definition bitmap = get_bitmap_definition(collection_index, bitmap_index);
                        assert(bitmap != null);

                        /* calculate row base addresses ... */
                        bitmap.row_addresses[0] = calculate_bitmap_origin(bitmap);
                        precalculate_bitmap_row_addresses(bitmap);

                        /* ... and remap it */
                        // Not ported: remap_bitmap(bitmap, remapping_table);
                    }

                    // Not ported: shading tables for each clut, build_collection_tinting_table, _change_clut.
                }
            }
        }

        /* ---------- collection accessors */
        // Some originally from shapes_macintosh.c

        public static collection_header get_collection_header(
            short collection_index)
        {
            // This one is intended to bomb because collection indices can only be from 1 to 31,
            // short of drastic changes in how collection indices are specified (a bigger structure
            // than shape_descriptor, for example).
            collection_header header = ((uint) collection_index < MAXIMUM_COLLECTIONS) ? collection_headers[collection_index] : null;
            vassert(header != null, string.Format("Collection index out of range: {0}", collection_index));

            return header;
        }

        public static collection_definition get_collection_definition(
            short collection_index)
        {
            return get_collection_header(collection_index).collection;
        }

        // The rgb_color_value * into color_tables is an array of the CLUT's colors (the same objects)
        public static rgb_color_value[] get_collection_colors(
            short collection_index,
            short clut_number)
        {
            collection_definition definition = get_collection_definition(collection_index);
            if (definition == null) return null;
            if (!(clut_number >= 0 && clut_number < definition.clut_count))
                return null;

            return clut_slice(definition, clut_number);
        }

        public static rgb_color_value[] get_collection_colors(
            short collection_index,
            short clut_number,
            ref int num_colors)
        {
            collection_definition definition = get_collection_definition(collection_index);
            if (definition == null) return null;
            if (!(clut_number >= 0 && clut_number < definition.clut_count)) return null;
            num_colors = definition.color_count;
            return clut_slice(definition, clut_number);
        }

        private static rgb_color_value[] clut_slice(collection_definition definition, short clut_number)
        {
            var colors = new rgb_color_value[definition.color_count];
            Array.Copy(definition.color_tables, clut_number * definition.color_count, colors, 0, definition.color_count);
            return colors;
        }

        public static high_level_shape_definition get_high_level_shape_definition(
            short collection_index,
            short high_level_shape_index)
        {
            collection_definition definition = get_collection_definition(collection_index);
            if (definition == null) return null;

            if (!(high_level_shape_index >= 0 && high_level_shape_index < definition.high_level_shapes.Length))
                return null;

            if (definition.high_level_shapes[high_level_shape_index] == null)
                return null;

            return definition.high_level_shapes[high_level_shape_index];
        }

        public static low_level_shape_definition get_low_level_shape_definition(
            short collection_index,
            short low_level_shape_index)
        {
            collection_definition definition = get_collection_definition(collection_index);
            if (definition == null) return null;
            if (low_level_shape_index >= 0 && low_level_shape_index < definition.low_level_shapes.Length)
            {
                return definition.low_level_shapes[low_level_shape_index];
            }
            else
                return null;
        }

        public static bitmap_definition get_bitmap_definition(
            short collection_index,
            short bitmap_index)
        {
            collection_definition definition = get_collection_definition(collection_index);
            if (definition == null) return null;
            if (!(bitmap_index >= 0 && bitmap_index < definition.bitmaps.Length))
                return null;

            if (definition.bitmaps[bitmap_index] == null)
                return null;

            return definition.bitmaps[bitmap_index];
        }

        // Not ported: get_collection_shading_tables(), get_collection_tint_tables()

        // LP additions:

        // Whether or not collection is present
        public static bool is_collection_present(short collection_index)
        {
            collection_header CollHeader = get_collection_header(collection_index);
            if (CollHeader == null) return false;
            return collection_loaded(CollHeader);
        }

        // Number of texture frames in a collection (good for wall-texture error checking)
        public static short get_number_of_collection_frames(short collection_index)
        {
            collection_definition Collection = get_collection_definition(collection_index);
            if (Collection == null) return 0;
            return Collection.low_level_shape_count;
        }

        // Number of bitmaps in a collection (good for allocating texture information for OpenGL)
        public static short get_number_of_collection_bitmaps(short collection_index)
        {
            collection_definition Collection = get_collection_definition(collection_index);
            if (Collection == null) return 0;
            return Collection.bitmap_count;
        }

        // Which bitmap index for a frame (good for OpenGL texture rendering)
        public static short get_bitmap_index(short collection_index, short low_level_shape_index)
        {
            low_level_shape_definition low_level_shape = get_low_level_shape_definition(collection_index, low_level_shape_index);
            if (low_level_shape == null) return NONE;
            return low_level_shape.bitmap_index;
        }

        // Not ported: infravision tint colors and MML
    }
}
