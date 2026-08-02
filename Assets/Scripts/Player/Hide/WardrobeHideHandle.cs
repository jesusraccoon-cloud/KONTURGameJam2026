using UnityEngine; // Подключаем Unity-классы
using System.Collections; // Подключаем корутины

public class WardrobeHideHandle : MonoBehaviour // Скрипт конкретного шкафа
{
    [Header("Door Zone")] // Блок зон дверей
    public Collider[] doorInteractZones; // Несколько коллайдеров, через которые E открывает или закрывает соответствующую дверь шкафа

    [Header("Inside Hide Zone")] // Блок внутренней зоны пряток
    public Collider insideHideInteractZone; // Внутренний коллайдер, через который Q запускает прятки

    public bool insideZoneOnlyAfterPropDestroyed = true; // Включать внутреннюю зону только после destroyed стадии пропса

    public bool setInsideZoneObjectActive = true; // Включать/выключать весь объект внутренней зоны

    [Header("Hide System")] // Блок системы пряток
    public PlayerHideController playerHideController; // Контроллер пряток игрока

    public UniversalDoor[] wardrobeDoors; // Несколько основных дверей шкафа, соответствующих Door Interact Zones

    public UniversalDoor[] wardrobeDoorsToCloseAfterHide; // Дополнительные двери шкафа, которые надо открывать/закрывать при входе и выходе

    public UniversalDoor wardrobeDoor // Совместимость со старым PlayerHideController; в Inspector это поле не появляется
    {
        get
        {
            if (wardrobeDoors != null) // Сначала проверяем двери, через которые разрешены прятки
            {
                for (int i = 0; i < wardrobeDoors.Length; i++) // Ищем первую назначенную дверь
                {
                    if (wardrobeDoors[i] != null) return wardrobeDoors[i]; // Возвращаем первую найденную дверь
                }
            }

            if (wardrobeDoorsToCloseAfterHide != null) // Если первый массив пуст, проверяем двери последовательности пряток
            {
                for (int i = 0; i < wardrobeDoorsToCloseAfterHide.Length; i++) // Ищем первую назначенную дверь
                {
                    if (wardrobeDoorsToCloseAfterHide[i] != null) return wardrobeDoorsToCloseAfterHide[i]; // Возвращаем первую найденную дверь
                }
            }

            return null; // Если двери не назначены, возвращаем пустую ссылку
        }
    }

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

    [Header("Peek While Hidden")] // Настройки щели, пока игрок сидит в шкафу
    public bool usePeekPositionWhileHidden = true; // Оставлять двери приоткрытыми вместо полного закрытия

    public float[] peekAngles; // Угол щели для каждой двери из Wardrobe Doors To Close After Hide

    public float delayBeforePeek = 0.15f; // Пауза после полного закрытия перед приоткрыванием

    [Header("Door Safety")] // Блок надежности дверей
    public int doorActionAttempts = 8; // Сколько раз пробовать открыть/закрыть двери

    public float doorActionRetryDelay = 0.15f; // Пауза между попытками открыть/закрыть двери

    [Header("Debug")] // Блок отладки
    [SerializeField] private bool insideZoneUnlocked = false; // Показывает, включена ли внутренняя зона

    private bool missingPropWarningShown = false; // Защита от спама предупреждением

    private void Start() // Запуск при старте сцены
    {
        SetupDoorZones(); // Настраиваем все дверные зоны

        SetupInsideHideZone(); // Настраиваем внутреннюю зону

        RefreshInsideHideZone(true); // Сразу выставляем правильное состояние внутренней зоны
    }

    private void Update() // Каждый кадр
    {
        RefreshInsideHideZone(false); // Проверяем, можно ли включить внутреннюю зону
    }

    private void SetupDoorZones() // Настраивает все зоны дверей
    {
        if (doorInteractZones == null) return; // Если массив не назначен, выходим

        for (int i = 0; i < doorInteractZones.Length; i++) // Проходим по всем зонам
        {
            Collider currentZone = doorInteractZones[i]; // Получаем текущую зону

            if (currentZone == null) continue; // Если зона не назначена, пропускаем её

            currentZone.isTrigger = true; // Делаем зону триггером

            WardrobeHideInteractForwarder forwarder = GetOrCreateForwarder(currentZone); // Получаем передатчик

            forwarder.wardrobe = this; // Передаем ссылку на шкаф

            forwarder.zoneType = WardrobeHideZoneType.DoorZone; // Указываем тип зоны

            forwarder.doorIndex = i; // Связываем зону с дверью под тем же индексом
        }
    }

    private void SetupInsideHideZone() // Настраивает внутреннюю зону
    {
        if (insideHideInteractZone == null) return; // Если зона не назначена, выходим

        insideHideInteractZone.isTrigger = true; // Делаем зону триггером

        WardrobeHideInteractForwarder forwarder = GetOrCreateForwarder(insideHideInteractZone); // Получаем передатчик

        forwarder.wardrobe = this; // Передаем ссылку на шкаф

        forwarder.zoneType = WardrobeHideZoneType.InsideHideZone; // Указываем тип зоны

        forwarder.doorIndex = -1; // Внутренняя зона не относится к конкретной двери
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

    public void InteractWithDoor(int doorIndex) // E по конкретной дверной зоне
    {
        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок спрятан, дверь снаружи не трогаем

        if (playerHideController != null && playerHideController.IsBusy) return; // Если идет вход/выход, ничего не делаем

        if (wardrobeDoors == null) return; // Если массив дверей не назначен, выходим

        if (doorIndex < 0 || doorIndex >= wardrobeDoors.Length) return; // Если индекс неправильный, выходим

        UniversalDoor selectedDoor = wardrobeDoors[doorIndex]; // Получаем дверь с тем же индексом

        if (selectedDoor != null) selectedDoor.Interact(); // Открываем или закрываем выбранную дверь
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

    public IEnumerator CloseDoorsAfterHide() // После входа закрывает двери и оставляет щель
    {
        yield return new WaitForSeconds(doorCloseAfterHideDelay); // Ждем перед закрытием

        yield return StartCoroutine(CloseHideDoorsRoutine()); // Полностью закрываем двери пряток

        if (usePeekPositionWhileHidden) // Если щель включена
        {
            if (delayBeforePeek > 0f) yield return new WaitForSeconds(delayBeforePeek); // Ждем перед приоткрыванием

            SetHideDoorsToPeekPosition(); // Оставляем щель
        }
    }

    public IEnumerator OpenDoorsBeforeExit() // Полностью открывает двери перед выходом
    {
        yield return StartCoroutine(OpenHideDoorsRoutine()); // Открываем двери пряток

        yield return new WaitForSeconds(doorOpenBeforeExitDelay); // Ждем перед выходом
    }

    public IEnumerator CloseDoorsAfterExit() // Полностью закрывает двери после выхода
    {
        yield return new WaitForSeconds(doorCloseAfterExitDelay); // Ждем перед закрытием

        yield return StartCoroutine(CloseHideDoorsRoutine()); // Закрываем двери пряток
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

    private IEnumerator OpenHideDoorsRoutine() // Несколько раз открывает двери пряток
    {
        for (int i = 0; i < doorActionAttempts; i++)
        {
            OpenHideDoors();
            yield return new WaitForSeconds(doorActionRetryDelay);
        }
    }

    private IEnumerator CloseHideDoorsRoutine() // Несколько раз полностью закрывает двери пряток
    {
        for (int i = 0; i < doorActionAttempts; i++)
        {
            CloseHideDoors();
            yield return new WaitForSeconds(doorActionRetryDelay);
        }
    }

    private void OpenHideDoors() // Полностью открывает двери из Wardrobe Doors To Close After Hide
    {
        if (wardrobeDoorsToCloseAfterHide == null) return;

        for (int i = 0; i < wardrobeDoorsToCloseAfterHide.Length; i++)
        {
            UniversalDoor currentDoor = wardrobeDoorsToCloseAfterHide[i];
            if (currentDoor == null) continue;
            currentDoor.OpenDoor();
        }
    }

    private void CloseHideDoors() // Полностью закрывает двери из Wardrobe Doors To Close After Hide
    {
        if (wardrobeDoorsToCloseAfterHide == null) return;

        for (int i = 0; i < wardrobeDoorsToCloseAfterHide.Length; i++)
        {
            UniversalDoor currentDoor = wardrobeDoorsToCloseAfterHide[i];
            if (currentDoor == null) continue;
            currentDoor.CloseDoor();
        }
    }

    private void SetHideDoorsToPeekPosition() // Приоткрывает каждую дверь на свой угол
    {
        if (wardrobeDoorsToCloseAfterHide == null) return;

        for (int i = 0; i < wardrobeDoorsToCloseAfterHide.Length; i++)
        {
            UniversalDoor currentDoor = wardrobeDoorsToCloseAfterHide[i];
            if (currentDoor == null) continue;

            float angle = currentDoor.defaultPeekAngle;

            if (peekAngles != null && i < peekAngles.Length) angle = peekAngles[i];

            currentDoor.SetPeekPosition(angle);
        }
    }

    private void OpenAllDoors() // Открывает все двери шкафа
    {
        if (wardrobeDoors != null) // Проверяем основные двери
        {
            for (int i = 0; i < wardrobeDoors.Length; i++) // Проходим по основным дверям
            {
                if (wardrobeDoors[i] != null) wardrobeDoors[i].OpenDoor(); // Открываем дверь
            }
        }

        if (wardrobeDoorsToCloseAfterHide != null) // Проверяем дополнительный массив дверей
        {
            for (int i = 0; i < wardrobeDoorsToCloseAfterHide.Length; i++) // Проходим по дополнительным дверям
            {
                if (wardrobeDoorsToCloseAfterHide[i] != null) wardrobeDoorsToCloseAfterHide[i].OpenDoor(); // Открываем дверь
            }
        }
    }

    private void CloseAllDoors() // Закрывает все двери шкафа
    {
        if (wardrobeDoors != null) // Проверяем основные двери
        {
            for (int i = 0; i < wardrobeDoors.Length; i++) // Проходим по основным дверям
            {
                if (wardrobeDoors[i] != null) wardrobeDoors[i].CloseDoor(); // Закрываем дверь
            }
        }

        if (wardrobeDoorsToCloseAfterHide != null) // Проверяем дополнительный массив дверей
        {
            for (int i = 0; i < wardrobeDoorsToCloseAfterHide.Length; i++) // Проходим по дополнительным дверям
            {
                if (wardrobeDoorsToCloseAfterHide[i] != null) wardrobeDoorsToCloseAfterHide[i].CloseDoor(); // Закрываем дверь
            }
        }
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

    public int doorIndex = -1; // Индекс двери для дверной зоны

    public void Interact() // Нажатие E
    {
        if (wardrobe == null) wardrobe = GetComponentInParent<WardrobeHideHandle>(); // Ищем шкаф выше

        if (wardrobe == null) return; // Если шкафа нет, выходим

        if (zoneType == WardrobeHideZoneType.InsideHideZone) return; // Внутренняя зона игнорирует E

        if (zoneType == WardrobeHideZoneType.DoorZone) wardrobe.InteractWithDoor(doorIndex); // Дверная зона открывает/закрывает соответствующую дверь
    }

    public void TryHide() // Нажатие Q
    {
        if (wardrobe == null) wardrobe = GetComponentInParent<WardrobeHideHandle>(); // Ищем шкаф выше

        if (wardrobe == null) return; // Если шкафа нет, выходим

        wardrobe.TryHide(); // Передаем попытку спрятаться шкафу
    }
}