using UnityEngine; // Подключаем основные Unity-классы
using UnityEngine.UI; // Подключаем UI Image для красной полоски
using TMPro; // Подключаем TextMeshPro для текста 0/10

public class NoiseMeterUI : MonoBehaviour // Скрипт визуальной шкалы шума игрока
{
    [Header("UI References")] // Блок ссылок UI
    public TMP_Text noiseText; // Текст под шкалой, например 0/10

    public Image noiseFillImage; // Красная полоска заполнения шума

    [Header("Main Settings")] // Главные настройки
    [Range(0, 10)] public int currentNoise = 0; // Текущий отображаемый шум от 0 до 10

    public int maxNoise = 10; // Максимальное значение шума

    [Header("Fade Settings")] // Настройки плавного затухания
    public bool autoFadeToZero = true; // Нужно ли шуму плавно спадать до нуля

    public float fadeDelay = 0.25f; // Задержка перед началом спада

    public float fadeDuration = 1.5f; // За сколько секунд шум должен упасть с 10 до 0

    [Header("Noise Mixing Settings")] // Настройки смешивания шума
    public bool allowNoiseMixing = true; // Разрешить ли смешивание нескольких шумов

    [Range(0, 10)] public int mixBonus = 2; // Бонус, если новый шум пришёл поверх старого

    private float visualNoise = 0f; // Плавное внутреннее значение шума

    private float lastNoiseTime = -999f; // Время последнего полученного шума

    private void Start() // Вызывается при старте сцены
    {
        visualNoise = currentNoise; // Синхронизируем плавное значение с текущим шумом

        UpdateUI(); // Обновляем интерфейс
    }

    private void Update() // Вызывается каждый кадр
    {
        HandleFade(); // Обрабатываем плавное затухание шума
    }

    private void HandleFade() // Метод плавного затухания шума
    {
        if (!autoFadeToZero) return; // Если автоспад выключен, ничего не делаем

        if (Time.time < lastNoiseTime + fadeDelay) return; // Ждём задержку перед спадом

        if (visualNoise <= 0f) return; // Если шум уже ноль, ничего не делаем

        float fadeSpeed = maxNoise / Mathf.Max(0.01f, fadeDuration); // Считаем скорость спада

        visualNoise = Mathf.MoveTowards(visualNoise, 0f, fadeSpeed * Time.deltaTime); // Плавно двигаем шум к нулю

        currentNoise = Mathf.RoundToInt(visualNoise); // Округляем значение для текста

        UpdateUI(); // Обновляем шкалу и текст
    }

    public void SetNoise(int value) // Установить шум напрямую
    {
        value = Mathf.Clamp(value, 0, maxNoise); // Ограничиваем значение от 0 до 10

        currentNoise = value; // Записываем текущий шум

        visualNoise = value; // Сразу выставляем визуальное значение

        lastNoiseTime = Time.time; // Запоминаем время последнего шума

        UpdateUI(); // Обновляем интерфейс
    }

    public void AddNoise(int value) // Добавить шум с ограниченным смешиванием
    {
        value = Mathf.Clamp(value, 0, maxNoise); // Ограничиваем входящий шум

        if (!allowNoiseMixing) // Если смешивание выключено
        {
            SetNoise(Mathf.Max(currentNoise, value)); // Просто показываем самый сильный шум

            return; // Выходим
        }

        int mixedNoise = Mathf.Max(currentNoise, value); // Берём самый сильный из текущего и нового шума

        if (currentNoise > 0 && value > 0) // Если шум уже был и пришёл новый шум
        {
            mixedNoise += mixBonus; // Добавляем небольшой бонус за наложение действий
        }

        mixedNoise = Mathf.Clamp(mixedNoise, 0, maxNoise); // Не даём шуму стать больше 10

        SetNoise(mixedNoise); // Показываем итоговый смешанный шум
    }

    private void UpdateUI() // Обновить текст и красную полоску
    {
        if (noiseText != null) // Если текст назначен
        {
            noiseText.text = currentNoise + "/" + maxNoise; // Пишем формат 4/10
        }

        if (noiseFillImage != null) // Если красная полоска назначена
        {
            noiseFillImage.fillAmount = visualNoise / maxNoise; // Заполняем шкалу от 0 до 1
        }
    }
}