using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour
    where T : class
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this as T;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    protected virtual void OnDestroy()
    {
        Instance = null;
    }
}
