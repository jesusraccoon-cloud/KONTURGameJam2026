using UnityEngine; // Подключаем Unity, чтобы использовать MonoBehaviour, Transform, Collider, Quaternion и другие классы Unity
using System.Collections; // Подключаем корутины, чтобы делать плавные последовательности во времени

public class UniversalDoor : MonoBehaviour, IInteractable // Основной скрипт двери, который можно вызвать через систему взаимодействия
{
    public enum DoorOpenDirection // Перечисление направлений открытия двери
    {
        Forward, // Дверь открывается вперед
        Backward // Дверь открывается назад
    }

    public enum DoorRotationAxis // Перечисление осей вращения двери
    {
        X, // Вращение по оси X
        Y, // Вращение по оси Y
        Z // Вращение по оси Z
    }

    [Header("Door Settings")] // Заголовок в Inspector для основных настроек двери
    public bool isOpen = false; // Хранит состояние двери: false закрыта, true открыта

    public bool IsOpen => isOpen; // Публичное свойство, чтобы другие скрипты могли узнать, открыта ли дверь

    public DoorOpenDirection openDirection = DoorOpenDirection.Forward; // Направление, в которое дверь будет открываться

    [Header("Rotation Axis")] // Заголовок в Inspector для выбора оси вращения
    public DoorRotationAxis rotationAxis = DoorRotationAxis.Y; // Ось, вокруг которой будет вращаться дверь

    [Header("Open Angle")] // Заголовок в Inspector для угла открытия
    public float openAngle = 90f; // Угол, на который дверь повернется при открытии

    [Header("Open / Close Speed")] // Заголовок в Inspector для скоростей движения двери
    public float openSpeed = 5f; // Скорость плавного открытия двери
    public float closeSpeed = 7f; // Скорость плавного закрытия двери

    [Header("References")] // Заголовок в Inspector для ссылок на коллайдеры и зоны
    public Collider handleInteractZone; // Старый коллайдер ручки, оставлен для совместимости со старыми дверями
    public Collider doorInteractZone; // Новый коллайдер самой двери, по которому игрок сможет нажимать E

    [Header("Handles")] // Заголовок в Inspector для настроек ручек
    public Transform outsideHandle; // Внешняя ручка двери
    public Transform insideHandle; // Внутренняя ручка двери
    public float handleDownAngle = 20f; // Угол, на который ручка опустится при взаимодействии
    public float handlePressSpeed = 12f; // Скорость опускания ручки
    public float handleReturnSpeed = 10f; // Скорость возвращения ручки обратно
    public float handleHoldTime = 0.05f; // Время, на которое ручка останется в нажатом положении

    [Header("Door Delay")] // Заголовок в Inspector для задержки перед движением двери
    public float doorOpenDelay = 0.03f; // Задержка между опусканием ручки и сменой состояния двери

    [Header("Monster Access")] // Заголовок в Inspector для доступа монстра
    public bool canMonsterOpen = true; // Может ли монстр открывать эту дверь

    [Header("Noise")] // Заголовок в Inspector для шума двери
    public NoiseEmitter noiseEmitter; // Компонент, который создает шум для ИИ или других систем

    [Range(1, 10)] public int openNoisePower = 3; // Сила шума при открытии двери
    [Range(1, 10)] public int closeNoisePower = 4; // Сила шума при закрытии двери

    [Header("Lock")] // Заголовок в Inspector для блокировки двери
    public bool isLocked = false; // Заблокирована ли дверь

    [Header("Tumbler Lock")] // Заголовок в Inspector для условия через тумблер
    public bool requiresTumbler = false; // Нужно ли проверять тумблер перед открытием двери
    public TumblerSwitch requiredTumbler; // Тумблер, который должен быть включен для открытия двери

    private Quaternion closedRotation; // Поворот двери в закрытом положении
    private Quaternion openedRotation; // Поворот двери в открытом положении

    private Quaternion outsideHandleStartRotation; // Стартовый поворот внешней ручки
    private Quaternion insideHandleStartRotation; // Стартовый поворот внутренней ручки
    private Quaternion outsideHandlePressedRotation; // Поворот внешней ручки в нажатом состоянии
    private Quaternion insideHandlePressedRotation; // Поворот внутренней ручки в нажатом состоянии

    private bool isBusy = false; // Защита от повторного нажатия, пока дверь уже выполняет анимацию

    private void Start() // Запускается один раз при старте сцены
    {
        closedRotation = transform.localRotation; // Запоминаем текущий поворот двери как закрытое состояние

        float direction = openDirection == DoorOpenDirection.Forward ? 1f : -1f; // Выбираем знак поворота в зависимости от направления открытия

        Vector3 rotationVector = Vector3.zero; // Создаем пустой вектор угла поворота

        switch (rotationAxis) // Проверяем, вокруг какой оси должна вращаться дверь
        {
            case DoorRotationAxis.X: // Если выбрана ось X
                rotationVector = new Vector3(openAngle * direction, 0f, 0f); // Задаем поворот по X
                break; // Выходим из этого варианта switch

            case DoorRotationAxis.Y: // Если выбрана ось Y
                rotationVector = new Vector3(0f, openAngle * direction, 0f); // Задаем поворот по Y
                break; // Выходим из этого варианта switch

            case DoorRotationAxis.Z: // Если выбрана ось Z
                rotationVector = new Vector3(0f, 0f, openAngle * direction); // Задаем поворот по Z
                break; // Выходим из этого варианта switch
        }

        openedRotation = closedRotation * Quaternion.Euler(rotationVector); // Рассчитываем поворот открытой двери относительно закрытого положения

        if (outsideHandle != null) // Проверяем, назначена ли внешняя ручка
        {
            outsideHandleStartRotation = outsideHandle.localRotation; // Запоминаем стартовый поворот внешней ручки
            outsideHandlePressedRotation = outsideHandleStartRotation * Quaternion.Euler(0f, 0f, -handleDownAngle); // Рассчитываем нажатый поворот внешней ручки
        }

        if (insideHandle != null) // Проверяем, назначена ли внутренняя ручка
        {
            insideHandleStartRotation = insideHandle.localRotation; // Запоминаем стартовый поворот внутренней ручки
            insideHandlePressedRotation = insideHandleStartRotation * Quaternion.Euler(0f, 0f, -handleDownAngle); // Рассчитываем нажатый поворот внутренней ручки
        }

        if (noiseEmitter == null) // Проверяем, назначен ли источник шума вручную
        {
            noiseEmitter = GetComponent<NoiseEmitter>(); // Если не назначен, пробуем найти NoiseEmitter на этом же объекте
        }

        SetupDoorInteractZone(); // Настраиваем коллайдер двери как интерактивную зону
    }

    private void Update() // Запускается каждый кадр
    {
        UpdateDoorRotation(); // Плавно двигаем дверь к открытому или закрытому положению
    }

    private void SetupDoorInteractZone() // Настраивает интеракцию через коллайдер самой двери
    {
        if (doorInteractZone == null) return; // Если коллайдер двери не назначен, ничего не делаем

        if (doorInteractZone.GetComponent<UniversalDoor>() == this) return; // Если UniversalDoor уже висит прямо на объекте этого коллайдера, дополнительная настройка не нужна

        UniversalDoorInteractForwarder forwarder = doorInteractZone.GetComponent<UniversalDoorInteractForwarder>(); // Ищем переадресатор взаимодействия на объекте коллайдера

        if (forwarder == null) // Если переадресатора еще нет
        {
            forwarder = doorInteractZone.gameObject.AddComponent<UniversalDoorInteractForwarder>(); // Добавляем переадресатор на объект с коллайдером двери
        }

        forwarder.door = this; // Передаем переадресатору ссылку на эту дверь
    }

    private bool CanOpenDoor() // Проверяет, можно ли открыть дверь по дополнительным условиям
    {
        if (!requiresTumbler) // Если тумблер не требуется
        {
            return true; // Открывать можно
        }

        if (requiredTumbler == null) // Если тумблер требуется, но ссылка на него не назначена
        {
            return false; // Открывать нельзя
        }

        return requiredTumbler.isOn; // Открывать можно только если нужный тумблер включен
    }

    public void Interact() // Основной метод взаимодействия, вызывается при нажатии E
    {
        if (isBusy) return; // Если дверь сейчас занята анимацией, новое нажатие игнорируем

        if (isLocked) return; // Если дверь заблокирована, взаимодействие игнорируем

        if (!isOpen) // Если дверь сейчас закрыта
        {
            if (CanOpenDoor()) // Проверяем, выполнены ли условия открытия
            {
                ToggleDoor(); // Запускаем открытие двери
            }
        }
        else // Если дверь сейчас открыта
        {
            ToggleDoor(); // Запускаем закрытие двери
        }
    }

    public void ToggleDoor() // Переключает дверь между открытым и закрытым состоянием
    {
        if (isBusy) return; // Если дверь занята, ничего не делаем

        StartCoroutine(InteractSequence(!isOpen)); // Запускаем последовательность с противоположным состоянием двери
    }

    public void OpenDoor() // Открывает дверь из другого скрипта
    {
        if (isBusy) return; // Если дверь занята, ничего не делаем

        if (isOpen) return; // Если дверь уже открыта, ничего не делаем

        if (isLocked) return; // Если дверь заблокирована, ничего не делаем

        if (!CanOpenDoor()) return; // Если условия открытия не выполнены, ничего не делаем

        StartCoroutine(InteractSequence(true)); // Запускаем последовательность открытия двери
    }

    public void CloseDoor() // Закрывает дверь из другого скрипта
    {
        if (isBusy) return; // Если дверь занята, ничего не делаем

        if (!isOpen) return; // Если дверь уже закрыта, ничего не делаем

        StartCoroutine(InteractSequence(false)); // Запускаем последовательность закрытия двери
    }

    public void SetLocked(bool value) // Устанавливает состояние замка
    {
        isLocked = value; // Записываем новое состояние блокировки
    }

    public void UnlockDoor() // Разблокирует дверь, удобно вызывать через UnityEvent
    {
        isLocked = false; // Снимаем блокировку двери
    }

    public void LockDoor() // Блокирует дверь, удобно вызывать через UnityEvent
    {
        isLocked = true; // Включаем блокировку двери
    }

    public void SetMonsterCanOpen(bool value) // Настраивает, может ли монстр открывать дверь
    {
        canMonsterOpen = value; // Записываем доступ монстра к двери
    }

    public void UnlockDoorAndBlockMonster() // Разблокирует дверь для игрока и запрещает монстру ее открывать
    {
        isLocked = false; // Снимаем блокировку двери

        canMonsterOpen = false; // Запрещаем монстру открывать эту дверь
    }

    public void UnlockDoorAndAllowMonster() // Разблокирует дверь и разрешает монстру ее открывать
    {
        isLocked = false; // Снимаем блокировку двери

        canMonsterOpen = true; // Разрешаем монстру открывать эту дверь
    }

    public void OpenDoorForMonster() // Открывает дверь монстром
    {
        if (!canMonsterOpen) return; // Если монстру нельзя открывать эту дверь, ничего не делаем

        if (isBusy) return; // Если дверь занята, ничего не делаем

        if (isOpen) return; // Если дверь уже открыта, ничего не делаем

        if (isLocked) return; // Если дверь заблокирована, ничего не делаем

        if (!CanOpenDoor()) return; // Если дополнительные условия открытия не выполнены, ничего не делаем

        StartCoroutine(InteractSequence(true)); // Запускаем последовательность открытия двери
    }

    private IEnumerator InteractSequence(bool targetOpenState) // Полная последовательность взаимодействия с дверью
    {
        isBusy = true; // Блокируем повторные взаимодействия на время последовательности

        yield return StartCoroutine(PressHandlesDown()); // Опускаем ручки и ждем окончания этой анимации

        if (doorOpenDelay > 0f) // Проверяем, нужна ли пауза перед движением двери
        {
            yield return new WaitForSeconds(doorOpenDelay); // Ждем заданную задержку
        }

        isOpen = targetOpenState; // Меняем состояние двери на нужное

        EmitDoorNoise(targetOpenState); // Создаем шум открытия или закрытия двери

        if (handleHoldTime > 0f) // Проверяем, нужно ли удержать ручку внизу
        {
            yield return new WaitForSeconds(handleHoldTime); // Ждем паузу удержания ручки
        }

        yield return StartCoroutine(ReturnHandlesBack()); // Возвращаем ручки обратно и ждем окончания анимации

        isBusy = false; // Разрешаем следующие взаимодействия с дверью
    }

    private IEnumerator PressHandlesDown() // Анимация опускания ручек
    {
        float t = 0f; // Прогресс анимации от 0 до 1

        Quaternion outStart = outsideHandle != null ? outsideHandle.localRotation : Quaternion.identity; // Берем текущий поворот внешней ручки или пустой поворот
        Quaternion inStart = insideHandle != null ? insideHandle.localRotation : Quaternion.identity; // Берем текущий поворот внутренней ручки или пустой поворот

        while (t < 1f) // Пока прогресс анимации меньше 1
        {
            t += Time.deltaTime * handlePressSpeed; // Увеличиваем прогресс с учетом времени кадра и скорости нажатия

            if (outsideHandle != null) // Если внешняя ручка назначена
            {
                outsideHandle.localRotation = Quaternion.Lerp(outStart, outsideHandlePressedRotation, t); // Плавно поворачиваем внешнюю ручку вниз
            }

            if (insideHandle != null) // Если внутренняя ручка назначена
            {
                insideHandle.localRotation = Quaternion.Lerp(inStart, insideHandlePressedRotation, t); // Плавно поворачиваем внутреннюю ручку вниз
            }

            yield return null; // Ждем следующий кадр
        }

        if (outsideHandle != null) outsideHandle.localRotation = outsideHandlePressedRotation; // Точно ставим внешнюю ручку в нажатое положение
        if (insideHandle != null) insideHandle.localRotation = insideHandlePressedRotation; // Точно ставим внутреннюю ручку в нажатое положение
    }

    private IEnumerator ReturnHandlesBack() // Анимация возвращения ручек обратно
    {
        float t = 0f; // Прогресс анимации от 0 до 1

        Quaternion outStart = outsideHandle != null ? outsideHandle.localRotation : Quaternion.identity; // Берем текущий поворот внешней ручки или пустой поворот
        Quaternion inStart = insideHandle != null ? insideHandle.localRotation : Quaternion.identity; // Берем текущий поворот внутренней ручки или пустой поворот

        while (t < 1f) // Пока прогресс анимации меньше 1
        {
            t += Time.deltaTime * handleReturnSpeed; // Увеличиваем прогресс с учетом времени кадра и скорости возврата

            if (outsideHandle != null) // Если внешняя ручка назначена
            {
                outsideHandle.localRotation = Quaternion.Lerp(outStart, outsideHandleStartRotation, t); // Плавно возвращаем внешнюю ручку к стартовому повороту
            }

            if (insideHandle != null) // Если внутренняя ручка назначена
            {
                insideHandle.localRotation = Quaternion.Lerp(inStart, insideHandleStartRotation, t); // Плавно возвращаем внутреннюю ручку к стартовому повороту
            }

            yield return null; // Ждем следующий кадр
        }

        if (outsideHandle != null) outsideHandle.localRotation = outsideHandleStartRotation; // Точно ставим внешнюю ручку в стартовое положение
        if (insideHandle != null) insideHandle.localRotation = insideHandleStartRotation; // Точно ставим внутреннюю ручку в стартовое положение
    }

    private void UpdateDoorRotation() // Плавно двигает дверь каждый кадр
    {
        Quaternion target = isOpen ? openedRotation : closedRotation; // Выбираем целевой поворот двери: открытый или закрытый

        float speed = isOpen ? openSpeed : closeSpeed; // Выбираем скорость движения: скорость открытия или закрытия

        transform.localRotation = Quaternion.Slerp( // Плавно поворачиваем дверь к целевому повороту
            transform.localRotation, // Текущий поворот двери
            target, // Целевой поворот двери
            Time.deltaTime * speed // Скорость сглаживания с учетом времени кадра
        );
    }

    private void EmitDoorNoise(bool targetOpenState) // Создает шум двери
    {
        if (noiseEmitter == null) return; // Если источник шума не назначен, ничего не делаем

        int noisePower = targetOpenState ? openNoisePower : closeNoisePower; // Выбираем силу шума: открытие или закрытие

        noiseEmitter.EmitNoise(noisePower); // Отправляем шум в систему NoiseEmitter
    }
}

public class UniversalDoorInteractForwarder : MonoBehaviour, IInteractable // Маленький переадресатор, который висит на объекте с коллайдером двери
{
    public UniversalDoor door; // Ссылка на основную дверь, которой нужно передать взаимодействие

    private void Awake() // Запускается при создании объекта
    {
        if (door == null) // Если дверь не назначена вручную
        {
            door = GetComponentInParent<UniversalDoor>(); // Пытаемся найти UniversalDoor выше по иерархии
        }
    }

    public void Interact() // Метод вызывается PlayerInteractor, когда игрок нажимает E по коллайдеру двери
    {
        if (door == null) return; // Если основная дверь не найдена, ничего не делаем

        door.Interact(); // Передаем взаимодействие в основной UniversalDoor
    }
}