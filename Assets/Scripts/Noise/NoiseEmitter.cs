using UnityEngine; // Подключаем Unity-классы

public class NoiseEmitter : MonoBehaviour // Универсальный источник шума для дверей, окон, шкафов, предметов
{
    [Header("References")] // Блок ссылок в Inspector
    public NoiseManager noiseManager; // NoiseManager конкретной квартиры

    public RoomTracker sourceRoomTracker; // RoomTracker нужен для движущихся объектов

    public RoomZone fixedRoom; // Основная фиксированная комната шума

    public RoomZone secondFixedRoom; // Вторая комната шума, например дверь между двумя комнатами

    public NoiseMeterUI noiseMeterUI; // UI-шкала шума игрока

    [Header("Noise Settings")] // Блок настроек шума
    [Range(1, 10)] public int noisePower = 5; // Базовая сила шума

    public bool useSecondRoom = false; // Использовать ли вторую комнату для шума

    public bool showNoiseOnUI = true; // Показывать ли этот шум на UI игрока

    public bool mixNoiseOnUI = true; // Смешивать ли этот шум с текущим шумом UI

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать ли Debug.Log

    public void EmitNoise() // Создать шум с базовой силой
    {
        EmitNoise(noisePower); // Вызываем шум с базовой силой
    }

    public void EmitNoise(int customPower) // Создать шум с заданной силой
    {
        if (noiseManager == null) // Если NoiseManager не назначен
        {
            Debug.LogWarning(gameObject.name + ": не назначен NoiseManager"); // Пишем предупреждение

            return; // Выходим
        }

        customPower = Mathf.Clamp(customPower, 1, 10); // Ограничиваем шум от 1 до 10

        ShowNoiseOnPlayerUI(customPower); // Показываем шум на UI игрока

        RoomZone mainRoom = GetMainRoom(); // Получаем основную комнату шума

        EmitNoiseToRoom(customPower, mainRoom); // Отправляем шум в основную комнату

        if (useSecondRoom && secondFixedRoom != null) // Если включена вторая комната и она назначена
        {
            EmitNoiseToRoom(customPower, secondFixedRoom); // Отправляем шум во вторую комнату
        }
    }

    private void ShowNoiseOnPlayerUI(int customPower) // Метод показа шума на UI игрока
    {
        if (!showNoiseOnUI) return; // Если показ шума выключен — выходим

        if (noiseMeterUI == null) return; // Если UI не назначен — выходим

        if (mixNoiseOnUI) // Если нужно смешивать шумы
        {
            noiseMeterUI.AddNoise(customPower); // Добавляем шум поверх текущего значения
        }
        else // Если смешивание не нужно
        {
            noiseMeterUI.SetNoise(customPower); // Просто выставляем шум напрямую
        }
    }

    private void EmitNoiseToRoom(int customPower, RoomZone room) // Отправить шум в конкретную комнату
    {
        if (showDebugLogs) // Если отладка включена
        {
            string roomName = room != null ? room.roomId : "Unknown"; // Получаем название комнаты

            Debug.Log(gameObject.name + " создал шум " + customPower + " в комнате: " + roomName); // Пишем лог
        }

        noiseManager.MakeNoise(transform.position, customPower, room); // Отправляем шум в NoiseManager
    }

    private RoomZone GetMainRoom() // Получить основную комнату источника
    {
        if (sourceRoomTracker != null && sourceRoomTracker.currentRoom != null) // Если есть RoomTracker и он знает комнату
        {
            return sourceRoomTracker.currentRoom; // Возвращаем комнату из RoomTracker
        }

        if (fixedRoom != null) // Если назначена фиксированная комната
        {
            return fixedRoom; // Возвращаем fixedRoom
        }

        RoomZone parentRoom = GetComponentInParent<RoomZone>(); // Ищем RoomZone в родителях

        if (parentRoom != null) // Если нашли RoomZone
        {
            return parentRoom; // Возвращаем найденную комнату
        }

        return null; // Если ничего не нашли — возвращаем null
    }
}