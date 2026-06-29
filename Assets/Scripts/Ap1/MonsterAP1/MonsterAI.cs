using UnityEngine; // Подключаем Unity

public class MonsterAI : MonoBehaviour // Центральный фасад монстра для внешних систем
{
    [Header("Main State")] // Блок основного состояния
    public bool isActivated = false; // Активен ли монстр

    public MonsterState currentState = MonsterState.Disabled; // Текущее состояние монстра

    [Header("References")] // Блок ссылок
    public Transform player; // Ссылка на игрока

    public PlayerHideController playerHide; // Ссылка на прятки игрока

    public MonsterMovement movement; // Система движения

    public MonsterVision vision; // Система зрения

    public MonsterHearing hearing; // Система слуха

    public MonsterDoorOpener doorOpener; // Система открытия дверей

    public MonsterAttack attack; // Система атаки

    public MonsterPatrol patrol; // Система патруля

    public MonsterFinalController finalController; // Система финальных режимов

    [Header("Chase")] // Блок обычной погони
    public float loseTime = 3f; // Время потери игрока

    private Vector3 lastSeenPosition; // Последняя позиция игрока

    private float loseTimer = 0f; // Таймер потери игрока

    private void Reset() // Автозаполнение при добавлении скрипта
    {
        movement = GetComponent<MonsterMovement>(); // Ищем движение

        vision = GetComponent<MonsterVision>(); // Ищем зрение

        hearing = GetComponent<MonsterHearing>(); // Ищем слух

        doorOpener = GetComponent<MonsterDoorOpener>(); // Ищем открытие дверей

        attack = GetComponent<MonsterAttack>(); // Ищем атаку

        patrol = GetComponent<MonsterPatrol>(); // Ищем патруль

        finalController = GetComponent<MonsterFinalController>(); // Ищем финальный контроллер
    }

    private void Awake() // Запуск объекта
    {
        AutoFindComponents(); // Автоматически находим компоненты

        SyncSharedReferences(); // Прокидываем общие ссылки в дочерние системы
    }

    private void Update() // Каждый кадр
    {
        if (!isActivated) // Если монстр не активен
        {
            HandleDisabledState(); // Обрабатываем выключенное состояние

            return; // Выходим
        }

        if (attack != null && attack.IsAttacking) // Если идёт атака
        {
            currentState = MonsterState.Attack; // Ставим состояние атаки

            return; // Не выполняем другую логику
        }

        if (finalController != null && finalController.IsFinalModeActive) // Если активен финальный режим
        {
            currentState = MonsterState.FinalMode; // Ставим финальное состояние

            finalController.Tick(); // Обновляем финальный режим

            return; // Обычная логика не работает во время финала
        }

        if (doorOpener != null) doorOpener.TryOpenDoorAhead(); // Пробуем открыть дверь перед монстром

        if (vision != null && vision.CanSeePlayer()) // Если монстр видит игрока
        {
            StartChaseInternal(); // Запускаем обычную погоню
        }

        if (currentState == MonsterState.Chase) // Если идёт обычная погоня
        {
            TickChase(); // Обновляем погоню

            return; // Выходим
        }

        if (hearing != null && hearing.IsBusy) // Если слуховая система занята
        {
            currentState = MonsterState.InvestigateNoise; // Ставим состояние шума

            hearing.Tick(); // Обновляем слуховую реакцию

            return; // Выходим
        }

        if (patrol != null && patrol.isPatrolActive) // Если патруль активен
        {
            currentState = MonsterState.Patrol; // Ставим состояние патруля

            return; // Патруль сам обновляется в MonsterPatrol
        }

        currentState = MonsterState.Idle; // Если ничего не происходит — монстр стоит
    }

    private void AutoFindComponents() // Найти компоненты автоматически
    {
        if (movement == null) movement = GetComponent<MonsterMovement>(); // Находим движение

        if (vision == null) vision = GetComponent<MonsterVision>(); // Находим зрение

        if (hearing == null) hearing = GetComponent<MonsterHearing>(); // Находим слух

        if (doorOpener == null) doorOpener = GetComponent<MonsterDoorOpener>(); // Находим открытие дверей

        if (attack == null) attack = GetComponent<MonsterAttack>(); // Находим атаку

        if (patrol == null) patrol = GetComponent<MonsterPatrol>(); // Находим патруль

        if (finalController == null) finalController = GetComponent<MonsterFinalController>(); // Находим финальный контроллер
    }

    private void SyncSharedReferences() // Синхронизировать ссылки между системами
    {
        if (vision != null) vision.player = player; // Передаём игрока в зрение

        if (vision != null) vision.playerHide = playerHide; // Передаём прятки в зрение

        if (attack != null) attack.player = player; // Передаём игрока в атаку

        if (finalController != null) finalController.player = player; // Передаём игрока в финальный контроллер
    }

    private void HandleDisabledState() // Логика выключенного монстра
    {
        currentState = MonsterState.Disabled; // Ставим состояние Disabled

        if (patrol != null) patrol.isPatrolActive = false; // Выключаем патруль

        if (movement != null) movement.Stop(); // Останавливаем движение
    }

    private void StartChaseInternal() // Внутренний запуск обычной погони
    {
        if (player == null) return; // Если игрока нет — выходим

        if (hearing != null) hearing.StopHearingLogic(); // Отключаем реакцию на шум

        if (finalController != null) finalController.StopFinalMode(); // Отключаем финальный режим, если это обычная погоня

        if (patrol != null) patrol.isPatrolActive = false; // Выключаем патруль

        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем стандартную скорость

        currentState = MonsterState.Chase; // Ставим состояние погони

        loseTimer = 0f; // Сбрасываем таймер потери

        lastSeenPosition = player.position; // Запоминаем позицию игрока
    }

    private void TickChase() // Обновить обычную погоню
    {
        if (player == null) return; // Если игрока нет — выходим

        if (attack != null && attack.IsPlayerInAttackDistance()) // Если игрок в зоне атаки
        {
            StartAttackInternal(); // Запускаем атаку

            return; // Выходим
        }

        if (vision != null && vision.CanSeePlayer()) // Если игрок всё ещё виден
        {
            loseTimer = 0f; // Сбрасываем таймер потери

            lastSeenPosition = player.position; // Обновляем последнюю позицию игрока

            if (movement != null) movement.MoveTo(player.position); // Идём за игроком

            return; // Выходим
        }

        loseTimer += Time.deltaTime; // Увеличиваем таймер потери

        if (loseTimer < loseTime) // Если монстр ещё помнит игрока
        {
            if (movement != null) movement.MoveTo(lastSeenPosition); // Идём к последней позиции

            return; // Выходим
        }

        currentState = MonsterState.Patrol; // Возвращаемся в патруль

        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем стандартную скорость

        if (patrol != null) patrol.StartPatrol(); // Запускаем патруль
    }

    private void StartAttackInternal() // Внутренний запуск атаки
    {
        currentState = MonsterState.Attack; // Ставим состояние атаки

        if (patrol != null) patrol.isPatrolActive = false; // Выключаем патруль

        if (movement != null) movement.Stop(); // Останавливаем монстра

        if (hearing != null) hearing.StopHearingLogic(); // Отключаем шум

        if (finalController != null) finalController.StopFinalMode(); // Отключаем финальный режим

        if (attack != null) attack.StartAttack(); // Запускаем атаку
    }

    public void ActivateMonster() // Публичная активация монстра
    {
        if (!gameObject.activeInHierarchy) return; // Если объект выключен — выходим

        isActivated = true; // Активируем монстра

        currentState = MonsterState.Patrol; // Ставим состояние патруля

        if (hearing != null) hearing.StopHearingLogic(); // Очищаем реакцию на шум

        if (finalController != null) finalController.StopFinalMode(); // Сначала сбрасываем финальные режимы

        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем стандартную скорость

        if (movement != null) movement.Resume(); // Разрешаем движение

        if (patrol != null) patrol.StartPatrol(); // Запускаем патруль
    }

    public void ReactToNoise(Vector3 noisePosition, int noisePower) // Публичная реакция на шум
    {
        if (currentState == MonsterState.Chase) return; // Если монстр гонится — шум игнорируем

        if (finalController != null && finalController.IsFinalModeActive) return; // Если финальный режим — шум игнорируем

        if (hearing == null) return; // Если слуха нет — выходим

        hearing.ReactToNoise(noisePosition, noisePower, isActivated); // Передаём шум в слух, но движение разрешаем только после активации
    }

    public void HearNoise(Vector3 noisePosition) // Старый метод для совместимости
    {
        ReactToNoise(noisePosition, 6); // Старый шум считаем силой 6
    }

    public void GoToPointAndStop(Transform targetPoint) // Публичный метод: идти к точке и остановиться
    {
        if (targetPoint == null) return; // Если точки нет — выходим

        isActivated = true; // Активируем монстра

        currentState = MonsterState.FinalMode; // Ставим финальный режим

        if (hearing != null) hearing.StopHearingLogic(); // Отключаем шум

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (finalController != null) finalController.GoToPointAndStop(targetPoint); // Запускаем движение к точке
    }

    public void StartFinalKitchenChase() // Публичный метод: финальная кухонная погоня
    {
        isActivated = true; // Активируем монстра

        currentState = MonsterState.FinalMode; // Ставим финальный режим

        if (hearing != null) hearing.StopHearingLogic(); // Отключаем шум

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (finalController != null) finalController.StartKitchenChase(); // Запускаем кухонную погоню
    }

    public void StartFinalWindowThreat(Transform targetPoint) // Публичный метод: угроза у окна
    {
        if (targetPoint == null) return; // Если точки нет — выходим

        isActivated = true; // Активируем монстра

        currentState = MonsterState.FinalMode; // Ставим финальный режим

        if (hearing != null) hearing.StopHearingLogic(); // Отключаем шум

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (finalController != null) finalController.StartWindowThreat(targetPoint); // Запускаем угрозу у окна
    }

    public void ForceChasePlayer() // Публичный метод: постоянная финальная погоня после ванной
    {
        isActivated = true; // Активируем монстра

        currentState = MonsterState.FinalMode; // Ставим финальный режим

        if (hearing != null) hearing.StopHearingLogic(); // Отключаем шум

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (finalController != null) finalController.StartBathroomChase(); // Запускаем погоню после ванной
    }

    public void StandAtFinalBlockPoint() // Публичный метод: стоять на финальной точке
    {
        isActivated = true; // Монстр остаётся активным

        currentState = MonsterState.FinalMode; // Ставим финальный режим

        if (hearing != null) hearing.StopHearingLogic(); // Отключаем шум

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (finalController != null) finalController.StandAtCurrentPoint(); // Ставим монстра стоять
    }
}