using UnityEngine; // Подключаем Unity

public class NoiseManager : MonoBehaviour // Главный менеджер шума для одной квартиры
{
    [System.Serializable] // Позволяет показывать этот класс в Inspector
    public class RoomConnection // Одна связь слышимости между двумя комнатами
    {
        public string fromRoom; // Комната, откуда идёт шум

        public string toRoom; // Комната, где находится монстр

        [Range(0f, 1f)] public float volumeMultiplier = 1f; // Насколько звук ослабляется между комнатами
    }

    [Header("Main References")] // Заголовок ссылок
    public MonsterAI monster; // Ссылка на MonsterAI этой квартиры

    public RoomTracker monsterRoomTracker; // Ссылка на RoomTracker монстра

    public ApartmentFinalSequence finalSequence; // Ссылка на сценарий квартиры для счётчика тревоги 3/3

    [Header("Unknown Room Noise")] // Настройки шума вне комнат
    public bool ignoreNoiseWithoutRoom = true; // Игнорировать шум, если источник не находится ни в одной RoomZone

    [Range(0f, 1f)] public float unknownRoomMultiplier = 0f; // Множитель шума вне RoomZone, если игнорирование выключено

    [Header("Room Connections")] // Настройки связей комнат
    public RoomConnection[] roomConnections; // Массив связей между комнатами

    [Header("Debug")] // Отладка
    public bool showDebugLogs = true; // Показывать ли сообщения в Console

    public void MakeNoise(Vector3 noisePosition, int noisePower, RoomZone sourceRoom) // Создать шум в конкретной позиции
    {
        noisePower = Mathf.Clamp(noisePower, 1, 10); // Ограничиваем силу шума от 1 до 10

        if (monster == null) // Если монстр не назначен
        {
            Debug.LogWarning("NoiseManager: не назначен MonsterAI"); // Предупреждаем о проблеме

            return; // Выходим
        }

        if (monsterRoomTracker == null) // Если RoomTracker монстра не назначен
        {
            Debug.LogWarning("NoiseManager: не назначен RoomTracker монстра"); // Предупреждаем о проблеме

            return; // Выходим
        }

        RoomZone monsterRoom = monsterRoomTracker.currentRoom; // Берём текущую комнату монстра

        if (monsterRoom == null) // Если монстр сейчас не находится в RoomZone
        {
            if (showDebugLogs) // Если debug включён
            {
                Debug.Log("NoiseManager: монстр вне RoomZone, шум игнорируется"); // Пишем понятный лог
            }

            return; // Не даём монстру реагировать на шум без комнаты
        }

        if (sourceRoom == null) // Если источник шума вне RoomZone
        {
            HandleNoiseWithoutSourceRoom(noisePosition, noisePower, monsterRoom); // Обрабатываем шум вне комнаты отдельным правилом

            return; // Выходим
        }

        float multiplier = GetVolumeMultiplier(sourceRoom.roomId, monsterRoom.roomId); // Получаем множитель слышимости между комнатами

        int finalNoise = CalculateFinalNoise(noisePower, multiplier); // Считаем итоговую силу шума

        if (showDebugLogs) // Если debug включён
        {
            Debug.Log("Шум: " + noisePower + " | из: " + sourceRoom.roomId + " | монстр в: " + monsterRoom.roomId + " | множитель: " + multiplier + " | итог: " + finalNoise); // Пишем подробный лог
        }

        TrySendNoiseToMonster(noisePosition, finalNoise); // Передаём итоговый шум монстру и сценарию
    }

    public void MakeNoiseFromTracker(Vector3 noisePosition, int noisePower, RoomTracker sourceTracker) // Создать шум от объекта с RoomTracker
    {
        RoomZone sourceRoom = null; // Создаём переменную комнаты источника

        if (sourceTracker != null) // Если трекер источника назначен
        {
            sourceRoom = sourceTracker.currentRoom; // Берём текущую комнату источника
        }

        MakeNoise(noisePosition, noisePower, sourceRoom); // Вызываем основной метод шума
    }

    private void HandleNoiseWithoutSourceRoom(Vector3 noisePosition, int noisePower, RoomZone monsterRoom) // Обработка шума вне RoomZone
    {
        if (ignoreNoiseWithoutRoom) // Если шум вне комнат нужно игнорировать
        {
            if (showDebugLogs) // Если debug включён
            {
                Debug.Log("NoiseManager: источник шума вне RoomZone, шум игнорируется. Сила: " + noisePower); // Пишем лог
            }

            return; // Полностью игнорируем шум
        }

        int finalNoise = CalculateFinalNoise(noisePower, unknownRoomMultiplier); // Ослабляем шум через отдельный множитель

        if (showDebugLogs) // Если debug включён
        {
            Debug.Log("Шум вне RoomZone: " + noisePower + " | монстр в: " + monsterRoom.roomId + " | множитель вне комнат: " + unknownRoomMultiplier + " | итог: " + finalNoise); // Пишем подробный лог
        }

        TrySendNoiseToMonster(noisePosition, finalNoise); // Передаём ослабленный шум монстру и сценарию
    }

    private int CalculateFinalNoise(int noisePower, float multiplier) // Посчитать итоговый шум
    {
        float finalNoiseFloat = noisePower * multiplier; // Умножаем исходный шум на множитель

        int finalNoise = Mathf.RoundToInt(finalNoiseFloat); // Округляем до целого

        finalNoise = Mathf.Clamp(finalNoise, 0, 10); // Ограничиваем результат от 0 до 10

        return finalNoise; // Возвращаем итоговый шум
    }

    private void TrySendNoiseToMonster(Vector3 noisePosition, int finalNoise) // Проверка реакции монстра на итоговый шум
    {
        finalNoise = Mathf.Clamp(finalNoise, 0, 10); // Ограничиваем итоговый шум от 0 до 10

        if (finalNoise <= 3) // Если шум 0-3
        {
            if (showDebugLogs) // Если debug включён
            {
                Debug.Log("Монстр игнорирует шум. Итоговый шум: " + finalNoise); // Пишем лог
            }

            return; // Ничего не делаем
        }

        if (finalSequence != null) // Если сценарий квартиры назначен
        {
            finalSequence.RegisterNoiseReactionForEarlyEvent(finalNoise); // Сообщаем сценарию, что монстр встревожен шумом
        }

        if (showDebugLogs) // Если debug включён
        {
            Debug.Log("Монстр реагирует на шум. Итоговый шум: " + finalNoise); // Пишем лог
        }

        monster.ReactToNoise(noisePosition, finalNoise); // Передаём монстру позицию и силу шума
    }

    private float GetVolumeMultiplier(string fromRoom, string toRoom) // Получить множитель между комнатами
    {
        if (fromRoom == toRoom) return 1f; // Если шум и монстр в одной комнате — звук полный

        foreach (RoomConnection connection in roomConnections) // Перебираем все связи
        {
            if (connection == null) continue; // Если связь пустая — пропускаем

            bool directMatch = connection.fromRoom == fromRoom && connection.toRoom == toRoom; // Проверяем прямую связь

            bool reverseMatch = connection.fromRoom == toRoom && connection.toRoom == fromRoom; // Проверяем обратную связь

            if (directMatch || reverseMatch) // Если связь найдена в любую сторону
            {
                return connection.volumeMultiplier; // Возвращаем множитель связи
            }
        }

        return 0f; // Если связи между комнатами нет — звук не проходит
    }
}