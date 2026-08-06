using UnityEngine; // Подключаем основные классы Unity.
using UnityEngine.Events; // Подключаем UnityEvent для событий в Inspector.

/// <summary>
/// Универсальный объект с тремя стадиями.
///
/// Стадии:
/// 0 — целый объект.
/// 1 — повреждённый или упавший объект.
/// 2 — уничтоженный объект.
///
/// Работает через E и через удар.
/// На третьей стадии может включать и выключать назначенные объекты.
/// </summary>
public class ThreeStageInteractableObject : MonoBehaviour, IInteractable, IHitInteractable
{
    public string hint = "Взаимодействовать";
    public string Hint => hint;
    [Header("STAGES")]

    [Tooltip("Визуал первой стадии: целый объект.")]
    public GameObject stageWhole; // Модель целого объекта.

    [Tooltip("Визуал второй стадии: повреждённый или упавший объект.")]
    public GameObject stageBroken; // Модель повреждённого объекта.

    [Tooltip("Визуал третьей стадии. Можно оставить пустым, если третий объект включается отдельным списком.")]
    public GameObject stageDestroyed; // Модель уничтоженного объекта.

    [Header("INTERACTION SETTINGS")]

    [Tooltip("Разрешить взаимодействие через клавишу E.")]
    public bool canUseE = true; // Можно ли использовать E.

    [Tooltip("Разрешить взаимодействие через удар.")]
    public bool canUseHit = true; // Можно ли использовать удар.

    [Min(1)]
    [Tooltip("Сколько нажатий E требуется для перехода на следующую стадию.")]
    public int ePressesForNextStage = 1; // Количество нажатий E.

    [Min(1)]
    [Tooltip("Сколько ударов требуется для перехода на следующую стадию.")]
    public int hitsForNextStage = 1; // Количество ударов.

    [Tooltip("Разрешить обычным E или ударом перейти из Broken (Stage 1) в Destroyed (Stage 2). Выключи для баррикады, которую уничтожает только сценарий окна.")]
    public bool allowInteractionToDestroyedStage = true; // Разрешаем или запрещаем ручной переход на третью стадию.

    [Header("OBJECTS ACTIVATED ONE BY ONE")]

    [Tooltip("Объекты, которые включаются по одному после каждого успешного взаимодействия.")]
    public GameObject[] objectsToActivate; // Последовательно включаемые объекты.

    [Tooltip("Автоматически выключить объекты из списка Objects To Activate при старте сцены.")]
    public bool disableObjectsOnStart = true; // Начальное выключение последовательных объектов.

    [Header("DESTROYED STAGE ACTIONS")]

    [Tooltip("Объекты, которые будут включены при переходе на третью стадию.")]
    public GameObject[] objectsToEnableWhenDestroyed; // Объекты для включения на стадии 2.

    [Tooltip("Объекты, которые будут выключены при переходе на третью стадию.")]
    public GameObject[] objectsToDisableWhenDestroyed; // Объекты для выключения на стадии 2.

    [Tooltip("Автоматически выключить Objects To Enable When Destroyed в начале сцены.")]
    public bool disableDestroyedObjectsOnStart = true; // Скрываем финальные объекты до уничтожения.

    [Header("FINAL SETTINGS")]

    [Tooltip("Отключить основной Collider после достижения третьей стадии.")]
    public bool disableColliderWhenDestroyed = true; // Нужно ли отключать Collider.

    [Tooltip("Основной Collider объекта взаимодействия.")]
    public Collider objectCollider; // Collider, принимающий взаимодействие.

    [Header("EVENTS")]

    [Tooltip("Вызывается при каждом успешном взаимодействии.")]
    public UnityEvent onInteract; // Событие каждого взаимодействия.

    [Tooltip("Вызывается при каждом переходе на следующую стадию.")]
    public UnityEvent onStageChanged; // Событие любой смены стадии.

    [Tooltip("Вызывается только один раз при достижении третьей стадии.")]
    public UnityEvent onDestroyed; // Отдельное событие уничтожения.

    [Header("DEBUG")]

    [SerializeField]
    private int currentStage = 0; // Текущая стадия: 0, 1 или 2.

    [SerializeField]
    private int nextObjectIndex = 0; // Индекс следующего включаемого объекта.

    private int currentEPresses = 0; // Текущий счётчик нажатий E.

    private int currentHits = 0; // Текущий счётчик ударов.

    private bool destroyedActionsApplied = false; // Выполнялись ли финальные действия.

    public int CurrentStage
    {
        get
        {
            return currentStage; // Возвращаем текущую стадию.
        }
    }

    public bool IsDestroyed
    {
        get
        {
            return currentStage >= 2; // Третья стадия имеет индекс 2.
        }
    }

    private void Start()
    {
        currentStage = Mathf.Clamp(currentStage, 0, 2); // Ограничиваем стадию допустимыми значениями.

        nextObjectIndex = 0; // Начинаем последовательность с первого объекта.

        if (disableObjectsOnStart)
        {
            DisableAllActivationObjects(); // Выключаем последовательные объекты.
        }

        if (disableDestroyedObjectsOnStart && !IsDestroyed)
        {
            SetObjectsActive(objectsToEnableWhenDestroyed, false); // Прячем финальные объекты до стадии 2.
        }

        ApplyStageVisuals(); // Применяем правильный визуал текущей стадии.

        if (IsDestroyed)
        {
            ApplyDestroyedStageActions(); // Восстанавливаем финальное состояние при старте со стадии 2.
        }
    }

    public void Interact()
    {
        if (!canUseE) return; // Взаимодействие через E запрещено.
        if (!CanAdvanceByInteraction()) return; // Проверяем, разрешён ли ручной переход с текущей стадии.

        onInteract.Invoke(); // Вызываем общее событие взаимодействия.
        ActivateNextObject(); // Включаем следующий объект из последовательного списка.
        currentEPresses++; // Увеличиваем счётчик E.

        if (currentEPresses < Mathf.Max(1, ePressesForNextStage)) return; // Ждём нужное количество нажатий.

        currentEPresses = 0; // Сбрасываем нажатия E.
        currentHits = 0; // Сбрасываем удары, чтобы способы не смешивались.
        AdvanceStage(); // Переходим на следующую стадию.
    }

    public void Hit()
    {
        if (!canUseHit) return; // Взаимодействие через удар запрещено.
        if (!CanAdvanceByInteraction()) return; // Проверяем, разрешён ли ручной переход с текущей стадии.

        onInteract.Invoke(); // Вызываем общее событие взаимодействия.
        ActivateNextObject(); // Включаем следующий объект из последовательного списка.
        currentHits++; // Увеличиваем счётчик ударов.

        if (currentHits < Mathf.Max(1, hitsForNextStage)) return; // Ждём нужное количество ударов.

        currentHits = 0; // Сбрасываем удары.
        currentEPresses = 0; // Сбрасываем нажатия E.
        AdvanceStage(); // Переходим на следующую стадию.
    }

    private bool CanAdvanceByInteraction()
    {
        if (IsDestroyed) return false; // После Stage 2 взаимодействие больше не работает.

        if (currentStage == 1 && !allowInteractionToDestroyedStage)
        {
            currentEPresses = 0; // Не сохраняем старые нажатия E на запрещённой стадии.
            currentHits = 0; // Не сохраняем старые удары на запрещённой стадии.
            return false; // Обычным взаимодействием нельзя перейти из Broken в Destroyed.
        }

        return true; // Для остальных разрешённых стадий взаимодействие работает.
    }

    private void AdvanceStage()
    {
        if (IsDestroyed) return; // Не разрешаем перейти дальше стадии 2.

        SetStage(currentStage + 1, true); // Увеличиваем стадию и вызываем события.
    }

    public void ForceSetStage(int newStage)
    {
        SetStage(newStage, true); // Устанавливаем стадию с вызовом событий.
    }

    private void SetStage(int newStage, bool invokeEvents)
    {
        int clampedStage = Mathf.Clamp(newStage, 0, 2); // Ограничиваем новое значение.

        if (currentStage == clampedStage) return; // Не повторяем ту же стадию.

        currentStage = clampedStage; // Сохраняем новую стадию.
        currentEPresses = 0; // Сбрасываем счётчик E.
        currentHits = 0; // Сбрасываем счётчик ударов.

        ApplyStageVisuals(); // Переключаем визуальные объекты.

        if (invokeEvents)
        {
            onStageChanged.Invoke(); // Сообщаем о смене стадии.
        }

        if (IsDestroyed)
        {
            if (invokeEvents)
            {
                onDestroyed.Invoke(); // Вызываем событие только для стадии 2.
            }

            ApplyDestroyedStageActions(); // Включаем и выключаем назначенные объекты.
        }
    }

    private void ActivateNextObject()
    {
        if (objectsToActivate == null) return; // Массив не назначен.
        if (objectsToActivate.Length == 0) return; // Массив пустой.

        while (nextObjectIndex < objectsToActivate.Length)
        {
            GameObject objectToActivate = objectsToActivate[nextObjectIndex]; // Получаем следующий объект.
            nextObjectIndex++; // Переходим к следующему индексу.

            if (objectToActivate == null) continue; // Пропускаем пустую ячейку.

            objectToActivate.SetActive(true); // Включаем найденный объект.
            return; // За одно взаимодействие включаем только один объект.
        }
    }

    private void DisableAllActivationObjects()
    {
        SetObjectsActive(objectsToActivate, false); // Выключаем весь последовательный список.
    }

    private void ApplyStageVisuals()
    {
        if (stageWhole != null)
        {
            stageWhole.SetActive(currentStage == 0); // Целая модель видна только на стадии 0.
        }

        if (stageBroken != null)
        {
            stageBroken.SetActive(currentStage == 1); // Упавшая модель видна только на стадии 1.
        }

        if (stageDestroyed != null)
        {
            stageDestroyed.SetActive(currentStage == 2); // Уничтоженная модель видна только на стадии 2.
        }

        if (objectCollider == null) return; // Если Collider не назначен, дальше ничего не меняем.

        bool shouldDisableCollider = IsDestroyed && disableColliderWhenDestroyed; // Определяем состояние Collider.
        objectCollider.enabled = !shouldDisableCollider; // Отключаем Collider только на финальной стадии.
    }

    private void ApplyDestroyedStageActions()
    {
        if (destroyedActionsApplied) return; // Не выполняем финальные действия повторно.

        destroyedActionsApplied = true; // Запоминаем выполнение.

        SetObjectsActive(objectsToEnableWhenDestroyed, true); // Сначала включаем внешние финальные объекты.
        SetObjectsActive(objectsToDisableWhenDestroyed, false); // Затем выключаем старые объекты.
    }

    private void SetObjectsActive(GameObject[] objects, bool activeState)
    {
        if (objects == null) return; // Массив отсутствует.

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null) continue; // Пропускаем пустые элементы.

            objects[i].SetActive(activeState); // Устанавливаем нужное состояние объекта.
        }
    }
}