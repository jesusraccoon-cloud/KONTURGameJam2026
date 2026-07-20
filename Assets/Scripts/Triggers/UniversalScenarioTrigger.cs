using UnityEngine; // Подключаем основные классы Unity.

public class UniversalScenarioTrigger : MonoBehaviour, IInteractable, IHitInteractable // Универсальный сценарный триггер с активацией через вход, E или удар.
{
    [Header("Activation Type")] // Раздел выбора способа активации.
    public bool activateOnEnter = false; // Разрешить активацию при входе игрока в Trigger.
    public bool activateOnInteract = true; // Разрешить активацию нажатием E.
    public bool activateOnHit = false; // Разрешить активацию ударом ЛКМ.

    [Header("Trigger Settings")] // Основные настройки триггера.
    public bool canActivate = true; // Может ли триггер сейчас сработать.
    public bool disableAfterActivation = true; // Отключать ли триггер после успешной активации.
    public string playerTag = "Player"; // Tag игрока для активации через вход.

    [Header("Apartment Final Sequence")] // Связь с режиссёром квартиры.
    public ApartmentFinalSequence apartmentFinalSequence; // Сюда назначается ApartmentFinalSequence нужной квартиры.

    [Header("Debug")] // Раздел отладки.
    public bool showDebugLogs = true; // Показывать ли сообщения триггера в Console.

    private Collider triggerCollider; // Collider этого объекта.
    private bool hasActivated = false; // Был ли триггер уже активирован.

    private void Awake() // Вызывается при загрузке объекта.
    {
        triggerCollider = GetComponent<Collider>(); // Получаем Collider с этого объекта.
    }

    private void OnTriggerEnter(Collider other) // Вызывается при входе объекта в Trigger.
    {
        if (!activateOnEnter) return; // Если активация через вход выключена, ничего не делаем.

        if (!other.CompareTag(playerTag)) return; // Если вошёл не игрок, ничего не делаем.

        TryActivate(); // Пытаемся активировать триггер.
    }

    public void Interact() // Вызывается PlayerInteractor при нажатии E.
    {
        if (!activateOnInteract) return; // Если активация через E выключена, ничего не делаем.

        TryActivate(); // Пытаемся активировать триггер.
    }

    public void Hit() // Вызывается PlayerInteractor при ударе ЛКМ.
    {
        if (!activateOnHit) return; // Если активация через удар выключена, ничего не делаем.

        TryActivate(); // Пытаемся активировать триггер.
    }

    public void TryActivate() // Главный метод попытки активации.
    {
        if (!canActivate) return; // Если триггер выключен, ничего не делаем.

        if (hasActivated && disableAfterActivation) return; // Если одноразовый триггер уже сработал, выходим.

        Activate(); // Выполняем успешную активацию.
    }

    private void Activate() // Выполняет фактическую активацию.
    {
        hasActivated = true; // Запоминаем успешную активацию.

        if (showDebugLogs) // Если включены отладочные сообщения.
        {
            Debug.Log("UniversalScenarioTrigger: триггер успешно активирован.", gameObject); // Показываем информацию об активации.
        }

        if (disableAfterActivation) // Если триггер должен отключиться после активации.
        {
            DisableTrigger(); // Отключаем триггер.
        }
    }

    public void EnableTrigger() // Публичный метод включения триггера.
    {
        canActivate = true; // Разрешаем активацию.

        hasActivated = false; // Сбрасываем прошлое использование.

        if (triggerCollider != null) // Если Collider найден.
        {
            triggerCollider.enabled = true; // Включаем Collider.
        }
    }

    public void DisableTrigger() // Публичный метод отключения триггера.
    {
        canActivate = false; // Запрещаем активацию.

        if (triggerCollider != null) // Если Collider найден.
        {
            triggerCollider.enabled = false; // Отключаем Collider.
        }
    }
}