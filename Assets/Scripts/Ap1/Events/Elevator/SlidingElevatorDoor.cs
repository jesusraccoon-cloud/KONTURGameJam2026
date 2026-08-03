using System.Collections; // Подключаем IEnumerator для плавного движения двери.
using UnityEngine; // Подключаем основные Unity-классы.
using UnityEngine.Events; // UnityEvent для звуковых хуков (старт открытия/закрытия).

/// <summary>
/// Управляет одной сдвижной частью двери лифта.
/// Текущая позиция Door Part при запуске считается открытой.
/// Закрытая позиция рассчитывается через Closed Local Offset.
/// </summary>
public class SlidingElevatorDoor : MonoBehaviour
{
    [Header("DOOR PART")]

    [Tooltip("Объект двери, который должен физически двигаться.")]
    [SerializeField]
    private Transform doorPart; // Подвижная часть двери.

    [Header("POSITIONS")]

    [Tooltip("Локальное смещение от открытого положения до закрытого.")]
    [SerializeField]
    private Vector3 closedLocalOffset = new Vector3(1.2f, 0f, 0f); // Направление и расстояние закрытия.

    [Header("SPEED")]

    [Min(0.01f)]
    [Tooltip("Скорость закрытия двери.")]
    [SerializeField]
    private float closeSpeed = 2f; // Скорость движения при закрытии.

    [Min(0.01f)]
    [Tooltip("Скорость открытия двери.")]
    [SerializeField]
    private float openSpeed = 2f; // Скорость движения при открытии.

    [Header("EVENTS")]

    [Tooltip("Вызывается в момент, когда дверь НАЧИНАЕТ закрываться. Вешай сюда SC_InteractSound.Play для разового звука.")]
    public UnityEvent onCloseStart; // Хук: старт закрытия.

    [Tooltip("Вызывается в момент, когда дверь НАЧИНАЕТ открываться (необязательно).")]
    public UnityEvent onOpenStart; // Хук: старт открытия.

    [Tooltip("Вызывается, когда дверь ПОЛНОСТЬЮ ЗАКРЫЛАСЬ. Вешай сюда включение звука лифта.")]
    public UnityEvent onCloseEnd; // Хук: закрытие завершено.

    [Tooltip("Вызывается, когда дверь ПОЛНОСТЬЮ ОТКРЫЛАСЬ (необязательно).")]
    public UnityEvent onOpenEnd; // Хук: открытие завершено.

    [Header("DEBUG")]

    [Tooltip("Показывать сообщения в Unity Console.")]
    [SerializeField]
    private bool showDebugLogs = true; // Включаем диагностические сообщения.

    private Vector3 openedLocalPosition; // Локальная позиция открытой двери.

    private Vector3 closedLocalPosition; // Локальная позиция закрытой двери.

    private Coroutine movementCoroutine; // Текущая корутина движения.

    private bool isBusy; // Двигается ли дверь сейчас.

    private bool isInitialized; // Рассчитаны ли позиции двери.

    /// <summary>
    /// Показывает, двигается ли дверь сейчас.
    /// </summary>
    public bool IsBusy => isBusy; // Публичное состояние движения.

    /// <summary>
    /// Показывает, находится ли дверь около закрытой позиции.
    /// </summary>
    public bool IsClosed =>
        isInitialized &&
        doorPart != null &&
        Vector3.Distance(doorPart.localPosition, closedLocalPosition) <= 0.01f;

    private void Awake()
    {
        InitializeDoorPositions(); // Рассчитываем позиции при запуске объекта.
    }

    private void OnEnable()
    {
        InitializeDoorPositions(); // Гарантируем инициализацию после включения объекта.
    }

    private void OnDisable()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine); // Останавливаем движение при отключении объекта.

            movementCoroutine = null; // Очищаем ссылку на корутину.
        }

        isBusy = false; // Сбрасываем состояние движения.
    }

    private void OnValidate()
    {
        closeSpeed = Mathf.Max(0.01f, closeSpeed); // Не допускаем нулевую скорость закрытия.

        openSpeed = Mathf.Max(0.01f, openSpeed); // Не допускаем нулевую скорость открытия.
    }

    private void InitializeDoorPositions()
    {
        if (isInitialized)
        {
            return; // Не пересчитываем позиции повторно.
        }

        if (doorPart == null)
        {
            doorPart = transform; // Без назначения двигаем объект со скриптом.
        }

        openedLocalPosition = doorPart.localPosition; // Текущую позицию считаем открытой.

        closedLocalPosition =
            openedLocalPosition + closedLocalOffset; // Рассчитываем закрытую позицию.

        isInitialized = true; // Запоминаем завершение настройки.

        if (showDebugLogs)
        {
            Debug.Log(
                "[SlidingElevatorDoor] Открытая позиция: "
                + openedLocalPosition
                + " | Закрытая позиция: "
                + closedLocalPosition,
                gameObject
            ); // Показываем рассчитанные позиции.
        }
    }

    /// <summary>
    /// Запускает закрытие двери.
    /// </summary>
    [ContextMenu("TEST — Close Door")]
    public void CloseDoor()
    {
        InitializeDoorPositions(); // Гарантируем наличие закрытой позиции.

        if (showDebugLogs)
        {
            Debug.Log(
                "[SlidingElevatorDoor] Получена команда закрыть дверь.",
                gameObject
            ); // Подтверждаем получение команды.
        }

        StartDoorMovement(
            closedLocalPosition,
            closeSpeed
        ); // Запускаем движение к закрытой позиции.
    }

    /// <summary>
    /// Закрывает дверь и позволяет другому сценарию дождаться завершения.
    /// </summary>
    public IEnumerator CloseDoorAndWait()
    {
        InitializeDoorPositions(); // Гарантируем наличие закрытой позиции.

        StartDoorMovement(
            closedLocalPosition,
            closeSpeed
        ); // Запускаем закрытие.

        while (isBusy)
        {
            yield return null; // Ждём завершения движения.
        }
    }

    /// <summary>
    /// Запускает открытие двери.
    /// </summary>
    [ContextMenu("TEST — Open Door")]
    public void OpenDoor()
    {
        InitializeDoorPositions(); // Гарантируем наличие открытой позиции.

        if (showDebugLogs)
        {
            Debug.Log(
                "[SlidingElevatorDoor] Получена команда открыть дверь.",
                gameObject
            ); // Подтверждаем получение команды.
        }

        StartDoorMovement(
            openedLocalPosition,
            openSpeed
        ); // Запускаем движение к открытой позиции.
    }

    /// <summary>
    /// Открывает дверь и позволяет другому сценарию дождаться завершения.
    /// </summary>
    public IEnumerator OpenDoorAndWait()
    {
        InitializeDoorPositions(); // Гарантируем наличие открытой позиции.

        StartDoorMovement(
            openedLocalPosition,
            openSpeed
        ); // Запускаем открытие.

        while (isBusy)
        {
            yield return null; // Ждём завершения движения.
        }
    }

    private void StartDoorMovement(
        Vector3 targetLocalPosition,
        float movementSpeed
    )
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "[SlidingElevatorDoor] Скрипт или объект отключён. Движение невозможно.",
                gameObject
            ); // Показываем причину отказа.

            return; // На выключенном компоненте корутина не запускается.
        }

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine); // Отменяем предыдущую команду движения.

            movementCoroutine = null; // Очищаем старую ссылку.
        }

        if (targetLocalPosition == closedLocalPosition) onCloseStart.Invoke(); // Дверь начинает закрываться — разовый звук
        else if (targetLocalPosition == openedLocalPosition) onOpenStart.Invoke(); // Дверь начинает открываться

        movementCoroutine = StartCoroutine(
            MoveDoorRoutine(
                targetLocalPosition,
                movementSpeed
            )
        ); // Запускаем новую команду.
    }

    private IEnumerator MoveDoorRoutine(
        Vector3 targetLocalPosition,
        float movementSpeed
    )
    {
        isBusy = true; // Запоминаем начало движения.

        float safeSpeed =
            Mathf.Max(0.01f, movementSpeed); // Защищаемся от нулевой скорости.

        while (
            Vector3.Distance(
                doorPart.localPosition,
                targetLocalPosition
            ) > 0.01f
        )
        {
            doorPart.localPosition = Vector3.MoveTowards(
                doorPart.localPosition,
                targetLocalPosition,
                safeSpeed * Time.deltaTime
            ); // Плавно двигаем дверь к цели.

            yield return null; // Ждём следующий кадр.
        }

        doorPart.localPosition =
            targetLocalPosition; // Точно устанавливаем конечную позицию.

        isBusy = false; // Запоминаем завершение движения.

        movementCoroutine = null; // Очищаем ссылку на корутину.

        if (targetLocalPosition == closedLocalPosition) onCloseEnd.Invoke(); // Дверь полностью закрылась — включаем звук лифта
        else if (targetLocalPosition == openedLocalPosition) onOpenEnd.Invoke(); // Дверь полностью открылась

        if (showDebugLogs)
        {
            Debug.Log(
                "[SlidingElevatorDoor] Движение завершено. Позиция: "
                + doorPart.localPosition,
                gameObject
            ); // Показываем итоговую позицию.
        }
    }
}