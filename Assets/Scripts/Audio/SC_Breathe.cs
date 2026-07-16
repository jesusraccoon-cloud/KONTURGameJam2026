using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager, RuntimeUtils)
using FMOD.Studio; // Подключаем EventInstance, STOP_MODE
using StarterAssets; // Подключаем FirstPersonController

[RequireComponent(typeof(CharacterController))] // Скорость читаем из CharacterController
public class SC_Breathe : MonoBehaviour // Зацикленное дыхание игрока с плавным переходом idle -> walk -> run
{
    [Header("References")] // Блок ссылок
    public FirstPersonController controller; // Контроллер игрока (из него берём MoveSpeed/SprintSpeed)

    private CharacterController characterController; // Для чтения текущей скорости

    [Header("FMOD")] // Блок FMOD
    public EventReference breatheEvent; // Зацикленное событие Breathe

    public string movementParameter = "Movement"; // Имя НЕПРЕРЫВНОГО параметра в FMOD (плавный idle/walk/run)

    public bool attachToPlayer = true; // Привязать звук к игроку (позиция следует за ним)

    [Header("Movement Mapping")] // Блок сопоставления скорости с параметром
    public bool readSpeedsFromController = true; // Брать скорости ходьбы/бега из FirstPersonController

    public float walkSpeed = 4.0f; // Запасная скорость ходьбы (если контроллер не назначен)

    public float runSpeed = 6.0f; // Запасная скорость бега (если контроллер не назначен)

    public float idleSpeedThreshold = 0.15f; // Ниже этой скорости считаем покой

    [Header("Parameter Values")] // Что писать в параметр FMOD в ключевых точках
    public float idleValue = 0f; // Покой

    public float walkValue = 1f; // Ходьба

    public float runValue = 2f; // Бег

    [Header("Smoothing")] // Блок плавности
    public float smoothTime = 0.25f; // Плавность перехода параметра (больше = медленнее/мягче)

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать текущее значение параметра

    private EventInstance breatheInstance; // Экземпляр зацикленного дыхания

    private bool instanceValid = false; // Создан ли инстанс

    private float currentValue = 0f; // Текущее (сглаженное) значение параметра

    private float smoothVelocity = 0f; // Служебная скорость SmoothDamp

    private void Awake() // Вызывается при создании объекта
    {
        characterController = GetComponent<CharacterController>(); // Берём CharacterController

        if (controller == null) // Если контроллер не назначен
        {
            controller = GetComponent<FirstPersonController>(); // Ищем на этом же объекте
        }
    }

    private void Start() // Вызывается перед первым кадром (банки уже загружены)
    {
        currentValue = idleValue; // Стартуем из покоя
        StartBreathing(); // Запускаем зацикленное дыхание
    }

    private void OnDestroy() // При уничтожении объекта
    {
        StopBreathing(); // Останавливаем и освобождаем инстанс
    }

    private void StartBreathing() // Запуск дыхания
    {
        if (breatheEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning("SC_Breathe: не назначено FMOD-событие Breathe"); // Предупреждение
            return; // Выходим
        }

        breatheInstance = RuntimeManager.CreateInstance(breatheEvent); // Создаём инстанс

        if (attachToPlayer) // Если привязываем к игроку
        {
            RuntimeManager.AttachInstanceToGameObject(breatheInstance, gameObject); // Позиция звука следует за игроком
        }
        else // Иначе один раз ставим позицию
        {
            breatheInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position)); // Позиция в пространстве
        }

        breatheInstance.setParameterByName(movementParameter, currentValue); // Ставим стартовое значение
        breatheInstance.start(); // Запускаем зацикленное дыхание

        instanceValid = true; // Инстанс создан
    }

    private void StopBreathing() // Остановка дыхания
    {
        if (!instanceValid) return; // Если инстанса нет — выходим

        breatheInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT); // Останавливаем с затуханием
        breatheInstance.release(); // Освобождаем инстанс

        instanceValid = false; // Помечаем, что инстанса нет
    }

    private void Update() // Вызывается каждый кадр
    {
        if (!instanceValid) return; // Без инстанса работать не с чем

        float target = MapSpeedToValue(GetHorizontalSpeed()); // Целевое значение по текущей скорости

        currentValue = Mathf.SmoothDamp(currentValue, target, ref smoothVelocity, smoothTime); // Плавно ползём к цели

        breatheInstance.setParameterByName(movementParameter, currentValue); // Пишем значение в FMOD

        if (showDebugLogs) Debug.Log("Breathe " + movementParameter + " = " + currentValue.ToString("0.00")); // Лог
    }

    private float MapSpeedToValue(float speed) // Скорость -> значение параметра (плавно idle/walk/run)
    {
        float wSpeed = (readSpeedsFromController && controller != null) ? controller.MoveSpeed : walkSpeed; // Скорость ходьбы
        float rSpeed = (readSpeedsFromController && controller != null) ? controller.SprintSpeed : runSpeed; // Скорость бега

        if (speed <= idleSpeedThreshold) // Если практически стоим
        {
            return idleValue; // Покой
        }

        if (speed <= wSpeed) // Между покоем и ходьбой
        {
            return Mathf.Lerp(idleValue, walkValue, Mathf.InverseLerp(idleSpeedThreshold, wSpeed, speed)); // idle -> walk
        }

        return Mathf.Lerp(walkValue, runValue, Mathf.InverseLerp(wSpeed, rSpeed, speed)); // walk -> run (за rSpeed зажмётся в runValue)
    }

    private float GetHorizontalSpeed() // Горизонтальная скорость из CharacterController
    {
        if (characterController == null) return 0f; // Если контроллера нет — 0

        Vector3 v = characterController.velocity; // Берём скорость
        v.y = 0f; // Убираем вертикаль
        return v.magnitude; // Длина
    }
}
