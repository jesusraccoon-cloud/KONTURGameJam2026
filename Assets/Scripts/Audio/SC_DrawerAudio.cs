using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)

// Играет FMOD-события при открытии и закрытии ящика DrawerInteract.
// Следит за состоянием IsOpen и срабатывает на переходах.
public class SC_DrawerAudio : MonoBehaviour
{
    [Header("References")] // Блок ссылок
    public DrawerInteract drawer; // Ящик, за состоянием которого следим

    [Header("FMOD")] // Блок FMOD
    public EventReference openEvent; // Событие открытия

    public EventReference closeEvent; // Событие закрытия

    public bool attachToDrawer = true; // Звук из позиции ящика и следует за ним

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private bool wasOpen; // Было ли открыто в прошлом кадре

    private void Awake() // При создании
    {
        if (drawer == null) // Если ящик не назначен
        {
            drawer = GetComponent<DrawerInteract>(); // Пробуем на этом же объекте

            if (drawer == null) drawer = GetComponentInParent<DrawerInteract>(); // Иначе выше по иерархии
        }
    }

    private void Start() // Перед первым кадром
    {
        if (drawer != null) // Если ящик найден
        {
            wasOpen = drawer.IsOpen; // Запоминаем стартовое состояние, чтобы не сыграть звук на старте
        }
        else if (showDebugLogs) // Если ящика нет
        {
            Debug.LogWarning(gameObject.name + ": SC_DrawerAudio не нашёл DrawerInteract"); // Предупреждение
        }
    }

    private void Update() // Каждый кадр
    {
        if (drawer == null) return; // Без ящика работать не с чем

        bool open = drawer.IsOpen; // Текущее состояние ящика

        if (open == wasOpen) return; // Состояние не менялось — выходим

        if (open) // Ящик только что открылся
        {
            PlayEvent(openEvent, "открытие"); // Играем открытие
        }
        else // Ящик только что закрылся
        {
            PlayEvent(closeEvent, "закрытие"); // Играем закрытие
        }

        wasOpen = open; // Запоминаем новое состояние
    }

    private void PlayEvent(EventReference e, string label) // Проиграть событие ящика
    {
        if (e.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning(gameObject.name + ": не назначено событие ящика (" + label + ")"); // Предупреждение
            return; // Выходим
        }

        if (attachToDrawer) // Если звук привязан к ящику
        {
            RuntimeManager.PlayOneShotAttached(e, drawer.gameObject); // Звук из позиции ящика, следует за ним
        }
        else // Иначе разово в точке ящика
        {
            RuntimeManager.PlayOneShot(e, drawer.transform.position); // Звук в позиции ящика
        }

        if (showDebugLogs) Debug.Log(gameObject.name + ": ящик — " + label); // Лог
    }
}
