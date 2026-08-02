using System.Collections; // Корутина для задержки перед музыкой
using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)
using FMOD.Studio; // Подключаем EventInstance

// Звук радио (RadioCassettePuzzle). Следит за его состоянием и играет цепочку:
//  включение (one-shot щелчок) -> музыка (зацикленная) -> поломка (one-shot).
// Само радио трогать не нужно — читаем публичные isRadioOn / isExploded.
public class SC_RadioAudio : MonoBehaviour, IOccludable
{
    [Header("References")] // Блок ссылок
    public RadioCassettePuzzle radio; // Радио, за которым следим

    [Header("FMOD Events")] // События
    public EventReference turnOnEvent; // Звук включения (one-shot)

    public EventReference musicEvent; // Музыка (ЗАЦИКЛЕННОЕ событие)

    public EventReference breakEvent; // Звук поломки/взрыва (one-shot)

    public bool attachToRadio = true; // Звук из позиции радио (следует за ним)

    [Header("Music Timing")] // Тайминг музыки
    public float musicStartDelay = 0.3f; // Задержка перед музыкой после щелчка включения

    [Header("Occlusion")] // Приглушение за стенами
    public bool occlude = true; // Глушить звук радио, если между ним и игроком стены

    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в событиях радио

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать логи

    private EventInstance musicInstance; // Экземпляр музыки

    private bool musicPlaying = false; // Играет ли музыка

    private bool wasOn = false; // Было ли радио включено в прошлом кадре

    private bool wasExploded = false; // Было ли взорвано в прошлом кадре

    private Coroutine musicStartRoutine; // Отложенный запуск музыки

    private void Awake() // При создании
    {
        if (radio == null) // Если радио не назначено
        {
            radio = GetComponent<RadioCassettePuzzle>(); // Пробуем на этом же объекте

            if (radio == null) radio = GetComponentInParent<RadioCassettePuzzle>(); // Иначе выше по иерархии
        }
    }

    private void Start() // Перед первым кадром
    {
        if (radio != null) // Синхронизируем стартовое состояние
        {
            wasOn = radio.isRadioOn; // Чтобы не сыграть щелчок на старте
            wasExploded = radio.isExploded; // И не сыграть поломку на старте
        }
    }

    private void OnEnable() // При включении
    {
        if (occlude) SC_OcclusionListener.Register(this); // Регистрируемся у слушателя окклюзии
    }

    private void OnDisable() // При выключении
    {
        SC_OcclusionListener.Unregister(this); // Отписываемся

        CancelPendingMusic(); // Отменяем отложенный запуск
        StopMusic(); // Глушим музыку
    }

    private void OnDestroy() // При уничтожении
    {
        CancelPendingMusic(); // Отменяем отложенный запуск
        StopMusic(); // Глушим музыку
    }

    private void Update() // Каждый кадр
    {
        if (radio == null) return; // Без радио работать не с чем

        bool on = radio.isRadioOn; // Текущее состояние
        bool exploded = radio.isExploded; // Взорвано ли

        if (exploded && !wasExploded) // Только что взорвалось
        {
            OnExplode(); // Поломка
        }
        else if (!exploded) // Пока не взорвано — следим за вкл/выкл
        {
            if (on && !wasOn) OnTurnOn(); // Только что включили
            else if (!on && wasOn) OnTurnOff(); // Только что выключили
        }

        wasOn = on; // Запоминаем
        wasExploded = exploded; // Запоминаем
    }

    private void OnTurnOn() // Радио включили
    {
        PlayOneShot(turnOnEvent); // Щелчок включения

        if (musicStartRoutine != null) StopCoroutine(musicStartRoutine); // Сбрасываем прежний отложенный запуск
        musicStartRoutine = StartCoroutine(StartMusicAfterDelay()); // Музыка через задержку (после щелчка)

        if (showDebugLogs) Debug.Log(gameObject.name + ": радио включено (звук)"); // Лог
    }

    private void OnTurnOff() // Радио выключили
    {
        CancelPendingMusic(); // Отменяем отложенный запуск, если музыка ещё не пошла
        StopMusic(); // Глушим музыку

        if (showDebugLogs) Debug.Log(gameObject.name + ": радио выключено (звук)"); // Лог
    }

    private void OnExplode() // Радио взорвалось
    {
        CancelPendingMusic(); // Отменяем отложенный запуск
        StopMusic(); // Глушим музыку

        PlayOneShot(breakEvent); // Звук поломки

        if (showDebugLogs) Debug.Log(gameObject.name + ": радио сломалось (звук)"); // Лог
    }

    private IEnumerator StartMusicAfterDelay() // Отложенный запуск музыки
    {
        yield return new WaitForSeconds(musicStartDelay); // Ждём, чтобы щелчок отыграл

        musicStartRoutine = null; // Очищаем ссылку

        if (radio.isRadioOn && !radio.isExploded) StartMusic(); // Если всё ещё включено и цело — играем музыку
    }

    private void StartMusic() // Запуск музыки
    {
        if (musicPlaying) return; // Уже играет — выходим

        if (musicEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning(gameObject.name + ": SC_RadioAudio — не назначено событие музыки"); // Предупреждение
            return; // Выходим
        }

        musicInstance = RuntimeManager.CreateInstance(musicEvent); // Создаём инстанс

        if (attachToRadio) RuntimeManager.AttachInstanceToGameObject(musicInstance, radio.gameObject); // Музыка из позиции радио
        else musicInstance.set3DAttributes(RuntimeUtils.To3DAttributes(radio.transform.position)); // Иначе один раз ставим позицию

        musicInstance.start(); // Запускаем зацикленную музыку

        musicPlaying = true; // Играет
    }

    private void StopMusic() // Остановка музыки
    {
        if (!musicPlaying) return; // Не играет — выходим

        musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT); // Останавливаем с затуханием
        musicInstance.release(); // Освобождаем инстанс

        musicPlaying = false; // Не играет
    }

    private void CancelPendingMusic() // Отменить отложенный запуск музыки
    {
        if (musicStartRoutine != null) // Если запуск запланирован
        {
            StopCoroutine(musicStartRoutine); // Отменяем
            musicStartRoutine = null; // Очищаем
        }
    }

    private void PlayOneShot(EventReference e) // Разовый звук в позиции радио (с окклюзией, если включена)
    {
        if (e.IsNull) return; // Не назначено — выходим

        GameObject src = radio != null ? radio.gameObject : gameObject; // Источник звука
        Vector3 pos = src.transform.position; // Позиция радио

        if (occlude) // С окклюзией — через инстанс, чтобы задать параметр
        {
            EventInstance inst = RuntimeManager.CreateInstance(e); // Создаём экземпляр
            RuntimeManager.AttachInstanceToGameObject(inst, src); // Из позиции радио
            inst.setParameterByName(occlusionParameter, SC_OcclusionListener.Sample(pos, src.transform)); // Замер окклюзии в точке радио
            inst.start(); // Запускаем
            inst.release(); // Освобождаем (one-shot доиграет и очистится)
        }
        else // Без окклюзии — обычный one-shot
        {
            if (attachToRadio) RuntimeManager.PlayOneShotAttached(e, src); // Из позиции радио
            else RuntimeManager.PlayOneShot(e, pos); // Разово в точке
        }
    }

    // --- IOccludable: окклюзию музыки раздаёт SC_OcclusionListener с игрока ---

    public Vector3 OcclusionPoint => radio != null ? radio.transform.position : transform.position; // Откуда звучит радио

    public bool WantsOcclusion => musicPlaying && occlude; // Окклюдим, только пока музыка играет

    public void ApplyOcclusion(float occlusion01) // Применить окклюзию к музыке
    {
        if (musicPlaying && occlude) musicInstance.setParameterByName(occlusionParameter, occlusion01); // Пишем в параметр музыки
    }
}
