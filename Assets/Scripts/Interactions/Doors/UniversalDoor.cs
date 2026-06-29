using UnityEngine; // Подключаем Unity
using System.Collections; // Подключаем работу с корутинами

public class UniversalDoor : MonoBehaviour, IInteractable // Универсальная дверь
{
    public enum DoorOpenDirection // Направление открытия двери
    {
        Forward, // Открытие вперёд
        Backward // Открытие назад
    }

    public enum DoorRotationAxis // Ось вращения двери
    {
        X, // Ось X
        Y, // Ось Y
        Z  // Ось Z
    }

    [Header("Door Settings")] // Настройки двери
    public bool isOpen = false; // Открыта ли дверь

    public bool IsOpen => isOpen; // Публичная проверка состояния двери

    public DoorOpenDirection openDirection = DoorOpenDirection.Forward; // Направление открытия

    [Header("Rotation Axis")] // Ось вращения
    public DoorRotationAxis rotationAxis = DoorRotationAxis.Y; // По какой оси вращается дверь

    [Header("Open Angle")] // Угол открытия
    public float openAngle = 90f; // Угол открытия двери

    [Header("Open / Close Speed")] // Скорость открытия/закрытия
    public float openSpeed = 5f; // Скорость открытия
    public float closeSpeed = 7f; // Скорость закрытия

    [Header("References")] // Ссылки
    public Collider handleInteractZone; // Зона взаимодействия с ручкой

    [Header("Handles")] // Ручки двери
    public Transform outsideHandle; // Внешняя ручка
    public Transform insideHandle; // Внутренняя ручка
    public float handleDownAngle = 20f; // Угол нажатия ручки
    public float handlePressSpeed = 12f; // Скорость нажатия ручки
    public float handleReturnSpeed = 10f; // Скорость возврата ручки
    public float handleHoldTime = 0.05f; // Пауза удержания ручки

    [Header("Door Delay")] // Задержка двери
    public float doorOpenDelay = 0.03f; // Задержка перед открытием

    [Header("Monster Access")] // Доступ монстра
    public bool canMonsterOpen = true; // Может ли монстр открыть дверь
        [Header("Noise")] // Заголовок шума двери
    public NoiseEmitter noiseEmitter; // Источник шума двери

    [Range(1, 10)] public int openNoisePower = 3; // Шум открытия двери

    [Range(1, 10)] public int closeNoisePower = 4; // Шум закрытия двери

    [Header("Lock")] // Блокировка двери
    public bool isLocked = false; // true — дверь заблокирована, false — дверь доступна

    [Header("Tumbler Lock")] // Старая проверка тумблера
    public bool requiresTumbler = false; // Нужно ли проверять тумблер
    public TumblerSwitch requiredTumbler; // Нужный тумблер

    private Quaternion closedRotation; // Закрытый поворот двери
    private Quaternion openedRotation; // Открытый поворот двери

    private Quaternion outsideHandleStartRotation; // Стартовый поворот внешней ручки
    private Quaternion insideHandleStartRotation; // Стартовый поворот внутренней ручки
    private Quaternion outsideHandlePressedRotation; // Нажатый поворот внешней ручки
    private Quaternion insideHandlePressedRotation; // Нажатый поворот внутренней ручки

    private bool isBusy = false; // Занята ли дверь анимацией

    private void Start() // Запуск сцены
    {
        closedRotation = transform.localRotation; // Запоминаем закрытое положение двери

        float direction = openDirection == DoorOpenDirection.Forward ? 1f : -1f; // Выбираем направление открытия

        Vector3 rotationVector = Vector3.zero; // Создаём пустой вектор поворота

        switch (rotationAxis) // Проверяем выбранную ось
        {
            case DoorRotationAxis.X: // Если ось X
                rotationVector = new Vector3(openAngle * direction, 0f, 0f); // Поворот по X
                break; // Выход из case

            case DoorRotationAxis.Y: // Если ось Y
                rotationVector = new Vector3(0f, openAngle * direction, 0f); // Поворот по Y
                break; // Выход из case

            case DoorRotationAxis.Z: // Если ось Z
                rotationVector = new Vector3(0f, 0f, openAngle * direction); // Поворот по Z
                break; // Выход из case
        }

        openedRotation = closedRotation * Quaternion.Euler(rotationVector); // Высчитываем открытое положение двери

        if (outsideHandle != null) // Если внешняя ручка назначена
        {
            outsideHandleStartRotation = outsideHandle.localRotation; // Запоминаем стартовый поворот
            outsideHandlePressedRotation = outsideHandleStartRotation * Quaternion.Euler(0f, 0f, -handleDownAngle); // Высчитываем нажатый поворот
        }

        if (insideHandle != null) // Если внутренняя ручка назначена
        {
            insideHandleStartRotation = insideHandle.localRotation; // Запоминаем стартовый поворот
            insideHandlePressedRotation = insideHandleStartRotation * Quaternion.Euler(0f, 0f, -handleDownAngle); // Высчитываем нажатый поворот
        }
        if (noiseEmitter == null) // Если NoiseEmitter не назначен вручную
        {
            noiseEmitter = GetComponent<NoiseEmitter>(); // Пробуем найти NoiseEmitter на этом же объекте
        }
    }
        

    private void Update() // Каждый кадр
    {
        UpdateDoorRotation(); // Плавно двигаем дверь
    }

    private bool CanOpenDoor() // Проверка, можно ли открыть дверь
    {
        if (!requiresTumbler) // Если тумблер не требуется
        {
            return true; // Открывать можно
        }

        if (requiredTumbler == null) // Если тумблер нужен, но не назначен
        {
            return false; // Открывать нельзя
        }

        return requiredTumbler.isOn; // Открывать можно только если тумблер включён
    }

    public void Interact() // Взаимодействие игрока с дверью
    {
        if (isBusy) return; // Если дверь занята — выходим

        if (isLocked) return; // Если дверь заблокирована — выходим

        if (!isOpen) // Если дверь закрыта
        {
            if (CanOpenDoor()) // Если открыть можно
            {
                ToggleDoor(); // Открываем дверь
            }
        }
        else // Если дверь открыта
        {
            ToggleDoor(); // Закрываем дверь
        }
    }

    public void ToggleDoor() // Переключить дверь
    {
        if (isBusy) return; // Если дверь занята — выходим

        StartCoroutine(InteractSequence(!isOpen)); // Запускаем открытие или закрытие
    }

    public void OpenDoor() // Открыть дверь из другого скрипта
    {
        if (isBusy) return; // Если дверь занята — выходим

        if (isOpen) return; // Если дверь уже открыта — выходим

        if (isLocked) return; // Если дверь заблокирована — выходим

        if (!CanOpenDoor()) return; // Если условия открытия не выполнены — выходим

        StartCoroutine(InteractSequence(true)); // Открываем дверь
    }

    public void CloseDoor() // Закрыть дверь из другого скрипта
    {
        if (isBusy) return; // Если дверь занята — выходим

        if (!isOpen) return; // Если дверь уже закрыта — выходим

        StartCoroutine(InteractSequence(false)); // Закрываем дверь
    }

    public void SetLocked(bool value) // Заблокировать или разблокировать дверь
    {
        isLocked = value; // Записываем состояние замка
    }
    public void UnlockDoor() // Метод для разблокировки двери через UnityEvent
    {
    isLocked = false; // Снимаем блокировку с двери
    }

    public void LockDoor() // Метод для блокировки двери через UnityEvent
    {
    isLocked = true; // Включаем блокировку двери
    }

    public void SetMonsterCanOpen(bool value) // Метод настройки доступа монстра через UnityEvent
    {
    canMonsterOpen = value; // Разрешаем или запрещаем монстру открывать дверь
    }

    public void UnlockDoorAndBlockMonster() // Разблокировать дверь для игрока, но запретить монстру
    {
    isLocked = false; // Снимаем блокировку двери

    canMonsterOpen = false; // Запрещаем монстру открывать эту дверь
    }

    public void UnlockDoorAndAllowMonster() // Разблокировать дверь и разрешить монстру
    {
    isLocked = false; // Снимаем блокировку двери

    canMonsterOpen = true; // Разрешаем монстру открывать эту дверь
    }

    public void OpenDoorForMonster() // Открытие двери монстром
    {
        if (!canMonsterOpen) return; // Если монстру нельзя открыть — выходим

        if (isBusy) return; // Если дверь занята — выходим

        if (isOpen) return; // Если дверь уже открыта — выходим

        if (isLocked) return; // Если дверь заблокирована — выходим

        if (!CanOpenDoor()) return; // Если условия открытия не выполнены — выходим

        StartCoroutine(InteractSequence(true)); // Открываем дверь
    }

    private IEnumerator InteractSequence(bool targetOpenState) // Последовательность открытия/закрытия
    {
        isBusy = true; // Блокируем повторные нажатия

        yield return StartCoroutine(PressHandlesDown()); // Опускаем ручки

        if (doorOpenDelay > 0f) // Если есть задержка
        {
            yield return new WaitForSeconds(doorOpenDelay); // Ждём задержку
        }

        isOpen = targetOpenState; // Меняем состояние двери
        EmitDoorNoise(targetOpenState); // Создаём шум открытия или закрытия двери

        if (handleHoldTime > 0f) // Если есть пауза ручки
        {
            yield return new WaitForSeconds(handleHoldTime); // Ждём паузу
        }

        yield return StartCoroutine(ReturnHandlesBack()); // Возвращаем ручки

        isBusy = false; // Разрешаем новое взаимодействие
    }

    private IEnumerator PressHandlesDown() // Нажатие ручек
    {
        float t = 0f; // Прогресс анимации

        Quaternion outStart = outsideHandle != null ? outsideHandle.localRotation : Quaternion.identity; // Старт внешней ручки
        Quaternion inStart = insideHandle != null ? insideHandle.localRotation : Quaternion.identity; // Старт внутренней ручки

        while (t < 1f) // Пока анимация не закончилась
        {
            t += Time.deltaTime * handlePressSpeed; // Увеличиваем прогресс

            if (outsideHandle != null) // Если внешняя ручка есть
            {
                outsideHandle.localRotation = Quaternion.Lerp(outStart, outsideHandlePressedRotation, t); // Опускаем внешнюю ручку
            }

            if (insideHandle != null) // Если внутренняя ручка есть
            {
                insideHandle.localRotation = Quaternion.Lerp(inStart, insideHandlePressedRotation, t); // Опускаем внутреннюю ручку
            }

            yield return null; // Ждём следующий кадр
        }

        if (outsideHandle != null) outsideHandle.localRotation = outsideHandlePressedRotation; // Фиксируем внешнюю ручку
        if (insideHandle != null) insideHandle.localRotation = insideHandlePressedRotation; // Фиксируем внутреннюю ручку
    }

    private IEnumerator ReturnHandlesBack() // Возврат ручек
    {
        float t = 0f; // Прогресс анимации

        Quaternion outStart = outsideHandle != null ? outsideHandle.localRotation : Quaternion.identity; // Текущий поворот внешней ручки
        Quaternion inStart = insideHandle != null ? insideHandle.localRotation : Quaternion.identity; // Текущий поворот внутренней ручки

        while (t < 1f) // Пока анимация не закончилась
        {
            t += Time.deltaTime * handleReturnSpeed; // Увеличиваем прогресс

            if (outsideHandle != null) // Если внешняя ручка есть
            {
                outsideHandle.localRotation = Quaternion.Lerp(outStart, outsideHandleStartRotation, t); // Возвращаем внешнюю ручку
            }

            if (insideHandle != null) // Если внутренняя ручка есть
            {
                insideHandle.localRotation = Quaternion.Lerp(inStart, insideHandleStartRotation, t); // Возвращаем внутреннюю ручку
            }

            yield return null; // Ждём следующий кадр
        }

        if (outsideHandle != null) outsideHandle.localRotation = outsideHandleStartRotation; // Фиксируем внешнюю ручку
        if (insideHandle != null) insideHandle.localRotation = insideHandleStartRotation; // Фиксируем внутреннюю ручку
    }

    private void UpdateDoorRotation() // Плавное вращение двери
    {
        Quaternion target = isOpen ? openedRotation : closedRotation; // Выбираем цель: открыто или закрыто

        float speed = isOpen ? openSpeed : closeSpeed; // Выбираем скорость

        transform.localRotation = Quaternion.Slerp( // Плавно вращаем дверь
            transform.localRotation, // От текущего поворота
            target, // К целевому повороту
            Time.deltaTime * speed // С учётом времени
        );
    }
    private void EmitDoorNoise(bool targetOpenState) // Метод шума двери
    {
        if (noiseEmitter == null) return; // Если источника шума нет — выходим

    int noisePower = targetOpenState ? openNoisePower : closeNoisePower; // Если открываем — шум открытия, если закрываем — шум закрытия

    noiseEmitter.EmitNoise(noisePower); // Отправляем шум в NoiseEmitter
    }
}