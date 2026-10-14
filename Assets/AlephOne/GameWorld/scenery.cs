// Port of Aleph One: Source_Files/GameWorld/scenery.h, scenery.cpp (scenery definitions)
//
// Not ported: everything that places, animates or damages scenery in a game.
using Unity.Scripting.LifecycleManagement;
using static AlephOne.csmacros;
using static AlephOne.shape_descriptors;

namespace AlephOne
{
    [NoAutoStaticsCleanup]
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

        // The definitions as they were before MML changed them (null while MML hasn't)
        private static scenery_definition[] original_scenery_definitions;

        // ForgePlus: whether MML has changed the definitions since they were last reset
        public static bool scenery_definitions_changed_by_mml()
        {
            return original_scenery_definitions != null;
        }

        public static void reset_mml_scenery()
        {
            if (original_scenery_definitions != null)
            {
                for (int i = 0; i < NUMBER_OF_SCENERY_DEFINITIONS; i++)
                    scenery_definitions[i] = original_scenery_definitions[i];
                original_scenery_definitions = null;
            }
        }

        public static void parse_mml_scenery(InfoTree root)
        {
            // back up old values first
            if (original_scenery_definitions == null)
            {
                original_scenery_definitions = new scenery_definition[NUMBER_OF_SCENERY_DEFINITIONS];
                for (int i = 0; i < NUMBER_OF_SCENERY_DEFINITIONS; i++)
                {
                    original_scenery_definitions[i] = scenery_definitions[i];
                    scenery_definitions[i] = scenery_definitions[i].Clone();
                }
            }

            foreach (var @object in root.children_named("object"))
            {
                short index = 0;
                if (!@object.read_indexed("index", ref index, NUMBER_OF_SCENERY_DEFINITIONS))
                    continue;
                scenery_definition def = scenery_definitions[index];

                @object.read_attr("flags", ref def.flags);
                @object.read_attr("radius", ref def.radius);
                @object.read_attr("height", ref def.height);
                @object.read_indexed("destruction", ref def.destroyed_effect, effects.NUMBER_OF_EFFECT_TYPES, true);

                foreach (var child in @object.children_named("normal"))
                    foreach (var shape in child.children_named("shape"))
                        shape.read_shape(ref def.shape);
                foreach (var child in @object.children_named("destroyed"))
                    foreach (var shape in child.children_named("shape"))
                        shape.read_shape(ref def.destroyed_shape);
            }

            // Not ported: reset_scenery_solidity()
        }
    }
}
