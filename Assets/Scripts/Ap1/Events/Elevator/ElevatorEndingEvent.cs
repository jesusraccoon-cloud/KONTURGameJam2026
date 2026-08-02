using System.Collections; // Подключаем корутины, чтобы выполнять действия с ожиданием во времени
using UnityEngine; // Подключаем Unity-классы: GameObject, Transform, Vector3, Collider, Debug и другие
using StarterAssets; // Подключаем Starter Assets, чтобы работать с FirstPersonController и StarterAssetsInputs
using Gameplay.Quest; // Подключаем систему задач

public class ElevatorEndingEvent : MonoBehaviour // Главный скрипт лифтовой концовки
{
    [Header("Event State")] // Заголовок в Inspector для состояния события
    public bool eventUnlocked = false; // Показывает, разблокировано ли событие лифта после 6/6

    public bool eventStarted = false; // Показывает, началась ли уже катсцена входа в лифт

    [Header("Objects After 6/6")] // Заголовок в Inspector для объектов, которые меняются после 6/6
    public GameObject normalExitDoor; // Обычная дверь выхода, которую нужно выключить после 6/6

    public GameObject finalExitDoor; // Финальная дверь выхода, которую нужно включить после 6/6

    public GameObject elevatorRoom; // Объект лифта или комнаты лифта, который нужно включить после 6/6

    [Header("Elevator Physical Collider")] // Заголовок в Inspector для физического коллайдера лифта
    public Collider elevatorPhysicalCollider; // Коллайдер лифта, если его нужно включать вместе с лифтом

    [Header("Final Door Script")] // Заголовок в Inspector для скрипта финальной двери
    public FinalExitDoorOpenSignal finalExitDoorOpenSignal; // Скрипт финальной двери, которую нужно закрыть после входа игрока в лифт

    [Header("Player")] // Заголовок в Inspector для ссылок на игрока
    public Transform playerTransform; // Transform игрока, нужен для перемещения и поворота

    public CharacterController playerCharacterController; // CharacterController игрока, временно выключается при автоперемещении

    public FirstPersonController firstPersonController; // Скрипт управления игроком от Starter Assets

    public StarterAssetsInputs starterAssetsInputs; // Скрипт ввода игрока от Starter Assets

    public PlayerInteractor playerInteractor; // Скрипт взаимодействия игрока с объектами

    public PlayerCrouch playerCrouch; // Скрипт приседа игрока

    public PlayerHideController playerHideController; // Скрипт пряток игрока

    public PlayerNoiseController playerNoiseController; // Скрипт шума игрока

    [Header("Player Points")] // Заголовок в Inspector для точек движения игрока
    public Transform elevatorEntryPoint; // Точка, куда игрок должен автоматически зайти внутри лифта

    public Transform elevatorLookPoint; // Точка, на которую игрок должен повернуться после входа

    public float playerMoveSpeed = 1.5f; // Скорость автоматического движения игрока

    public float playerRotateSpeed = 5f; // Скорость автоматического поворота игрока

    [Header("Elevator Sliding Door")] // Заголовок в Inspector для сдвижной двери лифта
    public SlidingElevatorDoor slidingElevatorDoor; // Скрипт двери лифта, который будет закрывать дверь лифта

    [Header("Timing")] // Заголовок в Inspector для задержек катсцены
    public float delayBeforePlayerMove = 0.15f; // Задержка перед началом движения игрока в лифт

    public float delayAfterPlayerEntered = 0.2f; // Задержка после того, как игрок дошел до точки внутри лифта и дверь квартиры закрылась

    public float delayBeforeElevatorDoorClose = 0.4f; // Задержка перед закрытием двери лифта

    [Header("Debug")] // Заголовок в Inspector для отладочных сообщений
    public bool showDebugLogs = true; // Показывать ли сообщения Debug.Log в Console

    private void Start() // Метод запускается один раз при старте сцены
    {
        if (finalExitDoor != null) finalExitDoor.SetActive(false); // Если финальная дверь назначена, выключаем ее на старте

        if (elevatorRoom != null) elevatorRoom.SetActive(false); // Если лифт назначен, выключаем его на старте

        if (elevatorPhysicalCollider == null) elevatorPhysicalCollider = GetComponent<Collider>(); // Если коллайдер не назначен, пытаемся взять Collider с этого объекта

        if (elevatorPhysicalCollider != null) elevatorPhysicalCollider.enabled = false; // Если коллайдер найден, выключаем его на старте
    }

    public void UnlockElevatorEvent() // Метод вызывается после достижения 6/6
    {
        if (eventUnlocked == true) return; // Если событие уже разблокировано, повторно ничего не делаем

        eventUnlocked = true; // Запоминаем, что событие лифта разблокировано

        if (normalExitDoor != null) normalExitDoor.SetActive(false); // Выключаем обычную дверь выхода

        if (finalExitDoor != null) finalExitDoor.SetActive(true); // Включаем финальную дверь выхода

        if (elevatorRoom != null) elevatorRoom.SetActive(true); // Включаем лифт или комнату лифта

        if (elevatorPhysicalCollider != null) elevatorPhysicalCollider.enabled = true; // Включаем физический коллайдер лифта, если он назначен

        if (finalExitDoorOpenSignal != null) finalExitDoorOpenSignal.UnlockDoor(); // Разрешаем игроку взаимодействовать с финальной дверью

        if (showDebugLogs) Debug.Log("ElevatorEndingEvent: дверь заменена, лифт включен"); // Пишем в Console, что событие лифта разблокировано

        QuestService.Instance?.CompleteTask("escape"); // Отмечаем задачу «Добраться до лифта»
    }

    public void StartElevatorEnding() // Метод запускает катсцену входа в лифт
    {
        if (eventUnlocked == false) return; // Если событие не разблокировано, катсцену не запускаем

        if (eventStarted == true) return; // Если катсцена уже началась, повторно ее не запускаем

        StartCoroutine(ElevatorEndingRoutine()); // Запускаем основную корутину катсцены
    }

    private IEnumerator ElevatorEndingRoutine() // Основная корутина входа игрока в лифт
    {
        eventStarted = true; // Запоминаем, что катсцена началась

        DisablePlayerControl(); // Забираем управление у игрока

        yield return new WaitForSeconds(delayBeforePlayerMove); // Ждем перед началом автоматического движения

        if (elevatorEntryPoint != null) // Проверяем, назначена ли точка входа в лифт
        {
            yield return StartCoroutine(MovePlayerToPoint(elevatorEntryPoint)); // Двигаем игрока к точке входа и ждем завершения
        }
        else // Если точка входа не назначена
        {
            Debug.LogError("ElevatorEndingEvent: Elevator Entry Point не назначен в Inspector"); // Пишем ошибку в Console
        }

        if (finalExitDoorOpenSignal != null) // Проверяем, назначен ли скрипт финальной двери квартиры
        {
            if (showDebugLogs) Debug.Log("ElevatorEndingEvent: закрываю финальную дверь за игроком"); // Пишем лог перед закрытием двери квартиры

            yield return StartCoroutine(finalExitDoorOpenSignal.CloseDoorAndWait()); // Закрываем дверь квартиры за игроком и ждем окончания закрытия
        }
        else // Если скрипт финальной двери не назначен
        {
            Debug.LogError("ElevatorEndingEvent: Final Exit Door Open Signal не назначен в Inspector"); // Пишем ошибку в Console
        }

        yield return new WaitForSeconds(delayAfterPlayerEntered); // Ждем после входа игрока и закрытия двери квартиры

        if (elevatorLookPoint != null) // Проверяем, назначена ли точка взгляда
        {
            yield return StartCoroutine(RotatePlayerToPoint(elevatorLookPoint)); // Разворачиваем игрока к точке взгляда
        }
        else // Если точка взгляда не назначена
        {
            Debug.LogError("ElevatorEndingEvent: Elevator Look Point не назначен в Inspector"); // Пишем ошибку в Console
        }

        yield return new WaitForSeconds(delayBeforeElevatorDoorClose); // Ждем перед закрытием двери лифта

        if (slidingElevatorDoor != null) // Проверяем, назначен ли скрипт двери лифта
        {
            if (showDebugLogs) Debug.Log("ElevatorEndingEvent: вызываю закрытие двери лифта"); // Пишем лог перед закрытием двери лифта

            yield return StartCoroutine(slidingElevatorDoor.CloseDoorAndWait()); // Закрываем дверь лифта и ждем окончания
        }
        else // Если скрипт двери лифта не назначен
        {
            Debug.LogError("ElevatorEndingEvent: Sliding Elevator Door не назначен в Inspector"); // Пишем ошибку в Console
        }

        if (showDebugLogs) Debug.Log("ElevatorEndingEvent: игрок вошел в лифт, дверь квартиры закрылась, игрок развернулся, дверь лифта закрылась"); // Пишем финальный лог катсцены
    }

    private IEnumerator MovePlayerToPoint(Transform targetPoint) // Корутина движения игрока к точке
    {
        if (playerTransform == null) yield break; // Если Transform игрока не назначен, выходим из корутины

        if (targetPoint == null) yield break; // Если целевая точка не назначена, выходим из корутины

        if (playerCharacterController != null) playerCharacterController.enabled = false; // Выключаем CharacterController, чтобы он не мешал двигать игрока вручную

        while (Vector3.Distance(playerTransform.position, targetPoint.position) > 0.03f) // Пока игрок не подошел достаточно близко к точке
        {
            playerTransform.position = Vector3.MoveTowards(playerTransform.position, targetPoint.position, playerMoveSpeed * Time.deltaTime); // Двигаем игрока к точке

            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetPoint.rotation, playerRotateSpeed * Time.deltaTime); // Плавно поворачиваем игрока к повороту точки

            yield return null; // Ждем следующий кадр
        }

        playerTransform.position = targetPoint.position; // Точно ставим игрока в позицию точки

        playerTransform.rotation = targetPoint.rotation; // Точно ставим игрока в поворот точки

        if (playerCharacterController != null) playerCharacterController.enabled = true; // Включаем CharacterController обратно
    }

    private IEnumerator RotatePlayerToPoint(Transform lookPoint) // Корутина поворота игрока к точке
    {
        if (playerTransform == null) yield break; // Если Transform игрока не назначен, выходим

        if (lookPoint == null) yield break; // Если точка взгляда не назначена, выходим

        Vector3 direction = lookPoint.position - playerTransform.position; // Считаем направление от игрока к точке взгляда

        direction.y = 0f; // Убираем вертикаль, чтобы поворот был только по горизонтали

        if (direction.sqrMagnitude <= 0.001f) yield break; // Если направление слишком маленькое, выходим

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized); // Создаем нужный поворот игрока

        float timer = 0f; // Таймер защиты от бесконечного цикла

        while (Quaternion.Angle(playerTransform.rotation, targetRotation) > 1f && timer < 3f) // Крутим игрока, пока угол больше 1 градуса и не прошло 3 секунды
        {
            timer += Time.deltaTime; // Увеличиваем таймер на время кадра

            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, playerRotateSpeed * Time.deltaTime); // Плавно поворачиваем игрока

            yield return null; // Ждем следующий кадр
        }

        playerTransform.rotation = targetRotation; // Точно ставим нужный поворот игрока
    }

    private void DisablePlayerControl() // Метод отключает управление игроком
    {
        if (firstPersonController != null) firstPersonController.enabled = false; // Отключаем движение игрока

        if (starterAssetsInputs != null) starterAssetsInputs.enabled = false; // Отключаем ввод игрока

        if (playerInteractor != null) playerInteractor.enabled = false; // Отключаем взаимодействие игрока с объектами

        if (playerCrouch != null) playerCrouch.enabled = false; // Отключаем присед игрока

        if (playerHideController != null) playerHideController.enabled = false; // Отключаем возможность прятаться

        if (playerNoiseController != null) playerNoiseController.enabled = false; // Отключаем шум игрока
    }
}