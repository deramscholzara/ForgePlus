using AlephOne;

namespace ForgePlus.Extensions
{
    public static class AlephOneExtensions
    {
        private const float DegreesPerAngleUnit = 360f / world.NUMBER_OF_ANGLES;

        public static string GetLevelName(this MapLevel level)
        {
            return csstrings.mac_roman_to_utf8(level.static_world.level_name);
        }

        public static string GetLevelName(this directory_data directory)
        {
            return csstrings.mac_roman_to_utf8(directory.level_name);
        }

        public static string GetText(this map_annotation annotation)
        {
            return csstrings.mac_roman_to_utf8(annotation.text);
        }

        public static string GetTypeName(this map_object mapObject)
        {
            switch (mapObject.type)
            {
                case map._saved_monster:
                    return "Monster";
                case map._saved_object:
                    return "Scenery";
                case map._saved_item:
                    return "Item";
                case map._saved_player:
                    return "Player";
                case map._saved_goal:
                    return "Goal";
                case map._saved_sound_source:
                    return "Sound";
                default:
                    return $"Unknown ({mapObject.type})";
            }
        }

        public static float AngleToDegrees(short angle)
        {
            return angle * DegreesPerAngleUnit;
        }

        public static float FixedToFloat(int value)
        {
            return value / (float) cstypes.FIXED_ONE;
        }

        public static bool IsEmptyShapeDescriptor(this ushort shapeDescriptor)
        {
            return shapeDescriptor == cstypes.UNONE;
        }

        public static int GetCollection(this ushort shapeDescriptor)
        {
            return shape_descriptors.GET_COLLECTION(shape_descriptors.GET_DESCRIPTOR_COLLECTION(shapeDescriptor));
        }

        public static int GetCLUT(this ushort shapeDescriptor)
        {
            return shape_descriptors.GET_COLLECTION_CLUT(shape_descriptors.GET_DESCRIPTOR_COLLECTION(shapeDescriptor));
        }

        // A low-level shape for walls, or a high-level shape for sprites
        public static int GetShape(this ushort shapeDescriptor)
        {
            return shape_descriptors.GET_DESCRIPTOR_SHAPE(shapeDescriptor);
        }

        public static ushort BuildShapeDescriptor(int collection, int shape, int clut = 0)
        {
            return shape_descriptors.BUILD_DESCRIPTOR(shape_descriptors.BUILD_COLLECTION(collection, clut), shape);
        }

        public static bool UsesLandscapeCollection(this ushort shapeDescriptor)
        {
            var collection = shapeDescriptor.GetCollection();
            return collection >= shape_descriptors._collection_landscape1 && collection <= shape_descriptors._collection_landscape4;
        }
    }
}
