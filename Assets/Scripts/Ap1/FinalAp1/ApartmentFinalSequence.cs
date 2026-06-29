using UnityEngine; // Подключаем Unity-классы
using System.Collections; // Подключаем корутины

public class ApartmentFinalSequence : MonoBehaviour // Главный режиссёр сценарных событий квартиры
{
    [Header("Early Hall Door Break 4/6")] // Блок раннего события на 4/6 кассет или 3/3 шума
    public GameObject normalHallDoors; // Рабочие двери из прихожей в зал

    public GameObject brokenHallDoors; // Сломанные двери из прихожей в зал

    public float hallDoorBreakDelay = 1.5f; // Задержка перед поломкой дверей при обычном 4/6

    public AudioSource hallDoorBreakAudioSource; // AudioSource для звука выбивания дверей

    public AudioClip hallDoorBreakSound; // Звук выбивания дверей

    private bool hallDoorBreakStarted = false; // Защита от повторного запуска события 4/6

    private bool hallDoorBreakCompleted = false; // Завершено ли состояние после 4/6

    [Header("Noise Alarm 3/3")] // Блок тревоги квартиры от шума
    public bool enableNoiseAlarmActivation = true; // Разрешить ли активацию 4/6 через шум

    public int noiseReactionThreshold = 4; // С какой силы шум считается реакцией монстра

    public int noiseReactionsToActivate = 3; // Сколько реакций нужно для досрочной активации

    public float noiseReactionCooldown = 2f; // Защита от слишком частого набора счётчика

    public int currentNoiseReactions = 0; // Текущий счётчик тревоги квартиры

    private float lastNoiseReactionTime = -999f; // Время последней засчитанной реакции

    [Header("Final Objects")] // Блок финальных объектов
    public GameObject fallenWardrobe; // Упавший шкаф

    [Header("Closet Fall")] // Блок падения шкафа
    public ClosetPhysicalFall closetPhysicalFall; // Скрипт падения шкафа

    [Header("Room 1 Door Break")] // Блок поломки двери комнаты
    public GameObject normalRoomDoor; // Обычная дверь комнаты

    public GameObject brokenDoorOnFloor; // Выбитая дверь комнаты

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

        if (brokenHallDoors != null) brokenHallDoors.SetActive(false); // На старте сломанные двери зала выключены
    }

    public void RegisterNoiseReactionForEarlyEvent(int finalNoisePower) // Засчитать реакцию квартиры на шум
    {
        if (!enableNoiseAlarmActivation) return; // Если активация через шум выключена — выходим

        if (hallDoorBreakStarted) return; // Если событие 4/6 уже запущено — выходим

        if (finalStarted) return; // Если финал 6/6 уже запущен — выходим

        if (finalNoisePower < noiseReactionThreshold) return; // Если шум слабее порога — не считаем

        if (Time.time - lastNoiseReactionTime < noiseReactionCooldown) return; // Если слишком рано после прошлого шума — не считаем

        lastNoiseReactionTime = Time.time; // Запоминаем время засчитанной реакции

        currentNoiseReactions = Mathf.Clamp(currentNoiseReactions + 1, 0, noiseReactionsToActivate); // Увеличиваем счётчик 3/3

        Debug.Log("Тревога квартиры: " + currentNoiseReactions + "/" + noiseReactionsToActivate + " | шум: " + finalNoisePower); // Пишем лог тревоги

        if (currentNoiseReactions >= noiseReactionsToActivate) // Если набрали 3/3
        {
            StartEarlyHallDoorBreakSequence(); // Запускаем то же событие, что и на 4/6 кассет
        }
    }

    public void StartEarlyHallDoorBreakSequence() // Запустить событие выламывания дверей на 4/6 или 3/3 шума
    {
        if (hallDoorBreakStarted) return; // Если событие уже запускалось — выходим

        hallDoorBreakStarted = true; // Запоминаем, что событие запущено

        CompleteEarlyHallDoorBreakState(); // Переводим квартиру в состояние после 4/6

if (!finalStarted && monsterAI != null) monsterAI.ActivateMonster(); // Запускаем патруль только если финал 6/6 ещё не начался

if (finalStarted && monsterAI != null && monsterExitBlockPoint != null) monsterAI.GoToPointAndStop(monsterExitBlockPoint); // Если финал уже начался — держим монстра у выхода
    }

    private IEnumerator EarlyHallDoorBreakRoutine() // Последовательность ранней поломки дверей
{
    if (monsterObject != null) monsterObject.SetActive(true); // Включаем монстра

    if (monsterPatrol != null) monsterPatrol.StopPatrol(); // Останавливаем патруль перед сценарием

    if (hallDoorBreakAudioSource != null && hallDoorBreakSound != null) hallDoorBreakAudioSource.PlayOneShot(hallDoorBreakSound); // Проигрываем звук выбивания дверей

    if (hallDoorBreakDelay > 0f) yield return new WaitForSeconds(hallDoorBreakDelay); // Ждём перед поломкой дверей

    CompleteEarlyHallDoorBreakState(); // Переводим квартиру в состояние после 4/6

    if (!finalStarted && monsterAI != null) monsterAI.ActivateMonster(); // Запускаем патруль только если финал 6/6 ещё не начался

    if (finalStarted && monsterAI != null && monsterExitBlockPoint != null) monsterAI.GoToPointAndStop(monsterExitBlockPoint); // Если финал уже начался — отправляем монстра к выходу

    Debug.Log("4/6 событие: монстр выломал двери"); // Пишем лог
}

    private void CompleteEarlyHallDoorBreakState() // Мгновенно применить состояние после 4/6
    {
        if (hallDoorBreakCompleted) return; // Если состояние уже применено — выходим

        hallDoorBreakCompleted = true; // Запоминаем, что состояние после 4/6 применено

        hallDoorBreakStarted = true; // Считаем, что событие 4/6 уже было

        if (monsterObject != null) monsterObject.SetActive(true); // Гарантированно включаем монстра

        if (normalHallDoors != null) normalHallDoors.SetActive(false); // Выключаем рабочие двери

        if (brokenHallDoors != null) brokenHallDoors.SetActive(true); // Включаем сломанные двери
    }

    public void StartFinalSequence() // Запуск финала
    {
        if (finalStarted) return; // Если финал уже был — выходим

        CompleteEarlyHallDoorBreakState(); // Если 6/6 запущен сразу через debug — мгновенно подготавливаем состояние после 4/6

        finalStarted = true; // Запоминаем запуск финала

        finalSequenceStarted = true; // Сообщаем другим скриптам, что финал начался

        if (closetPhysicalFall != null) closetPhysicalFall.canFall = true; // Разрешаем падение шкафа

        if (fallenWardrobe != null) fallenWardrobe.SetActive(true); // Включаем упавший шкаф

        if (normalRoomDoor != null) normalRoomDoor.SetActive(false); // Прячем обычную дверь комнаты

        if (brokenDoorOnFloor != null) brokenDoorOnFloor.SetActive(true); // Показываем выбитую дверь

        if (bathroomDoor != null) // Если дверь ванной назначена
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

        BlockExitWithMonster(); // Отправляем монстра блокировать выход

        Debug.Log("Финальная последовательность квартиры запущена"); // Пишем лог
    }

    public void BlockExitWithMonster() // Монстр идёт блокировать выход
    {
        if (exitBlocked) return; // Если уже блокировал — выходим

        exitBlocked = true; // Запоминаем блокировку

        if (monsterObject != null) monsterObject.SetActive(true); // Включаем монстра

        if (monsterPatrol != null) monsterPatrol.StopPatrol(); // Полностью выключаем патруль

        if (monsterAI != null && monsterExitBlockPoint != null) monsterAI.GoToPointAndStop(monsterExitBlockPoint); // Отправляем монстра к выходу

        Debug.Log("Монстр пошёл блокировать выход"); // Пишем лог
    }

    public void OnFinalWindowFirstHit() // Первый удар по окну
    {
        if (windowFirstHitReactionStarted) return; // Если реакция уже была — выходим

        windowFirstHitReactionStarted = true; // Запоминаем реакцию

        if (finalNormalDoor != null) finalNormalDoor.SetActive(false); // Прячем обычную дверь

        if (finalBrokenDoor != null) finalBrokenDoor.SetActive(true); // Показываем сломанную дверь

        if (fallenWardrobeRigidbody != null) // Если Rigidbody шкафа назначен
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
        if (!finalSequenceStarted) return; // Если финал не начался — выходим

        playerEscapedThroughWindow = true; // Запоминаем, что игрок перелез

        if (bathroomExitChaseTrigger != null) bathroomExitChaseTrigger.SetActive(true); // Включаем триггер погони после ванной

        Debug.Log("Игрок перелез через окно, триггер выхода из ванной включён"); // Пишем лог
    }

    public void OnBathroomExitTrigger() // Игрок вышел из ванной
    {
        if (bathroomExitTriggered) return; // Если уже сработало — выходим

        if (!finalSequenceStarted) return; // Если финал не начался — выходим

        if (!playerEscapedThroughWindow) return; // Если игрок не перелез через окно — выходим

        bathroomExitTriggered = true; // Запоминаем срабатывание

        if (monsterObject != null) monsterObject.SetActive(true); // Гарантированно включаем монстра

        if (monsterPatrol != null) monsterPatrol.StopPatrol(); // Гарантированно выключаем патруль

        if (monsterAI != null) monsterAI.ForceChasePlayer(); // Запускаем постоянную финальную погоню

        if (bathroomExitChaseTrigger != null) bathroomExitChaseTrigger.SetActive(false); // Отключаем триггер

        Debug.Log("Игрок вышел из ванной, монстр начал финальную погоню"); // Пишем лог
    }

    public void TryCompleteApartmentAfterExit() // Игрок вышел из квартиры после финала
    {
        if (apartmentCompleted) return; // Если квартира уже завершена — выходим

        if (!finalSequenceStarted) // Если финал ещё не начался
        {
            Debug.Log("Квартиру нельзя завершить: финал 6/6 ещё не запущен"); // Пишем лог

            return; // Выходим
        }

        apartmentCompleted = true; // Запоминаем завершение квартиры

        readyToDisableByTumbler = true; // Разрешаем отключение через тумблер УМПСР

        if (apartmentExitDoor != null) // Если входная дверь квартиры назначена
        {
            apartmentExitDoor.CloseDoor(); // Закрываем дверь квартиры

            if (lockApartmentDoorAfterExit) apartmentExitDoor.SetLocked(true); // Блокируем дверь квартиры
        }

        Debug.Log("Квартира завершена. Теперь её можно отключить тумблером УМПСР"); // Пишем лог
    }
}