using System.Collections; // Подключаем корутины для задержки и плавного исчезновения текста
using TMPro; // Подключаем TextMeshPro
using UnityEngine; // Подключаем основные Unity-классы

[DisallowMultipleComponent] // Запрещаем случайно добавлять два одинаковых компонента на один UI-объект
public class DialogueTextUI : MonoBehaviour // Универсальный контроллер вывода диалогового текста на экран
{
    [Header("UI References")] // Блок ссылок на элементы интерфейса
    public TMP_Text dialogueText; // Текст TextMeshPro, в котором будет показана реплика

    public CanvasGroup dialogueCanvasGroup; // CanvasGroup для плавного появления и исчезновения текста

    [Header("Timing")] // Блок настроек времени показа
    [Min(0f)] // Запрещаем отрицательное время через Inspector
    public float visibleDuration = 3f; // Сколько секунд текст остаётся полностью видимым

    [Min(0f)] // Запрещаем отрицательную длительность исчезновения
    public float fadeDuration = 0.35f; // За сколько секунд текст плавно исчезает

    public bool useUnscaledTime = true; // Продолжать ли показ текста, когда Time.timeScale равен нулю

    [Header("Debug")] // Блок отладки
    public bool showDebugWarnings = true; // Показывать ли предупреждения о неправильной настройке

    private Coroutine showTextCoroutine; // Ссылка на текущую корутину показа текста
    private string configuredMessage; // Текст, заранее введённый в поле Text Input самого компонента TextMeshPro
    private static DialogueTextUI currentDialogue; // Текущий диалог на экране, общий для всех объектов DialogueTextUI

    public event System.Action Shown; // Событие: реплику показали (SC_DialogueVoice цепляет сюда голос)

    private void Reset() // Unity вызывает этот метод при первом добавлении компонента
    {
        dialogueText = GetComponentInChildren<TMP_Text>(true); // Пытаемся автоматически найти TextMeshPro среди дочерних объектов
        dialogueCanvasGroup = GetComponent<CanvasGroup>(); // Пытаемся автоматически найти CanvasGroup на этом же объекте
    }

    private void Awake() // Вызывается при загрузке UI-объекта
    {
        if (dialogueText == null) // Проверяем, назначен ли TextMeshPro вручную
        {
            dialogueText = GetComponentInChildren<TMP_Text>(true); // Пробуем найти его автоматически среди дочерних объектов
        }

        if (dialogueCanvasGroup == null) // Проверяем, назначен ли CanvasGroup
        {
            dialogueCanvasGroup = GetComponent<CanvasGroup>(); // Сначала ищем CanvasGroup на этом объекте
        }

        if (dialogueCanvasGroup == null) // Если CanvasGroup всё ещё отсутствует
        {
            dialogueCanvasGroup = gameObject.AddComponent<CanvasGroup>(); // Автоматически добавляем CanvasGroup на UI-объект
        }

        if (dialogueText != null) // Проверяем, найден ли TextMeshPro
        {
            configuredMessage = dialogueText.text; // Запоминаем текст, который был заранее введён в Text Input через Inspector
        }

        HideImmediately(); // На старте сцены прячем пустой диалоговый текст
    }

    private void OnDisable() // Вызывается при выключении UI-компонента или его объекта
    {
        if (showTextCoroutine != null) // Проверяем, запущена ли корутина показа
        {
            StopCoroutine(showTextCoroutine); // Останавливаем старую корутину
            showTextCoroutine = null; // Очищаем ссылку на остановленную корутину
        }

        HideImmediately(); // Не оставляем текст видимым после повторного включения объекта

        if (currentDialogue == this) // Проверяем, был ли этот объект текущим диалогом
        {
            currentDialogue = null; // Очищаем общую ссылку на выключенный диалог
        }
    }

    public void ShowConfiguredText() // Показывает текст, заранее введённый в Text Input компонента TextMeshPro
    {
        ShowText(configuredMessage); // Передаём сохранённый текст существующему методу показа
    }

    public void ShowText(string message) // Публичный метод показа новой реплики
    {
        if (string.IsNullOrWhiteSpace(message)) return; // Не показываем пустую строку

        if (dialogueText == null) // Проверяем наличие TextMeshPro
        {
            if (showDebugWarnings) // Проверяем, разрешены ли предупреждения
            {
                Debug.LogWarning("DialogueTextUI: поле Dialogue Text не назначено.", this); // Сообщаем о неправильной настройке UI
            }

            return; // Без TextMeshPro показать реплику невозможно
        }

        if (!isActiveAndEnabled) // Проверяем, активен ли объект, на котором должна запускаться корутина
        {
            if (showDebugWarnings) // Проверяем, разрешены ли предупреждения
            {
                Debug.LogWarning("DialogueTextUI должен находиться на постоянно включённом UI-объекте.", this); // Объясняем причину отсутствия текста
            }

            return; // На выключенном MonoBehaviour корутина не запускается
        }

        if (currentDialogue != null && currentDialogue != this) // Проверяем, показывается ли сейчас другой диалоговый объект
        {
            currentDialogue.HideText(); // Скрываем предыдущую реплику, чтобы два текста не накладывались друг на друга
        }

        if (showTextCoroutine != null) // Проверяем, показывается ли предыдущая реплика
        {
            StopCoroutine(showTextCoroutine); // Останавливаем старый показ, чтобы реплики не конфликтовали
            showTextCoroutine = null; // Очищаем старую ссылку
        }

        dialogueText.text = message; // Записываем новую реплику в TextMeshPro
        dialogueText.gameObject.SetActive(true); // Включаем объект текста, если он был выключен вручную

        if (dialogueCanvasGroup != null) // Проверяем наличие CanvasGroup
        {
            dialogueCanvasGroup.alpha = 1f; // Делаем текст полностью видимым
            dialogueCanvasGroup.interactable = false; // Диалоговый текст не должен перехватывать UI-взаимодействие
            dialogueCanvasGroup.blocksRaycasts = false; // Диалоговый текст не должен блокировать лучи UI
        }

        currentDialogue = this; // Запоминаем этот объект как единственный текущий диалог

        Shown?.Invoke(); // Сообщаем подписчикам (голос) о показе реплики

        showTextCoroutine = StartCoroutine(ShowTextRoutine()); // Запускаем ожидание и плавное исчезновение
    }

    public void HideText() // Публичный метод для досрочного скрытия текущей реплики
    {
        if (showTextCoroutine != null) // Проверяем, запущена ли корутина показа
        {
            StopCoroutine(showTextCoroutine); // Останавливаем текущую корутину
            showTextCoroutine = null; // Очищаем ссылку
        }

        HideImmediately(); // Мгновенно скрываем текст

        if (currentDialogue == this) // Проверяем, является ли этот диалог текущим
        {
            currentDialogue = null; // Очищаем общую ссылку после ручного скрытия
        }
    }

    private IEnumerator ShowTextRoutine() // Корутина времени показа и плавного исчезновения
    {
        if (visibleDuration > 0f) // Проверяем, нужна ли задержка полной видимости
        {
            if (useUnscaledTime) // Проверяем выбранный способ отсчёта времени
            {
                yield return new WaitForSecondsRealtime(visibleDuration); // Ждём независимо от Time.timeScale
            }
            else // Если требуется обычное игровое время
            {
                yield return new WaitForSeconds(visibleDuration); // Ждём с учётом Time.timeScale
            }
        }

        if (dialogueCanvasGroup != null && fadeDuration > 0f) // Проверяем, можно ли выполнить плавное исчезновение
        {
            float elapsedTime = 0f; // Создаём счётчик времени исчезновения

            while (elapsedTime < fadeDuration) // Выполняем цикл до окончания исчезновения
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; // Выбираем подходящее время кадра
                elapsedTime += deltaTime; // Увеличиваем прошедшее время
                dialogueCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsedTime / fadeDuration); // Плавно уменьшаем прозрачность от 1 до 0
                yield return null; // Продолжаем исчезновение на следующем кадре
            }
        }

        HideImmediately(); // После завершения времени полностью скрываем реплику
        showTextCoroutine = null; // Очищаем ссылку на закончившуюся корутину

        if (currentDialogue == this) // Проверяем, является ли завершившийся диалог текущим
        {
            currentDialogue = null; // Очищаем общую ссылку после окончания показа
        }
    }

    private void HideImmediately() // Внутренний метод мгновенного скрытия текста
    {
        if (dialogueCanvasGroup != null) // Проверяем наличие CanvasGroup
        {
            dialogueCanvasGroup.alpha = 0f; // Делаем весь диалоговый UI полностью прозрачным
            dialogueCanvasGroup.interactable = false; // Запрещаем взаимодействие с невидимым UI
            dialogueCanvasGroup.blocksRaycasts = false; // Запрещаем невидимому UI блокировать мышь
        }

        if (dialogueText != null) // Проверяем наличие TextMeshPro
        {
            dialogueText.text = string.Empty; // Очищаем старую реплику
        }
    }
}
