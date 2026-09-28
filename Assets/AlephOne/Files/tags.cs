// Port of Aleph One: Source_Files/Files/tags.h
//
// Not ported: typecodes and preferences tags.

namespace AlephOne
{
    public static class tags
    {
        public const int MAXIMUM_LEVEL_NAME_SIZE = 64;

        /* Other tags-  */
        public const uint POINT_TAG = 0x504E5453; // 'PNTS'
        public const uint LINE_TAG = 0x4C494E53; // 'LINS'
        public const uint SIDE_TAG = 0x53494453; // 'SIDS'
        public const uint POLYGON_TAG = 0x504F4C59; // 'POLY'
        public const uint LIGHTSOURCE_TAG = 0x4C495445; // 'LITE'
        public const uint ANNOTATION_TAG = 0x4E4F5445; // 'NOTE'
        public const uint OBJECT_TAG = 0x4F424A53; // 'OBJS'
        public const uint GUARDPATH_TAG = 0x708C7468; // 'p\x8cth'
        public const uint MAP_INFO_TAG = 0x4D696E66; // 'Minf'
        public const uint ITEM_PLACEMENT_STRUCTURE_TAG = 0x706C6163; // 'plac'
        public const uint DOOR_EXTRA_DATA_TAG = 0x646F6F72; // 'door'
        public const uint PLATFORM_STATIC_DATA_TAG = 0x706C6174; // 'plat'
        public const uint ENDPOINT_DATA_TAG = 0x45504E54; // 'EPNT'
        public const uint MEDIA_TAG = 0x6D656469; // 'medi'
        public const uint AMBIENT_SOUND_TAG = 0x616D6269; // 'ambi'
        public const uint RANDOM_SOUND_TAG = 0x626F6E6B; // 'bonk'
        public const uint TERMINAL_DATA_TAG = 0x7465726D; // 'term'

        /* Save/Load game tags. */
        public const uint PLAYER_STRUCTURE_TAG = 0x706C7972; // 'plyr'
        public const uint DYNAMIC_STRUCTURE_TAG = 0x64776F6C; // 'dwol'
        public const uint OBJECT_STRUCTURE_TAG = 0x6D6F626A; // 'mobj'
        public const uint DOOR_STRUCTURE_TAG = 0x646F6F72; // 'door'
        public const uint MAP_INDEXES_TAG = 0x69696478; // 'iidx'
        public const uint AUTOMAP_LINES = 0x616C696E; // 'alin'
        public const uint AUTOMAP_POLYGONS = 0x61706F6C; // 'apol'
        public const uint MONSTERS_STRUCTURE_TAG = 0x6D4F6E73; // 'mOns'
        public const uint EFFECTS_STRUCTURE_TAG = 0x66782020; // 'fx  '
        public const uint PROJECTILES_STRUCTURE_TAG = 0x62616E67; // 'bang'
        public const uint PLATFORM_STRUCTURE_TAG = 0x504C4154; // 'PLAT'
        public const uint WEAPON_STATE_TAG = 0x77656170; // 'weap'
        public const uint TERMINAL_STATE_TAG = 0x63696E74; // 'cint'
        public const uint LUA_STATE_TAG = 0x736C7561; // 'slua'

        /* Save metadata tags */
        public const uint SAVE_META_TAG = 0x534D4554; // 'SMET'
        public const uint SAVE_IMG_TAG = 0x53494D47; // 'SIMG'

        /* Physix model tags */
        public const uint MONSTER_PHYSICS_TAG = 0x4D4E7078; // 'MNpx'
        public const uint EFFECTS_PHYSICS_TAG = 0x46587078; // 'FXpx'
        public const uint PROJECTILE_PHYSICS_TAG = 0x50527078; // 'PRpx'
        public const uint PHYSICS_PHYSICS_TAG = 0x50587078; // 'PXpx'
        public const uint WEAPONS_PHYSICS_TAG = 0x57507078; // 'WPpx'

        public const uint M1_MONSTER_PHYSICS_TAG = 0x6D6F6E73; // 'mons'
        public const uint M1_EFFECTS_PHYSICS_TAG = 0x65666665; // 'effe'
        public const uint M1_PROJECTILE_PHYSICS_TAG = 0x70726F6A; // 'proj'
        public const uint M1_PHYSICS_PHYSICS_TAG = 0x70687973; // 'phys'
        public const uint M1_WEAPONS_PHYSICS_TAG = 0x77656170; // 'weap'

        /* Embedded shapes */
        public const uint SHAPE_PATCH_TAG = 0x53685061; // 'ShPa'

        /* Embedded sounds */
        public const uint SOUND_PATCH_TAG = 0x536E5061; // 'SnPa'

        /* Embedded scripts */
        public const uint MMLS_TAG = 0x4D4D4C53; // 'MMLS'
        public const uint LUAS_TAG = 0x4C554153; // 'LUAS'
    }
}
