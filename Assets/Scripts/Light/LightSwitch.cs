using UnityEngine; // Подключаем Unity-классы

public class LightSwitch : MonoBehaviour, IInteractable // Выключатель света, который работает через PlayerInteractor
{
    [Header("Light Settings")] // Настройки света
    public Light[] lightsToToggle; // Все источники света, которыми управляет этот выключатель

    public bool isOn = true; // Включён ли свет в начале сцены

    [Header("Switch Audio")] // Звук самого выключателя
    public AudioSource switchAudioSource; // AudioSource на выключателе

    public AudioClip switchClickClip; // Звук щелчка выключателя

    [Header("Lamp Audio")] // Звук лампы
    public AudioSource[] lampHumAudioSources; // AudioSource гула ламп

    [Header("Noise For Monster")] // Шум для монстра
    public NoiseEmitter switchNoiseEmitter; // NoiseEmitter на выключателе

    [Range(1, 10)] public int switchNoisePower = 2; // Сила шума щелчка выключателя

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать сообщения в Console

    private void Start() // Запускается один раз при старте сцены
    {
        ApplyLightState(); // Применяем стартовое состояние света и гула ламп
    }

    public void Interact() // Вызывается PlayerInteractor, когда игрок нажимает E
    {
        ToggleLight(); // Переключаем свет
    }

    public void ToggleLight() // Переключение света
    {
        isOn = !isOn; // Меняем состояние света на противоположное

        PlaySwitchSound(); // Проигрываем звук щелчка

        EmitSwitchNoise(); // Создаём шум для монстра

        ApplyLightState(); // Применяем новое состояние света

        if (showDebugLogs) // Если debug включён
        {
            Debug.Log(gameObject.name + " переключил свет. isOn = " + isOn); // Пишем лог
        }
    }

    private void ApplyLightState() // Применяет состояние света и гула
    {
        if (lightsToToggle != null) // Если список света существует
        {
            for (int i = 0; i < lightsToToggle.Length; i++) // Проходим по всем Light
            {
                if (lightsToToggle[i] == null) continue; // Пропускаем пустые элементы

                lightsToToggle[i].enabled = isOn; // Включаем или выключаем свет
            }
        }

        ApplyLampHumState(); // Включаем или выключаем гул ламп
    }

    private void PlaySwitchSound() // Проигрывает щелчок выключателя
    {
        if (switchAudioSource == null) return; // Если AudioSource не назначен — выходим

        if (switchClickClip == null) return; // Если клип не назначен — выходим

        switchAudioSource.PlayOneShot(switchClickClip); // Проигрываем щелчок один раз
    }

    private void ApplyLampHumState() // Управляет гулом ламп
    {
        if (lampHumAudioSources == null) return; // Если массива нет — выходим

        for (int i = 0; i < lampHumAudioSources.Length; i++) // Проходим по всем AudioSource гула
        {
            if (lampHumAudioSources[i] == null) continue; // Пропускаем пустые элементы

            if (isOn) // Если свет включён
            {
                if (!lampHumAudioSources[i].isPlaying) // Если гул ещё не играет
                {
                    lampHumAudioSources[i].Play(); // Запускаем гул
                }
            }
            else // Если свет выключен
            {
                lampHumAudioSources[i].Stop(); // Останавливаем гул
            }
        }
    }

    private void EmitSwitchNoise() // Создаёт шум выключателя для монстра
    {
        if (switchNoiseEmitter == null) return; // Если NoiseEmitter не назначен — выходим

        switchNoiseEmitter.EmitNoise(switchNoisePower); // Отправляем шум в NoiseManager
    }
}