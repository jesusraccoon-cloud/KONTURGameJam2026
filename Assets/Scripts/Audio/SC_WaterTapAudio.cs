using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager, RuntimeUtils)
using FMOD.Studio; // Подключаем EventInstance

// Зацикленный звук воды из-под крана. Кран — это ThreeStageInteractableObject:
// первое нажатие включает воду (стадия 1), второе нажатие переводит на стадию 2 — вода замолкает.
// Звук играет, пока текущая стадия крана == Water On Stage; на любой другой стадии — молчит.
public class SC_WaterTapAudio : MonoBehaviour, IOccludable // Поддерживает окклюзию (как SC_LampHum)
{
    [Header("References")] // Блок ссылок
    public ThreeStageInteractableObject tap; // Кран, за стадией которого следим (авто-поиск на этом/родительском объекте)

    public Transform soundOrigin; // Откуда льётся вода (носик крана). Пусто — этот объект.

    [Header("FMOD")] // Блок FMOD
    public EventReference waterEvent; // Зацикленное событие воды

    public bool fadeOutOnStop = true; // Гасить с затуханием (иначе мгновенно, без хвоста)

    [Header("Stage")] // Блок стадий
    public int waterOnStage = 1; // На какой стадии крана вода льётся (обычно 1). Второе нажатие -> стадия 2 -> тишина.

    [Header("Distance Culling")] // Отсечка по дистанции (чтобы дальний кран не держал инстанс)
    public bool useDistanceCulling = false; // Включать воду только когда игрок близко

    public float maxHearDistance = 15f; // Дальше этого расстояния вода не играет

    public float cullingHysteresis = 2f; // Запас, чтобы не мигало на границе

    public Transform listenerOverride; // Слушатель вручную (если пусто — StudioListener/камера)

    [Header("Occlusion")] // Блок окклюзии
    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в событии воды (если есть)

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private EventInstance waterInstance; // Экземпляр зацикленной воды

    private bool isPlaying = false; // Играет ли вода сейчас

    private Transform cachedListener; // Кэш слушателя для отсечки по дистанции

    private void Awake() // Вызывается при создании объекта
    {
        if (tap == null) tap = GetComponent<ThreeStageInteractableObject>(); // Пробуем взять кран с этого объекта
        if (tap == null) tap = GetComponentInParent<ThreeStageInteractableObject>(); // Или с родителя
    }

    private void Start() // Вызывается перед первым кадром (банки загружены)
    {
        UpdateWaterState(); // Синхронизируем звук со стартовой стадией крана (в т.ч. после загрузки сейва)
    }

    private void OnEnable() // При включении компонента/объекта
    {
        SC_OcclusionListener.Register(this); // Регистрируемся у слушателя окклюзии
    }

    private void OnDisable() // При выключении компонента/объекта
    {
        SC_OcclusionListener.Unregister(this); // Отписываемся от окклюзии

        StopWater(true); // Гасим воду МГНОВЕННО (объект выключают — хвост затухания на нём не нужен)
    }

    private void OnDestroy() // При уничтожении объекта
    {
        StopWater(true); // Останавливаем и освобождаем звук мгновенно
    }

    private void Update() // Вызывается каждый кадр
    {
        UpdateWaterState(); // Следим за стадией крана
    }

    private void UpdateWaterState() // Включает/выключает воду под стадию крана
    {
        bool on = IsWaterOn() && WithinHearRange(); // Кран в «водной» стадии И игрок в пределах слышимости

        if (on && !isPlaying) // Воду включили, а звук не играет
        {
            StartWater(); // Запускаем звук воды
        }
        else if (!on && isPlaying) // Воду выключили, а звук играет
        {
            StopWater(); // Останавливаем звук воды
        }
    }

    private bool IsWaterOn() // Льётся ли вода сейчас
    {
        if (tap == null) return false; // Нет крана — считаем выключенной

        return tap.CurrentStage == waterOnStage; // Вода только на нужной стадии
    }

    private Transform Origin => soundOrigin != null ? soundOrigin : transform; // Откуда звучит вода

    private void StartWater() // Запуск звука воды
    {
        if (waterEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning("SC_WaterTapAudio: не назначено FMOD-событие воды"); // Предупреждение
            return; // Выходим
        }

        waterInstance = RuntimeManager.CreateInstance(waterEvent); // Создаём инстанс

        waterInstance.set3DAttributes(RuntimeUtils.To3DAttributes(Origin.position)); // Статичная позиция источника (кран не двигается — привязка не нужна, надёжнее для старта и остановки)

        waterInstance.start(); // Запускаем зацикленную воду

        isPlaying = true; // Вода играет

        if (showDebugLogs) Debug.Log(gameObject.name + ": вода включена"); // Лог
    }

    private void StopWater(bool forceImmediate = false) // Остановка звука воды (forceImmediate — рубить без затухания)
    {
        if (!isPlaying) return; // Если вода не играет — выходим

        FMOD.Studio.STOP_MODE mode = (fadeOutOnStop && !forceImmediate) // Выбираем режим остановки
            ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT // С затуханием/хвостом
            : FMOD.Studio.STOP_MODE.IMMEDIATE; // Мгновенно, без хвоста

        waterInstance.stop(mode); // Останавливаем воду
        waterInstance.release(); // Освобождаем инстанс

        isPlaying = false; // Вода не играет

        if (showDebugLogs) Debug.Log(gameObject.name + ": вода выключена"); // Лог
    }

    private bool WithinHearRange() // В пределах ли слышимости (для отсечки по дистанции)
    {
        if (!useDistanceCulling) return true; // Отсечка выключена — всегда «в радиусе»

        Transform lis = ResolveListener(); // Находим слушателя

        if (lis == null) return true; // Нет слушателя — не отсекаем

        Vector3 pos = Origin.position; // Позиция источника воды

        float limit = isPlaying ? maxHearDistance + cullingHysteresis : maxHearDistance; // Гистерезис: играющую воду глушим чуть дальше

        return (pos - lis.position).sqrMagnitude <= limit * limit; // Сравниваем квадраты дистанций
    }

    private Transform ResolveListener() // Находит слушателя (кэширует)
    {
        if (listenerOverride != null) return listenerOverride; // Указан вручную

        if (cachedListener != null) return cachedListener; // Уже нашли

        StudioListener sl = FindFirstObjectByType<StudioListener>(); // Ищем FMOD-слушателя
        if (sl != null) { cachedListener = sl.transform; return cachedListener; } // Нашли — кэшируем

        if (Camera.main != null) { cachedListener = Camera.main.transform; return cachedListener; } // Иначе камера

        return null; // Слушатель не найден
    }

    // --- IOccludable: окклюзию раздаёт SC_OcclusionListener с игрока ---

    public Vector3 OcclusionPoint => Origin.position; // Откуда звучит вода

    public bool WantsOcclusion => isPlaying; // Окклюдим, только пока вода играет

    public void ApplyOcclusion(float occlusion01) // Применить окклюзию к воде
    {
        if (isPlaying) waterInstance.setParameterByName(occlusionParameter, occlusion01); // Пишем в параметр инстанса
    }
}
