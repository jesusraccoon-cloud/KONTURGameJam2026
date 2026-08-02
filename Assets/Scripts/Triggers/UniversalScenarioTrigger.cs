using System.Collections; // Coroutine для задержек и движения
using System.Collections.Generic; // HashSet для коллайдеров игрока
using System.Reflection; // Синхронизация yaw Starter Assets
using UnityEngine; // Основные классы Unity
using UnityEngine.Events; // UnityEvent (хук для сохранений)

public class UniversalScenarioTrigger : MonoBehaviour, IInteractable, IHitInteractable
{
    private enum ActivationSource { Enter, Interact, Hit, External } // Источник активации

    [Header("Activation Type")]
    public bool activateOnEnter = false; // Активация при входе
    public bool activateOnInteract = true; // Активация через E
    public bool activateOnHit = false; // Активация через ЛКМ

    [Header("Trigger Settings")]
    public bool canActivate = true; // Разрешена ли активация
    public bool repeatActivation = false; // Разрешать повторные активации
    public bool disableAfterActivation = true; // Отключить Collider после срабатывания

    [Header("Enter Settings")]
    public string playerTag = "Player"; // Tag игрока или его родителя

    [Header("Dialogue Lists")]
    public DialogueTextUI[] interactDialogueList; // Реплики для E
    public DialogueTextUI[] hitDialogueList; // Реплики для удара
    public DialogueTextUI[] enterDialogueList; // Реплики для входа

    [Header("Dialogue Order")]
    public bool playDialogueListInOrder = false; // Проиграть весь выбранный список сверху вниз за одну активацию
    [Min(0.05f)] public float dialogueLineInterval = 3f; // Пауза между запуском соседних реплик (общая, если per-line выключен)
    public bool usePerLineDuration = false; // Ждать длину КАЖДОЙ реплики (её Visible Duration) вместо общего интервала — чтобы голос не обрывался
    [Min(0f)] public float perLinePauseAfter = 0.25f; // Доп. пауза после каждой реплики в режиме per-line

    [Header("Dialogue Delay")]
    public bool useDialogueDelay = false; // Использовать задержку реплики
    [Min(0f)] public float dialogueDelaySeconds = 0f; // Задержка в секундах

    [Header("Player References")]
    public Transform playerRoot; // Корень PlayerCapsule
    public MonoBehaviour playerController; // Starter Assets FirstPersonController
    public CharacterController playerCharacterController; // CharacterController игрока

    [Header("Player Control During Dialogue")]
    public bool lockPlayerControlDuringDialogue = false; // Блокировать управление во время диалога
    public bool lockControlDuringDialogueDelay = false; // Блокировать управление во время задержки
    [Min(0.05f)] public float dialogueControlLockDuration = 5f; // Длительность блокировки после старта реплики

    [Header("PLAYER WALK TO POINT - V8")]
    public bool movePlayerToPointOnEnter = false; // Вести игрока к Point при входе
    public Transform playerMovePoint; // Точка назначения
    [Min(0f)] public float playerMoveSpeed = 1.5f; // Скорость движения
    [Min(0f)] public float playerTurnSpeed = 360f; // Скорость разворота
    [Min(0.01f)] public float playerStoppingDistance = 0.15f; // Дистанция остановки
    [Min(0.1f)] public float maximumMoveTime = 10f; // Защита от зависания
    public float scriptedGravity = -2f; // Вертикальная скорость при движении

    [Header("Apartment Final Sequence")]
    public ApartmentFinalSequence apartmentFinalSequence; // Сохранённая ссылка на режиссёра квартиры

    [Header("Save Hook")]
    public UnityEvent onActivated; // Доп. реакция при активации (MarkUsed вызывается автоматически)

    [SerializeField] private SC_Saveable saveable; // Компонент сохранения (авто-поиск). Через него триггер сам себя помечает и глушит после загрузки

    [Header("Debug")]
    public bool showDebugLogs = true; // Сообщения в Console

    private Collider triggerCollider; // Collider этого объекта
    private bool hasActivated; // Был ли триггер активирован
    private bool movementLock; // Блокировка от автоматического движения
    private bool dialogueLock; // Блокировка от диалога
    private bool controllerStateCaptured; // Сохранено ли исходное состояние контроллера
    private bool controllerWasEnabled; // Исходное состояние FirstPersonController
    private Coroutine moveRoutine; // Coroutine движения
    private Coroutine dialogueDelayRoutine; // Coroutine задержки одной случайной реплики
    private Coroutine dialogueSequenceRoutine; // Coroutine последовательного списка реплик
    private Coroutine dialogueUnlockRoutine; // Coroutine возврата управления
    private FieldInfo yawField; // Кэш внутреннего yaw Starter Assets
    private MonoBehaviour yawFieldOwner; // Контроллер, для которого найден yaw
    private readonly HashSet<Collider> playerCollidersInside = new HashSet<Collider>(); // Коллайдеры игрока внутри Trigger

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>(); // Получаем Collider один раз
        if (saveable == null) saveable = GetComponent<SC_Saveable>(); // Ищем SC_Saveable на самом триггере
        if (saveable == null) saveable = GetComponentInChildren<SC_Saveable>(true); // Или на дочернем объекте
        CachePlayerReferences(); // Восстанавливаем ссылки игрока
        ValidateSettings(); // Проверяем настройку Trigger
    }

    private void Start()
    {
        // Если триггер уже срабатывал до загрузки чекпоинта — глушим его, чтобы сцена/звук не запустились повторно
        if (saveable != null && SC_SaveSystem.IsUsed(saveable.id))
        {
            hasActivated = true; // Помечаем сработавшим
            DisableTrigger(); // Выключаем Collider
        }
    }

    private void OnDisable()
    {
        StopPendingDialogue(); // Отменяем ещё не показанную реплику
        StopDialogueControlLock(); // Снимаем диалоговую блокировку
        StopPlayerMove(); // Останавливаем автоматическое движение
        playerCollidersInside.Clear(); // Очищаем коллайдеры
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!activateOnEnter || !IsPlayerCollider(other)) return; // Принимаем только игрока
        bool wasInside = playerCollidersInside.Count > 0; // Проверяем другие коллайдеры игрока
        playerCollidersInside.Add(other); // Запоминаем вошедший Collider
        if (!wasInside) TryActivate(ActivationSource.Enter); // Срабатываем один раз на вход игрока
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other)) playerCollidersInside.Remove(other); // Удаляем вышедший Collider игрока
    }

    private bool IsPlayerCollider(Collider target)
    {
        if (target == null) return false; // Защита от пустой ссылки
        for (Transform current = target.transform; current != null; current = current.parent) // Проверяем объект и родителей
            if (current.CompareTag(playerTag)) return true; // Найден Tag игрока
        return false; // Это не игрок
    }

    public void Interact()
    {
        if (activateOnInteract) TryActivate(ActivationSource.Interact); // Активация через E
    }

    public void Hit()
    {
        if (activateOnHit) TryActivate(ActivationSource.Hit); // Активация через ЛКМ
    }

    public void TryActivate()
    {
        TryActivate(ActivationSource.External); // Старый публичный внешний вызов
    }

    private void TryActivate(ActivationSource source)
    {
        if (!canActivate || (hasActivated && !repeatActivation)) return; // Проверяем разрешение и повтор
        hasActivated = true; // Запоминаем срабатывание
        if (saveable != null) saveable.MarkUsed(); // Автоматически помечаем триггер использованным (не нужно вручную вешать хук)
        onActivated.Invoke(); // Доп. реакция при активации
        ScheduleDialogue(source); // Запускаем реплику
        if (source == ActivationSource.Enter) StartPlayerMove(); // Движение используется только при входе
        if (showDebugLogs) Debug.Log("UniversalScenarioTrigger: активация — " + source, gameObject); // Отладка
        if (disableAfterActivation) DisableTrigger(); // При необходимости отключаем Collider
    }

    private void ScheduleDialogue(ActivationSource source)
    {
        DialogueTextUI[] list = GetDialogueList(source); // Получаем список нужного способа активации
        if (!HasDialogue(list, source)) return; // Нет назначенных реплик

        StopPendingDialogue(); // Останавливаем прежнюю задержку или последовательность
        StopDialogueControlLock(); // Сбрасываем прежний таймер блокировки

        bool hasDelay = useDialogueDelay && dialogueDelaySeconds > 0f; // Нужна ли задержка перед первой репликой
        if (lockPlayerControlDuringDialogue && lockControlDuringDialogueDelay && hasDelay) SetDialogueLock(true); // Блокируем сразу

        if (playDialogueListInOrder) // Нужно проиграть весь список сверху вниз
        {
            dialogueSequenceRoutine = StartCoroutine(PlayDialogueListInOrder(list, hasDelay)); // Запускаем Element 0, затем 1, 2 и дальше
            return; // Случайную реплику уже не выбираем
        }

        DialogueTextUI dialogue = GetRandomDialogue(list); // В старом режиме выбираем одну случайную реплику
        if (hasDelay) dialogueDelayRoutine = StartCoroutine(ShowDialogueAfterDelay(dialogue)); // Ждём задержку
        else ShowDialogue(dialogue); // Показываем сразу
    }

    private IEnumerator ShowDialogueAfterDelay(DialogueTextUI dialogue)
    {
        yield return new WaitForSeconds(dialogueDelaySeconds); // Ждём заданное время
        dialogueDelayRoutine = null; // Очищаем ссылку
        if (dialogue != null) ShowDialogue(dialogue); // Показываем реплику
        else SetDialogueLock(false); // Не оставляем игрока заблокированным
    }


    private IEnumerator PlayDialogueListInOrder(DialogueTextUI[] list, bool hasDelay)
    {
        if (hasDelay) yield return new WaitForSeconds(dialogueDelaySeconds); // Ждём задержку только перед первой репликой
        if (lockPlayerControlDuringDialogue) SetDialogueLock(true); // Удерживаем управление выключенным на всей последовательности

        for (int i = 0; i < list.Length; i++) // Идём по Inspector сверху вниз
        {
            if (list[i] == null) continue; // Пустые элементы безопасно пропускаем
            list[i].ShowConfiguredText(); // Проигрываем текущую реплику
            if (HasNextDialogue(list, i + 1)) // Проверяем, есть ли следующая реплика
            {
                float wait = usePerLineDuration // Выбираем паузу до следующей реплики
                    ? list[i].visibleDuration + perLinePauseAfter // Длина именно этой реплики + небольшая пауза (голос не обрывается)
                    : dialogueLineInterval; // Или общий интервал (старое поведение)
                yield return new WaitForSeconds(wait); // Ждём выбранное время
            }
        }

        if (lockPlayerControlDuringDialogue && dialogueControlLockDuration > 0f) // Даём последней реплике остаться на экране
            yield return new WaitForSeconds(dialogueControlLockDuration); // После списка это время работает как задержка возврата управления

        dialogueSequenceRoutine = null; // Очищаем ссылку на завершённую последовательность
        if (lockPlayerControlDuringDialogue) SetDialogueLock(false); // Возвращаем управление после всего списка
    }

    private void ShowDialogue(DialogueTextUI dialogue)
    {
        if (lockPlayerControlDuringDialogue) StartDialogueControlLock(); // Запускаем блокировку
        dialogue.ShowConfiguredText(); // Показываем настроенную реплику
    }

    private void StartDialogueControlLock()
    {
        if (dialogueUnlockRoutine != null) StopCoroutine(dialogueUnlockRoutine); // Перезапускаем старый таймер
        SetDialogueLock(true); // Запрещаем управление
        if (dialogueControlLockDuration <= 0f) { SetDialogueLock(false); return; } // Нулевая длительность
        dialogueUnlockRoutine = StartCoroutine(UnlockDialogueAfterDelay()); // Ждём возврата управления
    }

    private IEnumerator UnlockDialogueAfterDelay()
    {
        yield return new WaitForSeconds(dialogueControlLockDuration); // Ждём длительность сцены
        dialogueUnlockRoutine = null; // Очищаем ссылку
        SetDialogueLock(false); // Возвращаем управление, если движение уже завершено
    }

    private void StopPendingDialogue()
    {
        if (dialogueDelayRoutine != null) StopCoroutine(dialogueDelayRoutine); // Останавливаем задержку случайной реплики
        if (dialogueSequenceRoutine != null) StopCoroutine(dialogueSequenceRoutine); // Останавливаем последовательный список
        dialogueDelayRoutine = null; // Очищаем ссылку задержки
        dialogueSequenceRoutine = null; // Очищаем ссылку последовательности
    }

    private void StopDialogueControlLock()
    {
        if (dialogueUnlockRoutine != null) StopCoroutine(dialogueUnlockRoutine); // Останавливаем таймер
        dialogueUnlockRoutine = null; // Очищаем ссылку
        SetDialogueLock(false); // Снимаем только блокировку диалога
    }

    private DialogueTextUI[] GetDialogueList(ActivationSource source)
    {
        return source == ActivationSource.Interact ? interactDialogueList :
               source == ActivationSource.Hit ? hitDialogueList :
               source == ActivationSource.Enter ? enterDialogueList : null; // Выбираем нужный список
    }

    private bool HasDialogue(DialogueTextUI[] list, ActivationSource source)
    {
        if (list == null || list.Length == 0) // Список отсутствует или имеет Size 0
        {
            if (source != ActivationSource.External && showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: список DialogueTextUI пуст.", gameObject); // Предупреждение
            return false; // Проигрывать нечего
        }

        for (int i = 0; i < list.Length; i++) // Ищем хотя бы одну назначенную реплику
            if (list[i] != null) return true; // Нашли рабочий элемент

        if (showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: в списке нет назначенных DialogueTextUI.", gameObject); // Все элементы пустые
        return false; // Проигрывать нечего
    }

    private DialogueTextUI GetRandomDialogue(DialogueTextUI[] list)
    {
        DialogueTextUI selected = null; // Выбранная реплика
        int validCount = 0; // Количество непустых элементов
        for (int i = 0; i < list.Length; i++) // Один проход без временных массивов
        {
            if (list[i] == null) continue; // Пропускаем пустые поля
            validCount++; // Учитываем валидный элемент
            if (Random.Range(0, validCount) == 0) selected = list[i]; // Равномерный случайный выбор
        }
        return selected; // Возвращаем одну случайную реплику
    }

    private bool HasNextDialogue(DialogueTextUI[] list, int startIndex)
    {
        for (int i = startIndex; i < list.Length; i++) // Проверяем оставшуюся часть списка
            if (list[i] != null) return true; // Есть следующая непустая реплика
        return false; // Текущая реплика была последней
    }

    private void StartPlayerMove()
    {
        if (!movePlayerToPointOnEnter) return; // Движение выключено
        CachePlayerReferences(); // Восстанавливаем ссылки
        if (playerRoot == null || playerMovePoint == null || playerCharacterController == null) // Проверяем обязательные поля
        {
            if (showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: для движения назначь Player Root, Player Move Point и CharacterController.", gameObject); // Подсказка
            return; // Нельзя начать движение
        }

        StopPlayerMove(); // Перезапускаем прежнее движение
        SetMovementLock(true); // Отключаем ручное управление
        moveRoutine = StartCoroutine(MovePlayerToPoint()); // Начинаем движение
    }

    private IEnumerator MovePlayerToPoint()
    {
        float elapsed = 0f; // Прошедшее время
        while (elapsed < maximumMoveTime && playerRoot != null && playerMovePoint != null && playerCharacterController != null) // Движемся до точки или тайм-аута
        {
            Vector3 direction = playerMovePoint.position - playerRoot.position; // Направление к Point
            direction.y = 0f; // Только горизонтальное движение
            float distance = direction.magnitude; // Расстояние до Point
            if (distance <= playerStoppingDistance || distance <= Mathf.Epsilon) break; // Точка достигнута

            direction /= distance; // Нормализуем направление
            RotatePlayer(direction); // Поворачиваем игрока
            float step = Mathf.Min(playerMoveSpeed * Time.deltaTime, distance - playerStoppingDistance); // Не перелетаем Point
            Vector3 motion = direction * Mathf.Max(0f, step); // Горизонтальное движение
            motion.y = (playerCharacterController.isGrounded ? -0.5f : scriptedGravity) * Time.deltaTime; // Прижимаем к полу
            playerCharacterController.Move(motion); // Двигаем с учётом Collider

            elapsed += Time.deltaTime; // Обновляем таймер
            yield return null; // Следующий кадр
        }

        FaceMovePoint(); // Точно направляем игрока к Point
        if (elapsed >= maximumMoveTime && showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: игрок не дошёл до Point за Maximum Move Time.", gameObject); // Возможная преграда
        SynchronizeStarterAssetsYaw(); // Не даём камере вернуться назад
        moveRoutine = null; // Очищаем ссылку
        SetMovementLock(false); // Снимаем блокировку движения
    }

    private void RotatePlayer(Vector3 direction)
    {
        Quaternion target = Quaternion.LookRotation(direction, Vector3.up); // Целевой поворот
        playerRoot.rotation = playerTurnSpeed <= 0f ? target : Quaternion.RotateTowards(playerRoot.rotation, target, playerTurnSpeed * Time.deltaTime); // Плавный или мгновенный разворот
    }

    private void FaceMovePoint()
    {
        if (playerRoot == null || playerMovePoint == null) return; // Ссылки потеряны
        Vector3 direction = playerMovePoint.position - playerRoot.position; // Финальное направление
        direction.y = 0f; // Только ось Y
        if (direction.sqrMagnitude > 0.0001f) playerRoot.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up); // Точный финальный поворот
    }

    private void StopPlayerMove()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine); // Останавливаем движение
        moveRoutine = null; // Очищаем ссылку
        SetMovementLock(false); // Снимаем только блокировку движения
    }

    private void SetMovementLock(bool value)
    {
        movementLock = value; // Сохраняем запрос движения
        ApplyControllerLock(); // Применяем общее состояние
    }

    private void SetDialogueLock(bool value)
    {
        dialogueLock = value; // Сохраняем запрос диалога
        ApplyControllerLock(); // Применяем общее состояние
    }

    private void ApplyControllerLock()
    {
        CachePlayerReferences(); // Восстанавливаем ссылку при необходимости
        bool shouldLock = movementLock || dialogueLock; // Хотя бы одна система требует блокировки
        if (playerController == null) // Контроллер не назначен
        {
            if (shouldLock && showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: назначь FirstPersonController в Player Controller.", gameObject); // Подсказка
            return; // Нечего отключать
        }

        if (shouldLock) // Нужно заблокировать управление
        {
            if (!controllerStateCaptured) { controllerWasEnabled = playerController.enabled; controllerStateCaptured = true; } // Запоминаем исходное состояние
            playerController.enabled = false; // Отключаем ходьбу и камеру
        }
        else if (controllerStateCaptured) // Все блокировки сняты
        {
            playerController.enabled = controllerWasEnabled; // Возвращаем исходное состояние
            controllerStateCaptured = false; // Сбрасываем сохранение
            controllerWasEnabled = false; // Очищаем значение
        }
    }

    private void CachePlayerReferences()
    {
        if (playerRoot == null) // Пытаемся получить корень из назначенных компонентов
            playerRoot = playerCharacterController != null ? playerCharacterController.transform : playerController != null ? playerController.transform : null;
        if (playerRoot == null) return; // Искать компоненты негде
        if (playerCharacterController == null) playerCharacterController = playerRoot.GetComponent<CharacterController>(); // Ищем CharacterController
        if (playerController == null) playerController = playerRoot.GetComponent("FirstPersonController") as MonoBehaviour; // Ищем Starter Assets без namespace
    }

    private void SynchronizeStarterAssetsYaw()
    {
        if (playerController == null || playerRoot == null) return; // Нет нужных ссылок
        if (yawFieldOwner != playerController) // Контроллер изменился или поле ещё не найдено
        {
            yawFieldOwner = playerController; // Запоминаем владельца поля
            yawField = playerController.GetType().GetField("_cinemachineTargetYaw", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); // Ищем yaw
        }
        if (yawField != null) yawField.SetValue(playerController, playerRoot.eulerAngles.y); // Передаём итоговый угол
        else if (showDebugLogs) Debug.LogWarning("UniversalScenarioTrigger: поле _cinemachineTargetYaw не найдено.", gameObject); // Версия Starter Assets отличается
    }

    private void ValidateSettings()
    {
        if (!activateOnEnter || !showDebugLogs) return; // Проверка нужна только входному Trigger
        if (triggerCollider == null) Debug.LogWarning("UniversalScenarioTrigger: Activate On Enter включён, но Collider отсутствует.", gameObject); // Нет Collider
        else if (!triggerCollider.isTrigger) Debug.LogWarning("UniversalScenarioTrigger: для Activate On Enter включи Is Trigger.", gameObject); // Не включён Is Trigger
    }

    public void EnableTrigger()
    {
        canActivate = true; // Разрешаем активацию
        hasActivated = false; // Сбрасываем использование
        playerCollidersInside.Clear(); // Очищаем входы
        if (triggerCollider == null) triggerCollider = GetComponent<Collider>(); // Восстанавливаем Collider
        if (triggerCollider != null) triggerCollider.enabled = true; // Включаем Collider
    }

    public void DisableTrigger()
    {
        canActivate = false; // Запрещаем новую активацию
        playerCollidersInside.Clear(); // Очищаем входы
        if (triggerCollider == null) triggerCollider = GetComponent<Collider>(); // Восстанавливаем Collider
        if (triggerCollider != null) triggerCollider.enabled = false; // Отключаем только Collider
    }

    public void UnlockPlayerControlAfterDialogue()
    {
        StopDialogueControlLock(); // Ручной возврат управления из UnityEvent или другого скрипта
    }
}