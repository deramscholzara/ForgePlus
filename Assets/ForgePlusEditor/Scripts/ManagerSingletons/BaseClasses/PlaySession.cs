using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// Which Play session this is (counting from 1), for statics that AutoStaticsCleanup doesn't reset: a generic class's
// (one set for each type argument), such as the on-demand singletons'. Fast Enter Play Mode doesn't reload the domain,
// so they'd otherwise carry one session's objects (destroyed, by then) into the next.
// Kept from session to session itself, so it never repeats.
[NoAutoStaticsCleanup]
public static class PlaySession
{
    public static int Number { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Begin()
    {
        Number++;
    }
}
