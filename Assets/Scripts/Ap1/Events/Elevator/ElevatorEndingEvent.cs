using System.Collections; // Подключаем корутины, чтобы выполнять действия с ожиданием во времени
using UnityEngine; // Подключаем Unity-классы: GameObject, Transform, Vector3, Collider, Debug и другие
using UnityEngine.Events; // UnityEvent — хук на старт концовки (например включить музыку)
using StarterAssets; // Подключаем Starter Assets, чтобы работать с FirstPersonController и StarterAssetsInputs
using Gameplay.Quest;
using UnityEngine.SceneManagement; // Подключаем систему задач

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

    [Header("Events")] // Заголовок в Inspector для хуков события
    public UnityEvent onEndingStart; // Вызывается в момент старта концовки — вешай сюда SC_MusicPlayer.Play (финальная музыка)

    [Header("Debug")] // Заголовок в Inspector для отладочных сообщений
    public bool showDebugLogs = true; // Показывать ли сообщения Debug.Log в Console

    private Vector3 lockedPlayerPosition; // Позиция, в которой игрок должен оставаться после входа в лифт

    private bool lockPlayerPosition = false; // Нужно ли удерживать игрока на месте после входа

    private void LateUpdate() // Выполняется после обычного Update всех компонентов
    {
        if (!lockPlayerPosition) return; // Если фиксация не включена, ничего не делаем

        if (playerTransform == null) return; // Если игрок не назначен, выходим

        playerTransform.position = lockedPlayerPosition; // Гарантированно удерживаем игрока в точке после входа
    }

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

        QuestManager.Instance?.CompleteTask("escape"); // Отмечаем задачу «Добраться до лифта»
    }

    [ContextMenu("Выиграть")]
    public void StartElevatorEnding() // Метод запускает катсцену входа в лифт
    {
        if (eventUnlocked == false) return; // Если событие не разблокировано, катсцену не запускаем

        if (eventStarted == true) return; // Если катсцена уже началась, повторно ее не запускаем

        StartCoroutine(ElevatorEndingRoutine()); // Запускаем основную корутину катсцены
    }

    private IEnumerator ElevatorEndingRoutine() // Основная корутина входа игрока в лифт
    {
        eventStarted = true; // Запоминаем, что катсцена началась

        onEndingStart.Invoke(); // Хук старта концовки — включаем финальную музыку и т.п.

        DisablePlayerControl(); // Забираем управление у игрока

        yield return new WaitForSeconds(delayBeforePlayerMove); // Ждём перед началом автоматического входа

        if (elevatorEntryPoint != null) // Проверяем, назначена ли точка входа в лифт
        {
            yield return StartCoroutine(MovePlayerToPoint(elevatorEntryPoint)); // Двигаем игрока к точке внутри лифта
        }
        else // Если точка входа не назначена
        {
            Debug.LogError("ElevatorEndingEvent: Elevator Entry Point не назначен в Inspector"); // Пишем ошибку в Console
        }

        if (elevatorLookPoint != null) // Проверяем, назначена ли точка взгляда
        {
            yield return StartCoroutine(RotatePlayerToPoint(elevatorLookPoint)); // Сразу разворачиваем уже остановившегося игрока
        }
        else // Если точка взгляда не назначена
        {
            Debug.LogError("ElevatorEndingEvent: Elevator Look Point не назначен в Inspector"); // Пишем ошибку в Console
        }

        if (finalExitDoorOpenSignal != null) // Проверяем, назначен ли скрипт финальной двери квартиры
        {
            if (showDebugLogs) Debug.Log("ElevatorEndingEvent: игрок зафиксирован, закрываю финальную дверь"); // Пишем лог перед закрытием двери

            yield return StartCoroutine(finalExitDoorOpenSignal.CloseDoorAndWait()); // Закрываем дверь после фиксации игрока
        }
        else // Если скрипт финальной двери не назначен
        {
            Debug.LogError("ElevatorEndingEvent: Final Exit Door Open Signal не назначен в Inspector"); // Пишем ошибку в Console
        }

        yield return new WaitForSeconds(delayAfterPlayerEntered); // Ждём после разворота и закрытия двери квартиры

        yield return new WaitForSeconds(delayBeforeElevatorDoorClose); // Ждём перед закрытием двери лифта

        if (slidingElevatorDoor != null) // Проверяем, назначен ли скрипт двери лифта
        {
            if (showDebugLogs) Debug.Log("ElevatorEndingEvent: вызываю закрытие двери лифта"); // Пишем лог перед закрытием двери лифта

            yield return StartCoroutine(slidingElevatorDoor.CloseDoorAndWait()); // Закрываем дверь лифта и ждём окончания
        }
        else // Если скрипт двери лифта не назначен
        {
            Debug.LogError("ElevatorEndingEvent: Sliding Elevator Door не назначен в Inspector"); // Пишем ошибку в Console
        }

        if (showDebugLogs) Debug.Log("ElevatorEndingEvent: игрок вошёл, остановился, развернулся на месте и не смещался к двери"); // Пишем финальный лог

        yield return new WaitForSeconds(3);

        SceneManager.LoadSceneAsync("Main Menu");
    }

    private IEnumerator MovePlayerToPoint(Transform targetPoint) // Корутина движения игрока к точке входа в лифт
    {
        if (playerTransform == null) yield break; // Если Transform игрока не назначен, выходим

        if (targetPoint == null) yield break; // Если целевая точка не назначена, выходим

        lockPlayerPosition = false; // На время входа разрешаем менять позицию игрока

        if (playerCharacterController != null) playerCharacterController.enabled = false; // Выключаем физическую капсулу на время катсцены

        Vector3 targetPosition = new Vector3(
            targetPoint.position.x,
            playerTransform.position.y,
            targetPoint.position.z
        ); // Берём X/Z точки, но сохраняем текущую высоту игрока

        Vector2 playerHorizontalPosition = new Vector2(
            playerTransform.position.x,
            playerTransform.position.z
        ); // Получаем горизонтальную позицию игрока

        Vector2 targetHorizontalPosition = new Vector2(
            targetPosition.x,
            targetPosition.z
        ); // Получаем горизонтальную позицию точки

        while (Vector2.Distance(playerHorizontalPosition, targetHorizontalPosition) > 0.03f) // Пока игрок не дошёл по плоскости пола
        {
            playerTransform.position = Vector3.MoveTowards(
                playerTransform.position,
                targetPosition,
                playerMoveSpeed * Time.deltaTime
            ); // Двигаем игрока только к рассчитанной позиции с сохранённой высотой

            playerHorizontalPosition = new Vector2(
                playerTransform.position.x,
                playerTransform.position.z
            ); // Обновляем горизонтальную позицию после движения

            yield return null; // Ждём следующий кадр
        }

        playerTransform.position = targetPosition; // Точно ставим игрока по X/Z без изменения Y

        lockedPlayerPosition = playerTransform.position; // Запоминаем окончательную позицию внутри лифта

        lockPlayerPosition = true; // Запрещаем любым дверям и коллайдерам смещать игрока

        if (showDebugLogs)
        {
            Debug.Log(
                "ElevatorEndingEvent: игрок дошёл до точки. Позиция зафиксирована: " + lockedPlayerPosition,
                gameObject
            ); // Подтверждаем фиксацию позиции
        }
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