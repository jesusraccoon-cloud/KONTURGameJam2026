using System.Collections; // Для Coroutine
using TMPro; // Для TMP текста
using UnityEngine; // Unity классы

public class ClosetPhysicalFall : MonoBehaviour, IInteractable // Шкаф который валится после проверки
{
    [Header("Availability")] // Блок доступности
    public ApartmentFinalSequence finalSequence; // Ссылка на финальный сценарий квартиры
    public bool requireFinalSequence = true; // Требовать ли запуск 6/6 перед падением

    [Header("UI")] // Блок интерфейса
    public GameObject pressPanel; // Панель QTE
    public TextMeshProUGUI pressText; // Текст QTE

    [Header("Press Check")] // Блок проверки нажатий
    public KeyCode pressKey = KeyCode.E; // Кнопка проверки
    public int requiredPresses = 10; // Сколько раз нажать
    public float timeLimit = 4f; // Время на выполнение
    public string pressLabel = "ЖМИ Е"; // Надпись

    [Header("Wardrobe Physics")] // Настройки шкафа
    public Rigidbody wardrobeRigidbody; // Rigidbody шкафа
    public Vector3 fallDirection = new Vector3(1f, 0f, 0f); // Направление падения
    public float pushForce = 4f; // Сила толчка
    public float torqueForce = 25f; // Сила опрокидывания
    public Vector3 centerOfMassOffset = new Vector3(0f, 0.8f, 0f); // Смещение центра массы вверх

    [Header("After Fall")] // После падения
    public BoxCollider heavyMoveCollider; // Коллайдер для перетаскивания
    public float enableMoveDelay = 1.5f; // Через сколько включить движение

    private bool isRunning = false; // Защита от повторного запуска
    private bool completed = false; // Уже опрокинут
    public bool canFall = false; // Можно ли сейчас уронить шкаф

    private void Start() // При старте сцены
    {
        if (pressPanel != null) pressPanel.SetActive(false); // Прячем панель проверки

        if (heavyMoveCollider != null) heavyMoveCollider.enabled = false; // Выключаем движение шкафа до падения

        if (wardrobeRigidbody != null) // Если Rigidbody назначен
        {
            wardrobeRigidbody.isKinematic = true; // Фиксируем шкаф до успешной проверки
            wardrobeRigidbody.useGravity = true; // Гравитация будет работать после включения физики
            wardrobeRigidbody.centerOfMass = centerOfMassOffset; // Задаём центр массы для падения
        }
    }

    public void Interact()
{
    if (!canFall) return; // Если шкаф ещё не разрешён

    if (completed) return; // Уже упал

    if (isRunning) return; // Уже идёт проверка

    StartCoroutine(StartPressCheck()); // Запускаем проверку
}

    private IEnumerator StartPressCheck() // Проверка нажатий
    {
        isRunning = true; // Блокируем повторный запуск

        int currentPresses = 0; // Счётчик

        float timer = timeLimit; // Таймер

        if (pressPanel != null) pressPanel.SetActive(true); // Показываем панель

        while (timer > 0f) // Пока время не вышло
        {
            timer -= Time.deltaTime; // Уменьшаем время

            if (pressText != null) // Если текст назначен
            {
                pressText.text = pressLabel + "\n" + currentPresses + "/" + requiredPresses; // Обновляем текст
            }

            if (Input.GetKeyDown(pressKey)) // Если нажали кнопку
            {
                currentPresses++; // Добавляем нажатие

                if (currentPresses >= requiredPresses) // Если набрали нужное число
                {
                    break; // Проверка пройдена
                }
            }

            yield return null; // Следующий кадр
        }

        if (pressPanel != null) pressPanel.SetActive(false); // Прячем панель

        if (currentPresses >= requiredPresses) // Если проверка пройдена
        {
            CompleteFall(); // Валим шкаф
        }

        isRunning = false; // Разрешаем новый запуск, если проверка провалена
    }

    private void CompleteFall() // Опрокидывание шкафа
    {
        completed = true; // Запоминаем, что шкаф уже опрокинут

        if (wardrobeRigidbody == null) return; // Если Rigidbody нет — выходим

        wardrobeRigidbody.isKinematic = false; // Включаем физику

        wardrobeRigidbody.centerOfMass = centerOfMassOffset; // Смещаем центр массы

        Vector3 pushDirection = fallDirection.normalized; // Берём направление из Inspector

        wardrobeRigidbody.AddForce((pushDirection + Vector3.up * 0.35f).normalized * pushForce, ForceMode.Impulse); // Толкаем шкаф с небольшим подъёмом

        Vector3 torqueDirection = Vector3.Cross(pushDirection, Vector3.up); // Считаем направление опрокидывания

        wardrobeRigidbody.AddTorque(torqueDirection * torqueForce, ForceMode.Impulse); // Опрокидываем шкаф

        StartCoroutine(EnableMoveAfterDelay()); // Включаем движение позже
    }

    private IEnumerator EnableMoveAfterDelay() // Ждём падение
    {
        yield return new WaitForSeconds(enableMoveDelay); // Ждём, пока шкаф упадёт

        if (heavyMoveCollider != null) heavyMoveCollider.enabled = true; // Включаем коллайдер движения
    }
}