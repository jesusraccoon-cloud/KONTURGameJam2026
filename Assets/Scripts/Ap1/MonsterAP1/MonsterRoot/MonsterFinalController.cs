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

    public Transform player; // Ссылка на игрока

    public GameObject kitchenBarricadeObject; // Баррикада кухни

    public Transform kitchenAttackPoint; // Точка перед кухней

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
        if (point == null) return; // Если точки нет — выходим

        targetPoint = point; // Запоминаем точку

        currentMode = FinalMode.GoToPointAndStop; // Включаем режим движения к точке

        if (patrol != null) patrol.isPatrolActive = false; // Отключаем патруль

        if (movement != null) movement.RestoreDefaultSpeed(); // Возвращаем стандартную скорость

        if (movement != null) movement.MoveTo(targetPoint.position); // Отправляем монстра к точке
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
        if (doorOpener != null) doorOpener.TryOpenDoorAhead(); // Открываем двери по пути

        if (movement == null) return; // Если движения нет — выходим

        if (!movement.HasArrived(0.4f)) return; // Если монстр ещё не дошёл — ждём

        movement.Stop(); // Останавливаем монстра

        currentMode = FinalMode.StandingAtPoint; // Переводим в режим стояния
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

        bool barricadeBlocksMonster = kitchenBarricadeObject != null && kitchenBarricadeObject.activeInHierarchy; // Проверяем баррикаду

        if (barricadeBlocksMonster && kitchenAttackPoint != null) // Если баррикада стоит и точка назначена
        {
            if (movement != null) movement.MoveTo(kitchenAttackPoint.position); // Идём к точке перед баррикадой

            if (movement != null && movement.HasArrived(0.3f)) movement.Stop(); // Останавливаемся у баррикады

            return; // Не идём сквозь баррикаду
        }

        if (movement != null) movement.MoveTo(player.position); // Если баррикады нет — идём за игроком
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