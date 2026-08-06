using UnityEngine; // Подключаем Unity-классы
using UnityEngine.Events; // Подключаем UnityEvent
using FMODUnity; // Подключаем FMOD

public class CassettePickup : MonoBehaviour, IInteractable // Универсальная кассета: обычный подбор или выезд из радио
{
    public string hintGui = "Подобрать кассету";
    public string Hint => hintGui;
    public enum CassetteInteractionMode // Режим работы конкретной кассеты
    {
        SimplePickup, // Обычная кассета: одно нажатие E сразу добавляет её в инвентарь

        EjectThenPickup, // Первое E выдвигает кассету, второе E забирает её

        ExternalEjectThenPickup // Радио само вызывает EjectFromRadio, после выезда кассету можно забрать через E
    }

    [Header("Interaction Mode")] // Главный режим работы кассеты
    [SerializeField]
    private CassetteInteractionMode interactionMode =
        CassetteInteractionMode.SimplePickup; // По умолчанию обычная кассета забирается одним нажатием E

    [SerializeField]
    private bool autoCollectAfterEject = false; // Автоматически забирать кассету после завершения выезда

    [Header("Eject Movement")] // Настройки выезда кассеты
    [SerializeField]
    private Transform ejectPoint; // Точка, куда кассета должна выехать

    [SerializeField]
    [Min(0.01f)]
    private float moveSpeed = 5f; // Скорость движения кассеты к EjectPoint

    [Header("Inventory")] // Настройки инвентаря
    [SerializeField]
    private CassetteInventoryUI inventoryUI; // UI собранных кассет

    [Header("Noise")] // Настройки шума
    [SerializeField]
    private NoiseEmitter noiseEmitter; // Источник шума кассеты

    [SerializeField]
    [Range(1, 10)]
    private int ejectNoisePower = 5; // Сила шума при выезде кассеты

    [Header("FMOD")] // Настройки FMOD
    [SerializeField]
    private EventReference pickupEvent; // Звук выезда или подбора кассеты

    [Header("Save Hook")] // Настройки сохранения
    public UnityEvent onCollected; // Дополнительное событие после сбора кассеты


    [Header("Optional Auto Find")] // Автоматический поиск ссылок
    [SerializeField]
    private bool autoFindInventoryUI = true; // Автоматически искать CassetteInventoryUI

    [SerializeField]
    private bool autoFindEjectPoint = true; // Автоматически искать EjectPoint для кассеты из радио

    [SerializeField]
    private string ejectPointName = "EjectPoint"; // Имя объекта точки выезда

    [Header("Debug State")] // Состояние кассеты в Play Mode
    [SerializeField]
    private bool isEjected = false; // Выехала ли кассета полностью

    [SerializeField]
    private bool isEjecting = false; // Двигается ли кассета сейчас

    [SerializeField]
    private bool isCollected = false; // Забрана ли кассета игроком

    [Header("Debug")] // Настройки отладки
    [SerializeField]
    private bool showDebugLogs = true; // Показывать сообщения в Unity Console

    private Vector3 targetPosition; // Конечная мировая позиция кассеты

    public bool IsEjected => isEjected; // Публичная проверка завершённого выезда

    public bool IsEjecting => isEjecting; // Публичная проверка движения

    public bool IsCollected => isCollected; // Публичная проверка сбора

    private void Awake() // Вызывается при создании объекта
    {
        TryFindReferences(); // Ищем необходимые ссылки
    }

    private void Start() // Вызывается перед первым кадром
    {
        TryFindReferences(); // Повторно проверяем ссылки

        ValidateSetup(); // Проверяем настройку Inspector
    }

    private void Update() // Выполняется каждый кадр
    {
        if (isEjecting) // Если кассета сейчас выезжает
        {
            MoveToEjectPoint(); // Двигаем её к EjectPoint
        }
    }

    public void Interact() // Вызывается PlayerInteractor при нажатии E
    {
        if (isCollected) // Если кассета уже собрана
        {
            return; // Повторное взаимодействие не требуется
        }

        if (isEjecting) // Если кассета сейчас движется
        {
            return; // Во время движения повторное E игнорируем
        }

        if (interactionMode == CassetteInteractionMode.SimplePickup) // Если это обычная кассета
        {
            CompletePickup(); // Одно E сразу добавляет её в инвентарь

            return; // Завершаем взаимодействие
        }

        if (interactionMode == CassetteInteractionMode.EjectThenPickup) // Если первое E должно выдвигать кассету
        {
            if (!isEjected) // Если кассета ещё внутри
            {
                EjectFromRadio(false); // Запускаем плавный выезд

                return; // В этот же кадр кассету не забираем
            }

            CompletePickup(); // После завершённого выезда следующее E забирает кассету

            return; // Завершаем взаимодействие
        }

        if (interactionMode == CassetteInteractionMode.ExternalEjectThenPickup) // Если выездом управляет RadioCassettePuzzle
        {
            if (!isEjected) // Если радио ещё не выдвинуло кассету
            {
                if (showDebugLogs) // Если включены логи
                {
                    Debug.Log(
                        gameObject.name
                        + ": кассета ожидает вызов EjectFromRadio от радио.",
                        gameObject
                    ); // Объясняем текущее состояние
                }

                return; // До выезда подбор запрещён
            }

            CompletePickup(); // После выезда обычное E забирает кассету
        }
    }

    public void EjectFromRadio(bool instant) // Публичный метод, который может вызвать RadioCassettePuzzle
    {
        if (isCollected) // Если кассета уже собрана
        {
            return; // Не возвращаем её
        }

        if (isEjected) // Если кассета уже выехала
        {
            if (autoCollectAfterEject) // Если включён автоматический сбор
            {
                CompletePickup(); // Сразу зачисляем её игроку
            }

            return; // Повторный выезд не запускаем
        }

        if (isEjecting) // Если движение уже идёт
        {
            return; // Второе движение не запускаем
        }

        TryFindReferences(); // Проверяем ссылки перед началом движения

        if (ejectPoint == null) // Если точка выезда не назначена
        {
            Debug.LogWarning(
                gameObject.name + ": EjectPoint не назначен, кассета не может выехать.",
                gameObject
            ); // Пишем точную причину

            return; // Прерываем выезд
        }

        gameObject.SetActive(true); // Гарантированно включаем объект кассеты

        targetPosition = ejectPoint.position; // Запоминаем мировую позицию EjectPoint

        if (instant) // Если требуется мгновенное восстановление
        {
            transform.position = targetPosition; // Сразу ставим кассету в конечную точку

            isEjecting = false; // Движение не требуется

            isEjected = true; // Кассета считается выдвинутой

            if (autoCollectAfterEject) // Если включён автоматический сбор
            {
                CompletePickup(); // Сразу добавляем кассету в инвентарь
            }

            return; // Завершаем метод
        }

        isEjecting = true; // Включаем плавное движение

        PlayPickupSound(); // Проигрываем звук выезда

        EmitEjectNoise(); // Создаём шум выезда

        if (showDebugLogs) // Если включены логи
        {
            Debug.Log(
                gameObject.name + ": кассета начала выезжать.",
                gameObject
            ); // Пишем состояние
        }
    }

    public void CollectImmediately() // Публичный метод для мгновенного сбора из другого скрипта или UnityEvent
    {
        if (isCollected) // Если кассета уже собрана
        {
            return; // Повторно не собираем
        }

        CompletePickup(); // Добавляем кассету в инвентарь
    }

    private void TryFindReferences() // Автоматически находит нужные ссылки
    {
        if (autoFindInventoryUI && inventoryUI == null) // Если UI ещё не назначен
        {
            inventoryUI = FindFirstObjectByType<CassetteInventoryUI>(); // Ищем UI в сцене
        }

        bool modeUsesEjectPoint =
            interactionMode != CassetteInteractionMode.SimplePickup; // Определяем, нужен ли этой кассете EjectPoint

        if (modeUsesEjectPoint && autoFindEjectPoint && ejectPoint == null) // Если кассета использует выезд
        {
            Transform parentTransform = transform.parent; // Получаем родителя кассеты

            if (parentTransform != null) // Если родитель существует
            {
                ejectPoint = parentTransform.Find(ejectPointName); // Ищем EjectPoint среди соседних объектов
            }
        }

        if (noiseEmitter == null) // Если NoiseEmitter не назначен
        {
            noiseEmitter = GetComponent<NoiseEmitter>(); // Ищем его на кассете
        }
    }

    private void ValidateSetup() // Проверяет обязательные ссылки
    {
        if (inventoryUI == null) // Если UI не найден
        {
            Debug.LogWarning(
                gameObject.name + ": CassetteInventoryUI не найден.",
                gameObject
            ); // Предупреждаем о настройке
        }

        bool modeUsesEjectPoint =
            interactionMode != CassetteInteractionMode.SimplePickup; // Проверяем, должна ли кассета выезжать

        if (modeUsesEjectPoint && ejectPoint == null) // Только кассете из радио нужен EjectPoint
        {
            Debug.LogWarning(
                gameObject.name + ": для выбранного режима не найден EjectPoint.",
                gameObject
            ); // Предупреждаем о настройке
        }
    }

    private void MoveToEjectPoint() // Плавно двигает кассету наружу
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        ); // Двигаем кассету с постоянной скоростью

        float distanceToTarget = Vector3.Distance(
            transform.position,
            targetPosition
        ); // Считаем оставшееся расстояние

        if (distanceToTarget > 0.002f) // Если кассета ещё не достигла точки
        {
            return; // Продолжаем движение в следующем кадре
        }

        transform.position = targetPosition; // Точно ставим её в EjectPoint

        isEjecting = false; // Завершаем движение

        isEjected = true; // Помечаем кассету выдвинутой

        if (showDebugLogs) // Если включены логи
        {
            Debug.Log(
                gameObject.name + ": кассета полностью выехала.",
                gameObject
            ); // Пишем состояние
        }

        if (autoCollectAfterEject) // Если кассету нужно забрать автоматически
        {
            CompletePickup(); // Сразу добавляем её в инвентарь
        }
    }

    private void CompletePickup() // Добавляет кассету в инвентарь
    {
        if (isCollected) // Если кассета уже собрана
        {
            return; // Защищаемся от повторного сбора
        }

        isCollected = true; // Помечаем кассету собранной

        isEjecting = false; // На всякий случай останавливаем движение

        if (onCollected != null) // Если UnityEvent существует
        {
            onCollected.Invoke(); // Вызываем дополнительные события
        }

        if (inventoryUI != null) // Если UI назначен
        {
            inventoryUI.AddCassette(); // Добавляем кассету в счётчик
        }
        else // Если UI не назначен
        {
            Debug.LogWarning(
                gameObject.name + ": кассета собрана, но CassetteInventoryUI не назначен.",
                gameObject
            ); // Пишем предупреждение
        }

        PlayPickupSound(); // Проигрываем звук обычного подбора

        if (showDebugLogs) // Если включены логи
        {
            Debug.Log(
                gameObject.name + ": кассета добавлена в инвентарь.",
                gameObject
            ); // Пишем состояние
        }

        gameObject.SetActive(false); // Прячем собранную кассету
    }

    private void PlayPickupSound() // Проигрывает назначенный звук
    {
        if (pickupEvent.IsNull) // Если FMOD-событие не назначено
        {
            return; // Звук не запускаем
        }

        RuntimeManager.PlayOneShot(
            pickupEvent,
            transform.position
        ); // Запускаем звук в позиции кассеты
    }

    private void EmitEjectNoise() // Создаёт шум выезда
    {
        if (noiseEmitter == null) // Если NoiseEmitter не назначен
        {
            return; // Шум создать невозможно
        }

        noiseEmitter.EmitNoise(ejectNoisePower); // Отправляем шум монстру
    }

#if UNITY_EDITOR // Только для редактора Unity
    private void OnDrawGizmosSelected() // Показывает путь выезда в Scene
    {
        if (ejectPoint == null) // Если точка не назначена
        {
            return; // Рисовать нечего
        }

        Gizmos.color = Color.cyan; // Выбираем цвет линии

        Gizmos.DrawLine(
            transform.position,
            ejectPoint.position
        ); // Рисуем путь движения

        Gizmos.DrawSphere(
            ejectPoint.position,
            0.03f
        ); // Рисуем конечную точку
    }
#endif // Конец редакторного блока
}