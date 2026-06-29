using UnityEngine; // Подключаем Unity-классы
using System.Collections; // Подключаем корутины

public class HallReturnDeathTrigger : MonoBehaviour // Триггер постановочной смерти при возврате в зал
{
    [Header("Death Settings")] // Блок настроек смерти
    public bool deathEnabled = true; // Включена ли смерть

    public GameOverManager gameOverManager; // Ссылка на GameOverManager

    public float gameOverDelay = 2.5f; // Задержка перед Game Over после падения

    [Header("Direction Points")] // Блок точек направления
    public Transform hallSidePoint; // Точка со стороны зала

    public Transform corridorSidePoint; // Точка со стороны коридора

    public float sideDistance = 2f; // Дистанция проверки стороны

    [Header("Player References")] // Блок ссылок игрока
    public StarterAssets.FirstPersonController playerController; // Контроллер игрока

    public CharacterController playerCharacterController; // CharacterController игрока

    public Transform playerCameraRoot; // Корень камеры игрока, который будем наклонять при падении

    [Header("Fall Motion")] // Блок траектории падения
    public float fallForwardDistance = 1.2f; // Насколько игрок падает вперёд

    public float fallDownDistance = 1.1f; // Насколько игрок падает вниз

    public float fallDuration = 0.75f; // Длительность падения

    public float fallArcHeight = 0.25f; // Небольшая дуга вверх/вперёд перед падением

    public float cameraPitchDown = 65f; // Наклон камеры вниз

    public float cameraRoll = 22f; // Завал камеры набок

    public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Кривая падения

    [Header("Noise")] // Блок шума
    public NoiseEmitter tripNoiseEmitter; // Источник шума падения

    [Range(1, 10)] public int tripNoisePower = 10; // Сила шума падения

    [Header("Monster")] // Блок монстра
    public MonsterAI monsterAI; // Ссылка на MonsterAI

    public float monsterChaseDelay = 0.4f; // Задержка перед погоней

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = true; // Показывать логи

    private bool playerPassedToCorridor = false; // Игрок уже вышел из зала в коридор

    private bool triggered = false; // Смерть уже сработала

    private void OnTriggerEnter(Collider other) // Когда объект входит в триггер
    {
        if (!deathEnabled) return; // Если смерть выключена — выходим

        if (triggered) return; // Если уже сработало — выходим

        if (!other.CompareTag("Player")) return; // Если вошёл не игрок — выходим

        bool fromHall = IsCloserToPoint(other.transform, hallSidePoint); // Проверяем вход со стороны зала

        bool fromCorridor = IsCloserToPoint(other.transform, corridorSidePoint); // Проверяем вход со стороны коридора

        if (!playerPassedToCorridor && fromHall) // Если игрок впервые выходит из зала в коридор
        {
            playerPassedToCorridor = true; // Теперь возврат назад станет опасным

            if (showDebugLogs) Debug.Log("HallReturnDeathTrigger: игрок вышел из зала, возврат теперь опасен"); // Лог

            return; // Первый проход не убивает
        }

        if (playerPassedToCorridor && fromCorridor) // Если игрок возвращается обратно в зал
        {
            triggered = true; // Блокируем повтор

            if (showDebugLogs) Debug.Log("HallReturnDeathTrigger: игрок споткнулся и падает"); // Лог

            StartCoroutine(TripDeathSequence(other.transform)); // Запускаем смерть

            return; // Выходим
        }
    }

    private bool IsCloserToPoint(Transform playerTransform, Transform point) // Проверить близость игрока к точке
    {
        if (point == null) return false; // Если точка не назначена — false

        float distance = Vector3.Distance(playerTransform.position, point.position); // Считаем дистанцию

        return distance <= sideDistance; // Возвращаем true если рядом
    }

    private IEnumerator TripDeathSequence(Transform playerTransform) // Полная последовательность смерти
    {
        DisablePlayerControl(); // Отключаем управление

        EmitTripNoise(); // Создаём шум 10

        yield return StartCoroutine(ForwardFallSequence(playerTransform)); // Проигрываем падение вперёд

        yield return new WaitForSeconds(monsterChaseDelay); // Ждём перед реакцией монстра

        StartMonsterKillChase(); // Запускаем монстра

        yield return new WaitForSeconds(gameOverDelay); // Ждём до Game Over

        ShowGameOver(); // Показываем Game Over
    }

    private void DisablePlayerControl() // Отключить управление игрока
    {
        if (playerController != null) playerController.canMove = false; // Запрещаем движение

        if (playerController != null) playerController.canLook = false; // Запрещаем обзор
    }

    private void EmitTripNoise() // Создать шум падения
    {
        if (tripNoiseEmitter == null) return; // Если источника шума нет — выходим

        tripNoiseEmitter.EmitNoise(tripNoisePower); // Создаём шум
    }

    private IEnumerator ForwardFallSequence(Transform playerTransform) // Падение игрока вперёд по траектории
    {
        if (playerTransform == null) yield break; // Если игрока нет — выходим

        if (playerCameraRoot == null) playerCameraRoot = Camera.main != null ? Camera.main.transform : null; // Если корень камеры не назначен — пробуем взять MainCamera

        Vector3 startPosition = playerTransform.position; // Запоминаем позицию игрока

        Quaternion startCameraRotation = playerCameraRoot != null ? playerCameraRoot.localRotation : Quaternion.identity; // Запоминаем поворот камеры

        Vector3 forward = playerTransform.forward; // Берём направление игрока

        forward.y = 0f; // Убираем вертикаль

        forward.Normalize(); // Нормализуем направление

        Vector3 endPosition = startPosition + forward * fallForwardDistance + Vector3.down * fallDownDistance; // Считаем конечную позицию падения

        Quaternion endCameraRotation = startCameraRotation * Quaternion.Euler(cameraPitchDown, 0f, cameraRoll); // Считаем финальный завал камеры

        if (playerCharacterController != null) playerCharacterController.enabled = false; // Отключаем CharacterController

        float timer = 0f; // Таймер падения

        while (timer < fallDuration) // Пока падение не завершено
        {
            timer += Time.deltaTime; // Увеличиваем таймер

            float rawT = Mathf.Clamp01(timer / fallDuration); // Сырой прогресс 0-1

            float t = fallCurve.Evaluate(rawT); // Применяем кривую падения

            Vector3 flatPosition = Vector3.Lerp(startPosition, endPosition, t); // Двигаем игрока вперёд и вниз

            float arc = Mathf.Sin(rawT * Mathf.PI) * fallArcHeight; // Создаём небольшую дугу движения

            playerTransform.position = flatPosition + Vector3.up * arc; // Применяем позицию с дугой

            if (playerCameraRoot != null) // Если камера назначена
            {
                playerCameraRoot.localRotation = Quaternion.Slerp(startCameraRotation, endCameraRotation, t); // Плавно наклоняем камеру
            }

            yield return null; // Ждём следующий кадр
        }

        playerTransform.position = endPosition; // Фиксируем финальную позицию

        if (playerCameraRoot != null) playerCameraRoot.localRotation = endCameraRotation; // Фиксируем финальный наклон камеры

        if (playerCharacterController != null) playerCharacterController.enabled = true; // Включаем CharacterController обратно
    }

    private void StartMonsterKillChase() // Запустить монстра
    {
        if (monsterAI == null) return; // Если монстр не назначен — выходим

        monsterAI.ForceChasePlayer(); // Запускаем финальную погоню
    }

    private void ShowGameOver() // Показать Game Over
    {
        if (gameOverManager != null) // Если GameOverManager назначен
        {
            gameOverManager.ShowGameOver(); // Показываем Game Over
        }
        else // Если GameOverManager нет
        {
            Time.timeScale = 0f; // Останавливаем игру

            Debug.Log("GAME OVER"); // Пишем лог
        }
    }
}