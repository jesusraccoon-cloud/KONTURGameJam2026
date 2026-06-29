using UnityEngine; // Подключаем Unity-классы: MonoBehaviour, Transform, Vector3, Debug

public class CassettePickup : MonoBehaviour, IInteractable // Кассета является интерактивным объектом
{
    [Header("Move Settings")] // Блок настроек движения
    [SerializeField] private Transform ejectPoint; // Точка, куда кассета выезжает

    [SerializeField] private float moveSpeed = 5f; // Скорость движения кассеты

    [Header("Inventory")] // Блок инвентаря
    [SerializeField] private CassetteInventoryUI inventoryUI; // Ссылка на UI кассет

    [Header("Noise")] // Блок шума
    [SerializeField] private NoiseEmitter noiseEmitter; // Источник шума кассеты

    [SerializeField] [Range(1, 10)] private int ejectNoisePower = 5; // Сила шума выезда кассеты

    [Header("Optional Auto Find")] // Блок автопоиска
    [SerializeField] private bool autoFindInventoryUI = true; // Автоматически искать UI кассет

    [SerializeField] private bool autoFindEjectPoint = true; // Автоматически искать EjectPoint

    [SerializeField] private string ejectPointName = "EjectPoint"; // Имя точки выезда

    private Vector3 targetPosition; // Целевая позиция кассеты

    private bool isPickingUp = false; // Двигается ли кассета сейчас

    private bool isCollected = false; // Собрана ли кассета

    private void Awake() // Вызывается при создании объекта
    {
        TryFindReferences(); // Пробуем найти ссылки
    }

    private void Start() // Вызывается перед первым кадром
    {
        ValidateSetup(); // Проверяем настройки
    }

    private void Update() // Вызывается каждый кадр
    {
        if (isPickingUp) // Если кассета выезжает
        {
            MoveToEjectPoint(); // Двигаем кассету
        }
    }

    public void Interact() // Вызывается PlayerInteractor при E
    {
        if (isCollected) return; // Если кассета уже собрана — выходим

        if (isPickingUp) return; // Если кассета уже движется — выходим

        StartPickup(); // Запускаем подбор
    }

    private void TryFindReferences() // Метод автопоиска ссылок
    {
        if (autoFindInventoryUI && inventoryUI == null) // Если нужно найти UI
        {
            inventoryUI = FindFirstObjectByType<CassetteInventoryUI>(); // Ищем CassetteInventoryUI
        }

        if (autoFindEjectPoint && ejectPoint == null) // Если нужно найти EjectPoint
        {
            Transform foundPoint = transform.parent != null // Проверяем наличие родителя
                ? transform.parent.Find(ejectPointName) // Ищем точку внутри родителя
                : null; // Если родителя нет — null

            if (foundPoint != null) // Если точка найдена
            {
                ejectPoint = foundPoint; // Запоминаем точку
            }
        }

        if (noiseEmitter == null) // Если NoiseEmitter не назначен
        {
            noiseEmitter = GetComponent<NoiseEmitter>(); // Пробуем найти NoiseEmitter на кассете
        }
    }

    private void ValidateSetup() // Метод проверки настроек
    {
        if (inventoryUI == null) // Если UI не найден
        {
            Debug.LogWarning($"{gameObject.name}: CassetteInventoryUI не найден."); // Предупреждение
        }

        if (ejectPoint == null) // Если EjectPoint не найден
        {
            Debug.LogWarning($"{gameObject.name}: EjectPoint не найден."); // Предупреждение
        }
    }

    private void StartPickup() // Начать выезд кассеты
    {
        if (ejectPoint == null) return; // Если точки выезда нет — выходим

        targetPosition = ejectPoint.position; // Запоминаем позицию выезда

        isPickingUp = true; // Включаем движение кассеты

        if (noiseEmitter != null) // Если источник шума назначен
        {
            noiseEmitter.EmitNoise(ejectNoisePower); // Создаём шум выезда кассеты
        }
    }

    private void MoveToEjectPoint() // Метод движения кассеты
    {
        transform.position = Vector3.Lerp( // Плавно двигаем кассету
            transform.position, // От текущей позиции
            targetPosition, // К целевой позиции
            Time.deltaTime * moveSpeed // С учётом скорости
        );

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition); // Считаем расстояние до цели

        if (distanceToTarget <= 0.02f) // Если почти доехали
        {
            CompletePickup(); // Завершаем подбор
        }
    }

    private void CompletePickup() // Завершить подбор
    {
        isPickingUp = false; // Останавливаем движение

        isCollected = true; // Помечаем кассету собранной

        if (inventoryUI != null) // Если UI назначен
        {
            inventoryUI.AddCassette(); // Добавляем кассету
        }

        gameObject.SetActive(false); // Выключаем объект кассеты
    }

#if UNITY_EDITOR // Только для редактора Unity

    private void OnDrawGizmosSelected() // Рисуем подсказку в Scene
    {
        if (ejectPoint != null) // Если точка выезда назначена
        {
            Gizmos.color = Color.cyan; // Цвет Gizmos

            Gizmos.DrawLine(transform.position, ejectPoint.position); // Линия до точки

            Gizmos.DrawSphere(ejectPoint.position, 0.03f); // Шарик на точке
        }
    }

#endif // Конец блока редактора
}