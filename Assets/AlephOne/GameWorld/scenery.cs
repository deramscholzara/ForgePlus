// Port of Aleph One: Source_Files/GameWorld/scenery.h, scenery.cpp (scenery definitions)
//
// Not ported: everything that places, animates or damages scenery in a game, and MML parsing of
// scenery.
using static AlephOne.csmacros;
using static AlephOne.shape_descriptors;

namespace AlephOne
{
    public static partial class scenery
    {
        /* ---------- constants */

        public const int MAXIMUM_ANIMATED_SCENERY_OBJECTS = 20;

        /* ---------- code */

        public static void get_scenery_dimensions(
            short scenery_type,
            out short radius,
            out short height)
        {
            scenery_definition definition = get_scenery_definition(scenery_type);
            if (definition == null)
            {
                // Fallback
                radius = 0;
                height = 0;
                return;
            }

            radius = definition.radius;
            height = definition.height;
        }

        public static bool get_scenery_collection(short scenery_type, ref short collection)
        {
            scenery_definition definition = get_scenery_definition(scenery_type);
            if (definition == null) return false;

            collection = (short) GET_DESCRIPTOR_COLLECTION(definition.shape);
            return true;
        }

        public static bool get_damaged_scenery_collection(short scenery_type, ref short collection)
        {
            scenery_definition definition = get_scenery_definition(scenery_type);
            if (definition == null || (definition.flags & _scenery_can_be_destroyed) == 0)
                return false;

            collection = (short) GET_DESCRIPTOR_COLLECTION(definition.destroyed_shape);
            return true;
        }

        /* ---------- private code */

        public static scenery_definition get_scenery_definition(
            short scenery_type)
        {
            return GetMemberWithBounds(scenery_definitions, scenery_type, NUMBER_OF_SCENERY_DEFINITIONS);
        }
    }
}
