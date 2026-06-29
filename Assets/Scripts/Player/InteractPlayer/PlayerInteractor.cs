using UnityEngine; // Подключаем Unity-классы

public class PlayerInteractor : MonoBehaviour // Центральный скрипт взаимодействия игрока
{
    [Header("References")] // Блок ссылок
    public Camera playerCamera; // Камера игрока, из которой выпускаются лучи взаимодействия и удара

    public PlayerHideController playerHideController; // Контроллер пряток игрока

    public ObjectGrabber objectGrabber; // Скрипт захвата и бросания предметов

    [Header("Interaction Settings")] // Блок настроек обычного взаимодействия
    public float interactDistance = 3f; // Дистанция взаимодействия через E и Q

    [Header("Hit Settings")] // Блок настроек удара
    public float hitDistance = 2f; // Дистанция удара через ЛКМ

    [Header("Raycast Settings")] // Блок настроек лучей
    public LayerMask interactLayers = ~0; // Слои, по которым работают лучи взаимодействия и удара

    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide; // Разрешает Raycast попадать в Trigger-коллайдеры

    [Header("Keys")] // Блок кнопок
    public KeyCode interactKey = KeyCode.E; // Кнопка обычного взаимодействия

    public KeyCode hideKey = KeyCode.Q; // Кнопка пряток

    public KeyCode hitKey = KeyCode.Mouse0; // Кнопка удара

    [Header("Debug")] // Блок отладки
    public bool drawDebugRays = true; // Показывать debug-лучи в Scene View

    public bool showDebugLogs = false; // Показывать debug-логи в Console

    private IInteractable currentInteractable; // Текущий объект обычного взаимодействия

    private IHitInteractable currentHitInteractable; // Текущий объект удара

    private WardrobeHideHandle currentWardrobeHideHandle; // Текущий шкаф, в который можно спрятаться

    private ILookInteractable currentLookInteractable; // Текущий объект наведения

    private ILookInteractable previousLookInteractable; // Предыдущий объект наведения

    private void Start() // Запускается один раз при старте сцены
    {
        if (playerCamera == null) // Если камера не назначена вручную
        {
            playerCamera = Camera.main; // Берём главную камеру сцены
        }

        if (playerHideController == null) // Если контроллер пряток не назначен
        {
            playerHideController = GetComponent<PlayerHideController>(); // Ищем PlayerHideController на игроке
        }

        if (objectGrabber == null && playerCamera != null) // Если ObjectGrabber не назначен, но камера есть
        {
            objectGrabber = playerCamera.GetComponent<ObjectGrabber>(); // Ищем ObjectGrabber на камере игрока
        }
    }

    private void Update() // Выполняется каждый кадр
    {
        FindCurrentInteractable(); // Ищем объект для E и Q

        FindCurrentHitInteractable(); // Ищем объект для ЛКМ

        HandleLook(); // Обрабатываем наведение

        HandleInteractInput(); // Обрабатываем E

        HandleHideInput(); // Обрабатываем Q

        HandleHitInput(); // Обрабатываем ЛКМ

        DrawDebugRays(); // Рисуем debug-лучи
    }

    private void HandleInteractInput() // Обработка обычного взаимодействия через E
    {
        if (!Input.GetKeyDown(interactKey)) return; // Если E не нажали — выходим

        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан — E не работает наружу

        if (currentInteractable != null) // Если перед игроком есть объект взаимодействия
        {
            currentInteractable.Interact(); // Вызываем обычное взаимодействие

            return; // Выходим, чтобы не взять предмет в этот же кадр
        }

        if (objectGrabber != null) // Если обычного объекта нет, но ObjectGrabber назначен
        {
            objectGrabber.Interact(); // Берём или отпускаем предмет
        }
    }

    private void HandleHideInput() // Обработка пряток через Q
    {
        if (!Input.GetKeyDown(hideKey)) return; // Если Q не нажали — выходим

        if (playerHideController != null && playerHideController.isHidden) // Если игрок уже спрятан
        {
            playerHideController.TryExitHide(); // Выходим из шкафа

            return; // Выходим
        }

        if (currentWardrobeHideHandle != null) // Если перед игроком есть шкаф
        {
            currentWardrobeHideHandle.TryHide(); // Прячемся в шкаф

            return; // Выходим
        }
    }

    private void HandleHitInput() // Обработка удара через ЛКМ
    {
        if (!Input.GetKeyDown(hitKey)) return; // Если ЛКМ не нажали — выходим

        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан — удар наружу не работает

        if (currentHitInteractable != null) // Если перед игроком есть объект, который можно ударить
        {
            currentHitInteractable.Hit(); // Вызываем удар
        }
    }

    private void FindCurrentInteractable() // Поиск объекта для обычного взаимодействия
    {
        currentInteractable = null; // Сбрасываем найденный IInteractable

        currentWardrobeHideHandle = null; // Сбрасываем найденный шкаф

        currentLookInteractable = null; // Сбрасываем объект наведения

        if (playerCamera == null) return; // Если камеры нет — выходим

        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан — наружный луч не нужен

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward); // Создаём луч из камеры вперёд

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayers, triggerInteraction)) // Пускаем луч взаимодействия
        {
            currentInteractable = FindInterfaceInColliderOrParents<IInteractable>(hit.collider); // Ищем обычное взаимодействие

            currentWardrobeHideHandle = hit.collider.GetComponentInParent<WardrobeHideHandle>(); // Ищем шкаф для пряток

            currentLookInteractable = FindInterfaceInColliderOrParents<ILookInteractable>(hit.collider); // Ищем объект наведения

            if (showDebugLogs) // Если debug включён
            {
                Debug.Log("INTERACT RAY HIT: " + hit.collider.name); // Пишем имя объекта
            }
        }
    }

    private void FindCurrentHitInteractable() // Поиск объекта для удара
    {
        currentHitInteractable = null; // Сбрасываем найденный IHitInteractable

        if (playerCamera == null) return; // Если камеры нет — выходим

        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан — наружный удар не нужен

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward); // Создаём луч из камеры вперёд

        if (Physics.Raycast(ray, out RaycastHit hit, hitDistance, interactLayers, triggerInteraction)) // Пускаем луч удара
        {
            currentHitInteractable = FindInterfaceInColliderOrParents<IHitInteractable>(hit.collider); // Ищем объект удара

            if (showDebugLogs) // Если debug включён
            {
                Debug.Log("HIT RAY HIT: " + hit.collider.name); // Пишем имя объекта
            }
        }
    }

    private T FindInterfaceInColliderOrParents<T>(Collider targetCollider) where T : class // Универсальный поиск интерфейса
    {
        if (targetCollider == null) return null; // Если коллайдера нет — возвращаем null

        T interfaceOnCollider = targetCollider.GetComponent<T>(); // Ищем интерфейс на самом объекте коллайдера

        if (interfaceOnCollider != null) // Если интерфейс найден
        {
            return interfaceOnCollider; // Возвращаем найденный интерфейс
        }

        T interfaceInParents = targetCollider.GetComponentInParent<T>(); // Ищем интерфейс в родителях

        return interfaceInParents; // Возвращаем найденное или null
    }

    private void HandleLook() // Обработка наведения
    {
        if (previousLookInteractable != currentLookInteractable) // Если объект наведения изменился
        {
            if (previousLookInteractable != null) // Если раньше был объект
            {
                previousLookInteractable.LookExit(); // Сообщаем старому объекту, что игрок перестал смотреть
            }

            previousLookInteractable = currentLookInteractable; // Запоминаем новый объект
        }

        if (currentLookInteractable != null) // Если сейчас есть объект наведения
        {
            currentLookInteractable.LookUpdate(); // Обновляем наведение
        }
    }

    private void DrawDebugRays() // Рисует debug-лучи
    {
        if (!drawDebugRays) return; // Если debug выключен — выходим

        if (playerCamera == null) return; // Если камеры нет — выходим

        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан — лучи не рисуем

        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactDistance, Color.green); // Зелёный луч взаимодействия

        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * hitDistance, Color.red); // Красный луч удара
    }
}