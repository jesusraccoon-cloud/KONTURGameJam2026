using UnityEngine; // Подключаем основные классы Unity

public class MonsterVision : MonoBehaviour // Отвечает за зрение и ближнюю осведомлённость монстра
{
    [Header("References")] // Ссылки на игрока
    public Transform player; // Корневой Transform игрока
    public PlayerHideController playerHide; // Система пряток игрока

    [Header("Normal Vision")] // Обычное зрение перед монстром
    public float viewDistance = 8f; // Максимальная дистанция обычного зрения
    public float viewAngle = 115f; // Полный угол обычного сектора зрения

    [Header("Close Awareness 360")] // Круговое обнаружение вблизи
    public bool useCloseAwareness = true; // Использовать ближнее обнаружение за спиной
    public float closeAwarenessRadius = 1.5f; // Радиус гарантированного обнаружения игрока
    public bool drawCloseAwareness = true; // Показывать радиус в Scene

    [Header("Line Of Sight")] // Проверка стен и дверей
    public float rayStartHeight = 1.4f; // Высота начала луча от монстра
    public float playerTargetHeight = 1f; // Высота точки на теле игрока
    public LayerMask obstacleMask = ~0; // Слои игрока, стен, дверей и препятствий

    public bool CanSeePlayer() // Проверить обнаружение игрока с учётом пряток
    {
        return CheckPlayerDetection(false); // Спрятанный игрок не обнаруживается
    }

    public bool CanSeePlayerIgnoringHide() // Проверить обнаружение без учёта пряток
    {
        return CheckPlayerDetection(true); // Используется специальной логикой при необходимости
    }

    private bool CheckPlayerDetection(bool ignoreHideState) // Общая проверка зрения и ближней зоны
    {
        if (player == null) return false; // Без назначенного игрока обнаружение невозможно
        if (!ignoreHideState && playerHide != null && playerHide.isHidden) return false; // Спрятанного игрока игнорируем

        Vector3 flatDirection = player.position - transform.position; // Получаем направление к игроку
        flatDirection.y = 0f; // Не учитываем разницу высоты для дистанции и угла

        float sqrDistance = flatDirection.sqrMagnitude; // Считаем квадрат дистанции без квадратного корня
        float closeRadiusSqr = closeAwarenessRadius * closeAwarenessRadius; // Рассчитываем квадрат ближнего радиуса
        float viewDistanceSqr = viewDistance * viewDistance; // Рассчитываем квадрат дистанции зрения

        bool insideCloseRadius = useCloseAwareness && sqrDistance <= closeRadiusSqr; // Проверяем круговую ближнюю зону

        if (!insideCloseRadius) // Если игрок не находится в ближней зоне
        {
            if (sqrDistance > viewDistanceSqr) return false; // Игрок находится слишком далеко

            if (flatDirection.sqrMagnitude > 0.0001f) // Проверяем корректность направления
            {
                float angle = Vector3.Angle(transform.forward, flatDirection.normalized); // Считаем угол до игрока
                if (angle > viewAngle * 0.5f) return false; // За пределами обычного сектора игрок не виден
            }
        }

        return HasClearLineToPlayer(); // В ближней и обычной зоне обязательно проверяем стену
    }

    private bool HasClearLineToPlayer() // Проверяет отсутствие стены между монстром и игроком
    {
        Vector3 rayStart = transform.position + Vector3.up * rayStartHeight; // Поднимаем начало луча до головы монстра
        Vector3 targetPoint = player.position + Vector3.up * playerTargetHeight; // Направляем луч в корпус игрока
        Vector3 direction = targetPoint - rayStart; // Получаем направление луча
        float distance = direction.magnitude; // Получаем точную длину луча

        if (distance <= 0.01f) return true; // При почти нулевой дистанции игрок обнаружен

        if (!Physics.Raycast(rayStart, direction / distance, out RaycastHit hit, distance + 0.1f, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return true; // Если луч ни во что не упёрся, путь к игроку свободен
        }

        if (hit.transform == player) return true; // Луч попал в корневой объект игрока
        if (hit.transform.IsChildOf(player)) return true; // Луч попал в дочерний коллайдер игрока

        return false; // Первым объектом стала стена, дверь или другое препятствие
    }

    private void OnDrawGizmosSelected() // Рисует ближний радиус в Scene
    {
        if (!drawCloseAwareness) return; // Если отображение выключено, ничего не рисуем
        if (!useCloseAwareness) return; // Если механика выключена, радиус не показываем

        Gizmos.color = Color.yellow; // Выбираем жёлтый цвет
        Gizmos.DrawWireSphere(transform.position, closeAwarenessRadius); // Рисуем круговую зону обнаружения
    }
}