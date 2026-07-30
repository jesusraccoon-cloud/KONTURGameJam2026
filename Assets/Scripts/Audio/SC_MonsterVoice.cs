using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)
using FMOD.Studio; // Подключаем EventInstance

// Голос монстра (one-shot события):
//  - НЕ видит игрока: по случайному интервалу играет "плачет" или "говорит" (случайный выбор).
//  - Видит игрока (погоня): "кричит" при обнаружении и периодически, пока видит.
public class SC_MonsterVoice : MonoBehaviour
{
    [Header("References")] // Блок ссылок
    public MonsterAI monster; // ИИ монстра (состояние, активность)

    public MonsterVision vision; // Зрение монстра (если пусто — возьмём monster.vision)

    [Header("FMOD Events (one-shots)")] // События
    public EventReference cryEvent; // Плачет (когда не видит)

    public EventReference talkEvent; // Говорит (когда не видит)

    public EventReference screamEvent; // Кричит (когда видит игрока)

    [Header("Idle Vocal (когда НЕ видит)")] // Плач/говор
    public float idleMinInterval = 5f; // Минимальный интервал между вокализациями

    public float idleMaxInterval = 12f; // Максимальный интервал между вокализациями

    [Range(0f, 1f)] public float cryChance = 0.5f; // Вероятность плача (иначе — говор)

    [Header("Scream (когда видит)")] // Крик
    public bool screamOnSpot = true; // Крик в момент обнаружения игрока

    public bool screamRepeats = true; // Повторять крик, пока видит

    public float screamMinInterval = 3f; // Минимальный интервал повторов крика

    public float screamMaxInterval = 6f; // Максимальный интервал повторов крика

    [Header("Occlusion")] // Приглушение голоса за стенами
    public bool occludeVoice = true; // Глушить голос, если между монстром и игроком есть стены (замер в момент проигрывания)

    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в событиях голоса

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать логи

    private float idleTimer; // Таймер до следующего плача/говора

    private float screamTimer; // Таймер до следующего крика в погоне

    private bool wasSeeing; // Видел ли игрока в прошлом кадре

    private EventInstance currentVoice; // Текущий играющий голос (для монофоничности)

    private bool hasVoice; // Есть ли активный инстанс голоса

    private void Awake() // При создании
    {
        if (monster == null) monster = GetComponent<MonsterAI>(); // Ищем ИИ на этом же объекте

        if (vision == null) // Если зрение не назначено
        {
            vision = (monster != null && monster.vision != null) ? monster.vision : GetComponent<MonsterVision>(); // Берём из ИИ или с объекта
        }
    }

    private void Start() // Перед первым кадром
    {
        idleTimer = RandomIdleInterval(); // Первый плач/говор — после случайной паузы, не сразу
        screamTimer = 0f; // Крик готов сработать при обнаружении
        wasSeeing = false; // На старте игрока не видели
    }

    private void Update() // Каждый кадр
    {
        if (monster == null) return; // Без ИИ работать не с чем

        bool sees = monster.isActivated && vision != null && vision.CanSeePlayer(); // Видит ли игрока (и вообще активен)

        if (sees) // Видит игрока — режим крика
        {
            if (!wasSeeing) // Только что обнаружил
            {
                if (screamOnSpot) PlayScream(true); // Крик при обнаружении — прерывает текущий голос

                screamTimer = RandomScreamInterval(); // Заводим таймер повторов
            }
            else if (screamRepeats) // Продолжает видеть — повторяем крик
            {
                screamTimer -= Time.deltaTime; // Уменьшаем таймер

                if (screamTimer <= 0f) // Пора крикнуть снова
                {
                    if (PlayScream(false)) screamTimer = RandomScreamInterval(); // Крик только если предыдущий доиграл; интервал сбрасываем лишь при реальном крике
                }
            }

            idleTimer = RandomIdleInterval(); // Сбрасываем паузу плача/говора (после потери из виду будет задержка)
        }
        else if (CanIdleVocalize()) // Не видит и находится в спокойном состоянии — плач/говор
        {
            idleTimer -= Time.deltaTime; // Уменьшаем таймер

            if (idleTimer <= 0f) // Пора подать голос
            {
                if (PlayIdleVocal()) idleTimer = RandomIdleInterval(); // Играем только если голос свободен; иначе повторим в след. кадре
            }
        }
        else // Активная погоня без прямого контакта / атака / выключен — по idle молчим
        {
            idleTimer = RandomIdleInterval(); // Держим паузу наготове
        }

        wasSeeing = sees; // Запоминаем состояние
    }

    private bool CanIdleVocalize() // Можно ли сейчас плакать/говорить
    {
        if (!monster.isActivated) return false; // Выключенный монстр молчит

        switch (monster.currentState) // Только в спокойных состояниях
        {
            case MonsterState.Chase: // Погоня
            case MonsterState.Attack: // Атака
            case MonsterState.FinalMode: // Финальный режим
            case MonsterState.Disabled: // Выключен
                return false; // В этих состояниях плач/говор не играем

            default: // Idle, Patrol, InvestigateNoise, LookAroundNoise, SpecialPoint
                return true; // Здесь можно
        }
    }

    private bool PlayIdleVocal() // Проиграть плач или говор (случайно). true — если реально запустили
    {
        bool doCry = Random.value < cryChance; // Выбираем: плач или говор

        EventReference e = doCry ? cryEvent : talkEvent; // Основной выбор

        if (e.IsNull) e = doCry ? talkEvent : cryEvent; // Если пусто — берём другой

        if (e.IsNull) return false; // Оба не назначены — выходим

        if (!StartVoice(e, false)) return false; // Голос занят и не прерываем — не играли

        if (showDebugLogs) Debug.Log(gameObject.name + ": монстр " + (doCry ? "плачет" : "говорит")); // Лог

        return true; // Запустили
    }

    private bool PlayScream(bool interrupt) // Проиграть крик. interrupt — прервать текущий голос. true — если запустили
    {
        if (screamEvent.IsNull) return false; // Не назначено — выходим

        if (!StartVoice(screamEvent, interrupt)) return false; // Занято и не прерываем — не играли

        if (showDebugLogs) Debug.Log(gameObject.name + ": монстр кричит"); // Лог

        return true; // Запустили
    }

    private bool StartVoice(EventReference e, bool interrupt) // Запустить голос монофонично. true — если запустили
    {
        if (IsVoicePlaying()) // Если уже что-то звучит
        {
            if (!interrupt) return false; // Не прерываем — пропускаем, чтобы не было каши

            currentVoice.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); // Прерываем текущий голос (крик важнее)
        }

        GameObject src = monster != null ? monster.gameObject : gameObject; // Источник звука

        EventInstance inst = RuntimeManager.CreateInstance(e); // Создаём экземпляр

        RuntimeManager.AttachInstanceToGameObject(inst, src); // Привязываем к монстру — звук едет с ним

        if (occludeVoice) // Если нужна окклюзия
        {
            float occ = SC_OcclusionListener.Sample(src.transform.position, src.transform); // Замер окклюзии в точке монстра
            inst.setParameterByName(occlusionParameter, occ); // Ставим на инстанс
        }

        inst.start(); // Запускаем
        inst.release(); // Освободится сам, когда доиграет; хендл остаётся валидным, пока звучит

        currentVoice = inst; // Запоминаем как текущий голос
        hasVoice = true; // Голос активен

        return true; // Запустили
    }

    private bool IsVoicePlaying() // Играет ли сейчас голос
    {
        if (!hasVoice) return false; // Нечего проверять

        if (!currentVoice.isValid()) { hasVoice = false; return false; } // Инстанс уже освобождён

        currentVoice.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE state); // Узнаём состояние

        return state != FMOD.Studio.PLAYBACK_STATE.STOPPED; // Играет, если не остановлен
    }

    private void OnDestroy() // При уничтожении
    {
        if (hasVoice && currentVoice.isValid()) currentVoice.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); // Глушим текущий голос

        hasVoice = false; // Сбрасываем
    }

    private float RandomIdleInterval() // Случайный интервал плача/говора
    {
        return Random.Range(Mathf.Min(idleMinInterval, idleMaxInterval), Mathf.Max(idleMinInterval, idleMaxInterval)); // min..max
    }

    private float RandomScreamInterval() // Случайный интервал повторов крика
    {
        return Random.Range(Mathf.Min(screamMinInterval, screamMaxInterval), Mathf.Max(screamMinInterval, screamMaxInterval)); // min..max
    }
}
