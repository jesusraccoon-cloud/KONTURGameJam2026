using System.Collections; // Подключаем корутины
using UnityEngine; // Подключаем Unity
using StarterAssets; // Подключаем Starter Assets

public class ElevatorEndingEvent : MonoBehaviour // Главный ивент лифтовой концовки
{
    [Header("Event State")] // Блок состояния события
    public bool eventUnlocked = false; // Разблокирован ли ивент после 6/6

    public bool eventStarted = false; // Началась ли катсцена лифта

    [Header("Objects After 6/6")] // Блок объектов после 6/6
    public GameObject normalExitDoor; // Обычная дверь выхода из квартиры

    public GameObject finalExitDoor; // Финальная дверь выхода, которая появляется после 6/6

    public GameObject elevatorRoom; // Лифт, который включается после 6/6

    [Header("Final Door Script")] // Блок скрипта финальной двери
    public FinalExitDoorOpenSignal finalExitDoorOpenSignal; // Скрипт двери, который разблокируется после 6/6

    [Header("Player")] // Блок игрока
    public Transform playerTransform; // Transform игрока

    public CharacterController playerCharacterController; // CharacterController игрока

    public FirstPersonController firstPersonController; // Контроллер движения игрока

    public StarterAssetsInputs starterAssetsInputs; // Скрипт ввода игрока

    public PlayerInteractor playerInteractor; // Скрипт взаимодействия игрока

    public PlayerCrouch playerCrouch; // Скрипт приседа игрока

    public PlayerHideController playerHideController; // Скрипт пряток игрока

    public PlayerNoiseController playerNoiseController; // Скрипт шума игрока

    [Header("Player Points")] // Блок точек игрока
    public Transform elevatorEntryPoint; // Точка, куда игрок заходит в лифт

    public Transform elevatorLookPoint; // Точка, на которую игрок разворачивается внутри лифта

    public float playerMoveSpeed = 1.5f; // Скорость движения игрока

    public float playerRotateSpeed = 5f; // Скорость поворота игрока

    [Header("Elevator Sliding Door")] // Блок двери лифта
    public SlidingElevatorDoor slidingElevatorDoor; // Скрипт сдвижной двери лифта

    [Header("Timing")] // Блок задержек
    public float delayBeforePlayerMove = 0.15f; // Задержка перед входом игрока

    public float delayAfterPlayerEntered = 0.2f; // Задержка после входа игрока

    public float delayBeforeElevatorDoorClose = 0.4f; // Задержка перед закрытием двери лифта

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = true; // Показывать debug-логи

    private void Start() // Запуск сцены
    {
        if (finalExitDoor != null) finalExitDoor.SetActive(false); // Выключаем финальную дверь на старте

        if (elevatorRoom != null) elevatorRoom.SetActive(false); // Выключаем лифт на старте
    }

    public void UnlockElevatorEvent() // Вызывается из ApartmentFinalSequence после 6/6
    {
        if (eventUnlocked == true) return; // Если ивент уже разблокирован — выходим

        eventUnlocked = true; // Разблокируем ивент

        if (normalExitDoor != null) normalExitDoor.SetActive(false); // Выключаем обычную дверь

        if (finalExitDoor != null) finalExitDoor.SetActive(true); // Включаем финальную дверь

        if (elevatorRoom != null) elevatorRoom.SetActive(true); // Включаем лифт

        if (finalExitDoorOpenSignal != null) finalExitDoorOpenSignal.UnlockDoor(); // Разблокируем E на финальной двери

        if (showDebugLogs) Debug.Log("ElevatorEndingEvent: дверь заменена, лифт включён, подъезд не отключался"); // Пишем лог
    }

    public void StartElevatorEnding() // Запуск катсцены после E на финальной двери
    {
        if (eventUnlocked == false) return; // Если 6/6 ещё не было — выходим

        if (eventStarted == true) return; // Если катсцена уже стартовала — выходим

        StartCoroutine(ElevatorEndingRoutine()); // Запускаем катсцену
    }

    private IEnumerator ElevatorEndingRoutine() // Катсцена входа в лифт
    {
        eventStarted = true; // Запоминаем старт катсцены

        DisablePlayerControl(); // Забираем управление у игрока

        yield return new WaitForSeconds(delayBeforePlayerMove); // Ждём перед движением

        if (elevatorEntryPoint != null) yield return StartCoroutine(MovePlayerToPoint(elevatorEntryPoint)); // Заводим игрока в лифт

        yield return new WaitForSeconds(delayAfterPlayerEntered); // Ждём после входа

        if (elevatorLookPoint != null) yield return StartCoroutine(RotatePlayerToPoint(elevatorLookPoint)); // Разворачиваем игрока к двери лифта

        yield return new WaitForSeconds(delayBeforeElevatorDoorClose); // Ждём перед закрытием двери лифта

        if (slidingElevatorDoor != null) yield return StartCoroutine(slidingElevatorDoor.CloseDoorAndWait()); // Закрываем дверь лифта

        if (showDebugLogs) Debug.Log("ElevatorEndingEvent: игрок вошёл в лифт, развернулся, дверь лифта закрылась"); // Пишем лог
    }

    private IEnumerator MovePlayerToPoint(Transform targetPoint) // Движение игрока к точке
    {
        if (playerTransform == null) yield break; // Если игрок не назначен — выходим

        if (targetPoint == null) yield break; // Если точка не назначена — выходим

        if (playerCharacterController != null) playerCharacterController.enabled = false; // Выключаем CharacterController

        while (Vector3.Distance(playerTransform.position, targetPoint.position) > 0.03f) // Пока игрок не дошёл до точки
        {
            playerTransform.position = Vector3.MoveTowards(playerTransform.position, targetPoint.position, playerMoveSpeed * Time.deltaTime); // Двигаем игрока

            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetPoint.rotation, playerRotateSpeed * Time.deltaTime); // Поворачиваем игрока

            yield return null; // Ждём следующий кадр
        }

        playerTransform.position = targetPoint.position; // Фиксируем позицию игрока

        playerTransform.rotation = targetPoint.rotation; // Фиксируем поворот игрока

        if (playerCharacterController != null) playerCharacterController.enabled = true; // Включаем CharacterController обратно
    }

    private IEnumerator RotatePlayerToPoint(Transform lookPoint) // Разворот игрока к точке
    {
        if (playerTransform == null) yield break; // Если игрок не назначен — выходим

        if (lookPoint == null) yield break; // Если точка взгляда не назначена — выходим

        Vector3 direction = lookPoint.position - playerTransform.position; // Считаем направление к точке

        direction.y = 0f; // Убираем вертикальный наклон

        if (direction.sqrMagnitude <= 0.001f) yield break; // Если направление слишком маленькое — выходим

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized); // Считаем нужный поворот

        float timer = 0f; // Таймер защиты

        while (Quaternion.Angle(playerTransform.rotation, targetRotation) > 1f && timer < 3f) // Пока игрок не повернулся
        {
            timer += Time.deltaTime; // Увеличиваем таймер

            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, playerRotateSpeed * Time.deltaTime); // Плавно поворачиваем игрока

            yield return null; // Ждём следующий кадр
        }

        playerTransform.rotation = targetRotation; // Фиксируем поворот игрока
    }

    private void DisablePlayerControl() // Отключение управления игроком
    {
        if (firstPersonController != null) firstPersonController.enabled = false; // Отключаем движение

        if (starterAssetsInputs != null) starterAssetsInputs.enabled = false; // Отключаем ввод

        if (playerInteractor != null) playerInteractor.enabled = false; // Отключаем взаимодействие

        if (playerCrouch != null) playerCrouch.enabled = false; // Отключаем присед

        if (playerHideController != null) playerHideController.enabled = false; // Отключаем прятки

        if (playerNoiseController != null) playerNoiseController.enabled = false; // Отключаем шум игрока
    }
}