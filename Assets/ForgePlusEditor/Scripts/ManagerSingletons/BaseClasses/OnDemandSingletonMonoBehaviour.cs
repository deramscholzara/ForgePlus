using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[AutoStaticsCleanup]
public abstract partial class OnDemandSingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static GameObject singletonHolder;
    private static T instance;
    private static bool isQuitting;

    public static T Instance
    {
        get
        {
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
