using System.IO; // Работа с файлом сейва
using UnityEngine; // JsonUtility, Debug, Application

// Ядро сохранений: держит текущее сохранение в static-памяти (переживает перезагрузку сцены)
// и дублирует его на диск (JSON, переживает перезапуск игры).
public static class SC_SaveSystem
{
    private static SC_SaveData current; // Текущее (живое) сохранение

    public static bool ApplyOnNextLoad; // Флаг: применить сохранение при следующей загрузке сцены

    private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json"); // Путь к файлу сейва

    public static SC_SaveData Current // Текущее сохранение (создаётся пустым при первом обращении)
    {
        get { if (current == null) current = new SC_SaveData(); return current; }
    }

    public static bool HasDiskSave => File.Exists(FilePath); // Есть ли сейв на диске (для кнопки Continue)

    public static void MarkUsed(string id) // Пометить объект использованным
    {
        if (string.IsNullOrEmpty(id)) return; // Пустой ID игнорируем

        if (!Current.used.Contains(id)) Current.used.Add(id); // Добавляем, если ещё нет
    }

    public static bool IsUsed(string id) // Использован ли объект
    {
        return current != null && !string.IsNullOrEmpty(id) && current.used.Contains(id); // Есть ли в списке
    }

    // === Числовое состояние объектов (стадия/флаги/открыта-закрыта) ===

    public static void SetState(string id, int value) // Сохранить состояние объекта
    {
        if (string.IsNullOrEmpty(id)) return; // Пустой ID игнорируем

        int idx = Current.stateKeys.IndexOf(id); // Ищем существующую запись
        if (idx >= 0) Current.stateValues[idx] = value; // Обновляем значение
        else { Current.stateKeys.Add(id); Current.stateValues.Add(value); } // Или добавляем новую
    }

    public static bool TryGetState(string id, out int value) // Прочитать сохранённое состояние
    {
        value = 0; // Значение по умолчанию

        if (current == null || string.IsNullOrEmpty(id)) return false; // Нечего читать

        int idx = current.stateKeys.IndexOf(id); // Ищем запись
        if (idx < 0) return false; // Нет такой записи

        value = current.stateValues[idx]; // Возвращаем сохранённое значение
        return true; // Есть сохранённое состояние
    }

    // Финализировать чекпоинт: стадия/подпись/сцена/время + запись на диск.
    // Позиции игрока/монстра и список used менеджер выставляет в Current заранее.
    public static void SaveCheckpoint(int checkpoint, string label, string sceneName)
    {
        Current.checkpoint = checkpoint; // Номер чекпоинта
        Current.checkpointLabel = label; // Подпись
        Current.sceneName = sceneName; // Сцена
        Current.savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"); // Время

        WriteDisk(); // Дублируем на диск
    }

    public static void WriteDisk() // Записать текущее сохранение на диск
    {
        try { File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true)); } // JSON с отступами
        catch (System.Exception e) { Debug.LogWarning("SC_SaveSystem: не удалось записать сейв: " + e.Message); } // Ошибку не роняем
    }

    public static bool LoadDisk() // Прочитать сохранение с диска в память
    {
        if (!HasDiskSave) return false; // Нет файла — нечего грузить

        try
        {
            current = JsonUtility.FromJson<SC_SaveData>(File.ReadAllText(FilePath)); // Читаем JSON
            if (current != null && current.used == null) current.used = new System.Collections.Generic.List<string>(); // Защита от null-списка
            if (current != null && current.stateKeys == null) current.stateKeys = new System.Collections.Generic.List<string>(); // Защита от null-ключей состояния
            if (current != null && current.stateValues == null) current.stateValues = new System.Collections.Generic.List<int>(); // Защита от null-значений состояния
            return current != null; // Успех
        }
        catch (System.Exception e) { Debug.LogWarning("SC_SaveSystem: не удалось прочитать сейв: " + e.Message); return false; } // Ошибку не роняем
    }

    public static void DeleteSave() // Удалить сохранение (Новая игра)
    {
        current = null; // Сбрасываем память
        try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { } // Удаляем файл
    }
}
