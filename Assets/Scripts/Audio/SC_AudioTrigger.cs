using UnityEngine; // Подключаем Unity-классы
using UnityEngine.Events; // Подключаем UnityEvent
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)

// Триггер-зона: когда игрок входит внутрь (например, поднялся на второй этаж),
// запускает звук квартиры — разовое FMOD-событие и/или зацикленный SC_AmbienceLoop.
[RequireComponent(typeof(Collider))]
public class SC_AudioTrigger : MonoBehaviour
{
    [Header("Trigger")] // Блок триггера
    public string playerTag = "Player"; // Тег игрока (или его родителя)

    public bool triggerOnce = true; // Сработать только один раз

    [Header("FMOD One-Shot")] // Разовое событие (необязательно)
    public EventReference oneShotEvent; // Разовый звук при входе (скрип, крик и т.п.)

    [Header("Ambience Loop")] // Зацикленный эмбиенс (необязательно)
    public SC_AmbienceLoop ambienceLoop; // Эмбиенс квартиры, который включить при входе

    public bool stopOnExit = false; // Гасить эмбиенс при выходе из зоны

    [Header("Events")] // Произвольные действия (необязательно)
    public UnityEvent onPlayerEntered; // Что сделать при входе игрока

    public UnityEvent onPlayerExited; // Что сделать при выходе игрока

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private bool fired = false; // Уже срабатывал ли (для triggerOnce)

    private void Reset() // Вызывается при добавлении компонента в редакторе
    {
        Collider c = GetComponent<Collider>(); // Берём коллайдер

        if (c != null) c.isTrigger = true; // Сразу делаем его триггером — удобно
    }

    private void OnTriggerEnter(Collider other) // Кто-то вошёл в зону
    {
        if (!IsPlayer(other)) return; // Не игрок — выходим

        if (triggerOnce && fired) return; // Уже срабатывал и нужно один раз — выходим

        fired = true; // Помечаем срабатывание

        if (!oneShotEvent.IsNull) // Если назначено разовое событие
        {
            RuntimeManager.PlayOneShot(oneShotEvent, transform.position); // Играем разово в позиции зоны
        }

        if (ambienceLoop != null) // Если назначен эмбиенс
        {
            ambienceLoop.Play(); // Запускаем эмбиенс квартиры
        }

        onPlayerEntered.Invoke(); // Произвольные действия

        if (showDebugLogs) Debug.Log(gameObject.name + ": игрок вошёл в зону"); // Лог
    }

    private void OnTriggerExit(Collider other) // Кто-то вышел из зоны
    {
        if (!IsPlayer(other)) return; // Не игрок — выходим

        if (stopOnExit && ambienceLoop != null) // Если нужно гасить эмбиенс при выходе
        {
            ambienceLoop.Stop(); // Останавливаем эмбиенс
        }

        onPlayerExited.Invoke(); // Произвольные действия

        if (showDebugLogs) Debug.Log(gameObject.name + ": игрок вышел из зоны"); // Лог
    }

    private bool IsPlayer(Collider other) // Проверка, что это игрок (по тегу, включая родителей)
    {
        Transform t = other.transform; // Начинаем с самого коллайдера

        while (t != null) // Идём вверх по иерархии
        {
            if (t.CompareTag(playerTag)) return true; // Нашли тег игрока — это он

            t = t.parent; // Поднимаемся к родителю
        }

        return false; // Тег игрока не найден
    }
}
