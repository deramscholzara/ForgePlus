using AlephOne;
using ForgePlus.Extensions;

namespace RuntimeCore.Entities.MapObjects
{
    // The shapes Marathon 1's player, items and scenery use, as Aleph One's Marathon 1 scenario gives them (its
    // Scripts/Marathon.mml: <player>, <items> and <scenery>), as Marathon 1's shapes file has them in other places than
    // Marathon 2's does. Monsters' shapes aren't here, as physics (Marathon 1's, or any override) gives them.
    public static class MarathonOneSpriteDefinitions
    {
        private const byte PlayerCollection = 6;
        private const short StationaryLegs = 4;
        private const short PistolTorso = 5;

        private const byte ItemsCollection = 7;

        // Each item type's high-level shape (none for those with no shape)
        private static readonly short[] itemShapes = { cstypes.NONE, 0, 3, 1, 4, 2, 5, 6, 12, 7, 8, 9, 14, 13, cstypes.NONE, 10, 11, 15, 16, 20 };

        // Each scenery type's collection and high-level shape
        private static readonly (byte Collection, short Shape)[] sceneryShapes =
        {
            (23, 0), (23, 1), (23, 2), (23, 3), (23, 4), (23, 5), (23, 6), (23, 7), (23, 8),
            (12, 2),
            (25, 0), (25, 1), (25, 2), (25, 3), (25, 4), (25, 5), (25, 6),
        };

        // Whether Marathon 1's shapes are loaded, so these are the shapes in use
        public static bool IsInUse
        {
            get
            {
                return shapes.shapes_file_is_m1();
            }
        }

        public static SpriteDefinition GetPlayer(int teamCLUT)
        {
            return new SpriteDefinition(
                1f,
                new SpriteLayer(PlayerCollection, teamCLUT, StationaryLegs),
                new SpriteLayer(PlayerCollection, teamCLUT, PistolTorso));
        }

        // False for item types Marathon 1 doesn't have
        public static bool TryGetItem(short type, out ushort shapeDescriptor)
        {
            shapeDescriptor = cstypes.UNONE;
            if (type < 0 || type >= itemShapes.Length)
            {
                return false;
            }

            if (itemShapes[type] != cstypes.NONE)
            {
                shapeDescriptor = AlephOneExtensions.BuildShapeDescriptor(ItemsCollection, itemShapes[type], 0);
            }

            return true;
        }

        // False for scenery types Marathon 1 doesn't have
        public static bool TryGetScenery(short type, out ushort shapeDescriptor)
        {
            shapeDescriptor = cstypes.UNONE;
            if (type < 0 || type >= sceneryShapes.Length)
            {
                return false;
            }

            shapeDescriptor = AlephOneExtensions.BuildShapeDescriptor(sceneryShapes[type].Collection, sceneryShapes[type].Shape, 0);

            return true;
        }
    }
}
