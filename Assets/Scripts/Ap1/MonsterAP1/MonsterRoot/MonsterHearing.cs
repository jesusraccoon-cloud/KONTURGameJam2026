using UnityEngine; // Подключаем основные Unity-классы

public class MonsterHearing : MonoBehaviour // Отвечает за слух, поиск и осмотр монстра
{
    [Header("References")] public Animator animator; // Animator дочерней модели LolyGirl
    [Header("Noise Movement")] public float noiseArriveDistance = 1.2f; // Дистанция прибытия к шуму
    public float noiseWaitTime = 4f; // Длительность осмотра после сильного шума
    public float suspiciousLookTime = 2f; // Длительность осмотра при шуме 4
    public float normalNoiseSpeed = 2.5f; // Скорость к шуму 5–6
    public float loudNoiseSpeed = 4.5f; // Скорость к шуму 7–10

    [Header("Early Noise Alarm 3/3")] public ApartmentFinalSequence finalSequence; // Сценарий квартиры
    public bool enableNoiseAlarmBeforeActivation = true; // Разрешить тревогу до активации
    public int alarmNoiseThreshold = 4; // Минимальная сила тревожного шума
    public int alarmNoiseLimit = 3; // Количество шумов для запуска события
    public float alarmCooldown = 2f; // Задержка между шумами
    public int currentAlarmCount = 0; // Текущий счётчик тревоги

    private enum HearingState { None, Investigating, LookingAround } // Состояния слуха
    private static readonly int LookAroundHash = Animator.StringToHash("LookAround"); // Trigger осмотра
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving"); // Bool движения
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning"); // Bool бега

    private MonsterMovement movement; // Система движения
    private MonsterPatrol patrol; // Система патруля
    private HearingState state = HearingState.None; // Текущее состояние
    private Vector3 targetPosition; // Позиция интереса
    private float timer; // Таймер действия
    private float lookDuration; // Длительность осмотра
    private float lastAlarmTime = -999f; // Время последней тревоги

    public bool IsBusy => state != HearingState.None; // Занят ли монстр слуховой реакцией

    private void Reset() => FindReferences(); // Автозаполнение при добавлении
    private void Awake() => FindReferences(); // Автозаполнение при запуске

    private void FindReferences() // Находит существующие компоненты
    {
        if (movement == null) movement = GetComponent<MonsterMovement>(); // Ищем движение
        if (patrol == null) patrol = GetComponent<MonsterPatrol>(); // Ищем патруль
        if (animator == null) animator = GetComponentInChildren<Animator>(true); // Ищем Animator
    }

    public void ReactToNoise(Vector3 newNoisePosition, int noisePower, bool allowPhysicalReaction) // Получает шум
    {
        noisePower = Mathf.Clamp(noisePower, 1, 10); // Ограничиваем силу шума
        if (noisePower <= 3) return; // Шум 1–3 игнорируем
        RegisterNoiseAlarm(noisePower); // Засчитываем тревогу
        if (!allowPhysicalReaction) return; // До активации физически не реагируем

        if (noisePower == 4) { BeginLookAround(newNoisePosition, suspiciousLookTime); return; } // Слабый шум: осмотр на месте
        float speed = noisePower <= 6 ? normalNoiseSpeed : loudNoiseSpeed; // Выбираем скорость
        BeginInvestigation(newNoisePosition, speed); // Идём к сильному шуму
    }

    public void ReactToNoise(Vector3 newNoisePosition, int noisePower) => ReactToNoise(newNoisePosition, noisePower, true); // Совместимость
    public void SearchAtPosition(Vector3 position) => BeginLookAround(position, noiseWaitTime); // Осмотр после потери игрока

    public void Tick() // Обновляет слуховую реакцию
    {
        if (state == HearingState.Investigating) TickInvestigation(); // Движение к шуму
        else if (state == HearingState.LookingAround) TickLookAround(); // Стоячий осмотр
    }

    public void StopHearingLogic() // Прерывает слуховую реакцию
    {
        state = HearingState.None; // Сбрасываем состояние
        timer = 0f; // Сбрасываем таймер
        lookDuration = 0f; // Сбрасываем длительность
        if (animator != null) animator.ResetTrigger(LookAroundHash); // Убираем незапущенный Trigger
        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем скорость
    }

    private void BeginInvestigation(Vector3 position, float speed) // Начинает движение к шуму
    {
        state = HearingState.Investigating; // Включаем расследование
        targetPosition = position; // Запоминаем цель
        timer = 0f; // Сбрасываем таймер
        if (animator != null) animator.ResetTrigger(LookAroundHash); // Убираем старый Trigger
        if (patrol != null) patrol.isPatrolActive = false; // Останавливаем патруль
        if (movement != null) movement.SetSpeed(speed); // Ставим скорость
        if (movement != null) movement.MoveTo(targetPosition); // Идём к цели
    }

    private void BeginLookAround(Vector3 position, float duration) // Останавливает монстра и запускает осмотр
    {
        state = HearingState.LookingAround; // Включаем осмотр
        targetPosition = position; // Запоминаем точку интереса
        timer = 0f; // Сбрасываем таймер
        lookDuration = Mathf.Max(0.1f, duration); // Защищаемся от нулевой длительности
        if (patrol != null) patrol.isPatrolActive = false; // Останавливаем патруль
        if (movement != null) movement.Stop(); // Полностью останавливаем NavMeshAgent
        TurnTowardsTarget(); // Поворачиваемся к точке интереса

        if (animator == null) return; // Без Animator выходим
        animator.SetBool(IsMovingHash, false); // Немедленно выключаем Walk
        animator.SetBool(IsRunningHash, false); // Немедленно выключаем Run
        animator.ResetTrigger(LookAroundHash); // Сбрасываем старый Trigger
        animator.SetTrigger(LookAroundHash); // Запускаем MonsterLookAround
    }

    private void TickInvestigation() // Обновляет движение к шуму
    {
        if (movement == null) return; // Без движения выходим
        if (!movement.HasArrived(noiseArriveDistance)) return; // Пока не дошли — идём
        BeginLookAround(targetPosition, noiseWaitTime); // Прибыли: остановились и осматриваемся
    }

    private void TickLookAround() // Обновляет осмотр
    {
        timer += Time.deltaTime; // Увеличиваем таймер
        if (timer < lookDuration) return; // Пока время не вышло — продолжаем осмотр
        state = HearingState.None; // Завершаем реакцию
        timer = 0f; // Сбрасываем таймер
        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем скорость
        if (patrol != null) patrol.StartPatrol(); // Возвращаем патруль
    }

    private void TurnTowardsTarget() // Поворачивает монстра к точке интереса
    {
        Vector3 direction = targetPosition - transform.position; // Получаем направление
        direction.y = 0f; // Убираем вертикаль
        if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction.normalized); // Поворачиваем Monster
    }

    private void RegisterNoiseAlarm(int noisePower) // Засчитывает тревожный шум
    {
        if (!enableNoiseAlarmBeforeActivation || finalSequence == null) return; // Проверяем настройки
        if (noisePower < alarmNoiseThreshold) return; // Слабый шум не считаем
        if (Time.time < lastAlarmTime + alarmCooldown) return; // Частый шум не считаем
        lastAlarmTime = Time.time; // Запоминаем время
        currentAlarmCount = Mathf.Clamp(currentAlarmCount + 1, 0, alarmNoiseLimit); // Увеличиваем счётчик
        Debug.Log("Тревога квартиры: " + currentAlarmCount + "/" + alarmNoiseLimit + " | шум: " + noisePower); // Пишем лог
        if (currentAlarmCount >= alarmNoiseLimit) finalSequence.StartEarlyHallDoorBreakSequence(); // Запускаем событие
    }
}