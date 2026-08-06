using System.Collections; // Корутина плавных переходов
using UnityEngine; // Unity-классы
using FMODUnity; // RuntimeManager, EventReference
using FMOD.Studio; // EventInstance

// Универсальный музыкальный проигрыватель (2D-музыка).
// Один компонент на каждый трек: меню / вступление / концовка.
//  - Play()            — запустить трек (с fade in);
//  - FadeOutAndStop()  — плавно приглушить и выключить (например при входе в квартиру);
//  - Stop()            — выключить мгновенно.
public class SC_MusicPlayer : MonoBehaviour
{
    [Header("FMOD")] // Блок FMOD
    public EventReference music; // Музыкальный трек (обычно зациклённый)

    [Range(0f, 1f)] public float volume = 1f; // Громкость трека

    [Header("Auto Play")] // Блок автозапуска
    public bool playOnStart = true; // Запускать при старте сцены

    [Tooltip("Не запускать авто-музыку, если сохранённый чекпоинт >= этого значения (0 = всегда играть).\n" +
             "Для вступления поставь 1 — тогда после загрузки в квартиру вступление не заиграет заново.")]
    public int suppressIfCheckpointAtLeast = 0; // Не играть вступление, если игрок уже прошёл дальше

    [Header("Fades")] // Плавность
    [Min(0f)] public float fadeInSeconds = 1f; // Плавное появление

    [Min(0f)] public float fadeOutSeconds = 2f; // Плавное затухание

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать логи

    private EventInstance instance; // Экземпляр музыки

    private bool isPlaying = false; // Играет ли сейчас

    private Coroutine fadeRoutine; // Текущий переход

    private void Start() // При старте сцены
    {
        if (!playOnStart) return; // Автозапуск выключен

        if (suppressIfCheckpointAtLeast > 0)
        {
            if (showDebugLogs) Debug.Log(gameObject.name + ": музыка не запущена — игрок уже прошёл дальше (чекпоинт)"); // Лог
            return; // После загрузки дальше по игре — вступление не нужно
        }

        Play(); // Запускаем трек
    }

    // Запустить трек (с fade in). Вешай на onFinalStart и т.п. для концовки.
    public void Play()
    {
        if (music.IsNull) // Трек не назначен
        {
            if (showDebugLogs) Debug.LogWarning(gameObject.name + ": SC_MusicPlayer — трек не назначен"); // Предупреждение
            return; // Выходим
        }

        if (isPlaying) return; // Уже играет

        instance = RuntimeManager.CreateInstance(music); // Создаём инстанс
        instance.setVolume(fadeInSeconds > 0f ? 0f : volume); // Стартовая громкость (с нуля, если есть fade in)
        instance.start(); // Запускаем
        isPlaying = true; // Играет

        if (fadeInSeconds > 0f) StartFade(volume, fadeInSeconds, false); // Плавно поднимаем громкость

        if (showDebugLogs) Debug.Log(gameObject.name + ": музыка включена"); // Лог
    }

    // Плавно приглушить и выключить. Вешай на вход в квартиру (триггер / чекпоинт 1).
    public void FadeOutAndStop()
    {
        if (!isPlaying) return; // Не играет — нечего гасить

        StartFade(0f, fadeOutSeconds, true); // Затухаем и в конце останавливаем

        if (showDebugLogs) Debug.Log(gameObject.name + ": музыка затухает и выключится"); // Лог
    }

    // Выключить мгновенно.
    public void Stop()
    {
        if (!isPlaying) return; // Не играет

        StopFade(); // Останавливаем переход
        instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); // Стоп без хвоста
        instance.release(); // Освобождаем
        isPlaying = false; // Не играет
    }

    private void StartFade(float target, float dur, bool stopAtEnd) // Запустить плавный переход громкости
    {
        StopFade(); // Отменяем прежний
        fadeRoutine = StartCoroutine(FadeRoutine(target, dur, stopAtEnd)); // Запускаем новый
    }

    private void StopFade() // Остановить текущий переход
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine); // Стоп корутины
        fadeRoutine = null; // Очищаем
    }

    private IEnumerator FadeRoutine(float target, float dur, bool stopAtEnd) // Плавно менять громкость
    {
        instance.getVolume(out float start); // Текущая громкость
        float t = 0f;

        while (t < dur) // Линейно доводим до цели
        {
            t += Time.unscaledDeltaTime; // Не зависим от паузы/таймскейла
            instance.setVolume(Mathf.Lerp(start, target, dur > 0f ? Mathf.Clamp01(t / dur) : 1f));
            yield return null;
        }

        instance.setVolume(target); // Фиксируем итог
        fadeRoutine = null;

        if (stopAtEnd) // Нужно выключить после затухания
        {
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); // Стоп
            instance.release(); // Освобождаем
            isPlaying = false; // Не играет
        }
    }

    private void OnDestroy() // При выгрузке сцены/объекта
    {
        if (isPlaying) // Если музыка ещё играет
        {
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); // Гасим, чтобы не тянулась в другую сцену
            instance.release(); // Освобождаем
            isPlaying = false;
        }
    }
}
