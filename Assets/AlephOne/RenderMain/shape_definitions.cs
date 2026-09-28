// Port of Aleph One: Source_Files/RenderMain/shape_definitions.h
namespace AlephOne
{
    /* ---------- structures */

    public class collection_header /* 32 bytes on disk */
    {
        public short status;
        public ushort flags;

        public int offset, length;
        public int offset16, length16;

        // LP: handles to pointers
        public collection_definition collection;
        // Not ported: the shading tables
        public byte[] shading_tables = new byte[0];
    }

    public static class shape_definitions
    {
        public const int SIZEOF_collection_header = 32;

        /* ---------- globals */

        // static struct collection_header collection_headers[MAXIMUM_COLLECTIONS];
        internal static readonly collection_header[] collection_headers = new_collection_headers();

        private static collection_header[] new_collection_headers()
        {
            var headers = new collection_header[shape_descriptors.MAXIMUM_COLLECTIONS];
            for (int i = 0; i < headers.Length; i++)
            {
                headers[i] = new collection_header();
            }

            return headers;
        }
    }
}
