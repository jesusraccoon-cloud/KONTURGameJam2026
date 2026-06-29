using UnityEngine; // Подключаем Unity

public class ApartmentPowerController : MonoBehaviour // Контроллер включения и отключения конкретной квартиры
{
    [Header("Apartment Objects")] // Блок объектов квартиры
    public GameObject apartmentRoot; // Корень квартиры, который включается и отключается

    [Header("Door")] // Блок входной двери квартиры
    public UniversalDoor apartmentDoor; // Входная дверь квартиры

    [Header("Scenario Director")] // Блок сценарного режиссёра квартиры
    public ApartmentFinalSequence finalSequence; // Режиссёр этой квартиры

    [Header("Start State")] // Блок стартового состояния
    public bool startPoweredOn = false; // Включена ли квартира при старте

    [Header("Debug")] // Блок отладки
    public bool isPoweredOn = false; // Сейчас квартира включена
    public bool isPermanentlyDisabled = false; // Квартира окончательно отключена

    private void Start() // Запуск сцены
    {
        SetPower(startPoweredOn); // Ставим стартовое состояние
    }

    public void TogglePower() // Метод вызывается тумблером
    {
        if (isPermanentlyDisabled) // Если квартира уже окончательно отключена
        {
            Debug.Log("Тумблер больше не работает: квартира уже окончательно отключена"); // Debug
            return; // Выходим
        }

        if (isPoweredOn) // Если квартира включена
        {
            TryPowerOff(); // Пробуем отключить
        }
        else // Если квартира выключена
        {
            PowerOn(); // Включаем квартиру
        }
    }

    public void PowerOn() // Включить квартиру
    {
        if (isPermanentlyDisabled) return; // Если квартира окончательно отключена — выходим

        SetPower(true); // Включаем питание
    }

    public void TryPowerOff() // Попытка отключить квартиру
    {
        if (isPermanentlyDisabled) return; // Если уже отключена навсегда — выходим

        if (!CanPowerOff()) // Если условия отключения не выполнены
        {
            return; // Выходим
        }

        SetPower(false); // Отключаем квартиру

        isPermanentlyDisabled = true; // Запоминаем, что квартира больше не включается

        Debug.Log("Квартира окончательно отключена. Тумблер заблокирован."); // Debug
    }

    private bool CanPowerOff() // Проверка условий отключения
    {
        if (finalSequence == null) // Если режиссёр не назначен
        {
            Debug.LogWarning("Нельзя отключить квартиру: не назначен FinalSequence"); // Warning
            return false; // Запрещаем
        }

        if (!finalSequence.readyToDisableByTumbler) // Если сценарий ещё не разрешил отключение
        {
            Debug.Log("Нельзя отключить квартиру: сценарий ещё не завершён"); // Debug
            return false; // Запрещаем
        }

        if (apartmentDoor != null && apartmentDoor.IsOpen) // Если дверь открыта
        {
            Debug.Log("Нельзя отключить квартиру: дверь квартиры открыта"); // Debug
            return false; // Запрещаем
        }

        return true; // Можно отключать
    }

    private void SetPower(bool value) // Главный метод включения/отключения
    {
        isPoweredOn = value; // Запоминаем состояние

        if (apartmentDoor != null) // Если дверь назначена
        {
            apartmentDoor.SetLocked(!isPoweredOn); // Если квартира выключена — дверь заблокирована
        }

        if (apartmentRoot != null) // Если корень назначен
        {
            apartmentRoot.SetActive(isPoweredOn); // Включаем/отключаем квартиру
        }

        Debug.Log(isPoweredOn ? "Квартира включена через УМПСР" : "Квартира отключена через УМПСР"); // Debug
    }
}