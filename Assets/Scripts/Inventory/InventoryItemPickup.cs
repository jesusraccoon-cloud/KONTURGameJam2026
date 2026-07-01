using UnityEngine; // Подключаем Unity-классы

public class InventoryItemPickup : MonoBehaviour, IInteractable // Универсальный подбираемый предмет
{
    [Header("Item Settings")] // Блок настроек предмета
    public string itemId = "ClockKey"; // Уникальный ID предмета, например ClockKey

    [Header("References")] // Блок ссылок
    public UniversalInventory inventory; // Универсальный инвентарь игрока

    [Header("Objects")] // Блок объектов
    public GameObject objectToDisableAfterPickup; // Какой объект выключить после подбора

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = true; // Показывать сообщения в Console

    private bool isPickedUp = false; // Был ли предмет уже подобран

    public void Interact() // Вызывается PlayerInteractor при нажатии E
    {
        if (isPickedUp) return; // Если предмет уже подобран — выходим

        if (inventory == null) // Если инвентарь не назначен
        {
            Debug.LogWarning(gameObject.name + ": не назначен UniversalInventory"); // Пишем предупреждение

            return; // Выходим
        }

        inventory.AddItem(itemId); // Добавляем предмет в универсальный инвентарь

        isPickedUp = true; // Запоминаем, что предмет подобран

        if (showDebugLogs) // Если debug включён
        {
            Debug.Log("InventoryItemPickup: игрок подобрал предмет: " + itemId); // Пишем сообщение
        }

        if (objectToDisableAfterPickup != null) // Если объект для выключения назначен
        {
            objectToDisableAfterPickup.SetActive(false); // Выключаем назначенный объект
        }
        else // Если объект не назначен
        {
            gameObject.SetActive(false); // Выключаем сам объект
        }
    }
}