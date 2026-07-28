using UnityEngine; // Подключаем основные классы Unity
using UnityEngine.AI; // Подключаем NavMeshAgent

public class MonsterMovement : MonoBehaviour // Отвечает только за движение монстра
{
    [Header("References")] // Заголовок блока ссылок
    public NavMeshAgent agent; // Ссылка на NavMeshAgent на корневом Monster

    [Header("Path Update")] // Заголовок настроек маршрута
    public float repathInterval = 0.15f; // Как часто разрешено обновлять маршрут
    public float minTargetMoveDistance = 0.25f; // Насколько должна сместиться цель

    private float defaultSpeed; // Стандартная скорость монстра
    private float lastRepathTime = -999f; // Время последнего обновления пути
    private Vector3 lastDestination; // Последняя назначенная цель
    private bool hasDestination = false; // Есть ли сохранённая цель
    private bool movementLocked = false; // Заблокировано ли движение дверной логикой

    public bool IsMovementLocked => movementLocked; // Публичная проверка блокировки движения

    private void Reset() // Автозаполнение при добавлении компонента
    {
        agent = GetComponent<NavMeshAgent>(); // Ищем NavMeshAgent на этом объекте
    }

    private void Awake() // Вызывается при запуске объекта
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>(); // Если ссылка пустая, ищем агент
        if (agent != null) defaultSpeed = agent.speed; // Запоминаем стартовую скорость
    }

    public bool IsReady() // Проверяем готовность агента
    {
        if (agent == null) return false; // Если агента нет, движение невозможно
        if (!agent.isActiveAndEnabled) return false; // Если агент выключен, движение невозможно
        if (!agent.isOnNavMesh) return false; // Если агент не на NavMesh, движение невозможно
        return true; // Агент готов
    }

    public void MoveTo(Vector3 position) // Двигаться к указанной точке
    {
        if (movementLocked) return; // Во время открытия двери не разрешаем запуск движения
        if (!IsReady()) return; // Если агент не готов, выходим

        if (hasDestination) // Если ранее уже была назначена цель
        {
            float distance = Vector3.Distance(lastDestination, position); // Считаем изменение цели
            bool targetMovedLittle = distance < minTargetMoveDistance; // Проверяем, мало ли сместилась цель
            bool repathTooSoon = Time.time < lastRepathTime + repathInterval; // Проверяем частоту перестроения
            if (targetMovedLittle && repathTooSoon) return; // Не перестраиваем почти тот же путь слишком часто
        }

        agent.isStopped = false; // Разрешаем движение
        agent.SetDestination(position); // Назначаем новую цель
        lastDestination = position; // Запоминаем цель
        lastRepathTime = Time.time; // Запоминаем время
        hasDestination = true; // Помечаем, что цель есть
    }

    public void MoveToImmediate(Vector3 position) // Немедленно назначить новую цель
    {
        if (movementLocked) return; // Во время открытия двери маршрут не меняем
        if (!IsReady()) return; // Если агент не готов, выходим

        agent.isStopped = false; // Разрешаем движение
        agent.SetDestination(position); // Назначаем цель
        lastDestination = position; // Запоминаем цель
        lastRepathTime = Time.time; // Запоминаем время
        hasDestination = true; // Помечаем наличие цели
    }

    public void Stop() // Полностью остановить монстра и удалить текущий путь
    {
        if (!IsReady()) return; // Если агент не готов, выходим

        agent.ResetPath(); // Удаляем маршрут
        agent.isStopped = true; // Останавливаем агента
        hasDestination = false; // Сбрасываем цель
    }

    public void Resume() // Разрешить обычное движение
    {
        if (movementLocked) return; // Пока открывается дверь, движение не возобновляем
        if (!IsReady()) return; // Если агент не готов, выходим

        agent.isStopped = false; // Разрешаем движение
    }

    public void LockMovement() // Остановить монстра, но сохранить текущий маршрут
    {
        movementLocked = true; // Включаем программную блокировку
        if (!IsReady()) return; // Если агент не готов, выходим

        agent.isStopped = true; // Останавливаем без ResetPath
    }

    public void KeepMovementLocked() // Повторно удерживать монстра на месте
    {
        if (!movementLocked) return; // Если блокировки нет, ничего не делаем
        if (!IsReady()) return; // Если агент не готов, выходим

        agent.isStopped = true; // Не даём другим скриптам включить движение
    }

    public void UnlockMovement() // Снять дверную блокировку
    {
        movementLocked = false; // Снимаем программную блокировку
        if (!IsReady()) return; // Если агент не готов, выходим

        agent.isStopped = false; // Разрешаем продолжить сохранённый маршрут
    }

    public void SetSpeed(float speed) // Установить скорость
    {
        if (agent == null) return; // Если агента нет, выходим
        agent.speed = speed; // Устанавливаем скорость
    }

    public void RestoreDefaultSpeed() // Вернуть стандартную скорость
    {
        if (agent == null) return; // Если агента нет, выходим
        if (defaultSpeed <= 0f) defaultSpeed = agent.speed; // Если скорость не сохранилась, берём текущую
        agent.speed = defaultSpeed; // Возвращаем стандартную скорость
    }

    public bool HasArrived(float extraDistance) // Проверить достижение цели
    {
        if (!IsReady()) return false; // Если агент не готов, цель не достигнута
        if (agent.pathPending) return false; // Пока путь строится, ждём
        return agent.remainingDistance <= agent.stoppingDistance + extraDistance; // Проверяем дистанцию
    }

    private void OnDisable() // Вызывается при выключении объекта или компонента
    {
        movementLocked = false; // Сбрасываем блокировку
    }
}