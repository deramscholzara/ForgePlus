using Unity.Scripting.LifecycleManagement;

[AutoStaticsCleanup]
public abstract partial class OnDemandSingleton<T> where T : class, new()
{
    private static T instance;

    // Generic statics aren't reset between Play sessions (see PlaySession)
    private static int session;

    public static T Instance
    {
        get
        {
            if (instance == null || session != PlaySession.Number)
            {
                instance = new T();
                session = PlaySession.Number;
            }

            return instance;
        }
    }
}
