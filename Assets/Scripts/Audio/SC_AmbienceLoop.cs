using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager, RuntimeUtils)
using FMOD.Studio; // Подключаем EventInstance

// Зацикленный позиционный эмбиенс (например, звуки жизни из-за закрытой двери квартиры).
// Спатиализован, слышен по 3D-затуханию рядом с объектом. Поддерживает окклюзию —
// дверь/стена между игроком и источником приглушат звук через SC_OcclusionListener.
public class SC_AmbienceLoop : MonoBehaviour, IOccludable
{
    [Header("FMOD")] // Блок FMOD
    public EventReference ambienceEvent; // Зацикленное событие эмбиенса

    public bool attachToObject = true; // Привязать звук к этому объекту (позиция следует за ним)

    public bool playOnStart = true; // Запускать сразу на старте (иначе — вручную через Play())

    [Header("Occlusion")] // Блок окклюзии
    public bool occludable = true; // Приглушать звук геометрией (нужен SC_OcclusionListener на игроке и параметр в событии)

    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в событии

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private EventInstance instance; // Экземпляр эмбиенса

    private bool isPlaying = false; // Играет ли сейчас

    private void OnEnable() // При включении
    {
        if (occludable) SC_OcclusionListener.Register(this); // Регистрируемся у слушателя окклюзии
    }

    private void OnDisable() // При выключении
    {
        SC_OcclusionListener.Unregister(this); // Отписываемся (безопасно, даже если не были зарегистрированы)

        Stop(); // Гасим, чтобы не «завис» играющим
    }

    private void Start() // Перед первым кадром (банки загружены)
    {
        if (playOnStart) Play(); // Запускаем эмбиенс
    }

    private void OnDestroy() // При уничтожении
    {
        Stop(); // Останавливаем и освобождаем
    }

    public void Play() // Запустить эмбиенс (можно дёргать из других скриптов/UnityEvent)
    {
        if (isPlaying) return; // Уже играет — выходим

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

    public void Stop() // Остановить эмбиенс
    {
        if (!isPlaying) return; // Не играет — выходим

        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT); // Останавливаем с затуханием
        instance.release(); // Освобождаем инстанс

        isPlaying = false; // Не играет

        if (showDebugLogs) Debug.Log(gameObject.name + ": эмбиенс остановлен"); // Лог
    }

    // --- IOccludable: окклюзию раздаёт SC_OcclusionListener с игрока ---

    public Vector3 OcclusionPoint => transform.position; // Откуда звучит эмбиенс

    public bool WantsOcclusion => isPlaying && occludable; // Окклюдим, только пока играет и если разрешено

    public void ApplyOcclusion(float occlusion01) // Применить окклюзию
    {
        if (isPlaying && occludable) instance.setParameterByName(occlusionParameter, occlusion01); // Пишем в параметр инстанса
    }
}
