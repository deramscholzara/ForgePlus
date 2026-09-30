// Port of Aleph One: Source_Files/RenderOther/screen.cpp (bit_depth and interface_bit_depth, from screen_shared.h)
//
// The screen's bit depth selects a collection's 8-bit or 16-bit data in shapes.load_collection(), and the
// interface's selects a picture's 8-bit, 16-bit or 32-bit version in images.determine_pict_resource_id().
using Unity.Scripting.LifecycleManagement;
using static AlephOne.cstypes;

namespace AlephOne
{
    [NoAutoStaticsCleanup]
    public static class screen
    {
        public static short bit_depth = NONE;
        public static short interface_bit_depth = NONE;
    }
}
