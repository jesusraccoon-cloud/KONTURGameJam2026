using System.Collections; // Подключаем корутины, чтобы дверь могла плавно двигаться по кадрам
using UnityEngine; // Подключаем Unity-классы: MonoBehaviour, Transform, Vector3, Time и Debug

public class SlidingElevatorDoor : MonoBehaviour // Скрипт сдвижной двери лифта
{
    [Header("Door Part")] // Заголовок в Inspector для части двери
    public Transform doorPart; // Объект, который реально должен двигаться, например Body двери

    [Header("Close Movement")] // Заголовок в Inspector для настройки закрытия
    public Vector3 closedLocalOffset = new Vector3(1.2f, 0f, 0f); // Смещение от открытой позиции до закрытой позиции

    [Header("Speed")] // Заголовок в Inspector для скорости
    public float closeSpeed = 2f; // Скорость закрытия двери

    public float openSpeed = 2f; // Скорость открытия двери, если понадобится открыть обратно

    [Header("Debug")] // Заголовок в Inspector для отладки
    public bool showDebugLogs = true; // Показывать сообщения в Console

    private Vector3 openedLocalPosition; // Локальная позиция двери, когда она открыта

    private Vector3 closedLocalPosition; // Локальная позиция двери, когда она закрыта

    private bool isBusy = false; // Показывает, двигается ли дверь прямо сейчас

    private bool isInitialized = false; // Показывает, были ли уже рассчитаны позиции двери

    private void Awake() // Запускается при создании объекта
    {
        InitializeDoorPositions(); // Настраиваем позиции двери
    }

    private void OnEnable() // Запускается каждый раз, когда объект включается в иерархии
    {
        InitializeDoorPositions(); // Проверяем настройку позиций после включения лифта
    }

    private void InitializeDoorPositions() // Метод рассчитывает открытую и закрытую позиции двери
    {
        if (isInitialized == true) return; // Если позиции уже рассчитаны, повторно их не перезаписываем

        if (doorPart == null) doorPart = transform; // Если Door Part не назначен, двигаем объект со скриптом

        openedLocalPosition = doorPart.localPosition; // Запоминаем текущую позицию как открытую

        closedLocalPosition = openedLocalPosition + closedLocalOffset; // Считаем закрытую позицию через смещение

        isInitialized = true; // Запоминаем, что дверь настроена

        if (showDebugLogs == true) Debug.Log("SlidingElevatorDoor: открытая позиция = " + openedLocalPosition + ", закрытая позиция = " + closedLocalPosition); // Пишем позиции в Console
    }

    public void CloseDoor() // Закрыть дверь без ожидания
    {
        if (isBusy == true) return; // Если дверь уже двигается, не запускаем второй раз

        StartCoroutine(MoveDoorRoutine(closedLocalPosition, closeSpeed)); // Запускаем движение к закрытой позиции
    }

    public IEnumerator CloseDoorAndWait() // Закрыть дверь и дождаться окончания
    {
        InitializeDoorPositions(); // Гарантируем, что позиции рассчитаны

        if (isBusy == true) yield break; // Если дверь уже двигается, выходим

        if (showDebugLogs == true) Debug.Log("SlidingElevatorDoor: начинаю закрывать дверь"); // Пишем лог старта закрытия

        yield return StartCoroutine(MoveDoorRoutine(closedLocalPosition, closeSpeed)); // Двигаем дверь к закрытой позиции и ждем завершения
    }

    public void OpenDoor() // Открыть дверь без ожидания
    {
        if (isBusy == true) return; // Если дверь уже двигается, не запускаем второй раз

        StartCoroutine(MoveDoorRoutine(openedLocalPosition, openSpeed)); // Запускаем движение к открытой позиции
    }

    public IEnumerator OpenDoorAndWait() // Открыть дверь и дождаться окончания
    {
        InitializeDoorPositions(); // Гарантируем, что позиции рассчитаны

        if (isBusy == true) yield break; // Если дверь уже двигается, выходим

        yield return StartCoroutine(MoveDoorRoutine(openedLocalPosition, openSpeed)); // Двигаем дверь к открытой позиции и ждем завершения
    }

    private IEnumerator MoveDoorRoutine(Vector3 targetLocalPosition, float speed) // Плавное движение двери
    {
        isBusy = true; // Помечаем дверь занятой

        while (Vector3.Distance(doorPart.localPosition, targetLocalPosition) > 0.01f) // Двигаемся, пока дверь не дошла почти до цели
        {
            doorPart.localPosition = Vector3.MoveTowards(doorPart.localPosition, targetLocalPosition, speed * Time.deltaTime); // Двигаем дверь к цели с постоянной скоростью

            yield return null; // Ждем следующий кадр
        }

        doorPart.localPosition = targetLocalPosition; // Точно ставим дверь в конечную позицию

        isBusy = false; // Разрешаем следующий запуск движения

        if (showDebugLogs == true) Debug.Log("SlidingElevatorDoor: дверь закончила движение, текущая позиция = " + doorPart.localPosition); // Пишем финальную позицию
    }
}