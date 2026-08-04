using UnityEngine; // Unity-классы
using UnityEngine.UI; // Button
using FMODUnity; // EventReference

// Автоматически вешает SC_UIButtonSound на ВСЕ кнопки внутри Root (по умолчанию — этот объект)
// и задаёт им общие звуки hover/click. Положи один на Canvas/меню — и все кнопки зазвучат.
public class SC_UIButtonSoundInstaller : MonoBehaviour
{
    [Header("FMOD")]
    public EventReference hoverEvent; // Общий звук наведения

    public EventReference clickEvent; // Общий звук нажатия

    [Header("Scope")]
    [Tooltip("Корень, внутри которого искать кнопки. Пусто — этот объект.")]
    [SerializeField] private Transform root; // Где искать кнопки

    [Tooltip("Включая выключенные объекты (скрытые панели).")]
    public bool includeInactive = true; // Ловить и скрытые кнопки

    private void Start()
    {
        Install(); // Навешиваем звуки при старте
    }

    // Можно вызвать вручную (например после создания новых кнопок).
    public void Install()
    {
        Transform r = root != null ? root : transform; // Область поиска

        Button[] buttons = r.GetComponentsInChildren<Button>(includeInactive); // Все кнопки

        foreach (Button b in buttons)
        {
            SC_UIButtonSound s = b.GetComponent<SC_UIButtonSound>(); // Уже есть свой звук?

            if (s == null) // Нет — добавляем и настраиваем
            {
                s = b.gameObject.AddComponent<SC_UIButtonSound>();
                s.hoverEvent = hoverEvent;
                s.clickEvent = clickEvent;
            }
            // Если уже есть (настроен вручную) — не трогаем, уважаем индивидуальные звуки
        }
    }
}
