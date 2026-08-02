using UnityEngine; // Подключаем Unity-классы
using System.Collections; // Подключаем корутины

public class WardrobeHideHandle : MonoBehaviour // Скрипт конкретного шкафа
{
    [Header("Door Zone")] // Блок зон дверей

    public Collider[] doorInteractZones; // Несколько коллайдеров, через которые E открывает или закрывает соответствующую дверь шкафа

    [Header("Inside Hide Zone")] // Блок внутренней зоны пряток

    public Collider insideHideInteractZone; // Внутренний коллайдер, через который Q запускает прятки

    public bool insideZoneOnlyAfterPropDestroyed = true; // Включать внутреннюю зону только после destroyed стадии пропса

    public bool setInsideZoneObjectActive = true; // Включать или выключать весь объект внутренней зоны

    [Header("Hide System")] // Блок системы пряток

    public PlayerHideController playerHideController; // Контроллер пряток игрока

    public UniversalDoor[] wardrobeDoors; // Несколько основных дверей шкафа, соответствующих Door Interact Zones

    public UniversalDoor[] wardrobeDoorsToCloseAfterHide; // Дополнительные двери, которые открываются и закрываются при входе и выходе

    public UniversalDoor wardrobeDoor // Старое имя сохранено для совместимости с PlayerHideController
    {
        get // Возвращаем одну основную дверь старым скриптам
        {
            if (wardrobeDoors == null) return null; // Если массива нет, возвращаем пустую ссылку

            for (int i = 0; i < wardrobeDoors.Length; i++) // Перебираем все основные двери
            {
                if (wardrobeDoors[i] != null) return wardrobeDoors[i]; // Возвращаем первую назначенную дверь
            }

            return null; // Если все элементы пустые, возвращаем null
        }

        set // Позволяем старому коду при необходимости назначить основную дверь
        {
            if (wardrobeDoors == null || wardrobeDoors.Length == 0) // Проверяем наличие первого элемента
            {
                wardrobeDoors = new UniversalDoor[1]; // Создаём массив с одним элементом
            }

            wardrobeDoors[0] = value; // Назначаем переданную дверь первым элементом массива
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

    [Header("Door Safety")] // Блок надёжности дверей

    public int doorActionAttempts = 8; // Сколько раз пробовать открыть или закрыть двери

    public float doorActionRetryDelay = 0.15f; // Пауза между попытками открыть или закрыть двери

    [Header("Debug")] // Блок отладки

    [SerializeField] private bool insideZoneUnlocked = false; // Показывает, включена ли внутренняя зона

    private bool missingPropWarningShown = false; // Защита от повторяющегося предупреждения

    private void Start() // Запускается при старте сцены
    {
        SetupDoorZones(); // Настраиваем все дверные зоны

        SetupInsideHideZone(); // Настраиваем внутреннюю зону

        RefreshInsideHideZone(true); // Сразу устанавливаем правильное состояние внутренней зоны
    }

    private void Update() // Вызывается каждый кадр
    {
        RefreshInsideHideZone(false); // Проверяем, можно ли включить внутреннюю зону
    }

    private void SetupDoorZones() // Настраивает все зоны дверей
    {
        if (doorInteractZones == null) return; // Если массив отсутствует, выходим

        for (int i = 0; i < doorInteractZones.Length; i++) // Перебираем все дверные зоны
        {
            Collider currentZone = doorInteractZones[i]; // Получаем текущую зону

            if (currentZone == null) continue; // Пропускаем пустой элемент

            currentZone.isTrigger = true; // Делаем коллайдер триггером

            WardrobeHideInteractForwarder forwarder = GetOrCreateForwarder(currentZone); // Получаем передатчик взаимодействия

            forwarder.wardrobe = this; // Передаём ссылку на шкаф

            forwarder.zoneType = WardrobeHideZoneType.DoorZone; // Указываем, что это дверная зона

            forwarder.doorIndex = i; // Связываем зону с дверью того же индекса
        }
    }

    private void SetupInsideHideZone() // Настраивает внутреннюю зону
    {
        if (insideHideInteractZone == null) return; // Если зона не назначена, выходим

        insideHideInteractZone.isTrigger = true; // Делаем внутренний коллайдер триггером

        WardrobeHideInteractForwarder forwarder = GetOrCreateForwarder(insideHideInteractZone); // Получаем передатчик

        forwarder.wardrobe = this; // Передаём ссылку на шкаф

        forwarder.zoneType = WardrobeHideZoneType.InsideHideZone; // Указываем тип внутренней зоны

        forwarder.doorIndex = -1; // Внутренняя зона не относится к одной двери
    }

    private WardrobeHideInteractForwarder GetOrCreateForwarder(Collider zone) // Получает или создаёт передатчик
    {
        WardrobeHideInteractForwarder forwarder = zone.GetComponent<WardrobeHideInteractForwarder>(); // Ищем существующий передатчик

        if (forwarder == null) // Проверяем, найден ли компонент
        {
            forwarder = zone.gameObject.AddComponent<WardrobeHideInteractForwarder>(); // Добавляем передатчик
        }

        return forwarder; // Возвращаем готовый компонент
    }

    private void RefreshInsideHideZone(bool forceRefresh) // Включает или выключает внутреннюю зону
    {
        if (insideHideInteractZone == null) return; // Если зоны нет, выходим

        bool shouldBeEnabled = true; // По умолчанию зона доступна

        if (insideZoneOnlyAfterPropDestroyed == true) // Проверяем условие уничтожения пропса
        {
            shouldBeEnabled = CanPlayerHide(); // Определяем доступность пряток
        }

        if (forceRefresh == false && insideZoneUnlocked == shouldBeEnabled) return; // Не повторяем уже применённое состояние

        insideZoneUnlocked = shouldBeEnabled; // Запоминаем новое состояние

        if (setInsideZoneObjectActive == true) // Проверяем способ отключения зоны
        {
            insideHideInteractZone.gameObject.SetActive(shouldBeEnabled); // Переключаем весь объект зоны
        }
        else // Если весь объект переключать не нужно
        {
            insideHideInteractZone.enabled = shouldBeEnabled; // Переключаем только Collider
        }
    }

    public void InteractWithDoor(int doorIndex) // Вызывается при нажатии E по конкретной зоне двери
    {
        if (playerHideController != null && playerHideController.isHidden) return; // Спрятанный игрок не управляет дверью снаружи

        if (playerHideController != null && playerHideController.IsBusy) return; // Во время входа или выхода команды блокируются

        if (wardrobeDoors == null) return; // Если массива дверей нет, выходим

        if (doorIndex < 0 || doorIndex >= wardrobeDoors.Length) // Проверяем правильность индекса
        {
            Debug.LogWarning(
                "WardrobeHideHandle: неверный индекс двери " + doorIndex + ".",
                gameObject
            ); // Выводим понятное предупреждение

            return; // Не продолжаем
        }

        UniversalDoor selectedDoor = wardrobeDoors[doorIndex]; // Получаем дверь с таким же индексом

        if (selectedDoor == null) // Проверяем назначение двери
        {
            Debug.LogWarning(
                "WardrobeHideHandle: Wardrobe Doors, Element " + doorIndex + " не назначен.",
                gameObject
            ); // Показываем пустой элемент

            return; // Не продолжаем
        }

        selectedDoor.Interact(); // Открываем или закрываем выбранную дверь
    }

    public void TryHide() // Запускает попытку спрятаться
    {
        if (playerHideController == null) // Проверяем ссылку на игрока
        {
            Debug.LogWarning(
                "WardrobeHideHandle: Player Hide Controller не назначен.",
                gameObject
            ); // Показываем ошибку настройки

            return; // Не продолжаем
        }

        if (playerHideController.IsBusy) return; // Не запускаем повторный вход или выход

        if (CanPlayerHide() == false) return; // Не разрешаем прятки до выполнения условия

        playerHideController.TryEnterWardrobe(this); // Передаём шкаф контроллеру игрока
    }

    public bool CanPlayerHide() // Проверяет возможность спрятаться
    {
        if (requirePropDestroyed == false) return true; // Если условие отключено, разрешаем прятки

        if (requiredDestroyedProp == null) // Проверяем назначение пропса
        {
            if (missingPropWarningShown == false) // Не повторяем сообщение каждый кадр
            {
                Debug.LogWarning(
                    "WardrobeHideHandle: Required Destroyed Prop не назначен.",
                    gameObject
                ); // Показываем предупреждение
            }

            missingPropWarningShown = true; // Запоминаем показ сообщения

            return false; // Без пропса прятки недоступны
        }

        return requiredDestroyedProp.IsDestroyed; // Разрешаем прятки после стадии Destroyed
    }

    public IEnumerator OpenDoorsBeforeHide() // Открывает двери перед входом
    {
        yield return StartCoroutine(OpenAllDoorsRoutine()); // Надёжно открываем все двери

        yield return new WaitForSeconds(doorOpenBeforeHideDelay); // Ждём перед входом игрока
    }

    public IEnumerator CloseDoorsAfterHide() // Закрывает двери после входа
    {
        yield return new WaitForSeconds(doorCloseAfterHideDelay); // Ждём перед закрытием

        yield return StartCoroutine(CloseAllDoorsRoutine()); // Надёжно закрываем все двери
    }

    public IEnumerator OpenDoorsBeforeExit() // Открывает двери перед выходом
    {
        yield return StartCoroutine(OpenAllDoorsRoutine()); // Надёжно открываем двери

        yield return new WaitForSeconds(doorOpenBeforeExitDelay); // Ждём перед выходом
    }

    public IEnumerator CloseDoorsAfterExit() // Закрывает двери после выхода
    {
        yield return new WaitForSeconds(doorCloseAfterExitDelay); // Ждём перед закрытием

        yield return StartCoroutine(CloseAllDoorsRoutine()); // Надёжно закрываем двери
    }

    private IEnumerator OpenAllDoorsRoutine() // Несколько раз пробует открыть все двери
    {
        for (int i = 0; i < doorActionAttempts; i++) // Выполняем нужное количество попыток
        {
            OpenAllDoors(); // Пробуем открыть двери

            yield return new WaitForSeconds(doorActionRetryDelay); // Ждём перед повтором
        }
    }

    private IEnumerator CloseAllDoorsRoutine() // Несколько раз пробует закрыть все двери
    {
        for (int i = 0; i < doorActionAttempts; i++) // Выполняем нужное количество попыток
        {
            CloseAllDoors(); // Пробуем закрыть двери

            yield return new WaitForSeconds(doorActionRetryDelay); // Ждём перед повтором
        }
    }

    private void OpenAllDoors() // Открывает все двери шкафа
    {
        if (wardrobeDoors != null) // Проверяем основные двери
        {
            for (int i = 0; i < wardrobeDoors.Length; i++) // Перебираем основные двери
            {
                if (wardrobeDoors[i] != null) // Проверяем текущий элемент
                {
                    wardrobeDoors[i].OpenDoor(); // Открываем текущую дверь
                }
            }
        }

        if (wardrobeDoorsToCloseAfterHide != null) // Проверяем дополнительные двери
        {
            for (int i = 0; i < wardrobeDoorsToCloseAfterHide.Length; i++) // Перебираем дополнительные двери
            {
                if (wardrobeDoorsToCloseAfterHide[i] != null) // Проверяем текущий элемент
                {
                    wardrobeDoorsToCloseAfterHide[i].OpenDoor(); // Открываем текущую дверь
                }
            }
        }
    }

    private void CloseAllDoors() // Закрывает все двери шкафа
    {
        if (wardrobeDoors != null) // Проверяем основные двери
        {
            for (int i = 0; i < wardrobeDoors.Length; i++) // Перебираем основные двери
            {
                if (wardrobeDoors[i] != null) // Проверяем текущий элемент
                {
                    wardrobeDoors[i].CloseDoor(); // Закрываем текущую дверь
                }
            }
        }

        if (wardrobeDoorsToCloseAfterHide != null) // Проверяем дополнительные двери
        {
            for (int i = 0; i < wardrobeDoorsToCloseAfterHide.Length; i++) // Перебираем дополнительные двери
            {
                if (wardrobeDoorsToCloseAfterHide[i] != null) // Проверяем текущий элемент
                {
                    wardrobeDoorsToCloseAfterHide[i].CloseDoor(); // Закрываем текущую дверь
                }
            }
        }
    }
}

public enum WardrobeHideZoneType // Тип зоны шкафа
{
    DoorZone, // Зона конкретной двери

    InsideHideZone // Внутренняя зона пряток
}

public class WardrobeHideInteractForwarder : MonoBehaviour, IInteractable // Передатчик взаимодействия зоны шкафа
{
    public WardrobeHideHandle wardrobe; // Ссылка на корневой скрипт шкафа

    public WardrobeHideZoneType zoneType = WardrobeHideZoneType.DoorZone; // Тип текущей зоны

    public int doorIndex = -1; // Индекс двери, связанной с этой зоной

    public void Interact() // Вызывается при нажатии E
    {
        if (wardrobe == null) // Проверяем ссылку на шкаф
        {
            wardrobe = GetComponentInParent<WardrobeHideHandle>(); // Ищем шкаф среди родителей
        }

        if (wardrobe == null) return; // Без шкафа взаимодействие невозможно

        if (zoneType == WardrobeHideZoneType.InsideHideZone) return; // Внутренняя зона игнорирует E

        if (zoneType == WardrobeHideZoneType.DoorZone) // Проверяем дверную зону
        {
            wardrobe.InteractWithDoor(doorIndex); // Передаём индекс соответствующей двери
        }
    }

    public void TryHide() // Вызывается при нажатии Q
    {
        if (wardrobe == null) // Проверяем ссылку на шкаф
        {
            wardrobe = GetComponentInParent<WardrobeHideHandle>(); // Ищем шкаф среди родителей
        }

        if (wardrobe == null) return; // Без шкафа прятки невозможны

        wardrobe.TryHide(); // Передаём попытку спрятаться
    }
}