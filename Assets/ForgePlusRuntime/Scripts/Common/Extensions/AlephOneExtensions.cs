using AlephOne;
using System;
using System.Text;

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

        public static void SetText(this map_annotation annotation, string text)
        {
            annotation.text.SetMacRomanText(text);
        }

        // Leaves room for the NUL that Aleph One finds the text's end by
        public static int MacRomanTextCapacity(this byte[] buffer)
        {
            return buffer.Length - 1;
        }

        // Mac Roman has it, and it isn't a control character (which Aleph One drops when drawing)
        public static bool IsDrawableMacRomanCharacter(char character)
        {
            if (character < ' ' || character == '\u007f')
            {
                return false;
            }

            return character < '\u007f' || csstrings.unicode_to_mac_roman(character) != (byte) '?';
        }

        public static string ToDrawableMacRomanText(this string text, int maximumLength)
        {
            var drawable = new StringBuilder(text.Length);
            foreach (var character in text)
            {
                if (drawable.Length == maximumLength)
                {
                    break;
                }

                if (IsDrawableMacRomanCharacter(character))
                {
                    drawable.Append(character);
                }
            }

            return drawable.ToString();
        }

        // NUL-terminated, with the rest of the buffer zeroed
        public static void SetMacRomanText(this byte[] buffer, string text)
        {
            var bytes = csstrings.utf8_to_mac_roman(text.ToDrawableMacRomanText(buffer.MacRomanTextCapacity()));

            Array.Clear(buffer, 0, buffer.Length);
            Array.Copy(bytes, buffer, bytes.Length);
        }

        // For naming objects and logging (the inspector shows a localized one)
        public static string GetTypeIdentifier(this map_object mapObject)
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

        public static short DegreesToAngle(float degrees)
        {
            return world.NORMALIZE_ANGLE((int) Math.Round(degrees / 360f * world.NUMBER_OF_ANGLES));
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

        // Aleph One's OpenGL renderer draws big landscapes as landscapes too
        public static bool IsLandscapeTransferMode(short transferMode)
        {
            return transferMode == map._xfer_landscape || transferMode == map._xfer_big_landscape;
        }

        public static bool UsesLandscapeCollection(this ushort shapeDescriptor)
        {
            var collection = shapeDescriptor.GetCollection();
            return collection >= shape_descriptors._collection_landscape1 && collection <= shape_descriptors._collection_landscape4;
        }
    }
}
