using UnityEngine; // Подключаем основные Unity-классы
using UnityEngine.Events; // Подключаем UnityEvent
using FMODUnity; // Подключаем FMOD
using Gameplay.Quest; // Подключаем систему задач

public class RadioCassettePuzzle : MonoBehaviour, IInteractable // Один коллайдер включает радио, кассета выезжает и автоматически забирается
{
    [Header("Radio State")] // Состояние радио
    public bool isRadioOn = false; // Включено ли радио сейчас

    public bool isExploded = false; // Взорвалось ли радио

    [Header("Cassette - One Collider Logic")] // Настройки кассеты без отдельного интерактивного коллайдера
    public GameObject cassetteObject; // Корневой объект кассеты

    public Transform cassetteTransform; // Transform кассеты, который будет выезжать

    public Transform cassetteEjectPoint; // Точка, куда должна выехать кассета

    [Min(0.01f)]
    public float cassetteEjectSpeed = 0.25f; // Скорость выезда кассеты

    public bool cassetteEjectsOnlyOnce = true; // Кассета выезжает только при первом включении радио

    [Header("Cassette Inventory")] // Добавление кассеты в инвентарь
    public CassetteInventoryUI cassetteInventoryUI; // UI счётчика кассет

    public bool autoFindCassetteInventoryUI = true; // Автоматически искать UI кассет в сцене

    public SC_Saveable cassetteSaveable; // Отдельный SC_Saveable кассеты

    public UnityEvent onCassetteCollected; // Дополнительные события после подбора кассеты

    [Header("Cassette Sound And Noise")] // Звук и шум выезда кассеты
    public StudioEventEmitter cassetteEjectEmitter; // Необязательный FMOD emitter выезда кассеты

    [Range(1, 10)]
    public int cassetteEjectNoisePower = 5; // Сила шума выезда кассеты

    [Header("Explosion Timer")] // Таймер взрыва
    public bool canExplode = true; // Может ли радио взорваться

    public float explosionDelay = 20f; // Через сколько секунд включённое радио взрывается

    private float radioOnTimer = 0f; // Текущее время работы радио

    [Header("Noise")] // Основной шум радио
    public NoiseEmitter noiseEmitter; // NoiseEmitter радио

    [Range(1, 10)]
    public int radioNoisePower = 10; // Шум включения радио

    [Range(1, 10)]
    public int explosionNoisePower = 10; // Шум взрыва

    public bool emitMonsterNoiseOnTurnOn = true; // Создавать ли шум для монстра при включении

    public bool keepNoiseUILockedWhileRadioOn = true; // Держать ли индикатор шума на максимуме

    public NoiseMeterUI noiseMeterUI; // UI индикатора шума

    [Header("FMOD")] // FMOD радио
    public StudioEventEmitter radioMusicEmitter; // Постоянная музыка радио

    public StudioEventEmitter explosionEmitter; // Звук взрыва

    [Header("Objects After Explosion")] // Смена моделей после взрыва
    public GameObject intactRadioObject; // Целая модель радио

    public GameObject brokenRadioObject; // Сломанная модель радио

    [Header("Events")] // События радио
    public UnityEvent onRadioExploded; // Дополнительные события после взрыва

    [Header("Save")] // Сохранение состояния радио
    [SerializeField]
    private SC_Saveable saveable; // SC_Saveable самого радио

    [Header("Debug State")] // Текущее состояние для проверки в Play Mode
    [SerializeField]
    private bool cassetteWasEjected = false; // Выезжала ли кассета

    [SerializeField]
    private bool cassetteIsEjecting = false; // Двигается ли кассета сейчас

    [SerializeField]
    private bool cassetteIsWaitingForPickup = false; // Служебное состояние для совместимости со старыми сохранениями

    [SerializeField]
    private bool cassetteIsCollected = false; // Забрана ли кассета

    [Header("Debug")] // Отладка
    public bool showDebugLogs = true; // Показывать сообщения в Console

    private Vector3 cassetteTargetPosition; // Конечная мировая позиция кассеты

    private void Awake() // Вызывается раньше Start
    {
        AutoFindReferences(); // Автоматически находим простые ссылки
    }

    private void Start() // Вызывается перед первым кадром
    {
        RestoreRadioSavedState(); // Восстанавливаем состояние радио

        AutoFindReferences(); // Повторно проверяем ссылки

        RestoreCassetteState(); // Восстанавливаем положение или сбор кассеты

        SetupRadioModels(); // Восстанавливаем целую или сломанную модель радио
    }

    private void Update() // Выполняется каждый кадр
    {
        UpdateCassetteEjectMovement(); // Двигаем кассету наружу

        HandleExplosionTimer(); // Считаем время до взрыва
    }

    public void Interact() // Вызывается PlayerInteractor через единственный коллайдер RadioInteraction
    {
        if (cassetteIsEjecting) return; // Во время выезда кассеты повторное нажатие не обрабатываем

        if (isExploded) return; // После взрыва радио больше не переключается

        if (isRadioOn) // Если радио включено
        {
            TurnRadioOff(); // Выключаем радио
        }
        else // Если радио выключено
        {
            TurnRadioOn(); // Включаем радио
        }
    }

    private void TurnRadioOn() // Включает радио
    {
        isRadioOn = true; // Сохраняем состояние включения

        radioOnTimer = 0f; // Сбрасываем таймер взрыва

        PlayRadioMusic(); // Запускаем музыку

        StartCassetteEjectIfNeeded(); // При первом включении запускаем выезд кассеты

        if (emitMonsterNoiseOnTurnOn) // Если шум для монстра включён
        {
            EmitNoise(radioNoisePower); // Отправляем шум включения
        }

        LockNoiseUI(); // Держим индикатор шума на нужном значении

        if (showDebugLogs) Debug.Log(gameObject.name + ": радио включено"); // Пишем лог
    }

    private void TurnRadioOff() // Выключает радио
    {
        isRadioOn = false; // Сохраняем состояние выключения

        radioOnTimer = 0f; // Сбрасываем таймер

        StopRadioMusic(); // Останавливаем музыку

        UnlockNoiseUI(true); // Разблокируем UI с плавным спадом

        if (showDebugLogs) Debug.Log(gameObject.name + ": радио выключено"); // Пишем лог
    }

    private void StartCassetteEjectIfNeeded() // Запускает выезд кассеты при первом включении
    {
        if (cassetteIsCollected) return; // Собранную кассету больше не показываем

        if (cassetteEjectsOnlyOnce && cassetteWasEjected) return; // Повторный выезд не запускаем

        AutoFindReferences(); // Проверяем ссылки перед движением

        if (cassetteObject == null) // Проверяем объект кассеты
        {
            Debug.LogWarning(gameObject.name + ": Cassette Object не назначен.", gameObject); // Пишем точную ошибку

            return; // Выходим
        }

        if (cassetteTransform == null) // Проверяем Transform кассеты
        {
            Debug.LogWarning(gameObject.name + ": Cassette Transform не назначен.", gameObject); // Пишем точную ошибку

            return; // Выходим
        }

        if (cassetteEjectPoint == null) // Проверяем точку выезда
        {
            Debug.LogWarning(gameObject.name + ": Cassette Eject Point не назначен.", gameObject); // Пишем точную ошибку

            return; // Выходим
        }

        cassetteObject.SetActive(true); // Показываем кассету

        cassetteTargetPosition = cassetteEjectPoint.position; // Запоминаем конечную мировую позицию

        cassetteWasEjected = true; // Запоминаем, что кассета уже выезжала

        cassetteIsEjecting = true; // Включаем движение

        cassetteIsWaitingForPickup = false; // Пока кассета движется, забрать её нельзя

        SaveRadioState(); // Сохраняем факт выезда

        PlayCassetteEjectSound(); // Проигрываем звук выезда

        EmitNoise(cassetteEjectNoisePower); // Создаём шум выезда

        if (showDebugLogs) Debug.Log(gameObject.name + ": кассета начала выезжать"); // Пишем лог
    }

    private void UpdateCassetteEjectMovement() // Плавно двигает кассету к EjectPoint
    {
        if (cassetteIsEjecting == false) return; // Если выезда нет, ничего не делаем

        if (cassetteTransform == null) // Если Transform пропал
        {
            cassetteIsEjecting = false; // Останавливаем ошибочное движение

            return; // Выходим
        }

        cassetteTransform.position = Vector3.MoveTowards(
            cassetteTransform.position,
            cassetteTargetPosition,
            cassetteEjectSpeed * Time.deltaTime
        ); // Двигаем кассету с постоянной скоростью

        float remainingDistance = Vector3.Distance(
            cassetteTransform.position,
            cassetteTargetPosition
        ); // Измеряем оставшееся расстояние

        if (remainingDistance > 0.002f) return; // Если ещё не доехали, ждём следующий кадр

        cassetteTransform.position = cassetteTargetPosition; // Точно ставим кассету в EjectPoint

        cassetteIsEjecting = false; // Завершаем движение

        cassetteIsWaitingForPickup = false; // Отдельного ожидания и второго нажатия E больше нет

        if (showDebugLogs) Debug.Log(gameObject.name + ": кассета выехала и автоматически забирается"); // Пишем лог

        CollectCassette(); // Сразу добавляем кассету в инвентарь и скрываем её
    }

    private void CollectCassette() // Забирает кассету через тот же коллайдер радио
    {
        if (cassetteIsCollected) return; // Повторный сбор запрещён

        cassetteIsCollected = true; // Запоминаем сбор

        cassetteIsWaitingForPickup = false; // Кассета больше не ждёт взаимодействия

        cassetteIsEjecting = false; // На всякий случай останавливаем движение

        if (cassetteSaveable != null) // Если сохранение кассеты назначено
        {
            cassetteSaveable.MarkUsed(); // Помечаем кассету использованной
        }

        if (onCassetteCollected != null) // Если событие создано
        {
            onCassetteCollected.Invoke(); // Вызываем дополнительные события
        }

        QuestManager.Instance?.CompleteTask("radio_cassette"); // Завершаем задачу после фактического получения кассеты

        if (cassetteInventoryUI != null) // Если UI кассет назначен
        {
            cassetteInventoryUI.AddCassette(); // Добавляем одну кассету в счётчик
        }
        else // Если UI не назначен
        {
            Debug.LogWarning(gameObject.name + ": Cassette Inventory UI не найден.", gameObject); // Пишем предупреждение
        }

        if (cassetteObject != null) // Если объект кассеты существует
        {
            cassetteObject.SetActive(false); // Прячем собранную кассету
        }

        if (showDebugLogs) Debug.Log(gameObject.name + ": кассета собрана через коллайдер радио"); // Пишем лог
    }

    private void AutoFindReferences() // Автоматически находит необязательные ссылки
    {
        if (cassetteTransform == null && cassetteObject != null) // Если Transform не назначен
        {
            cassetteTransform = cassetteObject.transform; // Берём Transform объекта кассеты
        }

        if (autoFindCassetteInventoryUI && cassetteInventoryUI == null) // Если включён автопоиск UI
        {
            cassetteInventoryUI = FindFirstObjectByType<CassetteInventoryUI>(); // Ищем UI кассет в сцене
        }

        if (cassetteSaveable == null && cassetteObject != null) // Если сохранение кассеты не назначено
        {
            cassetteSaveable = cassetteObject.GetComponent<SC_Saveable>(); // Ищем на самой кассете

            if (cassetteSaveable == null) // Если не нашли на корне
            {
                cassetteSaveable = cassetteObject.GetComponentInChildren<SC_Saveable>(true); // Ищем ниже
            }
        }

        if (saveable == null) // Если сохранение радио не назначено
        {
            saveable = GetComponent<SC_Saveable>(); // Ищем на объекте радио
        }
    }

    private void RestoreCassetteState() // Восстанавливает кассету после загрузки
    {
        cassetteIsCollected =
            cassetteSaveable != null
            && SC_SaveSystem.IsUsed(cassetteSaveable.id); // Проверяем, собиралась ли кассета

        if (cassetteIsCollected) // Если кассета уже собрана
        {
            cassetteIsEjecting = false; // Движение выключено

            cassetteIsWaitingForPickup = false; // Подбор не нужен

            if (cassetteObject != null) cassetteObject.SetActive(false); // Прячем кассету

            return; // Завершаем восстановление
        }

        if (cassetteWasEjected) // Если кассета выезжала, но ещё не собрана
        {
            if (cassetteObject != null) cassetteObject.SetActive(true); // Показываем кассету

            if (cassetteTransform != null && cassetteEjectPoint != null) // Если ссылки назначены
            {
                cassetteTransform.position = cassetteEjectPoint.position; // Сразу ставим кассету снаружи
            }

            cassetteIsEjecting = false; // Анимация после загрузки не нужна

            cassetteIsWaitingForPickup = false; // Второе нажатие E не требуется

            CollectCassette(); // Сразу восстанавливаем кассету как полученную

            return; // Завершаем восстановление
        }

        cassetteIsEjecting = false; // До первого включения движения нет

        cassetteIsWaitingForPickup = false; // Подбор пока запрещён

        if (cassetteObject != null) cassetteObject.SetActive(false); // Прячем кассету внутри радио
    }

    private void RestoreRadioSavedState() // Восстанавливает флаги радио
    {
        if (saveable == null) saveable = GetComponent<SC_Saveable>(); // Ищем сохранение радио

        if (saveable == null) saveable = GetComponentInChildren<SC_Saveable>(true); // Или ниже

        if (saveable != null && SC_SaveSystem.TryGetState(saveable.id, out int state)) // Если состояние найдено
        {
            cassetteWasEjected = (state & 1) != 0; // Бит 0 — кассета уже выезжала

            isExploded = (state & 2) != 0; // Бит 1 — радио взорвано
        }
    }

    private void SaveRadioState() // Сохраняет флаги радио
    {
        if (saveable == null) return; // Без SC_Saveable сохранять некуда

        int state =
            (cassetteWasEjected ? 1 : 0)
            | (isExploded ? 2 : 0); // Собираем флаги в число

        SC_SaveSystem.SetState(saveable.id, state); // Записываем состояние
    }

    private void SetupRadioModels() // Восстанавливает модели радио
    {
        if (isExploded) // Если радио взорвано
        {
            isRadioOn = false; // Оно не может быть включено

            if (intactRadioObject != null) intactRadioObject.SetActive(false); // Прячем целую модель

            if (brokenRadioObject != null) brokenRadioObject.SetActive(true); // Показываем сломанную модель
        }
        else // Если радио целое
        {
            if (brokenRadioObject != null) brokenRadioObject.SetActive(false); // Прячем сломанную модель
        }
    }

    private void HandleExplosionTimer() // Считает время до взрыва
    {
        if (canExplode == false) return; // Если взрыв выключен, выходим

        if (isExploded) return; // Повторный взрыв запрещён

        if (isRadioOn == false) return; // Выключенное радио не считает таймер

        radioOnTimer += Time.deltaTime; // Увеличиваем время работы

        if (radioOnTimer >= explosionDelay) // Если время вышло
        {
            ExplodeRadio(); // Взрываем радио
        }
    }

    private void ExplodeRadio() // Взрывает радио
    {
        isExploded = true; // Запоминаем взрыв

        isRadioOn = false; // Радио выключается

        SaveRadioState(); // Сохраняем взрыв

        StopRadioMusic(); // Останавливаем музыку

        UnlockNoiseUI(false); // Снимаем постоянный шум UI

        if (noiseMeterUI != null) // Если UI назначен
        {
            noiseMeterUI.SetNoise(explosionNoisePower); // Показываем шум взрыва
        }

        PlayExplosionSound(); // Проигрываем звук взрыва

        EmitNoise(explosionNoisePower); // Отправляем шум монстру

        if (intactRadioObject != null) intactRadioObject.SetActive(false); // Прячем целую модель

        if (brokenRadioObject != null) brokenRadioObject.SetActive(true); // Показываем сломанную модель

        if (onRadioExploded != null) // Если событие создано
        {
            onRadioExploded.Invoke(); // Вызываем события
        }

        if (showDebugLogs) Debug.Log(gameObject.name + ": радио взорвалось"); // Пишем лог
    }

    private void LockNoiseUI() // Блокирует UI шума
    {
        if (keepNoiseUILockedWhileRadioOn == false) return; // Если функция выключена, выходим

        if (noiseMeterUI == null) return; // Если UI не назначен, выходим

        noiseMeterUI.LockNoise(radioNoisePower); // Удерживаем шум радио
    }

    private void UnlockNoiseUI(bool keepCurrentValue) // Разблокирует UI шума
    {
        if (noiseMeterUI == null) return; // Если UI не назначен, выходим

        noiseMeterUI.UnlockNoise(keepCurrentValue); // Снимаем блокировку
    }

    private void EmitNoise(int noisePower) // Создаёт шум
    {
        if (noiseEmitter == null) return; // Если NoiseEmitter не назначен, выходим

        noiseEmitter.EmitNoise(noisePower); // Отправляем шум
    }

    private void PlayRadioMusic() // Запускает музыку радио
    {
        if (radioMusicEmitter == null) return; // Если FMOD emitter не назначен, выходим

        radioMusicEmitter.Play(); // Включаем музыку
    }

    private void StopRadioMusic() // Останавливает музыку радио
    {
        if (radioMusicEmitter == null) return; // Если FMOD emitter не назначен, выходим

        radioMusicEmitter.Stop(); // Останавливаем музыку
    }

    private void PlayCassetteEjectSound() // Проигрывает звук выезда кассеты
    {
        if (cassetteEjectEmitter == null) return; // Если звук не назначен, выходим

        cassetteEjectEmitter.Play(); // Запускаем звук
    }

    private void PlayExplosionSound() // Проигрывает звук взрыва
    {
        if (explosionEmitter == null) return; // Если FMOD emitter не назначен, выходим

        explosionEmitter.Play(); // Запускаем звук
    }
}