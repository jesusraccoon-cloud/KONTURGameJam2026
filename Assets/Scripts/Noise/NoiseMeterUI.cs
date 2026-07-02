using UnityEngine; // Подключаем Unity-классы
using UnityEngine.UI; // Подключаем UI Image
using TMPro; // Подключаем TextMeshPro

public class NoiseMeterUI : MonoBehaviour // Скрипт визуальной шкалы шума игрока
{
    [Header("UI References")] // Блок ссылок UI
    public TMP_Text noiseText; // Текст шума

    public Image noiseFillImage; // Красная полоска шума

    [Header("Main Settings")] // Главные настройки
    [Range(0, 10)] public int currentNoise = 0; // Текущий шум

    public int maxNoise = 10; // Максимальный шум

    [Header("Fade Settings")] // Настройки спада
    public bool autoFadeToZero = true; // Спадать ли к нулю

    public float fadeDelay = 0.25f; // Задержка перед спадом

    public float fadeDuration = 1.5f; // Длительность спада

    [Header("Noise Mixing Settings")] // Настройки смешивания
    public bool allowNoiseMixing = true; // Разрешить смешивание

    [Range(0, 10)] public int mixBonus = 2; // Бонус смешивания

    [Header("Locked Noise")] // Заблокированный постоянный шум
    public bool isNoiseLocked = false; // Заблокирован ли шум

    [Range(0, 10)] public int lockedNoiseValue = 0; // Значение заблокированного шума

    private float visualNoise = 0f; // Плавное значение шума

    private float lastNoiseTime = -999f; // Время последнего шума

    private void Start() // При старте
    {
        visualNoise = currentNoise; // Синхронизируем значение

        UpdateUI(); // Обновляем UI
    }

    private void Update() // Каждый кадр
    {
        HandleLockedNoise(); // Проверяем заблокированный шум

        HandleFade(); // Обрабатываем спад
    }

    private void HandleLockedNoise() // Удерживает шум, если он заблокирован
    {
        if (!isNoiseLocked) return; // Если блокировки нет — выходим

        currentNoise = Mathf.Clamp(lockedNoiseValue, 0, maxNoise); // Ставим текущее значение

        visualNoise = currentNoise; // Ставим визуальное значение

        lastNoiseTime = Time.time; // Обновляем время, чтобы спад не начинался

        UpdateUI(); // Обновляем UI
    }

    private void HandleFade() // Плавный спад
    {
        if (isNoiseLocked) return; // Если шум заблокирован — не спадаем

        if (!autoFadeToZero) return; // Если автоспад выключен — выходим

        if (Time.time < lastNoiseTime + fadeDelay) return; // Ждём задержку

        if (visualNoise <= 0f) return; // Если уже ноль — выходим

        float fadeSpeed = maxNoise / Mathf.Max(0.01f, fadeDuration); // Скорость спада

        visualNoise = Mathf.MoveTowards(visualNoise, 0f, fadeSpeed * Time.deltaTime); // Двигаем к нулю

        currentNoise = Mathf.RoundToInt(visualNoise); // Округляем

        UpdateUI(); // Обновляем UI
    }

    public void SetNoise(int value) // Установить шум
    {
        if (isNoiseLocked) return; // Если шум заблокирован — обычный шум не перебивает

        value = Mathf.Clamp(value, 0, maxNoise); // Ограничиваем

        currentNoise = value; // Записываем шум

        visualNoise = value; // Записываем визуально

        lastNoiseTime = Time.time; // Запоминаем время

        UpdateUI(); // Обновляем UI
    }

    public void AddNoise(int value) // Добавить шум
    {
        if (isNoiseLocked) return; // Если шум заблокирован — обычный шум не перебивает

        value = Mathf.Clamp(value, 0, maxNoise); // Ограничиваем вход

        if (!allowNoiseMixing) // Если смешивание выключено
        {
            SetNoise(Mathf.Max(currentNoise, value)); // Ставим сильнейший шум

            return; // Выходим
        }

        int mixedNoise = Mathf.Max(currentNoise, value); // Берём сильнейший

        if (currentNoise > 0 && value > 0) // Если шум наложился
        {
            mixedNoise += mixBonus; // Добавляем бонус
        }

        mixedNoise = Mathf.Clamp(mixedNoise, 0, maxNoise); // Ограничиваем

        SetNoise(mixedNoise); // Устанавливаем итог
    }

    public void LockNoise(int value) // Заблокировать шум на постоянном значении
    {
        isNoiseLocked = true; // Включаем блокировку

        lockedNoiseValue = Mathf.Clamp(value, 0, maxNoise); // Записываем значение

        currentNoise = lockedNoiseValue; // Ставим текущий шум

        visualNoise = lockedNoiseValue; // Ставим визуальный шум

        lastNoiseTime = Time.time; // Обновляем время

        UpdateUI(); // Обновляем UI
    }

    public void UnlockNoise(bool keepCurrentValue = true) // Разблокировать шум
    {
        isNoiseLocked = false; // Выключаем блокировку

        if (keepCurrentValue) // Если нужно оставить текущее значение
        {
            currentNoise = Mathf.Clamp(currentNoise, 0, maxNoise); // Ограничиваем текущее

            visualNoise = currentNoise; // Синхронизируем визуально

            lastNoiseTime = Time.time; // Даем шкале спадать после задержки
        }
        else // Если нужно сразу сбросить
        {
            currentNoise = 0; // Ставим ноль

            visualNoise = 0f; // Ставим визуальный ноль

            lastNoiseTime = Time.time; // Обновляем время
        }

        UpdateUI(); // Обновляем UI
    }

    private void UpdateUI() // Обновить UI
    {
        if (noiseText != null) // Если текст назначен
        {
            noiseText.text = currentNoise + "/" + maxNoise; // Пишем 10/10
        }

        if (noiseFillImage != null) // Если полоска назначена
        {
            noiseFillImage.fillAmount = visualNoise / maxNoise; // Заполняем полоску
        }
    }
}