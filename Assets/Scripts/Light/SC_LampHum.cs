using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager, RuntimeUtils)
using FMOD.Studio; // Подключаем EventInstance

public class SC_LampHum : MonoBehaviour, IOccludable // Зацикленный гул лампы: играет, пока лампа включена; поддерживает окклюзию
{
    [Header("References")] // Блок ссылок
    public Light targetLight; // Лампа, за состоянием которой следим

    [Header("FMOD")] // Блок FMOD
    public EventReference humEvent; // Зацикленное событие гула лампы

    public bool attachToLamp = true; // Привязать звук к лампе (позиция следует за ней)

    public bool fadeOutOnStop = false; // Гасить гул с затуханием (иначе — мгновенно, без хвоста)

    [Header("On Detection")] // Блок определения «лампа включена»
    public bool alsoRequireIntensity = false; // Учитывать ещё и яркость (гул только если лампа реально светит)

    public float minIntensity = 0.05f; // Порог яркости, ниже которого считаем лампу выключенной

    [Header("Distance Culling")] // Отсечка по дистанции (чтобы дальние лампы не держали инстанс)
    public bool useDistanceCulling = false; // Включать гул только когда игрок близко

    public float maxHearDistance = 20f; // Дальше этого расстояния гул не играет

    public float cullingHysteresis = 2f; // Запас, чтобы не мигало на границе (стоп на maxHearDistance + это)

    public Transform listenerOverride; // Слушатель вручную (если пусто — StudioListener/камера)

    [Header("Occlusion")] // Блок окклюзии
    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в событии гула (если есть)

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private EventInstance humInstance; // Экземпляр зацикленного гула

    private bool isPlaying = false; // Играет ли гул сейчас

    private Transform cachedListener; // Кэш слушателя для отсечки по дистанции

    private void Awake() // Вызывается при создании объекта
    {
        if (targetLight == null) // Если лампа не назначена
        {
            targetLight = GetComponent<Light>(); // Пробуем взять Light с этого же объекта
        }
    }

    private void Start() // Вызывается перед первым кадром (банки уже загружены)
    {
        UpdateHumState(); // Синхронизируем гул со стартовым состоянием лампы
    }

    private void OnEnable() // При включении компонента/объекта
    {
        SC_OcclusionListener.Register(this); // Регистрируемся у слушателя окклюзии
    }

    private void OnDisable() // При выключении компонента/объекта
    {
        SC_OcclusionListener.Unregister(this); // Отписываемся от окклюзии

        StopHum(); // Страховка: гасим гул, чтобы он не «завис» играющим
    }

    private void OnDestroy() // При уничтожении объекта
    {
        StopHum(); // Останавливаем и освобождаем гул
    }

    private void Update() // Вызывается каждый кадр
    {
        UpdateHumState(); // Следим за состоянием лампы
    }

    private void UpdateHumState() // Включает/выключает гул под состояние лампы
    {
        bool on = IsLampOn() && WithinHearRange(); // Лампа включена И игрок в пределах слышимости

        if (on && !isPlaying) // Лампа включилась, а гул не играет
        {
            StartHum(); // Запускаем гул
        }
        else if (!on && isPlaying) // Лампа выключилась, а гул играет
        {
            StopHum(); // Останавливаем гул
        }
    }

    private bool IsLampOn() // Определение «лампа включена»
    {
        if (targetLight == null) return false; // Нет лампы — считаем выключенной

        if (!targetLight.isActiveAndEnabled) return false; // Light выключен ИЛИ его объект деактивирован — считаем выключенной

        if (alsoRequireIntensity && targetLight.intensity < minIntensity) return false; // Слишком тускло — считаем выключенной

        return true; // Иначе включена
    }

    private void StartHum() // Запуск гула
    {
        if (humEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning("SC_LampHum: не назначено FMOD-событие гула"); // Предупреждение
            return; // Выходим
        }

        humInstance = RuntimeManager.CreateInstance(humEvent); // Создаём инстанс

        Transform soundAt = targetLight != null ? targetLight.transform : transform; // Откуда звучит лампа

        if (attachToLamp) // Если привязываем к лампе
        {
            RuntimeManager.AttachInstanceToGameObject(humInstance, soundAt.gameObject); // Позиция следует за лампой
        }
        else // Иначе один раз ставим позицию
        {
            humInstance.set3DAttributes(RuntimeUtils.To3DAttributes(soundAt.position)); // Позиция в пространстве
        }

        humInstance.start(); // Запускаем зацикленный гул

        isPlaying = true; // Гул играет

        if (showDebugLogs) Debug.Log(gameObject.name + ": гул лампы включён"); // Лог
    }

    private void StopHum() // Остановка гула
    {
        if (!isPlaying) return; // Если гул не играет — выходим

        FMOD.Studio.STOP_MODE mode = fadeOutOnStop // Выбираем режим остановки
            ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT // С затуханием/хвостом
            : FMOD.Studio.STOP_MODE.IMMEDIATE; // Мгновенно, без хвоста

        humInstance.stop(mode); // Останавливаем гул
        humInstance.release(); // Освобождаем инстанс

        isPlaying = false; // Гул не играет

        if (showDebugLogs) Debug.Log(gameObject.name + ": гул лампы выключен"); // Лог
    }

    private bool WithinHearRange() // В пределах ли слышимости (для отсечки по дистанции)
    {
        if (!useDistanceCulling) return true; // Отсечка выключена — всегда «в радиусе»

        Transform lis = ResolveListener(); // Находим слушателя

        if (lis == null) return true; // Нет слушателя — не отсекаем

        Vector3 pos = targetLight != null ? targetLight.transform.position : transform.position; // Позиция лампы

        float limit = isPlaying ? maxHearDistance + cullingHysteresis : maxHearDistance; // Гистерезис: играющий гул глушим чуть дальше

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

    public Vector3 OcclusionPoint => targetLight != null ? targetLight.transform.position : transform.position; // Откуда звучит лампа

    public bool WantsOcclusion => isPlaying; // Окклюдим, только пока гул играет

    public void ApplyOcclusion(float occlusion01) // Применить окклюзию к гулу
    {
        if (isPlaying) humInstance.setParameterByName(occlusionParameter, occlusion01); // Пишем в параметр инстанса
    }
}
