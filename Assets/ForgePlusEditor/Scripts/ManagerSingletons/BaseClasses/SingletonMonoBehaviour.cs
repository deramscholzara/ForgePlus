using Unity.Scripting.LifecycleManagement;
using UnityEngine;

[AutoStaticsCleanup]
public abstract partial class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            return instance;
        }
    }

    protected virtual void Awake()
    {
        instance = this as T;
    }
}
