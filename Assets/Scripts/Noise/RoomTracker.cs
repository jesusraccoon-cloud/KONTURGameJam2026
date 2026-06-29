using UnityEngine; // Подключаем Unity-классы

public class RoomTracker : MonoBehaviour // Скрипт, который отслеживает, в какой комнате находится объект
{
    [Header("Current Room")] // Заголовок в Inspector
    public RoomZone currentRoom; // Текущая комната, в которой находится объект

    [Header("Debug")] // Заголовок отладки
    public bool showDebugLogs = false; // Показывать ли сообщения в Console

    private void OnTriggerEnter(Collider other) // Срабатывает, когда объект входит в Trigger
    {
        RoomZone zone = other.GetComponentInParent<RoomZone>(); // Ищем RoomZone у триггера или его родителя

        if (zone == null) return; // Если это не зона комнаты — выходим

        currentRoom = zone; // Запоминаем новую текущую комнату

        if (showDebugLogs) // Если отладка включена
        {
            Debug.Log(gameObject.name + " вошёл в комнату: " + currentRoom.roomId); // Пишем в Console
        }
    }

    private void OnTriggerStay(Collider other) // Срабатывает, пока объект находится внутри Trigger
    {
        if (currentRoom != null) return; // Если комната уже есть — ничего не делаем

        RoomZone zone = other.GetComponentInParent<RoomZone>(); // Ищем RoomZone на случай, если объект уже был внутри при старте

        if (zone == null) return; // Если зоны нет — выходим

        currentRoom = zone; // Назначаем текущую комнату
    }

    private void OnTriggerExit(Collider other) // Срабатывает, когда объект выходит из Trigger
    {
        RoomZone zone = other.GetComponentInParent<RoomZone>(); // Ищем RoomZone у триггера

        if (zone == null) return; // Если это не комната — выходим

        if (currentRoom == zone) // Если мы вышли именно из текущей комнаты
        {
            currentRoom = null; // Временно очищаем комнату

            if (showDebugLogs) // Если отладка включена
            {
                Debug.Log(gameObject.name + " вышел из комнаты: " + zone.roomId); // Пишем в Console
            }
        }
    }

    public string GetCurrentRoomId() // Метод для получения ID текущей комнаты
    {
        if (currentRoom == null) return ""; // Если комнаты нет — возвращаем пустую строку

        return currentRoom.roomId; // Возвращаем ID комнаты
    }

    public bool HasRoom() // Проверка, есть ли сейчас комната
    {
        return currentRoom != null; // true если комната есть
    }
}