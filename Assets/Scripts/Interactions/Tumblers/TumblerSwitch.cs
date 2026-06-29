using UnityEngine; // Подключаем Unity

public class TumblerSwitch : MonoBehaviour // Скрипт тумблера УМПСР
{
    public bool isOn = false; // Включен ли тумблер

    [Header("Renderer")] // Блок Renderer
    public Renderer targetRenderer; // Renderer тумблера

    [Header("Colors")] // Блок цветов
    public Color normalColor = Color.gray; // Цвет выключенного тумблера
    public Color highlightColor = Color.yellow; // Цвет при наведении
    public Color activeColor = Color.green; // Цвет включенного тумблера

    [Header("Apartment Power")] // Блок питания квартиры
    public ApartmentPowerController apartmentPowerController; // Контроллер квартиры

    private Material runtimeMaterial; // Индивидуальный материал тумблера

    private void Start() // Вызывается при старте сцены
    {
        if (targetRenderer == null) // Если Renderer не назначен
            targetRenderer = GetComponent<Renderer>(); // Ищем Renderer на объекте

        if (targetRenderer == null) // Если Renderer всё ещё не найден
            targetRenderer = GetComponentInChildren<Renderer>(); // Ищем Renderer в детях

        if (targetRenderer != null) // Если Renderer найден
        {
            runtimeMaterial = new Material(targetRenderer.material); // Создаём копию материала
            targetRenderer.material = runtimeMaterial; // Назначаем копию тумблеру
        }

        SyncWithApartmentPower(); // Синхронизируем состояние с квартирой
        UpdateVisual(); // Обновляем цвет
    }

    public void Toggle() // Вызывается при нажатии на тумблер
    {
        if (apartmentPowerController == null) // Если контроллер не назначен
        {
            Debug.LogWarning("У тумблера не назначен ApartmentPowerController"); // Warning
            return; // Выходим
        }

        apartmentPowerController.TogglePower(); // Просим квартиру переключить питание

        SyncWithApartmentPower(); // Берём реальное состояние квартиры

        UpdateVisual(); // Обновляем цвет
    }

    private void SyncWithApartmentPower() // Синхронизация тумблера с квартирой
    {
        if (apartmentPowerController == null) return; // Если контроллера нет — выходим

        isOn = apartmentPowerController.isPoweredOn; // Тумблер равен реальному питанию квартиры
    }

    public void SetHighlight(bool state) // Подсветка при наведении
    {
        if (runtimeMaterial == null) return; // Если материала нет — выходим

        if (isOn) // Если тумблер включён
        {
            runtimeMaterial.color = activeColor; // Оставляем зелёный цвет
            return; // Выходим
        }

        runtimeMaterial.color = state ? highlightColor : normalColor; // Жёлтый при наведении, серый без наведения
    }

    public void UpdateVisual() // Обновление цвета тумблера
    {
        if (runtimeMaterial == null) return; // Если материала нет — выходим

        runtimeMaterial.color = isOn ? activeColor : normalColor; // Зелёный если включён, серый если выключен
    }

    public void SetState(bool state) // Принудительная установка состояния
    {
        isOn = state; // Записываем состояние
        UpdateVisual(); // Обновляем цвет
    }
}