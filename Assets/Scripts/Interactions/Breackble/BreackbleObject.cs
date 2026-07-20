using UnityEngine; // Подключаем основные Unity-классы
using UnityEngine.Events; // Подключаем UnityEvent для событий в Inspector

public class BreakableObject : MonoBehaviour, IHitInteractable // Универсальный объект, который можно ударить и разрушить
{
    [Header("State")] // Блок состояния объекта
    public bool canBeHit = true; // Можно ли сейчас бить этот объект

    public bool isBroken = false; // Разрушен ли объект

    [Header("Hit Settings")] // Блок настроек ударов
    public int hitsToBreak = 3; // Сколько ударов нужно для разрушения

    public float hitDelay = 0.3f; // Минимальная задержка между ударами

    [Header("Apartment Final Sequence Lock")] // Блок ограничения через финал квартиры
    public bool requireApartmentFinalSequence = false; // Нужно ли разрешать разрушение только после 6/6

    public ApartmentFinalSequence apartmentFinalSequence; // Сюда назначается ApartmentFinalSequence нужной квартиры

    [Header("Noise Settings")] // Блок настроек шума
    public NoiseEmitter noiseEmitter; // Источник шума на этом объекте

    [Range(0, 10)] public int hitNoisePower = 7; // Сила шума обычного удара

    [Range(0, 10)] public int breakNoisePower = 8; // Сила шума разрушения

    public bool makeNoiseOnHit = true; // Создавать ли шум при каждом ударе

    public bool makeNoiseOnBreak = true; // Создавать ли шум при разрушении

    [Header("Object Swap")] // Блок замены моделей
    public GameObject intactObject; // Целая версия объекта

    public GameObject brokenObject; // Разрушенная версия объекта

    [Header("Objects To Enable After Break")] // Блок объектов, которые нужно включить после разрушения
    public GameObject[] objectsToEnableAfterBreak; // Список объектов для включения

    [Header("Objects To Disable After Break")] // Блок объектов, которые нужно выключить после разрушения
    public GameObject[] objectsToDisableAfterBreak; // Список объектов для выключения

    [Header("Events")] // Блок событий
    public UnityEvent onHit; // Событие при успешном ударе

    public UnityEvent onBreak; // Событие при полном разрушении

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = true; // Показывать ли сообщения в Console

    private int currentHits = 0; // Сколько ударов уже нанесено

    private float lastHitTime = -999f; // Время последнего удара

    public int CurrentHits // Публичное свойство для чтения количества ударов
    {
        get { return currentHits; } // Возвращаем текущее количество ударов
    }

    public bool IsBroken // Публичное свойство для проверки разрушения
    {
        get { return isBroken; } // Возвращаем состояние разрушения
    }

    private void Awake() // Вызывается при создании объекта
    {
        if (noiseEmitter == null) // Если NoiseEmitter не назначен вручную
        {
            noiseEmitter = GetComponent<NoiseEmitter>(); // Пробуем найти NoiseEmitter на этом же объекте
        }
    }

    private void Start() // Вызывается перед первым кадром
    {
        ApplyVisualState(); // Выставляем правильное визуальное состояние
    }

    public void Hit() // Метод вызывается игроком через IHitInteractable
    {
        if (!canBeHit) return; // Если объект нельзя бить, выходим

        if (isBroken) return; // Если объект уже разрушен, выходим

        if (!CanBreakByApartmentFinalSequence()) return; // Если финал 6/6 еще не начался, запрещаем удар

        if (Time.time < lastHitTime + hitDelay) return; // Если удар слишком быстрый, игнорируем

        lastHitTime = Time.time; // Запоминаем время удара

        currentHits++; // Увеличиваем количество ударов

        if (makeNoiseOnHit && noiseEmitter != null) // Если шум удара включен и NoiseEmitter есть
        {
            noiseEmitter.EmitNoise(hitNoisePower); // Создаем шум удара
        }

        if (onHit != null) onHit.Invoke(); // Вызываем событие удара

        if (showDebugLogs) // Если отладка включена
        {
            Debug.Log(gameObject.name + " удар: " + currentHits + "/" + hitsToBreak); // Пишем прогресс ударов
        }

        if (currentHits >= hitsToBreak) // Если ударов достаточно
        {
            BreakObject(); // Разрушаем объект
        }
    }

    private bool CanBreakByApartmentFinalSequence() // Проверяем, разрешено ли разрушение по финалу квартиры
    {
        if (requireApartmentFinalSequence == false) return true; // Если ограничение выключено, разрешаем бить всегда

        if (apartmentFinalSequence == null) // Если ограничение включено, но ApartmentFinalSequence не назначен
        {
            if (showDebugLogs) Debug.Log(gameObject.name + ": ApartmentFinalSequence не назначен, объект нельзя разбить"); // Пишем лог

            return false; // Не разрешаем разрушение без назначенной финальной последовательности
        }

        if (apartmentFinalSequence.finalSequenceStarted == false) // Если финал 6/6 еще не начался
        {
            if (showDebugLogs) Debug.Log(gameObject.name + ": финал 6/6 еще не начался, объект нельзя разбить"); // Пишем лог

            return false; // Запрещаем разрушение до 6/6
        }

        return true; // Если финал 6/6 начался, разрешаем разрушение
    }

    public void BreakObject() // Метод полного разрушения объекта
    {
        if (isBroken) return; // Если уже разрушен, выходим

        if (!CanBreakByApartmentFinalSequence()) return; // Не даем разрушить объект через код до 6/6

        isBroken = true; // Помечаем объект разрушенным

        canBeHit = false; // Запрещаем дальнейшие удары

        ApplyVisualState(); // Обновляем модели

        EnableObjectsAfterBreak(); // Включаем нужные объекты

        DisableObjectsAfterBreak(); // Выключаем нужные объекты

        if (makeNoiseOnBreak && noiseEmitter != null) // Если шум разрушения включен и NoiseEmitter есть
        {
            noiseEmitter.EmitNoise(breakNoisePower); // Создаем шум разрушения
        }

        if (onBreak != null) onBreak.Invoke(); // Вызываем событие разрушения

        if (showDebugLogs) // Если отладка включена
        {
            Debug.Log(gameObject.name + " разрушен"); // Пишем лог
        }
    }

    private void ApplyVisualState() // Метод переключения целой и сломанной модели
    {
        if (intactObject != null) // Если целая модель назначена
        {
            intactObject.SetActive(!isBroken); // Включаем ее только если объект не сломан
        }

        if (brokenObject != null) // Если сломанная модель назначена
        {
            brokenObject.SetActive(isBroken); // Включаем ее только если объект сломан
        }
    }

    private void EnableObjectsAfterBreak() // Метод включения объектов после разрушения
    {
        if (objectsToEnableAfterBreak == null) return; // Если список пустой, выходим

        for (int i = 0; i < objectsToEnableAfterBreak.Length; i++) // Проходим по списку включаемых объектов
        {
            if (objectsToEnableAfterBreak[i] != null) // Если элемент назначен
            {
                objectsToEnableAfterBreak[i].SetActive(true); // Включаем объект
            }
        }
    }

    private void DisableObjectsAfterBreak() // Метод выключения объектов после разрушения
    {
        if (objectsToDisableAfterBreak == null) return; // Если список пустой, выходим

        for (int i = 0; i < objectsToDisableAfterBreak.Length; i++) // Проходим по списку выключаемых объектов
        {
            if (objectsToDisableAfterBreak[i] != null) // Если элемент назначен
            {
                objectsToDisableAfterBreak[i].SetActive(false); // Выключаем объект
            }
        }
    }

    public void ResetBreakableObject() // Метод сброса для debug/reset
    {
        isBroken = false; // Сбрасываем разрушение

        canBeHit = true; // Снова разрешаем бить объект

        currentHits = 0; // Сбрасываем количество ударов

        lastHitTime = -999f; // Сбрасываем время последнего удара

        ApplyVisualState(); // Возвращаем визуальное состояние
    }
}