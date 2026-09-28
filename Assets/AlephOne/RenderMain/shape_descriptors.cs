// Port of Aleph One: Source_Files/RenderMain/shape_descriptors.h
namespace AlephOne
{
    // typedef uint16 shape_descriptor; /* [clut.3] [collection.5] [shape.8] */

    public static class shape_descriptors
    {
        public const int DESCRIPTOR_SHAPE_BITS = 8;
        public const int DESCRIPTOR_COLLECTION_BITS = 5;
        public const int DESCRIPTOR_CLUT_BITS = 3;

        public const int MAXIMUM_COLLECTIONS = (1 << DESCRIPTOR_COLLECTION_BITS);
        public const int MAXIMUM_SHAPES_PER_COLLECTION = (1 << DESCRIPTOR_SHAPE_BITS);
        public const int MAXIMUM_CLUTS_PER_COLLECTION = (1 << DESCRIPTOR_CLUT_BITS);

        /* ---------- collections */

        /* collection numbers */
        public const short _collection_interface = 0;
        public const short _collection_weapons_in_hand = 1;
        public const short _collection_juggernaut = 2;
        public const short _collection_tick = 3;
        public const short _collection_rocket = 4; // LP: also known as "Explosion Effects"
        public const short _collection_hunter = 5;
        public const short _collection_player = 6;
        public const short _collection_items = 7;
        public const short _collection_trooper = 8;
        public const short _collection_fighter = 9;
        public const short _collection_defender = 10;
        public const short _collection_yeti = 11;
        public const short _collection_civilian = 12;
        public const short _collection_civilian_fusion = 13; // LP: formerly _collection_madd
        public const short _collection_enforcer = 14;
        public const short _collection_hummer = 15;
        public const short _collection_compiler = 16;
        public const short _collection_walls1 = 17; // LP: Lh'owon water
        public const short _collection_walls2 = 18; // LP: Lh'owon lava
        public const short _collection_walls3 = 19; // LP: Lh'owon sewage
        public const short _collection_walls4 = 20; // LP: Jjaro
        public const short _collection_walls5 = 21; // LP: Pfhor
        public const short _collection_scenery1 = 22; // LP: Lh'owon water
        public const short _collection_scenery2 = 23; // LP: Lh'owon lava
        public const short _collection_scenery3 = 24; // LP: Lh'owon sewage
        public const short _collection_scenery4 = 25; // pathways -- LP: Jjaro
        public const short _collection_scenery5 = 26; // alien -- LP: Pfhor
        public const short _collection_landscape1 = 27; // day -- LP: Lh'owon day
        public const short _collection_landscape2 = 28; // night -- LP: Lh'owon night
        public const short _collection_landscape3 = 29; // moon -- LP: Lh'owon moon
        public const short _collection_landscape4 = 30; // LP: outer space
        public const short _collection_cyborg = 31;
        public const short NUMBER_OF_COLLECTIONS = 32;

        /* ---------- macros */

        public static ushort GET_DESCRIPTOR_SHAPE(ushort d) { return (ushort) (d & (ushort) (MAXIMUM_SHAPES_PER_COLLECTION - 1)); }
        public static ushort GET_DESCRIPTOR_COLLECTION(ushort d) { return (ushort) ((d >> DESCRIPTOR_SHAPE_BITS) & (ushort) ((1 << (DESCRIPTOR_COLLECTION_BITS + DESCRIPTOR_CLUT_BITS)) - 1)); }
        public static ushort BUILD_DESCRIPTOR(int collection, int shape) { return (ushort) ((collection << DESCRIPTOR_SHAPE_BITS) | shape); }

        public static ushort BUILD_COLLECTION(int collection, int clut) { return (ushort) (collection | (ushort) (clut << DESCRIPTOR_COLLECTION_BITS)); }
        public static ushort GET_COLLECTION_CLUT(int collection) { return (ushort) ((collection >> DESCRIPTOR_COLLECTION_BITS) & (ushort) (MAXIMUM_CLUTS_PER_COLLECTION - 1)); }
        public static ushort GET_COLLECTION(int collection) { return (ushort) (collection & (MAXIMUM_COLLECTIONS - 1)); }
    }
}
