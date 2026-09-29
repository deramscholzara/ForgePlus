// Port of Aleph One: Source_Files/Files/crc.h, crc.cpp
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csalerts;

namespace AlephOne
{
    [NoAutoStaticsCleanup]
    public static class crc
    {
        private const int TABLE_SIZE = 256;
        private const uint CRC32_POLYNOMIAL = 0xEDB88320;
        private const int BUFFER_SIZE = 1024;

        private static uint[] crc_table = null;

        public static uint calculate_crc_for_file(FileSpecifier File)
        {
            uint crc = 0;
            var OFile = new OpenedFile();
            if (File.Open(OFile))
            {
                crc = calculate_crc_for_opened_file(OFile);
                OFile.Close();
            }

            return crc;
        }

        public static uint calculate_crc_for_opened_file(OpenedFile OFile)
        {
            uint crc = 0;

            /* Build the crc table */
            if (build_crc_table())
            {
                var buffer = new byte[BUFFER_SIZE];
                crc = calculate_file_crc(buffer, BUFFER_SIZE, OFile);

                /* free the crc table! */
                free_crc_table();
            }

            return crc;
        }

        /* Calculate the crc for a buffer */
        public static uint calculate_data_crc(byte[] buffer, int length)
        {
            uint crc = 0;

            assert(buffer != null);

            /* Build the crc table */
            if (build_crc_table())
            {
                /* The odd permutions ensure that we get the same crc as for a file */
                crc = 0xFFFFFFFF;
                crc = calculate_buffer_crc(length, crc, buffer, 0);
                crc ^= 0xFFFFFFFF;

                /* free the crc table! */
                free_crc_table();
            }

            return crc;
        }

        private static bool build_crc_table()
        {
            assert(crc_table == null);
            crc_table = new uint[TABLE_SIZE];

            /* Build the table */
            for (int index = 0; index < TABLE_SIZE; ++index)
            {
                uint crc = (uint) index;
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0) crc = (crc >> 1) ^ CRC32_POLYNOMIAL;
                    else crc >>= 1;
                }

                crc_table[index] = crc;
            }

            return true;
        }

        private static void free_crc_table()
        {
            assert(crc_table != null);
            crc_table = null;
        }

        private static uint calculate_buffer_crc(int count, uint crc, byte[] buffer, int start)
        {
            int p = start;

            while (count-- > 0)
            {
                uint a = (crc >> 8) & 0x00FFFFFF;
                uint b = crc_table[((int) crc ^ buffer[p++]) & 0xff];
                crc = a ^ b;
            }

            return crc;
        }

        private static uint calculate_file_crc(byte[] buffer, short buffer_size, OpenedFile OFile)
        {
            uint crc;
            int count;
            int file_length, initial_position;

            /* Save and restore the initial file position */
            if (!OFile.GetPosition(out initial_position))
                return 0;

            /* Get the file_length */
            if (!OFile.GetLength(out file_length))
                return 0;

            /* Set to the start of the file */
            if (!OFile.SetPosition(0))
                return 0;

            crc = 0xFFFFFFFF;
            while (file_length > 0)
            {
                if (file_length > buffer_size)
                {
                    count = buffer_size;
                }
                else
                {
                    count = file_length;
                }

                if (!OFile.Read(count, buffer))
                    return 0;

                crc = calculate_buffer_crc(count, crc, buffer, 0);
                file_length -= count;
            }

            /* Restore the file position */
            OFile.SetPosition(initial_position);

            /* Invert it */
            crc ^= 0xFFFFFFFF;

            return crc;
        }

        // calculate_data_crc_ccitt() (used for networking) is not ported.
    }
}
