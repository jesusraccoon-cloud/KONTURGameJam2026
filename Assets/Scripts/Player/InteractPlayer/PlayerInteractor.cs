using UnityEngine; // Подключаем Unity-классы

public class PlayerInteractor : MonoBehaviour // Центральный скрипт взаимодействия игрока
{
    [Header("References")] // Блок ссылок
    public Camera playerCamera; // Камера игрока, из которой выпускаются лучи

    public PlayerHideController playerHideController; // Контроллер пряток игрока

    public ObjectGrabber objectGrabber; // Скрипт подбора и отпускания предметов

    [Header("Interaction Settings")] // Блок обычного взаимодействия
    public float interactDistance = 3f; // Дистанция взаимодействия через E и Q

    [Header("Hit Settings")] // Блок удара
    public float hitDistance = 2f; // Дистанция удара через ЛКМ

    [Header("Raycast Settings")] // Блок лучей
    public LayerMask interactLayers = ~0; // Слои, по которым работает луч

    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide; // Разрешаем лучу попадать в Trigger-коллайдеры

    [Header("Keys")] // Блок кнопок
    public KeyCode interactKey = KeyCode.E; // Кнопка обычного взаимодействия

    public KeyCode hideKey = KeyCode.Q; // Кнопка пряток

    public KeyCode hitKey = KeyCode.Mouse0; // Кнопка удара

    [Header("Debug")] // Блок отладки
    public bool drawDebugRays = true; // Показывать debug-лучи

    public bool showDebugLogs = false; // Показывать debug-логи

    private IInteractable currentInteractable; // Текущий объект для E

    private IHitInteractable currentHitInteractable; // Текущий объект для удара

    private WardrobeHideInteractForwarder currentWardrobeHideForwarder; // Текущая зона пряток для Q

    private ILookInteractable currentLookInteractable; // Текущий объект наведения

    private ILookInteractable previousLookInteractable; // Предыдущий объект наведения

    private void Start() // Запускается один раз при старте сцены
    {
        if (playerCamera == null) playerCamera = Camera.main; // Если камера не назначена, берем главную камеру

        if (playerHideController == null) playerHideController = GetComponent<PlayerHideController>(); // Если контроллер пряток не назначен, ищем на игроке

        if (objectGrabber == null && playerCamera != null) objectGrabber = playerCamera.GetComponent<ObjectGrabber>(); // Если подбор не назначен, ищем на камере
    }

    private void Update() // Выполняется каждый кадр
    {
        FindCurrentInteractable(); // Ищем объект для E, Q и наведения

        FindCurrentHitInteractable(); // Ищем объект для удара

        HandleLook(); // Обрабатываем наведение

        HandleInteractInput(); // Обрабатываем E

        HandleHideInput(); // Обрабатываем Q

        HandleHitInput(); // Обрабатываем ЛКМ

        DrawDebugRays(); // Рисуем debug-лучи
    }

    private void HandleInteractInput() // Обработка E
    {
        if (!Input.GetKeyDown(interactKey)) return; // Если E не нажали, выходим

        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан, E снаружи не работает

        if (currentInteractable != null) // Если перед игроком есть интерактивный объект
        {
            currentInteractable.Interact(); // Вызываем обычное взаимодействие

            return; // Выходим, чтобы не подобрать предмет в этот же кадр
        }

        if (objectGrabber != null) objectGrabber.Interact(); // Если интерактива нет, пробуем взять или отпустить предмет
    }

    private void HandleHideInput() // Обработка Q
    {
        if (!Input.GetKeyDown(hideKey)) return; // Если Q не нажали, выходим

        if (playerHideController != null && playerHideController.isHidden) // Если игрок уже спрятан
        {
            playerHideController.TryExitHide(); // Выходим из шкафа

            return; // Выходим
        }

        if (currentWardrobeHideForwarder != null) // Если луч смотрит на зону пряток
        {
            currentWardrobeHideForwarder.TryHide(); // Запускаем прятки именно через зону

            return; // Выходим
        }
    }

    private void HandleHitInput() // Обработка ЛКМ
    {
        if (!Input.GetKeyDown(hitKey)) return; // Если ЛКМ не нажали, выходим

        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан, удар не работает

        if (currentHitInteractable != null) currentHitInteractable.Hit(); // Если есть объект удара, вызываем Hit
    }

    private void FindCurrentInteractable() // Поиск объекта перед игроком
    {
        currentInteractable = null; // Сбрасываем объект для E

        currentWardrobeHideForwarder = null; // Сбрасываем зону пряток для Q

        currentLookInteractable = null; // Сбрасываем объект наведения

        if (playerCamera == null) return; // Если камеры нет, выходим

        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан, внешний луч не нужен

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward); // Создаем луч из камеры вперед

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayers, triggerInteraction)) // Пускаем луч взаимодействия
        {
            currentInteractable = FindInterfaceInColliderOrParents<IInteractable>(hit.collider); // Ищем объект для E

            currentWardrobeHideForwarder = hit.collider.GetComponent<WardrobeHideInteractForwarder>(); // Ищем зону пряток прямо на коллайдере

            if (currentWardrobeHideForwarder == null) currentWardrobeHideForwarder = hit.collider.GetComponentInParent<WardrobeHideInteractForwarder>(); // Если не нашли, ищем выше

            currentLookInteractable = FindInterfaceInColliderOrParents<ILookInteractable>(hit.collider); // Ищем объект наведения

            if (showDebugLogs) Debug.Log("INTERACT RAY HIT: " + hit.collider.name); // Пишем, во что попал луч

            if (showDebugLogs && currentWardrobeHideForwarder != null) Debug.Log("HIDE ZONE FOUND: " + currentWardrobeHideForwarder.name); // Пишем, что нашли зону пряток
        }
    }

    private void FindCurrentHitInteractable() // Поиск объекта для удара
    {
        currentHitInteractable = null; // Сбрасываем объект удара

        if (playerCamera == null) return; // Если камеры нет, выходим

        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан, удар не нужен

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward); // Создаем луч из камеры вперед

        if (Physics.Raycast(ray, out RaycastHit hit, hitDistance, interactLayers, triggerInteraction)) // Пускаем луч удара
        {
            currentHitInteractable = FindInterfaceInColliderOrParents<IHitInteractable>(hit.collider); // Ищем объект удара

            if (showDebugLogs) Debug.Log("HIT RAY HIT: " + hit.collider.name); // Пишем, во что попал луч удара
        }
    }

    private T FindInterfaceInColliderOrParents<T>(Collider targetCollider) where T : class // Универсальный поиск интерфейса
    {
        if (targetCollider == null) return null; // Если коллайдера нет, возвращаем null

        T interfaceOnCollider = targetCollider.GetComponent<T>(); // Ищем интерфейс на самом объекте коллайдера

        if (interfaceOnCollider != null) return interfaceOnCollider; // Если нашли, возвращаем его

        T interfaceInParents = targetCollider.GetComponentInParent<T>(); // Если не нашли, ищем в родителях

        return interfaceInParents; // Возвращаем найденное или null
    }

    private void HandleLook() // Обработка наведения
    {
        if (previousLookInteractable != currentLookInteractable) // Если объект наведения изменился
        {
            if (previousLookInteractable != null) previousLookInteractable.LookExit(); // Сообщаем старому объекту, что игрок больше не смотрит

            previousLookInteractable = currentLookInteractable; // Запоминаем новый объект
        }

        if (currentLookInteractable != null) currentLookInteractable.LookUpdate(); // Обновляем наведение текущего объекта
    }

    private void DrawDebugRays() // Рисует debug-лучи
    {
        if (!drawDebugRays) return; // Если debug выключен, выходим

        if (playerCamera == null) return; // Если камеры нет, выходим

        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан, лучи не рисуем

        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactDistance, Color.green); // Зеленый луч взаимодействия

        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * hitDistance, Color.red); // Красный луч удара
    }
}