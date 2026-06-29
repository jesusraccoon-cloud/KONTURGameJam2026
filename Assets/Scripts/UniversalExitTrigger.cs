using UnityEngine; // Подключаем Unity

public class UniversalExitTrigger : MonoBehaviour // Универсальный сценарный триггер
{
    [Header("Trigger Settings")] // Настройки триггера
    public bool triggerOnce = true; // Сработать только один раз

    [Header("Objects")] // Объекты для включения и выключения
    public GameObject[] objectsToEnable; // Объекты для включения
    public GameObject[] objectsToDisable; // Объекты для выключения

    [Header("Door")] // Настройки двери
    public UniversalDoor doorToClose; // Дверь, которую нужно закрыть
    public bool lockDoorAfterClose = false; // Нужно ли заблокировать дверь после закрытия

    [Header("Final Sequence")] // Финальный сценарий квартиры
    public ApartmentFinalSequence finalSequence; // Режиссёр квартиры

    [Header("Events")] // Какие события вызвать
    public bool callBathroomExit = false; // Игрок вышел из ванной
    public bool tryCompleteApartment = false; // Игрок завершил квартиру

    private bool triggered = false; // Срабатывал ли триггер

    private void OnTriggerEnter(Collider other) // Когда кто-то входит в триггер
    {
        if (triggered && triggerOnce) return; // Если уже сработал и одноразовый — выходим

        if (!other.CompareTag("Player")) return; // Если вошёл не игрок — выходим

        foreach (GameObject obj in objectsToEnable) // Перебираем объекты для включения
        {
            if (obj != null) obj.SetActive(true); // Включаем объект, если он назначен
        }

        foreach (GameObject obj in objectsToDisable) // Перебираем объекты для выключения
        {
            if (obj != null) obj.SetActive(false); // Выключаем объект, если он назначен
        }

        if (doorToClose != null) // Если дверь назначена
        {
            doorToClose.CloseDoor(); // Закрываем дверь

            if (lockDoorAfterClose) doorToClose.SetLocked(true); // Блокируем дверь через метод UniversalDoor
        }

        if (finalSequence != null) // Если назначен режиссёр квартиры
        {
            if (callBathroomExit) finalSequence.OnBathroomExitTrigger(); // Сообщаем, что игрок вышел из ванной

            if (tryCompleteApartment) finalSequence.TryCompleteApartmentAfterExit(); // Пытаемся завершить квартиру
        }

        if (triggerOnce) // Если триггер одноразовый
        {
            triggered = true; // Запоминаем срабатывание

            if (!tryCompleteApartment) gameObject.SetActive(false); // Выключаем триггер, если это не триггер завершения квартиры
        }
    }
}