using UnityEngine; // Подключаем Unity-классы
using StarterAssets; // Подключаем StarterAssets, чтобы читать FirstPersonController

[RequireComponent(typeof(CharacterController))] // Требуем CharacterController на игроке
public class PlayerNoiseController : MonoBehaviour // Скрипт шума игрока: ходьба, бег, прыжок
{
    [Header("References")] // Блок ссылок
    public NoiseManager noiseManager; // NoiseManager текущей квартиры

    public RoomTracker roomTracker; // RoomTracker игрока

    public NoiseMeterUI noiseMeterUI; // UI шума

    public FirstPersonController firstPersonController; // Контроллер игрока

    [Header("Movement Noise Power")] // Блок силы шума движения
    [Range(0, 10)] public int idleNoise = 0; // Шум стояния

    [Range(1, 10)] public int walkNoise = 2; // Шум ходьбы

    [Range(1, 10)] public int sprintNoise = 4; // Шум бега

    [Range(1, 10)] public int jumpNoise = 5; // Шум прыжка

    [Header("Step Intervals")] // Блок интервалов шагов
    public float walkStepInterval = 0.55f; // Частота шума ходьбы

    public float sprintStepInterval = 0.35f; // Частота шума бега

    [Header("Movement Detection")] // Блок определения движения
    public float minMoveSpeed = 0.15f; // Минимальная скорость для движения

    public float sprintSpeedThreshold = 5.0f; // Скорость, после которой считаем бег

    [Header("Jump Detection")] // Блок определения прыжка
    public bool enableJumpNoise = true; // Включён ли шум прыжка

    private bool wasGrounded = true; // Был ли игрок на земле в прошлом кадре

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать ли логи

    private CharacterController characterController; // Ссылка на CharacterController

    private float stepTimer = 0f; // Таймер шага

    private void Awake() // Вызывается при создании объекта
    {
        characterController = GetComponent<CharacterController>(); // Получаем CharacterController

        if (roomTracker == null) // Если RoomTracker не назначен
        {
            roomTracker = GetComponent<RoomTracker>(); // Ищем RoomTracker на игроке
        }

        if (firstPersonController == null) // Если FirstPersonController не назначен
        {
            firstPersonController = GetComponent<FirstPersonController>(); // Ищем FirstPersonController на игроке
        }
    }

    private void Start() // Вызывается перед первым кадром
    {
        if (firstPersonController != null) // Если контроллер игрока найден
        {
            wasGrounded = firstPersonController.Grounded; // Запоминаем стартовое состояние земли
        }
    }

    private void Update() // Вызывается каждый кадр
    {
        HandleJumpNoise(); // Проверяем шум прыжка

        HandleMovementNoise(); // Проверяем шум ходьбы/бега
    }

    private void HandleJumpNoise() // Метод шума прыжка
{
    if (!enableJumpNoise) return; // Если шум прыжка выключен — выходим

    if (firstPersonController == null) return; // Если контроллера нет — выходим

    bool isGroundedNow = firstPersonController.Grounded; // Берём текущее состояние земли

    if (wasGrounded && !isGroundedNow) // Если в прошлом кадре стояли, а сейчас оторвались от земли
    {
        EmitPlayerNoise(jumpNoise); // Отправляем реальный шум прыжка монстру

        if (noiseMeterUI != null) // Если UI шума назначен
        {
            noiseMeterUI.AddNoise(jumpNoise); // Добавляем прыжок поверх текущего шума движения
        }

        if (showDebugLogs) // Если отладка включена
        {
            Debug.Log("Прыжок игрока. Шум: " + jumpNoise); // Пишем лог прыжка
        }
    }

    wasGrounded = isGroundedNow; // Обновляем состояние земли
}

    private void HandleMovementNoise() // Метод шума движения
    {
        if (firstPersonController != null && !firstPersonController.canMove) // Если движение заблокировано
        {
            SetVisualNoise(idleNoise); // Показываем 0

            return; // Выходим
        }

        Vector3 horizontalVelocity = new Vector3( // Создаём горизонтальную скорость
            characterController.velocity.x, // Скорость по X
            0f, // Y не учитываем
            characterController.velocity.z // Скорость по Z
        );

        float speed = horizontalVelocity.magnitude; // Считаем скорость

        bool isMoving = speed > minMoveSpeed; // Проверяем, движется ли игрок

        if (!isMoving) // Если игрок стоит
        {
            stepTimer = 0f; // Сбрасываем таймер шага

            SetVisualNoise(idleNoise); // Показываем 0

            return; // Выходим
        }

        bool isSprinting = speed >= sprintSpeedThreshold; // Проверяем бег

        int currentNoise = isSprinting ? sprintNoise : walkNoise; // Выбираем шум движения

        float currentInterval = isSprinting ? sprintStepInterval : walkStepInterval; // Выбираем интервал шага

        SetVisualNoise(currentNoise); // Показываем шум движения в UI

        stepTimer -= Time.deltaTime; // Уменьшаем таймер шага

        if (stepTimer > 0f) return; // Если шаг ещё не наступил — выходим

        stepTimer = currentInterval; // Сбрасываем таймер шага

        EmitPlayerNoise(currentNoise); // Создаём шум шага

        if (showDebugLogs) // Если отладка включена
        {
            Debug.Log("Шаг игрока. Шум: " + currentNoise); // Пишем лог шага
        }
    }

    private void EmitPlayerNoise(int noisePower) // Универсальный метод шума игрока
    {
        if (noiseManager == null) // Если NoiseManager не назначен
        {
            if (showDebugLogs) Debug.LogWarning("PlayerNoiseController: не назначен NoiseManager"); // Пишем предупреждение

            return; // Выходим
        }

        RoomZone sourceRoom = null; // Создаём переменную комнаты

        if (roomTracker != null) // Если RoomTracker назначен
        {
            sourceRoom = roomTracker.currentRoom; // Берём текущую комнату игрока
        }

        noiseManager.MakeNoise(transform.position, noisePower, sourceRoom); // Отправляем шум в NoiseManager
    }

    private void SetVisualNoise(int value) // Метод показа шума движения игрока в UI
    {
        if (noiseMeterUI == null) return; // Если UI не назначен — выходим

        if (value <= 0) return; // Ноль не ставим резко, шум сам плавно затухает

        if (value < noiseMeterUI.currentNoise) return; // Не даём ходьбе/бегу сбить более сильный шум прыжка, удара, двери и т.д.

    noiseMeterUI.SetNoise(value); // Если новый шум сильнее или равен — показываем его
    }

    public void ShowExternalNoise(int value) // Метод показа внешних шумов: дверь, кассета, окно, удар
    {
        if (noiseMeterUI == null) return; // Если UI не назначен — выходим

    noiseMeterUI.AddNoise(value); // Добавляем внешний шум с ограниченным смешиванием
    }
}