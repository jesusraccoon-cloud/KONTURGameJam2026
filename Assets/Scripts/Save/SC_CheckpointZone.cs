using UnityEngine; // Unity-классы

// Триггер-зона чекпоинта. Когда игрок входит — сохраняет чекпоинт с заданным номером.
// Удобно для «вход в квартиру» (чекпоинт 1). Для 4/6 и 6/6 лучше вешать SaveCheckpoint
// на события ApartmentFinalSequence (onHallDoorBreak / onFinalStart).
[RequireComponent(typeof(Collider))]
public class SC_CheckpointZone : MonoBehaviour
{
    [Header("Checkpoint")] // Блок чекпоинта
    public SC_SaveManager saveManager; // Менеджер сохранений

    [Min(1)] public int checkpointIndex = 1; // Номер чекпоинта (1 — вход в квартиру)

    public string playerTag = "Player"; // Тег игрока (или его родителя)

    public bool once = true; // Срабатывать один раз за сессию

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать логи

    private bool fired = false; // Уже сработал ли

    private void Reset() // При добавлении компонента
    {
        Collider c = GetComponent<Collider>(); // Берём коллайдер

        if (c != null) c.isTrigger = true; // Делаем триггером
    }

    private void Awake() // При создании
    {
        if (saveManager == null) saveManager = FindFirstObjectByType<SC_SaveManager>(); // Ищем менеджер, если не назначен
    }

    private void OnTriggerEnter(Collider other) // Кто-то вошёл в зону
    {
        if (once && fired) return; // Уже срабатывал — выходим

        if (!IsPlayer(other)) return; // Не игрок — выходим

        if (saveManager == null) // Нет менеджера
        {
            if (showDebugLogs) Debug.LogWarning(gameObject.name + ": SC_CheckpointZone — не найден SC_SaveManager"); // Предупреждение
            return; // Выходим
        }

        fired = true; // Помечаем срабатывание

        saveManager.SaveCheckpoint(checkpointIndex); // Сохраняем чекпоинт

        if (showDebugLogs) Debug.Log(gameObject.name + ": чекпоинт " + checkpointIndex + " сохранён по входу"); // Лог
    }

    private bool IsPlayer(Collider other) // Проверка, что это игрок (по тегу, включая родителей)
    {
        Transform t = other.transform; // Начинаем с коллайдера

        while (t != null) // Идём вверх по иерархии
        {
            if (t.CompareTag(playerTag)) return true; // Нашли тег игрока

            t = t.parent; // К родителю
        }

        return false; // Не игрок
    }
}
