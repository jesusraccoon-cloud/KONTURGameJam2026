using System.Collections.Generic; // Подключаем списки List
using UnityEngine; // Подключаем Unity-классы

public class UniversalInventory : MonoBehaviour // Универсальный инвентарь сюжетных предметов игрока
{
    [Header("Collected Items")] // Блок собранных предметов
    public List<string> collectedItemIds = new List<string>(); // Список ID предметов, которые игрок уже подобрал

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = true; // Показывать сообщения в Console

    public void AddItem(string itemId) // Добавить предмет в инвентарь
    {
        if (string.IsNullOrEmpty(itemId)) return; // Если ID пустой — ничего не делаем

        if (HasItem(itemId)) return; // Если предмет уже есть — второй раз не добавляем

        collectedItemIds.Add(itemId); // Добавляем ID предмета в список

        if (showDebugLogs) // Если debug включён
        {
            Debug.Log("UniversalInventory: добавлен предмет: " + itemId); // Пишем сообщение в Console
        }
    }

    public bool HasItem(string itemId) // Проверить, есть ли предмет в инвентаре
    {
        if (string.IsNullOrEmpty(itemId)) return false; // Если ID пустой — считаем, что предмета нет

        return collectedItemIds.Contains(itemId); // Возвращаем true, если такой ID есть в списке
    }

    public void RemoveItem(string itemId) // Удалить предмет из инвентаря
    {
        if (string.IsNullOrEmpty(itemId)) return; // Если ID пустой — ничего не делаем

        if (!HasItem(itemId)) return; // Если предмета нет — ничего не делаем

        collectedItemIds.Remove(itemId); // Удаляем ID предмета из списка

        if (showDebugLogs) // Если debug включён
        {
            Debug.Log("UniversalInventory: удалён предмет: " + itemId); // Пишем сообщение в Console
        }
    }
}