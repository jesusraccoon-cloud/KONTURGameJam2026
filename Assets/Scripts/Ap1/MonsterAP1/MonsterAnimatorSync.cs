using UnityEngine;                 // подключаем базовые классы Unity (Transform, MonoBehaviour и т.д.)
using UnityEngine.AI;              // подключаем NavMeshAgent (навигация)

public class MonsterAnimatorSync : MonoBehaviour   // создаём класс-скрипт, который можно повесить на объект
{
    public NavMeshAgent agent;     // ссылка на компонент NavMeshAgent (движение монстра)
    public Animator animator;      // ссылка на Animator (переключение состояний Idle/Walk/Attack)

    public float movingThreshold = 0.1f;   // порог скорости: если скорость выше — считаем, что монстр движется

    void Reset()                   // вызывается автоматически при добавлении скрипта на объект
    {
        agent = GetComponent<NavMeshAgent>();   // пытаемся автоматически найти NavMeshAgent на этом объекте
        animator = GetComponent<Animator>();    // пытаемся автоматически найти Animator на этом объекте
    }

    void Update()                  // вызывается каждый кадр
    {
        if (agent == null || animator == null)  // проверяем, что ссылки не пустые (иначе будет ошибка)
            return;                             // если нет — выходим и ничего не делаем

        bool isMoving = agent.velocity.magnitude > movingThreshold;  
        // берём текущую скорость агента (vector) и сравниваем с порогом:
        // если скорость больше порога → true (движется)
        // если меньше → false (стоит)

        animator.SetBool("IsMoving", isMoving); 
        // передаём значение в Animator:
        // true → переключится в Walk
        // false → вернётся в Idle
    }
}