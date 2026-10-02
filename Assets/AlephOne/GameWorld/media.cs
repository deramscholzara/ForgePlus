// Port of Aleph One: Source_Files/GameWorld/media.h, media.cpp (map data)
//
// Not ported: the running game (update_medias, update_one_media, get_media_detonation_effect,
// get_media_damage, get_media_submerged_fade_effect, get_media_collection,
// IsMediaDangerous, media_in_environment), and MML parsing.
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csalerts;
using static AlephOne.csmacros;
using static AlephOne.Packing;

namespace AlephOne
{
    public class media_data /* 32 bytes */
    {
        public short type;
        public ushort flags;

        /* this light is not used as a real light; instead, the intensity of this light is used to
            determine the height of the media: height= low + (high-low)*intensity ... this sounds
            gross, but it makes media heights as flexible as light intensities; clearly discontinuous
            light functions (e.g., strobes) should not be used */
        public short light_index;

        /* this is the maximum external velocity due to current; acceleration is 1/32nd of this */
        public short current_direction; // angle
        public short current_magnitude; // world_distance

        public short low, high; // world_distance

        public world_point2d origin;
        public short height; // world_distance

        public int minimum_light_intensity; // _fixed
        public ushort texture; // shape_descriptor
        public short transfer_mode;

        public short[] unused = new short[2];

        public media_data Clone()
        {
            var copy = (media_data) MemberwiseClone();
            copy.unused = (short[]) unused.Clone();
            return copy;
        }
    }

    [NoAutoStaticsCleanup]
    public static partial class media
    {
        /* ---------- constants */

        // #define MAXIMUM_MEDIAS_PER_MAP 16

        // LP addition: added JjaroGoo support
        /* media types */
        public const short _media_water = 0;
        public const short _media_lava = 1;
        public const short _media_goo = 2;
        public const short _media_sewage = 3;
        public const short _media_jjaro = 4;
        public const short NUMBER_OF_MEDIA_TYPES = 5;

        /* media flags */
        public const int _media_sound_obstructed_by_floor = 0; // this media makes no sound when under the floor
        public const int NUMBER_OF_MEDIA_FLAGS = 1; /* <= 16 */

        public static bool MEDIA_SOUND_OBSTRUCTED_BY_FLOOR(media_data m) { return TEST_FLAG16((m).flags, _media_sound_obstructed_by_floor); }

        public static void SET_MEDIA_SOUND_OBSTRUCTED_BY_FLOOR(media_data m, bool v) { m.flags = SET_FLAG16((m).flags, _media_sound_obstructed_by_floor, (v)); }

        /* media detonation types */
        public const short _small_media_detonation_effect = 0;
        public const short _medium_media_detonation_effect = 1;
        public const short _large_media_detonation_effect = 2;
        public const short _large_media_emergence_effect = 3;
        public const short NUMBER_OF_MEDIA_DETONATION_TYPES = 4;

        /* media sounds */
        public const short _media_snd_feet_entering = 0;
        public const short _media_snd_feet_leaving = 1;
        public const short _media_snd_head_entering = 2;
        public const short _media_snd_head_leaving = 3;
        public const short _media_snd_splashing = 4;
        public const short _media_snd_ambient_over = 5;
        public const short _media_snd_ambient_under = 6;
        public const short _media_snd_platform_entering = 7;
        public const short _media_snd_platform_leaving = 8;
        public const short NUMBER_OF_MEDIA_SOUNDS = 9;

        /* ---------- macros */

        public static bool UNDER_MEDIA(media_data m, short z) { return ((z) <= (m).height); }

        public const int SIZEOF_media_data = 32;

        /* ---------- code */

        public static media_data get_media_data(MapLevel level, int media_index)
        {
            media_data media = GetMemberWithBounds(level.MediaList, media_index, level.MediaList.Count);

            if (media == null) return null;
            // if (!(SLOT_IS_USED(media))) return NULL;

            return media;
        }

        public static media_definition get_media_definition(short type)
        {
            return GetMemberWithBounds(media_definitions, type, NUMBER_OF_MEDIA_TYPES);
        }

        public static short get_media_sound(MapLevel level, short media_index, short type)
        {
            media_data media = get_media_data(level, media_index);
            // LP change: idiot-proofing
            if (media == null) return cstypes.NONE;

            media_definition definition = get_media_definition(media.type);
            if (definition == null) return cstypes.NONE;

            if (!(type >= 0 && type < NUMBER_OF_MEDIA_SOUNDS)) return cstypes.NONE;

            return definition.sounds[type];
        }

        // light_index must be loaded
        // ForgePlus: kept as loaded, so every medium is in use; marking the slot used and update_one_media()
        // would change what is saved
        public static int new_media(MapLevel level, media_data initializer)
        {
            level.MediaList.Add(initializer);
            return level.MediaList.Count - 1;
        }

        // LP addition: count number of media types used,
        // for better Infinity compatibility when saving games.
        public static int count_number_of_medias_used(MapLevel level)
        {
            return level.MediaList.Count;
        }

        public static void unpack_media_data(StreamPointer S, IList<media_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                media_data ObjPtr = Objects[k];
                StreamToValue(S, out ObjPtr.type);
                StreamToValue(S, out ObjPtr.flags);

                StreamToValue(S, out ObjPtr.light_index);

                StreamToValue(S, out ObjPtr.current_direction);
                StreamToValue(S, out ObjPtr.current_magnitude);

                StreamToValue(S, out ObjPtr.low);
                StreamToValue(S, out ObjPtr.high);

                StreamToValue(S, out ObjPtr.origin.x);
                StreamToValue(S, out ObjPtr.origin.y);
                StreamToValue(S, out ObjPtr.height);

                StreamToValue(S, out ObjPtr.minimum_light_intensity);
                StreamToValue(S, out ObjPtr.texture);
                StreamToValue(S, out ObjPtr.transfer_mode);

                S.Skip(2 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_media_data);
        }

        public static void pack_media_data(StreamPointer S, IList<media_data> Objects, int Count)
        {
            int Stream = S.Position;

            for (int k = 0; k < Count; k++)
            {
                media_data ObjPtr = Objects[k];
                ValueToStream(S, ObjPtr.type);
                ValueToStream(S, ObjPtr.flags);

                ValueToStream(S, ObjPtr.light_index);

                ValueToStream(S, ObjPtr.current_direction);
                ValueToStream(S, ObjPtr.current_magnitude);

                ValueToStream(S, ObjPtr.low);
                ValueToStream(S, ObjPtr.high);

                ValueToStream(S, ObjPtr.origin.x);
                ValueToStream(S, ObjPtr.origin.y);
                ValueToStream(S, ObjPtr.height);

                ValueToStream(S, ObjPtr.minimum_light_intensity);
                ValueToStream(S, ObjPtr.texture);
                ValueToStream(S, ObjPtr.transfer_mode);

                S.Skip(2 * 2);
            }

            assert((S.Position - Stream) == Count * SIZEOF_media_data);
        }
    }
}
