using System.Collections; // Подключаем корутины
using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD

public class CuckooClockPuzzle : MonoBehaviour, IInteractable // Часы открываются только ключом и запускают кукушку
{
    [Header("Inventory")] // Блок инвентаря
    public UniversalInventory inventory; // Универсальный инвентарь игрока

    public string requiredItemId = "ClockKey"; // ID предмета, который нужен для открытия часов

    public bool removeKeyAfterUse = true; // Удалять ли ключ после использования

    [Header("Cassette")] // Блок кассеты
    public GameObject cassetteObject; // Кассета, которая появится после кукушки

    [Header("Interaction Collider")] // Блок коллайдера часов
    public Collider clockInteractionCollider; // Коллайдер взаимодействия часов, который отключится после кукушки

    public bool disableInteractionColliderAfterSequence = true; // Отключать ли коллайдер часов после завершения кукушки

    [Header("Cuckoo Settings")] // Настройки кукушки
    public int cuckooCount = 12; // Сколько раз кукушка кричит

    public float delayBetweenCuckoo = 1f; // Пауза между криками

    [Header("Noise")] // Блок шума
    public NoiseEmitter noiseEmitter; // NoiseEmitter часов

    [Range(1, 10)] public int cuckooNoisePower = 7; // Сила каждого крика

    [Header("FMOD")] // Блок FMOD
    public StudioEventEmitter cuckooEmitter; // FMOD Emitter кукушки

    [Header("Visual Objects")] // Визуальные объекты
    public GameObject closedClockObject; // Закрытая версия часов

    public GameObject openedClockObject; // Открытая версия часов

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = true; // Показывать сообщения в Console

    private bool isOpened = false; // Открыты ли часы

    private bool isRunning = false; // Идёт ли сейчас сцена кукушки

    public event System.Action Opened; // Событие: часы открылись (для звука открытия)

    public event System.Action Cuckooed; // Событие: кукушка крикнула (для звука крика)

    public void Interact() // Вызывается PlayerInteractor при E
    {
        if (isOpened) return; // Если часы уже открыты — выходим

        if (isRunning) return; // Если сцена уже идёт — выходим

        if (inventory == null) // Если инвентарь не назначен
        {
            Debug.LogWarning(gameObject.name + ": не назначен UniversalInventory"); // Предупреждение

            return; // Выходим
        }

        if (!inventory.HasItem(requiredItemId)) // Если у игрока нет нужного предмета
        {
            if (showDebugLogs) // Если debug включён
            {
                Debug.Log("Часы закрыты. Нужен предмет: " + requiredItemId); // Пишем сообщение
            }

            return; // Не открываем часы
        }

        StartCoroutine(OpenClockSequence()); // Запускаем открытие часов
    }

    private IEnumerator OpenClockSequence() // Последовательность открытия часов
    {
        isRunning = true; // Помечаем, что сцена началась

        isOpened = true; // Помечаем часы открытыми

        if (removeKeyAfterUse) // Если ключ нужно удалить
        {
            inventory.RemoveItem(requiredItemId); // Удаляем ключ из инвентаря
        }

        if (cassetteObject != null) // Если кассета назначена
        {
            cassetteObject.SetActive(false); // Прячем кассету до конца криков
        }

        if (closedClockObject != null) // Если закрытая модель назначена
        {
            closedClockObject.SetActive(false); // Выключаем закрытую модель
        }

        if (openedClockObject != null) // Если открытая модель назначена
        {
            openedClockObject.SetActive(true); // Включаем открытую модель
        }

        Opened?.Invoke(); // Сообщаем подписчикам (звук) об открытии часов

        for (int i = 0; i < cuckooCount; i++) // Повторяем крик кукушки столько раз, сколько указано в Inspector
        {
            PlayCuckooSound(); // Проигрываем FMOD-звук (StudioEventEmitter, если назначен)

            Cuckooed?.Invoke(); // Сообщаем подписчикам (звук) о крике кукушки

            EmitCuckooNoise(); // Создаём шум для монстра

            yield return new WaitForSeconds(delayBetweenCuckoo); // Ждём перед следующим криком
        }

        if (cassetteObject != null) // Если кассета назначена
        {
            cassetteObject.SetActive(true); // Делаем кассету доступной
        }

        DisableClockInteractionCollider(); // Отключаем коллайдер взаимодействия часов, чтобы он не мешал кассете

        isRunning = false; // Завершаем сцену

        if (showDebugLogs) // Если debug включён
        {
            Debug.Log("Часы открылись. Кассета доступна. Коллайдер часов отключён."); // Пишем лог
        }
    }

    private void PlayCuckooSound() // Проиграть звук кукушки
    {
        if (cuckooEmitter == null) return; // Если FMOD emitter не назначен — выходим

        cuckooEmitter.Play(); // Проигрываем событие FMOD
    }

    private void EmitCuckooNoise() // Создать шум кукушки
    {
        if (noiseEmitter == null) return; // Если NoiseEmitter не назначен — выходим

        noiseEmitter.EmitNoise(cuckooNoisePower); // Отправляем шум
    }

    private void DisableClockInteractionCollider() // Отключить коллайдер часов после завершения
    {
        if (!disableInteractionColliderAfterSequence) return; // Если отключение выключено в Inspector — выходим

        if (clockInteractionCollider == null) return; // Если коллайдер не назначен — выходим

        clockInteractionCollider.enabled = false; // Отключаем коллайдер часов
    }
}