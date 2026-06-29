using UnityEngine; // Подключаем Unity

public class MonsterVision : MonoBehaviour // Отвечает только за зрение монстра
{
    public Transform player; // Ссылка на игрока

    public PlayerHideController playerHide; // Ссылка на систему пряток игрока

    public float viewDistance = 8f; // Дистанция зрения

    public float viewAngle = 115f; // Угол зрения

    public LayerMask obstacleMask = ~0; // Слои препятствий

    public bool CanSeePlayer() // Проверить, видит ли монстр игрока с учётом пряток
    {
        return CheckCanSeePlayer(false); // Проверяем зрение и учитываем, что спрятанного игрока не видно
    }

    public bool CanSeePlayerIgnoringHide() // Проверить, видел бы монстр игрока без учёта шкафа
    {
        return CheckCanSeePlayer(true); // Проверяем зрение и игнорируем состояние isHidden
    }

    private bool CheckCanSeePlayer(bool ignoreHideState) // Общая проверка зрения
    {
        if (player == null) return false; // Если игрок не назначен — не видим

        if (!ignoreHideState && playerHide != null && playerHide.isHidden) return false; // Если игрок спрятан и мы не игнорируем прятки — не видим

        float distance = Vector3.Distance(transform.position, player.position); // Считаем дистанцию до игрока

        if (distance > viewDistance) return false; // Если игрок слишком далеко — не видим

        Vector3 directionToPlayer = (player.position - transform.position).normalized; // Считаем направление к игроку

        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer); // Считаем угол до игрока

        if (angleToPlayer > viewAngle * 0.5f) return false; // Если игрок вне сектора зрения — не видим

        Vector3 rayStart = transform.position + Vector3.up * 1.4f; // Поднимаем луч до уровня головы монстра

        if (Physics.Raycast(rayStart, directionToPlayer, out RaycastHit hit, viewDistance, obstacleMask, QueryTriggerInteraction.Ignore)) // Пускаем луч зрения
        {
            if (!hit.transform.IsChildOf(player) && hit.transform != player) return false; // Если луч упёрся не в игрока — не видим
        }

        return true; // Игрок виден
    }
}