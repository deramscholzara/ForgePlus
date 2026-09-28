// Port of Aleph One: Source_Files/Misc/interface.h (shapes)
//
// Not ported: game states, dialogs, networking, key setups, and the rest of interface.h.
using static AlephOne.shape_descriptors;

namespace AlephOne
{
    /* ---------- structures */

    // _X_MIRRORED_BIT, _Y_MIRRORED_BIT, _KEYPOINT_OBSCURED_BIT: in collection_definition

    public class shape_information_data
    {
        public ushort flags; /* [x-mirror.1] [y-mirror.1] [keypoint_obscured.1] [unused.13] */

        public int minimum_light_intensity; /* in [0,FIXED_ONE] */

        // Overlays low_level_shape_definition's bitmap_index, origin_x, origin_y, key_x, key_y
        public short[] unused = new short[5];

        public short world_left, world_right, world_top, world_bottom;
        public short world_x0, world_y0;
    }

    public class shape_animation_data // Also used in high_level_shape_definition
    {
        public short number_of_views; /* must be 1, 2, 5 or 8 */

        public short frames_per_view, ticks_per_frame;
        public short key_frame;

        public short transfer_mode;
        public short transfer_mode_period; /* in ticks */

        public short first_frame_sound, key_frame_sound, last_frame_sound;

        public short pixels_to_world;

        public short loop_frame;

        public short[] unused = new short[14];

        /* N*frames_per_view indexes of low-level shapes follow, where
           N = 1 if number_of_views = _unanimated/_animated1,
           N = 4 if number_of_views = _animated3to4/_animated4,
           N = 5 if number_of_views = _animated3to5/_animated5,
           N = 8 if number_of_views = _animated2to8/_animated5to8/_animated8 */
        public short[] low_level_shape_indexes = new short[0];
    }

    public static class @interface
    {
        /* animation types */
        public const short _animated1 = 1;
        public const short _animated2to8 = 2; /* ?? */
        public const short _animated3to4 = 3;
        public const short _animated4 = 4;
        public const short _animated5to8 = 5;
        public const short _animated8 = 8;
        public const short _animated3to5 = 9;
        public const short _unanimated = 10;
        public const short _animated5 = 11;

        /* shading tables */
        public const short _darkening_table = 0;

        /* shape types (this is for the editor) */
        public const short _wall_shape = 0; /* things designated as walls */
        public const short _floor_or_ceiling_shape = 1; /* walls in raw format */
        public const short _object_shape = 2; /* things designated as objects */
        public const short _other_shape = 3; /* anything not falling into the above categories (guns, interface elements, etc) */

        public const int TOTAL_SHAPE_COLLECTIONS = 128;

        /* ---------- prototypes/SHAPES.C */

        // #define get_shape_bitmap_and_shading_table(shape, bitmap, shading_table, shading_mode)
        public static void get_shape_bitmap_and_shading_table(ushort shape, out bitmap_definition bitmap, short shading_mode)
        {
            shapes.extended_get_shape_bitmap_and_shading_table((short) GET_DESCRIPTOR_COLLECTION(shape),
                (short) GET_DESCRIPTOR_SHAPE(shape), out bitmap, shading_mode);
        }

        // #define get_shape_information(shape)
        public static shape_information_data get_shape_information(ushort shape)
        {
            return shapes.extended_get_shape_information((short) GET_DESCRIPTOR_COLLECTION(shape), (short) GET_DESCRIPTOR_SHAPE(shape));
        }

        // #define mark_collection_for_loading(c) / mark_collection_for_unloading(c)
        public static void mark_collection_for_loading(short c)
        {
            shapes.mark_collection(c, true);
        }

        public static void mark_collection_for_unloading(short c)
        {
            shapes.mark_collection(c, false);
        }
    }
}
