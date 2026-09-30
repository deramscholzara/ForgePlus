// Port of Aleph One: Source_Files/Files/resource_manager.h, resource_manager.cpp
//
// The list of open resource files is a List, and cur_res_file_t the index of the current one in it.
//
// Not ported: the external resources file (Marathon 1's terminals; set_external_resources_file,
// close_external_resources, initialize_resources) and logging.
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csalerts;
using static AlephOne.SDL_rwops;

namespace AlephOne
{
    // Structure for open resource file
    internal class res_file_t
    {
        public res_file_t(SDL_RWops file) { f = file; }

        public SDL_RWops f; // Opened resource file

        // Map of all resource types found in file: each type's map of resource ID to offset to resource data
        public SortedDictionary<uint, SortedDictionary<int, uint>> types = new SortedDictionary<uint, SortedDictionary<int, uint>>();

        /*
         *  Read and parse resource map from file
         */
        public bool read_map()
        {
            SDL_RWseek(f, 0, SEEK_END);
            uint file_size = (uint) SDL_RWtell(f);
            SDL_RWseek(f, 0, SEEK_SET);
            uint fork_start = 0;

            if (file_size < 16)
            {
                return false;
            }

            // Determine file type (AppleSingle and MacBinary II files are handled transparently)
            int offset, data_length, rsrc_length;
            if (resource_manager.is_applesingle(f, true, out offset, out rsrc_length))
            {
                fork_start = (uint) offset;
                file_size = (uint) (offset + rsrc_length);
            }
            else if (resource_manager.is_macbinary(f, out data_length, out rsrc_length))
            {
                fork_start = (uint) (128 + ((data_length + 0x7f) & ~0x7f));
                file_size = (uint) (fork_start + rsrc_length);
            }

            // Read resource header
            SDL_RWseek(f, fork_start, SEEK_SET);
            uint data_offset = SDL_ReadBE32(f) + fork_start;
            uint map_offset = SDL_ReadBE32(f) + fork_start;
            uint data_size = SDL_ReadBE32(f);
            uint map_size = SDL_ReadBE32(f);

            // Verify integrity of resource header
            if (data_offset >= file_size || map_offset >= file_size ||
                data_offset + data_size > file_size || map_offset + map_size > file_size)
            {
                return false;
            }

            // Read map header
            SDL_RWseek(f, map_offset + 24, SEEK_SET);
            uint type_list_offset = map_offset + SDL_ReadBE16(f);
            //uint32 name_list_offset = map_offset + SDL_ReadBE16(f);

            // Verify integrity of map header
            if (type_list_offset >= file_size)
            {
                return false;
            }

            // Read resource type list
            SDL_RWseek(f, type_list_offset, SEEK_SET);
            int num_types = (short) SDL_ReadBE16(f) + 1;
            for (int i = 0; i < num_types; i++)
            {
                // Read type list item
                uint type = SDL_ReadBE32(f);
                int num_refs = (short) SDL_ReadBE16(f) + 1;
                uint ref_list_offset = type_list_offset + SDL_ReadBE16(f);

                // Verify integrity of item
                if (ref_list_offset >= file_size)
                {
                    return false;
                }

                // Create ID map for this type
                if (!types.TryGetValue(type, out var id_map))
                {
                    id_map = new SortedDictionary<int, uint>();
                    types[type] = id_map;
                }

                // Read reference list
                uint cur = (uint) SDL_RWtell(f);
                SDL_RWseek(f, ref_list_offset, SEEK_SET);
                for (int j = 0; j < num_refs; j++)
                {
                    // Read list item
                    int id = (short) SDL_ReadBE16(f);
                    SDL_RWseek(f, 2, SEEK_CUR);
                    uint rsrc_data_offset = data_offset + (SDL_ReadBE32(f) & 0x00ffffff);

                    // Verify integrify of item
                    if (rsrc_data_offset >= file_size)
                    {
                        return false;
                    }

                    // Add ID to map
                    id_map[id] = rsrc_data_offset;

                    SDL_RWseek(f, 4, SEEK_CUR);
                }
                SDL_RWseek(f, cur, SEEK_SET);
            }

            return true;
        }

        /*
         *  Count number of resources of given type
         */
        public int count_resources(uint type)
        {
            if (!types.TryGetValue(type, out var i))
                return 0;
            else
                return i.Count;
        }

        /*
         *  Get resource from file
         */
        public bool get_resource(uint type, int id, LoadedResource rsrc)
        {
            rsrc.Unload();

            // Find resource in map
            if (types.TryGetValue(type, out var i))
            {
                if (i.TryGetValue(id, out var j))
                {
                    // Found, read data size
                    SDL_RWseek(f, j, SEEK_SET);
                    uint size = SDL_ReadBE32(f);

                    // Allocate memory and read data
                    var p = new byte[size];
                    SDL_RWread(f, p, 0, 1, (int) size);
                    rsrc.p = p;
                    rsrc.size = (int) size;

                    return true;
                }
            }
            return false;
        }

        /*
         *  Check if resource is present
         */
        public bool has_resource(uint type, int id)
        {
            if (types.TryGetValue(type, out var i))
            {
                if (i.ContainsKey(id))
                    return true;
            }
            return false;
        }
    }

    [NoAutoStaticsCleanup]
    public static class resource_manager
    {
        // List of open resource files
        private static readonly List<res_file_t> res_file_list = new List<res_file_t>();
        private static int cur_res_file_t = -1;

        /*
         *  Find file in list of opened files
         */
        private static int find_res_file_t(SDL_RWops f)
        {
            for (int i = 0; i < res_file_list.Count; i++)
            {
                res_file_t r = res_file_list[i];
                if (r.f == f)
                    return i;
            }
            return res_file_list.Count;
        }

        /*
         *  Check for AppleSingle resource/data fork
         */
        public static bool is_applesingle(SDL_RWops f, bool rsrc_fork, out int offset, out int length)
        {
            offset = 0;
            length = 0;

            // Check header
            SDL_RWseek(f, 0, SEEK_SET);
            uint id = SDL_ReadBE32(f);
            uint version = SDL_ReadBE32(f);
            if (id != 0x00051600 || version != 0x00020000)
                return false;

            // Find fork
            uint req_id = rsrc_fork ? 2u : 1u;
            SDL_RWseek(f, 0x18, SEEK_SET);
            int num_entries = SDL_ReadBE16(f);
            while (num_entries-- != 0)
            {
                uint entry_id = SDL_ReadBE32(f);
                int ofs = (int) SDL_ReadBE32(f);
                int len = (int) SDL_ReadBE32(f);
                if (entry_id == req_id)
                {
                    offset = ofs;
                    length = len;
                    return true;
                }
            }
            return false;
        }

        /*
         *  Check for MacBinary II resource/data fork
         */
        public static bool is_macbinary(SDL_RWops f, out int data_length, out int rsrc_length)
        {
            data_length = 0;
            rsrc_length = 0;

            // This recognizes up to macbinary III (0x81)
            SDL_RWseek(f, 0, SEEK_SET);
            var header = new byte[128];
            if (SDL_RWread(f, header, 0, 1, 128) != 128)
            {
                return false;
            }

            if (header[0] != 0 || header[1] > 63 || header[74] != 0 || header[123] > 0x81)
                return false;

            // Check CRC
            ushort crc = 0;
            for (int i = 0; i < 124; i++)
            {
                ushort data = (ushort) (header[i] << 8);
                for (int j = 0; j < 8; j++)
                {
                    if (((data ^ crc) & 0x8000) != 0)
                        crc = (ushort) ((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                    data <<= 1;
                }
            }
            if (crc != ((header[124] << 8) | header[125]))
                return false;

            // CRC valid, extract fork sizes
            data_length = (header[83] << 24) | (header[84] << 16) | (header[85] << 8) | header[86];
            rsrc_length = (header[87] << 24) | (header[88] << 16) | (header[89] << 8) | header[90];
            return true;
        }

        /*
         *  Open resource file, set current file to the newly opened one
         */
        public static SDL_RWops open_res_file_from_rwops(SDL_RWops f)
        {
            if (f != null)
            {
                // Successful, create res_file_t object and read resource map
                var r = new res_file_t(f);
                if (r.read_map())
                {
                    // Successful, add file to list of open files
                    res_file_list.Add(r);
                    cur_res_file_t = res_file_list.Count - 1;
                }
                else
                {
                    // Error reading resource map
                    SDL_RWclose(f);
                    return null;
                }
            }

            return f;
        }

        private static SDL_RWops open_res_file_from_path(string inPath)
        {
            return open_res_file_from_rwops(SDL_RWFromFile(inPath, "rb"));
        }

        public static SDL_RWops open_res_file(FileSpecifier file)
        {
            string rsrc_file_name = file.GetPath();
            string resources_file_name = rsrc_file_name;
            string darwin_rsrc_file_name = rsrc_file_name;
            rsrc_file_name += ".rsrc";
            resources_file_name += ".resources";
            darwin_rsrc_file_name += "/..namedfork/rsrc";

            SDL_RWops f = null;

            // Open file, try <name>.rsrc first, then <name>.resources, then <name>/rsrc then <name>
            if (f == null)
                f = open_res_file_from_path(rsrc_file_name);
            if (f == null)
                f = open_res_file_from_path(resources_file_name);
            if (f == null)
                f = open_res_file_from_path(file.GetPath());
            if (f == null)
                f = open_res_file_from_path(darwin_rsrc_file_name);

            return f;
        }

        /*
         *  Close resource file
         */
        public static void close_res_file(SDL_RWops file)
        {
            if (file == null)
                return;

            // Find file in list
            int i = find_res_file_t(file);
            if (i != res_file_list.Count)
            {
                // Remove it from the list, close the file and delete the res_file_t
                res_file_t r = res_file_list[i];
                SDL_RWclose(r.f);
                res_file_list.RemoveAt(i);

                cur_res_file_t = res_file_list.Count - 1;
            }
        }

        /*
         *  Return file ID of current resource file
         */
        public static SDL_RWops cur_res_file()
        {
            res_file_t r = cur_res_file_t >= 0 ? res_file_list[cur_res_file_t] : null;
            assert(r != null);
            return r.f;
        }

        /*
         *  Set current resource file
         */
        public static void use_res_file(SDL_RWops file)
        {
            int i = find_res_file_t(file);
            assert(i != res_file_list.Count);
            cur_res_file_t = i;
        }

        /*
         *  Count number of resources of given type
         */
        public static int count_1_resources(uint type)
        {
            return res_file_list[cur_res_file_t].count_resources(type);
        }

        public static int count_resources(uint type)
        {
            if (res_file_list.Count == 0)
                return 0;

            int count = 0;
            for (int i = cur_res_file_t; i >= 0; i--)
            {
                count += res_file_list[i].count_resources(type);
            }
            return count;
        }

        /*
         *  Get resource from file
         */
        public static bool get_1_resource(uint type, int id, LoadedResource rsrc)
        {
            return res_file_list[cur_res_file_t].get_resource(type, id, rsrc);
        }

        public static bool get_resource(uint type, int id, LoadedResource rsrc)
        {
            if (res_file_list.Count == 0)
                return false;

            for (int i = cur_res_file_t; i >= 0; i--)
            {
                bool found = res_file_list[i].get_resource(type, id, rsrc);
                if (found)
                    return true;
            }
            return false;
        }

        /*
         *  Check if resource is present
         */
        public static bool has_1_resource(uint type, int id)
        {
            return res_file_list[cur_res_file_t].has_resource(type, id);
        }

        public static bool has_resource(uint type, int id)
        {
            if (res_file_list.Count == 0)
                return false;

            for (int i = cur_res_file_t; i >= 0; i--)
            {
                if (res_file_list[i].has_resource(type, id))
                    return true;
            }
            return false;
        }
    }
}
