using UnityEngine; // Подключаем Unity

public class MonsterDoorOpener : MonoBehaviour // Отвечает только за открытие дверей монстром
{
    public float doorCheckDistance = 1.8f; // Дистанция проверки двери перед монстром

    public float doorCheckRadius = 0.6f; // Радиус проверки двери перед монстром

    public float checkHeight = 1.0f; // Высота проверки от пола

    public float forwardOffset = 1.0f; // Смещение проверки вперёд от монстра

    public float minForwardDot = 0.2f; // Насколько дверь должна быть впереди монстра

    public LayerMask doorLayers = ~0; // Слои дверей

    public bool drawDebugRay = true; // Рисовать ли debug-луч

    public bool drawDebugSphere = true; // Рисовать ли debug-сферу

    public void TryOpenDoorAhead() // Попробовать открыть дверь перед монстром
    {
        Vector3 rayStart = transform.position + Vector3.up * checkHeight; // Точка старта проверки на высоте груди монстра

        Vector3 checkCenter = rayStart + transform.forward * forwardOffset; // Центр сферы проверки перед монстром

        if (drawDebugRay) Debug.DrawRay(rayStart, transform.forward * doorCheckDistance, Color.red); // Рисуем луч вперёд

        Collider[] hits = Physics.OverlapSphere(checkCenter, doorCheckRadius, doorLayers, QueryTriggerInteraction.Collide); // Ищем коллайдеры дверей рядом

        UniversalDoor bestDoor = null; // Лучшая найденная дверь

        float bestDistance = Mathf.Infinity; // Дистанция до лучшей двери

        foreach (Collider hit in hits) // Перебираем найденные коллайдеры
        {
            if (hit == null) continue; // Если коллайдер пустой — пропускаем

            UniversalDoor door = hit.GetComponentInParent<UniversalDoor>(); // Ищем UniversalDoor в родителях

            if (door == null) continue; // Если двери нет — пропускаем

            if (!door.canMonsterOpen) continue; // Если монстру нельзя открывать эту дверь — пропускаем

            if (door.IsOpen) continue; // Если дверь уже открыта — пропускаем

            Vector3 directionToDoor = (hit.transform.position - transform.position).normalized; // Считаем направление от монстра к двери

            float forwardDot = Vector3.Dot(transform.forward, directionToDoor); // Проверяем, находится ли дверь впереди монстра

            if (forwardDot < minForwardDot) continue; // Если дверь сбоку или сзади — пропускаем

            float distance = Vector3.Distance(transform.position, hit.transform.position); // Считаем дистанцию до двери

            if (distance < bestDistance) // Если эта дверь ближе предыдущей
            {
                bestDistance = distance; // Запоминаем лучшую дистанцию

                bestDoor = door; // Запоминаем лучшую дверь
            }
        }

        if (bestDoor == null) return; // Если подходящей двери нет — выходим

        bestDoor.OpenDoorForMonster(); // Открываем найденную дверь монстром
    }

    private void OnDrawGizmosSelected() // Рисуем сферу проверки в редакторе
    {
        if (!drawDebugSphere) return; // Если debug-сфера выключена — выходим

        Vector3 rayStart = transform.position + Vector3.up * checkHeight; // Точка старта проверки

        Vector3 checkCenter = rayStart + transform.forward * forwardOffset; // Центр сферы проверки

        Gizmos.color = Color.red; // Цвет debug-сферы

        Gizmos.DrawWireSphere(checkCenter, doorCheckRadius); // Рисуем сферу проверки
    }
}