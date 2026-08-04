using UnityEngine; // Unity-классы
using UnityEngine.EventSystems; // IPointerEnterHandler и т.п.
using UnityEngine.UI; // Selectable (Button/Toggle/Slider)
using FMODUnity; // RuntimeManager, EventReference

// Звук кнопки UI: hover (наведение мышью или выбор геймпадом) и click.
// Играет 2D one-shot'ы. Работает вместе со стандартным Button (оба обработчика вызываются).
// Чтобы громкостью рулил слайдер UI — назначь эти события на vca:/UI в FMOD.
public class SC_UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler
{
    [Header("FMOD")]
    public EventReference hoverEvent; // Звук наведения

    public EventReference clickEvent; // Звук нажатия

    [Tooltip("Играть только если элемент интерактивный (не выключен).")]
    public bool onlyIfInteractable = true; // Не звенеть на выключенных кнопках

    private Selectable selectable; // Кнопка/тоггл (для проверки interactable)

    private void Awake()
    {
        selectable = GetComponent<Selectable>(); // Может быть null — тогда считаем всегда активным
    }

    public void OnPointerEnter(PointerEventData eventData) => PlayHover(); // Мышь навелась

    public void OnSelect(BaseEventData eventData) => PlayHover(); // Выбор геймпадом/клавиатурой

    public void OnPointerClick(PointerEventData eventData) => PlayClick(); // Клик

    private bool Interactable => !onlyIfInteractable || selectable == null || selectable.interactable; // Активен ли элемент

    private void PlayHover()
    {
        if (Interactable && !hoverEvent.IsNull) RuntimeManager.PlayOneShot(hoverEvent); // 2D-звук наведения
    }

    private void PlayClick()
    {
        if (Interactable && !clickEvent.IsNull) RuntimeManager.PlayOneShot(clickEvent); // 2D-звук нажатия
    }
}
