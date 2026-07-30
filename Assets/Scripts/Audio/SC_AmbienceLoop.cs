using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager, RuntimeUtils, StudioListener)
using FMOD.Studio; // Подключаем EventInstance

// Зацикленный позиционный эмбиенс (например, звуки жизни из-за закрытой двери квартиры).
// Спатиализован, слышен по 3D-затуханию рядом с объектом. Поддерживает окклюзию.
// Опционально — отсечка по дистанции: далеко от игрока инстанс не создаётся вообще.
public class SC_AmbienceLoop : MonoBehaviour, IOccludable
{
    [Header("FMOD")] // Блок FMOD
    public EventReference ambienceEvent; // Зацикленное событие эмбиенса

    public bool attachToObject = true; // Привязать звук к этому объекту (позиция следует за ним)

    public bool playOnStart = true; // «Хотеть» играть сразу на старте (иначе — вручную через Play())

    [Header("Occlusion")] // Блок окклюзии
    public bool occludable = true; // Приглушать звук геометрией (нужен SC_OcclusionListener и параметр в событии)

    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в событии

    [Header("Distance Culling")] // Отсечка по дистанции
    public bool useDistanceCulling = false; // Не держать инстанс, пока игрок далеко

    public float maxHearDistance = 25f; // Дальше этого — не играем

    public float cullingHysteresis = 2f; // Запас, чтобы не мигало на границе

    public Transform listenerOverride; // Слушатель вручную (если пусто — StudioListener/камера)

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private EventInstance instance; // Экземпляр эмбиенса

    private bool isPlaying = false; // Реально ли играет сейчас

    private bool wantPlaying = false; // Хотим ли играть (желаемое состояние: включено Play/playOnStart)

    private Transform cachedListener; // Кэш слушателя для отсечки

    private void OnEnable() // При включении
    {
        if (occludable) SC_OcclusionListener.Register(this); // Регистрируемся у слушателя окклюзии

        EvaluatePlayback(); // Возобновляем, если хотели играть и в радиусе
    }

    private void OnDisable() // При выключении
    {
        SC_OcclusionListener.Unregister(this); // Отписываемся

        StopInstance(); // Глушим реальный звук, желание играть НЕ сбрасываем (вернётся при OnEnable)
    }

    private void Start() // Перед первым кадром (банки загружены)
    {
        if (playOnStart) Play(); // Хотим играть с самого начала
    }

    private void OnDestroy() // При уничтожении
    {
        StopInstance(); // Останавливаем и освобождаем
    }

    private void Update() // Каждый кадр
    {
        if (useDistanceCulling) EvaluatePlayback(); // Пока включена отсечка — следим за дистанцией
    }

    public void Play() // Захотеть играть (из других скриптов/UnityEvent)
    {
        wantPlaying = true; // Помечаем желание играть

        EvaluatePlayback(); // Немедленно решаем, запускать ли сейчас
    }

    public void Stop() // Захотеть остановиться
    {
        wantPlaying = false; // Снимаем желание играть

        EvaluatePlayback(); // Останавливаем реальный звук
    }

    private void EvaluatePlayback() // Согласует реальное воспроизведение с желанием и дистанцией
    {
        bool shouldSound = wantPlaying && !ambienceEvent.IsNull && (!useDistanceCulling || WithinHearRange()); // Должно ли звучать сейчас

        if (shouldSound && !isPlaying) // Надо играть, а не играет
        {
            StartInstance(); // Запускаем
        }
        else if (!shouldSound && isPlaying) // Не надо, а играет
        {
            StopInstance(); // Останавливаем
        }
    }

    private void StartInstance() // Реально запустить эмбиенс
    {
        if (ambienceEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning(gameObject.name + ": SC_AmbienceLoop — не назначено событие"); // Предупреждение
            return; // Выходим
        }

        instance = RuntimeManager.CreateInstance(ambienceEvent); // Создаём инстанс

        if (attachToObject) // Если привязываем к объекту
        {
            RuntimeManager.AttachInstanceToGameObject(instance, gameObject); // Позиция следует за объектом
        }
        else // Иначе один раз ставим позицию
        {
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position)); // Позиция в пространстве
        }

        instance.start(); // Запускаем зацикленный эмбиенс

        isPlaying = true; // Играет

        if (showDebugLogs) Debug.Log(gameObject.name + ": эмбиенс запущен"); // Лог
    }

    private void StopInstance() // Реально остановить эмбиенс
    {
        if (!isPlaying) return; // Не играет — выходим

        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT); // Останавливаем с затуханием
        instance.release(); // Освобождаем инстанс

        isPlaying = false; // Не играет

        if (showDebugLogs) Debug.Log(gameObject.name + ": эмбиенс остановлен"); // Лог
    }

    private bool WithinHearRange() // В пределах ли слышимости
    {
        Transform lis = ResolveListener(); // Находим слушателя

        if (lis == null) return true; // Нет слушателя — не отсекаем

        float limit = isPlaying ? maxHearDistance + cullingHysteresis : maxHearDistance; // Гистерезис на границе

        return (transform.position - lis.position).sqrMagnitude <= limit * limit; // Сравниваем квадраты дистанций
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

    public Vector3 OcclusionPoint => transform.position; // Откуда звучит эмбиенс

    public bool WantsOcclusion => isPlaying && occludable; // Окклюдим, только пока играет и если разрешено

    public void ApplyOcclusion(float occlusion01) // Применить окклюзию
    {
        if (isPlaying && occludable) instance.setParameterByName(occlusionParameter, occlusion01); // Пишем в параметр инстанса
    }
}
