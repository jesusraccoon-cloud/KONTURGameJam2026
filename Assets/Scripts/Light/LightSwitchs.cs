using UnityEngine; // Подключаем Unity-классы

public class LightSwitch : MonoBehaviour, IInteractable // Создаём выключатель света и подключаем его к системе взаимодействия через IInteractable
{
    [Header("Light Settings")] // Заголовок блока настроек света в Inspector
    public Light[] lightsToToggle; // Список источников света, которые будет включать и выключать этот выключатель

    public bool isOn = true; // Текущее состояние света: true = включён, false = выключен

    [Header("Debug")] // Заголовок блока отладки в Inspector
    public bool showDebugLogs = false; // Включить сообщения в Console для проверки работы

    private void Start() // Метод запускается один раз при старте сцены
    {
        ApplyLightState(); // Применяем стартовое состояние света
    }

    public void Interact() // Метод вызывается PlayerInteractor, когда игрок нажимает E по выключателю
    {
        ToggleLight(); // Переключаем свет
    }

    public void ToggleLight() // Метод переключает состояние света
    {
        isOn = !isOn; // Меняем состояние на противоположное

        ApplyLightState(); // Применяем новое состояние ко всем лампам

        if (showDebugLogs) // Проверяем, включены ли debug-сообщения
        {
            Debug.Log(gameObject.name + " переключил свет. Сейчас isOn = " + isOn); // Пишем сообщение в Console
        }
    }

    private void ApplyLightState() // Метод применяет состояние света ко всем источникам света
    {
        if (lightsToToggle == null) return; // Если список света не создан — выходим

        for (int i = 0; i < lightsToToggle.Length; i++) // Проходим по всем источникам света в списке
        {
            if (lightsToToggle[i] == null) continue; // Если ячейка пустая — пропускаем её

            lightsToToggle[i].enabled = isOn; // Включаем или выключаем конкретный источник света
        }
    }
}