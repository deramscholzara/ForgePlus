using Unity.Scripting.LifecycleManagement;
using UnityEngine;

// Counts Play sessions (never reset itself), so statics AutoStaticsCleanup doesn't reset (a generic class's) can tell
// they're from an earlier session, since Fast Enter Play Mode doesn't reload the domain
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
