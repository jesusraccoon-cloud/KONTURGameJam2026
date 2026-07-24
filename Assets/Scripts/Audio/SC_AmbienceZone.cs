using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)
using FMOD.Studio; // Подключаем EventInstance

// Аудио-зона пространства (подъезд / квартира). Пока игрок внутри триггера,
// играет свой 2D-эмбиент слой и держит свой реверб-снапшот FMOD.
// При выходе — глушит и то, и другое. Любое из полей необязательно.
[RequireComponent(typeof(Collider))]
public class SC_AmbienceZone : MonoBehaviour
{
    [Header("Trigger")] // Блок триггера
    public string playerTag = "Player"; // Тег игрока (или его родителя)

    [Header("Ambience (2D loop)")] // Блок 2D-эмбиента
    public EventReference ambienceEvent; // Зацикленный эмбиент этой зоны

    public bool ambienceFollowsListener = true; // Привязать к слушателю. ОБЯЗАТЕЛЬНО для Scatterer Instrument (звуки вокруг игрока). Для чистого 2D — безвредно

    public Transform listenerOverride; // Слушатель вручную (если пусто — найдём StudioListener/камеру)

    [Header("Reverb Snapshot")] // Блок ревербации
    public EventReference reverbSnapshot; // Снапшот ревербации FMOD для этой зоны

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private EventInstance ambienceInstance; // Инстанс эмбиента

    private EventInstance reverbInstance; // Инстанс снапшота реверба

    private bool ambiencePlaying = false; // Играет ли эмбиент

    private bool reverbActive = false; // Активен ли реверб-снапшот

    private int playersInside = 0; // Счётчик коллайдеров игрока в зоне (защита от нескольких коллайдеров)

    private void Reset() // При добавлении компонента в редакторе
    {
        Collider c = GetComponent<Collider>(); // Берём коллайдер

        if (c != null) c.isTrigger = true; // Сразу делаем его триггером
    }

    private void OnTriggerEnter(Collider other) // Кто-то вошёл в зону
    {
        if (!IsPlayer(other)) return; // Не игрок — выходим

        playersInside++; // Ещё один коллайдер игрока внутри

        if (playersInside == 1) // Первый вход — активируем зону
        {
            EnterZone(); // Включаем эмбиент и реверб
        }
    }

    private void OnTriggerExit(Collider other) // Кто-то вышел из зоны
    {
        if (!IsPlayer(other)) return; // Не игрок — выходим

        playersInside--; // Один коллайдер игрока покинул зону

        if (playersInside <= 0) // Игрок полностью вышел
        {
            playersInside = 0; // Не уходим в минус
            ExitZone(); // Глушим эмбиент и реверб
        }
    }

    private void EnterZone() // Активация зоны
    {
        if (!ambiencePlaying && !ambienceEvent.IsNull) // Если эмбиент назначен и не играет
        {
            ambienceInstance = RuntimeManager.CreateInstance(ambienceEvent); // Создаём инстанс

            if (ambienceFollowsListener) // Нужно для Scatterer Instrument: даём 3D-позицию
            {
                GameObject lis = ResolveListenerObject(); // Находим слушателя

                if (lis != null) RuntimeManager.AttachInstanceToGameObject(ambienceInstance, lis); // Событие следует за игроком — скаттер сыплет вокруг него
                else ambienceInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position)); // Запасной вариант — позиция зоны
            }

            ambienceInstance.start(); // Запускаем
            ambiencePlaying = true; // Играет
        }

        if (!reverbActive && !reverbSnapshot.IsNull) // Если снапшот назначен и не активен
        {
            reverbInstance = RuntimeManager.CreateInstance(reverbSnapshot); // Создаём инстанс снапшота
            reverbInstance.start(); // Включаем реверб пространства
            reverbActive = true; // Активен
        }

        if (showDebugLogs) Debug.Log(gameObject.name + ": игрок вошёл — зона активна"); // Лог
    }

    private void ExitZone() // Деактивация зоны
    {
        StopAmbience(); // Глушим эмбиент

        StopReverb(); // Глушим реверб

        if (showDebugLogs) Debug.Log(gameObject.name + ": игрок вышел — зона выключена"); // Лог
    }

    private void StopAmbience() // Остановить эмбиент
    {
        if (!ambiencePlaying) return; // Не играет — выходим

        ambienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT); // Останавливаем с затуханием
        ambienceInstance.release(); // Освобождаем

        ambiencePlaying = false; // Не играет
    }

    private void StopReverb() // Остановить реверб-снапшот
    {
        if (!reverbActive) return; // Не активен — выходим

        reverbInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT); // Останавливаем с затуханием
        reverbInstance.release(); // Освобождаем

        reverbActive = false; // Не активен
    }

    private void OnDisable() // При выключении объекта
    {
        playersInside = 0; // Сбрасываем счётчик

        StopAmbience(); // Глушим эмбиент

        StopReverb(); // Глушим реверб
    }

    private GameObject ResolveListenerObject() // Найти объект слушателя (для 3D-позиции скаттера)
    {
        if (listenerOverride != null) return listenerOverride.gameObject; // Указан вручную

        StudioListener sl = FindObjectOfType<StudioListener>(); // Ищем FMOD-слушателя
        if (sl != null) return sl.gameObject; // Нашли — его объект

        if (Camera.main != null) return Camera.main.gameObject; // Иначе главная камера

        return null; // Слушатель не найден
    }

    private bool IsPlayer(Collider other) // Проверка, что это игрок (по тегу, включая родителей)
    {
        Transform t = other.transform; // Начинаем с коллайдера

        while (t != null) // Идём вверх по иерархии
        {
            if (t.CompareTag(playerTag)) return true; // Нашли тег игрока

            t = t.parent; // К родителю
        }

        return false; // Не игрок
    }
}
