using UnityEngine; // Подключаем Unity

public class MonsterHearing : MonoBehaviour // Отвечает только за слух монстра и тревогу квартиры от шума
{
    [Header("Noise Movement")] // Блок настроек движения к шуму
    public float noiseArriveDistance = 1.2f; // Дистанция прибытия к шуму

    public float noiseWaitTime = 4f; // Время ожидания на месте шума

    public float suspiciousLookTime = 2f; // Время осмотра при шуме 4

    public float normalNoiseSpeed = 2.5f; // Скорость реакции на шум 5-6

    public float loudNoiseSpeed = 4.5f; // Скорость реакции на шум 7-10

    [Header("Early Noise Alarm 3/3")] // Блок ранней тревоги квартиры
    public ApartmentFinalSequence finalSequence; // Ссылка на сценарий квартиры

    public bool enableNoiseAlarmBeforeActivation = true; // Разрешить тревогу до активации монстра

    public int alarmNoiseThreshold = 4; // С какой силы шум считается тревожным

    public int alarmNoiseLimit = 3; // Сколько тревожных реакций нужно для активации

    public float alarmCooldown = 2f; // Задержка между засчитанными шумами

    public int currentAlarmCount = 0; // Текущий счётчик тревоги

    private float lastAlarmTime = -999f; // Время последней засчитанной тревоги

    private MonsterMovement movement; // Ссылка на движение

    private MonsterPatrol patrol; // Ссылка на патруль

    private Vector3 noisePosition; // Последняя позиция шума

    private float timer; // Универсальный таймер

    private bool isLookingAround; // Монстр сейчас осматривается

    private bool isInvestigating; // Монстр сейчас идёт к шуму

    private bool isWaitingAtNoise; // Монстр стоит на месте шума

    public bool IsBusy => isLookingAround || isInvestigating || isWaitingAtNoise; // Занят ли монстр слуховой реакцией

    private void Awake() // Вызывается при запуске объекта
    {
        movement = GetComponent<MonsterMovement>(); // Получаем MonsterMovement

        patrol = GetComponent<MonsterPatrol>(); // Получаем MonsterPatrol
    }

    public void ReactToNoise(Vector3 newNoisePosition, int noisePower, bool allowPhysicalReaction) // Реакция на шум с учётом активности монстра
    {
        noisePower = Mathf.Clamp(noisePower, 1, 10); // Ограничиваем силу шума от 1 до 10

        if (noisePower <= 3) return; // Шум 0-3 игнорируется

        RegisterNoiseAlarm(noisePower); // Сначала засчитываем тревогу квартиры

        if (!allowPhysicalReaction) return; // Если монстр ещё не активен — он слышит, но не двигается

        if (noisePower == 4) // Если шум равен 4
        {
            StartLookAround(newNoisePosition); // Запускаем осмотр

            return; // Выходим
        }

        if (noisePower >= 5 && noisePower <= 6) // Если шум 5-6
        {
            StartInvestigation(newNoisePosition, normalNoiseSpeed); // Идём к шуму обычной скоростью

            return; // Выходим
        }

        if (noisePower >= 7) // Если шум 7-10
        {
            StartInvestigation(newNoisePosition, loudNoiseSpeed); // Идём к шуму быстро

            return; // Выходим
        }
    }

    public void ReactToNoise(Vector3 newNoisePosition, int noisePower) // Старый вариант метода для совместимости
    {
        ReactToNoise(newNoisePosition, noisePower, true); // По умолчанию разрешаем физическую реакцию
    }

    private void RegisterNoiseAlarm(int noisePower) // Засчитать тревожный шум
    {
        if (!enableNoiseAlarmBeforeActivation) return; // Если тревога выключена — выходим

        if (finalSequence == null) return; // Если сценарий квартиры не назначен — выходим

        if (noisePower < alarmNoiseThreshold) return; // Если шум слабее порога — выходим

        if (Time.time - lastAlarmTime < alarmCooldown) return; // Если шумы идут слишком часто — не считаем

        lastAlarmTime = Time.time; // Запоминаем время тревоги

        currentAlarmCount = Mathf.Clamp(currentAlarmCount + 1, 0, alarmNoiseLimit); // Увеличиваем счётчик тревоги

        Debug.Log("Тревога квартиры: " + currentAlarmCount + "/" + alarmNoiseLimit + " | шум: " + noisePower); // Пишем лог

        if (currentAlarmCount >= alarmNoiseLimit) // Если тревога достигла лимита
        {
            finalSequence.StartEarlyHallDoorBreakSequence(); // Просим сценарий квартиры запустить событие 4/6
        }
    }

    public void Tick() // Обновление слуховой логики
    {
        if (isLookingAround) TickLookAround(); // Если осматриваемся — обновляем осмотр

        if (isInvestigating) TickInvestigation(); // Если идём к шуму — обновляем расследование
    }

    public void StopHearingLogic() // Остановить реакцию на шум
    {
        isLookingAround = false; // Выключаем осмотр

        isInvestigating = false; // Выключаем движение к шуму

        isWaitingAtNoise = false; // Выключаем ожидание

        timer = 0f; // Сбрасываем таймер

        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем стандартную скорость
    }

    private void StartLookAround(Vector3 newNoisePosition) // Начать осмотр
    {
        noisePosition = newNoisePosition; // Запоминаем позицию шума

        isLookingAround = true; // Включаем осмотр

        isInvestigating = false; // Выключаем расследование

        isWaitingAtNoise = false; // Выключаем ожидание

        timer = 0f; // Сбрасываем таймер

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (movement != null) movement.Stop(); // Останавливаем монстра

        Vector3 directionToNoise = noisePosition - transform.position; // Считаем направление к шуму

        directionToNoise.y = 0f; // Убираем вертикаль

        if (directionToNoise.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(directionToNoise.normalized); // Поворачиваем монстра к шуму
    }

    private void StartInvestigation(Vector3 newNoisePosition, float speed) // Начать движение к шуму
    {
        noisePosition = newNoisePosition; // Запоминаем позицию шума

        isLookingAround = false; // Выключаем осмотр

        isInvestigating = true; // Включаем расследование

        isWaitingAtNoise = false; // Пока не ждём

        timer = 0f; // Сбрасываем таймер

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (movement != null) movement.SetSpeed(speed); // Ставим скорость реакции на шум

        if (movement != null) movement.MoveTo(noisePosition); // Отправляем монстра к шуму
    }

    private void TickLookAround() // Обновить осмотр
    {
        timer += Time.deltaTime; // Увеличиваем таймер

        if (timer < suspiciousLookTime) return; // Если время ещё не вышло — ждём

        isLookingAround = false; // Выключаем осмотр

        timer = 0f; // Сбрасываем таймер

        if (patrol != null) patrol.StartPatrol(); // Возвращаем патруль
    }

    private void TickInvestigation() // Обновить движение к шуму
    {
        if (movement == null) return; // Если движения нет — выходим

        if (!isWaitingAtNoise) // Если монстр ещё идёт
        {
            if (!movement.HasArrived(noiseArriveDistance)) return; // Если не дошёл — ждём

            isWaitingAtNoise = true; // Включаем ожидание

            timer = 0f; // Сбрасываем таймер

            movement.Stop(); // Останавливаем монстра

            return; // Выходим
        }

        timer += Time.deltaTime; // Увеличиваем таймер ожидания

        if (timer < noiseWaitTime) return; // Если ждать ещё рано — выходим

        StopHearingLogic(); // Останавливаем слуховую логику

        if (patrol != null) patrol.StartPatrol(); // Возвращаем патруль
    }
}