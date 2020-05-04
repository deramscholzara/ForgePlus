using UnityEngine;

public abstract class OnDemandSingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static GameObject singletonHolder;
    private static T instance;

    public static T Instance
    {
        get
        {
            if (!singletonHolder)
            {
                singletonHolder = new GameObject("Singleton Holder");
            }

            if (!instance)
            {
                instance = singletonHolder.AddComponent<T>();
            }

            return instance;
        }
    }
}
