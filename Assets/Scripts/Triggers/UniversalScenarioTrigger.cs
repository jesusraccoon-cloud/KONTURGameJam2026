using System.Collections.Generic; // Подключаем HashSet для надёжного учёта коллайдеров игрока внутри Trigger
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
    public bool repeatActivation = false; // Если включено, реплика может показываться при каждом новом взаимодействии
    public bool disableAfterActivation = true; // Отключать ли триггер после успешной активации

    [Header("Enter Settings")] // Раздел настройки определения игрока при входе
    public string playerTag = "Player"; // Tag игрока или одного из его родительских объектов

    [Header("Dialogue Lists")] // Раздел отдельных списков диалогов для E и ЛКМ
    public DialogueTextUI[] interactDialogueList; // Список готовых DialogueTextUI для случайного выбора при нажатии E

    public DialogueTextUI[] hitDialogueList; // Отдельный список готовых DialogueTextUI для случайного выбора при ударе ЛКМ

    public DialogueTextUI[] enterDialogueList; // Отдельный список готовых DialogueTextUI для случайного выбора при входе игрока

    [Header("Apartment Final Sequence")] // Связь с режиссёром квартиры
    public ApartmentFinalSequence apartmentFinalSequence; // Сюда назначается ApartmentFinalSequence нужной квартиры

    [Header("Debug")] // Раздел отладки
    public bool showDebugLogs = true; // Показывать ли сообщения триггера в Console

    private Collider triggerCollider; // Collider этого объекта
    private bool hasActivated = false; // Был ли триггер уже активирован
    private readonly HashSet<Collider> playerCollidersInside = new HashSet<Collider>(); // Храним все коллайдеры игрока, которые сейчас находятся внутри Trigger

    private void Awake() // Вызывается при загрузке объекта
    {
        triggerCollider = GetComponent<Collider>(); // Получаем Collider с этого объекта

        if (activateOnEnter && triggerCollider == null && showDebugLogs) // Проверяем наличие Collider для активации через вход
        {
            Debug.LogWarning("UniversalScenarioTrigger: Activate On Enter включён, но на объекте нет Collider.", gameObject); // Сообщаем причину отсутствия входной активации
        }

        if (activateOnEnter && triggerCollider != null && !triggerCollider.isTrigger && showDebugLogs) // Проверяем режим Collider для события OnTriggerEnter
        {
            Debug.LogWarning("UniversalScenarioTrigger: для Activate On Enter включи Is Trigger у Collider.", gameObject); // Напоминаем необходимую настройку физического триггера
        }
    }

    private void OnTriggerEnter(Collider other) // Unity вызывает этот метод, когда другой Collider входит внутрь Trigger
    {
        if (!activateOnEnter) return; // Если активация через вход выключена, ничего не делаем

        if (!IsPlayerCollider(other)) return; // Если вошедший Collider не принадлежит игроку, ничего не делаем

        bool wasPlayerAlreadyInside = playerCollidersInside.Count > 0; // Запоминаем, находился ли внутри другой Collider игрока
        playerCollidersInside.Add(other); // Добавляем вошедший Collider игрока в набор

        if (wasPlayerAlreadyInside) return; // Не активируем триггер повторно из-за второго Collider того же игрока

        TryActivate(ActivationSource.Enter); // Активируем триггер и выбираем реплику из списка входа
    }

    private void OnTriggerExit(Collider other) // Unity вызывает этот метод, когда другой Collider выходит из Trigger
    {
        if (!IsPlayerCollider(other)) return; // Если вышедший Collider не принадлежит игроку, ничего не меняем

        playerCollidersInside.Remove(other); // Удаляем вышедший Collider из набора находящихся внутри
    }

    private bool IsPlayerCollider(Collider targetCollider) // Проверяет Collider и всех его родителей на Tag игрока
    {
        if (targetCollider == null) return false; // Защищаемся от пустой ссылки

        Transform currentTransform = targetCollider.transform; // Начинаем проверку с объекта вошедшего Collider

        while (currentTransform != null) // Поднимаемся вверх по Hierarchy до самого верхнего родителя
        {
            if (currentTransform.CompareTag(playerTag)) return true; // Возвращаем успех, если нашли объект с Tag игрока

            currentTransform = currentTransform.parent; // Переходим к следующему родительскому объекту
        }

        return false; // Сообщаем, что Collider не принадлежит игроку
    }

    public void Interact() // Вызывается PlayerInteractor при нажатии E
    {
        if (!activateOnInteract) return; // Если активация через E выключена, ничего не делаем

        TryActivate(ActivationSource.Interact); // Пытаемся активировать триггер и показать текст для E
    }

    public void Hit() // Вызывается PlayerInteractor при ударе ЛКМ
    {
        if (!activateOnHit) return; // Если активация через удар выключена, ничего не делаем

        TryActivate(ActivationSource.Hit); // Пытаемся активировать триггер и показать текст для ЛКМ
    }

    public void TryActivate() // Сохраняем старый публичный метод для вызовов из других скриптов и UnityEvent
    {
        TryActivate(ActivationSource.External); // Выполняем внешнюю активацию без текста E или ЛКМ
    }

    private void TryActivate(ActivationSource activationSource) // Главный внутренний метод попытки активации с указанием источника
    {
        if (!canActivate) return; // Если триггер выключен, ничего не делаем

        if (hasActivated && !repeatActivation) return; // Без разрешённого повтора триггер говорит реплику только один раз

        Activate(activationSource); // Выполняем успешную активацию выбранным способом
    }

    private void Activate(ActivationSource activationSource) // Выполняет фактическую активацию
    {
        hasActivated = true; // Запоминаем успешную активацию

        ShowRandomDialogue(activationSource); // Случайно выбираем DialogueTextUI из списка E или ЛКМ

        if (showDebugLogs) // Если включены отладочные сообщения
        {
            Debug.Log("UniversalScenarioTrigger: триггер успешно активирован. Способ: " + activationSource, gameObject); // Показываем информацию об активации
        }

        if (disableAfterActivation) // Если триггер должен отключиться после активации
        {
            DisableTrigger(); // Отключаем триггер после передачи текста отдельному UI-контроллеру
        }
    }

    private void ShowRandomDialogue(ActivationSource activationSource) // Выбирает случайный готовый DialogueTextUI из нужного списка
    {
        DialogueTextUI[] selectedList = null; // Создаём ссылку на список, из которого будет сделан случайный выбор

        if (activationSource == ActivationSource.Interact) // Проверяем активацию через E
        {
            selectedList = interactDialogueList; // Выбираем отдельный список взаимодействия через E
        }
        else if (activationSource == ActivationSource.Hit) // Проверяем активацию через ЛКМ
        {
            selectedList = hitDialogueList; // Выбираем отдельный список удара через ЛКМ
        }
        else if (activationSource == ActivationSource.Enter) // Проверяем активацию через вход игрока
        {
            selectedList = enterDialogueList; // Выбираем отдельный список диалогов для входа
        }
        else // Если это внешний вызов
        {
            return; // Не выбираем диалог из списков E или ЛКМ
        }

        DialogueTextUI selectedDialogue = GetRandomDialogue(selectedList); // Получаем случайный непустой элемент выбранного списка

        if (selectedDialogue == null) return; // Если в списке нет готовых диалогов, безопасно выходим

        selectedDialogue.ShowConfiguredText(); // Показываем текст, заранее настроенный внутри выбранного DialogueTextUI
    }

    private DialogueTextUI GetRandomDialogue(DialogueTextUI[] dialogueList) // Возвращает случайный непустой элемент переданного списка
    {
        if (dialogueList == null || dialogueList.Length == 0) // Проверяем, существует ли список и есть ли в нём элементы
        {
            if (showDebugLogs) // Проверяем, включена ли отладка
            {
                Debug.LogWarning("UniversalScenarioTrigger: список DialogueTextUI пуст.", gameObject); // Сообщаем причину отсутствия диалога
            }

            return null; // Возвращаем пустую ссылку
        }

        int validDialogueCount = 0; // Создаём счётчик непустых ссылок в списке

        for (int i = 0; i < dialogueList.Length; i++) // Перебираем весь список
        {
            if (dialogueList[i] != null) validDialogueCount++; // Считаем только реально назначенные DialogueTextUI
        }

        if (validDialogueCount == 0) // Проверяем, найден ли хотя бы один назначенный диалог
        {
            if (showDebugLogs) // Проверяем, включена ли отладка
            {
                Debug.LogWarning("UniversalScenarioTrigger: в списке нет назначенных DialogueTextUI.", gameObject); // Сообщаем о пустых ячейках списка
            }

            return null; // Возвращаем пустую ссылку
        }

        int randomValidIndex = Random.Range(0, validDialogueCount); // Выбираем случайный номер среди непустых элементов с равной вероятностью
        int currentValidIndex = 0; // Создаём счётчик текущего непустого элемента

        for (int i = 0; i < dialogueList.Length; i++) // Ещё раз перебираем список для поиска выбранного элемента
        {
            if (dialogueList[i] == null) continue; // Пропускаем пустые ячейки Inspector

            if (currentValidIndex == randomValidIndex) // Проверяем, достигли ли случайно выбранного номера
            {
                return dialogueList[i]; // Возвращаем выбранный DialogueTextUI
            }

            currentValidIndex++; // Переходим к следующему непустому элементу
        }

        return null; // Запасной безопасный возврат на случай неожиданного изменения списка
    }

    public void EnableTrigger() // Публичный метод включения триггера
    {
        canActivate = true; // Разрешаем активацию

        hasActivated = false; // Сбрасываем прошлое использование

        playerCollidersInside.Clear(); // Очищаем старые данные о коллайдерах игрока внутри Trigger

        if (triggerCollider == null) // Проверяем, была ли ранее получена ссылка на Collider
        {
            triggerCollider = GetComponent<Collider>(); // Повторно ищем Collider на этом объекте
        }

        if (triggerCollider != null) // Если Collider найден
        {
            triggerCollider.enabled = true; // Включаем Collider
        }
    }

    public void DisableTrigger() // Публичный метод отключения триггера
    {
        canActivate = false; // Запрещаем активацию

        playerCollidersInside.Clear(); // Очищаем данные о нахождении игрока внутри Trigger

        if (triggerCollider == null) // Проверяем, была ли ранее получена ссылка на Collider
        {
            triggerCollider = GetComponent<Collider>(); // Повторно ищем Collider на этом объекте
        }

        if (triggerCollider != null) // Если Collider найден
        {
            triggerCollider.enabled = false; // Отключаем Collider
        }
    }
}
