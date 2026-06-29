using UnityEngine; // Подключаем Unity-классы
using System.Collections; // Подключаем корутины

public class WardrobeHideHandle : MonoBehaviour, IInteractable // Ручка шкафа: E открывает дверь, Q прячет игрока
{
    public PlayerHideController playerHideController; // Ссылка на систему пряток игрока

    public UniversalDoor wardrobeDoor; // Дверь шкафа, которую нужно открыть или закрыть

    public Transform hidePoint; // Точка внутри шкафа, куда переносится игрок

    public Transform exitPoint; // Точка выхода перед шкафом

    public float doorOpenBeforeHideDelay = 0.4f; // Пауза после открытия двери перед прятаньем

    public float doorCloseAfterHideDelay = 0.4f; // Пауза перед закрытием двери после прятанья

    private bool isHiding = false; // Защита от повторного запуска прятанья

    public void Interact() // Вызывается PlayerInteractor при нажатии E
    {
        if (isHiding) return; // Если уже идёт прятанье — ничего не делаем

        if (playerHideController != null && playerHideController.isHidden) return; // Если игрок уже спрятан — дверь снаружи не трогаем

        if (wardrobeDoor != null) wardrobeDoor.Interact(); // E открывает или закрывает дверь шкафа
    }

    public void TryHide() // Вызывается PlayerInteractor при нажатии Q
    {
        if (isHiding) return; // Если уже идёт прятанье — выходим

        if (playerHideController == null) return; // Если контроллер пряток не назначен — выходим

        if (playerHideController.isHidden) return; // Если игрок уже спрятан — выходим

        if (hidePoint == null || exitPoint == null) return; // Если точки не назначены — выходим

        StartCoroutine(HideSequence()); // Запускаем последовательность прятанья
    }

    private IEnumerator HideSequence() // Последовательность залезания в шкаф
    {
        isHiding = true; // Блокируем повторный запуск

        bool monsterSawHide = playerHideController.WouldMonsterSeeHideNow(); // Проверяем, видел ли монстр игрока перед входом в шкаф

        if (wardrobeDoor != null) wardrobeDoor.OpenDoor(); // Открываем дверь шкафа

        yield return new WaitForSeconds(doorOpenBeforeHideDelay); // Ждём, чтобы дверь успела открыться

        playerHideController.Hide(hidePoint, exitPoint, wardrobeDoor); // Прячем игрока внутрь шкафа

        if (monsterSawHide) // Если монстр видел вход в шкаф
        {
            playerHideController.PunishSeenHide(wardrobeDoor); // Запускаем вытаскивание и Game Over

            isHiding = false; // Разрешаем системе выйти из состояния прятанья

            yield break; // Не закрываем дверь, потому что монстр “нашёл” игрока
        }

        yield return new WaitForSeconds(doorCloseAfterHideDelay); // Ждём немного после входа

        if (wardrobeDoor != null) wardrobeDoor.CloseDoor(); // Закрываем дверь шкафа

        isHiding = false; // Разрешаем следующий запуск
    }
}