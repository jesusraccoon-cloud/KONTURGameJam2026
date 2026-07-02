using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD

public class TVTogglePuzzle : MonoBehaviour, IInteractable // Телевизор включается и выключается через E
{
    [Header("TV State")] // Блок состояния телевизора
    public bool isOn = false; // Включён ли телевизор сейчас

    public bool isBroken = false; // Сломан ли телевизор

    [Header("Screen Light")] // Блок света экрана
    public Light[] screenLights; // Источники света от экрана

    public GameObject screenGlowObject; // Объект свечения экрана, если есть

    [Header("Noise")] // Блок шума
    public NoiseEmitter noiseEmitter; // NoiseEmitter телевизора

    [Range(1, 10)] public int tvNoisePower = 6; // Сила шума телевизора

    public NoiseMeterUI noiseMeterUI; // UI шума

    public bool lockNoiseUIWhileOn = true; // Держать шум на UI пока телевизор включён

    public bool emitMonsterNoiseOnTurnOn = true; // Давать шум монстру при включении

    [Header("FMOD")] // Блок FMOD
    public StudioEventEmitter tvSoundEmitter; // FMOD-звук телевизора

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = true; // Показывать логи

    private void Start() // При старте сцены
    {
        ApplyTVState(); // Применяем стартовое состояние
    }

    public void Interact() // Вызывается PlayerInteractor
    {
        if (isBroken) return; // Если телевизор сломан — не работает

        ToggleTV(); // Переключаем телевизор
    }

    private void ToggleTV() // Переключить телевизор
    {
        if (isOn) // Если включён
        {
            TurnOff(); // Выключаем
        }
        else // Если выключен
        {
            TurnOn(); // Включаем
        }
    }

    private void TurnOn() // Включить телевизор
    {
        isOn = true; // Запоминаем включение

        ApplyTVState(); // Применяем свет и звук

        if (emitMonsterNoiseOnTurnOn) // Если шум монстру включён
        {
            EmitTVNoise(); // Создаём шум
        }

        if (lockNoiseUIWhileOn) // Если нужно держать UI
        {
            LockNoiseUI(); // Держим шум
        }

        if (showDebugLogs) Debug.Log(gameObject.name + ": телевизор включён"); // Лог
    }

    private void TurnOff() // Выключить телевизор
    {
        isOn = false; // Запоминаем выключение

        ApplyTVState(); // Применяем выключение

        UnlockNoiseUI(true); // Отпускаем UI

        if (showDebugLogs) Debug.Log(gameObject.name + ": телевизор выключен"); // Лог
    }

    private void ApplyTVState() // Применить состояние телевизора
    {
        if (screenLights != null) // Если массив света есть
        {
            for (int i = 0; i < screenLights.Length; i++) // Перебираем свет
            {
                if (screenLights[i] == null) continue; // Пропускаем пустые

                screenLights[i].enabled = isOn; // Включаем или выключаем свет
            }
        }

        if (screenGlowObject != null) // Если объект свечения есть
        {
            screenGlowObject.SetActive(isOn); // Включаем или выключаем свечение
        }

        if (tvSoundEmitter != null) // Если FMOD назначен
        {
            if (isOn) tvSoundEmitter.Play(); // Если включён — играем

            else tvSoundEmitter.Stop(); // Если выключен — стоп
        }
    }

    private void EmitTVNoise() // Создать шум телевизора
    {
        if (noiseEmitter == null) return; // Если NoiseEmitter нет — выходим

        noiseEmitter.EmitNoise(tvNoisePower); // Отправляем шум
    }

    private void LockNoiseUI() // Заблокировать UI шума
    {
        if (noiseMeterUI == null) return; // Если UI нет — выходим

        noiseMeterUI.LockNoise(tvNoisePower); // Держим шум
    }

    private void UnlockNoiseUI(bool keepCurrentValue) // Разблокировать UI шума
    {
        if (noiseMeterUI == null) return; // Если UI нет — выходим

        noiseMeterUI.UnlockNoise(keepCurrentValue); // Отпускаем шум
    }

    public void BreakTV() // Метод для будущей поломки телевизора
    {
        isBroken = true; // Помечаем сломанным

        isOn = false; // Выключаем

        ApplyTVState(); // Выключаем свет и звук

        UnlockNoiseUI(true); // Отпускаем UI

        if (showDebugLogs) Debug.Log(gameObject.name + ": телевизор сломан"); // Лог
    }
}