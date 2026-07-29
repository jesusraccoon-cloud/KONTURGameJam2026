using System.Collections; // Подключаем Coroutine для плавного движения игрока
using System.Collections.Generic; // Подключаем HashSet для надёжного учёта коллайдеров игрока внутри Trigger
using System.Reflection; // Подключаем Reflection для синхронизации угла Starter Assets FirstPersonController
using UnityEngine; // Подключаем основные классы Unity

public class UniversalScenarioTrigger : MonoBehaviour, IInteractable, IHitInteractable // Универсальный сценарный триггер с активацией через вход, E или удар
{
    private enum ActivationSource // Перечисляем возможные источники активации триггера
    {
        Enter, // Игрок вошёл внутрь Trigger
        Interact, // Игрок нажал E
        Hit, // Игрок нажал ЛКМ и нанёс удар
        External // Триггер был вызван публичным методом из другого скрипта или UnityEvent
    }

    [Header("Activation Type")] // Раздел выбора способа активации
    public bool activateOnEnter = false; // Разрешить активацию, когда игрок входит внутрь Trigger
    public bool activateOnInteract = true; // Разрешить активацию нажатием E
    public bool activateOnHit = false; // Разрешить активацию ударом ЛКМ

    [Header("Trigger Settings")] // Основные настройки триггера
    public bool canActivate = true; // Может ли триггер сейчас сработать
    public bool repeatActivation = false; // Если включено, реплика и движение могут запускаться при каждом новом входе
    public bool disableAfterActivation = true; // Отключать ли Collider триггера после успешной активации

    [Header("Enter Settings")] // Раздел настройки определения игрока при входе
    public string playerTag = "Player"; // Tag игрока или одного из его родительских объектов

    [Header("Dialogue Lists")] // Раздел отдельных списков диалогов для E, ЛКМ и входа
    public DialogueTextUI[] interactDialogueList; // Список DialogueTextUI для случайного выбора при нажатии E
    public DialogueTextUI[] hitDialogueList; // Список DialogueTextUI для случайного выбора при ударе ЛКМ
    public DialogueTextUI[] enterDialogueList; // Список DialogueTextUI для случайного выбора при входе игрока

    [Header("PLAYER WALK TO POINT - V4")] // Раздел автоматического движения игрока к точке после входа
    public bool movePlayerToPointOnEnter = false; // Главная галочка: вести ли игрока к Point после входа в Trigger
    public Transform playerRoot; // Сюда назначается корневой объект PlayerCapsule
    public Transform playerMovePoint; // Сюда назначается Point, к которому должен подойти игрок
    public MonoBehaviour playerController; // Сюда назначается компонент Starter Assets FirstPersonController с PlayerCapsule
    public CharacterController playerCharacterController; // Сюда назначается CharacterController с PlayerCapsule
    [Min(0f)] public float playerMoveSpeed = 1.5f; // Скорость автоматической ходьбы игрока в метрах за секунду
    [Min(0f)] public float playerTurnSpeed = 360f; // Скорость разворота игрока к Point в градусах за секунду
    [Min(0.01f)] public float playerStoppingDistance = 0.15f; // На каком расстоянии от Point игрок должен остановиться
    [Min(0.1f)] public float maximumMoveTime = 10f; // Максимальное время движения, чтобы управление не зависло при препятствии
    public float scriptedGravity = -2f; // Небольшая вертикальная скорость, чтобы CharacterController оставался прижатым к полу

    [Header("Apartment Final Sequence")] // Связь с режиссёром квартиры
    public ApartmentFinalSequence apartmentFinalSequence; // Сюда назначается ApartmentFinalSequence нужной квартиры

    [Header("Debug")] // Раздел отладки
    public bool showDebugLogs = true; // Показывать ли сообщения триггера в Console

    private Collider triggerCollider; // Collider этого объекта
    private bool hasActivated = false; // Был ли триггер уже активирован
    private Coroutine playerMoveCoroutine; // Храним активную Coroutine движения игрока
    private bool playerControllerWasEnabledBeforeMove = false; // Запоминаем исходное состояние управления игроком
    private readonly HashSet<Collider> playerCollidersInside = new HashSet<Collider>(); // Храним коллайдеры игрока, находящиеся внутри Trigger

    private void Awake() // Вызывается при загрузке объекта
    {
        triggerCollider = GetComponent<Collider>(); // Получаем Collider с этого объекта

        if (activateOnEnter && triggerCollider == null && showDebugLogs) // Проверяем наличие Collider для входной активации
        {
            Debug.LogWarning("UniversalScenarioTrigger: Activate On Enter включён, но на объекте нет Collider.", gameObject); // Сообщаем о проблеме
        }

        if (activateOnEnter && triggerCollider != null && !triggerCollider.isTrigger && showDebugLogs) // Проверяем Is Trigger
        {
            Debug.LogWarning("UniversalScenarioTrigger: для Activate On Enter включи Is Trigger у Collider.", gameObject); // Напоминаем нужную настройку
        }
    }

    private void OnDisable() // Вызывается, если сам компонент или весь объект отключили
    {
        StopPlayerMoveAndRestoreController(); // Не оставляем управление игроком выключенным при отключении объекта
    }

    private void OnTriggerEnter(Collider other) // Unity вызывает этот метод, когда Collider входит внутрь Trigger
    {
        if (!activateOnEnter) return; // Если активация через вход выключена, ничего не делаем
        if (!IsPlayerCollider(other)) return; // Если вошёл не игрок, ничего не делаем

        bool wasPlayerAlreadyInside = playerCollidersInside.Count > 0; // Проверяем, был ли внутри другой Collider того же игрока
        playerCollidersInside.Add(other); // Добавляем Collider игрока в набор

        if (wasPlayerAlreadyInside) return; // Не запускаем событие второй раз из-за нескольких коллайдеров игрока

        TryActivate(ActivationSource.Enter); // Запускаем входную реплику и при включённой галочке движение игрока к Point
    }

    private void OnTriggerExit(Collider other) // Unity вызывает этот метод, когда Collider выходит из Trigger
    {
        if (!IsPlayerCollider(other)) return; // Если вышел не Collider игрока, ничего не меняем

        playerCollidersInside.Remove(other); // Удаляем Collider игрока из набора
    }

    private bool IsPlayerCollider(Collider targetCollider) // Проверяет Collider и его родителей на Tag игрока
    {
        if (targetCollider == null) return false; // Защищаемся от пустой ссылки

        Transform currentTransform = targetCollider.transform; // Начинаем проверку с вошедшего Collider

        while (currentTransform != null) // Поднимаемся вверх по Hierarchy
        {
            if (currentTransform.CompareTag(playerTag)) return true; // Возвращаем true, если нашли Tag игрока

            currentTransform = currentTransform.parent; // Переходим к родителю
        }

        return false; // Сообщаем, что Collider не принадлежит игроку
    }

    public void Interact() // Вызывается PlayerInteractor при нажатии E
    {
        if (!activateOnInteract) return; // Если активация через E выключена, ничего не делаем

        TryActivate(ActivationSource.Interact); // Запускаем активацию через E
    }

    public void Hit() // Вызывается PlayerInteractor при ударе ЛКМ
    {
        if (!activateOnHit) return; // Если активация через удар выключена, ничего не делаем

        TryActivate(ActivationSource.Hit); // Запускаем активацию через удар
    }

    public void TryActivate() // Сохраняем старый публичный метод для других скриптов и UnityEvent
    {
        TryActivate(ActivationSource.External); // Запускаем внешнюю активацию
    }

    private void TryActivate(ActivationSource activationSource) // Главный внутренний метод проверки активации
    {
        if (!canActivate) return; // Если триггер сейчас запрещён, ничего не делаем
        if (hasActivated && !repeatActivation) return; // Если повтор запрещён и триггер уже срабатывал, ничего не делаем

        Activate(activationSource); // Выполняем успешную активацию
    }

    private void Activate(ActivationSource activationSource) // Выполняет фактическую активацию
    {
        hasActivated = true; // Запоминаем успешное срабатывание

        ShowRandomDialogue(activationSource); // Показываем случайную реплику из нужного списка

        if (activationSource == ActivationSource.Enter) // Автоматическое движение нужно только при входе игрока в Trigger
        {
            StartPlayerMove(); // Запускаем разворот и движение PlayerCapsule к Point
        }

        if (showDebugLogs) // Если включены сообщения отладки
        {
            Debug.Log("UniversalScenarioTrigger: триггер успешно активирован. Способ: " + activationSource, gameObject); // Пишем способ активации
        }

        if (disableAfterActivation) // Если Collider нужно отключить после срабатывания
        {
            DisableTrigger(); // Отключаем Collider; Coroutine движения продолжает работать, потому что компонент остаётся включённым
        }
    }

    private void StartPlayerMove() // Проверяет настройки и запускает автоматическое движение игрока
    {
        if (!movePlayerToPointOnEnter) return; // Если главная галочка выключена, движение не выполняем

        if (playerRoot == null) // Проверяем PlayerCapsule
        {
            if (showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: поле Player Root пустое. Назначь PlayerCapsule.", gameObject); // Сообщаем, что назначить
            return; // Не запускаем движение без объекта игрока
        }

        if (playerMovePoint == null) // Проверяем Point
        {
            if (showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: поле Player Move Point пустое. Назначь точку назначения.", gameObject); // Сообщаем, что назначить
            return; // Не запускаем движение без Point
        }

        if (playerCharacterController == null) // Если CharacterController не назначен вручную
        {
            playerCharacterController = playerRoot.GetComponent<CharacterController>(); // Пытаемся автоматически найти его на PlayerCapsule
        }

        if (playerCharacterController == null) // Проверяем результат автоматического поиска
        {
            if (showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: CharacterController не найден. Назначь его в Player Character Controller.", gameObject); // Сообщаем, что назначить
            return; // Не двигаем игрока через Transform, чтобы не проходить сквозь стены
        }

        StopPlayerMoveAndRestoreController(); // Безопасно завершаем предыдущее движение, если оно ещё выполнялось

        playerControllerWasEnabledBeforeMove = playerController != null && playerController.enabled; // Запоминаем, было ли управление включено

        if (playerControllerWasEnabledBeforeMove) // Если FirstPersonController был активен
        {
            playerController.enabled = false; // Временно отключаем ввод игрока, чтобы он не мешал автоматическому движению
        }

        playerMoveCoroutine = StartCoroutine(MovePlayerToPointRoutine()); // Запускаем Coroutine движения к Point
    }

    private IEnumerator MovePlayerToPointRoutine() // Плавно поворачивает игрока и ведёт его к Point
    {
        float elapsedTime = 0f; // Считаем время движения для защиты от зависания

        while (elapsedTime < maximumMoveTime) // Продолжаем движение, пока не пришли или не вышло максимальное время
        {
            Vector3 directionToPoint = playerMovePoint.position - playerRoot.position; // Получаем направление от PlayerCapsule к Point
            directionToPoint.y = 0f; // Убираем высоту, чтобы двигаться только по горизонтали

            float horizontalDistance = directionToPoint.magnitude; // Вычисляем горизонтальное расстояние до Point

            if (horizontalDistance <= playerStoppingDistance) // Проверяем, достиг ли игрок нужной дистанции
            {
                break; // Завершаем движение
            }

            Vector3 normalizedDirection = directionToPoint / horizontalDistance; // Получаем нормализованное направление движения
            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection, Vector3.up); // Вычисляем поворот лицом к Point

            if (playerTurnSpeed <= 0f) // Проверяем мгновенный поворот
            {
                playerRoot.rotation = targetRotation; // Сразу поворачиваем игрока к Point
            }
            else // Иначе выполняем плавный разворот
            {
                playerRoot.rotation = Quaternion.RotateTowards( // Плавно приближаем текущий поворот к Point
                    playerRoot.rotation, // Текущий поворот игрока
                    targetRotation, // Нужный поворот
                    playerTurnSpeed * Time.deltaTime); // Скорость с учётом времени кадра
            }

            float remainingDistance = Mathf.Max(0f, horizontalDistance - playerStoppingDistance); // Считаем, сколько ещё можно пройти до дистанции остановки
            float moveDistanceThisFrame = Mathf.Min(playerMoveSpeed * Time.deltaTime, remainingDistance); // Не позволяем перелететь через Point
            Vector3 frameMovement = normalizedDirection * moveDistanceThisFrame; // Формируем горизонтальное перемещение этого кадра

            if (!playerCharacterController.isGrounded) // Если CharacterController не считает игрока стоящим на земле
            {
                frameMovement.y = scriptedGravity * Time.deltaTime; // Добавляем небольшое движение вниз
            }
            else // Если игрок стоит на поверхности
            {
                frameMovement.y = -0.5f * Time.deltaTime; // Слегка прижимаем CharacterController к полу
            }

            playerCharacterController.Move(frameMovement); // Двигаем игрока через CharacterController с учётом столкновений

            elapsedTime += Time.deltaTime; // Увеличиваем прошедшее время
            yield return null; // Ждём следующий кадр
        }

        Vector3 finalDirection = playerMovePoint.position - playerRoot.position; // Повторно получаем направление на Point после остановки
        finalDirection.y = 0f; // Убираем высоту

        if (finalDirection.sqrMagnitude > 0.0001f) // Проверяем, можно ли безопасно вычислить финальный поворот
        {
            playerRoot.rotation = Quaternion.LookRotation(finalDirection.normalized, Vector3.up); // Оставляем игрока лицом к Point
        }

        if (elapsedTime >= maximumMoveTime && showDebugLogs) // Проверяем, завершилось ли движение по тайм-ауту
        {
            Debug.LogWarning("UniversalScenarioTrigger: игрок не смог дойти до Player Move Point за Maximum Move Time. Возможно, путь перекрыт Collider.", gameObject); // Сообщаем возможную причину
        }

        SynchronizeStarterAssetsYaw(); // Перед включением управления синхронизируем внутренний yaw Starter Assets
        RestorePlayerController(); // Возвращаем управление игроку
        playerMoveCoroutine = null; // Очищаем ссылку на завершённую Coroutine
    }

    private void StopPlayerMoveAndRestoreController() // Останавливает текущее движение и безопасно возвращает управление
    {
        if (playerMoveCoroutine != null) // Если Coroutine сейчас выполняется
        {
            StopCoroutine(playerMoveCoroutine); // Останавливаем её
            playerMoveCoroutine = null; // Очищаем ссылку
        }

        RestorePlayerController(); // Возвращаем управление, если оно было включено до движения
    }

    private void RestorePlayerController() // Возвращает исходное состояние FirstPersonController
    {
        if (playerControllerWasEnabledBeforeMove && playerController != null) // Проверяем, нужно ли включить управление обратно
        {
            playerController.enabled = true; // Включаем FirstPersonController
        }

        playerControllerWasEnabledBeforeMove = false; // Сбрасываем сохранённое состояние
    }

    private void SynchronizeStarterAssetsYaw() // Синхронизирует итоговый угол с внутренним yaw Starter Assets
    {
        if (playerController == null || playerRoot == null) return; // Без контроллера или PlayerCapsule ничего не синхронизируем

        FieldInfo yawField = playerController.GetType().GetField( // Ищем внутреннее поле yaw в текущем FirstPersonController
            "_cinemachineTargetYaw", // Стандартное имя поля в Starter Assets
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); // Ищем закрытые и открытые поля экземпляра

        if (yawField == null) // Если версия Starter Assets использует другое имя поля
        {
            if (showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: поле _cinemachineTargetYaw не найдено. Проверь, не возвращается ли камера после движения.", gameObject); // Предупреждаем, но не ломаем событие
            return; // Завершаем синхронизацию
        }

        yawField.SetValue(playerController, playerRoot.eulerAngles.y); // Передаём конечный мировой угол Y игрока во FirstPersonController
    }

    private void ShowRandomDialogue(ActivationSource activationSource) // Выбирает случайный DialogueTextUI из нужного списка
    {
        DialogueTextUI[] selectedList = null; // Создаём ссылку на выбранный список

        if (activationSource == ActivationSource.Interact) // Проверяем активацию через E
        {
            selectedList = interactDialogueList; // Выбираем список E
        }
        else if (activationSource == ActivationSource.Hit) // Проверяем активацию через ЛКМ
        {
            selectedList = hitDialogueList; // Выбираем список удара
        }
        else if (activationSource == ActivationSource.Enter) // Проверяем активацию через вход
        {
            selectedList = enterDialogueList; // Выбираем список входа
        }
        else // Если это внешний вызов
        {
            return; // Не показываем реплику из этих трёх списков
        }

        DialogueTextUI selectedDialogue = GetRandomDialogue(selectedList); // Получаем случайный непустой элемент

        if (selectedDialogue == null) return; // Если список пуст, безопасно завершаем метод

        selectedDialogue.ShowConfiguredText(); // Показываем настроенный текст
    }

    private DialogueTextUI GetRandomDialogue(DialogueTextUI[] dialogueList) // Возвращает случайный непустой элемент списка
    {
        if (dialogueList == null || dialogueList.Length == 0) // Проверяем наличие массива и элементов
        {
            if (showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: список DialogueTextUI пуст.", gameObject); // Сообщаем причину отсутствия текста
            return null; // Возвращаем пустую ссылку
        }

        int validDialogueCount = 0; // Считаем назначенные элементы

        for (int i = 0; i < dialogueList.Length; i++) // Перебираем список
        {
            if (dialogueList[i] != null) validDialogueCount++; // Считаем только непустые ссылки
        }

        if (validDialogueCount == 0) // Если ни одного диалога не назначено
        {
            if (showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: в списке нет назначенных DialogueTextUI.", gameObject); // Сообщаем о пустых ячейках
            return null; // Возвращаем пустую ссылку
        }

        int randomValidIndex = Random.Range(0, validDialogueCount); // Выбираем случайный индекс среди назначенных элементов
        int currentValidIndex = 0; // Создаём счётчик текущего назначенного элемента

        for (int i = 0; i < dialogueList.Length; i++) // Ещё раз перебираем список
        {
            if (dialogueList[i] == null) continue; // Пропускаем пустые ячейки

            if (currentValidIndex == randomValidIndex) // Проверяем, достигли ли выбранного индекса
            {
                return dialogueList[i]; // Возвращаем выбранный DialogueTextUI
            }

            currentValidIndex++; // Переходим к следующему назначенному элементу
        }

        return null; // Запасной безопасный возврат
    }

    public void EnableTrigger() // Публичный метод включения триггера
    {
        canActivate = true; // Разрешаем активацию
        hasActivated = false; // Сбрасываем прошлое использование
        playerCollidersInside.Clear(); // Очищаем данные о коллайдерах игрока внутри Trigger

        if (triggerCollider == null) // Если ссылка на Collider ещё не получена
        {
            triggerCollider = GetComponent<Collider>(); // Повторно ищем Collider
        }

        if (triggerCollider != null) // Если Collider найден
        {
            triggerCollider.enabled = true; // Включаем Collider
        }
    }

    public void DisableTrigger() // Публичный метод отключения триггера
    {
        canActivate = false; // Запрещаем новую активацию
        playerCollidersInside.Clear(); // Очищаем данные о нахождении игрока внутри Trigger

        if (triggerCollider == null) // Если ссылка на Collider ещё не получена
        {
            triggerCollider = GetComponent<Collider>(); // Повторно ищем Collider
        }

        if (triggerCollider != null) // Если Collider найден
        {
            triggerCollider.enabled = false; // Отключаем только Collider, не отключая сам компонент
        }
    }
}