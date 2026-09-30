// ForgePlus: one loaded level, holding the globals Aleph One keeps the level in
using System.Collections.Generic;

namespace AlephOne
{
    public class MapLevel
    {
        /* ---------- map.h / map.cpp */

        public static_data static_world = new static_data();

        public List<endpoint_data> EndpointList = new List<endpoint_data>();
        public List<line_data> LineList = new List<line_data>();
        public List<side_data> SideList = new List<side_data>();
        public List<polygon_data> PolygonList = new List<polygon_data>();

        public List<ambient_sound_image_data> AmbientSoundImageList = new List<ambient_sound_image_data>();
        public List<random_sound_image_data> RandomSoundImageList = new List<random_sound_image_data>();

        public List<short> MapIndexList = new List<short>();

        public List<map_annotation> MapAnnotationList = new List<map_annotation>();
        public List<map_object> SavedObjectList = new List<map_object>();

        /* ---------- lightsource.h / lightsource.cpp */

        public List<light_data> LightList = new List<light_data>();

        /* ---------- media.h / media.cpp */

        public List<media_data> MediaList = new List<media_data>();

        /* ---------- platforms.h / platforms.cpp */

        public List<platform_data> PlatformList = new List<platform_data>();

        /* ---------- game_wad.cpp */

        // keep these around for level export
        public List<static_platform_data> static_platforms = new List<static_platform_data>();

        /* ---------- placement.cpp */

        /* This is done in a single array to facilitate the saving of the game state. */
        // [0, MAXIMUM_OBJECT_TYPES) is item_placement_info, [MAXIMUM_OBJECT_TYPES, 2*MAXIMUM_OBJECT_TYPES) is monster_placement_info
        public object_frequency_definition[] object_placement_info = placement.new_object_placement_info();

        /* ---------- computer_interface.cpp */

        public List<terminal_text_t> map_terminal_text = new List<terminal_text_t>();

        /* ---------- ForgePlus */

        public LoadedWad loaded_wad = new LoadedWad();
    }

    // ForgePlus: what save_level needs to write a level back without losing anything
    public class LoadedWad
    {
        // header.data_version of the file the level was loaded from
        public short data_version = editor.MARATHON_INFINITY_DATA_VERSION;

        // the tags of the level's chunks, in the order they were read
        public List<uint> chunk_order = new List<uint>();

        // the chunks MapLevel doesn't model, byte for byte (including those Aleph One ignores, such as
        // 'EPNT' when 'PNTS' is present)
        public Dictionary<uint, byte[]> preserved_chunks = new Dictionary<uint, byte[]>();

        // the chunks MapLevel models, as loaded, and as MapLevel packed them right after loading
        public HashSet<uint> modeled_chunks = new HashSet<uint>();
        public Dictionary<uint, byte[]> chunk_bytes = new Dictionary<uint, byte[]>();
        public Dictionary<uint, byte[]> chunk_packing = new Dictionary<uint, byte[]>();

        // the level's directory data (SIZEOF_directory_data bytes), or null if the file had none
        public byte[] directory_data;
    }
}
