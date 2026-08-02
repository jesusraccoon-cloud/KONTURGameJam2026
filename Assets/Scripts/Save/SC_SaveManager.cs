using System.Collections; // Корутина
using UnityEngine; // Unity-классы
using UnityEngine.Events; // UnityEvent
using UnityEngine.SceneManagement; // Загрузка сцен
using StarterAssets; // FirstPersonController

// Менеджер сохранений в сцене. Сохраняет чекпоинты, перезагружает сцену и применяет
// сохранённое состояние: спавн игрока на точке чекпоинта -> восстановление стадии ->
// отметка «использованных» объектов. Данные живут в SC_SaveSystem (static + диск).
public class SC_SaveManager : MonoBehaviour
{
    [System.Serializable]
    public class CheckpointConfig
    {
        public string label; // Подпись (Вход в квартиру / 4 из 6 / 6 из 6) — для UI

        public Transform spawnPoint; // НЕОБЯЗАТЕЛЬНЫЙ override точки спавна. Пусто — используется сохранённая позиция игрока (авто-запоминание)

        public UnityEvent onRestore; // Восстановить стадию: вызвать методы сиквенса, задать счётчик кассет и т.п.
    }

    [Header("Player")] // Блок игрока
    public Transform player; // Корень игрока (PlayerCapsule) для телепорта

    public CharacterController playerController; // CharacterController игрока (отключаем на миг телепорта)

    public FirstPersonController firstPersonController; // Контроллер игрока (синхронизируем поворот после телепорта, чтобы камера не «отскочила»)

    [Header("Monster")] // Блок монстра
    public MonsterAI monsterAI; // Монстр (для сохранения/восстановления позиции)

    public bool restoreMonsterState = true; // Восстанавливать позицию монстра при загрузке

    [Header("Checkpoints (Element 0 = чекпоинт 1)")] // Массив чекпоинтов: вход / 4-6 / 6-6
    public CheckpointConfig[] checkpoints; // Настройка каждого чекпоинта

    [Header("New Game")] // Блок новой игры
    public string gameplaySceneName = ""; // Имя игровой сцены (для кнопки «Новая игра»). Пусто — текущая сцена

    [Header("Debug")] // Отладка
    public bool showDebugLogs = true; // Показывать логи

    public bool enableDebugReloadKey = true; // Дебаг-клавиша для загрузки последнего сохранения

    public KeyCode debugReloadKey = KeyCode.N; // Какая клавиша грузит последний чекпоинт

    private void Start() // При старте сцены
    {
        if (SC_SaveSystem.ApplyOnNextLoad) // Если пришли сюда через загрузку сохранения
        {
            SC_SaveSystem.ApplyOnNextLoad = false; // Сбрасываем флаг
            StartCoroutine(ApplyAfterFrame()); // Применяем через кадр (даём сцене проиниться)
        }
    }

    private IEnumerator ApplyAfterFrame() // Применить сохранение через кадр
    {
        yield return null; // Ждём один кадр, чтобы все Awake/Start отработали
        ApplyLoadedState(); // Применяем
    }

    private void Update() // Каждый кадр — дебаг-клавиша
    {
        if (enableDebugReloadKey && Input.GetKeyDown(debugReloadKey)) // Нажата дебаг-клавиша
        {
            if (showDebugLogs) Debug.Log("SaveManager: DEBUG — загрузка последнего сохранения (" + debugReloadKey + ")"); // Лог
            ReloadLastCheckpoint(); // Грузим последний чекпоинт
        }
    }

    // === Публичные методы (вешай на UnityEvent'ы чекпоинтов / кнопки UI) ===

    // Сохранить чекпоинт (1 — вход, 2 — 4/6, 3 — 6/6). Вешается на onPlayerEntered триггера входа,
    // а также на onHallDoorBreak (2) и onFinalStart (3) у ApartmentFinalSequence.
    public void SaveCheckpoint(int checkpointIndex)
    {
        string label = (checkpoints != null && checkpointIndex >= 1 && checkpointIndex <= checkpoints.Length)
            ? checkpoints[checkpointIndex - 1].label // Берём подпись из конфига
            : ("Checkpoint " + checkpointIndex); // Или дефолтную

        SC_SaveData d = SC_SaveSystem.Current; // Текущее сохранение

        if (player != null) // Запоминаем позицию игрока
        {
            d.playerPos = player.position; // Позиция
            d.playerYaw = player.eulerAngles.y; // Поворот
        }

        if (monsterAI != null) // Запоминаем состояние монстра
        {
            d.monsterActive = monsterAI.isActivated; // Активен ли
            d.monsterPos = monsterAI.transform.position; // Позиция
            d.monsterYaw = monsterAI.transform.eulerAngles.y; // Поворот
        }

        SC_SaveSystem.SaveCheckpoint(checkpointIndex, label, SceneManager.GetActiveScene().name); // Финализируем + пишем на диск

        if (showDebugLogs) Debug.Log($"SaveManager: чекпоинт {checkpointIndex} ('{label}'), player={d.playerPos}, monsterActive={d.monsterActive}"); // Лог
    }

    // Перезагрузка сцены на последний чекпоинт (смерть игрока / кнопка «Загрузить»).
    public void ReloadLastCheckpoint()
    {
        if (SC_SaveSystem.Current.checkpoint <= 0) // Если ещё ни одного чекпоинта не было
        {
            if (showDebugLogs) Debug.Log("SaveManager: нет чекпоинта — перезагрузка с начала"); // Лог
        }

        SC_SaveSystem.ApplyOnNextLoad = true; // Просим применить сохранение после загрузки
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Перезагружаем текущую сцену
    }

    // Continue из меню: читаем сейв с диска и грузим сохранённую сцену.
    public void ContinueFromDisk()
    {
        if (!SC_SaveSystem.LoadDisk()) // Пробуем прочитать диск
        {
            if (showDebugLogs) Debug.Log("SaveManager: сейва на диске нет"); // Лог
            return; // Выходим
        }

        SC_SaveSystem.ApplyOnNextLoad = true; // Применить после загрузки

        string scene = SC_SaveSystem.Current.sceneName; // Сцена из сейва
        SceneManager.LoadScene(string.IsNullOrEmpty(scene) ? SceneManager.GetActiveScene().name : scene); // Грузим
    }

    // Новая игра: удаляем сейв и грузим игровую сцену с нуля.
    public void NewGame()
    {
        SC_SaveSystem.DeleteSave(); // Стираем сохранение
        SC_SaveSystem.ApplyOnNextLoad = false; // Ничего не применяем
        SceneManager.LoadScene(string.IsNullOrEmpty(gameplaySceneName) ? SceneManager.GetActiveScene().name : gameplaySceneName); // Грузим сцену
    }

    public bool HasSave() => SC_SaveSystem.HasDiskSave; // Есть ли сейв (для активации кнопки Continue)

    // === Применение сохранённого состояния ===

    private void ApplyLoadedState()
    {
        SC_SaveData data = SC_SaveSystem.Current; // Текущее сохранение

        if (data == null || data.checkpoint <= 0) // Нечего применять
        {
            if (showDebugLogs) Debug.LogWarning("SaveManager: применять нечего — чекпоинт не сохранён (checkpoint<=0).");
            return; // Выходим
        }

        int i = data.checkpoint; // Номер чекпоинта

        CheckpointConfig cp = (checkpoints != null && i >= 1 && i <= checkpoints.Length) ? checkpoints[i - 1] : null; // Конфиг чекпоинта (если есть)

        if (cp != null && cp.spawnPoint != null) // Если задан override-точка
        {
            TeleportPlayer(cp.spawnPoint.position, cp.spawnPoint.eulerAngles.y); // Спавн на фиксированной точке
        }
        else // Иначе — сохранённая позиция игрока (авто-запоминание)
        {
            TeleportPlayer(data.playerPos, data.playerYaw); // Спавн там, где игрок был на чекпоинте
        }

        if (cp != null) cp.onRestore.Invoke(); // Восстанавливаем стадию (методы сиквенса, счётчик кассет и т.п.)

        SC_Saveable[] saveables = FindObjectsByType<SC_Saveable>(FindObjectsInactive.Include, FindObjectsSortMode.None); // Все Saveable в сцене
        for (int s = 0; s < saveables.Length; s++) saveables[s].RestoreIfUsed(); // Отмечаем использованные

        RestoreMonster(data); // Возвращаем монстра на сохранённую позицию (после восстановления стадии)

        if (showDebugLogs) Debug.Log($"SaveManager: применён чекпоинт {i} ('{data.checkpointLabel}'), использованных объектов: {data.used.Count}"); // Лог
    }

    private void RestoreMonster(SC_SaveData data) // Возврат монстра на сохранённую позицию
    {
        if (!restoreMonsterState || monsterAI == null) return; // Выключено или монстра нет

        if (!data.monsterActive) return; // На чекпоинте монстр был неактивен — не трогаем

        if (!monsterAI.gameObject.activeInHierarchy) return; // Монстр ещё не включён сценой — пропускаем

        Vector3 pos = data.monsterPos; // Сохранённая позиция

        var agent = monsterAI.movement != null ? monsterAI.movement.agent : null; // NavMeshAgent монстра

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) // Если агент готов
        {
            agent.Warp(pos); // Корректный перенос по NavMesh
        }
        else // Запасной вариант
        {
            monsterAI.transform.position = pos; // Ставим позицию напрямую
        }

        monsterAI.transform.rotation = Quaternion.Euler(0f, data.monsterYaw, 0f); // Поворот монстра
    }

    private void TeleportPlayer(Vector3 pos, float yaw) // Телепорт игрока (с учётом CharacterController и поворота камеры)
    {
        if (player == null) return; // Нет игрока — выходим

        bool ccWasEnabled = playerController != null && playerController.enabled; // Был ли включён CC

        if (ccWasEnabled) playerController.enabled = false; // CharacterController мешает прямой установке позиции

        player.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f)); // Ставим позицию и поворот по Y

        if (ccWasEnabled) playerController.enabled = true; // Возвращаем CC

        if (firstPersonController != null) firstPersonController.SetYaw(yaw); // Синхронизируем внутренний yaw контроллера (иначе камера отскочит)
    }
}
