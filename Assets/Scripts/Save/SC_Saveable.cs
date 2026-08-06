using UnityEngine; // Unity-классы
using UnityEngine.Events; // UnityEvent

// Вешается на объект, который должен «запоминаться» как использованный
// (звуковой триггер, интерактив, кассета и т.п.), чтобы после загрузки он не срабатывал снова.
public class SC_Saveable : MonoBehaviour
{
    // Что автоматически сделать при загрузке, если объект уже был использован.
    public enum RestoreAction
    {
        DisableGameObject, // Спрятать объект целиком (кассеты, разовые предметы) — самый частый случай
        DisableColliders,  // Только выключить коллайдеры (триггер остаётся в сцене, но больше не срабатывает)
        EventOnly          // Ничего автоматически — только ручной onRestoreUsed
    }

    [Tooltip("Уникальный ID (генерируется автоматически). Если скопировал объект — нажми 'New Id'.")]
    public string id; // Стабильный идентификатор между сессиями

    [Tooltip("Что сделать АВТОМАТИЧЕСКИ при загрузке, если объект был использован.\n" +
             "Disable Game Object — спрятать (кассеты, предметы).\n" +
             "Disable Colliders — выключить коллайдеры (звуковые/сценарные триггеры).\n" +
             "Event Only — ничего, всё вручную через On Restore Used.")]
    public RestoreAction restoreAction = RestoreAction.DisableGameObject; // Встроенная реакция — НЕ нужно вручную вешать SetActive на каждый объект

    [Tooltip("Дополнительная реакция при загрузке (в ДОПОЛНЕНИЕ к Restore Action). Обычно можно оставить пустым.")]
    public UnityEvent onRestoreUsed; // Ручная реакция для особых случаев (задать стадию пазла и т.п.)

    [Tooltip("Писать лог при MarkUsed / успешном восстановлении (для отладки).")]
    public bool showDebugLogs = false; // По умолчанию тихо, чтобы не заваливать консоль

    private void Reset() // При добавлении компонента в редакторе
    {
        GenerateIdIfEmpty(); // Генерируем ID
    }

    private void OnValidate() // При изменении в инспекторе
    {
        GenerateIdIfEmpty(); // Гарантируем наличие ID
    }

    private void GenerateIdIfEmpty() // Сгенерировать ID, если пусто
    {
        if (string.IsNullOrEmpty(id)) id = System.Guid.NewGuid().ToString(); // Новый GUID
    }

    [ContextMenu("New Id")] // Пункт в контекстном меню компонента — на случай дублей после копирования
    private void NewId()
    {
        id = System.Guid.NewGuid().ToString(); // Принудительно новый ID
    }

    // Вызывай, когда объект «использован» (интеракция/активация). Вешается на UnityEvent объекта.
    public void MarkUsed()
    {
        SC_SaveSystem.MarkUsed(id); // Запоминаем в сохранении

        if (showDebugLogs) Debug.Log($"[SAVEABLE] MarkUsed: {gameObject.name} id={id}"); // Отладка
    }

    // Вызывается менеджером при загрузке: если объект был использован — применяем сохранённое состояние.
    public void RestoreIfUsed()
    {
        if (!SC_SaveSystem.IsUsed(id)) return; // Не использован — ничего не делаем (и не спамим в лог)

        if (showDebugLogs) Debug.Log($"[SAVEABLE] Restore (used): {gameObject.name} id={id} action={restoreAction}"); // Отладка

        onRestoreUsed.Invoke(); // Сначала — ручная реакция (пока объект ещё активен)

        switch (restoreAction) // Затем — встроенное действие
        {
            case RestoreAction.DisableGameObject: // Спрятать объект
                gameObject.SetActive(false); // Выключаем целиком
                break;

            case RestoreAction.DisableColliders: // Выключить коллайдеры
                Collider[] cols = GetComponentsInChildren<Collider>(true); // Все коллайдеры (включая выключенные)
                for (int c = 0; c < cols.Length; c++) cols[c].enabled = false; // Гасим каждый
                break;

            // EventOnly — только onRestoreUsed, ничего больше
        }
    }
}
