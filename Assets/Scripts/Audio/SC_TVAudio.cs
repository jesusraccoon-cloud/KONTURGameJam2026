using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager, RuntimeUtils)
using FMOD.Studio; // Подключаем EventInstance

// Звук телевизора. Телевизор — ThreeStageInteractableObject: на стадии On Stage он «включён».
// При переходе во «включён» — разовый звук включения + запуск зацикленного гула/помех.
// При переходе из «включён» — стоп лупа + разовый звук выключения.
public class SC_TVAudio : MonoBehaviour, IOccludable
{
    [Header("References")] // Блок ссылок
    public ThreeStageInteractableObject tv; // Телевизор, за стадией которого следим (авто-поиск)

    public Transform soundOrigin; // Откуда звучит телевизор. Пусто — этот объект. ЗАДАЙ, если телек в «складе» AP1 (−350).

    [Header("FMOD")] // Блок FMOD
    public EventReference turnOnEvent; // Разовый звук включения (щелчок/бум ЭЛТ)

    public EventReference turnOffEvent; // Разовый звук выключения

    public EventReference loopEvent; // Зацикленный звук работающего телевизора (гул/помехи)

    public bool fadeOutOnStop = true; // Гасить луп с затуханием (иначе мгновенно)

    [Header("Stage")] // Блок стадий
    public int onStage = 1; // На какой стадии телевизор ВКЛючён (обычно 1). Другая стадия — выключен.

    [Header("Distance Culling")] // Отсечка по дистанции (чтобы дальний телек не держал инстанс)
    public bool useDistanceCulling = false; // Играть луп только когда игрок близко

    public float maxHearDistance = 15f; // Дальше этого расстояния луп не играет

    public float cullingHysteresis = 2f; // Запас, чтобы не мигало на границе

    public Transform listenerOverride; // Слушатель вручную (если пусто — StudioListener/камера)

    [Header("Occlusion")] // Блок окклюзии
    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в лупе (если есть)

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private EventInstance loopInstance; // Экземпляр зацикленного звука

    private bool isLoopPlaying = false; // Играет ли луп сейчас

    private bool wasOn = false; // Был ли телевизор включён в прошлом кадре (для разовых звуков)

    private Transform cachedListener; // Кэш слушателя для отсечки по дистанции

    private int lastLoggedStage = -999; // Для отладочного лога смены стадии

    private void Awake() // При создании объекта
    {
        if (tv == null) tv = GetComponent<ThreeStageInteractableObject>(); // Пробуем взять телевизор с этого объекта
        if (tv == null) tv = GetComponentInParent<ThreeStageInteractableObject>(); // Или с родителя
    }

    private void Start() // При старте сцены (банки загружены)
    {
        wasOn = IsOn(); // Запоминаем стартовое состояние БЕЗ разового звука (в т.ч. после загрузки сейва)

        if (wasOn && WithinHearRange()) StartLoop(); // Если телек уже включён на старте — сразу запускаем луп (без «щелчка включения»)
    }

    private void OnEnable() // При включении компонента/объекта
    {
        SC_OcclusionListener.Register(this); // Регистрируемся у слушателя окклюзии
    }

    private void OnDisable() // При выключении компонента/объекта
    {
        SC_OcclusionListener.Unregister(this); // Отписываемся от окклюзии

        StopLoop(true); // Страховка: гасим луп мгновенно
    }

    private void OnDestroy() // При уничтожении объекта
    {
        StopLoop(true); // Останавливаем и освобождаем луп
    }

    private void Update() // Каждый кадр
    {
        if (showDebugLogs && tv != null && tv.CurrentStage != lastLoggedStage) // Стадия сменилась — логируем
        {
            lastLoggedStage = tv.CurrentStage; // Запоминаем
            Debug.Log($"{gameObject.name}: стадия ТВ = {lastLoggedStage} (включён при стадии {onStage}), tv='{tv.name}'"); // Текущая стадия
        }

        bool on = IsOn(); // Включён ли телевизор сейчас (по стадии)

        if (on && !wasOn) OnTurnedOn(); // Только что включили — щелчок включения + луп
        else if (!on && wasOn) OnTurnedOff(); // Только что выключили — стоп лупа + щелчок выключения
        wasOn = on; // Запоминаем состояние

        // Луп играет, пока включён И игрок в пределах слышимости
        bool shouldLoop = on && WithinHearRange();
        if (shouldLoop && !isLoopPlaying) StartLoop(); // Вернулись в радиус — вернуть луп
        else if (!shouldLoop && isLoopPlaying) StopLoop(false); // Выключен / вышли из радиуса — стоп

        if (isLoopPlaying) loopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(Origin.position)); // Держим позицию источника
    }

    private void OnTurnedOn() // Телевизор включили
    {
        if (!turnOnEvent.IsNull) RuntimeManager.PlayOneShot(turnOnEvent, Origin.position); // Разовый звук включения
        StartLoop(); // Запускаем зацикленный звук
        if (showDebugLogs) Debug.Log(gameObject.name + ": телевизор включён"); // Лог
    }

    private void OnTurnedOff() // Телевизор выключили
    {
        StopLoop(false); // Останавливаем зацикленный звук
        if (!turnOffEvent.IsNull) RuntimeManager.PlayOneShot(turnOffEvent, Origin.position); // Разовый звук выключения
        if (showDebugLogs) Debug.Log(gameObject.name + ": телевизор выключен"); // Лог
    }

    private bool IsOn() // Включён ли телевизор сейчас
    {
        if (tv == null) return false; // Нет телевизора — считаем выключенным

        return tv.CurrentStage == onStage; // Включён только на нужной стадии
    }

    private Transform Origin => soundOrigin != null ? soundOrigin : transform; // Откуда звучит телевизор

    private void StartLoop() // Запуск зацикленного звука
    {
        if (isLoopPlaying) return; // Уже играет

        if (loopEvent.IsNull) // Луп не назначен
        {
            if (showDebugLogs) Debug.LogWarning("SC_TVAudio: не назначен Loop Event"); // Предупреждение
            return; // Выходим
        }

        loopInstance = RuntimeManager.CreateInstance(loopEvent); // Создаём инстанс
        loopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(Origin.position)); // Статичная позиция источника (без привязки — надёжнее)
        FMOD.RESULT r = loopInstance.start(); // Запускаем зацикленный звук

        isLoopPlaying = true; // Луп играет

        if (showDebugLogs) Debug.Log($"{gameObject.name}: луп ТВ запущен в {Origin.position} (startResult={r})"); // Диагностика старта лупа
    }

    private void StopLoop(bool forceImmediate) // Остановка зацикленного звука
    {
        if (!isLoopPlaying) return; // Не играет — выходим

        FMOD.Studio.STOP_MODE mode = (fadeOutOnStop && !forceImmediate) // Режим остановки
            ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT // С затуханием
            : FMOD.Studio.STOP_MODE.IMMEDIATE; // Мгновенно

        loopInstance.stop(mode); // Останавливаем
        loopInstance.release(); // Освобождаем инстанс

        isLoopPlaying = false; // Луп не играет
    }

    private bool WithinHearRange() // В пределах ли слышимости (для отсечки по дистанции)
    {
        if (!useDistanceCulling) return true; // Отсечка выключена — всегда «в радиусе»

        Transform lis = ResolveListener(); // Находим слушателя

        if (lis == null) return true; // Нет слушателя — не отсекаем

        float limit = isLoopPlaying ? maxHearDistance + cullingHysteresis : maxHearDistance; // Гистерезис

        return (Origin.position - lis.position).sqrMagnitude <= limit * limit; // Сравниваем квадраты дистанций
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

    public Vector3 OcclusionPoint => Origin.position; // Откуда звучит телевизор

    public bool WantsOcclusion => isLoopPlaying; // Окклюдим, только пока играет луп

    public void ApplyOcclusion(float occlusion01) // Применить окклюзию к лупу
    {
        if (isLoopPlaying) loopInstance.setParameterByName(occlusionParameter, occlusion01); // Пишем в параметр инстанса
    }
}
