using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)
using FMOD.Studio; // Подключаем EventInstance

// Играет звук пролезания, пока игрок проходит через CrawlPassage.
// Следит за isBusy: стало true — запускаем звук, стало false — глушим.
public class SC_CrawlAudio : MonoBehaviour
{
    [Header("References")] // Блок ссылок
    public CrawlPassage passage; // Проход, за состоянием которого следим

    [Header("FMOD")] // Блок FMOD
    public EventReference crawlEvent; // Звук пролезания (лучше зацикленный — играет всё время прохода)

    public bool attachToPlayer = true; // Звук у игрока (следует за ним); иначе — у самого прохода

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private EventInstance instance; // Экземпляр звука пролезания

    private bool isPlaying = false; // Играет ли сейчас

    private bool wasBusy = false; // Было ли занято в прошлом кадре

    private void Awake() // При создании
    {
        if (passage == null) // Если проход не назначен
        {
            passage = GetComponent<CrawlPassage>(); // Пробуем на этом же объекте

            if (passage == null) passage = GetComponentInParent<CrawlPassage>(); // Иначе выше по иерархии
        }
    }

    private void Start() // Перед первым кадром
    {
        wasBusy = passage != null && passage.isBusy; // Запоминаем стартовое состояние
    }

    private void OnDisable() // При выключении
    {
        StopCrawl(); // Страховка: глушим звук
    }

    private void OnDestroy() // При уничтожении
    {
        StopCrawl(); // Останавливаем и освобождаем
    }

    private void Update() // Каждый кадр
    {
        if (passage == null) return; // Без прохода работать не с чем

        bool busy = passage.isBusy; // Идёт ли пролезание сейчас

        if (busy && !wasBusy) // Пролезание только что началось
        {
            StartCrawl(); // Запускаем звук
        }
        else if (!busy && wasBusy) // Пролезание только что закончилось
        {
            StopCrawl(); // Глушим звук
        }

        wasBusy = busy; // Запоминаем состояние
    }

    private void StartCrawl() // Запуск звука пролезания
    {
        if (crawlEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning(gameObject.name + ": SC_CrawlAudio — событие не назначено"); // Предупреждение
            return; // Выходим
        }

        if (isPlaying) return; // Уже играет — выходим

        instance = RuntimeManager.CreateInstance(crawlEvent); // Создаём инстанс

        GameObject followTarget = (attachToPlayer && passage.playerRoot != null) // Куда привязать звук
            ? passage.playerRoot.gameObject // К игроку (следует за ним при пролезании)
            : gameObject; // Иначе к самому проходу

        RuntimeManager.AttachInstanceToGameObject(instance, followTarget); // Привязываем позицию

        instance.start(); // Запускаем

        isPlaying = true; // Играет

        if (showDebugLogs) Debug.Log(gameObject.name + ": звук пролезания запущен"); // Лог
    }

    private void StopCrawl() // Остановка звука пролезания
    {
        if (!isPlaying) return; // Не играет — выходим

        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT); // Останавливаем с затуханием (зацикленный оборвётся мягко)
        instance.release(); // Освобождаем инстанс

        isPlaying = false; // Не играет

        if (showDebugLogs) Debug.Log(gameObject.name + ": звук пролезания остановлен"); // Лог
    }
}
