using UnityEngine; // Подключаем основные классы Unity
using UnityEngine.AI; // Подключаем систему NavMesh и NavMeshAgent

public class MonsterAnimatorSync : MonoBehaviour // Синхронизирует движение NavMeshAgent с Animator монстра
{
    [Header("References")] // Заголовок блока ссылок в Inspector

    public NavMeshAgent agent; // Ссылка на NavMeshAgent корневого объекта Monster

    public Animator animator; // Ссылка на Animator дочерней модели LolyGirl


    [Header("Movement Animation")] // Заголовок настроек анимации движения

    public float movingThreshold = 0.1f; // Минимальная фактическая скорость, после которой монстр считается движущимся

    public float runningSpeedThreshold = 2f; // Заданная скорость NavMeshAgent, начиная с которой включается Run


    private static readonly int isMovingHash = Animator.StringToHash("IsMoving");
    // Сохраняем параметр IsMoving в виде числового идентификатора

    private static readonly int isRunningHash = Animator.StringToHash("IsRunning");
    // Сохраняем параметр IsRunning в виде числового идентификатора


    private void Reset() // Вызывается Unity при первом добавлении компонента
    {
        agent = GetComponent<NavMeshAgent>();
        // Пытаемся найти NavMeshAgent на этом же объекте

        animator = GetComponentInChildren<Animator>(true);
        // Пытаемся найти Animator на дочерней модели, включая выключенные объекты
    }


    private void Awake() // Вызывается при запуске сцены
    {
        if (agent == null) // Проверяем, назначен ли NavMeshAgent
        {
            agent = GetComponent<NavMeshAgent>();
            // Если ссылка пустая, ищем NavMeshAgent на объекте автоматически
        }

        if (animator == null) // Проверяем, назначен ли Animator
        {
            animator = GetComponentInChildren<Animator>(true);
            // Если ссылка пустая, ищем Animator среди дочерних объектов
        }
    }


    private void Update() // Вызывается каждый кадр
    {
        if (agent == null) // Проверяем наличие NavMeshAgent
        {
            return;
            // Если NavMeshAgent отсутствует, прекращаем выполнение
        }

        if (animator == null) // Проверяем наличие Animator
        {
            return;
            // Если Animator отсутствует, прекращаем выполнение
        }

        bool agentCanMove =
            agent.isActiveAndEnabled &&
            agent.isOnNavMesh &&
            !agent.isStopped;
        // Проверяем, что агент включён, находится на NavMesh и не остановлен принудительно

        float currentVelocity = agent.velocity.magnitude;
        // Получаем фактическую текущую скорость перемещения монстра

        bool isMoving =
            agentCanMove &&
            currentVelocity > movingThreshold;
        // Монстр считается движущимся, если агент работает и фактическая скорость выше порога

        bool isRunning =
            isMoving &&
            agent.speed >= runningSpeedThreshold;
        // Run включается только во время движения и при достаточно высокой заданной скорости агента

        animator.SetBool(isMovingHash, isMoving);
        // Передаём состояние движения в параметр IsMoving

        animator.SetBool(isRunningHash, isRunning);
        // Передаём состояние бега в параметр IsRunning
    }


    private void OnDisable() // Вызывается, когда компонент или объект выключается
    {
        if (animator == null) // Проверяем наличие Animator
        {
            return;
            // Если Animator отсутствует, ничего не меняем
        }

        animator.SetBool(isMovingHash, false);
        // При выключении объекта сбрасываем IsMoving

        animator.SetBool(isRunningHash, false);
        // При выключении объекта сбрасываем IsRunning
    }
}