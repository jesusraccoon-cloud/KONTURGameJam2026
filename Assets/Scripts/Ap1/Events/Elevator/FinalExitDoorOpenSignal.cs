using System.Collections; // Подключаем корутины
using UnityEngine; // Подключаем Unity

public class FinalExitDoorOpenSignal : MonoBehaviour, IInteractable // Скрипт финальной двери: открыть дверь и запустить лифтовый ивент
{
    [Header("State")] // Блок состояния
    public bool isUnlocked = false; // Разрешена ли дверь после 6/6

    public bool wasUsed = false; // Была ли дверь уже использована

    [Header("Door Opening")] // Блок открытия двери
    public Transform doorPivot; // Петля двери, которую нужно открыть

    public Vector3 openLocalEuler = new Vector3(0f, 90f, 0f); // Локальный поворот открытия двери

    public float openSpeed = 2f; // Скорость открытия двери

    [Header("Signal")] // Блок сигнала
    public ElevatorEndingEvent elevatorEndingEvent; // Главный ивент лифта

    public bool signalImmediatelyAfterPress = true; // Запускать лифт сразу после нажатия E, не дожидаясь открытия двери

    [Header("Audio")] // Блок звука
    public AudioSource audioSource; // Источник звука

    public AudioClip doorOpenSound; // Звук открытия двери

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = true; // Показывать debug-сообщения

    private Quaternion closedRotation; // Закрытый поворот двери

    private Quaternion openedRotation; // Открытый поворот двери

    private void Start() // Запуск сцены
    {
        if (doorPivot != null) closedRotation = doorPivot.localRotation; // Запоминаем закрытый поворот двери

        if (doorPivot != null) openedRotation = closedRotation * Quaternion.Euler(openLocalEuler); // Считаем открытый поворот двери
    }

    public void UnlockDoor() // Разблокировать дверь после 6/6
    {
        isUnlocked = true; // Разрешаем взаимодействие

        if (showDebugLogs) Debug.Log("Финальная дверь выхода разблокирована"); // Пишем лог
    }

    public void Interact() // Вызывается PlayerInteractor при E
    {
        if (isUnlocked == false) return; // Если дверь ещё не разблокирована — выходим

        if (wasUsed == true) return; // Если дверь уже использовалась — выходим

        wasUsed = true; // Запоминаем использование двери

        if (audioSource != null && doorOpenSound != null) audioSource.PlayOneShot(doorOpenSound); // Проигрываем звук двери

        if (signalImmediatelyAfterPress == true) SendSignalToElevatorEnding(); // Сразу запускаем лифтовый ивент

        StartCoroutine(OpenDoorRoutine()); // Запускаем анимацию открытия двери
    }

    private IEnumerator OpenDoorRoutine() // Корутина открытия двери
    {
        float t = 0f; // Прогресс открытия

        while (t < 1f) // Пока дверь не открылась
        {
            t += Time.deltaTime * openSpeed; // Увеличиваем прогресс

            if (doorPivot != null) doorPivot.localRotation = Quaternion.Slerp(closedRotation, openedRotation, t); // Плавно открываем дверь

            yield return null; // Ждём следующий кадр
        }

        if (doorPivot != null) doorPivot.localRotation = openedRotation; // Фиксируем открытую дверь

        if (signalImmediatelyAfterPress == false) SendSignalToElevatorEnding(); // Если выбрано ждать открытия — запускаем ивент после открытия
    }

    private void SendSignalToElevatorEnding() // Отправка сигнала в ElevatorEndingEvent
    {
        if (elevatorEndingEvent == null) return; // Если ивент не назначен — выходим

        elevatorEndingEvent.StartElevatorEnding(); // Запускаем лифтовую катсцену

        if (showDebugLogs) Debug.Log("Финальная дверь дала сигнал ElevatorEndingEvent"); // Пишем лог
    }
}