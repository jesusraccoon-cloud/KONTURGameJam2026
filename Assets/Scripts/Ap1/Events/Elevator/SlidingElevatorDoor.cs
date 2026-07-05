using System.Collections; // Подключаем корутины
using UnityEngine; // Подключаем Unity

public class SlidingElevatorDoor : MonoBehaviour // Скрипт раздвижной/сдвижной двери лифта
{
    [Header("Door Part")] // Блок двери
    public Transform doorPart; // Та часть двери, которая будет двигаться

    [Header("Positions")] // Блок позиций
    public Vector3 closedLocalPosition; // Локальная позиция закрытой двери

    public Vector3 openedLocalOffset = new Vector3(-1.2f, 0f, 0f); // Насколько дверь уезжает при открытии

    [Header("Speed")] // Блок скорости
    public float openSpeed = 2f; // Скорость открытия двери

    public float closeSpeed = 2f; // Скорость закрытия двери

    [Header("Start State")] // Блок стартового состояния
    public bool startOpened = true; // Дверь лифта на старте открыта

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = true; // Показывать сообщения в Console

    private Vector3 openedLocalPosition; // Локальная позиция открытой двери

    private bool isBusy = false; // Занята ли дверь анимацией

    private void Start() // Запускается при старте сцены
    {
        if (doorPart == null) doorPart = transform; // Если часть двери не назначена, двигаем этот объект

        closedLocalPosition = doorPart.localPosition; // Запоминаем закрытую позицию двери

        openedLocalPosition = closedLocalPosition + openedLocalOffset; // Считаем открытую позицию двери

        if (startOpened == true) doorPart.localPosition = openedLocalPosition; // Если дверь должна быть открыта, сразу ставим её в открытую позицию

        if (startOpened == false) doorPart.localPosition = closedLocalPosition; // Если дверь должна быть закрыта, оставляем её закрытой
    }

    public void OpenDoor() // Открыть дверь лифта
    {
        if (isBusy == true) return; // Если дверь уже двигается, выходим

        StartCoroutine(MoveDoorRoutine(openedLocalPosition, openSpeed)); // Запускаем открытие
    }

    public void CloseDoor() // Закрыть дверь лифта
    {
        if (isBusy == true) return; // Если дверь уже двигается, выходим

        StartCoroutine(MoveDoorRoutine(closedLocalPosition, closeSpeed)); // Запускаем закрытие
    }

    public IEnumerator CloseDoorAndWait() // Закрыть дверь и дождаться конца
    {
        if (isBusy == true) yield break; // Если дверь уже двигается, выходим

        yield return StartCoroutine(MoveDoorRoutine(closedLocalPosition, closeSpeed)); // Закрываем дверь и ждём завершения
    }

    public IEnumerator OpenDoorAndWait() // Открыть дверь и дождаться конца
    {
        if (isBusy == true) yield break; // Если дверь уже двигается, выходим

        yield return StartCoroutine(MoveDoorRoutine(openedLocalPosition, openSpeed)); // Открываем дверь и ждём завершения
    }

    private IEnumerator MoveDoorRoutine(Vector3 targetLocalPosition, float speed) // Плавное движение двери
    {
        isBusy = true; // Блокируем повторный запуск

        Vector3 startLocalPosition = doorPart.localPosition; // Запоминаем текущую позицию двери

        float t = 0f; // Создаём прогресс движения

        while (t < 1f) // Пока движение не завершено
        {
            t += Time.deltaTime * speed; // Увеличиваем прогресс с учётом скорости

            doorPart.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, t); // Плавно двигаем дверь

            yield return null; // Ждём следующий кадр
        }

        doorPart.localPosition = targetLocalPosition; // Точно ставим дверь в конечную позицию

        isBusy = false; // Разрешаем новый запуск

        if (showDebugLogs == true) Debug.Log("Дверь лифта закончила движение"); // Пишем лог
    }
}