using Unity.Scripting.LifecycleManagement;

[AutoStaticsCleanup]
public abstract partial class OnDemandSingleton<T> where T : class, new()
{
    private static T instance;

    // AutoStaticsCleanup doesn't reset a generic class's statics, so one made in an earlier Play session is replaced
    private static int instanceSession;

    public static T Instance
    {
        get
        {
            if (instance == null || instanceSession != PlaySession.Number)
            {
                instance = new T();
                instanceSession = PlaySession.Number;
            }

            return instance;
        }
    }
}
