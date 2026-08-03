using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)
using FMOD.Studio; // Подключаем EventInstance

// Звук поездки лифта, привязанный к ИГРОКУ. Для 2D-события позиция не важна, а если
// позже сделаешь событие 3D — звук будет исходить от игрока (привязка сохранится).
// Вешай Play() на SlidingElevatorDoor.onCloseEnd (дверь закрылась → поехали).
public class SC_ElevatorRideSound : MonoBehaviour
{
    [Header("FMOD")] // Блок FMOD
    public EventReference rideEvent; // Звук лифта (2D или 3D)

    [Tooltip("Событие зациклено (нужно потом вызвать Stop()). Если звук разовый — оставь выкл.")]
    public bool looping = false; // Зациклен ли звук поездки

    public bool fadeOutOnStop = true; // Гасить с затуханием при Stop()

    [Header("Attach To Player")] // Блок привязки к игроку
    public Transform attachTarget; // К кому привязать. Пусто — ищем игрока по тегу.

    public string playerTag = "Player"; // Тег игрока для авто-поиска

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private EventInstance instance; // Экземпляр (для зацикленного звука)

    private bool isPlaying = false; // Играет ли зацикленный звук сейчас

    // Вешай на onCloseEnd двери лифта.
    public void Play()
    {
        if (rideEvent.IsNull) // Событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning("SC_ElevatorRideSound: не назначен Ride Event"); // Предупреждение
            return; // Выходим
        }

        GameObject target = ResolveTarget(); // Кому привязать (игрок)

        if (looping) // Зацикленный звук — держим инстанс, чтобы потом остановить
        {
            if (isPlaying) return; // Уже играет

            instance = RuntimeManager.CreateInstance(rideEvent); // Создаём инстанс
            if (target != null) RuntimeManager.AttachInstanceToGameObject(instance, target); // Привязываем к игроку
            instance.start(); // Запускаем
            isPlaying = true; // Играет
        }
        else // Разовый звук
        {
            if (target != null) RuntimeManager.PlayOneShotAttached(rideEvent, target); // Разово, привязано к игроку
            else RuntimeManager.PlayOneShot(rideEvent); // Запасной вариант: просто 2D
        }

        if (showDebugLogs) Debug.Log(gameObject.name + ": звук лифта включён" + (target != null ? " (на игроке)" : "")); // Лог
    }

    // Остановить зацикленный звук лифта (например когда лифт приехал). Для разового не нужно.
    public void Stop()
    {
        if (!isPlaying) return; // Не играет — выходим

        instance.stop(fadeOutOnStop ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE); // Останавливаем
        instance.release(); // Освобождаем инстанс

        isPlaying = false; // Не играет

        if (showDebugLogs) Debug.Log(gameObject.name + ": звук лифта выключен"); // Лог
    }

    private GameObject ResolveTarget() // Найти игрока для привязки
    {
        if (attachTarget != null) return attachTarget.gameObject; // Указан вручную

        GameObject p = GameObject.FindGameObjectWithTag(playerTag); // Ищем по тегу
        if (p == null && showDebugLogs) Debug.LogWarning($"SC_ElevatorRideSound: игрок с тегом '{playerTag}' не найден — играю 2D без привязки"); // Не нашли

        return p; // Игрок или null
    }

    private void OnDestroy() // При уничтожении
    {
        Stop(); // Останавливаем зацикленный звук, если играл
    }
}
