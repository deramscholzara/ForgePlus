// Port of Aleph One: Source_Files/RenderOther/screen.cpp (bit_depth, from screen_shared.h)
//
// The screen's bit depth selects a collection's 8-bit or 16-bit data in shapes.load_collection().
using static AlephOne.cstypes;

namespace AlephOne
{
    public static class screen
    {
        public static short bit_depth = NONE;
    }
}
