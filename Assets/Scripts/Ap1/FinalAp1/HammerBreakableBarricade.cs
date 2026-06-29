using UnityEngine; // Подключаем Unity-классы

public class HammerBreakableBarricade : MonoBehaviour, IHitInteractable // Баррикада, которую можно сбить ударом
{
    [Header("Final Sequence")] // Блок финальной последовательности
    public ApartmentFinalSequence finalSequence; // Ссылка на финальную фазу квартиры

    [Header("Hit Settings")] // Блок ударов
    public int hitsToBreak = 3; // Сколько ударов нужно для сбивания

    public float hitDelay = 0.3f; // Минимальная пауза между ударами

    [Header("Object Swap")] // Блок замены объектов
    public GameObject standingObject; // Стоящий шкаф до сбивания

    public GameObject fallenObject; // Упавший шкаф после сбивания

    [Header("Noise")] // Блок шума
    public NoiseEmitter noiseEmitter; // Источник шума баррикады

    [Range(1, 10)] public int hitNoisePower = 7; // Шум каждого удара

    [Range(1, 10)] public int breakNoisePower = 10; // Шум падения шкафа

    [Header("Optional")] // Дополнительные настройки
    public bool disableThisAfterBreak = true; // Отключить ли этот скрипт после сбивания

    private int currentHits = 0; // Сколько ударов уже нанесено

    private float lastHitTime = -999f; // Время последнего удара

    private bool isBroken = false; // Сбит ли шкаф

    private void Start() // Запуск сцены
    {
        if (noiseEmitter == null) // Если NoiseEmitter не назначен вручную
        {
            noiseEmitter = GetComponent<NoiseEmitter>(); // Пробуем найти NoiseEmitter на этом же объекте
        }
    }

    public void Hit() // Метод вызывается PlayerInteractor при ЛКМ
    {
        if (finalSequence == null) return; // Если финальная последовательность не назначена — выходим

        if (!finalSequence.finalSequenceStarted) return; // Если финальная фаза не началась — не ломаем

        if (isBroken) return; // Если уже сбито — выходим

        if (Time.time < lastHitTime + hitDelay) return; // Если удар слишком быстрый — игнорируем

        lastHitTime = Time.time; // Запоминаем время удара

        currentHits++; // Увеличиваем количество ударов

        if (noiseEmitter != null) // Если NoiseEmitter есть
        {
            noiseEmitter.EmitNoise(hitNoisePower); // Создаем шум удара
        }

        Debug.Log("Удар по баррикаде: " + currentHits + "/" + hitsToBreak); // Пишем прогресс

        if (currentHits >= hitsToBreak) // Если ударов достаточно
        {
            BreakBarricade(); // Сбиваем шкаф
        }
    }

    void BreakBarricade() // Метод сбивания шкафа
    {
        isBroken = true; // Запоминаем, что шкаф сбит

        if (standingObject != null) // Если стоящий шкаф назначен
        {
            standingObject.SetActive(false); // Выключаем стоящий шкаф
        }

        if (fallenObject != null) // Если упавший шкаф назначен
        {
            fallenObject.SetActive(true); // Включаем упавший шкаф
        }

        if (noiseEmitter != null) // Если NoiseEmitter есть
        {
            noiseEmitter.EmitNoise(breakNoisePower); // Создаем сильный шум падения
        }

        Debug.Log("Баррикада сбита"); // Пишем лог

        if (disableThisAfterBreak) // Если нужно отключить скрипт
        {
            enabled = false; // Отключаем этот скрипт
        }
    }
}