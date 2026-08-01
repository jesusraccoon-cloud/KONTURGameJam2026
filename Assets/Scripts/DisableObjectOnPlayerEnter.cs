using UnityEngine; // Подключаем основные классы Unity.

/// <summary>
/// Универсальный скрипт отключения объекта.
///
/// Может сработать тремя способами:
/// 1. Когда игрок входит в Trigger-зону.
/// 2. Когда игрок нажимает E.
/// 3. Когда игрок ударяет объект ЛКМ.
/// </summary>
[RequireComponent(typeof(Collider))] // На объекте обязательно должен находиться Collider.
public class DisableObjectOnPlayerEnter : MonoBehaviour, IInteractable, IHitInteractable
{
    [Header("ОБЪЕКТ ДЛЯ ОТКЛЮЧЕНИЯ")]

    [Tooltip("Перетащи сюда папку или отдельный объект, который должен отключиться.")]
    [SerializeField]
    private GameObject objectToDisable; // Объект, который будет отключён через SetActive(false).

    [Header("СПОСОБЫ АКТИВАЦИИ")]

    [Tooltip("Отключить объект, когда игрок войдёт в Trigger-зону.")]
    [SerializeField]
    private bool activateOnPlayerEnter = true; // Разрешаем активацию при входе игрока.

    [Tooltip("Отключить объект после короткого нажатия E.")]
    [SerializeField]
    private bool activateOnInteract = false; // Разрешаем активацию через интерфейс IInteractable.

    [Tooltip("Отключить объект после удара ЛКМ.")]
    [SerializeField]
    private bool activateOnHit = false; // Разрешаем активацию через интерфейс IHitInteractable.

    [Header("НАСТРОЙКИ TRIGGER-ЗОНЫ")]

    [Tooltip("Tag объекта игрока. Обычно используется Player.")]
    [SerializeField]
    private string playerTag = "Player"; // Tag, по которому определяется игрок.

    [Tooltip("Автоматически включить Is Trigger у Collider, если используется вход в зону.")]
    [SerializeField]
    private bool automaticallyEnableTrigger = true; // Автоматически превращаем Collider в Trigger-зону.

    [Header("ОБЩИЕ НАСТРОЙКИ")]

    [Tooltip("После успешного срабатывания скрипт больше не будет выполнять действие.")]
    [SerializeField]
    private bool activateOnlyOnce = true; // Ограничиваем действие одним успешным запуском.

    [Tooltip("Показывать сообщения скрипта в Unity Console.")]
    [SerializeField]
    private bool showDebugLogs = true; // Разрешаем диагностические сообщения.

    private bool hasActivated; // Запоминаем, сработал ли скрипт ранее.

    private Collider activationCollider; // Сохраняем Collider этого объекта.

    private void Awake()
    {
        // Получаем Collider, который находится на этом же GameObject.
        activationCollider = GetComponent<Collider>();

        // Проверяем, должен ли скрипт работать как зона входа игрока.
        if (activateOnPlayerEnter && automaticallyEnableTrigger)
        {
            // Автоматически включаем режим Trigger.
            activationCollider.isTrigger = true;
        }
    }

    private void OnValidate()
    {
        // Получаем Collider во время настройки объекта в Inspector.
        Collider currentCollider = GetComponent<Collider>();

        // Проверяем, найден ли Collider.
        if (currentCollider == null)
        {
            // Прекращаем выполнение, если Collider отсутствует.
            return;
        }

        // Проверяем, используется ли вход игрока и разрешена ли автоматическая настройка.
        if (activateOnPlayerEnter && automaticallyEnableTrigger)
        {
            // Автоматически включаем Is Trigger прямо в редакторе.
            currentCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, разрешена ли активация при входе игрока.
        if (!activateOnPlayerEnter)
        {
            // Ничего не делаем, если способ отключён в Inspector.
            return;
        }

        // Проверяем Tag объекта, который вошёл в Trigger.
        if (!other.CompareTag(playerTag))
        {
            // Выходим, если в зону вошёл не игрок.
            return;
        }

        // Пытаемся отключить назначенный объект.
        TryDisableObject("вход игрока в Trigger-зону");
    }

    public void Interact()
    {
        // Проверяем, разрешено ли взаимодействие через E.
        if (!activateOnInteract)
        {
            // Ничего не делаем, если способ отключён в Inspector.
            return;
        }

        // Пытаемся отключить назначенный объект.
        TryDisableObject("нажатие E");
    }

    public void Hit()
    {
        // Проверяем, разрешено ли взаимодействие через удар.
        if (!activateOnHit)
        {
            // Ничего не делаем, если способ отключён в Inspector.
            return;
        }

        // Пытаемся отключить назначенный объект.
        TryDisableObject("удар ЛКМ");
    }

    private void TryDisableObject(string activationSource)
    {
        // Проверяем, был ли скрипт уже активирован.
        if (activateOnlyOnce && hasActivated)
        {
            // Повторное выполнение запрещено.
            return;
        }

        // Проверяем, назначен ли отключаемый объект.
        if (objectToDisable == null)
        {
            // Показываем предупреждение, если поле Inspector осталось пустым.
            Debug.LogWarning(
                "[DisableObjectOnPlayerEnter] Не назначено поле Object To Disable на объекте: "
                + gameObject.name,
                gameObject
            );

            // Не продолжаем работу без назначенного объекта.
            return;
        }

        // Проверяем, не был ли объект уже отключён.
        if (!objectToDisable.activeSelf)
        {
            // Запоминаем завершённое состояние.
            hasActivated = true;

            // При необходимости показываем сообщение.
            if (showDebugLogs)
            {
                Debug.Log(
                    "[DisableObjectOnPlayerEnter] Объект уже отключён: "
                    + objectToDisable.name,
                    gameObject
                );
            }

            // Повторно SetActive(false) не вызываем.
            return;
        }

        // Запоминаем успешную активацию.
        hasActivated = true;

        // Показываем информацию о способе активации.
        if (showDebugLogs)
        {
            Debug.Log(
                "[DisableObjectOnPlayerEnter] Способ активации: "
                + activationSource
                + ". Отключаем объект: "
                + objectToDisable.name,
                gameObject
            );
        }

        // Полностью отключаем выбранный GameObject вместе со всеми дочерними объектами.
        objectToDisable.SetActive(false);
    }

    /// <summary>
    /// Позволяет другим сценарным скриптам отключить объект напрямую.
    /// </summary>
    public void DisableTargetObject()
    {
        // Используем общий безопасный метод отключения.
        TryDisableObject("вызов из другого скрипта");
    }

    /// <summary>
    /// Сбрасывает внутреннее состояние для повторного тестирования.
    /// Сам отключённый объект этот метод не включает.
    /// </summary>
    public void ResetActivationState()
    {
        // Разрешаем скрипту сработать повторно.
        hasActivated = false;
    }
}