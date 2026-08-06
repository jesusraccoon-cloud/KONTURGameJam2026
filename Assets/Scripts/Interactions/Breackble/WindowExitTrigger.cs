using UnityEngine; // Подключаем Unity-классы

public class WindowExitTrigger : MonoBehaviour, IInteractable // Универсальный двухсторонний переход через разбитое окно
{
    [Header("Availability")] // Настройки доступности окна
    public bool availableImmediately = true; // Разрешать использование окна в любой момент после разрушения

    public bool requireBrokenWindow = true; // Требовать обязательного разрушения окна перед перелезанием

    [Header("Two-Way Traversal")] // Настройки перехода в обе стороны
    public bool allowTwoWayTraversal = true; // Разрешать переход из ванной в кухню и обратно

    public bool checkAllowedSide = true; // В одностороннем режиме разрешать переход только со стороны Allowed Point

    [Header("Window")] // Разрушаемое окно
    public BreakableObject breakableWindow; // BreakableObject, который сообщает, разбито ли окно

    [Header("Player")] // Ссылка на игрока
    public CharacterController characterController; // CharacterController объекта PlayerCapsule

    [Header("Destination Points")] // Точки назначения
    public Transform exitPoint; // Куда поставить игрока при переходе со стороны Allowed Point в сторону Blocked Point

    public Transform reverseExitPoint; // Куда поставить игрока при переходе со стороны Blocked Point обратно к Allowed Point

    [Header("Side Detection Points")] // Точки определения текущей стороны игрока
    public Transform allowedPoint; // Точка со стороны ванной

    public Transform blockedPoint; // Точка со стороны кухни

    [Header("QTE")] // Дополнительный финальный QTE
    public bool startQTEAfterExit = false; // Запускать ли FinalQTE после перехода

    public bool startQTEOnlyFromAllowedSide = true; // Запускать QTE только при движении из ванной в кухню

    public FinalQTE finalQTE; // Ссылка на FinalQTE

    [Header("Final Sequence")] // Связь с финальным сценарием квартиры
    public bool notifyFinalSequenceOnlyDuringFinal = true; // Не трогать финальный сценарий при раннем использовании окна

    public ApartmentFinalSequence finalSequence; // ApartmentFinalSequence этой квартиры

    [Header("Use Once")] // Повторное использование
    public bool disableAfterUse = false; // Для двухстороннего перехода обязательно оставить выключенным

    [Header("Debug")] // Отладка
    public bool showDebugLogs = true; // Показывать причины отказа и направление перехода

    [HideInInspector]
    public bool playerStartedWindowExit = false; // Финальный флаг выхода через окно

    private bool exitUsed = false; // Был ли одноразовый переход уже использован

    private void Reset() // Автоматическая настройка при добавлении компонента
    {
        if (characterController == null) // Если CharacterController не назначен
        {
            characterController = FindFirstObjectByType<CharacterController>(); // Пробуем найти игрока в сцене
        }

        availableImmediately = true; // Разрешаем окно независимо от этапа квартиры

        requireBrokenWindow = true; // Требуем сначала разбить окно

        allowTwoWayTraversal = true; // Включаем переход в обе стороны

        disableAfterUse = false; // Не отключаем коллайдер после первого перехода
    }

    public void Interact() // Вызывается PlayerInteractor при нажатии E
    {
        if (!availableImmediately) // Если раннее использование выключено
        {
            if (finalSequence == null || !finalSequence.finalSequenceStarted) // Проверяем начало финала
            {
                LogReason("окно недоступно до начала финала 6/6"); // Пишем причину

                return; // Запрещаем переход
            }
        }

        if (disableAfterUse && exitUsed) // Проверяем одноразовое использование
        {
            LogReason("окно уже использовано"); // Пишем причину

            return; // Повторный переход запрещён
        }

        if (requireBrokenWindow) // Если окно обязательно должно быть разбито
        {
            if (breakableWindow == null) // Если BreakableObject не назначен
            {
                LogReason("не назначен Breakable Window"); // Пишем причину

                return; // Проверка разрушения невозможна
            }

            if (!breakableWindow.IsBroken) // Если окно ещё целое
            {
                LogReason("окно ещё не разбито"); // Пишем причину

                return; // Перелезать нельзя
            }
        }

        if (characterController == null) // Если игрок не назначен
        {
            LogReason("не назначен Character Controller"); // Пишем причину

            return; // Перенос невозможен
        }

        if (!TryGetDestination(out Transform destinationPoint, out bool startedFromAllowedSide)) // Определяем направление
        {
            return; // Причина уже выведена внутри метода
        }

        TeleportPlayer(destinationPoint, startedFromAllowedSide); // Переносим игрока на противоположную сторону
    }

    private bool TryGetDestination( // Выбирает нужную точку назначения
        out Transform destinationPoint, // Возвращаемая точка назначения
        out bool startedFromAllowedSide // Возвращаемая начальная сторона
    )
    {
        destinationPoint = null; // Сначала очищаем точку назначения

        startedFromAllowedSide = true; // По умолчанию считаем, что игрок находится на разрешённой стороне

        if (exitPoint == null) // Проверяем основную точку назначения
        {
            LogReason("не назначен Exit Point"); // Пишем причину

            return false; // Переход невозможен
        }

        if (allowTwoWayTraversal) // Если включён переход в обе стороны
        {
            if (reverseExitPoint == null) // Проверяем обратную точку
            {
                LogReason("не назначен Reverse Exit Point"); // Пишем причину

                return false; // Обратный переход невозможен
            }

            if (allowedPoint == null) // Проверяем точку стороны ванной
            {
                LogReason("не назначен Allowed Point"); // Пишем причину

                return false; // Невозможно определить сторону
            }

            if (blockedPoint == null) // Проверяем точку стороны кухни
            {
                LogReason("не назначен Blocked Point"); // Пишем причину

                return false; // Невозможно определить сторону
            }

            startedFromAllowedSide = IsPlayerOnAllowedSide(); // Определяем, с какой стороны находится игрок

            destinationPoint =
                startedFromAllowedSide
                ? exitPoint
                : reverseExitPoint; // Из ванной идём в кухню, из кухни возвращаемся в ванную

            return true; // Точка успешно выбрана
        }

        if (checkAllowedSide) // Если односторонний режим проверяет сторону
        {
            if (allowedPoint == null) // Проверяем Allowed Point
            {
                LogReason("не назначен Allowed Point"); // Пишем причину

                return false; // Невозможно определить сторону
            }

            if (blockedPoint == null) // Проверяем Blocked Point
            {
                LogReason("не назначен Blocked Point"); // Пишем причину

                return false; // Невозможно определить сторону
            }

            if (!IsPlayerOnAllowedSide()) // Если игрок подошёл с запрещённой стороны
            {
                LogReason("игрок находится с запрещённой стороны окна"); // Пишем причину

                return false; // Запрещаем переход
            }
        }

        destinationPoint = exitPoint; // В одностороннем режиме используем основной Exit Point

        startedFromAllowedSide = true; // Переход считается прямым

        return true; // Точка успешно выбрана
    }

    private bool IsPlayerOnAllowedSide() // Определяет, находится ли игрок со стороны ванной
    {
        Vector3 playerPosition = characterController.transform.position; // Получаем позицию игрока

        float distanceToAllowed = Vector3.Distance(
            playerPosition,
            allowedPoint.position
        ); // Считаем расстояние до точки ванной

        float distanceToBlocked = Vector3.Distance(
            playerPosition,
            blockedPoint.position
        ); // Считаем расстояние до точки кухни

        return distanceToAllowed < distanceToBlocked; // Ближайшая точка определяет текущую сторону
    }

    private void TeleportPlayer( // Безопасно переносит игрока
        Transform destinationPoint, // Точка назначения
        bool startedFromAllowedSide // Направление перехода
    )
    {
        exitUsed = true; // Запоминаем использование для возможного одноразового режима

        bool finalContextActive =
            finalSequence != null
            && (
                !notifyFinalSequenceOnlyDuringFinal
                || finalSequence.finalSequenceStarted
            ); // Проверяем, должен ли работать финальный сценарий

        if (startedFromAllowedSide && finalContextActive) // Только прямой финальный выход из ванной
        {
            playerStartedWindowExit = true; // Сообщаем финальным системам о начале выхода

            finalSequence.OnPlayerEscapedThroughWindow(); // Уведомляем ApartmentFinalSequence
        }

        characterController.enabled = false; // Отключаем CharacterController перед переносом

        characterController.transform.SetPositionAndRotation(
            destinationPoint.position,
            destinationPoint.rotation
        ); // Ставим игрока в выбранную точку и поворачиваем по ней

        characterController.enabled = true; // Возвращаем CharacterController

        bool shouldStartQTE =
            startQTEAfterExit
            && finalQTE != null
            && (
                !startQTEOnlyFromAllowedSide
                || startedFromAllowedSide
            ); // Определяем, требуется ли QTE для этого направления

        if (shouldStartQTE) // Если QTE разрешён
        {
            finalQTE.StartQTE(); // Запускаем QTE
        }

        if (showDebugLogs) // Если включены логи
        {
            string directionText =
                startedFromAllowedSide
                ? "из ванной в кухню"
                : "из кухни в ванную"; // Формируем понятное направление

            Debug.Log(
                "WindowExitTrigger: игрок перелез " + directionText + ".",
                gameObject
            ); // Выводим успешный переход
        }

        if (disableAfterUse) // Если включён одноразовый режим
        {
            gameObject.SetActive(false); // Отключаем объект вместе с коллайдером
        }
    }

    private void LogReason(string reason) // Выводит причину отказа
    {
        if (!showDebugLogs) return; // Если логи выключены, выходим

        Debug.Log(
            "WindowExitTrigger: " + reason + ".",
            gameObject
        ); // Пишем сообщение в Console
    }
}