using UnityEngine; // Подключаем Unity-классы
using System.Collections; // Подключаем корутины

public class ApartmentFinalSequence : MonoBehaviour // Главный режиссер сценарных событий квартиры
{
    [Header("Early Event 4/6")] // Блок раннего события на 4/6 кассет или 3/3 шума
    public GameObject[] objectsToDisableAfterFourOfSix; // Объекты, которые нужно выключить после 4/6

    public GameObject[] objectsToEnableAfterFourOfSix; // Объекты, которые нужно включить после 4/6

    public float hallDoorBreakDelay = 1.5f; // Задержка перед событием 4/6

    public AudioSource hallDoorBreakAudioSource; // AudioSource для звука события 4/6

    public AudioClip hallDoorBreakSound; // Звук события 4/6

    private bool hallDoorBreakStarted = false; // Защита от повторного запуска события 4/6

    private bool hallDoorBreakCompleted = false; // Завершено ли состояние после 4/6

    [Header("Noise Alarm 3/3")] // Блок тревоги квартиры от шума
    public bool enableNoiseAlarmActivation = true; // Разрешить ли активацию 4/6 через шум

    public int noiseReactionThreshold = 4; // С какой силы шум считается реакцией монстра

    public int noiseReactionsToActivate = 3; // Сколько реакций нужно для досрочной активации

    public float noiseReactionCooldown = 2f; // Защита от слишком частого набора счетчика

    public int currentNoiseReactions = 0; // Текущий счетчик тревоги квартиры

    private float lastNoiseReactionTime = -999f; // Время последней засчитанной реакции

    [Header("Final Objects 6/6")] // Блок объектов финала 6/6
    public GameObject[] objectsToDisableAfterSixOfSix; // Объекты, которые нужно выключить после 6/6

    public GameObject[] objectsToEnableAfterSixOfSix; // Объекты, которые нужно включить после 6/6

    [Header("Closet Fall")] // Блок падения шкафа
    public ClosetPhysicalFall closetPhysicalFall; // Скрипт падения шкафа

    [Header("Bathroom Door")] // Блок двери ванной
    public UniversalDoor bathroomDoor; // Дверь ванной

    [Header("Bathroom Lock")] // Блок замка ванной
    public GameObject bathroomLockCollider; // Коллайдер замка ванной

    [Header("Monster")] // Блок монстра
    public GameObject monsterObject; // Объект монстра

    public MonsterAI monsterAI; // AI монстра

    public MonsterPatrol monsterPatrol; // Патруль монстра

    public Transform monsterExitBlockPoint; // Точка блокировки выхода

    [Header("Window First Hit Reaction")] // Блок реакции на первый удар по окну
    public GameObject finalNormalDoor; // Обычная дверь перед реакцией

    public GameObject finalBrokenDoor; // Сломанная дверь после реакции

    public Rigidbody fallenWardrobeRigidbody; // Rigidbody шкафа

    public Vector3 wardrobeForceDirection = new Vector3(1f, 0.2f, 0f); // Направление толчка шкафа

    public float wardrobeForce = 4f; // Сила толчка шкафа

    public float wardrobeTorque = 2f; // Сила вращения шкафа

    public Transform monsterAfterWindowHitPoint; // Точка монстра после удара по окну

    [Header("Triggers")] // Блок триггеров
    public GameObject hallReturnDeathTrigger; // Триггер смерти при возврате в коридор

    public GameObject kitchenFinalTrigger; // Триггер кухни

    public GameObject bathroomExitChaseTrigger; // Триггер выхода из ванной

    public GameObject apartmentExitCompleteTrigger; // Триггер завершения квартиры после выхода

    [Header("Apartment Completion")] // Блок завершения квартиры
    public UniversalDoor apartmentExitDoor; // Входная дверь квартиры

    public bool lockApartmentDoorAfterExit = true; // Блокировать ли дверь после выхода

    [Header("Elevator Ending")] // Блок концовки с лифтом
    public ElevatorEndingEvent elevatorEndingEvent; // Ивент лифта, который включается после 6/6

    [HideInInspector] public bool finalSequenceStarted = false; // Финал начался

    [HideInInspector] public bool apartmentCompleted = false; // Квартира завершена

    [HideInInspector] public bool readyToDisableByTumbler = false; // Можно отключить квартиру тумблером УМПСР

    private bool finalStarted = false; // Финал уже запускался

    private bool exitBlocked = false; // Выход уже блокировался

    private bool windowFirstHitReactionStarted = false; // Реакция на первый удар уже была

    private bool playerEscapedThroughWindow = false; // Игрок перелез через окно

    private bool bathroomExitTriggered = false; // Триггер выхода из ванной уже сработал

    private void Start() // При старте сцены
    {
        if (closetPhysicalFall != null) closetPhysicalFall.canFall = false; // Запрещаем падение шкафа до финала

        if (bathroomExitChaseTrigger != null) bathroomExitChaseTrigger.SetActive(false); // Выключаем триггер выхода из ванной

        if (apartmentExitCompleteTrigger != null) apartmentExitCompleteTrigger.SetActive(false); // Выключаем триггер завершения квартиры
    }

    public void RegisterNoiseReactionForEarlyEvent(int finalNoisePower) // Засчитать реакцию квартиры на шум
    {
        if (!enableNoiseAlarmActivation) return; // Если активация через шум выключена, выходим

        if (hallDoorBreakStarted) return; // Если событие 4/6 уже запущено, выходим

        if (finalStarted) return; // Если финал 6/6 уже запущен, выходим

        if (finalNoisePower < noiseReactionThreshold) return; // Если шум слабее порога, не считаем

        if (Time.time - lastNoiseReactionTime < noiseReactionCooldown) return; // Если прошло мало времени, не считаем

        lastNoiseReactionTime = Time.time; // Запоминаем время засчитанной реакции

        currentNoiseReactions = Mathf.Clamp(currentNoiseReactions + 1, 0, noiseReactionsToActivate); // Увеличиваем счетчик тревоги

        Debug.Log("Тревога квартиры: " + currentNoiseReactions + "/" + noiseReactionsToActivate + " | шум: " + finalNoisePower); // Пишем лог тревоги

        if (currentNoiseReactions >= noiseReactionsToActivate) // Если набрали нужное количество реакций
        {
            StartEarlyHallDoorBreakSequence(); // Запускаем событие 4/6
        }
    }

    public void StartEarlyHallDoorBreakSequence() // Запустить событие 4/6 или 3/3 шума
    {
        if (hallDoorBreakStarted) return; // Если событие уже запускалось, выходим

        hallDoorBreakStarted = true; // Запоминаем запуск события

        CompleteEarlyHallDoorBreakState(); // Применяем состояние после 4/6

        if (!finalStarted && monsterAI != null) monsterAI.ActivateMonster(); // Запускаем монстра, если финал еще не начался

        if (finalStarted && monsterAI != null && monsterExitBlockPoint != null) monsterAI.GoToPointAndStop(monsterExitBlockPoint); // Если финал уже идет, держим монстра у выхода
    }

    private IEnumerator EarlyHallDoorBreakRoutine() // Последовательность раннего события 4/6
    {
        if (monsterObject != null) monsterObject.SetActive(true); // Включаем монстра

        if (monsterPatrol != null) monsterPatrol.StopPatrol(); // Останавливаем патруль

        if (hallDoorBreakAudioSource != null && hallDoorBreakSound != null) hallDoorBreakAudioSource.PlayOneShot(hallDoorBreakSound); // Проигрываем звук события

        if (hallDoorBreakDelay > 0f) yield return new WaitForSeconds(hallDoorBreakDelay); // Ждем перед применением состояния

        CompleteEarlyHallDoorBreakState(); // Применяем состояние после 4/6

        if (!finalStarted && monsterAI != null) monsterAI.ActivateMonster(); // Запускаем монстра, если финал еще не начался

        if (finalStarted && monsterAI != null && monsterExitBlockPoint != null) monsterAI.GoToPointAndStop(monsterExitBlockPoint); // Если финал идет, отправляем монстра к выходу

        Debug.Log("4/6 событие выполнено"); // Пишем лог
    }

    private void CompleteEarlyHallDoorBreakState() // Мгновенно применить состояние после 4/6
    {
        if (hallDoorBreakCompleted) return; // Если состояние уже применено, выходим

        hallDoorBreakCompleted = true; // Запоминаем применение состояния

        hallDoorBreakStarted = true; // Считаем, что событие 4/6 уже было

        if (monsterObject != null) monsterObject.SetActive(true); // Включаем монстра

        SetObjectsActive(objectsToDisableAfterFourOfSix, false); // Выключаем дополнительные объекты после 4/6

        SetObjectsActive(objectsToEnableAfterFourOfSix, true); // Включаем дополнительные объекты после 4/6
    }

    public void StartFinalSequence() // Запуск финала
    {
        if (finalStarted) return; // Если финал уже был, выходим

        CompleteEarlyHallDoorBreakState(); // Если 6/6 запущен сразу, сначала применяем состояние после 4/6

        finalStarted = true; // Запоминаем запуск финала

        finalSequenceStarted = true; // Сообщаем другим скриптам, что финал начался

        SetObjectsActive(objectsToDisableAfterSixOfSix, false); // Выключаем дополнительные объекты после 6/6

        SetObjectsActive(objectsToEnableAfterSixOfSix, true); // Включаем дополнительные объекты после 6/6

        if (closetPhysicalFall != null) closetPhysicalFall.canFall = true; // Разрешаем падение шкафа

        if (bathroomDoor != null) // Проверяем, назначена ли дверь ванной
        {
            bathroomDoor.CloseDoor(); // Закрываем дверь ванной

            bathroomDoor.SetLocked(true); // Блокируем дверь ванной

            bathroomDoor.canMonsterOpen = false; // Запрещаем монстру открыть ванную
        }

        if (bathroomLockCollider != null) bathroomLockCollider.SetActive(true); // Включаем замок ванной

        if (hallReturnDeathTrigger != null) hallReturnDeathTrigger.SetActive(true); // Включаем триггер смерти

        if (kitchenFinalTrigger != null) kitchenFinalTrigger.SetActive(true); // Включаем кухонный триггер

        if (bathroomExitChaseTrigger != null) bathroomExitChaseTrigger.SetActive(false); // Пока держим триггер ванной выключенным

        if (apartmentExitCompleteTrigger != null) apartmentExitCompleteTrigger.SetActive(true); // Включаем триггер завершения квартиры

        if (elevatorEndingEvent != null) elevatorEndingEvent.UnlockElevatorEvent(); // Разблокируем лифтовую концовку

        BlockExitWithMonster(); // Отправляем монстра блокировать выход

        Debug.Log("Финальная последовательность квартиры запущена"); // Пишем лог
    }

    public void BlockExitWithMonster() // Монстр идет блокировать выход
    {
        if (exitBlocked) return; // Если выход уже блокировался, выходим

        exitBlocked = true; // Запоминаем блокировку

        if (monsterObject != null) monsterObject.SetActive(true); // Включаем монстра

        if (monsterPatrol != null) monsterPatrol.StopPatrol(); // Останавливаем патруль

        if (monsterAI != null && monsterExitBlockPoint != null) monsterAI.GoToPointAndStop(monsterExitBlockPoint); // Отправляем монстра к выходу

        Debug.Log("Монстр пошел блокировать выход"); // Пишем лог
    }

    public void OnFinalWindowFirstHit() // Первый удар по окну
    {
        if (windowFirstHitReactionStarted) return; // Если реакция уже была, выходим

        windowFirstHitReactionStarted = true; // Запоминаем реакцию

        if (finalNormalDoor != null) finalNormalDoor.SetActive(false); // Прячем обычную дверь

        if (finalBrokenDoor != null) finalBrokenDoor.SetActive(true); // Показываем сломанную дверь

        if (fallenWardrobeRigidbody != null) // Проверяем Rigidbody шкафа
        {
            fallenWardrobeRigidbody.isKinematic = false; // Включаем физику шкафа

            fallenWardrobeRigidbody.AddForce(wardrobeForceDirection.normalized * wardrobeForce, ForceMode.Impulse); // Толкаем шкаф

            fallenWardrobeRigidbody.AddTorque(Random.insideUnitSphere * wardrobeTorque, ForceMode.Impulse); // Добавляем вращение
        }

        if (monsterObject != null) monsterObject.SetActive(true); // Включаем монстра

        if (monsterAI != null && monsterAfterWindowHitPoint != null) monsterAI.StartFinalWindowThreat(monsterAfterWindowHitPoint); // Запускаем угрозу у окна

        Debug.Log("Первый удар по окну: монстр начал угрозу у окна"); // Пишем лог
    }

    public void OnPlayerEscapedThroughWindow() // Игрок перелез через окно
    {
        if (!finalSequenceStarted) return; // Если финал не начался, выходим

        playerEscapedThroughWindow = true; // Запоминаем, что игрок перелез

        if (bathroomExitChaseTrigger != null) bathroomExitChaseTrigger.SetActive(true); // Включаем триггер погони после ванной

        Debug.Log("Игрок перелез через окно, триггер выхода из ванной включен"); // Пишем лог
    }

    public void OnBathroomExitTrigger() // Игрок вышел из ванной
    {
        if (bathroomExitTriggered) return; // Если уже сработало, выходим

        if (!finalSequenceStarted) return; // Если финал не начался, выходим

        if (!playerEscapedThroughWindow) return; // Если игрок не перелез через окно, выходим

        bathroomExitTriggered = true; // Запоминаем срабатывание

        if (monsterObject != null) monsterObject.SetActive(true); // Включаем монстра

        if (monsterPatrol != null) monsterPatrol.StopPatrol(); // Останавливаем патруль

        if (monsterAI != null) monsterAI.ForceChasePlayer(); // Запускаем финальную погоню

        if (bathroomExitChaseTrigger != null) bathroomExitChaseTrigger.SetActive(false); // Выключаем триггер

        Debug.Log("Игрок вышел из ванной, монстр начал финальную погоню"); // Пишем лог
    }

    public void TryCompleteApartmentAfterExit() // Игрок вышел из квартиры после финала
    {
        if (apartmentCompleted) return; // Если квартира уже завершена, выходим

        if (!finalSequenceStarted) // Проверяем, начался ли финал
        {
            Debug.Log("Квартиру нельзя завершить: финал 6/6 еще не запущен"); // Пишем лог

            return; // Выходим
        }

        apartmentCompleted = true; // Запоминаем завершение квартиры

        readyToDisableByTumbler = true; // Разрешаем отключение через тумблер

        if (apartmentExitDoor != null) // Проверяем дверь квартиры
        {
            apartmentExitDoor.CloseDoor(); // Закрываем дверь квартиры

            if (lockApartmentDoorAfterExit) apartmentExitDoor.SetLocked(true); // Блокируем дверь квартиры
        }

        Debug.Log("Квартира завершена. Теперь ее можно отключить тумблером УМПСР"); // Пишем лог
    }

    private void SetObjectsActive(GameObject[] objects, bool activeState) // Метод включает или выключает список объектов
    {
        if (objects == null) return; // Если список не назначен, ничего не делаем

        for (int i = 0; i < objects.Length; i++) // Проходим по всем объектам списка
        {
            if (objects[i] == null) continue; // Если ячейка пустая, пропускаем

            objects[i].SetActive(activeState); // Включаем или выключаем объект
        }
    }
}