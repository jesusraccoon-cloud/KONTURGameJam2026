using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)

public class LightSwitch : MonoBehaviour, IInteractable // Выключатель света, который работает через PlayerInteractor
{
    public string hint = "Взаимодействовать";
    public string Hint => hint;
    [Header("Light Settings")] // Настройки света
    public Light[] lightsToToggle; // Все источники света, которыми управляет этот выключатель

    public bool isOn = true; // Включён ли свет в начале сцены

    [Header("Switch FMOD")] // Звук самого выключателя (FMOD)
    public EventReference turnOnEvent; // Событие щелчка при включении

    public EventReference turnOffEvent; // Событие щелчка при выключении

    public Transform soundOrigin; // Откуда звучит щелчок (если пусто — позиция этого объекта). Важно для 3D-события

    [Header("Noise For Monster")] // Шум для монстра
    public NoiseEmitter switchNoiseEmitter; // NoiseEmitter на выключателе

    [Range(1, 10)] public int switchNoisePower = 2; // Сила шума щелчка выключателя

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать сообщения в Console

    private void Start() // Запускается один раз при старте сцены
    {
        ApplyLightState(); // Применяем стартовое состояние света
    }

    public void Interact() // Вызывается PlayerInteractor, когда игрок нажимает E
    {
        ToggleLight(); // Переключаем свет
    }

    public void ToggleLight() // Переключение света
    {
        isOn = !isOn; // Меняем состояние света на противоположное

        PlaySwitchSound(); // Проигрываем звук щелчка (Turn On / Turn Off)

        EmitSwitchNoise(); // Создаём шум для монстра

        ApplyLightState(); // Применяем новое состояние света

        if (showDebugLogs) // Если debug включён
        {
            Debug.Log(gameObject.name + " переключил свет. isOn = " + isOn); // Пишем лог
        }
    }

    private void ApplyLightState() // Применяет состояние света
    {
        if (lightsToToggle != null) // Если список света существует
        {
            for (int i = 0; i < lightsToToggle.Length; i++) // Проходим по всем Light
            {
                if (lightsToToggle[i] == null) continue; // Пропускаем пустые элементы

                lightsToToggle[i].enabled = isOn; // Включаем или выключаем свет (гул лампы реагирует сам через SC_LampHum)
            }
        }
    }

    private void PlaySwitchSound() // Проигрывает щелчок выключателя через FMOD
    {
        EventReference clickEvent = isOn ? turnOnEvent : turnOffEvent; // Выбираем событие по новому состоянию

        if (clickEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning(gameObject.name + ": не назначено событие " + (isOn ? "Turn On" : "Turn Off")); // Предупреждение
            return; // Выходим
        }

        Vector3 pos = soundOrigin != null ? soundOrigin.position : transform.position; // Точка, откуда звучит щелчок

        RuntimeManager.PlayOneShot(clickEvent, pos); // Проигрываем щелчок в позиции выключателя

        Debug.Log($"[SWITCH DIAG] {gameObject.name}: своя позиция={transform.position}, SoundOrigin={(soundOrigin != null ? soundOrigin.name + " @ " + soundOrigin.position : "None")}, звук играет в={pos}"); // ВРЕМЕННО безусловно
    }

    private void EmitSwitchNoise() // Создаёт шум выключателя для монстра
    {
        if (switchNoiseEmitter == null) return; // Если NoiseEmitter не назначен — выходим

        switchNoiseEmitter.EmitNoise(switchNoisePower); // Отправляем шум в NoiseManager
    }
}
