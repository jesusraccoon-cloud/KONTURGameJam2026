using UnityEngine; // Подключаем Unity
using UnityEngine.AI; // Подключаем NavMeshAgent

public class MonsterMovement : MonoBehaviour // Отвечает только за движение монстра
{
    public NavMeshAgent agent; // Ссылка на NavMeshAgent

    [Header("Path Update")] // Настройки обновления пути
    public float repathInterval = 0.15f; // Как часто можно обновлять путь

    public float minTargetMoveDistance = 0.25f; // Насколько должна сместиться цель

    private float defaultSpeed; // Стандартная скорость монстра

    private float lastRepathTime = -999f; // Время последного обновления пути

    private Vector3 lastDestination; // Последняя точка назначения

    private bool hasDestination = false; // Есть ли уже точка назначения

    private void Reset() // Автозаполнение
    {
        agent = GetComponent<NavMeshAgent>(); // Ищем NavMeshAgent
    }

    private void Awake() // При запуске объекта
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>(); // Если агент не назначен — ищем

        if (agent != null) defaultSpeed = agent.speed; // Запоминаем стандартную скорость
    }

    public bool IsReady() // Проверяем готовность агента
    {
        if (agent == null) return false; // Если агента нет — нельзя двигаться

        if (!agent.isActiveAndEnabled) return false; // Если агент выключен — нельзя двигаться

        if (!agent.isOnNavMesh) return false; // Если агент не на NavMesh — нельзя двигаться

        return true; // Агент готов
    }

    public void MoveTo(Vector3 position) // Двигаться к точке
    {
        if (!IsReady()) return; // Если агент не готов — выходим

        if (hasDestination) // Если цель уже была
        {
            float distance = Vector3.Distance(lastDestination, position); // Считаем смещение новой цели

            bool targetMovedLittle = distance < minTargetMoveDistance; // Проверяем, мало ли сместилась цель

            bool repathTooSoon = Time.time < lastRepathTime + repathInterval; // Проверяем, рано ли перестраивать путь

            if (targetMovedLittle && repathTooSoon) return; // Если цель почти та же и время не прошло — не обновляем путь
        }

        agent.isStopped = false; // Разрешаем движение

        agent.SetDestination(position); // Ставим обычную цель без NavMesh.SamplePosition

        lastDestination = position; // Запоминаем цель

        lastRepathTime = Time.time; // Запоминаем время

        hasDestination = true; // Помечаем, что цель есть
    }

    public void MoveToImmediate(Vector3 position) // Немедленно двигаться к точке
    {
        if (!IsReady()) return; // Если агент не готов — выходим

        agent.isStopped = false; // Разрешаем движение

        agent.SetDestination(position); // Ставим цель напрямую

        lastDestination = position; // Запоминаем цель

        lastRepathTime = Time.time; // Запоминаем время

        hasDestination = true; // Помечаем, что цель есть
    }

    public void Stop() // Остановить монстра
    {
        if (!IsReady()) return; // Если агент не готов — выходим

        agent.ResetPath(); // Сбрасываем путь

        agent.isStopped = true; // Останавливаем агента

        hasDestination = false; // Сбрасываем цель
    }

    public void Resume() // Разрешить движение
    {
        if (!IsReady()) return; // Если агент не готов — выходим

        agent.isStopped = false; // Разрешаем движение
    }

    public void SetSpeed(float speed) // Установить скорость
    {
        if (agent == null) return; // Если агента нет — выходим

        agent.speed = speed; // Ставим скорость
    }

    public void RestoreDefaultSpeed() // Вернуть стандартную скорость
    {
        if (agent == null) return; // Если агента нет — выходим

        if (defaultSpeed <= 0f) defaultSpeed = agent.speed; // Если скорость не записалась — берём текущую

        agent.speed = defaultSpeed; // Возвращаем стандартную скорость
    }

    public bool HasArrived(float extraDistance) // Проверить, дошёл ли монстр
    {
        if (!IsReady()) return false; // Если агент не готов — не дошёл

        if (agent.pathPending) return false; // Если путь строится — ждём

        return agent.remainingDistance <= agent.stoppingDistance + extraDistance; // Проверяем дистанцию
    }
}