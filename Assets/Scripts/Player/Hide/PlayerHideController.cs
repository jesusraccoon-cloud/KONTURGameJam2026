using UnityEngine; // Подключаем Unity-классы
using System.Collections; // Подключаем корутины

public class PlayerHideController : MonoBehaviour // Контроллер пряток игрока через отдельную кнопку Q
{
    public bool isHidden = false; // Спрятан ли игрок сейчас

    public CharacterController characterController; // CharacterController игрока

    public Behaviour[] movementScriptsToDisable; // Скрипты движения, которые отключаются в шкафу

    public MonsterVision monsterVision; // Зрение монстра для проверки “видел ли он вход в шкаф”

    public MonsterAttack monsterAttack; // Атака монстра для смерти при прятках на глазах

    public bool dieIfMonsterSeesHide = true; // Убивать ли игрока, если он спрятался на глазах монстра

    public float exitInputDelay = 0.5f; // Задержка после входа, чтобы нельзя было сразу выйти

    public float doorOpenBeforeExitDelay = 0.4f; // Пауза после открытия двери перед выходом

    public float doorCloseAfterExitDelay = 0.4f; // Пауза перед закрытием двери после выхода

    private Transform currentExitPoint; // Точка выхода из текущего шкафа

    private UniversalDoor currentDoor; // Дверь текущего шкафа

    private float hideEnterTime = 0f; // Время, когда игрок спрятался

    private bool isExiting = false; // Защита от повторного выхода

    private void Reset() // Вызывается при добавлении скрипта на объект
    {
        characterController = GetComponent<CharacterController>(); // Автоматически ищем CharacterController
    }

    public bool WouldMonsterSeeHideNow() // Проверить, видит ли монстр игрока прямо сейчас перед прятками
    {
        if (!dieIfMonsterSeesHide) return false; // Если наказание выключено — монстр как будто не видел

        if (monsterVision == null) return false; // Если зрение монстра не назначено — не считаем игрока увиденным

        return monsterVision.CanSeePlayerIgnoringHide(); // Проверяем зрение без учёта будущего шкафа
    }

    public void Hide(Transform hidePoint, Transform exitPoint, UniversalDoor door) // Метод входа в шкаф
    {
        if (isHidden) return; // Если игрок уже спрятан — выходим

        if (hidePoint == null || exitPoint == null) return; // Если точки не назначены — выходим

        isHidden = true; // Помечаем игрока как спрятанного

        hideEnterTime = Time.time; // Запоминаем момент входа

        currentExitPoint = exitPoint; // Запоминаем точку выхода

        currentDoor = door; // Запоминаем дверь шкафа

        SetMovement(false); // Отключаем движение игрока

        TeleportPlayer(hidePoint.position); // Переносим игрока внутрь шкафа
    }

    public void PunishSeenHide(UniversalDoor wardrobeDoor) // Наказать игрока за прятки на глазах монстра
    {
        if (!dieIfMonsterSeesHide) return; // Если наказание выключено — выходим

        if (monsterAttack == null) return; // Если атака монстра не назначена — выходим

        monsterAttack.StartHideCatchAttack(wardrobeDoor); // Запускаем вытаскивание из шкафа и Game Over
    }

    public void TryExitHide() // Попытка выйти из шкафа по Q
    {
        if (!isHidden) return; // Если игрок не спрятан — выходим

        if (isExiting) return; // Если уже выходим — выходим

        if (Time.time < hideEnterTime + exitInputDelay) return; // Если задержка после входа ещё не прошла — выходим

        StartCoroutine(ExitHideSequence()); // Запускаем выход из шкафа
    }

    private IEnumerator ExitHideSequence() // Последовательность выхода из шкафа
    {
        isExiting = true; // Блокируем повторный выход

        if (currentDoor != null) currentDoor.OpenDoor(); // Открываем дверь шкафа

        yield return new WaitForSeconds(doorOpenBeforeExitDelay); // Ждём открытия двери

        isHidden = false; // Игрок больше не спрятан

        if (currentExitPoint != null) TeleportPlayer(currentExitPoint.position); // Переносим игрока наружу

        SetMovement(true); // Включаем движение игрока

        yield return new WaitForSeconds(doorCloseAfterExitDelay); // Ждём после выхода

        if (currentDoor != null) currentDoor.CloseDoor(); // Закрываем дверь шкафа

        currentDoor = null; // Очищаем ссылку на дверь

        currentExitPoint = null; // Очищаем точку выхода

        isExiting = false; // Разрешаем следующий выход
    }

    private void TeleportPlayer(Vector3 targetPosition) // Безопасный перенос игрока
    {
        if (characterController != null) characterController.enabled = false; // Отключаем CharacterController перед переносом

        transform.position = targetPosition; // Переносим игрока

        if (characterController != null) characterController.enabled = true; // Включаем CharacterController обратно
    }

    private void SetMovement(bool enabledState) // Включает или выключает движение
    {
        if (movementScriptsToDisable == null) return; // Если массив не назначен — выходим

        for (int i = 0; i < movementScriptsToDisable.Length; i++) // Перебираем все скрипты движения
        {
            if (movementScriptsToDisable[i] != null) movementScriptsToDisable[i].enabled = enabledState; // Включаем или выключаем скрипт
        }
    }
}