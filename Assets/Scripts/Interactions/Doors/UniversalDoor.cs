using UnityEngine; // Подключаем Unity-классы
using UnityEngine.AI; // Подключаем NavMeshObstacle
using System.Collections; // Подключаем корутины

public class UniversalDoor : MonoBehaviour // Основной скрипт двери, теперь сам не является IInteractable
{
    public enum DoorOpenDirection { Forward, Backward } // Направление открытия двери

    public enum DoorRotationAxis { X, Y, Z } // Ось вращения двери

    [Header("Door Settings")] // Блок настроек двери
    public bool isOpen = false; // Открыта ли дверь

    public bool IsOpen => isOpen; // Публичная проверка логического состояния двери

    public bool IsBusy => isBusy; // Публичная проверка занятости двери

    public bool IsFullyOpen => isOpen && Quaternion.Angle(transform.localRotation, openedRotation) <= fullyOpenAngleTolerance; // Полотно физически дошло до открытого положения

    public bool CanMonsterOpenNow => canMonsterOpen && !isBusy && !isOpen && !isLocked && CanOpenDoor(); // Может ли монстр начать открытие сейчас

    public DoorOpenDirection openDirection = DoorOpenDirection.Forward; // Куда открывается дверь

    [Header("Rotation Axis")] // Блок оси вращения
    public DoorRotationAxis rotationAxis = DoorRotationAxis.Y; // Ось вращения двери

    [Header("Open Angle")] // Блок угла открытия
    public float openAngle = 90f; // Угол открытия
    public float fullyOpenAngleTolerance = 2f; // Погрешность угла полного открытия

    [Header("Open / Close Speed")] // Блок скоростей
    public float openSpeed = 5f; // Скорость открытия
    public float closeSpeed = 7f; // Скорость закрытия

    [Header("Auto Close")] // Блок автоматического закрытия двери
    public bool autoClose = false; // Должна ли дверь автоматически закрываться после открытия

    [Min(0f)] public float autoCloseDelay = 3f; // Через сколько секунд после открытия начать закрытие

    public bool AutoCloseEnabled => autoClose; // Публичная проверка состояния автозакрытия

    [Header("Player Opening Block")] // Блок запрета повторного открытия двери игроком через E
    public bool blockPlayerOpeningAfterFirstOpen = false; // После первого открытия игроком запустить таймер запрета

    [Min(0f)] public float playerOpeningBlockDelay = 20f; // Через сколько секунд запретить игроку открывать дверь через E

    public bool IsPlayerOpeningBlocked => isPlayerOpeningBlocked; // Публичная проверка запрета открытия через E

    [Header("Interact Zone")] // Блок отдельной зоны взаимодействия
    public Collider doorInteractZone; // Отдельный коллайдер-зона, по которому игрок нажимает E

    [Header("Handles")] // Блок ручек
    public Transform outsideHandle; // Внешняя ручка
    public Transform insideHandle; // Внутренняя ручка
    public float handleDownAngle = 20f; // Угол опускания ручки
    public float handlePressSpeed = 12f; // Скорость опускания ручки
    public float handleReturnSpeed = 10f; // Скорость возврата ручки
    public float handleHoldTime = 0.05f; // Пауза удержания ручки

    [Header("Door Delay")] // Блок задержки двери
    public float doorOpenDelay = 0.03f; // Задержка между ручкой и дверью

    [Header("Monster Access")] // Блок доступа монстра
    public bool canMonsterOpen = true; // Может ли монстр открыть дверь
    public NavMeshObstacle navMeshObstacle; // Препятствие для обхода открытого полотна

    [Header("Noise")] // Блок шума
    public NoiseEmitter noiseEmitter; // Источник шума двери

    [Range(1, 10)] public int openNoisePower = 3; // Сила шума открытия
    [Range(1, 10)] public int closeNoisePower = 4; // Сила шума закрытия

    [Header("Lock")] // Блок замка
    public bool isLocked = false; // Заблокирована ли дверь

    [Header("Tumbler Lock")] // Блок тумблера
    public bool requiresTumbler = false; // Требуется ли тумблер
    public TumblerSwitch requiredTumbler; // Нужный тумблер

    [Header("Save")] // Блок сохранения
    [SerializeField] private SC_Saveable saveable; // Компонент сохранения (авто-поиск). Добавь его на дверь, если её открытое/закрытое состояние должно сохраняться. Без него дверь не сохраняется.

    private Quaternion closedRotation; // Закрытый поворот двери
    private Quaternion openedRotation; // Открытый поворот двери
    private Quaternion outsideHandleStartRotation; // Стартовый поворот внешней ручки
    private Quaternion insideHandleStartRotation; // Стартовый поворот внутренней ручки
    private Quaternion outsideHandlePressedRotation; // Нажатый поворот внешней ручки
    private Quaternion insideHandlePressedRotation; // Нажатый поворот внутренней ручки
    private bool isBusy = false; // Занята ли дверь анимацией
    private Coroutine autoCloseCoroutine; // Текущий таймер автоматического закрытия
    private bool isPlayerOpeningBlocked = false; // Запрещено ли игроку открывать дверь через E
    private bool playerOpeningBlockCountdownStarted = false; // Был ли уже запущен таймер после первого открытия
    private Coroutine playerOpeningBlockCoroutine; // Текущий таймер запрета открытия через E

    private void OnValidate() // Вызывается при изменении значений в Inspector
    {
        if (navMeshObstacle == null) navMeshObstacle = GetComponent<NavMeshObstacle>(); // Ищем NavMeshObstacle на этом объекте

        if (autoCloseDelay < 0f) autoCloseDelay = 0f; // Не разрешаем отрицательную задержку автозакрытия

        if (playerOpeningBlockDelay < 0f) playerOpeningBlockDelay = 0f; // Не разрешаем отрицательную задержку запрета открытия

        SyncNavMeshObstacleWithBoxCollider(); // Копируем форму Box Collider в obstacle
    }

    private void Start() // Запуск при старте сцены
    {
        closedRotation = transform.localRotation; // Запоминаем закрытое положение

        float direction = openDirection == DoorOpenDirection.Forward ? 1f : -1f; // Выбираем знак открытия

        Vector3 rotationVector = Vector3.zero; // Создаем вектор поворота

        if (rotationAxis == DoorRotationAxis.X) rotationVector = new Vector3(openAngle * direction, 0f, 0f); // Поворот по X
        if (rotationAxis == DoorRotationAxis.Y) rotationVector = new Vector3(0f, openAngle * direction, 0f); // Поворот по Y
        if (rotationAxis == DoorRotationAxis.Z) rotationVector = new Vector3(0f, 0f, openAngle * direction); // Поворот по Z

        openedRotation = closedRotation * Quaternion.Euler(rotationVector); // Рассчитываем открытый поворот

        if (outsideHandle != null) // Если внешняя ручка назначена
        {
            outsideHandleStartRotation = outsideHandle.localRotation; // Запоминаем старт внешней ручки
            outsideHandlePressedRotation = outsideHandleStartRotation * Quaternion.Euler(0f, 0f, -handleDownAngle); // Считаем нажатое положение
        }

        if (insideHandle != null) // Если внутренняя ручка назначена
        {
            insideHandleStartRotation = insideHandle.localRotation; // Запоминаем старт внутренней ручки
            insideHandlePressedRotation = insideHandleStartRotation * Quaternion.Euler(0f, 0f, -handleDownAngle); // Считаем нажатое положение
        }

        if (noiseEmitter == null) noiseEmitter = GetComponent<NoiseEmitter>(); // Если шум не назначен, ищем его на этом объекте

        if (navMeshObstacle == null) navMeshObstacle = GetComponent<NavMeshObstacle>(); // Ищем NavMeshObstacle на этом объекте

        SyncNavMeshObstacleWithBoxCollider(); // Синхронизируем obstacle с дверным Box Collider

        if (navMeshObstacle != null) navMeshObstacle.carving = false; // Закрытая дверь не вырезает проход из NavMesh

        SetupDoorInteractZone(); // Настраиваем отдельную зону взаимодействия

        RestoreSavedState(); // Восстанавливаем открытое/закрытое и запертое состояние двери после загрузки
    }

    private void RestoreSavedState() // Восстановить состояние двери из сохранения
    {
        if (saveable == null) saveable = GetComponent<SC_Saveable>(); // Ищем SC_Saveable на двери
        if (saveable == null) saveable = GetComponentInChildren<SC_Saveable>(true); // Или на дочернем

        if (saveable == null || !SC_SaveSystem.TryGetState(saveable.id, out int st)) return; // Нет сохранённого состояния — оставляем как в сцене

        isLocked = (st & 2) != 0; // Бит 1 — заперта
        isOpen = (st & 1) != 0; // Бит 0 — открыта

        transform.localRotation = isOpen ? openedRotation : closedRotation; // Мгновенно ставим полотно в нужное положение (без анимации)

        if (navMeshObstacle != null) navMeshObstacle.carving = IsFullyOpen; // Синхронизируем проход в NavMesh
    }

    private void SaveDoorState() // Сохранить текущее состояние двери
    {
        if (saveable == null) return; // Дверь без SC_Saveable не сохраняется

        int st = (isOpen ? 1 : 0) | (isLocked ? 2 : 0); // Пакуем открыта/заперта в биты
        SC_SaveSystem.SetState(saveable.id, st); // Сохраняем
    }

    private void Update() // Каждый кадр
    {
        UpdateDoorRotation(); // Плавно двигаем дверь

        UpdateNavMeshObstacle(); // Обновляем режим обхода полотна
    }

    private void SyncNavMeshObstacleWithBoxCollider() // Копировать форму дверного коллайдера в obstacle
    {
        if (navMeshObstacle == null) return; // Если obstacle отсутствует, выходим

        BoxCollider boxCollider = GetComponent<BoxCollider>(); // Ищем Box Collider полотна

        if (boxCollider == null) return; // Если Box Collider отсутствует, выходим

        navMeshObstacle.shape = NavMeshObstacleShape.Box; // Используем прямоугольную форму
        navMeshObstacle.center = boxCollider.center; // Копируем локальный центр
        navMeshObstacle.size = boxCollider.size; // Копируем локальный размер
        navMeshObstacle.carveOnlyStationary = true; // Carving работает только на остановившемся полотне
    }

    private void UpdateNavMeshObstacle() // Переключить carving после полного открытия
    {
        if (navMeshObstacle == null) return; // Если obstacle отсутствует, выходим

        bool shouldCarve = IsFullyOpen; // Полностью открытая дверь должна учитываться при построении пути

        if (navMeshObstacle.carving == shouldCarve) return; // Если состояние уже правильное, выходим

        navMeshObstacle.carving = shouldCarve; // Закрытая или движущаяся дверь: false; полностью открытая: true
    }

    private void SetupDoorInteractZone() // Настройка отдельной зоны взаимодействия
    {
        if (doorInteractZone == null) return; // Если зона не назначена, выходим

        UniversalDoorInteractForwarder forwarder = doorInteractZone.GetComponent<UniversalDoorInteractForwarder>(); // Ищем forwarder на зоне

        if (forwarder == null) forwarder = doorInteractZone.gameObject.AddComponent<UniversalDoorInteractForwarder>(); // Если его нет, добавляем

        forwarder.door = this; // Передаем forwarder ссылку на эту дверь
    }

    private bool CanOpenDoor() // Проверка возможности открыть дверь
    {
        if (!requiresTumbler) return true; // Если тумблер не нужен, открыть можно

        if (requiredTumbler == null) return false; // Если тумблер нужен, но не назначен, открыть нельзя

        return requiredTumbler.isOn; // Открыть можно только если тумблер включен
    }

    public void Interact() // Вызов взаимодействия от отдельной зоны
    {
        if (isBusy) return; // Если дверь занята, выходим

        if (isLocked) return; // Если дверь закрыта на замок, выходим

        if (!isOpen) // Если дверь сейчас закрыта
        {
            if (isPlayerOpeningBlocked) return; // После окончания таймера игрок больше не может открыть её через E

            if (CanOpenDoor()) StartCoroutine(InteractSequence(true, true)); // Открываем дверь и отмечаем, что это сделал игрок через E

            return; // Завершаем обработку открытия
        }

        StartCoroutine(InteractSequence(false)); // Открытую дверь игрок по-прежнему может закрыть через E
    }

    public void ToggleDoor() // Переключить дверь
    {
        if (isBusy) return; // Если дверь занята, выходим

        StartCoroutine(InteractSequence(!isOpen)); // Запускаем переключение
    }

    public void OpenDoor() // Открыть дверь из другого скрипта
    {
        if (isBusy || isOpen || isLocked || !CanOpenDoor()) return; // Проверяем запреты

        StartCoroutine(InteractSequence(true)); // Запускаем открытие
    }

    public void CloseDoor() // Закрыть дверь из другого скрипта
    {
        if (isBusy || !isOpen) return; // Проверяем запреты

        StartCoroutine(InteractSequence(false)); // Запускаем закрытие
    }

    public void SetAutoClose(bool value) // Включить или выключить автозакрытие из другого скрипта
    {
        autoClose = value; // Записываем новое состояние автозакрытия

        if (!autoClose) // Если автозакрытие выключили
        {
            CancelAutoCloseCountdown(); // Отменяем уже запущенный таймер

            return; // Больше ничего не делаем
        }

        if (isOpen && !isBusy) // Если автозакрытие включили, когда дверь уже открыта и свободна
        {
            StartAutoCloseCountdown(); // Запускаем новый таймер закрытия
        }
    }

    public void EnableAutoClose() // Включить автозакрытие
    {
        SetAutoClose(true); // Используем общий метод
    }

    public void DisableAutoClose() // Выключить автозакрытие
    {
        SetAutoClose(false); // Используем общий метод
    }

    public void ToggleAutoClose() // Переключить автозакрытие
    {
        SetAutoClose(!autoClose); // Меняем текущее состояние на противоположное
    }

    private void StartAutoCloseCountdown() // Запустить таймер автоматического закрытия
    {
        CancelAutoCloseCountdown(); // Сначала отменяем старый таймер, чтобы не было двух одновременно

        if (!autoClose) return; // Если автозакрытие выключено, таймер не запускаем

        if (!isOpen) return; // Если дверь закрыта, таймер не нужен

        autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay()); // Запускаем ожидание перед закрытием
    }

    private void CancelAutoCloseCountdown() // Отменить текущий таймер автоматического закрытия
    {
        if (autoCloseCoroutine == null) return; // Если таймер не запущен, ничего не делаем

        StopCoroutine(autoCloseCoroutine); // Останавливаем запущенную корутину

        autoCloseCoroutine = null; // Очищаем сохранённую ссылку
    }

    private IEnumerator AutoCloseAfterDelay() // Ожидание и автоматическое закрытие двери
    {
        if (autoCloseDelay > 0f) // Если задана задержка больше нуля
        {
            yield return new WaitForSeconds(autoCloseDelay); // Ждём указанное количество секунд
        }

        while (isBusy) // Если дверь ещё выполняет другую анимацию
        {
            yield return null; // Ждём следующий кадр
        }

        if (!autoClose || !isOpen) // Если автозакрытие успели выключить или дверь уже закрыта
        {
            autoCloseCoroutine = null; // Очищаем ссылку на завершённый таймер

            yield break; // Завершаем корутину
        }

        autoCloseCoroutine = null; // Очищаем ссылку перед запуском закрытия

        CloseDoor(); // Закрываем дверь штатным методом с ручками и шумом
    }

    public void SetLocked(bool value) // Установить замок
    {
        isLocked = value; // Записываем состояние замка
        SaveDoorState(); // Запоминаем состояние замка для сохранения
    }

    public void UnlockDoor() // Разблокировать дверь
    {
        isLocked = false; // Снимаем замок
        SaveDoorState(); // Запоминаем состояние замка для сохранения
    }

    public void LockDoor() // Заблокировать дверь
    {
        isLocked = true; // Ставим замок
        SaveDoorState(); // Запоминаем состояние замка для сохранения
    }

    public void SetMonsterCanOpen(bool value) // Настроить доступ монстра
    {
        canMonsterOpen = value; // Записываем доступ монстра
    }

    public bool OpenDoorForMonster() // Попытаться открыть дверь монстром
    {
        if (!CanMonsterOpenNow) return false; // Если открыть нельзя, сообщаем об отказе

        StartCoroutine(InteractSequence(true)); // Запускаем штатное открытие

        return true; // Сообщаем, что открытие началось
    }

    private IEnumerator InteractSequence(bool targetOpenState, bool openedByPlayer = false) // Последовательность открытия/закрытия
    {
        if (!targetOpenState) CancelAutoCloseCountdown(); // При любом ручном или сценарном закрытии отменяем старый таймер

        isBusy = true; // Блокируем повторное нажатие

        yield return StartCoroutine(PressHandlesDown()); // Опускаем ручки

        if (doorOpenDelay > 0f) yield return new WaitForSeconds(doorOpenDelay); // Ждем задержку

        isOpen = targetOpenState; // Меняем состояние двери

        SaveDoorState(); // Запоминаем новое открытое/закрытое состояние для сохранения

        EmitDoorNoise(targetOpenState); // Создаем шум

        if (handleHoldTime > 0f) yield return new WaitForSeconds(handleHoldTime); // Ждем удержание ручки

        yield return StartCoroutine(ReturnHandlesBack()); // Возвращаем ручки

        isBusy = false; // Разрешаем новое взаимодействие

        if (targetOpenState && openedByPlayer) // Если дверь открылась именно через E игрока
        {
            StartPlayerOpeningBlockCountdown(); // Запускаем таймер будущего запрета повторного открытия через E
        }

        if (targetOpenState && autoClose) // Если дверь только что открылась и автозакрытие разрешено
        {
            StartAutoCloseCountdown(); // Запускаем отсчёт до автоматического закрытия
        }
    }

    private void StartPlayerOpeningBlockCountdown() // Запустить таймер запрета повторного открытия игроком
    {
        if (!blockPlayerOpeningAfterFirstOpen) return; // Если галочка выключена, ничего не запускаем

        if (playerOpeningBlockCountdownStarted) return; // Таймер запускается только после самого первого открытия через E

        playerOpeningBlockCountdownStarted = true; // Запоминаем, что отсчёт уже начался

        playerOpeningBlockCoroutine = StartCoroutine(BlockPlayerOpeningAfterDelay()); // Запускаем ожидание
    }

    private IEnumerator BlockPlayerOpeningAfterDelay() // Ожидание перед запретом открытия через E
    {
        if (playerOpeningBlockDelay > 0f) // Если указана задержка больше нуля
        {
            yield return new WaitForSeconds(playerOpeningBlockDelay); // Ждём заданное в Inspector время
        }

        playerOpeningBlockCoroutine = null; // Очищаем ссылку на завершённую корутину

        if (!blockPlayerOpeningAfterFirstOpen) // Если галочку выключили во время ожидания
        {
            playerOpeningBlockCountdownStarted = false; // Разрешаем запустить таймер заново после повторного включения

            yield break; // Не запрещаем открытие
        }

        isPlayerOpeningBlocked = true; // Запрещаем только открытие двери игроком через E
    }

    private IEnumerator PressHandlesDown() // Опускание ручек
    {
        float t = 0f; // Прогресс

        Quaternion outStart = outsideHandle != null ? outsideHandle.localRotation : Quaternion.identity; // Старт внешней ручки
        Quaternion inStart = insideHandle != null ? insideHandle.localRotation : Quaternion.identity; // Старт внутренней ручки

        while (t < 1f) // Пока анимация не закончена
        {
            t += Time.deltaTime * handlePressSpeed; // Увеличиваем прогресс

            if (outsideHandle != null) outsideHandle.localRotation = Quaternion.Lerp(outStart, outsideHandlePressedRotation, t); // Опускаем внешнюю ручку

            if (insideHandle != null) insideHandle.localRotation = Quaternion.Lerp(inStart, insideHandlePressedRotation, t); // Опускаем внутреннюю ручку

            yield return null; // Ждем кадр
        }
    }

    private IEnumerator ReturnHandlesBack() // Возврат ручек
    {
        float t = 0f; // Прогресс

        Quaternion outStart = outsideHandle != null ? outsideHandle.localRotation : Quaternion.identity; // Старт внешней ручки
        Quaternion inStart = insideHandle != null ? insideHandle.localRotation : Quaternion.identity; // Старт внутренней ручки

        while (t < 1f) // Пока анимация не закончена
        {
            t += Time.deltaTime * handleReturnSpeed; // Увеличиваем прогресс

            if (outsideHandle != null) outsideHandle.localRotation = Quaternion.Lerp(outStart, outsideHandleStartRotation, t); // Возвращаем внешнюю ручку

            if (insideHandle != null) insideHandle.localRotation = Quaternion.Lerp(inStart, insideHandleStartRotation, t); // Возвращаем внутреннюю ручку

            yield return null; // Ждем кадр
        }
    }

    private void UpdateDoorRotation() // Плавное движение двери
    {
        Quaternion target = isOpen ? openedRotation : closedRotation; // Выбираем цель

        float speed = isOpen ? openSpeed : closeSpeed; // Выбираем скорость

        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * speed); // Поворачиваем дверь
    }

    private void EmitDoorNoise(bool targetOpenState) // Шум двери
    {
        if (noiseEmitter == null) return; // Если шума нет, выходим

        int noisePower = targetOpenState ? openNoisePower : closeNoisePower; // Выбираем силу шума

        noiseEmitter.EmitNoise(noisePower); // Отправляем шум
    }
}

public class UniversalDoorInteractForwarder : MonoBehaviour, IInteractable // Скрипт отдельной зоны взаимодействия двери
{
    public UniversalDoor door; // Ссылка на дверь

    public void Interact() // Игрок нажал E по отдельной зоне
    {
        if (door == null) door = GetComponentInParent<UniversalDoor>(); // Если ссылка пустая, ищем дверь выше

        if (door == null) return; // Если дверь не найдена, выходим

        door.Interact(); // Передаем взаимодействие двери
    }
}