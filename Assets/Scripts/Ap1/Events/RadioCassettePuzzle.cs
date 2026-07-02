using UnityEngine; // Подключаем Unity-классы
using UnityEngine.Events; // Подключаем UnityEvent
using FMODUnity; // Подключаем FMOD

public class RadioCassettePuzzle : MonoBehaviour, IInteractable // Радио включает музыку, один раз открывает кассету и взрывается
{
    [Header("Radio State")] // Состояние радио
    public bool isRadioOn = false; // Включено ли радио

    public bool isExploded = false; // Взорвалось ли радио

    [Header("Cassette")] // Кассета
    public GameObject cassetteObject; // Кассета, которая станет доступна после первого включения

    public bool cassetteUnlocksOnlyOnce = true; // Кассета появляется только один раз

    private bool cassetteWasUnlocked = false; // Была ли кассета открыта

    [Header("Explosion Timer")] // Таймер взрыва
    public bool canExplode = true; // Может ли радио взорваться

    public float explosionDelay = 20f; // Через сколько секунд взрыв

    private float radioOnTimer = 0f; // Таймер включённого радио

    [Header("Noise")] // Шум
    public NoiseEmitter noiseEmitter; // NoiseEmitter радио

    [Range(1, 10)] public int radioNoisePower = 10; // Шум радио

    [Range(1, 10)] public int explosionNoisePower = 10; // Шум взрыва

    public bool emitMonsterNoiseOnTurnOn = true; // Шум монстру при включении

    public bool keepNoiseUILockedWhileRadioOn = true; // Держать UI на 10/10 пока радио включено

    public NoiseMeterUI noiseMeterUI; // UI шума

    [Header("FMOD")] // FMOD
    public StudioEventEmitter radioMusicEmitter; // Постоянная музыка радио

    public StudioEventEmitter explosionEmitter; // Звук взрыва

    [Header("Objects After Explosion")] // Объекты после взрыва
    public GameObject intactRadioObject; // Целая модель радио

    public GameObject brokenRadioObject; // Сломанная модель радио

    [Header("Events")] // События
    public UnityEvent onRadioExploded; // События после взрыва

    [Header("Debug")] // Отладка
    public bool showDebugLogs = true; // Показывать логи

    private void Start() // При старте сцены
    {
        if (cassetteObject != null && !cassetteWasUnlocked) // Если кассета назначена и ещё не открыта
        {
            cassetteObject.SetActive(false); // Прячем кассету
        }

        if (brokenRadioObject != null) // Если сломанная модель назначена
        {
            brokenRadioObject.SetActive(false); // Прячем сломанную модель
        }
    }

    private void Update() // Каждый кадр
    {
        HandleExplosionTimer(); // Считаем таймер взрыва
    }

    public void Interact() // Вызывается PlayerInteractor
    {
        if (isExploded) return; // Если взорвано — не работает

        if (isRadioOn) // Если включено
        {
            TurnRadioOff(); // Выключаем
        }
        else // Если выключено
        {
            TurnRadioOn(); // Включаем
        }
    }

    private void TurnRadioOn() // Включить радио
    {
        isRadioOn = true; // Радио включено

        radioOnTimer = 0f; // Сброс таймера

        PlayRadioMusic(); // Включаем FMOD-музыку

        UnlockCassetteIfNeeded(); // Открываем кассету один раз

        if (emitMonsterNoiseOnTurnOn) // Если нужен шум монстру
        {
            EmitNoise(radioNoisePower); // Отправляем шум монстру один раз
        }

        LockNoiseUI(); // Блокируем UI на 10/10

        if (showDebugLogs) Debug.Log(gameObject.name + ": радио включено"); // Лог
    }

    private void TurnRadioOff() // Выключить радио
    {
        isRadioOn = false; // Радио выключено

        radioOnTimer = 0f; // Сброс таймера

        StopRadioMusic(); // Останавливаем FMOD-музыку

        UnlockNoiseUI(true); // Отпускаем UI, чтобы он плавно спадал

        if (showDebugLogs) Debug.Log(gameObject.name + ": радио выключено"); // Лог
    }

    private void HandleExplosionTimer() // Таймер взрыва
    {
        if (!canExplode) return; // Если взрыв выключен — выход

        if (isExploded) return; // Если уже взорвалось — выход

        if (!isRadioOn) return; // Если радио выключено — выход

        radioOnTimer += Time.deltaTime; // Увеличиваем таймер

        if (radioOnTimer >= explosionDelay) // Если время вышло
        {
            ExplodeRadio(); // Взрываем
        }
    }

    private void UnlockCassetteIfNeeded() // Открыть кассету
    {
        if (cassetteUnlocksOnlyOnce && cassetteWasUnlocked) return; // Если уже открыта — выход

        cassetteWasUnlocked = true; // Запоминаем открытие

        if (cassetteObject != null) // Если кассета есть
        {
            cassetteObject.SetActive(true); // Включаем кассету
        }

        if (showDebugLogs) Debug.Log(gameObject.name + ": кассета стала доступна"); // Лог
    }

    private void ExplodeRadio() // Взрыв радио
    {
        isExploded = true; // Помечаем взрыв

        isRadioOn = false; // Радио больше не включено

        StopRadioMusic(); // Останавливаем музыку

        UnlockNoiseUI(false); // Снимаем постоянный шум

        if (noiseMeterUI != null) // Если UI есть
        {
            noiseMeterUI.SetNoise(explosionNoisePower); // Показываем шум взрыва
        }

        PlayExplosionSound(); // Звук взрыва

        EmitNoise(explosionNoisePower); // Шум взрыва монстру

        if (intactRadioObject != null) // Если целая модель есть
        {
            intactRadioObject.SetActive(false); // Выключаем целую модель
        }

        if (brokenRadioObject != null) // Если сломанная модель есть
        {
            brokenRadioObject.SetActive(true); // Включаем сломанную модель
        }

        onRadioExploded.Invoke(); // Запускаем события

        if (showDebugLogs) Debug.Log(gameObject.name + ": радио взорвалось"); // Лог
    }

    private void LockNoiseUI() // Заблокировать UI шума
    {
        if (!keepNoiseUILockedWhileRadioOn) return; // Если выключено — выход

        if (noiseMeterUI == null) return; // Если UI нет — выход

        noiseMeterUI.LockNoise(radioNoisePower); // Держим 10/10
    }

    private void UnlockNoiseUI(bool keepCurrentValue) // Разблокировать UI шума
    {
        if (noiseMeterUI == null) return; // Если UI нет — выход

        noiseMeterUI.UnlockNoise(keepCurrentValue); // Снимаем блокировку
    }

    private void EmitNoise(int noisePower) // Создать шум
    {
        if (noiseEmitter == null) return; // Если NoiseEmitter нет — выход

        noiseEmitter.EmitNoise(noisePower); // Отправляем шум
    }

    private void PlayRadioMusic() // Запустить музыку
    {
        if (radioMusicEmitter == null) return; // Если FMOD нет — выход

        radioMusicEmitter.Play(); // Играть
    }

    private void StopRadioMusic() // Остановить музыку
    {
        if (radioMusicEmitter == null) return; // Если FMOD нет — выход

        radioMusicEmitter.Stop(); // Стоп
    }

    private void PlayExplosionSound() // Звук взрыва
    {
        if (explosionEmitter == null) return; // Если FMOD нет — выход

        explosionEmitter.Play(); // Играть взрыв
    }
}