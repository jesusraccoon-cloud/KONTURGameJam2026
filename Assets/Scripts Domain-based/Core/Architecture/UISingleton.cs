using UnityEngine.EventSystems;

public class UISingleton<T> : UIBehaviour
    where T : class
{
    public static T Instance { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        Instance = this as T;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Instance = null;
    }
}