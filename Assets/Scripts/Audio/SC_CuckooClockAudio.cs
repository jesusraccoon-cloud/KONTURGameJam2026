using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)
using FMOD.Studio; // Подключаем EventInstance

// Звук часов с кукушкой (CuckooClockPuzzle). Подписывается на события часов и играет:
//  открытие часов (one-shot) и крик кукушки (one-shot, повторяется по сценарию).
// Оба звука — с окклюзией (глухо, если часы за стеной от игрока).
public class SC_CuckooClockAudio : MonoBehaviour
{
    [Header("References")] // Блок ссылок
    public CuckooClockPuzzle clock; // Часы, за которыми следим

    public Transform soundOrigin; // Откуда реально звучат часы (меш/точка часов). Если пусто — позиция объекта clock

    [Header("FMOD Events")] // События
    public EventReference openEvent; // Звук открытия часов (one-shot)

    public EventReference cuckooEvent; // Крик кукушки (one-shot)

    [Header("Occlusion")] // Приглушение за стенами
    public bool occlude = true; // Глушить, если часы за стеной от игрока

    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в событиях часов

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать логи

    private void Awake() // При создании
    {
        if (clock == null) // Если часы не назначены
        {
            clock = GetComponent<CuckooClockPuzzle>(); // Пробуем на этом же объекте

            if (clock == null) clock = GetComponentInParent<CuckooClockPuzzle>(); // Иначе выше по иерархии
        }
    }

    private void OnEnable() // При включении
    {
        if (clock != null) // Если часы найдены
        {
            clock.Opened += PlayOpen; // Подписываемся на открытие
            clock.Cuckooed += PlayCuckoo; // Подписываемся на крик
        }

        Debug.Log($"[CUCKOO] {gameObject.name}: подписка, clock={(clock != null)}"); // ВРЕМЕННО безусловно
    }

    private void OnDisable() // При выключении
    {
        if (clock != null) // Если часы найдены
        {
            clock.Opened -= PlayOpen; // Отписываемся
            clock.Cuckooed -= PlayCuckoo; // Отписываемся
        }
    }

    private void PlayOpen() // Звук открытия часов
    {
        Debug.Log($"[CUCKOO] {gameObject.name}: PlayOpen() вызван"); // ВРЕМЕННО безусловно

        PlayOneShot(openEvent); // Разовый звук
    }

    private void PlayCuckoo() // Крик кукушки
    {
        Debug.Log($"[CUCKOO] {gameObject.name}: PlayCuckoo() вызван"); // ВРЕМЕННО безусловно

        PlayOneShot(cuckooEvent); // Разовый звук
    }

    private void PlayOneShot(EventReference e) // Разовый звук в позиции часов (с окклюзией, если включена)
    {
        if (e.IsNull) // Не назначено
        {
            Debug.LogWarning($"[CUCKOO] {gameObject.name}: событие НЕ назначено (Open или Cuckoo пустой)"); // ВРЕМЕННО безусловно
            return; // Выходим
        }

        Transform originT = soundOrigin != null // Откуда звучат часы
            ? soundOrigin // Явно указанная точка часов
            : (clock != null ? clock.transform : transform); // Иначе объект clock / этот объект

        GameObject src = originT.gameObject; // Источник звука
        Vector3 pos = originT.position; // Позиция часов

        EventInstance inst = RuntimeManager.CreateInstance(e); // Создаём экземпляр
        bool valid = inst.isValid(); // Загружено ли событие в банк (false = событие не собрано)

        RuntimeManager.AttachInstanceToGameObject(inst, src); // Из позиции часов

        float occ = 0f; // Значение окклюзии для лога
        if (occlude) // Если нужна окклюзия
        {
            occ = SC_OcclusionListener.Sample(pos, src.transform); // Замер окклюзии в точке часов
            inst.setParameterByName(occlusionParameter, occ); // Ставим окклюзию
        }

        FMOD.RESULT r = inst.start(); // Запускаем и ловим результат
        inst.release(); // Освобождаем (one-shot доиграет и очистится)

        Debug.Log($"[CUCKOO] play: valid={valid} occ={occ:0.00} start={r} pos={pos}"); // ВРЕМЕННО безусловно — диагностика FMOD
    }
}
