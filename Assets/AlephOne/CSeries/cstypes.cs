// Port of Aleph One: Source_Files/CSeries/cstypes.h
namespace AlephOne
{
    public static class cstypes
    {
        public const short NONE = -1;
        public const ushort UNONE = 65535;

        public const short INT16_MAX = 32767;
        public const ushort UINT16_MAX = 65535;
        public const short INT16_MIN = (-INT16_MAX - 1);
        public const int INT32_MAX = 2147483647;
        public const int INT32_MIN = (-INT32_MAX - 1);

        // Fixed point (16.16) type: typedef int32 _fixed;
        public const int FIXED_FRACTIONAL_BITS = 16;
        public static int INTEGER_TO_FIXED(int i) { return i << FIXED_FRACTIONAL_BITS; }
        public static int FIXED_INTEGERAL_PART(int f) { return f >> FIXED_FRACTIONAL_BITS; }
        public const int FIXED_ONE = (1 << FIXED_FRACTIONAL_BITS);
        public const int FIXED_ONE_HALF = (1 << (FIXED_FRACTIONAL_BITS - 1));

        // Binary powers
        public const int MEG = 0x100000;
        public const int KILO = 0x400;

        // Construct four-character-code
        public static uint FOUR_CHARS_TO_INT(char a, char b, char c, char d)
        {
            return ((uint) a << 24) | ((uint) b << 16) | ((uint) c << 8) | (uint) d;
        }
    }
}
