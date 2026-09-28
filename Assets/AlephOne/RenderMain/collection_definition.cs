// Port of Aleph One: Source_Files/RenderMain/collection_definition.h
//
// Aleph One keeps each high-level shape and bitmap in a std::vector<uint8>: the struct, then its low-level
// shape indexes, or its row pointers and pixels. Here it is the struct holding those as arrays, or null
// for an empty vector.
namespace AlephOne
{
    /* ---------- collection definition structure */

    public class collection_definition
    {
        /* 2 added pixels_to_world to collection_definition structure */
        /* 3 added size to collection_definition structure */
        public const short COLLECTION_VERSION = 3;

        /* at the beginning of the clut, used by the extractor for various opaque reasons */
        public const int NUMBER_OF_PRIVATE_COLORS = 3;

        /* collection types */
        public const short _unused_collection = 0; /* raw */
        public const short _wall_collection = 1; /* raw */
        public const short _object_collection = 2; /* rle */
        public const short _interface_collection = 3; /* raw */
        public const short _scenery_collection = 4; /* rle */

        public const int SIZEOF_collection_definition = 544;

        /* ---------- high level shape definition */

        public const int HIGH_LEVEL_SHAPE_NAME_LENGTH = 32;

        public const int SIZEOF_high_level_shape_definition = 90;

        /* --------- low-level shape definition */

        public const int _X_MIRRORED_BIT = 0x8000;
        public const int _Y_MIRRORED_BIT = 0x4000;
        public const int _KEYPOINT_OBSCURED_BIT = 0x2000;

        public const int SIZEOF_low_level_shape_definition = 36;

        /* ---------- colors */

        public const int SELF_LUMINESCENT_COLOR_FLAG = 0x80;

        public const int SIZEOF_rgb_color_value = 8;

        // struct collection_definition
        public short version;

        public short type; /* used for get_shape_descriptors() */
        public ushort flags; /* [unused.16] */

        public short color_count, clut_count;
        public int color_table_offset; /* an array of clut_count arrays of color_count ColorSpec structures */

        public short high_level_shape_count;
        public int high_level_shape_offset_table_offset;

        public short low_level_shape_count;
        public int low_level_shape_offset_table_offset;

        public short bitmap_count;
        public int bitmap_offset_table_offset;

        public short pixels_to_world; /* used to shift pixel values into world coordinates */

        public int size; /* used to assert offsets */

        public short[] unused = new short[253];

        // std::vector<rgb_color_value> color_tables;
        public rgb_color_value[] color_tables = new rgb_color_value[0];
        // std::vector<std::vector<uint8> > high_level_shapes;
        public high_level_shape_definition[] high_level_shapes = new high_level_shape_definition[0];
        // std::vector<low_level_shape_definition> low_level_shapes;
        public low_level_shape_definition[] low_level_shapes = new low_level_shape_definition[0];
        // std::vector<std::vector<uint8> > bitmaps;
        public bitmap_definition[] bitmaps = new bitmap_definition[0];
    }

    /* ---------- high level shape definition */

    public class high_level_shape_definition // Starting with number_of_views, this is a shape_animation_data structure
    {
        public short type; /* ==0 */
        public ushort flags; /* [unused.16] */

        public byte[] name = new byte[collection_definition.HIGH_LEVEL_SHAPE_NAME_LENGTH + 2];

        public short number_of_views;

        public short frames_per_view, ticks_per_frame;
        public short key_frame;

        public short transfer_mode;
        public short transfer_mode_period; /* in ticks */

        public short first_frame_sound, key_frame_sound, last_frame_sound;

        public short pixels_to_world;

        public short loop_frame;

        public short[] unused = new short[14];

        /* see the interface.h/shape_animation_data for a decription of how many
           low-level indices follow (it's not simply number_of_view * frames_per_view) */
        public short[] low_level_shape_indexes = new short[0]; // int16 low_level_shape_indexes[1];
    }

    /* --------- low-level shape definition */

    public class low_level_shape_definition
    {
        public ushort flags; /* [x-mirror.1] [y-mirror.1] [keypoint_obscured.1] [unused.13] */

        public int minimum_light_intensity; /* in [0,FIXED_ONE] */

        public short bitmap_index;

        /* (x,y) in pixel coordinates of origin */
        public short origin_x, origin_y;

        /* (x,y) in pixel coordinates of key point */
        public short key_x, key_y;

        public short world_left, world_right, world_top, world_bottom;
        public short world_x0, world_y0;

        public short[] unused = new short[4];
    }

    /* ---------- colors */

    public class rgb_color_value
    {
        public byte flags;
        public byte value;

        public ushort red, green, blue;
    }
}
