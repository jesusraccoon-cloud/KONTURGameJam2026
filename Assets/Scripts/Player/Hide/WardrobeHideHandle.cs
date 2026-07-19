using UnityEngine; // Подключаем Unity-классы
using System.Collections; // Подключаем корутины

public class WardrobeHideHandle : MonoBehaviour // Скрипт конкретного шкафа
{
    [Header("Door Zone")] // Блок зоны двери
    public Collider doorInteractZone; // Коллайдер, через который E открывает или закрывает дверь шкафа

    [Header("Inside Hide Zone")] // Блок внутренней зоны пряток
    public Collider insideHideInteractZone; // Внутренний коллайдер, через который Q запускает прятки

    public bool insideZoneOnlyAfterPropDestroyed = true; // Включать внутреннюю зону только после destroyed стадии пропса

    public bool setInsideZoneObjectActive = true; // Включать/выключать весь объект внутренней зоны

    [Header("Hide System")] // Блок системы пряток
    public PlayerHideController playerHideController; // Контроллер пряток игрока

    public UniversalDoor wardrobeDoor; // Основная дверь шкафа

    public UniversalDoor[] wardrobeDoorsToCloseAfterHide; // Все двери шкафа, которые надо открывать/закрывать при входе и выходе

    public Transform hidePoint; // Точка внутри шкафа

    public Transform exitPoint; // Точка выхода перед шкафом

    [Header("Required Destroyed Prop")] // Блок условия с пропсом
    public ThreeStageInteractableObject requiredDestroyedProp; // Пропс, который должен быть уничтожен

    public bool requirePropDestroyed = true; // Нужно ли ждать destroyed стадии пропса

    [Header("Door Timings")] // Блок задержек дверей
    public float doorOpenBeforeHideDelay = 0.6f; // Пауза после открытия дверей перед входом игрока

    public float doorCloseAfterHideDelay = 0.4f; // Пауза перед закрытием дверей после входа

    public float doorOpenBeforeExitDelay = 0.4f; // Пауза после открытия дверей перед выходом игрока

    public float doorCloseAfterExitDelay = 0.4f; // Пауза перед закрытием дверей после выхода

    [Header("Door Safety")] // Блок надежности дверей
    public int doorActionAttempts = 8; // Сколько раз пробовать открыть/закрыть двери

    public float doorActionRetryDelay = 0.15f; // Пауза между попытками открыть/закрыть двери

    [Header("Debug")] // Блок отладки
    [SerializeField] private bool insideZoneUnlocked = false; // Показывает, включена ли внутренняя зона

    private bool missingPropWarningShown = false; // Защита от спама предупреждением

    private void Start() // Запуск при старте сцены
    {
        SetupDoorZone(); // Настраиваем дверную зону

        SetupInsideHideZone(); // Настраиваем внутреннюю зону

        RefreshInsideHideZone(true); // Сразу выставляем правильное состояние внутренней зоны
    }

    private void Update() // Каждый кадр
    {
        RefreshInsideHideZone(false); // Проверяем, можно ли включить внутреннюю зону
    }

    private void SetupDoorZone() // Настраивает зону двери
    {
        if (doorInteractZone == null) return; // Если зона не назначена, выходим

        doorInteractZone.isTrigger = true; // Делаем зону триггером

        WardrobeHideInteractForwarder forwarder = GetOrCreateForwarder(doorInteractZone); // Получаем передатчик

        forwarder.wardrobe = this; // Передаем ссылку на шкаф

        forwarder.zoneType = WardrobeHideZoneType.DoorZone; // Указываем тип зоны
    }

    private void SetupInsideHideZone() // Настраивает внутреннюю зону
    {
        if (insideHideInteractZone == null) return; // Если зона не назначена, выходим

        insideHideInteractZone.isTrigger = true; // Делаем зону триггером

        WardrobeHideInteractForwarder forwarder = GetOrCreateForwarder(insideHideInteractZone); // Получаем передатчик

        forwarder.wardrobe = this; // Передаем ссылку на шкаф

        forwarder.zoneType = WardrobeHideZoneType.InsideHideZone; // Указываем тип зоны
    }

    private WardrobeHideInteractForwarder GetOrCreateForwarder(Collider zone) // Получает или создает передатчик
    {
        WardrobeHideInteractForwarder forwarder = zone.GetComponent<WardrobeHideInteractForwarder>(); // Ищем передатчик

        if (forwarder == null) forwarder = zone.gameObject.AddComponent<WardrobeHideInteractForwarder>(); // Если нет, добавляем

        return forwarder; // Возвращаем передатчик
    }

    private void RefreshInsideHideZone(bool forceRefresh) // Включает или выключает внутреннюю зону
    {
        if (insideHideInteractZone == null) return; // Если зоны нет, выходим

        bool shouldBeEnabled = true; // По умолчанию зона включена

        if (insideZoneOnlyAfterPropDestroyed == true) shouldBeEnabled = CanPlayerHide(); // Если нужно ждать пропс, проверяем условие

        if (forceRefresh == false && insideZoneUnlocked == shouldBeEnabled) return; // Если состояние не изменилось, выходим

        insideZoneUnlocked = shouldBeEnabled; // Запоминаем состояние

        if (setInsideZoneObjectActive == true) insideHideInteractZone.gameObject.SetActive(shouldBeEnabled); // Включаем/выключаем объект

        if (setInsideZoneObjectActive == false) insideHideInteractZone.enabled = shouldBeEnabled; // Или только коллайдер
    }

    public void Interact() // E по дверной зоне
    {
        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан, дверь снаружи не трогаем

        if (playerHideController != null && playerHideController.IsBusy) return; // Если идет вход/выход, ничего не делаем

        if (wardrobeDoor != null) wardrobeDoor.Interact(); // Открываем или закрываем основную дверь
    }

    public void TryHide() // Q по зоне пряток
    {
        if (playerHideController == null) return; // Если контроллер игрока не назначен, выходим

        playerHideController.TryEnterWardrobe(this); // Передаем шкаф контроллеру игрока
    }

    public bool CanPlayerHide() // Проверяет, можно ли прятаться в этот шкаф
    {
        if (requirePropDestroyed == false) return true; // Если пропс не нужен, разрешаем

        if (requiredDestroyedProp == null) // Если пропс не назначен
        {
            if (missingPropWarningShown == false) Debug.LogWarning("WardrobeHideHandle: Required Destroyed Prop не назначен.", this); // Пишем предупреждение

            missingPropWarningShown = true; // Запоминаем, что уже предупредили

            return false; // Запрещаем прятки
        }

        return requiredDestroyedProp.IsDestroyed; // Разрешаем только после destroyed
    }

    public IEnumerator OpenDoorsBeforeHide() // Открывает двери перед входом
    {
        yield return StartCoroutine(OpenAllDoorsRoutine()); // Надежно открываем двери

        yield return new WaitForSeconds(doorOpenBeforeHideDelay); // Ждем перед входом
    }

    public IEnumerator CloseDoorsAfterHide() // Закрывает двери после входа
    {
        yield return new WaitForSeconds(doorCloseAfterHideDelay); // Ждем перед закрытием

        yield return StartCoroutine(CloseAllDoorsRoutine()); // Надежно закрываем двери
    }

    public IEnumerator OpenDoorsBeforeExit() // Открывает двери перед выходом
    {
        yield return StartCoroutine(OpenAllDoorsRoutine()); // Надежно открываем двери

        yield return new WaitForSeconds(doorOpenBeforeExitDelay); // Ждем перед телепортом наружу
    }

    public IEnumerator CloseDoorsAfterExit() // Закрывает двери после выхода
    {
        yield return new WaitForSeconds(doorCloseAfterExitDelay); // Ждем перед закрытием

        yield return StartCoroutine(CloseAllDoorsRoutine()); // Надежно закрываем двери
    }

    private IEnumerator OpenAllDoorsRoutine() // Несколько раз пробует открыть все двери
    {
        for (int i = 0; i < doorActionAttempts; i++) // Повторяем попытки
        {
            OpenAllDoors(); // Пробуем открыть двери

            yield return new WaitForSeconds(doorActionRetryDelay); // Ждем перед следующей попыткой
        }
    }

    private IEnumerator CloseAllDoorsRoutine() // Несколько раз пробует закрыть все двери
    {
        for (int i = 0; i < doorActionAttempts; i++) // Повторяем попытки
        {
            CloseAllDoors(); // Пробуем закрыть двери

            yield return new WaitForSeconds(doorActionRetryDelay); // Ждем перед следующей попыткой
        }
    }

    private void OpenAllDoors() // Открывает все двери шкафа
    {
        if (wardrobeDoorsToCloseAfterHide != null) // Проверяем массив дверей
        {
            for (int i = 0; i < wardrobeDoorsToCloseAfterHide.Length; i++) // Проходим по дверям
            {
                if (wardrobeDoorsToCloseAfterHide[i] != null) wardrobeDoorsToCloseAfterHide[i].OpenDoor(); // Открываем дверь
            }
        }

        if (wardrobeDoor != null) wardrobeDoor.OpenDoor(); // Открываем основную дверь
    }

    private void CloseAllDoors() // Закрывает все двери шкафа
    {
        if (wardrobeDoorsToCloseAfterHide != null) // Проверяем массив дверей
        {
            for (int i = 0; i < wardrobeDoorsToCloseAfterHide.Length; i++) // Проходим по дверям
            {
                if (wardrobeDoorsToCloseAfterHide[i] != null) wardrobeDoorsToCloseAfterHide[i].CloseDoor(); // Закрываем дверь
            }
        }

        if (wardrobeDoor != null) wardrobeDoor.CloseDoor(); // Закрываем основную дверь
    }
}

public enum WardrobeHideZoneType // Тип зоны шкафа
{
    DoorZone, // Зона двери
    InsideHideZone // Внутренняя зона пряток
}

public class WardrobeHideInteractForwarder : MonoBehaviour, IInteractable // Передатчик взаимодействия зоны шкафа
{
    public WardrobeHideHandle wardrobe; // Ссылка на шкаф

    public WardrobeHideZoneType zoneType = WardrobeHideZoneType.DoorZone; // Тип зоны

    public void Interact() // Нажатие E
    {
        if (wardrobe == null) wardrobe = GetComponentInParent<WardrobeHideHandle>(); // Ищем шкаф выше

        if (wardrobe == null) return; // Если шкафа нет, выходим

        if (zoneType == WardrobeHideZoneType.InsideHideZone) return; // Внутренняя зона игнорирует E

        if (zoneType == WardrobeHideZoneType.DoorZone) wardrobe.Interact(); // Дверная зона открывает/закрывает дверь
    }

    public void TryHide() // Нажатие Q
    {
        if (wardrobe == null) wardrobe = GetComponentInParent<WardrobeHideHandle>(); // Ищем шкаф выше

        if (wardrobe == null) return; // Если шкафа нет, выходим

        wardrobe.TryHide(); // Передаем попытку спрятаться шкафу
    }
}