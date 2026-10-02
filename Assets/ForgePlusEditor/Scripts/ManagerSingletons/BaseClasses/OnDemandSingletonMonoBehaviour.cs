using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[AutoStaticsCleanup]
public abstract partial class OnDemandSingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static GameObject singletonHolder;
    private static T instance;
    private static bool isQuitting;

    // AutoStaticsCleanup doesn't reset a generic class's statics, so the ones from an earlier Play session (its
    // destroyed instance, and its quitting) are reset
    private static int session;

    public static T Instance
    {
        get
        {
            if (session != PlaySession.Number)
            {
                session = PlaySession.Number;
                singletonHolder = null;
                instance = null;
                isQuitting = false;
            }

            // Don't create a new one while quitting (it would be left behind)
            if (isQuitting)
            {
                return instance;
            }

            if (!singletonHolder && Application.isPlaying)
            {
                singletonHolder = new GameObject("Singleton Holder");

                Application.quitting += OnApplicationQuitting;
            }

            if (!instance)
            {
                instance = singletonHolder.AddComponent<T>();
            }

            return instance;
        }
    }

    private static void OnApplicationQuitting()
    {
        Application.quitting -= OnApplicationQuitting;

        isQuitting = true;
    }
}
