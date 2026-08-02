using UnityEngine; // Подключаем Unity-классы
using UnityEngine.Events; // Подключаем UnityEvent (для звуковых хуков)
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

    [Header("Kitchen Barricade")] // Блок трёхстадийной кухонной баррикады
    public ThreeStageInteractableObject kitchenBarricade; // Баррикада: 0 — целая, 1 — упавшая, 2 — уничтоженная

    [Header("Bathroom Door")] // Блок двери ванной
    public UniversalDoor bathroomDoor; // Дверь ванной

    [Header("Bathroom Door Stage Watch")] // Проверка стадии событийной двери ванной
    public ThreeStageInteractableObject bathroomDoorStageObject; // Объект EventBathroomDoor с ThreeStageInteractableObject

    [Min(1)]
    public int bathroomDoorBrokenStage = 1; // Стадия Broken, после которой монстр начинает погоню

    [Min(0.02f)]
    public float bathroomDoorStageCheckInterval = 0.1f; // Как часто проверять CurrentStage двери ванной

    [Min(0.1f)]
    public float monsterWindowFinalReachDistance = 0.6f; // Дистанция, на которой монстр считается вставшим у окна

    [Min(1f)]
    public float monsterWindowFinalReachTimeout = 20f; // Максимальное время ожидания монстра у конечной точки окна

    [Header("Monster")] // Блок монстра
    public GameObject monsterObject; // Объект монстра

    public MonsterAI monsterAI; // AI монстра

    public MonsterPatrol monsterPatrol; // Патруль монстра

    public Transform monsterExitBlockPoint; // Точка блокировки выхода

    [Header("Window First Hit Reaction")] // Блок реакции на первый удар по окну
    public GameObject finalNormalDoor; // Обычная дверь перед реакцией

    public GameObject finalBrokenDoor; // Сломанная дверь после реакции

    public Transform monsterWindowRoutePoint; // Обязательная промежуточная точка сразу за разрушенной баррикадой

    public Transform monsterAfterWindowHitPoint; // Конечная точка перед окном, где монстр должен остаться

    [Min(0.1f)]
    public float monsterWindowRouteReachDistance = 0.5f; // Дистанция, на которой промежуточная точка считается достигнутой

    [Min(0.1f)]
    public float monsterWindowRouteTimeout = 12f; // Максимальное время ожидания достижения промежуточной точки

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

    [Header("Audio Hooks (FMOD)")] // Звуковые хуки — вешай сюда SC_SoundCue3D.Play() из нужной точки
    public UnityEvent onHallDoorBreak; // Выбивание двери 4/6 (3D-звук от места двери)

    public UnityEvent onFinalStart; // Старт финала 6/6 (3D-звук)

    [HideInInspector] public bool finalSequenceStarted = false; // Финал начался

    [HideInInspector] public bool apartmentCompleted = false; // Квартира завершена

    [HideInInspector] public bool readyToDisableByTumbler = false; // Можно отключить квартиру тумблером УМПСР

    private bool finalStarted = false; // Финал уже запускался

    private bool exitBlocked = false; // Выход уже блокировался

    private bool windowFirstHitReactionStarted = false; // Реакция на первый удар уже была

    private bool playerEscapedThroughWindow = false; // Игрок перелез через окно

    private bool bathroomExitTriggered = false; // Триггер выхода из ванной уже сработал

    private bool monsterReachedWindowPoint = false; // Монстр действительно дошёл до конечной точки у окна

    private bool bathroomDoorBroken = false; // Дверь или замок ванной уже разрушены

    private bool bathroomChaseStarted = false; // Финальная погоня после ванной уже запущена

    private Coroutine monsterWindowRouteCoroutine; // Корутина принудительного маршрута монстра к окну

    private Coroutine bathroomDoorBreakWatchCoroutine; // Корутина ожидания монстра у окна и разрушения двери ванной


    private void Start() // При старте сцены
    {
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

        onHallDoorBreak.Invoke(); // Звук выбивания двери 4/6 (3D)
    }

    public void StartFinalSequence() // Запуск финала
    {
        if (finalStarted) return; // Если финал уже был, выходим

        CompleteEarlyHallDoorBreakState(); // Если 6/6 запущен сразу, сначала применяем состояние после 4/6

        finalStarted = true; // Запоминаем запуск финала

        finalSequenceStarted = true; // Сообщаем другим скриптам, что финал начался

        SetObjectsActive(objectsToDisableAfterSixOfSix, false); // Выключаем дополнительные объекты после 6/6

        SetObjectsActive(objectsToEnableAfterSixOfSix, true); // Включаем дополнительные объекты после 6/6

        if (bathroomDoor != null) // Проверяем, назначена ли дверь ванной
        {
            bathroomDoor.CloseDoor(); // Закрываем дверь ванной

            bathroomDoor.SetLocked(true); // Блокируем дверь ванной

            bathroomDoor.canMonsterOpen = false; // Запрещаем монстру открыть ванную
        }

        if (hallReturnDeathTrigger != null) hallReturnDeathTrigger.SetActive(true); // Включаем триггер смерти

        if (kitchenFinalTrigger != null) kitchenFinalTrigger.SetActive(true); // Включаем кухонный триггер

        if (bathroomExitChaseTrigger != null) bathroomExitChaseTrigger.SetActive(false); // Пока держим триггер ванной выключенным

        if (apartmentExitCompleteTrigger != null) apartmentExitCompleteTrigger.SetActive(true); // Включаем триггер завершения квартиры

        if (elevatorEndingEvent != null) elevatorEndingEvent.UnlockElevatorEvent(); // Разблокируем лифтовую концовку

        BlockExitWithMonster(); // Отправляем монстра блокировать выход

        onFinalStart.Invoke(); // Звук старта финала 6/6 (3D)

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

        windowFirstHitReactionStarted = true; // Запоминаем реакцию, чтобы событие не повторялось

        if (finalNormalDoor != null) finalNormalDoor.SetActive(false); // Прячем обычную дверь, если она используется отдельно

        if (finalBrokenDoor != null) finalBrokenDoor.SetActive(true); // Показываем сломанную дверь, если она используется отдельно

        if (kitchenBarricade != null) kitchenBarricade.ForceSetStage(2); // Переводим кухонную баррикаду в стадию Destroyed

        if (monsterObject != null) monsterObject.SetActive(true); // Гарантированно включаем объект монстра

        if (monsterPatrol != null) monsterPatrol.StopPatrol(); // Гарантированно выключаем обычный патруль

        if (monsterAI != null) monsterAI.SetFinalCloseRangeReactionBlocked(true); // Запрещаем близкому игроку сорвать ожидание монстра у окна

        monsterReachedWindowPoint = false; // Сбрасываем состояние прибытия к окну

        bathroomDoorBroken = false; // Сбрасываем состояние двери ванной

        bathroomChaseStarted = false; // Сбрасываем состояние финальной погони

        if (bathroomDoorBreakWatchCoroutine != null) // Проверяем старую корутину ожидания двери
        {
            StopCoroutine(bathroomDoorBreakWatchCoroutine); // Останавливаем старое ожидание
            bathroomDoorBreakWatchCoroutine = null; // Очищаем ссылку
        }

        if (monsterWindowRouteCoroutine != null) // Проверяем, не запущен ли маршрут повторно
        {
            StopCoroutine(monsterWindowRouteCoroutine); // Останавливаем старую корутину маршрута
        }

        monsterWindowRouteCoroutine = StartCoroutine(MoveMonsterToWindowThroughRoutePoint()); // Запускаем обязательный маршрут через проём

        Debug.Log("Первый удар по окну: баррикада уничтожена, запущен маршрут монстра через точку за баррикадой"); // Пишем лог
    }

    private IEnumerator MoveMonsterToWindowThroughRoutePoint() // Ведёт монстра сначала через баррикаду, затем к окну
    {
        yield return null; // Даём Unity один кадр на переключение объектов стадии Destroyed

        if (monsterAI == null) // Проверяем ссылку на AI
        {
            Debug.LogWarning("ApartmentFinalSequence: не назначен Monster AI.", gameObject); // Пишем понятную ошибку
            monsterWindowRouteCoroutine = null; // Очищаем ссылку на корутину
            yield break; // Без AI движение невозможно
        }

        if (monsterAfterWindowHitPoint == null) // Проверяем конечную точку
        {
            Debug.LogWarning("ApartmentFinalSequence: не назначен Monster After Window Hit Point.", gameObject); // Пишем понятную ошибку
            monsterWindowRouteCoroutine = null; // Очищаем ссылку на корутину
            yield break; // Без конечной точки маршрут невозможен
        }

        if (monsterWindowRoutePoint != null) // Проверяем, назначена ли обязательная промежуточная точка
        {
            monsterAI.GoToPointAndStop(monsterWindowRoutePoint); // Сначала отправляем монстра прямо к проходу за разрушенной баррикадой

            float routeTimer = 0f; // Создаём таймер ожидания промежуточной точки

            while (Vector3.Distance(monsterAI.transform.position, monsterWindowRoutePoint.position) > monsterWindowRouteReachDistance) // Ждём реального прибытия
            {
                routeTimer += Time.deltaTime; // Увеличиваем таймер ожидания

                if (routeTimer >= monsterWindowRouteTimeout) // Проверяем, не истекло ли максимальное время
                {
                    Debug.LogWarning(
                        "ApartmentFinalSequence: монстр не смог дойти до Monster Window Route Point. Проверь положение точки на NavMesh.",
                        gameObject
                    ); // Пишем причину остановки последовательности

                    monsterWindowRouteCoroutine = null; // Очищаем ссылку на корутину
                    yield break; // Не отправляем монстра обходным путём к окну
                }

                yield return null; // Ждём следующий кадр
            }

            Debug.Log("ApartmentFinalSequence: монстр прошёл обязательную точку за баррикадой.", gameObject); // Подтверждаем прохождение проёма

            yield return null; // Даём финальному контроллеру один кадр завершить первую команду
        }

        monsterAI.GoToPointAndStop(monsterAfterWindowHitPoint); // После прохода через баррикаду отправляем монстра к конечной точке окна

        Debug.Log("ApartmentFinalSequence: монстр направлен к конечной точке перед окном.", gameObject); // Подтверждаем вторую команду

        monsterWindowRouteCoroutine = null; // Очищаем ссылку на завершённую корутину

        bathroomDoorBreakWatchCoroutine = StartCoroutine(
            WaitForMonsterAtWindowAndBathroomDoorBreak()
        ); // Запускаем ожидание прибытия монстра и разрушения двери ванной
    }

    private IEnumerator WaitForMonsterAtWindowAndBathroomDoorBreak() // Удерживает монстра у окна до разрушения двери ванной
    {
        float reachTimer = 0f; // Создаём таймер ожидания конечной точки

        while (!HasMonsterReachedWindowPoint()) // Ждём, пока монстр действительно дойдёт к точке окна
        {
            reachTimer += Time.deltaTime; // Увеличиваем таймер ожидания

            if (reachTimer >= monsterWindowFinalReachTimeout) // Проверяем максимальное время ожидания
            {
                Debug.LogWarning(
                    "ApartmentFinalSequence: монстр не считается дошедшим до точки окна. "
                    + "Проверь Monster Window Final Reach Distance и положение точки по X/Z.",
                    gameObject
                ); // Пишем понятную причину

                bathroomDoorBreakWatchCoroutine = null; // Очищаем ссылку
                yield break; // Не запускаем погоню из неправильного положения
            }

            yield return null; // Ждём следующий кадр
        }

        monsterReachedWindowPoint = true; // Запоминаем реальное прибытие монстра

        Debug.Log(
            "ApartmentFinalSequence: монстр встал у окна и ждёт разрушения двери ванной.",
            gameObject
        ); // Подтверждаем начало ожидания

        if (
            bathroomDoorStageObject != null
            && bathroomDoorStageObject.CurrentStage >= bathroomDoorBrokenStage
        ) // Проверяем, не была ли дверь уже переведена в Broken
        {
            bathroomDoorBroken = true; // Запоминаем уже наступившую стадию Broken
            StartBathroomChaseAfterDoorBreak(); // Сразу запускаем погоню после прибытия монстра
            bathroomDoorBreakWatchCoroutine = null; // Очищаем ссылку на корутину
            yield break; // Дальнейшее ожидание больше не требуется
        }

        if (bathroomDoorStageObject == null) // Проверяем, назначен ли EventDoor
        {
            Debug.LogWarning(
                "ApartmentFinalSequence: не назначен Bathroom Door Stage Object. Перетащи объект EventDoor.",
                gameObject
            ); // Пишем понятную ошибку настройки

            bathroomDoorBreakWatchCoroutine = null; // Очищаем ссылку
            yield break; // Без объекта двери нельзя определить стадию Broken
        }

        while (
            !bathroomDoorBroken
            && bathroomDoorStageObject.CurrentStage < bathroomDoorBrokenStage
        ) // Ждём, пока EventDoor перейдёт в стадию Broken
        {
            yield return new WaitForSeconds(bathroomDoorStageCheckInterval); // Проверяем стадию с заданной частотой
        }

        bathroomDoorBroken = true; // Запоминаем достижение стадии Broken

        Debug.Log(
            "ApartmentFinalSequence: EventDoor перешёл в стадию Broken.",
            gameObject
        ); // Подтверждаем правильное событие

        StartBathroomChaseAfterDoorBreak(); // Запускаем погоню только после прибытия монстра и Broken двери

        bathroomDoorBreakWatchCoroutine = null; // Очищаем ссылку на завершённую корутину
    }

    public void OnBathroomDoorBroken() // Вызывается событием On Stage Changed объекта EventBathroomDoor
    {
        if (bathroomDoorStageObject == null) // Проверяем ссылку на событийную дверь
        {
            Debug.LogWarning(
                "ApartmentFinalSequence: не назначен Bathroom Door Stage Object.",
                gameObject
            ); // Показываем точную ошибку настройки

            return; // Без объекта двери стадию проверить нельзя
        }

        if (bathroomDoorStageObject.CurrentStage < bathroomDoorBrokenStage) // Проверяем, достигнута ли стадия Broken
        {
            return; // Стадия 0 ещё не должна запускать погоню
        }

        bathroomDoorBroken = true; // Запоминаем стадию Broken

        if (HasMonsterReachedWindowPoint()) // Дополнительно проверяем реальное положение монстра по X/Z
        {
            monsterReachedWindowPoint = true; // Исправляем флаг прибытия, даже если высота точки Y отличается
        }

        Debug.Log(
            "ApartmentFinalSequence: получено событие Broken от EventBathroomDoor. Stage = "
            + bathroomDoorStageObject.CurrentStage,
            gameObject
        ); // Подтверждаем получение события двери

        StartBathroomChaseAfterDoorBreak(); // Метод сам проверит оба обязательных условия
    }

    private bool HasMonsterReachedWindowPoint() // Проверяет прибытие монстра к окну без учёта разницы высоты Y
    {
        if (monsterAI == null) return false; // Без монстра проверка невозможна

        if (monsterAfterWindowHitPoint == null) return false; // Без конечной точки проверка невозможна

        Vector2 monsterPosition = new Vector2(
            monsterAI.transform.position.x,
            monsterAI.transform.position.z
        ); // Получаем горизонтальную позицию монстра

        Vector2 windowPointPosition = new Vector2(
            monsterAfterWindowHitPoint.position.x,
            monsterAfterWindowHitPoint.position.z
        ); // Получаем горизонтальную позицию точки окна

        float horizontalDistance = Vector2.Distance(
            monsterPosition,
            windowPointPosition
        ); // Считаем расстояние только по плоскости пола

        return horizontalDistance <= monsterWindowFinalReachDistance; // Возвращаем результат прибытия
    }

    private void StartBathroomChaseAfterDoorBreak() // Запускает финальную погоню после разрушения двери ванной
    {
        if (bathroomChaseStarted) return; // Не запускаем погоню повторно

        if (!monsterReachedWindowPoint) return; // Монстр сначала обязан дойти до точки у окна

        if (!bathroomDoorBroken) return; // Дверь ванной должна быть разрушена

        bathroomChaseStarted = true; // Запоминаем запуск погони

        if (monsterObject != null) monsterObject.SetActive(true); // Гарантированно включаем монстра

        if (monsterPatrol != null) monsterPatrol.StopPatrol(); // Гарантированно выключаем патруль

        if (monsterAI != null)
        {
            monsterAI.SetFinalCloseRangeReactionBlocked(false); // Снимаем защищённое ожидание

            monsterAI.ForceChasePlayer(); // Запускаем постоянную погоню после ванной
        }

        if (bathroomExitChaseTrigger != null) bathroomExitChaseTrigger.SetActive(false); // Выключаем запасной триггер после запуска

        Debug.Log(
            "ApartmentFinalSequence: дверь ванной разрушена, монстр начал финальную погоню.",
            gameObject
        ); // Подтверждаем запуск погони
    }

    public void OnPlayerEscapedThroughWindow() // Игрок полностью перелез через окно
    {
        if (!finalSequenceStarted) return; // Если финал не начался, выходим

        if (playerEscapedThroughWindow) return; // Не выполняем действие повторно

        playerEscapedThroughWindow = true; // Запоминаем, что игрок перелез через окно

        Debug.Log(
            "ApartmentFinalSequence: игрок перелез через окно.",
            gameObject
        ); // Двери уже переключены списками этапа 6/6
    }

    public void OnBathroomExitTrigger() // Игрок прошёл через выход из ванной
    {
        if (bathroomExitTriggered) return; // Если уже сработало, выходим

        if (!finalSequenceStarted) return; // Если финал не начался, выходим

        if (!playerEscapedThroughWindow) return; // Если игрок не перелез через окно, выходим

        bathroomExitTriggered = true; // Запоминаем срабатывание

        if (bathroomExitChaseTrigger != null) bathroomExitChaseTrigger.SetActive(false); // Выключаем одноразовый триггер

        Debug.Log(
            "Игрок прошёл через выход ванной. Погоня зависит только от стадии Broken объекта EventDoor.",
            gameObject
        ); // Триггер больше не запускает погоню
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


    public bool CanActivateUniversalTrigger(int requiredStage) // Проверяем, разрешает ли текущая стадия квартиры работу универсального триггера
    {
        if (requiredStage <= 0) return true; // Значение 0 означает, что триггер может работать в любой момент

        if (requiredStage == 4) return hallDoorBreakCompleted; // Для стадии 4/6 разрешаем работу только после завершения раннего события

        if (requiredStage == 6) return finalSequenceStarted; // Для стадии 6/6 разрешаем работу только после запуска финала

        return false; // Любое другое значение считаем недопустимым
    }

    public void OnUniversalTriggerActivated(string triggerID) // Получаем сообщение от универсального триггера после его успешной активации
    {
        Debug.Log("ApartmentFinalSequence: активирован триггер " + triggerID); // Пишем в Console, какой именно триггер сработал
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