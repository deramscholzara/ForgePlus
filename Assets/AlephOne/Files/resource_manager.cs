// Port of Aleph One: Source_Files/Files/resource_manager.cpp (fork detection only)
using static AlephOne.SDL_rwops;

namespace AlephOne
{
    public static class resource_manager
    {
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
    }
}
