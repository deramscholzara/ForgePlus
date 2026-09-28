// Port of Aleph One: Source_Files/CSeries/csmacros.h
//
// The macros that assign (SET_FLAG32, SET_FLAG16, SET_FLAG) return the new value instead.
using System.Collections.Generic;

namespace AlephOne
{
    public static class csmacros
    {
        public static int MAX(int a, int b) { return ((a) >= (b) ? (a) : (b)); }
        public static int MIN(int a, int b) { return ((a) <= (b) ? (a) : (b)); }

        // 1L<<(bit), unsigned so that FLAG(a)|FLAG(b) fits the uint flag fields
        public static uint FLAG(int bit) { return (1u << (bit)); }
        public static bool TEST_FLAG32(uint flags, int bit) { return (((flags) & FLAG(bit)) != 0); }
        public static uint SET_FLAG32(uint flags, int bit, bool value) { return ((value) ? ((flags) | FLAG(bit)) : ((flags) & ~FLAG(bit))); }

        public static int FLAG16(int bit) { return (1 << (bit)); }
        public static bool TEST_FLAG16(ushort flags, int bit) { return (((flags) & FLAG16(bit)) != 0); }
        public static ushort SET_FLAG16(ushort flags, int bit, bool value) { return (ushort) ((value) ? ((flags) | FLAG16(bit)) : ((flags) & ~FLAG16(bit))); }

        // LP addition (Mar 2, 2000): some more generic routines for flags
        public static bool TEST_FLAG(int obj, int flag) { return ((obj) & (flag)) != 0; }
        public static int SET_FLAG(int obj, int flag, bool value) { return ((value) ? ((obj) | (flag)) : ((obj) & ~(flag))); }
        public static uint SET_FLAG(uint obj, uint flag, bool value) { return ((value) ? ((obj) | (flag)) : ((obj) & ~(flag))); }

        /*
            LP addition: template class for doing bounds checking when accessing an array;
            it uses an array, an index value, and an intended number of members for that array.
            It will return a pointer to the array member, if that member is in range, or else
            the null pointer. Its caller must check whether a null pointer had been returned,
            and then perform the appropriate action.
        */
        public static T GetMemberWithBounds<T>(IList<T> Array, int Index, int Number) where T : class
        {
            // Index and Number are size_t
            return ((uint) Index < (uint) Number) ? Array[Index] : null;
        }
    }
}
