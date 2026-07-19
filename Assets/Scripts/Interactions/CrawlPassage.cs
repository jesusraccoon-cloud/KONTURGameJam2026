using UnityEngine; // Подключаем Unity
using System.Collections; // Подключаем корутины
using StarterAssets; // Подключаем Starter Assets

public class CrawlPassage : MonoBehaviour, IInteractable // Универсальный двухсторонний проход
{
    [Header("Player References")]
    public Transform playerRoot; // Объект игрока PlayerCapsule
    public CharacterController characterController; // CharacterController игрока
    public FirstPersonController firstPersonController; // FirstPersonController игрока

    [Header("Side A Points")]
    public Transform sideAEnterPoint; // Вход со стороны комнаты
    public Transform sideAExitPoint; // Выход в комнату

    [Header("Side B Points")]
    public Transform sideBEnterPoint; // Вход со стороны зала
    public Transform sideBExitPoint; // Выход в зал

    [Header("Interaction Distance")]
    public float maxUseDistance = 3f; // Максимальная дистанция использования

    [Header("Movement Settings")]
    public float moveToEnterTime = 0.35f; // Время подтягивания
    public float crawlTime = 1.2f; // Время пролезания
    public bool rotatePlayerAfterCrawl = true; // Поворачивать игрока после выхода

    [Header("Sideways Rotation")]
    [Range(0f, 100f)]
    public float turnRightChance = 50f; // Вероятность поворота вправо в процентах

    [Header("Usage Settings")]
    public bool canUse = true; // Можно ли использовать
    public bool useOnlyOnce = false; // Одноразовый проход
    public bool isBusy = false; // Сейчас используется

    [Header("Noise")]
    public NoiseEmitter noiseEmitter; // Источник шума
    public float noiseDelay = 0.4f; // Задержка шума

    [Header("Debug")]
    public bool showDebugLogs = true; // Показывать сообщения в Console

    public void Interact() // Вызывается PlayerInteractor
    {
        if (showDebugLogs) Debug.Log("CrawlPassage: нажали E по проходу."); // Проверка вызова

        if (canUse == false) // Если проход выключен
        {
            if (showDebugLogs) Debug.Log("CrawlPassage: canUse выключен."); // Лог
            return; // Выходим
        }

        if (isBusy == true) // Если уже используется
        {
            if (showDebugLogs) Debug.Log("CrawlPassage: проход уже используется."); // Лог
            return; // Выходим
        }

        if (playerRoot == null) // Если игрок не назначен
        {
            if (showDebugLogs) Debug.LogError("CrawlPassage: не назначен Player Root."); // Ошибка
            return; // Выходим
        }

        if (sideAEnterPoint == null || sideAExitPoint == null || sideBEnterPoint == null || sideBExitPoint == null) // Если нет точек
        {
            if (showDebugLogs) Debug.LogError("CrawlPassage: назначены не все 4 точки."); // Ошибка
            return; // Выходим
        }

        float distanceToA = Vector3.Distance(playerRoot.position, sideAEnterPoint.position); // Дистанция до A
        float distanceToB = Vector3.Distance(playerRoot.position, sideBEnterPoint.position); // Дистанция до B
        float nearestDistance = Mathf.Min(distanceToA, distanceToB); // Ближайшая дистанция

        if (showDebugLogs) Debug.Log("CrawlPassage: Distance A = " + distanceToA + " / Distance B = " + distanceToB + " / Nearest = " + nearestDistance); // Лог дистанции

        if (nearestDistance > maxUseDistance) // Если игрок далеко
        {
            if (showDebugLogs) Debug.Log("CrawlPassage: игрок слишком далеко."); // Лог
            return; // Выходим
        }

        if (distanceToA <= distanceToB) // Если игрок ближе к стороне A
        {
            if (showDebugLogs) Debug.Log("CrawlPassage: запускаю переход A -> B."); // Лог
            StartCoroutine(CrawlRoutine(sideAEnterPoint, sideBExitPoint)); // Ползём A -> B
        }
        else // Если игрок ближе к стороне B
        {
            if (showDebugLogs) Debug.Log("CrawlPassage: запускаю переход B -> A."); // Лог
            StartCoroutine(CrawlRoutine(sideBEnterPoint, sideAExitPoint)); // Ползём B -> A
        }
    }

    private IEnumerator CrawlRoutine(Transform enterPoint, Transform exitPoint) // Процесс пролезания
    {
        isBusy = true; // Занимаем проход

        if (firstPersonController != null) firstPersonController.enabled = false; // Отключаем управление

        if (characterController != null) characterController.enabled = false; // Отключаем CharacterController

        if (noiseEmitter != null) StartCoroutine(EmitNoiseAfterDelay()); // Запускаем шум

        yield return MovePlayer(playerRoot.position, enterPoint.position, moveToEnterTime); // Подтягиваем ко входу

        Vector3 crawlDirection = exitPoint.position - enterPoint.position; // Получаем направление прохода

        crawlDirection.y = 0f; // Убираем вертикальный наклон

        Quaternion forwardRotation = Quaternion.LookRotation(crawlDirection); // Получаем поворот по направлению прохода

        bool turnRight = Random.Range(0f, 100f) < turnRightChance; // Проверяем вероятность поворота вправо

        float sideAngle = turnRight ? 90f : -90f; // Выбираем правый или левый поворот

        playerRoot.rotation = forwardRotation * Quaternion.Euler(0f, sideAngle, 0f); // Поворачиваем игрока боком

        yield return MovePlayer(playerRoot.position, exitPoint.position, crawlTime); // Перемещаем игрока к выходу

        if (rotatePlayerAfterCrawl == true) playerRoot.rotation = exitPoint.rotation; // Поворачиваем игрока после выхода

        if (characterController != null) characterController.enabled = true; // Включаем CharacterController

        if (firstPersonController != null) firstPersonController.enabled = true; // Включаем управление

        if (useOnlyOnce == true) canUse = false; // Если одноразовый, отключаем

        isBusy = false; // Освобождаем проход
    }

    private IEnumerator MovePlayer(Vector3 startPosition, Vector3 targetPosition, float duration) // Плавное движение
    {
        float timer = 0f; // Таймер

        while (timer < duration) // Пока идёт движение
        {
            timer += Time.deltaTime; // Добавляем время

            float t = Mathf.Clamp01(timer / duration); // Прогресс 0-1

            t = Mathf.SmoothStep(0f, 1f, t); // Сглаживание

            playerRoot.position = Vector3.Lerp(startPosition, targetPosition, t); // Двигаем игрока

            yield return null; // Ждём кадр
        }

        playerRoot.position = targetPosition; // Ставим точно в точку
    }

    private IEnumerator EmitNoiseAfterDelay() // Шум с задержкой
    {
        yield return new WaitForSeconds(noiseDelay); // Ждём

        if (noiseEmitter != null) noiseEmitter.EmitNoise(); // Создаём шум
    }
}