using System.Collections; // Корутина плавного затухания
using UnityEngine; // Unity-классы
using FMODUnity; // RuntimeManager, StudioEventEmitter
using FMOD.Studio; // Bus

// Глушит звуки подъезда, когда игрок зашёл в квартиру и дверь за ним закрылась.
//
// Рекомендуется: в FMOD Studio завести ВСЕ звуки подъезда под одну шину (например bus:/Stairwell)
// и указать её путь в Bus Path. Тогда один вызов Silence() приглушит сразу всё — лампы, эмбиенс,
// эмиттеры — кем бы они ни проигрывались (StudioEventEmitter или кастомные скрипты вроде SC_LampHum).
//
// Вешай Silence() на событие "дверь закрылась" (UniversalDoor.onClosed) двери квартиры.
public class SC_StairwellSilencer : MonoBehaviour
{
    [Header("FMOD Bus (рекомендуется)")]
    [Tooltip("Путь к шине подъезда в FMOD, например 'bus:/Stairwell'. Все звуки подъезда должны идти через неё.")]
    [SerializeField] private string busPath = "bus:/Stairwell"; // Шина подъезда

    [Tooltip("Обычная громкость шины (1 = как в микшере FMOD). Меняй, если шина в миксе не на 0 dB.")]
    [SerializeField] private float restoreVolume = 1f; // Уровень «включённого» состояния

    [Min(0f)]
    [Tooltip("Плавность выключения, сек. 0 — мгновенная тишина (резко, хорошо для хоррора).")]
    [SerializeField] private float fadeSeconds = 0.5f; // Время затухания

    [Header("Дополнительно (необязательно)")]
    [Tooltip("Явно остановить эти эмиттеры (для звуков, которые НЕ идут через шину подъезда).")]
    [SerializeField] private StudioEventEmitter[] emittersToStop; // Точечные эмиттеры

    [Tooltip("Выключить эти объекты при заглушении (например источники, не привязанные к шине).")]
    [SerializeField] private GameObject[] objectsToDisable; // Объекты выключить

    [Header("Save (необязательно)")]
    [Tooltip("SC_Saveable, чтобы 'подъезд заглушен' переживало загрузку чекпоинта. Авто-поиск на этом объекте.")]
    [SerializeField] private SC_Saveable saveable; // Для сохранения состояния

    private Coroutine fadeRoutine; // Текущее затухание
    private bool silenced; // Заглушено ли сейчас

    private void Awake()
    {
        if (saveable == null) saveable = GetComponent<SC_Saveable>(); // Ищем SC_Saveable на этом объекте
    }

    private void Start()
    {
        // FMOD-шины глобальны и переживают перезагрузку сцены — сбрасываем возможный «залипший» уровень с прошлой сессии
        SetBusVolume(restoreVolume);

        // Если в сохранении отмечено, что подъезд уже заглушен — глушим сразу, без затухания
        if (saveable != null && SC_SaveSystem.TryGetState(saveable.id, out int st) && st == 1)
        {
            silenced = true;
            SetBusVolume(0f);
            StopExtras();
        }
    }

    // Вешай на UniversalDoor.onClosed двери квартиры (или вызывай из любого события).
    public void Silence()
    {
        if (silenced) return; // Уже заглушено
        silenced = true;

        if (fadeSeconds > 0f) // Плавно
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeBus(0f, fadeSeconds));
        }
        else SetBusVolume(0f); // Мгновенно

        StopExtras(); // Точечные эмиттеры/объекты

        if (saveable != null) SC_SaveSystem.SetState(saveable.id, 1); // Запоминаем: подъезд заглушен
    }

    // Вернуть звуки подъезда (если вдруг понадобится). Эмиттеры из списка сами не перезапустятся — только шина/объекты.
    public void Unsilence()
    {
        if (!silenced) return; // Не было заглушено
        silenced = false;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        if (fadeSeconds > 0f) fadeRoutine = StartCoroutine(FadeBus(restoreVolume, fadeSeconds)); // Плавно вернуть
        else SetBusVolume(restoreVolume); // Мгновенно вернуть

        if (objectsToDisable != null)
            foreach (GameObject o in objectsToDisable) if (o != null) o.SetActive(true); // Возвращаем объекты

        if (saveable != null) SC_SaveSystem.SetState(saveable.id, 0); // Запоминаем: подъезд снова слышен
    }

    private void StopExtras() // Остановить точечные эмиттеры и выключить объекты
    {
        if (emittersToStop != null)
            foreach (StudioEventEmitter e in emittersToStop) if (e != null) e.Stop(); // Стоп с затуханием эвента

        if (objectsToDisable != null)
            foreach (GameObject o in objectsToDisable) if (o != null) o.SetActive(false); // Выключаем объекты
    }

    private IEnumerator FadeBus(float target, float dur) // Плавно менять громкость шины
    {
        if (!TryGetBus(out Bus bus)) yield break; // Нет шины — выходим

        bus.getVolume(out float start); // Текущая громкость
        float t = 0f;

        while (t < dur) // Линейно доводим до цели
        {
            t += Time.deltaTime;
            bus.setVolume(Mathf.Lerp(start, target, Mathf.Clamp01(t / dur)));
            yield return null;
        }

        bus.setVolume(target); // Фиксируем итог
        fadeRoutine = null;
    }

    private void SetBusVolume(float v) // Мгновенно задать громкость шины
    {
        if (TryGetBus(out Bus bus)) bus.setVolume(v);
    }

    private bool TryGetBus(out Bus bus) // Получить шину по пути (с защитой)
    {
        bus = default;
        if (string.IsNullOrEmpty(busPath)) return false; // Путь не задан

        try { bus = RuntimeManager.GetBus(busPath); return bus.isValid(); } // Ищем шину в FMOD
        catch { Debug.LogWarning($"SC_StairwellSilencer: FMOD-шина '{busPath}' не найдена. Проверь путь.", this); return false; }
    }
}
