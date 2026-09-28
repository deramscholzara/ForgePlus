// Port of Aleph One: Source_Files/GameWorld/map.h, and the map-data parts of map.cpp
//
// Not ported (runtime only): object_data, dynamic_data and game_data (saved games), map objects and
// their polygon lists, animation, sounds, rendering, collision (keep_line_segment_out_of_walls,
// find_line_crossed_leaving_polygon, line_is_obstructed, ...), the automap, and the MARATHON.C,
// PLACEMENT.C, DEVICES.C and GAME_WAD.C prototypes (those an editor needs are in their own files).
using System;
using static AlephOne.csalerts;
using static AlephOne.csmacros;
using static AlephOne.cstypes;
using static AlephOne.FilmProfileGlobals;
using static AlephOne.shape_descriptors;
using static AlephOne.world;

namespace AlephOne
{
    /* ---------- damage */

    public class damage_definition
    {
        public short type, flags;

        public short @base, random;
        public int scale; // _fixed

        public damage_definition Clone() { return (damage_definition) MemberwiseClone(); }
    }

    /* ---------- saved objects (initial map locations, etc.) */

    public class map_object /* 16 bytes */
    {
        public short type; /* _saved_monster, _saved_object, _saved_item, ... */
        public short index;
        public short facing;
        public short polygon_index;
        public world_point3d location; // .z is a delta

        public ushort flags;

        public map_object Clone() { return (map_object) MemberwiseClone(); }
    }

    // Due to misalignments, these have different sizes
    // typedef world_point2d saved_map_pt; typedef struct line_data saved_line; typedef struct side_data saved_side; ...

    /* ---------- map loading/new game structures */

    public class entry_point
    {
        public short level_number;
        public byte[] level_name = new byte[64 + 2];
    }

    public class player_start_data
    {
        public short team;
        public short identifier; /* [weapon_switch_flag.1] [UNUSED.1] [identifier.14] */
        public short color;
        public byte[] name = new byte[map.MAXIMUM_PLAYER_START_NAME_LENGTH + 1]; /* PLAYER_NAME_LENGTH+1 */
    }

    public class directory_data
    {
        public short mission_flags;
        public short environment_flags;
        public int entry_point_flags;
        public byte[] level_name = new byte[map.LEVEL_NAME_LENGTH];
    }

    /* ---------- map annotations */

    public class map_annotation
    {
        public short type; /* turns into color, font, size, style, etc... */

        public world_point2d location; /* where to draw this (lower left) */
        public short polygon_index; /* only displayed if this polygon is in the automap */

        public byte[] text = new byte[map.MAXIMUM_ANNOTATION_TEXT_LENGTH];

        public map_annotation Clone()
        {
            var copy = (map_annotation) MemberwiseClone();
            copy.text = (byte[]) text.Clone();
            return copy;
        }
    }

    /* ---------- ambient sound images */

    // non-directional ambient component
    public class ambient_sound_image_data // 16 bytes
    {
        public ushort flags;

        public short sound_index;
        public short volume;

        public short[] unused = new short[5];

        public ambient_sound_image_data Clone()
        {
            var copy = (ambient_sound_image_data) MemberwiseClone();
            copy.unused = (short[]) unused.Clone();
            return copy;
        }
    }

    /* ---------- random sound images */

    // possibly directional random sound effects
    public class random_sound_image_data // 32 bytes
    {
        public ushort flags;

        public short sound_index;

        public short volume, delta_volume;
        public short period, delta_period;
        public short direction, delta_direction; // angle
        public int pitch, delta_pitch; // _fixed

        // only used at run-time; initialize to NONE
        public short phase;

        public short[] unused = new short[3];

        public random_sound_image_data Clone()
        {
            var copy = (random_sound_image_data) MemberwiseClone();
            copy.unused = (short[]) unused.Clone();
            return copy;
        }
    }

    /* ---------- object structure */

    public class object_location
    {
        public world_point3d p;
        public short polygon_index;

        public short yaw, pitch; // angle

        public ushort flags;
    }


    /* ------------ endpoint definition */

    public class endpoint_data /* 16 bytes */
    {
        public ushort flags;
        public short highest_adjacent_floor_height, lowest_adjacent_ceiling_height; // world_distance

        public world_point2d vertex;
        public world_point2d transformed;

        public short supporting_polygon_index;

        public endpoint_data Clone() { return (endpoint_data) MemberwiseClone(); }
    }

    /* ------------ line definition */

    public class line_data /* 32 bytes */
    {
        public short[] endpoint_indexes = new short[2];
        public ushort flags; /* no permutation field */

        public short length; // world_distance
        public short highest_adjacent_floor, lowest_adjacent_ceiling; // world_distance

        /* the side definition facing the clockwise polygon which references this side, and the side
            definition facing the counterclockwise polygon (can be NONE) */
        public short clockwise_polygon_side_index, counterclockwise_polygon_side_index;

        /* a line can be owned by a clockwise polygon, a counterclockwise polygon, or both (but never
            two of the same) (can be NONE) */
        public short clockwise_polygon_owner, counterclockwise_polygon_owner;

        public short[] unused = new short[6];

        // decorative lines always pass projectiles through their transparent sides
        public bool is_decorative()
        {
            return (flags & map.LINE_IS_DECORATIVE_BIT) != 0;
        }

        public void set_decorative(bool b)
        {
            if (b) flags |= map.LINE_IS_DECORATIVE_BIT;
            else flags &= unchecked((ushort) ~map.LINE_IS_DECORATIVE_BIT);
        }

        public line_data Clone()
        {
            var copy = (line_data) MemberwiseClone();
            copy.endpoint_indexes = (short[]) endpoint_indexes.Clone();
            copy.unused = (short[]) unused.Clone();
            return copy;
        }
    }

    /* --------------- side definition */

    public class side_texture_definition
    {
        public short x0, y0; // world_distance
        public ushort texture; // shape_descriptor

        public side_texture_definition Clone() { return (side_texture_definition) MemberwiseClone(); }
    }

    public class side_exclusion_zone
    {
        public world_point2d e0, e1, e2, e3;

        public side_exclusion_zone Clone() { return (side_exclusion_zone) MemberwiseClone(); }
    }

    public class side_data /* size platform-dependant */
    {
        public short type;
        public ushort flags;

        public side_texture_definition primary_texture = new side_texture_definition();
        public side_texture_definition secondary_texture = new side_texture_definition();
        public side_texture_definition transparent_texture = new side_texture_definition(); /* not drawn if .texture==NONE */

        /* all sides have the potential of being impassable; the exclusion zone is the area near
            the side which cannot be walked through */
        public side_exclusion_zone exclusion_zone = new side_exclusion_zone();

        public short control_panel_type; /* Only valid if side->flags & _side_is_control_panel */
        public short control_panel_permutation; /* platform index, light source index, etc... */

        public short primary_transfer_mode; /* These should be in the side_texture_definition.. */
        public short secondary_transfer_mode;
        public short transparent_transfer_mode;

        public short polygon_index, line_index;

        public short primary_lightsource_index;
        public short secondary_lightsource_index;
        public short transparent_lightsource_index;

        public int ambient_delta;

        public short[] unused = new short[1];

        public side_data Clone()
        {
            var copy = (side_data) MemberwiseClone();
            copy.primary_texture = primary_texture.Clone();
            copy.secondary_texture = secondary_texture.Clone();
            copy.transparent_texture = transparent_texture.Clone();
            copy.exclusion_zone = exclusion_zone.Clone();
            copy.unused = (short[]) unused.Clone();
            return copy;
        }
    }

    /* ----------- polygon definition */

    public class horizontal_surface_data /* should be in polygon structure */
    {
        public short height; // world_distance
        public short lightsource_index;
        public ushort texture; // shape_descriptor
        public short transfer_mode, transfer_mode_data;

        public world_point2d origin;
    }

    public class polygon_data /* 128 bytes */
    {
        public short type;
        public ushort flags;
        public short permutation;

        public ushort vertex_count;
        public short[] endpoint_indexes = new short[map.MAXIMUM_VERTICES_PER_POLYGON]; /* clockwise */
        public short[] line_indexes = new short[map.MAXIMUM_VERTICES_PER_POLYGON];

        public ushort floor_texture, ceiling_texture; // shape_descriptor
        public short floor_height, ceiling_height; // world_distance
        public short floor_lightsource_index, ceiling_lightsource_index;

        public int area; /* in world_distance^2 units */

        public short first_object;

        /* precalculated impassability information; each polygon has a list of lines and points
            that anything big (i.e., monsters but not projectiles) inside it must check against when
            ending a move inside it. */
        public short first_exclusion_zone_index;
        public short line_exclusion_zone_count;
        public short point_exclusion_zone_count;

        public short floor_transfer_mode;
        public short ceiling_transfer_mode;

        public short[] adjacent_polygon_indexes = new short[map.MAXIMUM_VERTICES_PER_POLYGON];

        /* a list of polygons within WORLD_ONE of us */
        public short first_neighbor_index;
        public short neighbor_count;

        public world_point2d center;

        public short[] side_indexes = new short[map.MAXIMUM_VERTICES_PER_POLYGON];

        public world_point2d floor_origin, ceiling_origin;

        public short media_index;
        public short media_lightsource_index;

        /* NONE terminated list of _saved_sound_source indexes which must be checked while a
            listener is inside this polygon (can be none) */
        public short sound_source_indexes;

        // either can be NONE
        public short ambient_sound_image_index;
        public short random_sound_image_index;

        public short[] unused = new short[1];

        public polygon_data Clone()
        {
            var copy = (polygon_data) MemberwiseClone();
            copy.endpoint_indexes = (short[]) endpoint_indexes.Clone();
            copy.line_indexes = (short[]) line_indexes.Clone();
            copy.adjacent_polygon_indexes = (short[]) adjacent_polygon_indexes.Clone();
            copy.side_indexes = (short[]) side_indexes.Clone();
            copy.unused = (short[]) unused.Clone();
            return copy;
        }
    }

    /* ----------- static light definition */

    public class saved_lighting_function_specification /* 7*2 == 14 bytes */
    {
        public short function;

        public short period, delta_period;
        public ushort intensity_hi, intensity_lo, delta_intensity_hi, delta_intensity_lo;
    }

    public class saved_static_light_data /* 8*2 + 6*14 == 100 bytes */
    {
        public short type;
        public ushort flags;

        public short phase; // initializer, so lights may start out-of-phase with each other

        public saved_lighting_function_specification primary_active = new saved_lighting_function_specification(),
            secondary_active = new saved_lighting_function_specification(),
            becoming_active = new saved_lighting_function_specification();
        public saved_lighting_function_specification primary_inactive = new saved_lighting_function_specification(),
            secondary_inactive = new saved_lighting_function_specification(),
            becoming_inactive = new saved_lighting_function_specification();

        public short tag;

        public short[] unused = new short[4];
    }

    /* ---------- new object frequency structures. */

    public class object_frequency_definition
    {
        public ushort flags;

        public short initial_count;   // number that initially appear. can be greater than maximum_count
        public short minimum_count;   // this number of objects will be maintained.
        public short maximum_count;   // can't exceed this, except at the beginning of the level.

        public short random_count;    // maximum random occurences of the object
        public ushort random_chance;    // in (0, 65535]

        public object_frequency_definition Clone() { return (object_frequency_definition) MemberwiseClone(); }
    }

    /* ---------- map */

    /* current map number is in player->map */
    public class static_data
    {
        public short environment_code;

        public short physics_model;
        public short song_index;
        public short mission_flags;
        public short environment_flags;

        public bool ball_in_play; // true if there's a ball in play
        public bool unused1;
        public short[] unused = new short[3];

        public byte[] level_name = new byte[map.LEVEL_NAME_LENGTH];
        public uint entry_point_flags;

        public static_data Clone()
        {
            var copy = (static_data) MemberwiseClone();
            copy.unused = (short[]) unused.Clone();
            copy.level_name = (byte[]) level_name.Clone();
            return copy;
        }
    }

    /* ---------- prototypes/GAME_WAD.C */

    public class map_identifier
    {
        public uint scenario_checksum;
        public short level_index;
    }

    public static class map
    {
        /* ---------- constants */

        public const int TICKS_PER_SECOND = 30;
        public const int TICKS_PER_MINUTE = (60 * TICKS_PER_SECOND);

        public const int MAP_INDEX_BUFFER_SIZE = 8192;
        public const short MINIMUM_SEPARATION_FROM_WALL = (WORLD_ONE / 4);
        public const short MINIMUM_SEPARATION_FROM_PROJECTILE = ((3 * WORLD_ONE) / 4);

        public const int TELEPORTING_DURATION = (2 * TELEPORTING_MIDPOINT);
        public const int TELEPORTING_MIDPOINT = (TICKS_PER_SECOND / 2);

        /* These arrays are the absolute limits, and are used only by the small memory allocating */
        /*  arrays.  */
        public const int MAXIMUM_LEVELS_PER_MAP = (128);

        public const int LEVEL_NAME_LENGTH = (64 + 2);

        /* ---------- damage */

        /* damage types */
        public const short _damage_explosion = 0;
        public const short _damage_electrical_staff = 1;
        public const short _damage_projectile = 2;
        public const short _damage_absorbed = 3;
        public const short _damage_flame = 4;
        public const short _damage_hound_claws = 5;
        public const short _damage_alien_projectile = 6;
        public const short _damage_hulk_slap = 7;
        public const short _damage_compiler_bolt = 8;
        public const short _damage_fusion_bolt = 9;
        public const short _damage_hunter_bolt = 10;
        public const short _damage_fist = 11;
        public const short _damage_teleporter = 12;
        public const short _damage_defender = 13;
        public const short _damage_yeti_claws = 14;
        public const short _damage_yeti_projectile = 15;
        public const short _damage_crushing = 16;
        public const short _damage_lava = 17;
        public const short _damage_suffocation = 18;
        public const short _damage_goo = 19;
        public const short _damage_energy_drain = 20;
        public const short _damage_oxygen_drain = 21;
        public const short _damage_hummer_bolt = 22;
        public const short _damage_shotgun_projectile = 23;
        public const short NUMBER_OF_DAMAGE_TYPES = 24;

        /* damage flags */
        public const short _alien_damage = 0x1; /* will be decreased at lower difficulty levels */

        public const int SIZEOF_damage_definition = 12;

        /* ---------- saved objects (initial map locations, etc.) */

        // #define MAXIMUM_SAVED_OBJECTS 384

        /* map object types */
        public const short _saved_monster = 0; /* .index is monster type */
        public const short _saved_object = 1; /* .index is scenery type */
        public const short _saved_item = 2; /* .index is item type */
        public const short _saved_player = 3; /* .index is team bitfield */
        public const short _saved_goal = 4; /* .index is goal number */
        public const short _saved_sound_source = 5; /* .index is source type, .facing is sound volume */

        /* map object flags */
        public const ushort _map_object_is_invisible = 0x0001; /* initially invisible */
        public const ushort _map_object_is_platform_sound = 0x0001;
        public const ushort _map_object_hanging_from_ceiling = 0x0002; /* used for calculating absolute .z coordinate */
        public const ushort _map_object_is_blind = 0x0004; /* monster cannot activate by sight */
        public const ushort _map_object_is_deaf = 0x0008; /* monster cannot activate by sound */
        public const ushort _map_object_floats = 0x0010; /* used by sound sources caused by media */
        public const ushort _map_object_is_network_only = 0x0020; /* for items only */
        // top four bits is activation bias for monsters

        public static int DECODE_ACTIVATION_BIAS(int f) { return ((f) >> 12); }
        public static int ENCODE_ACTIVATION_BIAS(int b) { return ((b) << 12); }

        public const int SIZEOF_map_object = 16;

        /* ---------- map loading/new game structures */

        /* entry point types- this is per map level (int32). */
        public const int _single_player_entry_point = 0x01;
        public const int _multiplayer_cooperative_entry_point = 0x02;
        public const int _multiplayer_carnage_entry_point = 0x04;
        public const int _kill_the_man_with_the_ball_entry_point = 0x08; // was _capture_the_flag_entry_point, even though Bungie used it for KTMWTB
        public const int _king_of_hill_entry_point = 0x10;
        public const int _defense_entry_point = 0x20;
        public const int _rugby_entry_point = 0x40;
        public const int _capture_the_flag_entry_point = 0x80;

        public const int MAXIMUM_PLAYER_START_NAME_LENGTH = 32;

        public const ushort _player_start_doesnt_auto_switch_weapons_flag = 0x8000;

        public const ushort player_start_identifier_mask = (1 << 14) - 1;

        /* inline definitions for relevant player_start_data flags */
        public static short player_identifier_value(short identifier)
        { return (short) (identifier & player_start_identifier_mask); }

        public static short player_start_identifier_value(player_start_data p)
        { return (short) ((p).identifier & player_start_identifier_mask); }

        public static bool player_identifier_doesnt_auto_switch_weapons(short identifier)
        { return TEST_FLAG(identifier, _player_start_doesnt_auto_switch_weapons_flag); }

        public static bool player_start_doesnt_auto_switch_Weapons(player_start_data p)
        { return TEST_FLAG(p.identifier, _player_start_doesnt_auto_switch_weapons_flag); }

        public static void set_player_start_doesnt_auto_switch_weapons_status(player_start_data p, bool v)
        { p.identifier = (short) SET_FLAG(p.identifier, _player_start_doesnt_auto_switch_weapons_flag, v); }
        /* end - inline definitions for relevant player_start_data flags */

        public const int SIZEOF_directory_data = 74;

        /* ---------- map annotations */

        // #define MAXIMUM_ANNOTATIONS_PER_MAP 20
        public const int MAXIMUM_ANNOTATION_TEXT_LENGTH = 64;

        public const int SIZEOF_map_annotation = 72;

        /* ---------- ambient sound images */

        // #define MAXIMUM_AMBIENT_SOUND_IMAGES_PER_MAP 64

        public const int SIZEOF_ambient_sound_image_data = 16;

        /* ---------- random sound images */

        // #define MAXIMUM_RANDOM_SOUND_IMAGES_PER_MAP 64

        // sound image flags
        public const ushort _sound_image_is_non_directional = 0x0001; // ignore direction

        public const int SIZEOF_random_sound_image_data = 32;

        /* ---------- object structure */

        /* SLOT_IS_USED(), SLOT_IS_FREE(), MARK_SLOT_AS_FREE(), MARK_SLOT_AS_USED() macros are also used
            for monsters, effects and projectiles */
        public static bool SLOT_IS_USED(ushort flags) { return (flags & (ushort) 0x8000) != 0; }
        public static bool SLOT_IS_FREE(ushort flags) { return (!SLOT_IS_USED(flags)); }
        public static void MARK_SLOT_AS_FREE(ref ushort flags) { flags &= unchecked((ushort) ~0xC000); }
        public static void MARK_SLOT_AS_USED(ref ushort flags) { flags = (ushort) ((flags | (ushort) 0x8000) & unchecked((ushort) ~0x4000)); }

        /* object was animated flags */
        public const ushort _obj_not_animated = 0x0000; /* nothing happened */
        public const ushort _obj_animated = 0x2000; /* a new frame was reached */
        public const ushort _obj_keyframe_started = 0x1000; /* the key-frame was reached */
        public const ushort _obj_last_frame_animated = 0x0800; /* sequence complete, returning to first frame */
        public const ushort _obj_transfer_mode_finished = 0x0400; /* transfer mode phase is about to loop */

        /* object scale flags */
        public const ushort _object_is_enlarged = 0x0200;
        public const ushort _object_is_tiny = 0x0100;
        public const ushort OBJECT_SCALE_FLAGS_MASK = _object_is_enlarged | _object_is_tiny;

        /* object owners (8) */
        public const short _object_is_normal = 0; /* normal */
        public const short _object_is_scenery = 1; /* impassable scenery */
        public const short _object_is_monster = 2; /* monster index in .permutation */
        public const short _object_is_projectile = 3; /* active projectile index in .permutation */
        public const short _object_is_effect = 4; /* explosion or something; index in .permutation */
        public const short _object_is_item = 5; /* .permutation is item type */
        public const short _object_is_device = 6; /* status given by bit in flags field, device type in .permutation */
        public const short _object_is_garbage = 7; /* will be removed by garbage collection algorithms */

        /* because of sign problems, we must rip out the values before we modify them; frame is
            in [0,16), phase is in [0,4096) ... this is for shape animations */
        public static int GET_SEQUENCE_FRAME(int s) { return ((s) >> 12); }
        public static int GET_SEQUENCE_PHASE(int s) { return ((s) & 4095); }
        public static int BUILD_SEQUENCE(int f, int p) { return (((f) << 12) | (p)); }

        /* object transfer modes (high-level) */
        public const short _xfer_normal = 0;
        public const short _xfer_fade_out_to_black = 1; /* reduce ambient light until black, then tint-fade out */
        public const short _xfer_invisibility = 2;
        public const short _xfer_subtle_invisibility = 3;
        public const short _xfer_pulsate = 4; /* only valid for polygons */
        public const short _xfer_wobble = 5; /* only valid for polygons */
        public const short _xfer_fast_wobble = 6; /* only valid for polygons */
        public const short _xfer_static = 7;
        public const short _xfer_50percent_static = 8;
        public const short _xfer_landscape = 9;
        public const short _xfer_smear = 10; /* repeat pixel(0,0) of texture everywhere */
        public const short _xfer_fade_out_static = 11;
        public const short _xfer_pulsating_static = 12;
        public const short _xfer_fold_in = 13; /* appear */
        public const short _xfer_fold_out = 14; /* disappear */
        public const short _xfer_horizontal_slide = 15;
        public const short _xfer_fast_horizontal_slide = 16;
        public const short _xfer_vertical_slide = 17;
        public const short _xfer_fast_vertical_slide = 18;
        public const short _xfer_wander = 19;
        public const short _xfer_fast_wander = 20;
        public const short _xfer_big_landscape = 21;
        public const short _xfer_reverse_horizontal_slide = 22;
        public const short _xfer_reverse_fast_horizontal_slide = 23;
        public const short _xfer_reverse_vertical_slide = 24;
        public const short _xfer_reverse_fast_vertical_slide = 25;
        public const short _xfer_2x = 26; // scales texture by 2x
        public const short _xfer_4x = 27; // scales texture by 4x
        public const short NUMBER_OF_TRANSFER_MODES = 28;

        public const int SIZEOF_object_data = 32;

        /* ------------ endpoint definition */

        public static bool ENDPOINT_IS_SOLID(endpoint_data e) { return ((e).flags & 1) != 0; }
        public static void SET_ENDPOINT_SOLIDITY(endpoint_data e, bool s) { if (s) e.flags |= 1; else e.flags &= unchecked((ushort) ~(ushort) 1); }

        public static bool ENDPOINT_IS_TRANSPARENT(endpoint_data e) { return ((e).flags & 4) != 0; }
        public static void SET_ENDPOINT_TRANSPARENCY(endpoint_data e, bool s) { if (s) e.flags |= 4; else e.flags &= unchecked((ushort) ~(ushort) 4); }

        /* false if all polygons sharing this endpoint have the same height */
        public static bool ENDPOINT_IS_ELEVATION(endpoint_data e) { return ((e).flags & 2) != 0; }
        public static void SET_ENDPOINT_ELEVATION(endpoint_data e, bool s) { if (s) e.flags |= 2; else e.flags &= unchecked((ushort) ~(ushort) 2); }

        public const int SIZEOF_endpoint_data = 16;

        // For loading plain points:
        public const int SIZEOF_world_point2d = 4;

        /* ------------ line definition */

        public const ushort SOLID_LINE_BIT = 0x4000;
        public const ushort TRANSPARENT_LINE_BIT = 0x2000;
        public const ushort LANDSCAPE_LINE_BIT = 0x1000;
        public const ushort ELEVATION_LINE_BIT = 0x800;
        public const ushort VARIABLE_ELEVATION_LINE_BIT = 0x400;
        public const ushort LINE_HAS_TRANSPARENT_SIDE_BIT = 0x200;
        public const ushort LINE_IS_DECORATIVE_BIT = 0x100;

        private static void SET_LINE_BIT(line_data l, ushort bit, bool v) { if (v) l.flags |= bit; else l.flags &= unchecked((ushort) ~bit); }

        public static void SET_LINE_SOLIDITY(line_data l, bool v) { SET_LINE_BIT(l, SOLID_LINE_BIT, v); }
        public static bool LINE_IS_SOLID(line_data l) { return ((l).flags & SOLID_LINE_BIT) != 0; }

        public static void SET_LINE_TRANSPARENCY(line_data l, bool v) { SET_LINE_BIT(l, TRANSPARENT_LINE_BIT, v); }
        public static bool LINE_IS_TRANSPARENT(line_data l) { return ((l).flags & TRANSPARENT_LINE_BIT) != 0; }

        public static void SET_LINE_LANDSCAPE_STATUS(line_data l, bool v) { SET_LINE_BIT(l, LANDSCAPE_LINE_BIT, v); }
        public static bool LINE_IS_LANDSCAPED(line_data l) { return ((l).flags & LANDSCAPE_LINE_BIT) != 0; }

        public static void SET_LINE_ELEVATION(line_data l, bool v) { SET_LINE_BIT(l, ELEVATION_LINE_BIT, v); }
        public static bool LINE_IS_ELEVATION(line_data l) { return ((l).flags & ELEVATION_LINE_BIT) != 0; }

        public static void SET_LINE_VARIABLE_ELEVATION(line_data l, bool v) { SET_LINE_BIT(l, VARIABLE_ELEVATION_LINE_BIT, v); }
        public static bool LINE_IS_VARIABLE_ELEVATION(line_data l) { return ((l).flags & VARIABLE_ELEVATION_LINE_BIT) != 0; }

        public static void SET_LINE_HAS_TRANSPARENT_SIDE(line_data l, bool v) { SET_LINE_BIT(l, LINE_HAS_TRANSPARENT_SIDE_BIT, v); }
        public static bool LINE_HAS_TRANSPARENT_SIDE(line_data l) { return ((l).flags & LINE_HAS_TRANSPARENT_SIDE_BIT) != 0; }

        public const int SIZEOF_line_data = 32;

        /* --------------- side definition */

        /* side flags */
        public const ushort _control_panel_status = 0x0001;
        public const ushort _side_is_control_panel = 0x0002;
        public const ushort _side_is_repair_switch = 0x0004; // must be toggled to exit level
        public const ushort _side_is_destructive_switch = 0x0008; // uses an item
        public const ushort _side_is_lighted_switch = 0x0010; // switch must be lighted to use
        public const ushort _side_switch_can_be_destroyed = 0x0020; // projectile hits toggle and destroy this switch
        public const ushort _side_switch_can_only_be_hit_by_projectiles = 0x0040;
        public const ushort _side_item_is_optional = 0x0080; // in Marathon, switches still work without items
        public const ushort _side_is_m1_lighted_switch = 0x0100; // in Marathon, lighted switches must be above 50% (unlike M2, 75%)

        public const ushort _editor_dirty_bit = 0x4000; // used by the editor...
        public const ushort _reserved_side_flag = 0x8000; // some maps written by an old map editor
                                                          // (Pfhorte?) set lots of side flags; use this
                                                          // to detect and correct

        /* control panel side types */
        public const short _panel_is_oxygen_refuel = 0;
        public const short _panel_is_shield_refuel = 1;
        public const short _panel_is_double_shield_refuel = 2;
        public const short _panel_is_triple_shield_refuel = 3;
        public const short _panel_is_light_switch = 4; // light index in .permutation
        public const short _panel_is_platform_switch = 5; // platform index in .permutation
        public const short _panel_is_tag_switch = 6; // tag in .permutation (NONE is tagless)
        public const short _panel_is_pattern_buffer = 7;
        public const short _panel_is_computer_terminal = 8;
        public const short NUMBER_OF_CONTROL_PANELS = 9;

        private static void SET_SIDE_BIT(side_data s, ushort bit, bool v) { if (v) s.flags |= bit; else s.flags &= unchecked((ushort) ~bit); }

        public static bool SIDE_IS_CONTROL_PANEL(side_data s) { return ((s).flags & _side_is_control_panel) != 0; }
        public static void SET_SIDE_CONTROL_PANEL(side_data s, bool t) { SET_SIDE_BIT(s, _side_is_control_panel, t); }

        public static bool GET_CONTROL_PANEL_STATUS(side_data s) { return (((s).flags & _control_panel_status) != 0); }
        public static void SET_CONTROL_PANEL_STATUS(side_data s, bool t) { SET_SIDE_BIT(s, _control_panel_status, t); }
        public static void TOGGLE_CONTROL_PANEL_STATUS(side_data s) { (s).flags ^= _control_panel_status; }

        public static bool SIDE_IS_REPAIR_SWITCH(side_data s) { return ((s).flags & _side_is_repair_switch) != 0; }
        public static void SET_SIDE_IS_REPAIR_SWITCH(side_data s, bool t) { SET_SIDE_BIT(s, _side_is_repair_switch, t); }

        /* Flags used by Vulcan */
        public static bool SIDE_IS_DIRTY(side_data s) { return ((s).flags & _editor_dirty_bit) != 0; }
        public static void SET_SIDE_IS_DIRTY(side_data s, bool t) { SET_SIDE_BIT(s, _editor_dirty_bit, t); }

        /* side types (largely redundant; most of this could be guessed for examining adjacent polygons) */
        public const short _full_side = 0; /* primary texture is mapped floor-to-ceiling */
        public const short _high_side = 1; /* primary texture is mapped on a panel coming down from the ceiling (implies 2 adjacent polygons) */
        public const short _low_side = 2; /* primary texture is mapped on a panel coming up from the floor (implies 2 adjacent polygons) */
        public const short _composite_side = 3; /* primary texture is mapped floor-to-ceiling, secondary texture is mapped into it (i.e., control panel) */
        public const short _split_side = 4; /* primary texture is mapped onto a panel coming down from the ceiling, secondary
            texture is mapped on a panel coming up from the floor */

        public const int SIZEOF_side_data = 64;

        /* ----------- polygon definition */

        public const int MAXIMUM_VERTICES_PER_POLYGON = 8;

        // LP/AlexJLS change: added Marathon 1 polygon damage and glue stuff
        /* polygon types */
        public const short _polygon_is_normal = 0;
        public const short _polygon_is_item_impassable = 1;
        public const short _polygon_is_monster_impassable = 2;
        public const short _polygon_is_hill = 3; /* for king-of-the-hill */
        public const short _polygon_is_base = 4; /* for capture the flag, rugby, etc. (team in .permutation) */
        public const short _polygon_is_platform = 5; /* platform index in .permutation */
        public const short _polygon_is_light_on_trigger = 6; /* lightsource index in .permutation */
        public const short _polygon_is_platform_on_trigger = 7; /* polygon index in .permutation */
        public const short _polygon_is_light_off_trigger = 8; /* lightsource index in .permutation */
        public const short _polygon_is_platform_off_trigger = 9; /* polygon index in .permutation */
        public const short _polygon_is_teleporter = 10; /* .permutation is polygon_index of destination */
        public const short _polygon_is_zone_border = 11;
        public const short _polygon_is_goal = 12;
        public const short _polygon_is_visible_monster_trigger = 13;
        public const short _polygon_is_invisible_monster_trigger = 14;
        public const short _polygon_is_dual_monster_trigger = 15;
        public const short _polygon_is_item_trigger = 16; /* activates all items in this zone */
        public const short _polygon_must_be_explored = 17;
        public const short _polygon_is_automatic_exit = 18; /* if success conditions are met, causes automatic transport too next level */
        public const short _polygon_is_minor_ouch = 19;
        public const short _polygon_is_major_ouch = 20;
        public const short _polygon_is_glue = 21;
        public const short _polygon_is_glue_trigger = 22;
        public const short _polygon_is_superglue = 23;

        public const ushort POLYGON_IS_DETACHED_BIT = 0x4000;
        public static bool POLYGON_IS_DETACHED(polygon_data p) { return ((p).flags & POLYGON_IS_DETACHED_BIT) != 0; }
        public static void SET_POLYGON_DETACHED_STATE(polygon_data p, bool v) { if (v) p.flags |= POLYGON_IS_DETACHED_BIT; else p.flags &= unchecked((ushort) ~POLYGON_IS_DETACHED_BIT); }

        public const int SIZEOF_polygon_data = 128;

        /* ----------- static light definition */

        public const int SIZEOF_saved_static_light_data = 100;

        /* ---------- random placement data structures.. */

        /* game difficulty levels */
        public const short _wuss_level = 0;
        public const short _easy_level = 1;
        public const short _normal_level = 2;
        public const short _major_damage_level = 3;
        public const short _total_carnage_level = 4;
        public const short NUMBER_OF_GAME_DIFFICULTY_LEVELS = 5;

        /* for difficulty level names (moved here so it is in a common header file) */
        public const short kDifficultyLevelsStringSetID = 145;

        /* ---------- new object frequency structures. */

        public const int MAXIMUM_OBJECT_TYPES = 64;

        // flags for object_frequency_definition
        public const ushort _reappears_in_random_location = 0x0001;

        public const int SIZEOF_object_frequency_definition = 12;

        /* ---------- map */

        /* mission flags */
        public const short _mission_none = 0x0000;
        public const short _mission_extermination = 0x0001;
        public const short _mission_exploration = 0x0002;
        public const short _mission_retrieval = 0x0004;
        public const short _mission_repair = 0x0008;
        public const short _mission_rescue = 0x0010;
        public const short _mission_exploration_m1 = 0x0020;
        public const short _mission_rescue_m1 = 0x0040;
        public const short _mission_repair_m1 = 0x0080;

        /* environment flags */
        public const short _environment_normal = 0x0000;
        public const short _environment_vacuum = 0x0001; // prevents certain weapons from working, player uses oxygen
        public const short _environment_magnetic = 0x0002; // motion sensor works poorly
        public const short _environment_rebellion = 0x0004; // makes clients fight pfhor
        public const short _environment_low_gravity = 0x0008; // low gravity
        public const short _environment_glue_m1 = 0x0010; // handle glue polygons like Marathon 1
        public const short _environment_ouch_m1 = 0x0020; // the floor is lava
        public const short _environment_rebellion_m1 = 0x0040;  // use Marathon 1 rebellion (don't strip items/health)
        public const short _environment_song_index_m1 = 0x0080; // play music
        public const short _environment_terminals_stop_time = 0x0100; // solo only
        public const short _environment_activation_ranges = 0x0200; // Marathon 1 monster activation limits
        public const short _environment_m1_weapons = 0x0400;    // multiple weapon pickups on TC; low gravity grenades

        public const short _environment_network = 0x2000; // these two pseudo-environments are used to prevent items
        public const short _environment_single_player = 0x4000; // from arriving in the items.c code.

        public const int SIZEOF_static_data = 88;

        /* game options.. */
        public const ushort _multiplayer_game = 0x0001; /* multi or single? */
        public const ushort _ammo_replenishes = 0x0002; /* Does or doesn't */
        public const ushort _weapons_replenish = 0x0004; /* Weapons replenish? */
        public const ushort _specials_replenish = 0x0008; /* Invisibility, Ammo? */
        public const ushort _monsters_replenish = 0x0010; /* Monsters are lazarus.. */
        public const ushort _motion_sensor_does_not_work = 0x00020; /* Motion sensor works */
        public const ushort _overhead_map_is_omniscient = 0x0040; /* Only show teammates on overhead map */
        public const ushort _burn_items_on_death = 0x0080; /* When you die, you lose everything but the initial crap.. */
        public const ushort _live_network_stats = 0x0100;
        public const ushort _game_has_kill_limit = 0x0200;  /* Game ends when the kill limit is reached. */
        public const ushort _force_unique_teams = 0x0400; /* every player must have a unique team */
        public const ushort _dying_is_penalized = 0x0800; /* time penalty for dying */
        public const ushort _suicide_is_penalized = 0x1000; /* time penalty for killing yourselves */
        public const ushort _overhead_map_shows_items = 0x2000;
        public const ushort _overhead_map_shows_monsters = 0x4000;
        public const ushort _overhead_map_shows_projectiles = 0x8000;

        /* cheat flags */
        public const ushort _allow_crosshair = 0x0001;
        public const ushort _allow_tunnel_vision = 0x0002;
        public const ushort _allow_behindview = 0x0004;
        public const ushort _disable_carnage_messages = 0x0008;
        public const ushort _disable_saving_level = 0x0010;
        public const ushort _allow_overlay_map = 0x0020;

        // specifies how the user completed the level. saved in dynamic_data
        public const short _level_unfinished = 0;
        public const short _level_finished = 1;
        public const short _level_failed = 2;

        /* Game types! */
        public const short _game_of_kill_monsters = 0; // single player & combative use this
        public const short _game_of_cooperative_play = 1; // multiple players, working together
        public const short _game_of_capture_the_flag = 2; // A team game.
        public const short _game_of_king_of_the_hill = 3;
        public const short _game_of_kill_man_with_ball = 4;
        public const short _game_of_defense = 5;
        public const short _game_of_rugby = 6;
        public const short _game_of_tag = 7;
        public const short _game_of_custom = 8;
        public const short NUMBER_OF_GAME_TYPES = 9;

        public const int SIZEOF_dynamic_data = 604;

        /* ---------- globals (map.cpp) */

        // LP: modified texture-environment management so as to be easier to handle with XML

        public const int NUMBER_OF_ENVIRONMENTS = 5;
        public const int NUMBER_OF_ENV_COLLECTIONS = 7;

        public static readonly short[,] Environments = new short[NUMBER_OF_ENVIRONMENTS, NUMBER_OF_ENV_COLLECTIONS]
        {
            {_collection_walls1, _collection_scenery1, NONE, NONE, NONE, NONE, NONE}, // Lh'owon Water
            {_collection_walls2, _collection_scenery2, NONE, NONE, NONE, NONE, NONE}, // Lh'owon Lava
            {_collection_walls3, _collection_scenery3, NONE, NONE, NONE, NONE, NONE}, // Lh'owon Sewage
            {_collection_walls4, _collection_scenery4, NONE, NONE, NONE, NONE, NONE}, // Jjaro (originally to be Pathways or Marathon)
            {_collection_walls5, _collection_scenery5, NONE, NONE, NONE, NONE, NONE} // Pfhor
        };

        /* ---------- accessors (map.cpp; map_accessors.c) */

        // LP changed: previously inline; now de-inlined for less code bulk
        // When the index is out of range,
        // the geometry ones make failed asserts,
        // while the sound ones return null pointers.

        public static polygon_data get_polygon_data(MapLevel level, short polygon_index)
        {
            assert(level.PolygonList != null);
            polygon_data polygon = GetMemberWithBounds(level.PolygonList, polygon_index, level.PolygonList.Count);

            vassert(polygon != null, $"polygon index #{polygon_index} is out of range");

            return polygon;
        }

        public static line_data get_line_data(MapLevel level, short line_index)
        {
            assert(level.LineList != null);
            line_data line = GetMemberWithBounds(level.LineList, line_index, level.LineList.Count);

            vassert(line != null, $"line index #{line_index} is out of range");

            return line;
        }

        public static side_data get_side_data(MapLevel level, short side_index)
        {
            assert(level.SideList != null);
            side_data side = GetMemberWithBounds(level.SideList, side_index, level.SideList.Count);

            vassert(side != null, $"side index #{side_index} is out of range");

            return side;
        }

        public static endpoint_data get_endpoint_data(MapLevel level, short endpoint_index)
        {
            assert(level.EndpointList != null);
            endpoint_data endpoint = GetMemberWithBounds(level.EndpointList, endpoint_index, level.EndpointList.Count);

            vassert(endpoint != null, $"endpoint index #{endpoint_index} is out of range");

            return endpoint;
        }

        // The index of map_indexes[index] in MapIndexList, or NONE for NULL
        public static int get_map_indexes(MapLevel level, short index, short count)
        {
            assert(level.MapIndexList != null);
            ushort i = unchecked((ushort) index);
            int number = unchecked((ushort) level.MapIndexList.Count) - count + 1;

            // vassert(map_index, csprintf(temporary, "map_indexes(#%d,#%d) are out of range", index, count));

            return (number > 0 && i < (uint) number) ? i : NONE;
        }

        public static ambient_sound_image_data get_ambient_sound_image_data(MapLevel level, short ambient_sound_image_index)
        {
            return GetMemberWithBounds(level.AmbientSoundImageList, ambient_sound_image_index, level.AmbientSoundImageList.Count);
        }

        public static random_sound_image_data get_random_sound_image_data(MapLevel level, short random_sound_image_index)
        {
            return GetMemberWithBounds(level.RandomSoundImageList, random_sound_image_index, level.RandomSoundImageList.Count);
        }

        /* ---------- map.cpp */

        public static bool collection_in_environment(short collection_code, short environment_code)
        {
            short collection_index = (short) GET_COLLECTION(collection_code);
            bool found = false;
            int i;

            if (!(environment_code >= 0 && environment_code < NUMBER_OF_ENVIRONMENTS)) return false;
            assert(collection_index >= 0 && collection_index < NUMBER_OF_COLLECTIONS);

            for (i = 0; i < NUMBER_OF_ENV_COLLECTIONS; ++i)
            {
                if (Environments[environment_code, i] == collection_index)
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        public static void calculate_line_midpoint(MapLevel level, short line_index, out world_point3d midpoint)
        {
            line_data line = get_line_data(level, line_index);
            world_point2d e0 = get_endpoint_data(level, line.endpoint_indexes[0]).vertex;
            world_point2d e1 = get_endpoint_data(level, line.endpoint_indexes[1]).vertex;

            midpoint.x = (short) ((e0.x + e1.x) >> 1);
            midpoint.y = (short) ((e0.y + e1.y) >> 1);
            midpoint.z = (short) ((line.lowest_adjacent_ceiling + line.highest_adjacent_floor) >> 1);
        }

        public static bool point_in_polygon(MapLevel level, short polygon_index, world_point2d p)
        {
            polygon_data polygon = get_polygon_data(level, polygon_index);
            bool point_inside = true;
            short i;

            for (i = 0; i < polygon.vertex_count; ++i)
            {
                line_data line = get_line_data(level, polygon.line_indexes[i]);
                bool clockwise = line.endpoint_indexes[0] == polygon.endpoint_indexes[i];
                world_point2d e0 = get_endpoint_data(level, line.endpoint_indexes[0]).vertex;
                world_point2d e1 = get_endpoint_data(level, line.endpoint_indexes[1]).vertex;
                int cross_product = unchecked((p.x - e0.x) * (e1.y - e0.y) - (p.y - e0.y) * (e1.x - e0.x));

                if ((clockwise && cross_product > 0) || (!clockwise && cross_product < 0))
                {
                    point_inside = false;
                    break;
                }
            }

            return point_inside;
        }

        public static short clockwise_endpoint_in_line(MapLevel level, short polygon_index, short line_index, short index)
        {
            line_data line = get_line_data(level, line_index);
            bool line_is_clockwise = true;

            if (line.clockwise_polygon_owner != polygon_index)
            {
                // LP change: get around some Pfhorte bugs
                line_is_clockwise = false;
            }

            switch (index)
            {
                case 0:
                    index = (short) (line_is_clockwise ? 0 : 1);
                    break;
                case 1:
                    index = (short) (line_is_clockwise ? 1 : 0);
                    break;
                default:
                    assert(false);
                    break;
            }

            return line.endpoint_indexes[index];
        }

        public static short world_point_to_polygon_index(MapLevel level, world_point2d location)
        {
            short polygon_index;

            for (polygon_index = 0; polygon_index < level.PolygonList.Count; ++polygon_index)
            {
                polygon_data polygon = level.PolygonList[polygon_index];
                if (!POLYGON_IS_DETACHED(polygon))
                {
                    if (point_in_polygon(level, polygon_index, location)) break;
                }
            }
            if (polygon_index == level.PolygonList.Count) polygon_index = NONE;

            return polygon_index;
        }

        /* return the polygon on the other side of the given line from the given polygon (i.e., return
            the polygon adjacent to line_index which isn't polygon_index).  can return NONE. */
        public static short find_adjacent_polygon(MapLevel level, short polygon_index, short line_index)
        {
            line_data line = get_line_data(level, line_index);
            short new_polygon_index;

            if (polygon_index == line.clockwise_polygon_owner)
            {
                new_polygon_index = line.counterclockwise_polygon_owner;
            }
            else
            {
                // LP change: get around some Pfhorte bugs
                new_polygon_index = line.clockwise_polygon_owner;
            }

            assert(new_polygon_index != polygon_index);

            return new_polygon_index;
        }

        private static short find_flooding_polygon_helper(MapLevel level, short parent, short polygon_index)
        {
            polygon_data polygon = get_polygon_data(level, polygon_index);

            for (int i = 0; i < polygon.vertex_count; ++i)
            {
                short adjacent_index = polygon.adjacent_polygon_indexes[i];
                if (adjacent_index != NONE && adjacent_index != parent)
                {
                    polygon_data adjacent = get_polygon_data(level, adjacent_index);
                    if (adjacent.type == _polygon_is_major_ouch ||
                        adjacent.type == _polygon_is_minor_ouch)
                    {
                        return adjacent_index;
                    }
                }
            }

            if (film_profile.m1_platform_flood)
            {
                for (int i = 0; i < polygon.vertex_count; ++i)
                {
                    short adjacent_index = polygon.adjacent_polygon_indexes[i];
                    if (adjacent_index != NONE && adjacent_index != parent)
                    {
                        polygon_data adjacent = get_polygon_data(level, adjacent_index);
                        if (adjacent.type == _polygon_is_platform)
                        {
                            platform_data platform = platforms.get_platform_data(level, adjacent.permutation);
                            if (platform != null && platforms.PLATFORM_IS_FLOODED(platform))
                            {
                                short index = find_flooding_polygon_helper(level, polygon_index, adjacent_index);
                                if (index != NONE)
                                {
                                    return index;
                                }
                            }
                        }
                    }
                }
            }

            return NONE;
        }

        /* Find the polygon whose attributes we'll mimic on a flooded platform */
        public static short find_flooding_polygon(MapLevel level, short polygon_index)
        {
            return find_flooding_polygon_helper(level, NONE, polygon_index);
        }

        public static short find_adjacent_side(MapLevel level, short polygon_index, short line_index)
        {
            line_data line = get_line_data(level, line_index);
            short side_index;

            if (line.clockwise_polygon_owner == polygon_index)
            {
                side_index = line.clockwise_polygon_side_index;
            }
            else
            {
                assert(line.counterclockwise_polygon_owner == polygon_index);
                side_index = line.counterclockwise_polygon_side_index;
            }

            return side_index;
        }

        public static bool line_is_landscaped(MapLevel level, short polygon_index, short line_index, short z)
        {
            bool landscaped = false;
            short side_index = find_adjacent_side(level, polygon_index, line_index);

            if (side_index != NONE)
            {
                line_data line = get_line_data(level, line_index);
                side_data side = get_side_data(level, side_index);

                switch (side.type)
                {
                    case _full_side:
                        landscaped = side.primary_transfer_mode == _xfer_landscape;
                        break;
                    case _split_side: /* render _low_side first */
                        if (z < line.highest_adjacent_floor)
                        {
                            landscaped = side.secondary_transfer_mode == _xfer_landscape;
                            break;
                        }
                        goto case _high_side;
                    case _high_side:
                        landscaped = z > line.lowest_adjacent_ceiling ?
                            side.primary_transfer_mode == _xfer_landscape :
                            side.transparent_transfer_mode == _xfer_landscape;
                        break;
                    case _low_side:
                        landscaped = z < line.highest_adjacent_floor ?
                            side.primary_transfer_mode == _xfer_landscape :
                            side.transparent_transfer_mode == _xfer_landscape;
                        break;

                    default:
                        assert(false);
                        break;
                }
            }

            return landscaped;
        }

        /* return the line_index where the two polygons meet (or NONE if they don't meet) */
        public static short find_shared_line(MapLevel level, short polygon_index1, short polygon_index2)
        {
            polygon_data polygon = get_polygon_data(level, polygon_index1);
            short shared_line_index = NONE;
            short i;

            for (i = 0; i < polygon.vertex_count; ++i)
            {
                line_data line = get_line_data(level, polygon.line_indexes[i]);
                if (line.clockwise_polygon_owner == polygon_index2 || line.counterclockwise_polygon_owner == polygon_index2)
                {
                    shared_line_index = polygon.line_indexes[i];
                    break;
                }
            }

            return shared_line_index;
        }

        public static void find_center_of_polygon(MapLevel level, short polygon_index, out world_point2d center)
        {
            polygon_data polygon = get_polygon_data(level, polygon_index);
            int x = 0, y = 0;
            short i;

            for (i = 0; i < polygon.vertex_count; ++i)
            {
                world_point2d p = get_endpoint_data(level, polygon.endpoint_indexes[i]).vertex;

                x += p.x; y += p.y;
            }

            // polygon->vertex_count could possibly be zero, unsure of what to do here
            // making a note.
            center.x = unchecked((short) (x / polygon.vertex_count));
            center.y = unchecked((short) (y / polygon.vertex_count));
        }

        /* returns height at clipped p1 */
        public static void push_out_line(ref world_point2d e0, ref world_point2d e1, short d, short line_length)
        {
            short line_dx, line_dy;
            short dx, dy;

            /* if line_length is zero, calculate it */
            if (line_length == 0)
            {
                line_length = distance2d(e0, e1);
                if (line_length == 0)
                    return;
            }

            /* calculate dx, dy (a vector of length d perpendicular (outwards) to the line e0e1 */
            line_dx = unchecked((short) (e1.x - e0.x)); line_dy = unchecked((short) (e1.y - e0.y));
            dx = unchecked((short) (-(d * line_dy) / line_length)); dy = unchecked((short) ((d * line_dx) / line_length));

            /* adjust the line */
            e0.x = unchecked((short) (e0.x + dx)); e0.y = unchecked((short) (e0.y + dy));
            e1.x = unchecked((short) (e1.x + dx)); e1.y = unchecked((short) (e1.y + dy));
        }

        /* computes the squared distance from p to the line segment e0e1 */
        public static int point_to_line_segment_distance_squared(world_point2d p, world_point2d a, world_point2d b)
        {
            short abx = unchecked((short) (b.x - a.x)), aby = unchecked((short) (b.y - a.y));
            short apx = unchecked((short) (p.x - a.x)), apy = unchecked((short) (p.y - a.y));
            short bpx = unchecked((short) (p.x - b.x)), bpy = unchecked((short) (p.y - b.y));
            int distance;

            /* if AB dot BP is greather than or equal to zero, d is the distance between B and P */
            if (unchecked(abx * bpx + aby * bpy) >= 0)
            {
                distance = unchecked(bpx * bpx + bpy * bpy);
            }
            else
            {
                /* if BA dot AP is greather than or equal to zero, d is the distance between A and P
                    (we don't calculate BA and use -AB instead */
                if (unchecked(abx * apx + aby * apy) <= 0)
                {
                    distance = unchecked(apx * apx + apy * apy);
                }
                else
                {
                    distance = point_to_line_distance_squared(p, a, b);
                }
            }

            return distance;
        }

        public static int point_to_line_distance_squared(world_point2d p, world_point2d a, world_point2d b)
        {
            short abx = unchecked((short) (b.x - a.x)), aby = unchecked((short) (b.y - a.y));
            short apx = unchecked((short) (p.x - a.x)), apy = unchecked((short) (p.y - a.y));
            int signed_numerator;
            uint numerator, denominator;

            /* numerator is absolute value of the cross product of AB and AP, denominator is the
                magnitude of AB squared */
            signed_numerator = unchecked(apx * aby - apy * abx);
            numerator = unchecked((uint) Math.Abs(signed_numerator));
            denominator = unchecked((uint) (abx * abx + aby * aby));

            /* before squaring numerator we make sure that it is smaller than fifteen bits (and we
                adjust the denominator to compensate).  if denominator==0 then we make it ==1.  */
            while (numerator >= (1 << 16)) { numerator >>= 1; denominator >>= 2; }
            if (denominator == 0) denominator = 1;

            return unchecked((int) ((numerator * numerator) / denominator));
        }

        public static map_annotation get_next_map_annotation(MapLevel level, ref short count)
        {
            map_annotation annotation = null;

            if (count < level.MapAnnotationList.Count) annotation = level.MapAnnotationList[count++];

            return annotation;
        }

        /* for saving or whatever; finds the highest used index plus one for objects, monsters, projectiles
            and effects */
        // Only dynamic_world->light_count, which is returned; the other counts are the running game's
        public static short recalculate_map_counts(MapLevel level)
        {
            int count;

            // LP: fixed serious bug in the counting logic

            for (count = level.LightList.Count;
                    count > 0 && (!SLOT_IS_USED(level.LightList[count - 1].flags));
                    --count)
                ;
            return (short) count; // dynamic_world->light_count
        }

        public static bool line_has_variable_height(MapLevel level, short line_index)
        {
            line_data line = get_line_data(level, line_index);
            polygon_data polygon;

            if (line.clockwise_polygon_owner != NONE)
            {
                if (line.counterclockwise_polygon_owner != NONE)
                {
                    polygon = get_polygon_data(level, line.counterclockwise_polygon_owner);
                    if (polygon.type == _polygon_is_platform)
                    {
                        return true;
                    }
                }

                polygon = get_polygon_data(level, line.clockwise_polygon_owner);
                if (polygon.type == _polygon_is_platform)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
