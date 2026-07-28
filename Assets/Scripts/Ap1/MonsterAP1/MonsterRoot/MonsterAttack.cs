using System.Collections; // Подключаем корутины
using UnityEngine; // Подключаем основные Unity-классы

[DefaultExecutionOrder(100)] // Выполняем LateUpdate после обычного контроллера игрока
public class MonsterAttack : MonoBehaviour // Управляет остановкой, разворотом и атакой монстра
{
    [Header("References")] // Ссылки на существующие объекты
    public Transform player; // Корневой объект Player
    public Transform playerCameraTarget; // PlayerCameraRoot или CinemachineCameraTarget игрока
    public MonsterMovement movement; // Система движения монстра
    public Animator animator; // Animator дочерней модели LolyGirl
    public GameOverManager gameOverManager; // Менеджер экрана Game Over
    public StarterAssets.FirstPersonController playerController; // Контроллер Starter Assets игрока

    [Header("Attack")] // Основные настройки атаки
    public float attackDistance = 1.2f; // Дистанция начала обычной атаки
    public float attackDelay = 1.2f; // Задержка от запуска Kill до Game Over
    public float hideCatchDelay = 1f; // Задержка при вытаскивании из шкафа

    [Header("Face Each Other")] // Настройки взаимного разворота
    public float faceDuration = 0.2f; // Время плавного разворота перед анимацией
    public float monsterLookHeight = 1.45f; // Высота точки, куда смотрит камера игрока
    public bool keepFacingDuringAttack = true; // Удерживать взаимный взгляд до Game Over

    private static readonly int AttackHash = Animator.StringToHash("Attack"); // Кэшируем Trigger Attack
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving"); // Кэшируем Bool движения
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning"); // Кэшируем Bool бега

    private bool isAttacking; // Идёт ли атака сейчас
    private bool isAligning; // Выполняется ли начальный плавный разворот
    private float alignmentProgress; // Прогресс начального разворота от 0 до 1

    private Quaternion monsterStartRotation; // Поворот монстра до начала разворота
    private Quaternion playerStartRotation; // Поворот игрока до начала разворота
    private Quaternion cameraStartRotation; // Поворот камеры до начала разворота

    public bool IsAttacking => isAttacking; // Публичная проверка состояния атаки

    private void Reset() => FindReferences(); // Автоматически ищем ссылки при добавлении компонента
    private void Awake() => FindReferences(); // Проверяем ссылки при запуске сцены

    private void FindReferences() // Находит существующие компоненты
    {
        if (movement == null) movement = GetComponent<MonsterMovement>(); // Ищем MonsterMovement на корневом Monster
        if (animator == null) animator = GetComponentInChildren<Animator>(true); // Ищем Animator на дочерней LolyGirl
        if (player == null && playerController != null) player = playerController.transform; // Берём корень игрока из контроллера
    }

    public bool IsPlayerInAttackDistance() // Проверяет дистанцию до игрока
    {
        if (player == null) return false; // Без игрока атаковать нельзя

        Vector3 difference = player.position - transform.position; // Получаем направление к игроку
        difference.y = 0f; // Не учитываем разницу высоты

        return difference.sqrMagnitude <= attackDistance * attackDistance; // Сравниваем дистанцию без квадратного корня
    }

    public void StartAttack() // Запускает обычную атаку
    {
        if (isAttacking) return; // Не запускаем вторую атаку поверх первой
        StartCoroutine(AttackSequence(attackDelay, null)); // Запускаем общую последовательность
    }

    public void StartHideCatchAttack(UniversalDoor wardrobeDoor) // Запускает смерть при прятках на глазах монстра
    {
        if (isAttacking) return; // Не запускаем вторую атаку поверх первой
        StartCoroutine(AttackSequence(hideCatchDelay, wardrobeDoor)); // Запускаем ту же последовательность с дверью шкафа
    }

    private IEnumerator AttackSequence(float gameOverDelay, UniversalDoor wardrobeDoor) // Полная последовательность атаки
    {
        isAttacking = true; // Блокируем повторный запуск атаки
        FreezePlayer(); // Отключаем движение и обзор игрока

        if (movement != null) movement.Stop(); // Полностью останавливаем NavMeshAgent монстра

        if (animator != null) // Проверяем Animator
        {
            animator.SetBool(IsMovingHash, false); // Немедленно выключаем Walk
            animator.SetBool(IsRunningHash, false); // Немедленно выключаем Run
        }

        if (wardrobeDoor != null) wardrobeDoor.OpenDoor(); // Открываем шкаф перед атакой спрятавшегося игрока

        BeginAlignment(); // Запоминаем стартовые повороты

        float timer = 0f; // Создаём таймер разворота
        float duration = Mathf.Max(0f, faceDuration); // Не разрешаем отрицательное время

        while (timer < duration) // Плавно разворачиваем персонажей
        {
            timer += Time.deltaTime; // Увеличиваем таймер
            alignmentProgress = duration <= 0f ? 1f : Mathf.Clamp01(timer / duration); // Рассчитываем прогресс
            yield return null; // Ждём следующий кадр
        }

        alignmentProgress = 1f; // Гарантируем полный разворот
        yield return null; // Даём LateUpdate применить финальное положение
        isAligning = false; // Завершаем начальное выравнивание

        ApplyExactFacing(); // Ещё раз точно направляем персонажей друг на друга

        if (animator != null) // Проверяем Animator
        {
            animator.ResetTrigger(AttackHash); // Сбрасываем возможный старый Trigger
            animator.SetTrigger(AttackHash); // Запускаем анимацию Kill
        }

        yield return new WaitForSeconds(Mathf.Max(0f, gameOverDelay)); // Ждём настроенный момент удара

        FinishAttack(); // Показываем Game Over
    }

    private void BeginAlignment() // Подготавливает плавный разворот
    {
        isAligning = true; // Включаем режим выравнивания
        alignmentProgress = faceDuration <= 0f ? 1f : 0f; // Сбрасываем прогресс
        monsterStartRotation = transform.rotation; // Запоминаем поворот монстра
        playerStartRotation = player != null ? player.rotation : Quaternion.identity; // Запоминаем поворот игрока
        cameraStartRotation = playerCameraTarget != null ? playerCameraTarget.rotation : Quaternion.identity; // Запоминаем поворот камеры
    }

    private void LateUpdate() // Применяет разворот после контроллера игрока
    {
        if (!isAttacking) return; // Вне атаки ничего не меняем

        if (isAligning) ApplySmoothFacing(alignmentProgress); // Во время подготовки плавно разворачиваемся
        else if (keepFacingDuringAttack) ApplyExactFacing(); // Во время Kill удерживаем взаимный взгляд
    }

    private void ApplySmoothFacing(float progress) // Плавно направляет монстра и игрока друг на друга
    {
        float smoothProgress = progress * progress * (3f - 2f * progress); // Добавляем мягкое ускорение и торможение
        Quaternion monsterTarget = GetMonsterTargetRotation(); // Получаем целевой поворот монстра
        Quaternion playerTarget = GetPlayerTargetRotation(); // Получаем целевой поворот игрока

        transform.rotation = Quaternion.Slerp(monsterStartRotation, monsterTarget, smoothProgress); // Плавно поворачиваем монстра

        if (player != null) player.rotation = Quaternion.Slerp(playerStartRotation, playerTarget, smoothProgress); // Плавно поворачиваем игрока

        if (playerCameraTarget != null) // Проверяем цель камеры
        {
            Quaternion cameraTarget = GetCameraTargetRotation(); // Получаем целевой поворот камеры
            playerCameraTarget.rotation = Quaternion.Slerp(cameraStartRotation, cameraTarget, smoothProgress); // Плавно направляем взгляд
        }
    }

    private void ApplyExactFacing() // Точно направляет монстра и игрока друг на друга
    {
        transform.rotation = GetMonsterTargetRotation(); // Монстр смотрит на игрока

        if (player != null) player.rotation = GetPlayerTargetRotation(); // Игрок разворачивается к монстру
        if (playerCameraTarget != null) playerCameraTarget.rotation = GetCameraTargetRotation(); // Камера смотрит на верхнюю часть монстра
    }

    private Quaternion GetMonsterTargetRotation() // Рассчитывает горизонтальный поворот монстра
    {
        if (player == null) return transform.rotation; // Без игрока сохраняем текущий поворот

        Vector3 direction = player.position - transform.position; // Получаем направление к игроку
        direction.y = 0f; // Не наклоняем монстра вверх или вниз

        return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction.normalized) : transform.rotation; // Возвращаем безопасный поворот
    }

    private Quaternion GetPlayerTargetRotation() // Рассчитывает горизонтальный поворот игрока
    {
        if (player == null) return Quaternion.identity; // Без игрока возвращаем нейтральный поворот

        Vector3 direction = transform.position - player.position; // Получаем направление к монстру
        direction.y = 0f; // Не наклоняем корень игрока

        return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction.normalized) : player.rotation; // Возвращаем безопасный поворот
    }

    private Quaternion GetCameraTargetRotation() // Рассчитывает вертикальный и горизонтальный взгляд камеры
    {
        if (playerCameraTarget == null) return Quaternion.identity; // Без цели камеры поворачивать нечего

        Vector3 monsterLookPoint = transform.position + Vector3.up * monsterLookHeight; // Получаем точку на верхней части монстра
        Vector3 direction = monsterLookPoint - playerCameraTarget.position; // Получаем направление взгляда

        return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction.normalized) : playerCameraTarget.rotation; // Возвращаем безопасный поворот
    }

    private void FreezePlayer() // Блокирует управление игрока
    {
        if (playerController == null) return; // Без контроллера блокировать нечего
        playerController.canMove = false; // Запрещаем игроку двигаться
        playerController.canLook = false; // Запрещаем игроку вращать камерой
    }

    private void FinishAttack() // Завершает атаку
    {
        if (gameOverManager != null) gameOverManager.ShowGameOver(); // Показываем экран Game Over
    }

    private void OnDisable() // Сбрасывает незавершённую атаку при выключении объекта
    {
        StopAllCoroutines(); // Останавливаем корутины
        isAttacking = false; // Сбрасываем состояние атаки
        isAligning = false; // Сбрасываем выравнивание
    }
}