using UnityEngine; // Подключаем основные классы Unity.
using System.Collections; // Подключаем корутины для задержки.

/// <summary>
/// Универсальный скрипт отключения объекта.
///
/// Может сработать тремя способами:
/// 1. Когда игрок входит в Trigger-зону.
/// 2. Когда игрок нажимает E.
/// 3. Когда игрок ударяет объект ЛКМ.
///
/// Перед отключением объекта может использоваться настраиваемая задержка.
/// </summary>
[RequireComponent(typeof(Collider))] // На объекте обязательно должен находиться Collider.
public class DisableObjectOnPlayerEnter : MonoBehaviour, IInteractable, IHitInteractable
{
    public string hint = "Взаимодействовать";


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

    [Header("ЗАДЕРЖКА")]

    [Tooltip("Через сколько секунд после активации отключится объект. Значение 0 отключает объект сразу.")]
    [Min(0f)]
    [SerializeField]
    private float activationDelay = 0f; // Задержка перед отключением объекта.

    [Header("НАСТРОЙКИ TRIGGER-ЗОНЫ")]

    [Tooltip("Tag объекта игрока. Обычно используется Player.")]
    [SerializeField]
    private string playerTag = "Player"; // Tag, по которому определяется игрок.

    [Tooltip("Автоматически включить Is Trigger у Collider, если используется вход в зону.")]
    [SerializeField]
    private bool automaticallyEnableTrigger = true; // Автоматически превращаем Collider в Trigger-зону.

    [Header("ОБЩИЕ НАСТРОЙКИ")]

    [Tooltip("После успешного запуска скрипт больше не будет выполнять действие повторно.")]
    [SerializeField]
    private bool activateOnlyOnce = true; // Ограничиваем действие одним запуском.

    [Tooltip("Показывать сообщения скрипта в Unity Console.")]
    [SerializeField]
    private bool showDebugLogs = true; // Разрешаем диагностические сообщения.

    private bool hasActivated; // Запоминаем, запускался ли скрипт ранее.

    private Collider activationCollider; // Сохраняем Collider этого объекта.

    private Coroutine activationCoroutine; // Сохраняем запущенную корутину задержки.

    public string Hint => hint;

    public bool ActivateOnInteract { get => activateOnInteract; set => activateOnInteract = value; }

    private void Awake()
    {
        // Получаем Collider, который находится на этом же GameObject.
        activationCollider = GetComponent<Collider>();

        // Проверяем, должен ли скрипт работать как Trigger-зона.
        if (activateOnPlayerEnter && automaticallyEnableTrigger)
        {
            // Автоматически включаем режим Trigger.
            activationCollider.isTrigger = true;
        }
    }

    private void OnValidate()
    {
        // Не позволяем установить отрицательную задержку.
        activationDelay = Mathf.Max(0f, activationDelay);

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

        // Запускаем отключение объекта.
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

        // Запускаем отключение объекта.
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

        // Запускаем отключение объекта.
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

        // Проверяем, запущено ли уже ожидание задержки.
        if (activationCoroutine != null)
        {
            // Не запускаем вторую корутину одновременно.
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

        // Запоминаем запуск активации сразу, чтобы действие не запустилось повторно во время задержки.
        hasActivated = true;

        // Запускаем корутину ожидания и отключения.
        activationCoroutine = StartCoroutine(
            DisableObjectAfterDelay(activationSource)
        );
    }

    private IEnumerator DisableObjectAfterDelay(string activationSource)
    {
        // Показываем сообщение о запуске задержки.
        if (showDebugLogs)
        {
            Debug.Log(
                "[DisableObjectOnPlayerEnter] Способ активации: "
                + activationSource
                + ". Задержка перед отключением: "
                + activationDelay
                + " сек.",
                gameObject
            );
        }

        // Проверяем, установлена ли задержка больше нуля.
        if (activationDelay > 0f)
        {
            // Ждём указанное в Inspector количество секунд.
            yield return new WaitForSeconds(activationDelay);
        }

        // Проверяем, существует ли объект после ожидания.
        if (objectToDisable != null)
        {
            // Показываем сообщение перед отключением.
            if (showDebugLogs)
            {
                Debug.Log(
                    "[DisableObjectOnPlayerEnter] Отключаем объект: "
                    + objectToDisable.name,
                    gameObject
                );
            }

            // Полностью отключаем выбранный GameObject вместе со всеми дочерними объектами.
            objectToDisable.SetActive(false);
        }

        // Очищаем ссылку на завершённую корутину.
        activationCoroutine = null;
    }

    /// <summary>
    /// Позволяет другим сценарным скриптам отключить объект напрямую.
    /// Указанная в Inspector задержка также будет применена.
    /// </summary>
    public void DisableTargetObject()
    {
        // Используем общий безопасный метод отключения.
        TryDisableObject("вызов из другого скрипта");
    }

    /// <summary>
    /// Сбрасывает внутреннее состояние для повторного тестирования.
    /// Ожидающее отключение отменяется.
    /// Сам отключённый объект этот метод не включает.
    /// </summary>
    public void ResetActivationState()
    {
        // Проверяем, запущена ли корутина задержки.
        if (activationCoroutine != null)
        {
            // Отменяем ожидающее отключение объекта.
            StopCoroutine(activationCoroutine);

            // Очищаем ссылку на корутину.
            activationCoroutine = null;
        }

        // Разрешаем скрипту сработать повторно.
        hasActivated = false;
    }
}