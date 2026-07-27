using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager, RuntimeUtils)
using FMOD.Studio; // Подключаем EventInstance

// Играет FMOD-события при открытии и закрытии двери UniversalDoor.
// Следит за её состоянием IsOpen и срабатывает на переходах — работает
// независимо от того, кто открыл дверь (игрок, монстр, скрипт).
public class SC_DoorAudio : MonoBehaviour
{
    [Header("References")] // Блок ссылок
    public UniversalDoor door; // Дверь, за состоянием которой следим

    [Header("FMOD")] // Блок FMOD
    public EventReference openEvent; // Событие открытия

    public EventReference closeEvent; // Событие закрытия

    public bool attachToDoor = true; // Звук из позиции двери и следует за ней (для качающейся створки)

    [Header("Occlusion")] // Блок окклюзии
    public bool occludeDoorSounds = true; // Приглушать скрип, если дверь за стеной от игрока (замер в момент проигрывания)

    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в событиях двери

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private bool wasOpen; // Было ли открыто в прошлом кадре

    private void Awake() // Вызывается при создании объекта
    {
        if (door == null) // Если дверь не назначена
        {
            door = GetComponent<UniversalDoor>(); // Пробуем на этом же объекте

            if (door == null) door = GetComponentInParent<UniversalDoor>(); // Иначе выше по иерархии
        }
    }

    private void Start() // Вызывается перед первым кадром
    {
        if (door != null) // Если дверь найдена
        {
            wasOpen = door.IsOpen; // Запоминаем стартовое состояние, чтобы не сыграть звук на старте
        }
        else if (showDebugLogs) // Если двери нет
        {
            Debug.LogWarning(gameObject.name + ": SC_DoorAudio не нашёл UniversalDoor"); // Предупреждение
        }
    }

    private void Update() // Вызывается каждый кадр
    {
        if (door == null) return; // Без двери работать не с чем

        bool open = door.IsOpen; // Текущее состояние двери

        if (open == wasOpen) return; // Состояние не менялось — выходим

        if (open) // Дверь только что открылась
        {
            PlayEvent(openEvent, "открытие"); // Играем открытие
        }
        else // Дверь только что закрылась
        {
            PlayEvent(closeEvent, "закрытие"); // Играем закрытие
        }

        wasOpen = open; // Запоминаем новое состояние
    }

    private void PlayEvent(EventReference e, string label) // Проиграть событие двери
    {
        if (e.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning(gameObject.name + ": не назначено событие двери (" + label + ")"); // Предупреждение
            return; // Выходим
        }

        EventInstance inst = RuntimeManager.CreateInstance(e); // Создаём экземпляр (нужен, чтобы задать окклюзию до старта)

        Vector3 pos = door.transform.position; // Позиция двери

        if (attachToDoor) // Если звук привязан к двери
        {
            RuntimeManager.AttachInstanceToGameObject(inst, door.gameObject); // Звук из позиции двери, следует за створкой
        }
        else // Иначе разово в точке двери
        {
            inst.set3DAttributes(RuntimeUtils.To3DAttributes(pos)); // Звук в позиции двери
        }

        if (occludeDoorSounds) // Если нужна окклюзия
        {
            float occ = SC_OcclusionListener.Sample(pos, door.transform); // Замеряем окклюзию в точке двери (игнорируя саму дверь)
            inst.setParameterByName(occlusionParameter, occ); // Ставим её на разовый инстанс
        }

        inst.start(); // Запускаем
        inst.release(); // Освобождаем (one-shot доиграет и очистится)

        if (showDebugLogs) Debug.Log(gameObject.name + ": дверь — " + label); // Лог
    }
}
