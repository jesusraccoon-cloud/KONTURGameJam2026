using UnityEngine; // Подключаем Unity-классы
using UnityEngine.Events; // Подключаем UnityEvent (для автосейва на 4/6 и 6/6)
using TMPro; // Подключаем TextMeshPro
using Gameplay.Quest; // Подключаем систему задач

public class CassetteInventoryUI : MonoBehaviour // Скрипт счетчика кассет
{
    [Header("UI")] // Блок UI
    public TextMeshProUGUI cassetteCounterText; // Текст счетчика кассет

    [Header("Settings")] // Блок настроек
    public int currentCassetteCount = 0; // Текущее количество кассет

    public int maxCassetteCount = 6; // Максимум кассет

    public int activateMonsterAt = 4; // На какой кассете активировать монстра

    [Header("Monster")] // Блок монстра
    public MonsterAI monsterAI; // Ссылка на главный скрипт монстра

    [Header("Hall Doors")] // Блок дверей в зал
    public UniversalDoor[] hallDoors; // Массив дверей в зал

    [Header("Final Sequence")] // Блок финальной последовательности
    public ApartmentFinalSequence finalSequence; // Ссылка на режиссёрский скрипт финала квартиры

    [Header("Save Checkpoints")] // Автосейв — вешай сюда SaveManager.SaveCheckpoint
    public UnityEvent onReachedFourOfSix; // Собрано 4/6 кассет (чекпоинт 2)

    public UnityEvent onReachedSixOfSix; // Собрано 6/6 кассет (чекпоинт 3)

    private bool monsterActivated = false; // Защита от повторной активации монстра

    private bool finalEventTriggered = false; // Защита от повторного запуска финального события

    private void Start() // При старте сцены
    {
        UpdateUI(); // Обновляем UI
    }

    public void AddCassette() // Добавить кассету
    {
        currentCassetteCount = Mathf.Clamp(currentCassetteCount + 1, 0, maxCassetteCount); // Увеличиваем счетчик и не даём выйти за максимум

        if (!monsterActivated && currentCassetteCount >= activateMonsterAt) // Если монстр ещё не активирован и собрано 4+ кассеты
        {
            ActivateMonsterByCassette(); // Запускаем событие 4/6 через кассеты
        }

        if (!finalEventTriggered && currentCassetteCount >= maxCassetteCount) // Если финал ещё не запущен и собраны все кассеты
        {
            TriggerFinalEvent(); // Запускаем финальную последовательность
        }

        QuestService.Instance?.CompleteTask("cassettes"); // Отмечаем задачу «Собрать все кассеты»

        UpdateUI(); // Обновляем текст счетчика
    }

    private void ActivateMonsterByCassette() // Метод события на 4/6 кассет
    {
        monsterActivated = true; // Запоминаем, что событие 4/6 уже запущено со стороны кассет

        onReachedFourOfSix.Invoke(); // Автосейв: чекпоинт 2 (собрано 4/6)

        UnlockHallDoors(); // Разблокируем двери, если они ещё существуют как UniversalDoor

        if (finalSequence != null) // Если ApartmentFinalSequence назначен
        {
            finalSequence.StartEarlyHallDoorBreakSequence(); // Запускаем единое событие 4/6
        }
        else if (monsterAI != null) // Если финальный режиссёр не назначен, но монстр есть
        {
            monsterAI.ActivateMonster(); // Запасной вариант: просто активируем монстра
        }
        else // Если ничего не назначено
        {
            Debug.LogWarning("CassetteInventoryUI: не назначен ApartmentFinalSequence или MonsterAI"); // Пишем предупреждение
        }
    }

    private void UnlockHallDoors() // Разблокировать двери в зал
    {
        if (hallDoors == null) return; // Если массива нет — выходим

        foreach (UniversalDoor door in hallDoors) // Проходим по каждой двери
        {
            if (door == null) continue; // Если дверь не назначена — пропускаем

            door.SetLocked(false); // Разблокируем дверь через метод
        }
    }

    private void TriggerFinalEvent() // Метод запуска финального события
    {
        finalEventTriggered = true; // Запоминаем, что финал уже был запущен

        onReachedSixOfSix.Invoke(); // Автосейв: чекпоинт 3 (собрано 6/6)

        if (finalSequence != null) // Если режиссёр финала назначен
        {
            finalSequence.StartFinalSequence(); // Запускаем финальную последовательность квартиры
        }
        else // Если ссылка не назначена
        {
            Debug.LogWarning("ApartmentFinalSequence не назначен в CassetteInventoryUI"); // Пишем предупреждение в Console
        }
    }

    private void UpdateUI() // Обновление UI
    {
        if (cassetteCounterText == null) return; // Если текста нет — выходим

        cassetteCounterText.text = currentCassetteCount + "/" + maxCassetteCount; // Показываем 0/6, 1/6 и т.д.
    }

    public void SetCassetteCountDebug(int newCount) // Debug-установка количества кассет
    {
        currentCassetteCount = Mathf.Clamp(newCount, 0, maxCassetteCount); // Ставим нужное количество кассет и ограничиваем максимумом

        if (!monsterActivated && currentCassetteCount >= activateMonsterAt) // Если монстр ещё не активирован и кассет 4 или больше
        {
            ActivateMonsterByCassette(); // Активируем монстра через тот же сценарий 4/6
        }

        if (!finalEventTriggered && currentCassetteCount >= maxCassetteCount) // Если финал ещё не запускался и кассет 6 или больше
        {
            TriggerFinalEvent(); // Запускаем финальную последовательность
        }

        QuestService.Instance?.CompleteTask("cassettes"); // Отмечаем задачу «Собрать все кассеты»

        UpdateUI(); // Обновляем текст счетчика
    }
}