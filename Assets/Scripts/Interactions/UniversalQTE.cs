using UnityEngine; // Подключаем Unity
using TMPro; // Подключаем TextMeshPro
using UnityEngine.Events; // Подключаем UnityEvent

public class UniversalQTE : MonoBehaviour // Универсальный QTE-контроллер
{
    [Header("UI")] // Блок интерфейса
    public GameObject qtePanel; // Панель QTE

    public TextMeshProUGUI qteText; // Текст QTE

    [Header("Settings")] // Блок настроек
    public KeyCode qteKey = KeyCode.E; // Кнопка QTE

    public int requiredPresses = 10; // Сколько раз нужно нажать

    public float qteTime = 4f; // Сколько секунд даётся

    public string qteLabel = "ЖМИ E"; // Текст сверху

    [Header("Events")] // Блок событий
    public UnityEvent onQTESuccess; // Что произойдёт при успехе

    public UnityEvent onQTEFail; // Что произойдёт при провале

    private int currentPresses = 0; // Текущее количество нажатий

    private float timer = 0f; // Таймер QTE

    private bool qteActive = false; // Активен ли QTE

    private void Start() // При старте сцены
    {
        if (qtePanel != null) // Если панель назначена
        {
            qtePanel.SetActive(false); // Выключаем панель
        }
    }

    private void Update() // Каждый кадр
    {
        if (!qteActive) return; // Если QTE не активен — выходим

        timer -= Time.deltaTime; // Уменьшаем таймер

        if (Input.GetKeyDown(qteKey)) // Если нажали кнопку QTE
        {
            currentPresses++; // Увеличиваем счётчик

            UpdateText(); // Обновляем текст

            if (currentPresses >= requiredPresses) // Если набрали нужное количество
            {
                CompleteQTE(); // Завершаем QTE успешно
            }
        }

        if (timer <= 0f) // Если время вышло
        {
            FailQTE(); // Проваливаем QTE
        }
    }

    public void StartQTE() // Запустить QTE с настройками из Inspector
    {
        if (qteActive) return; // Если QTE уже идёт — выходим

        qteActive = true; // Включаем QTE

        currentPresses = 0; // Сбрасываем нажатия

        timer = qteTime; // Ставим таймер

        if (qtePanel != null) // Если панель назначена
        {
            qtePanel.SetActive(true); // Показываем панель
        }

        UpdateText(); // Обновляем текст
    }

    public void StartQTE(int presses, float time, string label) // Запустить QTE с параметрами из другого скрипта
    {
        requiredPresses = presses; // Задаём количество нажатий

        qteTime = time; // Задаём время

        qteLabel = label; // Задаём текст

        StartQTE(); // Запускаем QTE
    }

    private void UpdateText() // Обновить текст QTE
    {
        if (qteText == null) return; // Если текста нет — выходим

        qteText.text = qteLabel + "\n" + currentPresses + "/" + requiredPresses; // Показываем прогресс
    }

    private void CompleteQTE() // Успешное завершение QTE
    {
        qteActive = false; // Выключаем QTE

        if (qtePanel != null) // Если панель назначена
        {
            qtePanel.SetActive(false); // Скрываем панель
        }

        onQTESuccess.Invoke(); // Вызываем событие успеха

        Debug.Log("UniversalQTE: успех"); // Сообщение в Console
    }

    private void FailQTE() // Провал QTE
    {
        qteActive = false; // Выключаем QTE

        if (qtePanel != null) // Если панель назначена
        {
            qtePanel.SetActive(false); // Скрываем панель
        }

        onQTEFail.Invoke(); // Вызываем событие провала

        Debug.Log("UniversalQTE: провал"); // Сообщение в Console
    }
}