using UnityEngine; // Подключаем Unity-классы
using System.Collections; // Подключаем корутины

public class UniversalDoor : MonoBehaviour // Основной скрипт двери, теперь сам не является IInteractable
{
    public enum DoorOpenDirection { Forward, Backward } // Направление открытия двери

    public enum DoorRotationAxis { X, Y, Z } // Ось вращения двери

    [Header("Door Settings")] // Блок настроек двери
    public bool isOpen = false; // Открыта ли дверь

    public bool IsOpen => isOpen; // Публичная проверка состояния двери

    public DoorOpenDirection openDirection = DoorOpenDirection.Forward; // Куда открывается дверь

    [Header("Rotation Axis")] // Блок оси вращения
    public DoorRotationAxis rotationAxis = DoorRotationAxis.Y; // Ось вращения двери

    [Header("Open Angle")] // Блок угла открытия
    public float openAngle = 90f; // Угол открытия

    [Header("Open / Close Speed")] // Блок скоростей
    public float openSpeed = 5f; // Скорость открытия
    public float closeSpeed = 7f; // Скорость закрытия

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

    [Header("Noise")] // Блок шума
    public NoiseEmitter noiseEmitter; // Источник шума двери

    [Range(1, 10)] public int openNoisePower = 3; // Сила шума открытия
    [Range(1, 10)] public int closeNoisePower = 4; // Сила шума закрытия

    [Header("Lock")] // Блок замка
    public bool isLocked = false; // Заблокирована ли дверь

    [Header("Tumbler Lock")] // Блок тумблера
    public bool requiresTumbler = false; // Требуется ли тумблер
    public TumblerSwitch requiredTumbler; // Нужный тумблер

    private Quaternion closedRotation; // Закрытый поворот двери
    private Quaternion openedRotation; // Открытый поворот двери
    private Quaternion outsideHandleStartRotation; // Стартовый поворот внешней ручки
    private Quaternion insideHandleStartRotation; // Стартовый поворот внутренней ручки
    private Quaternion outsideHandlePressedRotation; // Нажатый поворот внешней ручки
    private Quaternion insideHandlePressedRotation; // Нажатый поворот внутренней ручки
    private bool isBusy = false; // Занята ли дверь анимацией

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

        SetupDoorInteractZone(); // Настраиваем отдельную зону взаимодействия
    }

    private void Update() // Каждый кадр
    {
        UpdateDoorRotation(); // Плавно двигаем дверь
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

        if (!isOpen && CanOpenDoor()) ToggleDoor(); // Если дверь закрыта и можно открыть, открываем

        else if (isOpen) ToggleDoor(); // Если дверь открыта, закрываем
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

    public void SetLocked(bool value) // Установить замок
    {
        isLocked = value; // Записываем состояние замка
    }

    public void UnlockDoor() // Разблокировать дверь
    {
        isLocked = false; // Снимаем замок
    }

    public void LockDoor() // Заблокировать дверь
    {
        isLocked = true; // Ставим замок
    }

    public void SetMonsterCanOpen(bool value) // Настроить доступ монстра
    {
        canMonsterOpen = value; // Записываем доступ монстра
    }

    public void OpenDoorForMonster() // Открыть дверь монстром
    {
        if (!canMonsterOpen || isBusy || isOpen || isLocked || !CanOpenDoor()) return; // Проверяем запреты

        StartCoroutine(InteractSequence(true)); // Запускаем открытие
    }

    private IEnumerator InteractSequence(bool targetOpenState) // Последовательность открытия/закрытия
    {
        isBusy = true; // Блокируем повторное нажатие

        yield return StartCoroutine(PressHandlesDown()); // Опускаем ручки

        if (doorOpenDelay > 0f) yield return new WaitForSeconds(doorOpenDelay); // Ждем задержку

        isOpen = targetOpenState; // Меняем состояние двери

        EmitDoorNoise(targetOpenState); // Создаем шум

        if (handleHoldTime > 0f) yield return new WaitForSeconds(handleHoldTime); // Ждем удержание ручки

        yield return StartCoroutine(ReturnHandlesBack()); // Возвращаем ручки

        isBusy = false; // Разрешаем новое взаимодействие
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