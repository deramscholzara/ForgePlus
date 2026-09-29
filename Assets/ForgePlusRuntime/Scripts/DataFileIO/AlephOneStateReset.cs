using AlephOne;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    // Aleph One keeps its loaded shapes and last error in global state, which Fast Enter Play Mode
    // would otherwise carry from one Play session to the next
    public static class AlephOneStateReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            shapes.unload_all_collections();
            shapes.close_shapes_file();
            screen.bit_depth = cstypes.NONE;

            game_errors.clear_game_error();
        }
    }
}
