using UnityEngine; // Подключаем Unity
using UnityEngine.AI; // Подключаем NavMeshAgent

public class MonsterPatrol : MonoBehaviour // Скрипт патруля монстра
{
    public bool isPatrolActive = false; // Активен ли патруль

    public NavMeshAgent agent; // Ссылка на NavMeshAgent

    public Transform[] patrolPoints; // Точки патруля

    public float waitTime = 2f; // Сколько ждать на точке

    public float arriveExtraDistance = 0.3f; // Дополнительная дистанция, чтобы считать точку достигнутой

    private int currentPointIndex = 0; // Индекс текущей точки

    private float waitTimer = 0f; // Таймер ожидания

    private bool hasDestination = false; // Есть ли сейчас назначенная точка

    private void Reset() // Автозаполнение при добавлении скрипта
    {
        agent = GetComponent<NavMeshAgent>(); // Ищем NavMeshAgent на этом объекте
    }

    private void Awake() // Вызывается при запуске объекта
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>(); // Если агент не назначен — ищем автоматически
    }

    private void Update() // Каждый кадр
    {
        if (!isPatrolActive) return; // Если патруль выключен — выходим

        if (agent == null) return; // Если агента нет — выходим

        if (!agent.isActiveAndEnabled) return; // Если агент выключен — выходим

        if (!agent.isOnNavMesh) return; // Если агент не на NavMesh — выходим

        if (patrolPoints == null || patrolPoints.Length == 0) return; // Если точек нет — выходим

        if (!hasDestination) // Если точка ещё не назначена
        {
            SetCurrentPointDestination(); // Назначаем текущую точку

            return; // Выходим до следующего кадра
        }

        if (agent.pathPending) return; // Если путь ещё строится — ждём

        if (agent.remainingDistance > agent.stoppingDistance + arriveExtraDistance) return; // Если ещё не дошли — выходим

        waitTimer += Time.deltaTime; // Увеличиваем таймер ожидания

        if (waitTimer < waitTime) return; // Если ждать ещё рано — выходим

        GoToNextPoint(); // Переходим к следующей точке
    }

    public void StartPatrol() // Запустить патруль
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>(); // Если агент не назначен — ищем

        if (agent == null) return; // Если агента всё равно нет — выходим

        if (!agent.isActiveAndEnabled) return; // Если агент выключен — выходим

        if (!agent.isOnNavMesh) return; // Если агент не стоит на NavMesh — выходим

        if (patrolPoints == null || patrolPoints.Length == 0) return; // Если точек нет — выходим

        isPatrolActive = true; // Включаем патруль

        waitTimer = 0f; // Сбрасываем таймер ожидания

        hasDestination = false; // Сбрасываем назначенную точку

        agent.isStopped = false; // ВАЖНО: разрешаем агенту двигаться после любых Stop()

        agent.ResetPath(); // Сбрасываем старый путь

        SetCurrentPointDestination(); // Назначаем текущую точку
    }

    public void StopPatrol() // Остановить патруль
    {
        isPatrolActive = false; // Выключаем патруль

        waitTimer = 0f; // Сбрасываем таймер

        hasDestination = false; // Сбрасываем точку
    }

    private void SetCurrentPointDestination() // Назначить текущую точку
    {
        if (patrolPoints[currentPointIndex] == null) // Если текущая точка пустая
        {
            GoToNextPoint(); // Переходим к следующей

            return; // Выходим
        }

        agent.isStopped = false; // Разрешаем движение

        agent.SetDestination(patrolPoints[currentPointIndex].position); // Отправляем монстра к текущей точке

        hasDestination = true; // Запоминаем, что цель назначена
    }

    private void GoToNextPoint() // Перейти к следующей точке
    {
        waitTimer = 0f; // Сбрасываем ожидание

        currentPointIndex++; // Увеличиваем индекс

        if (currentPointIndex >= patrolPoints.Length) currentPointIndex = 0; // Если вышли за массив — возвращаемся к первой точке

        hasDestination = false; // Сбрасываем цель, чтобы назначить новую
    }
}