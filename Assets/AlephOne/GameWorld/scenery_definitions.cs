// Port of Aleph One: Source_Files/GameWorld/scenery_definitions.h
using static AlephOne.cstypes;
using static AlephOne.effects;
using static AlephOne.shape_descriptors;
using static AlephOne.world;

namespace AlephOne
{
    /* ---------- structures */

    public class scenery_definition
    {
        public ushort flags;
        public ushort shape;

        public short radius, height;

        public short destroyed_effect;
        public ushort destroyed_shape;
    }

    public static partial class scenery
    {
        /* ---------- constants */

        // enum
        public const short _scenery_is_solid = 0x0001;
        public const short _scenery_is_animated = 0x0002; // ghs: unused; sequence is how to make scenery animated
        public const short _scenery_can_be_destroyed = 0x0004;

        /* ---------- globals */

        // #define NUMBER_OF_SCENERY_DEFINITIONS (sizeof(scenery_definitions)/sizeof(struct scenery_definition))
        public const short NUMBER_OF_SCENERY_DEFINITIONS = 61;

        public static readonly scenery_definition[] scenery_definitions = new scenery_definition[NUMBER_OF_SCENERY_DEFINITIONS]
        {
            // lava
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery2, 3)}, // light dirt
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery2, 4)}, // dark dirt
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery2, 5)}, // bones
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery2, 6)}, // bone
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery2, 7)}, // ribs
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery2, 8)}, // skull
            new scenery_definition {flags = _scenery_is_solid|_scenery_can_be_destroyed, shape = BUILD_DESCRIPTOR(_collection_scenery2, 9), radius = WORLD_ONE/8, height = -WORLD_ONE/8, destroyed_effect = _effect_lava_lamp_breaking, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery2, 19)}, // hanging light
            new scenery_definition {flags = _scenery_is_solid|_scenery_can_be_destroyed, shape = BUILD_DESCRIPTOR(_collection_scenery2, 10), radius = WORLD_ONE/8, height = -WORLD_ONE/8, destroyed_effect = _effect_lava_lamp_breaking, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery2, 20)}, // hanging light
            new scenery_definition {flags = (ushort) _scenery_is_solid, shape = BUILD_DESCRIPTOR(_collection_scenery2, 12), radius = WORLD_ONE/8, height = WORLD_ONE_HALF}, // small cylinder
            new scenery_definition {flags = (ushort) _scenery_is_solid, shape = BUILD_DESCRIPTOR(_collection_scenery2, 13), radius = WORLD_ONE_FOURTH, height = WORLD_ONE_HALF}, // large cylinder
            new scenery_definition {flags = (ushort) _scenery_is_solid, shape = BUILD_DESCRIPTOR(_collection_scenery2, 14), radius = WORLD_ONE_FOURTH, height = WORLD_ONE_HALF}, // block
            new scenery_definition {flags = (ushort) _scenery_is_solid, shape = BUILD_DESCRIPTOR(_collection_scenery2, 15), radius = WORLD_ONE_FOURTH, height = WORLD_ONE_HALF}, // block
            new scenery_definition {flags = (ushort) _scenery_is_solid, shape = BUILD_DESCRIPTOR(_collection_scenery2, 16), radius = WORLD_ONE_FOURTH, height = WORLD_ONE_HALF}, // block

            // water
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery1, 4)}, // pistol clip
            new scenery_definition {flags = _scenery_is_solid|_scenery_can_be_destroyed, shape = BUILD_DESCRIPTOR(_collection_scenery1, 5), radius = WORLD_ONE/6, height = -WORLD_ONE/8, destroyed_effect = _effect_water_lamp_breaking, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery1, 6)}, // short light
            new scenery_definition {flags = _scenery_is_solid|_scenery_can_be_destroyed, shape = BUILD_DESCRIPTOR(_collection_scenery1, 7), radius = WORLD_ONE/8, height = -WORLD_ONE/8, destroyed_effect = _effect_water_lamp_breaking, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery1, 8)}, // long light
            new scenery_definition {flags = _scenery_is_solid|_scenery_can_be_destroyed, shape = BUILD_DESCRIPTOR(_collection_scenery1, 9), radius = WORLD_ONE/4, height = -WORLD_ONE/6, destroyed_effect = _effect_grenade_explosion, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery1, 23)}, // siren
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery1, 10)}, // rocks
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery1, 21)}, // blood drops
            new scenery_definition {flags = (ushort) _scenery_is_animated, shape = BUILD_DESCRIPTOR(_collection_scenery1, 11)}, // water thing
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery1, 12)}, // gun
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery1, 13)}, // bob remains
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery1, 14)}, // puddles
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery1, 15)}, // big puddles
            new scenery_definition {flags = (ushort) _scenery_is_solid, shape = BUILD_DESCRIPTOR(_collection_scenery1, 16), radius = WORLD_ONE_FOURTH, height = WORLD_ONE_HALF}, // security monitor
            new scenery_definition {flags = (ushort) _scenery_is_solid, shape = BUILD_DESCRIPTOR(_collection_scenery1, 17), radius = WORLD_ONE_FOURTH, height = WORLD_ONE_HALF}, // alien supply can
            new scenery_definition {flags = (ushort) _scenery_is_animated, shape = BUILD_DESCRIPTOR(_collection_scenery1, 18)}, // machine
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery1, 20)}, // fighter’s staff

            // sewage
            new scenery_definition {flags = _scenery_is_solid|_scenery_can_be_destroyed, shape = BUILD_DESCRIPTOR(_collection_scenery3, 5), radius = WORLD_ONE/6, height = -WORLD_ONE/8, destroyed_effect = _effect_sewage_lamp_breaking, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery3, 6)}, // stubby green light
            new scenery_definition {flags = _scenery_is_solid|_scenery_can_be_destroyed, shape = BUILD_DESCRIPTOR(_collection_scenery3, 7), radius = WORLD_ONE/6, height = -WORLD_ONE/8, destroyed_effect = _effect_sewage_lamp_breaking, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery3, 8)}, // long green light
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery3, 4)}, // junk
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery3, 9)}, // big antenna
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery3, 10)}, // big antenna
            new scenery_definition {flags = (ushort) _scenery_is_solid, shape = BUILD_DESCRIPTOR(_collection_scenery3, 11), radius = WORLD_ONE_FOURTH, height = WORLD_ONE_HALF}, // alien supply can
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery3, 13)}, // bones
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery3, 17)}, // big bones
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery3, 12)}, // pfhor pieces
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery3, 14)}, // bob pieces
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery3, 15)}, // bob blood

            // alien
            new scenery_definition {flags = _scenery_is_solid|_scenery_can_be_destroyed, shape = BUILD_DESCRIPTOR(_collection_scenery5, 4), radius = WORLD_ONE/6, height = -WORLD_ONE/8, destroyed_effect = _effect_alien_lamp_breaking, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery5, 5)}, // green light
            new scenery_definition {flags = _scenery_is_solid|_scenery_can_be_destroyed, shape = BUILD_DESCRIPTOR(_collection_scenery5, 14), radius = WORLD_ONE/6, height = -WORLD_ONE/8, destroyed_effect = _effect_alien_lamp_breaking, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery5, 15)}, // small alien light
            new scenery_definition {flags = _scenery_is_solid|_scenery_can_be_destroyed, shape = BUILD_DESCRIPTOR(_collection_scenery5, 16), radius = WORLD_ONE/6, height = -WORLD_ONE/8, destroyed_effect = _effect_alien_lamp_breaking, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery5, 17)}, // alien ceiling rod light
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery5, 6)}, // bulbous yellow alien object
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery5, 7)}, // square grey organic object
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery5, 9)}, // pfhor skeleton
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery5, 10)}, // pfhor mask
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery5, 11)}, // green stuff
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery5, 12)}, // hunter shield
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery5, 13)}, // bones
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery5, 18)}, // alien sludge

            // jjaro (should lamps be made destroyable?)
            new scenery_definition {flags = (ushort) _scenery_is_solid, shape = BUILD_DESCRIPTOR(_collection_scenery4, 5), radius = WORLD_ONE/6, height = -WORLD_ONE/8, destroyed_effect = NONE, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery4, 6)}, // short ceiling light
            new scenery_definition {flags = (ushort) _scenery_is_solid, shape = BUILD_DESCRIPTOR(_collection_scenery4, 7), radius = WORLD_ONE/8, height = WORLD_ONE, destroyed_effect = NONE, destroyed_shape = BUILD_DESCRIPTOR(_collection_scenery4, 8)}, // long light
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery4, 4)}, // weird rod
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery4, 9)}, // pfhor ship
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery4, 10)}, // sun
            new scenery_definition {flags = (ushort) _scenery_is_solid, shape = BUILD_DESCRIPTOR(_collection_scenery4, 11), radius = WORLD_ONE_FOURTH, height = WORLD_ONE_HALF}, // large glass container
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery4, 13)}, // nub 1
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery4, 17)}, // nub 2
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery4, 12)}, // lh'owon
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery4, 14)}, // floor whip antenna
            new scenery_definition {flags = 0, shape = BUILD_DESCRIPTOR(_collection_scenery4, 15)}, // ceiling whip antenna
        };
    }
}
