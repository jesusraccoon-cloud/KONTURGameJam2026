using UnityEngine; // Подключаем Unity-классы
using System.Collections; // Подключаем корутины

public class PlayerHideController : MonoBehaviour // Контроллер входа и выхода игрока из укрытий
{
    [Header("State")] // Блок состояния
    public bool isHidden = false; // Спрятан ли игрок сейчас

    public bool IsBusy => isEntering || isExiting; // Занят ли игрок входом или выходом

    [Header("Player")] // Блок игрока
    public CharacterController characterController; // CharacterController игрока

    public Behaviour[] movementScriptsToDisable; // Скрипты управления, которые отключаются внутри шкафа

    [Header("Enter Movement")] // Блок входа в шкаф
    public bool walkIntoWardrobe = true; // Если true, игрок заходит внутрь плавно

    public float walkIntoSpeed = 1.4f; // Скорость входа

    public float rotateIntoSpeed = 8f; // Скорость поворота при входе

    public float walkStopDistance = 0.03f; // Дистанция остановки возле Hide Point

    public float maxWalkIntoTime = 2f; // Максимальное время входа

    public bool disableCharacterControllerWhileWalking = true; // Отключать CharacterController, чтобы не застревать

    [Header("Exit Movement")] // Блок выхода из шкафа
    public bool walkOutOfWardrobe = true; // Если true, игрок выходит из шкафа плавно

    public float walkOutSpeed = 1.4f; // Скорость выхода

    public float rotateOutSpeed = 8f; // Скорость поворота при выходе

    public float walkOutStopDistance = 0.03f; // Дистанция остановки возле Exit Point

    public float maxWalkOutTime = 2f; // Максимальное время выхода

    [Header("Monster")] // Блок монстра
    public MonsterVision monsterVision; // Зрение монстра

    public MonsterAttack monsterAttack; // Атака монстра

    public bool dieIfMonsterSeesHide = true; // Наказывать ли игрока, если монстр видел вход

    [Header("Exit Settings")] // Блок настроек выхода
    public float exitInputDelay = 0.5f; // Задержка после входа, прежде чем можно выйти

    private WardrobeHideHandle currentWardrobe; // Текущий шкаф, в котором спрятан игрок

    private float hideEnterTime = 0f; // Время входа в шкаф

    private bool isEntering = false; // Идет ли сейчас вход

    private bool isExiting = false; // Идет ли сейчас выход

    private void Reset() // Автонастройка при добавлении скрипта
    {
        characterController = GetComponent<CharacterController>(); // Ищем CharacterController на игроке
    }

    public void TryEnterWardrobe(WardrobeHideHandle wardrobe) // Попытка войти в шкаф по Q
    {
        if (isHidden) return; // Если игрок уже спрятан, выходим

        if (isEntering) return; // Если уже идет вход, выходим

        if (isExiting) return; // Если идет выход, выходим

        if (wardrobe == null) return; // Если шкаф не передан, выходим

        if (wardrobe.CanPlayerHide() == false) return; // Если условие шкафа не выполнено, выходим

        if (wardrobe.hidePoint == null) return; // Если точки внутри шкафа нет, выходим

        if (wardrobe.exitPoint == null) return; // Если точки выхода нет, выходим

        StartCoroutine(EnterWardrobeSequence(wardrobe)); // Запускаем вход в шкаф
    }

    public void TryExitHide() // Попытка выйти из шкафа по Q
    {
        if (isHidden == false) return; // Если игрок не спрятан, выходим

        if (isExiting) return; // Если выход уже идет, выходим

        if (Time.time < hideEnterTime + exitInputDelay) return; // Если задержка после входа еще не прошла, выходим

        StartCoroutine(ExitWardrobeSequence()); // Запускаем выход из шкафа
    }

    private IEnumerator EnterWardrobeSequence(WardrobeHideHandle wardrobe) // Последовательность входа в шкаф
    {
        isEntering = true; // Помечаем, что вход начался

        currentWardrobe = wardrobe; // Запоминаем текущий шкаф

        bool monsterSawHide = WouldMonsterSeeHideNow(); // Проверяем, видел ли монстр вход

        yield return StartCoroutine(wardrobe.OpenDoorsBeforeHide()); // Просим шкаф открыть двери перед входом

        SetMovement(false); // Отключаем управление игроком

        if (walkIntoWardrobe == true) yield return StartCoroutine(WalkToPoint(wardrobe.hidePoint, walkIntoSpeed, rotateIntoSpeed, walkStopDistance, maxWalkIntoTime)); // Плавно заводим игрока внутрь

        if (walkIntoWardrobe == false) TeleportPlayer(wardrobe.hidePoint.position, wardrobe.hidePoint.rotation); // Или телепортируем внутрь

        isHidden = true; // Помечаем игрока спрятанным

        hideEnterTime = Time.time; // Запоминаем время входа

        if (monsterSawHide == true) // Если монстр видел вход
        {
            PunishSeenHide(wardrobe.wardrobeDoor); // Запускаем наказание

            isEntering = false; // Снимаем флаг входа

            yield break; // Останавливаем последовательность
        }

        yield return StartCoroutine(wardrobe.CloseDoorsAfterHide()); // Просим шкаф закрыть двери после входа

        isEntering = false; // Вход завершен
    }

    private IEnumerator ExitWardrobeSequence() // Последовательность выхода из шкафа
    {
        isExiting = true; // Помечаем, что выход начался

        WardrobeHideHandle wardrobeToExit = currentWardrobe; // Сохраняем шкаф в локальную переменную

        if (wardrobeToExit != null) yield return StartCoroutine(wardrobeToExit.OpenDoorsBeforeExit()); // Просим шкаф открыть двери перед выходом

        isHidden = false; // Снимаем состояние пряток

        if (wardrobeToExit != null && wardrobeToExit.exitPoint != null) // Проверяем точку выхода
        {
            if (walkOutOfWardrobe == true) yield return StartCoroutine(WalkToPoint(wardrobeToExit.exitPoint, walkOutSpeed, rotateOutSpeed, walkOutStopDistance, maxWalkOutTime)); // Плавно выводим игрока наружу

            if (walkOutOfWardrobe == false) TeleportPlayer(wardrobeToExit.exitPoint.position, wardrobeToExit.exitPoint.rotation); // Или телепортируем наружу
        }

        SetMovement(true); // Возвращаем управление игроку

        if (wardrobeToExit != null) yield return StartCoroutine(wardrobeToExit.CloseDoorsAfterExit()); // Просим шкаф закрыть двери после выхода

        currentWardrobe = null; // Очищаем текущий шкаф

        isExiting = false; // Выход завершен
    }

    private IEnumerator WalkToPoint(Transform targetPoint, float moveSpeed, float rotateSpeed, float stopDistance, float maxMoveTime) // Плавно ведет игрока к точке
    {
        if (targetPoint == null) yield break; // Если точки нет, выходим

        bool controllerWasEnabled = characterController != null && characterController.enabled == true; // Запоминаем состояние CharacterController

        if (disableCharacterControllerWhileWalking == true && characterController != null) characterController.enabled = false; // Отключаем CharacterController, чтобы не застревать

        float timer = 0f; // Таймер движения

        while (Vector3.Distance(transform.position, targetPoint.position) > stopDistance && timer < maxMoveTime) // Пока не дошли или не вышло время
        {
            timer += Time.deltaTime; // Увеличиваем таймер

            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime); // Двигаем игрока к точке

            transform.rotation = Quaternion.Slerp(transform.rotation, targetPoint.rotation, rotateSpeed * Time.deltaTime); // Поворачиваем игрока к повороту точки

            yield return null; // Ждем следующий кадр
        }

        transform.SetPositionAndRotation(targetPoint.position, targetPoint.rotation); // Точно ставим игрока в конечную точку

        if (disableCharacterControllerWhileWalking == true && characterController != null && controllerWasEnabled == true) characterController.enabled = true; // Возвращаем CharacterController
    }

    public bool WouldMonsterSeeHideNow() // Проверяет, видит ли монстр вход
    {
        if (dieIfMonsterSeesHide == false) return false; // Если наказание выключено, возвращаем false

        if (monsterVision == null) return false; // Если зрение монстра не назначено, возвращаем false

        return monsterVision.CanSeePlayerIgnoringHide(); // Проверяем видимость игрока
    }

    public void PunishSeenHide(UniversalDoor wardrobeDoor) // Наказание за вход на глазах монстра
    {
        if (dieIfMonsterSeesHide == false) return; // Если наказание выключено, выходим

        if (monsterAttack == null) return; // Если атака не назначена, выходим

        monsterAttack.StartHideCatchAttack(wardrobeDoor); // Запускаем атаку монстра
    }

    private void TeleportPlayer(Vector3 targetPosition, Quaternion targetRotation) // Безопасный телепорт игрока
    {
        if (characterController != null) characterController.enabled = false; // Отключаем CharacterController перед телепортом

        transform.SetPositionAndRotation(targetPosition, targetRotation); // Переносим и поворачиваем игрока

        if (characterController != null) characterController.enabled = true; // Возвращаем CharacterController
    }

    private void SetMovement(bool enabledState) // Включает или выключает управление
    {
        if (movementScriptsToDisable == null) return; // Если списка нет, выходим

        for (int i = 0; i < movementScriptsToDisable.Length; i++) // Проходим по скриптам
        {
            if (movementScriptsToDisable[i] == null) continue; // Если элемент пустой, пропускаем

            if (movementScriptsToDisable[i] is PlayerInteractor) continue; // PlayerInteractor не выключаем, чтобы Q работала

            movementScriptsToDisable[i].enabled = enabledState; // Включаем или выключаем скрипт
        }
    }
}