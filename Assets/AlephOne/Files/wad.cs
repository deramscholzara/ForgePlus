// Port of Aleph One: Source_Files/Files/wad.h, wad.cpp
//
// Not ported: flat wad data (get_flat_data, inflate_flat_data), which only exists to transfer
// levels over the network, and dump_wad (debug output).
//
// Aleph One's read-only wads point into the raw buffer; here every tag owns a copy of its data,
// so read-only and modifiable wads behave identically apart from the read_only_data marker.
using System;
using System.Text;
using static AlephOne.csalerts;
using static AlephOne.cstypes;
using static AlephOne.game_errors;
using static AlephOne.Packing;

namespace AlephOne
{
    /* ------------- file structures */
    public class wad_header /* 128 bytes */
    {
        public short version;                                       /* Used internally */
        public short data_version;                                  /* Used by the data.. */
        public byte[] file_name = new byte[wad.MAXIMUM_WADFILE_NAME_LENGTH];
        public uint checksum;
        public int directory_offset;
        public short wad_count;
        public short application_specific_directory_data_size;
        public short entry_header_size;
        public short directory_entry_base_size;
        public uint parent_checksum;  /* If non-zero, this is the checksum of our parent, and we are simply modifications! */
        public short[] unused = new short[20];
    }

    public class old_directory_entry /* 8 bytes */
    {
        public int offset_to_start; /* From start of file */
        public int length; /* Of total level */
    }

    public class directory_entry /* >=10 bytes */
    {
        public int offset_to_start; /* From start of file */
        public int length; /* Of total level */
        public short index; /* For inplace modification of the wadfile! */
    }

    public class old_entry_header /* 12 bytes */
    {
        public uint tag;
        public int next_offset; /* From current file location-> ie directory_entry.offset_to_start+next_offset */
        public int length; /* Of entry */
    }

    public class entry_header /* 16 bytes */
    {
        public uint tag;
        public int next_offset; /* From current file location-> ie directory_entry.offset_to_start+next_offset */
        public int length; /* Of entry */
        public int offset; /* Offset for inplace expansion of data */
    }

    /* ---------- Memory Data structures ------------ */
    public class tag_data
    {
        public uint tag;        /* What type of data is this? */
        public byte[] data;     /* Offset into the wad.. */
        public int length;      /* Length of the data */
        public int offset;      /* Offset for patches */
    }

    /* This is what a wad * actually is.. */
    public class wad_data
    {
        public short tag_count;         /* Tag count */
        public short padding;
        public byte[] read_only_data;   /* If this is non NULL, we are read only.... */
        public tag_data[] tag_data;     /* Tag data array */
    }

    public static class wad
    {
        public const short PRE_ENTRY_POINT_WADFILE_VERSION = 0;
        public const short WADFILE_HAS_DIRECTORY_ENTRY = 1;
        public const short WADFILE_SUPPORTS_OVERLAYS = 2;
        public const short WADFILE_HAS_INFINITY_STUFF = 4;

        public const short CURRENT_WADFILE_VERSION = (WADFILE_HAS_INFINITY_STUFF);

        public const int MAXIMUM_DIRECTORY_ENTRIES_PER_FILE = 64;
        public const int MAXIMUM_WADFILE_NAME_LENGTH = 64;
        public const int MAXIMUM_UNION_WADFILES = 16;
        public const int MAXIMUM_OPEN_WADFILES = 3;

        public const int SIZEOF_wad_header = 128;   // don't trust sizeof()
        public const int SIZEOF_old_directory_entry = 8;
        public const int SIZEOF_directory_entry = 10;
        public const int SIZEOF_old_entry_header = 12;
        public const int SIZEOF_entry_header = 16;

        /* ----- miscellaneous functions */

        public static bool wad_file_has_checksum(FileSpecifier File, uint checksum)
        {
            return read_wad_file_checksum(File) == checksum;
        }

        public static uint read_wad_file_checksum(FileSpecifier File)
        {
            var header = new wad_header();
            var OFile = new OpenedFile();
            uint checksum = 0;

            if (open_wad_file_for_reading(File, OFile))
            {
                if (read_wad_header(OFile, header))
                {
                    checksum = header.checksum;
                }

                close_wad_file(OFile);
            }

            return checksum;
        }

        public static uint read_wad_file_parent_checksum(FileSpecifier File)
        {
            var header = new wad_header();
            var OFile = new OpenedFile();
            uint checksum = 0;

            if (open_wad_file_for_reading(File, OFile))
            {
                if (read_wad_header(OFile, header))
                {
                    checksum = header.parent_checksum;
                }

                close_wad_file(OFile);
            }

            return checksum;
        }

        public static bool wad_file_has_parent_checksum(FileSpecifier File, uint checksum)
        {
            return read_wad_file_parent_checksum(File) == checksum;
        }

        /* Find out how many wads there are in the map */
        public static short number_of_wads_in_file(FileSpecifier File) /* returns -1 on error */
        {
            short count = NONE;
            var OFile = new OpenedFile();

            if (open_wad_file_for_reading(File, OFile))
            {
                var header = new wad_header();

                /* read the header */
                read_wad_header(OFile, header);
                count = header.wad_count;

                close_wad_file(OFile);
            }

            return count;
        }

        /* ----- Open/Close functions */

        public static bool create_wadfile(FileSpecifier File)
        {
            return File.Create();
        }

        private static bool open_wad_file_or_set_error(FileSpecifier File, OpenedFile OFile, bool Writable)
        {
            if (!File.Open(OFile, Writable))
            {
                set_game_error(systemError, (short) File.GetError());
                return false;
            }

            return true;
        }

        public static bool open_wad_file_for_reading(FileSpecifier File, OpenedFile OFile)
        {
            return open_wad_file_or_set_error(File, OFile, false);
        }

        public static bool open_wad_file_for_writing(FileSpecifier File, OpenedFile OFile)
        {
            return open_wad_file_or_set_error(File, OFile, true);
        }

        public static void close_wad_file(OpenedFile File)
        {
            File.Close();
        }

        /* ----- Read File functions */

        /* Read the header from the wad file */
        public static bool read_wad_header(OpenedFile OFile, wad_header header)
        {
            int error = 0;
            bool success = true;

            var buffer = new byte[SIZEOF_wad_header];
            error = !read_from_file(OFile, 0, buffer, SIZEOF_wad_header) ? 1 : 0;
            unpack_wad_header(new StreamPointer(buffer), header, 1);

            if (error != 0)
            {
                set_game_error(systemError, (short) error);
                success = false;
            }
            else
            {
                // Thomas Herzog made this error checking more careful
                if ((header.version > CURRENT_WADFILE_VERSION) || (header.data_version > 2) || (header.wad_count < 1))
                {
                    set_game_error(gameError, errUnknownWadVersion);
                    success = false;
                }
            }

            return success;
        }

        /* Read the indexed wad from the file */
        public static wad_data read_indexed_wad_from_file(OpenedFile OFile, wad_header header, short index, bool read_only)
        {
            wad_data read_wad = null;
            byte[] raw_wad = null;
            int length = 0;
            int error = 0;

            if (size_of_indexed_wad(OFile, header, index, out length))
            {
                // The padding is so that one can use later-Marathon entry-header reading
                // on Marathon 1 wadfiles, which have a shorter entry header
                int padded_length = length + (SIZEOF_entry_header - SIZEOF_old_entry_header);

                raw_wad = new byte[padded_length];

                /* Read into the buffer */
                if (read_indexed_wad_from_file_into_buffer(OFile, header, index, raw_wad, ref length))
                {
                    /* Got the raw wad. Convert it into our internal representation... */
                    if (read_only)
                    {
                        read_wad = convert_wad_from_raw(header, raw_wad, 0, length);
                    }
                    else
                    {
                        read_wad = convert_wad_from_raw_modifiable(header, raw_wad, length);
                    }

                    if (read_wad == null)
                    {
                        /* Error.. */
                        error = 1;
                    }
                }
            }

            if (error != 0)
            {
                set_game_error(systemError, (short) error);
            }

            return read_wad;
        }

        /* Properly deal with the memory.. */
        public static void free_wad(wad_data wad)
        {
            assert(wad != null);

            // Garbage collected: just drop the references
            wad.read_only_data = null;
            wad.tag_data = null;
            wad.tag_count = 0;
        }

        public static int get_size_of_directory_data(wad_header header)
        {
            short base_entry_size = get_directory_base_length(header);

            assert(header.wad_count != 0);
            assert(header.version >= WADFILE_HAS_DIRECTORY_ENTRY || header.application_specific_directory_data_size == 0);

            return (header.wad_count *
                (header.application_specific_directory_data_size + base_entry_size));
        }

        /* -----  Read Wad functions */

        /* Given a wad, extract the given tag from it */
        public static byte[] extract_type_from_wad(wad_data wad, uint type, out int length)
        {
            byte[] return_value = null;

            length = 0;

            assert(wad != null);
            for (short index = 0; index < wad.tag_count; ++index)
            {
                if (wad.tag_data[index].tag == type)
                {
                    return_value = wad.tag_data[index].data;
                    length = wad.tag_data[index].length;
                    assert(wad.tag_data[index].length >= 0);
                    break;
                }
            }

            return return_value;
        }

        /* Calculate the length of the wad */
        public static int calculate_wad_length(wad_header file_header, wad_data wad)
        {
            short header_length = get_entry_header_length(file_header);
            int running_length = 0;

            for (short ii = 0; ii < wad.tag_count; ++ii)
            {
                running_length += wad.tag_data[ii].length + header_length;
            }

            return running_length;
        }

        /* Note wad_count and directory offset in the header! better be correct! */
        // Returns the offset, within the raw directory data, of this entry's application-specific data
        public static int get_indexed_directory_data(wad_header header, short index, byte[] directories)
        {
            int data_ptr = 0;
            short base_entry_size = get_directory_base_length(header);

            assert(header.version >= WADFILE_HAS_DIRECTORY_ENTRY);
            assert(index >= 0 && index < header.wad_count);
            data_ptr += index * (header.application_specific_directory_data_size + base_entry_size);
            data_ptr += base_entry_size; /* Because the application specific junk follows the standard entries */

            return data_ptr;
        }

        // Returns raw, unswapped directory data
        public static byte[] read_directory_data(OpenedFile OFile, wad_header header)
        {
            int size;
            byte[] data;

            assert(header.version >= WADFILE_HAS_DIRECTORY_ENTRY);
            size = get_size_of_directory_data(header);
            data = new byte[size];
            read_from_file(OFile, header.directory_offset, data, size);

            return data;
        }

        /* ------------  Write File functions */

        public static wad_data create_empty_wad()
        {
            return new wad_data(); /* IMPORTANT! (cleared) */
        }

        public static void fill_default_wad_header(FileSpecifier File, short wadfile_version, short data_version,
            short wad_count, short application_directory_data_size, wad_header header)
        {
            obj_clear(header);
            header.version = wadfile_version;
            header.data_version = data_version;

            var name = Encoding.UTF8.GetBytes(File.GetName());
            Array.Copy(name, header.file_name, Math.Min(name.Length, MAXIMUM_WADFILE_NAME_LENGTH));
            header.file_name[MAXIMUM_WADFILE_NAME_LENGTH - 1] = 0;

            header.wad_count = wad_count;
            header.application_specific_directory_data_size = application_directory_data_size;

            header.entry_header_size = get_entry_header_length(header);
            if (header.entry_header_size == 0)
            {
                /* Default.. */
                header.entry_header_size = SIZEOF_entry_header;
            }

            header.directory_entry_base_size = get_directory_base_length(header);
            if (header.directory_entry_base_size == 0)
            {
                header.directory_entry_base_size = SIZEOF_directory_entry;
            }

            /* Things left for caller to fill in: */
            /* uint32 checksum, int32 directory_offset, uint32 parent_checksum */
        }

        public static bool write_wad_header(OpenedFile OFile, wad_header header)
        {
            bool success = true;

            var buffer = new byte[SIZEOF_wad_header];
            pack_wad_header(new StreamPointer(buffer), header, 1);
            write_to_file(OFile, 0, buffer, SIZEOF_wad_header);

            return success;
        }

        // Takes raw, unswapped directory data
        public static bool write_directorys(OpenedFile OFile, wad_header header, byte[] entries)
        {
            int size_to_write = get_size_of_directory_data(header);
            bool success = true;

            assert(header.version >= WADFILE_HAS_DIRECTORY_ENTRY);
            write_to_file(OFile, header.directory_offset, entries, size_to_write);

            return success;
        }

        /* Now uses CRC to checksum.. */
        public static void calculate_and_store_wadfile_checksum(OpenedFile OFile)
        {
            var header = new wad_header();

            /* read the header */
            read_wad_header(OFile, header);

            /* Make sure we don't checksum the checksum value.. */
            header.checksum = 0;
            write_wad_header(OFile, header);

            /* Unused bytes better ALWAYS be initialized to zero.. */
            header.checksum = crc.calculate_crc_for_opened_file(OFile);

            /* Save it.. */
            write_wad_header(OFile, header);
        }

        public static bool write_wad(OpenedFile OFile, wad_header file_header, wad_data wad, int offset)
        {
            int error = 0;
            bool success;
            short entry_header_length = get_entry_header_length(file_header);
            var header = new entry_header();
            int running_offset = 0;

            assert(wad != null);
            assert(wad.read_only_data == null);
            for (short index = 0; error == 0 && index < wad.tag_count; ++index)
            {
                header.tag = wad.tag_data[index].tag;
                header.length = wad.tag_data[index].length;
                /* On older versions, this will get overwritten by the copy.. */
                header.offset = wad.tag_data[index].offset;

                if (index == wad.tag_count - 1)
                {
                    /* Last one's next offset is zero.. */
                    header.next_offset = 0;
                }
                else
                {
                    running_offset += header.length + entry_header_length;
                    header.next_offset = running_offset;
                }

                /* Write this to the file... */
                var buffer = new byte[Math.Max(SIZEOF_old_entry_header, SIZEOF_entry_header)];
                switch (entry_header_length)
                {
                    case SIZEOF_old_entry_header:
                        pack_old_entry_header(new StreamPointer(buffer), header, 1);
                        break;
                    case SIZEOF_entry_header:
                        pack_entry_header(new StreamPointer(buffer), header, 1);
                        break;
                    default:
                        vassert(false, $"Unrecognized entry-header length: {entry_header_length}");
                        break;
                }

                if (write_to_file(OFile, offset, buffer, entry_header_length))
                {
                    offset += entry_header_length;

                    /* Write the data.. */
                    write_to_file(OFile, offset, wad.tag_data[index].data, wad.tag_data[index].length);
                    {
                        offset += wad.tag_data[index].length;
                    }
                }
            }

            if (error != 0)
            {
                success = false;
                set_game_error(systemError, (short) error);
            }
            else
            {
                success = true;
            }

            return success;
        }

        public static void set_indexed_directory_offset_and_length(wad_header header, byte[] entries, short index,
            int offset, int length, short wad_index)
        {
            int data_offset;

            assert(header.version >= WADFILE_HAS_DIRECTORY_ENTRY);

            /* calculate_directory_offset is for the file, by subtracting the base, we get the actual offset.. */
            data_offset = calculate_directory_offset(header, index) - header.directory_offset;

            // LP: should be correct for packing also
            if (header.version >= WADFILE_SUPPORTS_OVERLAYS)
            {
                var entry = new directory_entry();

                entry.length = length;
                entry.offset_to_start = offset;
                entry.index = wad_index;

                pack_directory_entry(new StreamPointer(entries, data_offset), entry, 1);
            }
            else
            {
                var entry = new old_directory_entry();

                entry.length = length;
                entry.offset_to_start = offset;

                pack_old_directory_entry(new StreamPointer(entries, data_offset), entry, 1);
            }
        }

        /* ------ Write Wad Functions */

        public static wad_data append_data_to_wad(wad_data wad, uint type, byte[] data, int size, int offset) /* Allows for inplace creation of wadfiles */
        {
            short index;

            assert(size != 0); /* You can't append zero length data anymore! */
            assert(wad != null);
            assert(wad.read_only_data == null);

            /* Find the index to replace */
            for (index = 0; index < wad.tag_count; ++index)
            {
                if (wad.tag_data[index].tag == type)
                {
                    /* Just free it, and let it go. */
                    wad.tag_data[index].data = null;
                    break;
                }
            }

            /* If we are appending... */
            if (index == wad.tag_count)
            {
                var old_data = wad.tag_data;

                wad.tag_count++;

                wad.tag_data = new tag_data[wad.tag_count];
                for (int i = 0; i < wad.tag_count; i++) wad.tag_data[i] = new tag_data();

                if (old_data != null)
                {
                    Array.Copy(old_data, wad.tag_data, wad.tag_count - 1);
                }
            }

            /* Copy it in.. */
            assert(index >= 0 && index < wad.tag_count);
            wad.tag_data[index].data = new byte[size];
            Array.Copy(data, wad.tag_data[index].data, size);

            /* Setup the tag data. */
            wad.tag_data[index].tag = type;
            wad.tag_data[index].length = size;
            wad.tag_data[index].offset = offset;

            return wad;
        }

        public static void remove_tag_from_wad(wad_data wad, uint type)
        {
            short index;

            assert(wad != null);
            assert(wad.read_only_data == null);

            /* Find the index to replace */
            for (index = 0; index < wad.tag_count; ++index)
            {
                if (wad.tag_data[index].tag == type) break;
            }

            /* If we are appending... */
            if (index != wad.tag_count)
            {
                var old_data = wad.tag_data;

                wad.tag_count -= 1;

                wad.tag_data = new tag_data[wad.tag_count];

                if (old_data != null)
                {
                    /* Copy the stuff below it. */
                    Array.Copy(old_data, 0, wad.tag_data, 0, index);

                    /* Copy the stuff above it. */
                    Array.Copy(old_data, index + 1, wad.tag_data, index, wad.tag_count - index);
                }
            }
        }

        /* ------------------------------ Private Code --------------- */

        private static bool size_of_indexed_wad(OpenedFile OFile, wad_header header, short index, out int length)
        {
            var entry = new directory_entry();
            length = 0;

            if (read_indexed_directory_data(OFile, header, index, entry))
            {
                length = entry.length;
            }
            else return false;

            return true;
        }

        private static int calculate_directory_offset(wad_header header, short index)
        {
            int offset;
            int unit_size = 0;

            switch (header.version)
            {
                case PRE_ENTRY_POINT_WADFILE_VERSION:
                    assert(header.application_specific_directory_data_size == 0);
                    // OK for Marathon 1
                    goto case WADFILE_HAS_DIRECTORY_ENTRY;
                case WADFILE_HAS_DIRECTORY_ENTRY:
                case WADFILE_SUPPORTS_OVERLAYS:
                // LP addition:
                case WADFILE_HAS_INFINITY_STUFF:
                    assert(header.application_specific_directory_data_size >= 0);
                    unit_size = header.application_specific_directory_data_size + get_directory_base_length(header);
                    break;

                default:
                    vhalt($"what is version {header.version}?");
                    break;
            }

            /* Now actually calculate it (Note that the directory_entry data is first) */
            offset = header.directory_offset + (index * unit_size);

            return offset;
        }

        private static short get_entry_header_length(wad_header header)
        {
            short size;

            assert(header != null);

            switch (header.version)
            {
                case PRE_ENTRY_POINT_WADFILE_VERSION:
                case WADFILE_HAS_DIRECTORY_ENTRY:
                    size = SIZEOF_old_entry_header;
                    break;

                default:
                    /* After this point, I stored it. */
                    size = header.entry_header_size;
                    break;
            }

            return size;
        }

        private static short get_directory_base_length(wad_header header)
        {
            short size;

            assert(header != null);
            assert(header.version <= CURRENT_WADFILE_VERSION);

            switch (header.version)
            {
                case PRE_ENTRY_POINT_WADFILE_VERSION:
                case WADFILE_HAS_DIRECTORY_ENTRY:
                    size = SIZEOF_old_directory_entry;
                    break;

                default:
                    /* After this point, I stored it. */
                    size = header.directory_entry_base_size;
                    break;
            }

            return size;
        }

        /* This searches the directories for the given index, to allow for special replacements. */
        private static bool read_indexed_directory_data(OpenedFile OFile, wad_header header, short index, directory_entry entry)
        {
            short base_entry_size;
            int offset;

            /* Get the sizes of the data structures */
            base_entry_size = get_directory_base_length(header);

            /* For old files, the index==the actual index */
            if (header.version <= WADFILE_HAS_DIRECTORY_ENTRY)
            {
                /* Calculate the offset */
                offset = calculate_directory_offset(header, index);

                /* Read it! */
                assert(base_entry_size <= SIZEOF_directory_entry);

                var buffer = new byte[Math.Max(SIZEOF_old_directory_entry, SIZEOF_directory_entry)];
                if (!read_from_file(OFile, offset, buffer, base_entry_size))
                    return false;

                switch (base_entry_size)
                {
                    case SIZEOF_old_directory_entry:
                        unpack_old_directory_entry(new StreamPointer(buffer), entry, 1);
                        break;
                    case SIZEOF_directory_entry:
                        unpack_directory_entry(new StreamPointer(buffer), entry, 1);
                        break;
                    default:
                        vassert(false, $"Unrecognized base-entry length: {base_entry_size}");
                        break;
                }

                return true;
            }
            else
            {
                /* Pin it, so we can try to read future file formats */
                if (base_entry_size > SIZEOF_directory_entry)
                {
                    base_entry_size = SIZEOF_directory_entry;
                }

                /* We have to loop.. */
                for (short directory_index = 0; directory_index < header.wad_count; ++directory_index)
                {
                    /* We use a hint, that the index is the real index, to help make this have */
                    /* a "hit" on the first try */
                    short test_index = (short) ((index + directory_index) % header.wad_count);

                    /* Calculate the offset */
                    offset = calculate_directory_offset(header, test_index);

                    /* Read it.. */
                    var buffer = new byte[Math.Max(SIZEOF_old_directory_entry, SIZEOF_directory_entry)];
                    if (!read_from_file(OFile, offset, buffer, base_entry_size))
                        return false;

                    switch (base_entry_size)
                    {
                        case SIZEOF_old_directory_entry:
                            unpack_old_directory_entry(new StreamPointer(buffer), entry, 1);
                            break;
                        case SIZEOF_directory_entry:
                            unpack_directory_entry(new StreamPointer(buffer), entry, 1);
                            break;
                        default:
                            vassert(false, $"Unrecognized base-entry length: {base_entry_size}");
                            break;
                    }

                    if (entry.index == index)
                    {
                        return true; /* Got it */
                    }
                }
            }

            /* Not found */
            return false;
        }

        /* Internal function.. */
        private static bool read_indexed_wad_from_file_into_buffer(OpenedFile OFile, wad_header header, short index,
            byte[] buffer, ref int length) /* Length of maximum buffer on entry, actual length on return */
        {
            var entry = new directory_entry();
            bool success = false;

            /* Read the directory entry first */
            if (read_indexed_directory_data(OFile, header, index, entry))
            {
                /* Some sanity checks */
                assert(length <= entry.length);
                assert(buffer != null);

                /* Set the length */
                length = entry.length;

                /* Read into it. */
                if (entry.length > 0)
                {
                    success = read_from_file(OFile, entry.offset_to_start, buffer, entry.length);

                    /* Veracity Check */
                    /* ! an error, it has a length non-zero and calculated != actual */
                    assert(entry.length == calculate_raw_wad_length(header, buffer));
                }
            }

            return success;
        }

        /* This *MUST* be a base wad.. */
        private static wad_data convert_wad_from_raw(wad_header header, byte[] data, int wad_start_offset, int raw_length)
        {
            wad_data wad;

            /* In case we are somewhere else, like, for example, in a net transferred level.. */
            int raw_wad = wad_start_offset;

            wad = new wad_data();
            {
                short tag_count;

                /* Clear it */

                /* If the wad is of non-zero length... */
                if (raw_length != 0)
                {
                    /* Count the tags */
                    tag_count = count_raw_tags(data, raw_wad);

                    /* Allocate the tags.. */
                    wad.tag_count = tag_count;
                    wad.tag_data = new tag_data[tag_count];
                    {
                        short index;
                        short entry_header_size;

                        /* Clear it */

                        entry_header_size = get_entry_header_length(header);
                        var wad_entry_header = new entry_header();
                        int raw_wad_entry_header = raw_wad;
                        // Will work OK for Marathon 1
                        unpack_entry_header(new StreamPointer(data, raw_wad_entry_header), wad_entry_header, 1);

                        /* Note that this is a read only wad.. */
                        wad.read_only_data = data;

                        for (index = 0; index < tag_count; ++index)
                        {
                            assert(header.version < WADFILE_SUPPORTS_OVERLAYS || wad_entry_header.offset == 0);
                            wad.tag_data[index] = new tag_data();
                            wad.tag_data[index].tag = wad_entry_header.tag;
                            wad.tag_data[index].length = wad_entry_header.length;
                            wad.tag_data[index].offset = 0;
                            wad.tag_data[index].data = new byte[wad.tag_data[index].length];
                            Array.Copy(data, raw_wad_entry_header + entry_header_size, wad.tag_data[index].data, 0, wad.tag_data[index].length);

                            raw_wad_entry_header = raw_wad + wad_entry_header.next_offset;
                            // Will work OK for Marathon 1
                            unpack_entry_header(new StreamPointer(data, raw_wad_entry_header), wad_entry_header, 1);
                        }
                    }
                }
            }

            return wad;
        }

        /* This *MUST* be a base wad.. */
        private static wad_data convert_wad_from_raw_modifiable(wad_header header, byte[] raw_wad, int raw_length)
        {
            wad_data wad;

            wad = new wad_data();
            {
                short tag_count;

                /* Clear it */

                /* If the wad is of non-zero length... */
                if (raw_length != 0)
                {
                    /* Count the tags */
                    tag_count = count_raw_tags(raw_wad, 0);

                    /* Allocate the tags.. */
                    wad.tag_count = tag_count;
                    wad.tag_data = new tag_data[tag_count];
                    {
                        short index;
                        short entry_header_size;

                        /* Clear it */

                        entry_header_size = get_entry_header_length(header);
                        var wad_entry_header = new entry_header();
                        int raw_wad_entry_header = 0;
                        // Will work OK for Marathon 1
                        unpack_entry_header(new StreamPointer(raw_wad, raw_wad_entry_header), wad_entry_header, 1);

                        for (index = 0; index < tag_count; ++index)
                        {
                            wad.tag_data[index] = new tag_data();
                            wad.tag_data[index].tag = wad_entry_header.tag;
                            wad.tag_data[index].length = wad_entry_header.length;
                            wad.tag_data[index].data = new byte[wad.tag_data[index].length];
                            wad.tag_data[index].offset = 0;

                            /* This MUST be a base! */
                            assert(header.version < WADFILE_SUPPORTS_OVERLAYS || wad_entry_header.offset == 0);

                            /* Copy the data.. */
                            Array.Copy(raw_wad, raw_wad_entry_header + entry_header_size, wad.tag_data[index].data, 0, wad.tag_data[index].length);
                            raw_wad_entry_header = wad_entry_header.next_offset;
                            // Will work OK for Marathon 1
                            unpack_entry_header(new StreamPointer(raw_wad, raw_wad_entry_header), wad_entry_header, 1);
                        }
                    }
                }
            }

            return wad;
        }

        // Will work OK for Marathon 1
        private static short count_raw_tags(byte[] raw_wad, int start)
        {
            int tag_count = 0;

            var header = new entry_header();
            unpack_entry_header(new StreamPointer(raw_wad, start), header, 1);
            while (true)
            {
                tag_count++;
                uint next_offset = (uint) header.next_offset;
                if (next_offset == 0)
                    break;
                unpack_entry_header(new StreamPointer(raw_wad, start + (int) next_offset), header, 1);
            }

            return (short) tag_count;
        }

        // Will work OK for Marathon 1
        private static int calculate_raw_wad_length(wad_header file_header, byte[] wad)
        {
            int entry_header_size = get_entry_header_length(file_header);
            int length = 0;

            var header = new entry_header();
            unpack_entry_header(new StreamPointer(wad), header, 1);
            while (true)
            {
                length += header.length + entry_header_size;
                uint next_offset = (uint) header.next_offset;
                if (next_offset == 0)
                    break;
                unpack_entry_header(new StreamPointer(wad, (int) next_offset), header, 1);
            }

            return length;
        }

        private static bool write_to_file(OpenedFile OFile, int offset, byte[] data, int length)
        {
            if (!OFile.SetPosition(offset)) return false;
            return OFile.Write(length, data);
        }

        private static bool read_from_file(OpenedFile OFile, int offset, byte[] data, int length)
        {
            if (!OFile.SetPosition(offset)) return false;
            return OFile.Read(length, data);
        }

        private static void obj_clear(wad_header header)
        {
            header.version = 0;
            header.data_version = 0;
            Array.Clear(header.file_name, 0, header.file_name.Length);
            header.checksum = 0;
            header.directory_offset = 0;
            header.wad_count = 0;
            header.application_specific_directory_data_size = 0;
            header.entry_header_size = 0;
            header.directory_entry_base_size = 0;
            header.parent_checksum = 0;
            Array.Clear(header.unused, 0, header.unused.Length);
        }

        private static void unpack_wad_header(StreamPointer S, wad_header ObjPtr, int Count)
        {
            int Stream = S.Position;
            for (int k = 0; k < Count; k++)
            {
                StreamToValue(S, out ObjPtr.version);
                StreamToValue(S, out ObjPtr.data_version);
                StreamToBytes(S, ObjPtr.file_name, MAXIMUM_WADFILE_NAME_LENGTH);
                StreamToValue(S, out ObjPtr.checksum);
                StreamToValue(S, out ObjPtr.directory_offset);
                StreamToValue(S, out ObjPtr.wad_count);
                StreamToValue(S, out ObjPtr.application_specific_directory_data_size);
                StreamToValue(S, out ObjPtr.entry_header_size);
                StreamToValue(S, out ObjPtr.directory_entry_base_size);
                StreamToValue(S, out ObjPtr.parent_checksum);
                S.Skip(2 * 20);
            }

            assert((S.Position - Stream) == Count * SIZEOF_wad_header);
        }

        private static void pack_wad_header(StreamPointer S, wad_header ObjPtr, int Count)
        {
            int Stream = S.Position;
            for (int k = 0; k < Count; k++)
            {
                ValueToStream(S, ObjPtr.version);
                ValueToStream(S, ObjPtr.data_version);
                BytesToStream(S, ObjPtr.file_name, MAXIMUM_WADFILE_NAME_LENGTH);
                ValueToStream(S, ObjPtr.checksum);
                ValueToStream(S, ObjPtr.directory_offset);
                ValueToStream(S, ObjPtr.wad_count);
                ValueToStream(S, ObjPtr.application_specific_directory_data_size);
                ValueToStream(S, ObjPtr.entry_header_size);
                ValueToStream(S, ObjPtr.directory_entry_base_size);
                ValueToStream(S, ObjPtr.parent_checksum);
                S.Skip(2 * 20);
            }

            assert((S.Position - Stream) == Count * SIZEOF_wad_header);
        }

        // Aleph One unpacks old entries through a cast to the newer struct; the shared fields are identical
        private static void unpack_old_directory_entry(StreamPointer S, directory_entry ObjPtr, int Count)
        {
            int Stream = S.Position;
            for (int k = 0; k < Count; k++)
            {
                StreamToValue(S, out ObjPtr.offset_to_start);
                StreamToValue(S, out ObjPtr.length);
            }

            assert((S.Position - Stream) == Count * SIZEOF_old_directory_entry);
        }

        private static void pack_old_directory_entry(StreamPointer S, old_directory_entry ObjPtr, int Count)
        {
            int Stream = S.Position;
            for (int k = 0; k < Count; k++)
            {
                ValueToStream(S, ObjPtr.offset_to_start);
                ValueToStream(S, ObjPtr.length);
            }

            assert((S.Position - Stream) == Count * SIZEOF_old_directory_entry);
        }

        private static void unpack_directory_entry(StreamPointer S, directory_entry ObjPtr, int Count)
        {
            int Stream = S.Position;
            for (int k = 0; k < Count; k++)
            {
                StreamToValue(S, out ObjPtr.offset_to_start);
                StreamToValue(S, out ObjPtr.length);
                StreamToValue(S, out ObjPtr.index);
            }

            assert((S.Position - Stream) == Count * SIZEOF_directory_entry);
        }

        private static void pack_directory_entry(StreamPointer S, directory_entry ObjPtr, int Count)
        {
            int Stream = S.Position;
            for (int k = 0; k < Count; k++)
            {
                ValueToStream(S, ObjPtr.offset_to_start);
                ValueToStream(S, ObjPtr.length);
                ValueToStream(S, ObjPtr.index);
            }

            assert((S.Position - Stream) == Count * SIZEOF_directory_entry);
        }

        // Aleph One packs old entry headers through a cast from the newer struct; the shared fields are identical
        private static void pack_old_entry_header(StreamPointer S, entry_header ObjPtr, int Count)
        {
            int Stream = S.Position;
            for (int k = 0; k < Count; k++)
            {
                ValueToStream(S, ObjPtr.tag);
                ValueToStream(S, ObjPtr.next_offset);
                ValueToStream(S, ObjPtr.length);
            }

            assert((S.Position - Stream) == Count * SIZEOF_old_entry_header);
        }

        private static void unpack_entry_header(StreamPointer S, entry_header ObjPtr, int Count)
        {
            int Stream = S.Position;
            for (int k = 0; k < Count; k++)
            {
                StreamToValue(S, out ObjPtr.tag);
                StreamToValue(S, out ObjPtr.next_offset);
                StreamToValue(S, out ObjPtr.length);
                StreamToValue(S, out ObjPtr.offset);
            }

            assert((S.Position - Stream) == Count * SIZEOF_entry_header);
        }

        private static void pack_entry_header(StreamPointer S, entry_header ObjPtr, int Count)
        {
            int Stream = S.Position;
            for (int k = 0; k < Count; k++)
            {
                ValueToStream(S, ObjPtr.tag);
                ValueToStream(S, ObjPtr.next_offset);
                ValueToStream(S, ObjPtr.length);
                ValueToStream(S, ObjPtr.offset);
            }

            assert((S.Position - Stream) == Count * SIZEOF_entry_header);
        }
    }
}
