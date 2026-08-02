using System.Collections; // Подключаем корутины, чтобы открывать и закрывать дверь плавно по кадрам
using UnityEngine; // Подключаем Unity-классы: MonoBehaviour, Transform, Quaternion, AudioSource, Debug и другие

public class FinalExitDoorOpenSignal : MonoBehaviour, IInteractable // Скрипт финальной двери: открывает дверь и запускает лифтовый ивент
{
    [Header("State")] // Заголовок в Inspector для состояния двери
    public bool isUnlocked = false; // Разрешена ли дверь после 6/6

    public bool wasUsed = false; // Использовал ли игрок эту дверь уже один раз

    [Header("Door Opening")] // Заголовок в Inspector для открытия и закрытия двери
    public Transform doorPivot; // Петля/объект двери, который будет вращаться

    public Vector3 openLocalEuler = new Vector3(0f, 90f, 0f); // Локальный угол, на который дверь повернется при открытии

    public float openSpeed = 2f; // Скорость открытия двери

    public float closeSpeed = 2f; // Скорость закрытия двери

    [Header("Signal")] // Заголовок в Inspector для связи с лифтовым событием
    public ElevatorEndingEvent elevatorEndingEvent; // Скрипт лифтовой катсцены, который нужно запустить после открытия двери

    public bool signalAfterDoorOpened = true; // Если true, лифтовая катсцена начнется после полного открытия двери

    [Header("Audio")] // Заголовок в Inspector для звука двери
    public AudioSource audioSource; // Источник звука двери

    public AudioClip doorOpenSound; // Звук открытия двери

    public AudioClip doorCloseSound; // Звук закрытия двери

    [Header("Debug")] // Заголовок в Inspector для отладки
    public bool showDebugLogs = true; // Показывать ли сообщения Debug.Log в Console

    private Quaternion closedRotation; // Поворот двери в закрытом состоянии

    private Quaternion openedRotation; // Поворот двери в открытом состоянии

    private bool isOpen = false; // Открыта ли дверь сейчас

    private bool isMoving = false; // Двигается ли дверь сейчас

    private void Start() // Запускается один раз при старте сцены
    {
        if (doorPivot != null) closedRotation = doorPivot.localRotation; // Если дверь назначена, запоминаем текущий поворот как закрытый

        if (doorPivot != null) openedRotation = closedRotation * Quaternion.Euler(openLocalEuler); // Если дверь назначена, считаем открытый поворот от закрытого
    }

    public void UnlockDoor() // Разблокировать дверь после 6/6
    {
        isUnlocked = true; // Разрешаем игроку взаимодействовать с дверью

        if (showDebugLogs) Debug.Log("Финальная дверь выхода разблокирована"); // Пишем сообщение в Console
    }

    public void Interact() // Метод вызывается PlayerInteractor, когда игрок нажимает E по двери
    {
        if (isUnlocked == false) return; // Если дверь еще не разблокирована, ничего не делаем

        if (wasUsed == true) return; // Если дверь уже использовали, повторно не запускаем катсцену

        if (isMoving == true) return; // Если дверь сейчас двигается, не запускаем новое действие

        wasUsed = true; // Запоминаем, что игрок уже использовал дверь

        StartCoroutine(OpenDoorAndSignalRoutine()); // Запускаем последовательность открытия двери и сигнала лифту
    }

    private IEnumerator OpenDoorAndSignalRoutine() // Последовательность открытия двери и запуска лифта
    {
        if (signalAfterDoorOpened == false) SendSignalToElevatorEnding(); // Если нужно запускать лифт сразу, отправляем сигнал до открытия

        yield return StartCoroutine(OpenDoorAndWait()); // Открываем дверь и ждем завершения открытия

        if (signalAfterDoorOpened == true) SendSignalToElevatorEnding(); // Если нужно ждать открытия, отправляем сигнал после полного открытия
    }

    public IEnumerator OpenDoorAndWait() // Открыть дверь и дождаться конца движения
    {
        if (doorPivot == null) yield break; // Если дверь не назначена, прекращаем корутину

        if (isOpen == true) yield break; // Если дверь уже открыта, прекращаем корутину

        if (isMoving == true) yield break; // Если дверь уже двигается, прекращаем корутину

        if (audioSource != null && doorOpenSound != null) audioSource.PlayOneShot(doorOpenSound); // Если звук назначен, проигрываем звук открытия

        yield return StartCoroutine(RotateDoorRoutine(doorPivot.localRotation, openedRotation, openSpeed)); // Плавно вращаем дверь в открытое положение

        isOpen = true; // Запоминаем, что дверь теперь открыта

        if (showDebugLogs) Debug.Log("Финальная дверь открылась"); // Пишем сообщение в Console
    }

    public IEnumerator CloseDoorAndWait() // Закрыть дверь и дождаться конца движения
    {
        if (doorPivot == null) yield break; // Если дверь не назначена, прекращаем корутину

        if (isOpen == false) yield break; // Если дверь уже закрыта, прекращаем корутину

        if (isMoving == true) yield break; // Если дверь сейчас двигается, прекращаем корутину

        if (audioSource != null && doorCloseSound != null) audioSource.PlayOneShot(doorCloseSound); // Если звук назначен, проигрываем звук закрытия

        yield return StartCoroutine(RotateDoorRoutine(doorPivot.localRotation, closedRotation, closeSpeed)); // Плавно вращаем дверь в закрытое положение

        isOpen = false; // Запоминаем, что дверь теперь закрыта

        if (showDebugLogs) Debug.Log("Финальная дверь закрылась за игроком"); // Пишем сообщение в Console
    }

    private IEnumerator RotateDoorRoutine(Quaternion startRotation, Quaternion targetRotation, float speed) // Плавный поворот двери от одной ротации к другой
    {
        isMoving = true; // Помечаем дверь как двигающуюся

        float t = 0f; // Создаем прогресс движения от 0 до 1

        while (t < 1f) // Пока прогресс меньше 1, продолжаем движение
        {
            t += Time.deltaTime * speed; // Увеличиваем прогресс с учетом скорости и времени кадра

            doorPivot.localRotation = Quaternion.Slerp(startRotation, targetRotation, t); // Плавно поворачиваем дверь между стартовым и целевым поворотом

            yield return null; // Ждем следующий кадр
        }

        doorPivot.localRotation = targetRotation; // Точно ставим дверь в конечный поворот

        isMoving = false; // Помечаем дверь как свободную
    }

    private void SendSignalToElevatorEnding() // Отправить сигнал в лифтовую катсцену
    {
        if (elevatorEndingEvent == null) return; // Если скрипт лифта не назначен, ничего не делаем

        elevatorEndingEvent.StartElevatorEnding(); // Запускаем катсцену входа в лифт

        if (showDebugLogs) Debug.Log("Финальная дверь дала сигнал ElevatorEndingEvent"); // Пишем сообщение в Console
    }
    
}