using UnityEngine; // Подключаем Unity
using UnityEngine.SceneManagement; // Подключаем работу со сценами
using System.Collections.Generic; // Подключаем List

public class DebugScenarioController : MonoBehaviour // Debug-пульт сценария квартиры
{
    public enum DebugActionType // Тип debug-действия
    {
        SetPhaseFour, // Поставить 4/6 кассет
        SetPhaseSix, // Поставить 6/6 кассет
        TeleportToStart, // Телепорт на старт
        TeleportToKitchen, // Телепорт на кухню
        ResetScene, // Перезапуск сцены
        ToggleApartmentPower, // Включить/выключить питание квартиры
        ToggleMonster // Включить/выключить монстра
    }

    [System.Serializable] // Показываем класс в Inspector
    public class DebugAction // Одна строка debug-действия
    {
        public string name; // Название действия для удобства
        public DebugActionType actionType; // Что именно делает действие
        public KeyCode key = KeyCode.None; // Кнопка действия
        public bool enabled = true; // Включено ли действие
    }

    [Header("Debug Actions")] // Список debug-действий
    public List<DebugAction> actions = new List<DebugAction>(); // Все действия и кнопки

    [Header("Cassette System")] // Блок кассет
    public CassetteInventoryUI cassetteInventoryUI; // UI/логика кассет

    [Header("Final Systems")] // Блок финала
    public ApartmentFinalSequence finalSequence; // Главный режиссёр финала
    public MonsterAI monsterAI; // AI монстра

    [Header("Apartment Power")] // Блок питания квартиры
    public ApartmentPowerController apartmentPowerController; // Контроллер питания квартиры
    public TumblerSwitch[] tumblersToSync; // Тумблеры для обновления визуала

    [Header("Monster Debug")] // Блок debug-монстра
    public GameObject monsterObject; // Главный объект монстра
    public bool monsterEnabledOnStart = true; // Включён ли монстр при старте

    [Header("Player")] // Блок игрока
    public CharacterController playerController; // CharacterController игрока
    public Transform playerTransform; // Transform игрока
    public PlayerHideController playerHideController; // Система пряток игрока

    [Header("Teleport Points")] // Блок точек телепорта
    public Transform startTeleportPoint; // Точка старта
    public Transform kitchenTeleportPoint; // Точка кухни

    private bool monsterIsEnabled; // Текущее состояние монстра

    private void Reset() // Автозаполнение при добавлении скрипта
    {
        actions.Clear(); // Очищаем список действий

        AddAction("Фаза 4/6", DebugActionType.SetPhaseFour, KeyCode.F1); // Добавляем F1
        AddAction("Фаза 6/6", DebugActionType.SetPhaseSix, KeyCode.F2); // Добавляем F2
        AddAction("Телепорт на старт", DebugActionType.TeleportToStart, KeyCode.F3); // Добавляем F3
        AddAction("Телепорт на кухню", DebugActionType.TeleportToKitchen, KeyCode.F4); // Добавляем F4
        AddAction("Перезапуск сцены", DebugActionType.ResetScene, KeyCode.F5); // Добавляем F5
        AddAction("Питание квартиры", DebugActionType.ToggleApartmentPower, KeyCode.F6); // Добавляем F6
        AddAction("Монстр", DebugActionType.ToggleMonster, KeyCode.F7); // Добавляем F7
    }

    private void Start() // Старт сцены
    {
        if (monsterObject == null && monsterAI != null) // Если объект монстра не назначен, но AI есть
        {
            monsterObject = monsterAI.gameObject; // Берём объект со скриптом MonsterAI
        }

        monsterIsEnabled = monsterEnabledOnStart; // Запоминаем стартовое состояние монстра

        ApplyMonsterState(); // Применяем состояние монстра
    }

    private void Update() // Каждый кадр
    {
        for (int i = 0; i < actions.Count; i++) // Перебираем все debug-действия
        {
            if (actions[i] == null) continue; // Если строка пустая — пропускаем

            if (!actions[i].enabled) continue; // Если действие выключено — пропускаем

            if (actions[i].key == KeyCode.None) continue; // Если кнопка не назначена — пропускаем

            if (Input.GetKeyDown(actions[i].key)) // Если кнопка нажата
            {
                ExecuteAction(actions[i].actionType); // Выполняем действие
            }
        }
    }

    private void AddAction(string actionName, DebugActionType actionType, KeyCode key) // Добавить действие в список
    {
        DebugAction action = new DebugAction(); // Создаём новое действие

        action.name = actionName; // Записываем название
        action.actionType = actionType; // Записываем тип действия
        action.key = key; // Записываем кнопку
        action.enabled = true; // Включаем действие

        actions.Add(action); // Добавляем действие в список
    }

    private void ExecuteAction(DebugActionType actionType) // Выполнить debug-действие
    {
        switch (actionType) // Проверяем тип действия
        {
            case DebugActionType.SetPhaseFour: // Если выбрана фаза 4/6
                DebugSetPhaseFour(); // Ставим 4/6
                break; // Выходим из case

            case DebugActionType.SetPhaseSix: // Если выбрана фаза 6/6
                DebugSetPhaseSix(); // Ставим 6/6
                break; // Выходим из case

            case DebugActionType.TeleportToStart: // Если выбран старт
                TeleportPlayer(startTeleportPoint, "старт"); // Телепортируем на старт
                break; // Выходим из case

            case DebugActionType.TeleportToKitchen: // Если выбрана кухня
                TeleportPlayer(kitchenTeleportPoint, "кухня"); // Телепортируем на кухню
                break; // Выходим из case

            case DebugActionType.ResetScene: // Если выбран reset
                ResetScene(); // Перезапускаем сцену
                break; // Выходим из case

            case DebugActionType.ToggleApartmentPower: // Если выбрано питание
                ToggleApartmentPower(); // Переключаем питание
                break; // Выходим из case

            case DebugActionType.ToggleMonster: // Если выбран монстр
                ToggleMonster(); // Переключаем монстра
                break; // Выходим из case
        }
    }

    private void DebugSetPhaseFour() // Debug-фаза 4/6
    {
        if (cassetteInventoryUI != null) // Если система кассет назначена
        {
            cassetteInventoryUI.SetCassetteCountDebug(4); // Ставим 4 кассеты
        }

        if (monsterAI != null) // Если монстр назначен
        {
            monsterIsEnabled = true; // Включаем состояние монстра
            ApplyMonsterState(); // Применяем включение монстра
            monsterAI.ActivateMonster(); // Активируем AI монстра
        }

        Debug.Log("DEBUG: включена фаза 4/6"); // Лог в Console
    }

    private void DebugSetPhaseSix() // Debug-фаза 6/6
    {
        if (cassetteInventoryUI != null) // Если система кассет назначена
        {
            cassetteInventoryUI.SetCassetteCountDebug(6); // Ставим 6 кассет
        }

        if (monsterObject != null) // Если объект монстра назначен
        {
            monsterIsEnabled = true; // Включаем состояние монстра
            ApplyMonsterState(); // Применяем включение монстра
        }

        Debug.Log("DEBUG: включена фаза 6/6"); // Лог в Console
    }

    private void ToggleApartmentPower() // Переключить питание квартиры
    {
        if (apartmentPowerController == null) // Если контроллер питания не назначен
        {
            Debug.LogWarning("DEBUG: ApartmentPowerController не назначен"); // Warning
            return; // Выходим
        }

        apartmentPowerController.TogglePower(); // Переключаем питание квартиры

        SyncTumblers(); // Обновляем визуал тумблеров

        Debug.Log("DEBUG: питание квартиры переключено"); // Лог в Console
    }

    private void SyncTumblers() // Синхронизация тумблеров
    {
        if (apartmentPowerController == null) return; // Если питания нет — выходим

        if (tumblersToSync == null) return; // Если массива нет — выходим

        for (int i = 0; i < tumblersToSync.Length; i++) // Перебираем тумблеры
        {
            if (tumblersToSync[i] == null) continue; // Если тумблер пустой — пропускаем

            tumblersToSync[i].SetState(apartmentPowerController.isPoweredOn); // Ставим визуал по реальному питанию
        }
    }

    private void ToggleMonster() // Переключить монстра
    {
        monsterIsEnabled = !monsterIsEnabled; // Инвертируем состояние

        ApplyMonsterState(); // Применяем состояние

        Debug.Log("DEBUG: монстр включен: " + monsterIsEnabled); // Лог в Console
    }

    private void ApplyMonsterState() // Применить состояние монстра
    {
        if (monsterObject == null) // Если объект монстра не назначен
        {
            Debug.LogWarning("DEBUG: MonsterObject не назначен"); // Warning
            return; // Выходим
        }

        monsterObject.SetActive(monsterIsEnabled); // Включаем или выключаем монстра
    }

    private void TeleportPlayer(Transform targetPoint, string pointName) // Телепорт игрока
    {
        if (playerTransform == null || targetPoint == null) // Если игрок или точка не назначены
        {
            Debug.LogWarning("DEBUG: PlayerTransform или точка телепорта не назначены"); // Warning
            return; // Выходим
        }

        if (playerHideController != null) // Если система пряток назначена
        {
            playerHideController.isHidden = false; // Сбрасываем состояние пряток
        }

        if (playerController != null) // Если CharacterController назначен
        {
            playerController.enabled = false; // Отключаем CharacterController
        }

        playerTransform.position = targetPoint.position; // Переносим игрока

        playerTransform.rotation = targetPoint.rotation; // Поворачиваем игрока

        if (playerController != null) // Если CharacterController назначен
        {
            playerController.enabled = true; // Включаем CharacterController
        }

        Debug.Log("DEBUG: игрок телепортирован: " + pointName); // Лог в Console
    }

    private void ResetScene() // Перезапуск сцены
    {
        Time.timeScale = 1f; // Возвращаем нормальное время

        Cursor.lockState = CursorLockMode.Locked; // Блокируем курсор

        Cursor.visible = false; // Прячем курсор

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Перезагружаем текущую сцену

        Debug.Log("DEBUG: сцена перезапущена"); // Лог в Console
    }
}