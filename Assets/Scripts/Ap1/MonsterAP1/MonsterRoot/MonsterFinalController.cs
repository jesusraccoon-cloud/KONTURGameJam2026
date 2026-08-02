using UnityEngine; // Подключаем Unity

public class MonsterFinalController : MonoBehaviour // Отвечает только за финальные режимы монстра
{
    private enum FinalMode // Внутренние финальные режимы
    {
        None, // Финальный режим не активен
        GoToPointAndStop, // Идти к точке и остановиться
        StandingAtPoint, // Стоять на специальной точке
        KitchenChase, // Финальная погоня на кухне
        WindowThreat, // Угроза у окна
        BathroomChase // Постоянная погоня после ванной
    }

    [Header("References")] // Основные ссылки финального контроллера

    public Transform player; // Ссылка на игрока

    [Header("Kitchen Barricade By Stage")] // Настройки кухонного шкафа через ThreeStageInteractableObject

    public ThreeStageInteractableObject kitchenBarricade; // Трёхстадийный кухонный шкаф

    [Range(0, 2)] public int kitchenBlockFromStage = 1; // Начиная с какой стадии шкаф блокирует монстра

    [Range(1, 3)] public int kitchenStopBlockingAtStage = 3; // С какой стадии шкаф перестаёт блокировать; 3 означает не перестаёт

    public Transform kitchenAttackPoint; // Точка перед кухней, где монстр должен остановиться

    public WindowExitTrigger finalWindowExitTrigger; // Триггер выхода через окно

    private MonsterMovement movement; // Ссылка на движение

    private MonsterDoorOpener doorOpener; // Ссылка на открытие дверей

    private MonsterAttack attack; // Ссылка на атаку

    private MonsterPatrol patrol; // Ссылка на патруль

    private FinalMode currentMode = FinalMode.None; // Текущий финальный режим

    private Transform targetPoint; // Целевая специальная точка

    public bool IsFinalModeActive => currentMode != FinalMode.None; // Активен ли любой финальный режим

    public bool IsStandingAtSpecialPoint => currentMode == FinalMode.StandingAtPoint; // Стоит ли монстр на спецточке

    private void Awake() // Вызывается при запуске объекта
    {
        movement = GetComponent<MonsterMovement>(); // Получаем движение

        doorOpener = GetComponent<MonsterDoorOpener>(); // Получаем открытие дверей

        attack = GetComponent<MonsterAttack>(); // Получаем атаку

        patrol = GetComponent<MonsterPatrol>(); // Получаем патруль
    }

    public void Tick() // Обновить финальный режим
    {
        if (currentMode == FinalMode.None) return; // Если режима нет — выходим

        if (currentMode == FinalMode.GoToPointAndStop) TickGoToPointAndStop(); // Обновляем движение к спецточке

        if (currentMode == FinalMode.StandingAtPoint) TickStandingAtPoint(); // Обновляем стояние

        if (currentMode == FinalMode.KitchenChase) TickKitchenChase(); // Обновляем кухонную погоню

        if (currentMode == FinalMode.WindowThreat) TickWindowThreat(); // Обновляем угрозу у окна

        if (currentMode == FinalMode.BathroomChase) TickBathroomChase(); // Обновляем погоню после ванной
    }

    public void StopFinalMode() // Остановить финальные режимы
    {
        currentMode = FinalMode.None; // Сбрасываем финальный режим

        targetPoint = null; // Очищаем целевую точку

        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем стандартную скорость
    }

    public void GoToPointAndStop(Transform point) // Отправить монстра к точке и остановить там
    {
        if (point == null) // Проверяем, назначена ли точка
        {
            Debug.LogWarning("MonsterFinalController: точка движения не назначена.", gameObject); // Пишем понятную ошибку
            return; // Без точки движение невозможно
        }

        targetPoint = point; // Запоминаем точку

        currentMode = FinalMode.GoToPointAndStop; // Включаем режим движения к точке

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем стандартную скорость

        if (movement != null) movement.Resume(); // Снимаем обычную остановку NavMeshAgent, если она была

        if (movement != null && !movement.IsMovementLocked) // Проверяем, не удерживает ли монстра дверная логика
        {
            movement.MoveToImmediate(targetPoint.position); // Сразу пытаемся назначить путь к новой точке
        }

        Debug.Log(
            "MonsterFinalController: получена команда идти к точке "
            + targetPoint.name,
            gameObject
        ); // Подтверждаем получение команды
    }

    public void StandAtCurrentPoint() // Оставить монстра стоять
    {
        currentMode = FinalMode.StandingAtPoint; // Включаем стояние

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (movement != null) movement.Stop(); // Останавливаем монстра
    }

    public void StartKitchenChase() // Запустить кухонную финальную погоню
    {
        currentMode = FinalMode.KitchenChase; // Включаем кухонную погоню

        targetPoint = null; // Очищаем спецточку

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем стандартную скорость

        if (movement != null) movement.Resume(); // Разрешаем движение
    }

    public void StartWindowThreat(Transform point) // Запустить угрозу у окна
    {
        if (point == null) return; // Если точки нет — выходим

        targetPoint = point; // Запоминаем точку окна

        currentMode = FinalMode.WindowThreat; // Включаем угрозу у окна

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем стандартную скорость

        if (movement != null) movement.MoveTo(targetPoint.position); // Отправляем монстра к окну
    }

    public void StartBathroomChase() // Запустить постоянную погоню после ванной
    {
        currentMode = FinalMode.BathroomChase; // Включаем погоню после ванной

        targetPoint = null; // Очищаем целевую точку

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем стандартную скорость

        if (movement != null) movement.Resume(); // Разрешаем движение
    }

    private void TickGoToPointAndStop() // Обновить движение к спецточке
    {
        if (targetPoint == null) // Проверяем, не потерялась ли точка
        {
            Debug.LogWarning("MonsterFinalController: потеряна целевая точка движения.", gameObject); // Пишем причину остановки
            StopFinalMode(); // Без точки завершаем текущий финальный режим
            return; // Дальше двигаться некуда
        }

        if (doorOpener != null) doorOpener.TryOpenDoorAhead(); // Открываем двери по пути

        if (movement == null) return; // Если движения нет — выходим

        if (movement.IsMovementLocked) return; // Пока дверная логика удерживает монстра, ждём разблокировки

        movement.RestoreDefaultSpeed(); // Гарантированно возвращаем скорость после прежней остановки

        movement.MoveTo(targetPoint.position); // Повторно назначаем путь каждый кадр с внутренним ограничением repath

        if (!movement.HasArrived(0.4f)) return; // Если монстр ещё не дошёл — продолжаем движение

        movement.Stop(); // Останавливаем монстра после реального прибытия

        currentMode = FinalMode.StandingAtPoint; // Переводим в постоянный режим стояния

        Debug.Log(
            "MonsterFinalController: монстр дошёл до точки "
            + targetPoint.name
            + " и остановился.",
            gameObject
        ); // Подтверждаем прибытие
    }

    private void TickStandingAtPoint() // Обновить стояние на точке
    {
        if (movement != null) movement.Stop(); // Гарантированно держим монстра на месте

        if (patrol != null) patrol.isPatrolActive = false; // Гарантированно не даём патрулю включиться
    }

    private void TickKitchenChase() // Обновить кухонную погоню
    {
        if (player == null) return; // Если игрока нет — выходим

        if (doorOpener != null) doorOpener.TryOpenDoorAhead(); // Открываем двери по пути

        if (attack != null && attack.IsPlayerInAttackDistance()) // Если игрок рядом
        {
            attack.StartAttack(); // Запускаем атаку

            return; // Выходим
        }

        bool barricadeBlocksMonster = IsKitchenBarricadeBlocking(); // Проверяем текущую стадию кухонного шкафа

        if (barricadeBlocksMonster && kitchenAttackPoint != null) // Если шкаф блокирует проход и точка назначена
        {
            if (movement != null) movement.MoveTo(kitchenAttackPoint.position); // Идём к точке перед баррикадой

            if (movement != null && movement.HasArrived(0.3f)) movement.Stop(); // Останавливаемся у баррикады

            return; // Не идём сквозь баррикаду
        }

        if (movement != null) movement.MoveTo(player.position); // Если баррикады нет — идём за игроком
    }

    private bool IsKitchenBarricadeBlocking() // Проверить, блокирует ли текущая стадия шкафа проход
    {
        if (kitchenBarricade == null) return false; // Если шкаф не назначен, проход считается свободным

        int currentStage = kitchenBarricade.CurrentStage; // Получаем текущую стадию напрямую из ThreeStageInteractableObject

        int blockFromStage = Mathf.Clamp(kitchenBlockFromStage, 0, 2); // Защищаем нижнюю границу стадии блокировки

        int stopBlockingAtStage = Mathf.Clamp(kitchenStopBlockingAtStage, 1, 3); // Значение 3 означает блокировку до конца

        return currentStage >= blockFromStage && currentStage < stopBlockingAtStage; // Возвращаем итоговое состояние баррикады
    }

    private void TickWindowThreat() // Обновить угрозу у окна
    {
        if (player == null) return; // Если игрока нет — выходим

        if (targetPoint == null) return; // Если точки окна нет — выходим

        if (doorOpener != null) doorOpener.TryOpenDoorAhead(); // Открываем двери по пути

        bool playerStartedExit = finalWindowExitTrigger != null && finalWindowExitTrigger.playerStartedWindowExit; // Проверяем, начал ли игрок выход

        if (playerStartedExit) // Если игрок начал перелезать
        {
            if (movement != null) movement.MoveTo(targetPoint.position); // Идём к точке окна

            if (movement != null && movement.HasArrived(0.3f)) StandAtCurrentPoint(); // Если дошли — стоим у окна

            return; // Не продолжаем обычную погоню
        }

        if (attack != null && attack.IsPlayerInAttackDistance()) // Если игрок рядом
        {
            attack.StartAttack(); // Атакуем

            return; // Выходим
        }

        if (movement != null) movement.MoveTo(player.position); // Пока игрок не начал перелезать — идём за ним
    }

    private void TickBathroomChase() // Обновить погоню после ванной
    {
        if (player == null) return; // Если игрока нет — выходим

        if (doorOpener != null) doorOpener.TryOpenDoorAhead(); // Открываем двери по пути

        if (attack != null && attack.IsPlayerInAttackDistance()) // Если игрок рядом
        {
            attack.StartAttack(); // Атакуем

            return; // Выходим
        }

        if (movement != null) movement.MoveTo(player.position); // Постоянно идём за игроком

        if (patrol != null) patrol.isPatrolActive = false; // Не даём патрулю включиться
    }
}