using UnityEngine; // Подключаем основные Unity-классы

public class MonsterAI : MonoBehaviour // Центральный контроллер поведения монстра
{
    [Header("Main State")] public bool isActivated = false; // Активен ли монстр
    public MonsterState currentState = MonsterState.Disabled; // Текущее состояние

    [Header("References")] public Transform player; // Игрок
    public PlayerHideController playerHide; // Система пряток
    public MonsterMovement movement; // Движение
    public MonsterVision vision; // Зрение
    public MonsterHearing hearing; // Слух и осмотр
    public MonsterDoorOpener doorOpener; // Открытие дверей
    public MonsterAttack attack; // Атака
    public MonsterPatrol patrol; // Патруль
    public MonsterFinalController finalController; // Финальные режимы

    [Header("Chase And Search")] public float loseTime = 5f; // Максимальное время движения к последней позиции
    public float lastSeenArriveDistance = 0.5f; // Дистанция прибытия к последней позиции

    private Vector3 lastSeenPosition; // Последняя известная позиция игрока
    private float loseTimer; // Таймер после потери игрока

    private bool blockCloseRangeReactionInFinalMode = false; // Запрещает ближней реакции прерывать защищённое финальное ожидание

    private void Reset() => AutoFindComponents(); // Автозаполнение при добавлении
    private void Awake() { AutoFindComponents(); SyncSharedReferences(); } // Заполняем ссылки при запуске

    private void Update() // Обновляет поведение каждый кадр
    {
        if (!isActivated) { HandleDisabledState(); return; } // Выключенный монстр ничего не делает
        if (attack != null && attack.IsAttacking) { currentState = MonsterState.Attack; return; } // Во время атаки ждём
        if (doorOpener != null && doorOpener.IsHandlingDoor) return; // Во время открытия двери ждём

        if (finalController != null && finalController.IsFinalModeActive) // Проверяем финальный режим
        {
            bool closeRangeCanInterrupt =
                !blockCloseRangeReactionInFinalMode
                && CanDetectPlayerAtCloseRange(); // Проверяем, разрешено ли ближней реакции сорвать финальный режим

            if (closeRangeCanInterrupt) // Если текущий финальный режим разрешает реакцию на близкого игрока
            {
                StartChaseInternal(); // Прерываем финальный режим и запускаем обычную погоню
            }
            else // Если реакция запрещена или игрок не вошёл в ближний радиус
            {
                currentState = MonsterState.FinalMode; // Ставим финальное состояние
                finalController.Tick(); // Обновляем текущий финальный режим
                return; // Обычную логику не выполняем
            }
        }

        if (doorOpener != null && doorOpener.TryOpenDoorAhead()) return; // Закрытая дверь найдена: ждём
        if (vision != null && vision.CanSeePlayer()) StartChaseInternal(); // Видим игрока: включаем погоню

        if (currentState == MonsterState.Chase) { TickChase(); return; } // Обновляем погоню
        if (hearing != null && hearing.IsBusy) { currentState = MonsterState.InvestigateNoise; hearing.Tick(); return; } // Обновляем поиск
        if (patrol != null && patrol.isPatrolActive) { currentState = MonsterState.Patrol; return; } // Патруль работает сам

        currentState = MonsterState.Idle; // Иначе монстр стоит
    }

    private bool CanDetectPlayerAtCloseRange() // Проверяет только ближнюю круговую зону MonsterVision
    {
        if (vision == null) return false; // Без MonsterVision проверка невозможна

        if (!vision.useCloseAwareness) return false; // Ближняя зона отключена в Inspector

        if (vision.player == null) return false; // Игрок не назначен в MonsterVision

        Vector3 flatDirection = vision.player.position - vision.transform.position; // Получаем направление до игрока

        flatDirection.y = 0f; // Не учитываем разницу высоты

        float closeRadius = Mathf.Max(0f, vision.closeAwarenessRadius); // Получаем безопасный радиус ближнего обнаружения

        if (flatDirection.sqrMagnitude > closeRadius * closeRadius) return false; // Игрок находится дальше ближнего радиуса

        return vision.CanSeePlayer(); // Проверяем прятки и отсутствие стены между монстром и игроком
    }

    private void AutoFindComponents() // Находит компоненты на Monster
    {
        if (movement == null) movement = GetComponent<MonsterMovement>(); // Находим движение
        if (vision == null) vision = GetComponent<MonsterVision>(); // Находим зрение
        if (hearing == null) hearing = GetComponent<MonsterHearing>(); // Находим слух
        if (doorOpener == null) doorOpener = GetComponent<MonsterDoorOpener>(); // Находим двери
        if (attack == null) attack = GetComponent<MonsterAttack>(); // Находим атаку
        if (patrol == null) patrol = GetComponent<MonsterPatrol>(); // Находим патруль
        if (finalController == null) finalController = GetComponent<MonsterFinalController>(); // Находим финальный контроллер
    }

    private void SyncSharedReferences() // Передаёт общие ссылки
    {
        if (vision != null) vision.player = player; // Передаём игрока в зрение
        if (vision != null) vision.playerHide = playerHide; // Передаём прятки
        if (attack != null) attack.player = player; // Передаём игрока в атаку
        if (finalController != null) finalController.player = player; // Передаём игрока в финальный контроллер
    }

    private void HandleDisabledState() // Обрабатывает выключенного монстра
    {
        currentState = MonsterState.Disabled; // Ставим Disabled
        if (patrol != null) patrol.isPatrolActive = false; // Выключаем патруль
        if (movement != null) movement.Stop(); // Останавливаем движение
    }

    private void StartChaseInternal() // Начинает или продолжает погоню
    {
        if (player == null) return; // Без игрока выходим
        if (hearing != null) hearing.StopHearingLogic(); // Прерываем осмотр
        if (finalController != null) finalController.StopFinalMode(); // Прерываем обычным преследованием финал
        if (patrol != null) patrol.isPatrolActive = false; // Выключаем патруль
        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем скорость
        currentState = MonsterState.Chase; // Включаем погоню
        loseTimer = 0f; // Сбрасываем потерю
        lastSeenPosition = player.position; // Запоминаем позицию игрока
    }

    private void TickChase() // Обновляет погоню и потерю игрока
    {
        if (player == null) return; // Без игрока выходим

        if (attack != null && attack.IsPlayerInAttackDistance()) // Проверяем дистанцию атаки
        {
            StartAttackInternal(); // Запускаем атаку
            return; // Завершаем кадр
        }

        if (vision != null && vision.CanSeePlayer()) // Игрок виден
        {
            loseTimer = 0f; // Сбрасываем таймер
            lastSeenPosition = player.position; // Обновляем позицию
            if (movement != null) movement.MoveTo(player.position); // Идём за игроком
            return; // Завершаем кадр
        }

        loseTimer += Time.deltaTime; // Увеличиваем таймер после потери
        if (movement != null) movement.MoveTo(lastSeenPosition); // Идём к последней позиции

        bool arrived = movement != null && movement.HasArrived(lastSeenArriveDistance); // Проверяем прибытие
        if (!arrived && loseTimer < loseTime) return; // Пока не дошли и время не вышло — продолжаем путь

        StartLostPlayerSearch(); // Останавливаемся и осматриваемся
    }

    private void StartLostPlayerSearch() // Запускает осмотр после потери игрока
    {
        currentState = MonsterState.InvestigateNoise; // Включаем состояние поиска
        if (patrol != null) patrol.isPatrolActive = false; // Не даём патрулю включиться
        if (movement != null) movement.Stop(); // Останавливаем монстра
        if (hearing != null) hearing.SearchAtPosition(lastSeenPosition); // Осматриваем последнюю позицию
        else if (patrol != null) patrol.StartPatrol(); // Без слуха возвращаем патруль
    }

    private void StartAttackInternal() // Запускает атаку
    {
        currentState = MonsterState.Attack; // Ставим атаку
        if (patrol != null) patrol.isPatrolActive = false; // Выключаем патруль
        if (movement != null) movement.Stop(); // Останавливаем монстра
        if (hearing != null) hearing.StopHearingLogic(); // Прерываем поиск
        if (finalController != null) finalController.StopFinalMode(); // Прерываем финальный режим
        if (attack != null) attack.StartAttack(); // Запускаем атаку
    }

    public void ActivateMonster() // Активирует монстра
    {
        if (!gameObject.activeInHierarchy) return; // Выключенный объект не активируем
        blockCloseRangeReactionInFinalMode = false; // Обычная активация не должна сохранять защищённое ожидание
        isActivated = true; // Включаем монстра
        currentState = MonsterState.Patrol; // Ставим патруль
        if (hearing != null) hearing.StopHearingLogic(); // Очищаем поиск
        if (finalController != null) finalController.StopFinalMode(); // Сбрасываем финал
        if (movement != null) { movement.RestoreDefaultSpeed(); movement.Resume(); } // Возвращаем движение
        if (patrol != null) patrol.StartPatrol(); // Запускаем патруль
    }

    public void ReactToNoise(Vector3 noisePosition, int noisePower) // Передаёт шум
    {
        if (currentState == MonsterState.Chase) return; // Во время погони шум игнорируем
        if (doorOpener != null && doorOpener.IsHandlingDoor) return; // Во время двери шум не перебивает действие
        if (finalController != null && finalController.IsFinalModeActive) return; // В финале шум игнорируем
        if (hearing != null) hearing.ReactToNoise(noisePosition, noisePower, isActivated); // Передаём шум
    }

    public void SetFinalCloseRangeReactionBlocked(bool blocked) // Разрешить или запретить ближней реакции прерывать финальный режим
    {
        blockCloseRangeReactionInFinalMode = blocked; // Сохраняем требуемое состояние защиты
    }

    public void HearNoise(Vector3 noisePosition) => ReactToNoise(noisePosition, 6); // Старый метод совместимости

    public void GoToPointAndStop(Transform targetPoint) // Идти к специальной точке
    {
        if (targetPoint == null) return; // Без точки выходим
        isActivated = true; currentState = MonsterState.FinalMode; // Активируем финал
        if (hearing != null) hearing.StopHearingLogic(); // Отключаем слух
        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль
        if (finalController != null) finalController.GoToPointAndStop(targetPoint); // Передаём команду
    }

    public void StartFinalKitchenChase() // Финальная погоня на кухне
    {
        blockCloseRangeReactionInFinalMode = false; // На кухне близкий игрок должен активировать погоню
        isActivated = true; currentState = MonsterState.FinalMode; // Активируем финал
        if (hearing != null) hearing.StopHearingLogic(); // Отключаем слух
        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль
        if (finalController != null) finalController.StartKitchenChase(); // Запускаем режим
    }

    public void StartFinalWindowThreat(Transform targetPoint) // Угроза у окна
    {
        if (targetPoint == null) return; // Без точки выходим
        isActivated = true; currentState = MonsterState.FinalMode; // Активируем финал
        if (hearing != null) hearing.StopHearingLogic(); // Отключаем слух
        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль
        if (finalController != null) finalController.StartWindowThreat(targetPoint); // Запускаем режим
    }

    public void ForceChasePlayer() // Постоянная погоня после ванной
    {
        blockCloseRangeReactionInFinalMode = false; // Снимаем защищённое ожидание у окна
        isActivated = true; currentState = MonsterState.FinalMode; // Активируем финал
        if (hearing != null) hearing.StopHearingLogic(); // Отключаем слух
        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль
        if (finalController != null) finalController.StartBathroomChase(); // Запускаем режим
    }

    public void StandAtFinalBlockPoint() // Стоять на финальной точке
    {
        isActivated = true; currentState = MonsterState.FinalMode; // Активируем финал
        if (hearing != null) hearing.StopHearingLogic(); // Отключаем слух
        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль
        if (finalController != null) finalController.StandAtCurrentPoint(); // Оставляем монстра стоять
    }
}