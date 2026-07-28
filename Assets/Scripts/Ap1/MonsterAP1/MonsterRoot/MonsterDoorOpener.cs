using System.Collections; // Подключаем корутины
using UnityEngine; // Подключаем основные классы Unity

public class MonsterDoorOpener : MonoBehaviour // Управляет открытием дверей монстром
{
    [Header("References")] // Ссылки на существующие системы
    public MonsterMovement movement; // Движение монстра
    public Animator animator; // Animator модели LolyGirl

    [Header("Door Detection")] // Поиск двери перед монстром
    public float doorCheckDistance = 1.8f; // Максимальная дистанция до двери
    public float doorCheckRadius = 0.6f; // Радиус сферы поиска
    public float checkHeight = 1f; // Высота проверки
    public float forwardOffset = 1f; // Смещение сферы вперёд
    public float minForwardDot = 0.2f; // Насколько дверь должна быть впереди
    public LayerMask doorLayers = ~0; // Слои, на которых ищутся двери

    [Header("Wall Check")] // Проверка стены между монстром и дверью
    public LayerMask lineOfSightLayers = ~0; // Слои стен, дверей и препятствий
    public float lineOfSightExtraDistance = 0.1f; // Небольшой запас длины луча

    [Header("Opening Sequence")] // Настройки последовательности открытия
    public float turnSpeed = 360f; // Скорость поворота к двери
    public float facingAngle = 12f; // Допустимая ошибка поворота
    public float animationOpenMoment = 0.35f; // Момент начала физического открытия
    public float afterFullyOpenDelay = 0.15f; // Пауза после полного открытия
    public float maxDoorOpenWait = 3f; // Максимальное ожидание открытия
    public float checkCooldown = 0.1f; // Пауза между проверками двери

    [Header("Debug")] // Отладочное отображение
    public bool drawDebugRay = true; // Рисовать направление проверки
    public bool drawDebugSphere = true; // Рисовать сферу поиска
    public bool drawLineOfSight = true; // Рисовать линию видимости двери

    private const int HitBufferSize = 24; // Максимум коллайдеров в одной проверке
    private static readonly int OpenDoorHash = Animator.StringToHash("OpenDoor"); // Кэш Trigger без поиска строки
    private readonly Collider[] doorHits = new Collider[HitBufferSize]; // Переиспользуемый буфер без Garbage Collector

    private bool isHandlingDoor; // Выполняется ли открытие двери
    private float nextCheckTime; // Время следующей проверки
    private UniversalDoor currentDoor; // Текущая дверь
    private Vector3 currentDoorPoint; // Точка поворота к двери

    public bool IsHandlingDoor => isHandlingDoor; // Доступ для MonsterAI и финальной логики

    private void Reset() => FindReferences(); // Автоматически ищем ссылки при добавлении компонента
    private void Awake() => FindReferences(); // Проверяем ссылки при запуске

    private void FindReferences() // Находит существующие компоненты
    {
        if (movement == null) movement = GetComponent<MonsterMovement>(); // Ищем движение на корневом Monster
        if (animator == null) animator = GetComponentInChildren<Animator>(true); // Ищем Animator на дочерней модели
    }

    private void LateUpdate() // Выполняется после основной AI-логики
    {
        if (isHandlingDoor && movement != null) movement.KeepMovementLocked(); // Не даём другим скриптам включить движение
    }

    public bool TryOpenDoorAhead() // Пытается начать открытие двери
    {
        if (isHandlingDoor) return true; // Пока дверь открывается, обычная AI-логика должна ждать
        if (Time.time < nextCheckTime) return false; // Не проверяем дверь каждый кадр
        nextCheckTime = Time.time + checkCooldown; // Назначаем следующую разрешённую проверку

        if (!TryFindVisibleDoor(out UniversalDoor door, out Vector3 doorPoint)) return false; // Ищем доступную дверь
        StartCoroutine(OpenDoorSequence(door, doorPoint)); // Запускаем остановку, анимацию и открытие
        return true; // Сообщаем AI, что обычное движение нужно прервать
    }

    private bool TryFindVisibleDoor(out UniversalDoor foundDoor, out Vector3 foundPoint) // Ищет ближайшую дверь без стены между ней и монстром
    {
        foundDoor = null; // Сбрасываем результат
        foundPoint = Vector3.zero; // Сбрасываем точку
        Vector3 start = GetCheckStart(); // Получаем начало проверки на уровне корпуса
        Vector3 center = start + transform.forward * forwardOffset; // Получаем центр сферы перед монстром
        DrawForwardDebug(start); // Рисуем красный debug-луч

        int count = Physics.OverlapSphereNonAlloc(center, doorCheckRadius, doorHits, doorLayers, QueryTriggerInteraction.Collide); // Ищем коллайдеры без выделения памяти
        float bestSqrDistance = doorCheckDistance * doorCheckDistance; // Ограничиваем максимальную дистанцию

        for (int i = 0; i < count; i++) // Перебираем найденные коллайдеры
        {
            Collider hit = doorHits[i]; // Берём текущий коллайдер
            if (hit == null) continue; // Пустой элемент пропускаем

            UniversalDoor door = hit.GetComponentInParent<UniversalDoor>(); // Ищем UniversalDoor в родителях
            if (door == null || !door.CanMonsterOpenNow) continue; // Игнорируем открытые, занятые, запертые и запрещённые двери

            Collider body = door.GetComponent<Collider>(); // Получаем основной коллайдер полотна
            if (body == null) body = hit; // Используем найденный коллайдер как запасной вариант

            Vector3 point = body.ClosestPoint(transform.position); // Получаем ближайшую поверхность двери
            Vector3 flatDirection = point - transform.position; // Получаем направление к двери
            flatDirection.y = 0f; // Убираем вертикальную составляющую

            if (flatDirection.sqrMagnitude < 0.0001f) continue; // Некорректную точку пропускаем
            if (Vector3.Dot(transform.forward, flatDirection.normalized) < minForwardDot) continue; // Дверь сбоку или сзади пропускаем

            float sqrDistance = (point - transform.position).sqrMagnitude; // Считаем дистанцию без квадратного корня
            if (sqrDistance >= bestSqrDistance) continue; // Более далёкую дверь не выбираем
            if (!HasClearLineToDoor(door, body, start)) continue; // Дверь за стеной не открываем

            bestSqrDistance = sqrDistance; // Запоминаем лучшую дистанцию
            foundDoor = door; // Запоминаем дверь
            foundPoint = body.bounds.center; // Запоминаем точку поворота
        }

        return foundDoor != null; // Возвращаем true, если дверь найдена
    }

    private bool HasClearLineToDoor(UniversalDoor door, Collider body, Vector3 start) // Проверяет, что первым объектом луча является нужная дверь
    {
        Vector3 target = body.ClosestPoint(start); // Направляем луч в ближайшую точку дверного полотна
        Vector3 direction = target - start; // Получаем направление луча
        float distance = direction.magnitude; // Получаем длину луча

        if (distance <= 0.01f) return true; // При непосредственном контакте считаем дверь доступной

        bool hasHit = Physics.Raycast(start, direction / distance, out RaycastHit hit, distance + lineOfSightExtraDistance, lineOfSightLayers, QueryTriggerInteraction.Ignore); // Луч не создаёт Garbage Collector
        bool visible = hasHit && hit.collider.GetComponentInParent<UniversalDoor>() == door; // Дверь видна только если попалась первой

        if (drawLineOfSight) Debug.DrawLine(start, target, visible ? Color.green : Color.yellow, checkCooldown); // Зелёная линия — доступна, жёлтая — закрыта стеной
        return visible; // Возвращаем результат проверки
    }

    private IEnumerator OpenDoorSequence(UniversalDoor door, Vector3 doorPoint) // Полная последовательность открытия
    {
        isHandlingDoor = true; // Блокируем обычную AI-логику
        currentDoor = door; // Запоминаем текущую дверь
        currentDoorPoint = doorPoint; // Запоминаем точку поворота

        if (movement != null) movement.LockMovement(); // Останавливаем монстра без удаления старого маршрута
        yield return null; // Даём Animator Sync перейти из Walk или Run в Idle
        yield return TurnTowardsDoor(); // Поворачиваем монстра лицом к двери

        PlayOpenDoorAnimation(); // Запускаем Trigger OpenDoor
        yield return WaitLocked(animationOpenMoment); // Ждём момент соприкосновения руки с дверью

        bool openingStarted = currentDoor != null && (currentDoor.IsOpen || currentDoor.OpenDoorForMonster()); // Открываем дверь или ждём уже открывающуюся

        if (!openingStarted) // Если открыть дверь не удалось
        {
            FinishDoorSequence(); // Снимаем блокировку движения
            yield break; // Завершаем последовательность
        }

        float timer = 0f; // Создаём таймер ожидания полного открытия

        while (currentDoor != null && !currentDoor.IsFullyOpen && timer < maxDoorOpenWait) // Ждём физического освобождения прохода
        {
            timer += Time.deltaTime; // Увеличиваем таймер
            KeepMonsterStopped(); // Не даём монстру войти в полотно
            yield return null; // Ждём следующий кадр
        }

        yield return WaitLocked(afterFullyOpenDelay); // Даём NavMeshAgent обновить обход открытого полотна
        FinishDoorSequence(); // Возвращаем монстра к сохранённому маршруту
    }

    private IEnumerator TurnTowardsDoor() // Плавно поворачивает монстра к двери
    {
        float timer = 0f; // Ограничиваем поворот одной секундой

        while (timer < 1f) // Поворачиваемся до нужного угла или до тайм-аута
        {
            timer += Time.deltaTime; // Увеличиваем таймер
            Vector3 direction = currentDoorPoint - transform.position; // Получаем направление к двери
            direction.y = 0f; // Не наклоняем монстра вверх или вниз

            if (direction.sqrMagnitude < 0.0001f) yield break; // Некорректное направление завершает поворот

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized); // Рассчитываем нужный мировой поворот
            if (Quaternion.Angle(transform.rotation, targetRotation) <= facingAngle) yield break; // Монстр уже смотрит достаточно точно

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime); // Плавно поворачиваем корень Monster
            KeepMonsterStopped(); // Удерживаем NavMeshAgent на месте
            yield return null; // Ждём следующий кадр
        }
    }

    private IEnumerator WaitLocked(float duration) // Ждёт указанное время и удерживает монстра
    {
        for (float timer = 0f; timer < duration; timer += Time.deltaTime) // Выполняем ожидание без WaitForSeconds
        {
            KeepMonsterStopped(); // Не даём другим скриптам включить движение
            yield return null; // Ждём следующий кадр
        }
    }

    private void PlayOpenDoorAnimation() // Запускает анимацию открытия двери
    {
        if (animator == null) return; // Без Animator пропускаем анимацию
        animator.ResetTrigger(OpenDoorHash); // Сбрасываем возможный старый Trigger
        animator.SetTrigger(OpenDoorHash); // Запускаем состояние MonsterOpenDoor
    }

    private void KeepMonsterStopped() // Удерживает движение заблокированным
    {
        if (movement != null) movement.KeepMovementLocked(); // Используем существующую систему движения
    }

    private void FinishDoorSequence() // Завершает работу с дверью
    {
        currentDoor = null; // Очищаем ссылку на дверь
        currentDoorPoint = Vector3.zero; // Очищаем точку поворота
        isHandlingDoor = false; // Разрешаем обычную AI-логику
        if (movement != null) movement.UnlockMovement(); // Продолжаем сохранённый маршрут
        nextCheckTime = Time.time + checkCooldown; // Предотвращаем мгновенную повторную проверку
    }

    private Vector3 GetCheckStart() => transform.position + Vector3.up * checkHeight; // Возвращаем начало проверки на уровне корпуса

    private void DrawForwardDebug(Vector3 start) // Рисует направление проверки
    {
        if (drawDebugRay) Debug.DrawRay(start, transform.forward * doorCheckDistance, Color.red, checkCooldown); // Показываем максимальную дистанцию
    }

    private void OnDisable() // Вызывается при выключении монстра или компонента
    {
        StopAllCoroutines(); // Останавливаем незавершённую последовательность
        currentDoor = null; // Очищаем ссылку
        isHandlingDoor = false; // Сбрасываем состояние
        if (movement != null && movement.IsMovementLocked) movement.UnlockMovement(); // Не оставляем монстра заблокированным
    }

    private void OnDrawGizmosSelected() // Рисует сферу поиска в Scene
    {
        if (!drawDebugSphere) return; // Если debug выключен, ничего не рисуем
        Gizmos.color = Color.red; // Выбираем красный цвет
        Gizmos.DrawWireSphere(GetCheckStart() + transform.forward * forwardOffset, doorCheckRadius); // Рисуем сферу перед монстром
    }
}