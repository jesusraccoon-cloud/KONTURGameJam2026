using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour
    where T : class
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject); // Дубликат (например при возврате в меню) — убираем
            return;
        }

        Instance = this as T;

        // DontDestroyOnLoad работает ТОЛЬКО на корневых объектах. Если синглтон вложен
        // (напр. Services под [ Management ]), открепляем его к корню — иначе он уничтожится при смене сцены.
        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(this.gameObject);
    }

    protected virtual void OnDestroy()
    {
        Instance = null;
    }
}
