using UnityEngine; // Подключаем Unity-классы
using System.Collections; // Подключаем корутины
using StarterAssets; // Подключаем FirstPersonController

public class PlayerHideController : MonoBehaviour // Контроллер входа и выхода игрока из укрытий
{
    [Header("State")] // Блок состояния
    public bool isHidden = false; // Спрятан ли игрок сейчас

    public bool IsBusy => isEntering || isExiting; // Занят ли игрок входом или выходом

    [Header("Player")] // Блок игрока
    public CharacterController characterController; // CharacterController игрока

    public FirstPersonController firstPersonController; // Контроллер движения и камеры игрока

    public StarterAssetsInputs starterAssetsInputs; // Источник движения мыши из Starter Assets

    public Transform cameraTarget; // CinemachineCameraTarget или другой объект, который вращает FPS-камеру

    public Behaviour[] movementScriptsToDisable; // Скрипты управления, которые отключаются внутри шкафа

    [Header("Hidden Peek Camera")] // Настройки камеры, пока игрок сидит в шкафу
    public bool enableHiddenPeekCamera = true; // Включать выдвижение камеры и свободный ограниченный обзор

    [Min(0f)]
    public float cameraForwardOffset = 0.28f; // Камера выдвигается вперёд на 28 см: прежние 18 см плюс ещё 10 см

    [Min(0.01f)]
    public float cameraMoveSpeed = 5f; // Скорость плавного выдвижения и возврата камеры

    [Min(0.01f)]
    public float hiddenLookSensitivity = 1f; // Чувствительность мыши внутри шкафа

    [Range(0f, 89f)]
    public float hiddenHorizontalLookLimit = 35f; // Максимальный поворот влево и вправо от центра

    [Min(0f)]
    public float cameraReturnBeforeExitTime = 0.2f; // Сколько ждать возврата камеры перед открытием дверей

    [Header("Enter Movement")] // Блок входа в шкаф
    public bool walkIntoWardrobe = true; // Если true, игрок заходит внутрь плавно

    public float walkIntoSpeed = 1.4f; // Скорость входа

    public float rotateIntoSpeed = 8f; // Скорость поворота при входе

    public float walkStopDistance = 0.03f; // Дистанция остановки возле Hide Point

    public float maxWalkIntoTime = 2f; // Максимальное время входа

    public bool disableCharacterControllerWhileWalking = true; // Отключать CharacterController, чтобы не застревать

    [Header("Turn Toward Wardrobe Doors")] // Блок автоматического разворота внутри шкафа
    public bool turnTowardDoorsAfterEntering = true; // Разворачивать ли игрока лицом к дверям после входа

    public float turnTowardDoorsSpeed = 360f; // Скорость разворота в градусах в секунду

    public float maxTurnTowardDoorsTime = 1f; // Максимальное время разворота

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

    private bool hiddenPeekViewActive = false; // Разрешён ли сейчас обзор из щели

    private Vector3 cameraTargetStartLocalPosition; // Исходная локальная позиция Camera Target

    private Quaternion cameraTargetStartLocalRotation; // Исходный локальный поворот Camera Target

    private float hiddenBaseYaw = 0f; // Центральный горизонтальный угол при входе в шкаф

    private float hiddenYawOffset = 0f; // Текущий поворот влево или вправо от центра

    private void Reset() // Автонастройка при добавлении скрипта
    {
        characterController = GetComponent<CharacterController>(); // Ищем CharacterController на игроке

        firstPersonController = GetComponent<FirstPersonController>(); // Ищем FirstPersonController на игроке

        starterAssetsInputs = GetComponent<StarterAssetsInputs>(); // Ищем StarterAssetsInputs на игроке

        if (firstPersonController != null) cameraTarget = firstPersonController.CinemachineCameraTarget.transform; // Берём стандартную цель камеры Starter Assets
    }

    private void Awake() // Автоматически находит обязательные ссылки перед началом игры
    {
        if (characterController == null) characterController = GetComponent<CharacterController>(); // Если ссылка не назначена, ищем CharacterController

        if (firstPersonController == null) firstPersonController = GetComponent<FirstPersonController>(); // Если ссылка не назначена, ищем FirstPersonController

        if (firstPersonController == null) firstPersonController = GetComponentInParent<FirstPersonController>(); // Дополнительно ищем FirstPersonController выше по иерархии

        if (starterAssetsInputs == null) starterAssetsInputs = GetComponent<StarterAssetsInputs>(); // Ищем ввод Starter Assets

        if (starterAssetsInputs == null) starterAssetsInputs = GetComponentInParent<StarterAssetsInputs>(); // Дополнительно ищем ввод выше

        if (cameraTarget == null && firstPersonController != null) cameraTarget = firstPersonController.CinemachineCameraTarget.transform; // Автоматически берём стандартную цель камеры

        if (cameraTarget != null) // Если Camera Target найден
        {
            cameraTargetStartLocalPosition = cameraTarget.localPosition; // Запоминаем исходную локальную позицию

            cameraTargetStartLocalRotation = cameraTarget.localRotation; // Запоминаем исходный локальный поворот
        }
    }

    private void LateUpdate() // Обновляет обзор и положение камеры после обычной логики игрока
    {
        UpdateHiddenPeekCamera(); // Двигаем и вращаем камеру только в режиме пряток
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

        SetFirstPersonControl(false); // Запрещаем игроку двигаться и сбивать автоматический поворот мышью

        if (walkIntoWardrobe == true) yield return StartCoroutine(WalkToPoint(wardrobe.hidePoint, walkIntoSpeed, rotateIntoSpeed, walkStopDistance, maxWalkIntoTime)); // Плавно заводим игрока внутрь

        if (walkIntoWardrobe == false) TeleportPlayer(wardrobe.hidePoint.position, wardrobe.hidePoint.rotation); // Или телепортируем внутрь

        if (turnTowardDoorsAfterEntering == true) yield return StartCoroutine(TurnTowardPoint(wardrobe.exitPoint, turnTowardDoorsSpeed, maxTurnTowardDoorsTime)); // Автоматически разворачиваем игрока лицом к дверям

        SyncFirstPersonView(); // Запоминаем новый поворот в контроллере камеры, чтобы он не вернул старый угол

        isHidden = true; // Помечаем игрока спрятанным

        hideEnterTime = Time.time; // Запоминаем время входа

        if (monsterSawHide == true) // Если монстр видел вход
        {
            PunishSeenHide(wardrobe.wardrobeDoor); // Запускаем наказание

            isEntering = false; // Снимаем флаг входа

            yield break; // Останавливаем последовательность
        }

        yield return StartCoroutine(wardrobe.CloseDoorsAfterHide()); // Просим шкаф закрыть двери или оставить их в Peek Position

        BeginHiddenPeekView(); // Выдвигаем камеру вперёд и разрешаем ограниченный обзор мышью

        isEntering = false; // Вход завершен
    }

    private IEnumerator ExitWardrobeSequence() // Последовательность выхода из шкафа
    {
        isExiting = true; // Помечаем, что выход начался

        WardrobeHideHandle wardrobeToExit = currentWardrobe; // Сохраняем шкаф в локальную переменную

        EndHiddenPeekView(); // Запрещаем обзор из щели и начинаем возвращать камеру назад

        if (cameraReturnBeforeExitTime > 0f) yield return new WaitForSeconds(cameraReturnBeforeExitTime); // Даём камере время вернуться перед открытием дверей

        if (wardrobeToExit != null) yield return StartCoroutine(wardrobeToExit.OpenDoorsBeforeExit()); // Просим шкаф полностью открыть двери перед выходом

        isHidden = false; // Снимаем состояние пряток

        if (wardrobeToExit != null && wardrobeToExit.exitPoint != null) // Проверяем точку выхода
        {
            if (walkOutOfWardrobe == true) yield return StartCoroutine(WalkToPoint(wardrobeToExit.exitPoint, walkOutSpeed, rotateOutSpeed, walkOutStopDistance, maxWalkOutTime)); // Плавно выводим игрока наружу

            if (walkOutOfWardrobe == false) TeleportPlayer(wardrobeToExit.exitPoint.position, wardrobeToExit.exitPoint.rotation); // Или телепортируем наружу
        }

        SyncFirstPersonView(); // Перед возвратом управления запоминаем итоговый поворот после выхода

        SetMovement(true); // Возвращаем управление игроку

        SetFirstPersonControl(true); // Снова разрешаем движение и обзор

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

    private IEnumerator TurnTowardPoint(Transform targetPoint, float turnSpeed, float maxTurnTime) // Плавно разворачивает игрока лицом к указанной точке
    {
        if (targetPoint == null) yield break; // Если точки нет, разворот выполнить нельзя

        Vector3 lookDirection = targetPoint.position - transform.position; // Получаем направление от игрока к точке перед дверями

        lookDirection.y = 0f; // Убираем вертикальный наклон, чтобы игрок не заваливался вверх или вниз

        if (lookDirection.sqrMagnitude < 0.0001f) yield break; // Если точки почти совпали, направление определить нельзя

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up); // Создаем поворот лицом к дверям

        float timer = 0f; // Таймер разворота

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f && timer < maxTurnTime) // Поворачиваемся до нужного угла или окончания времени
        {
            timer += Time.deltaTime; // Увеличиваем таймер

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime); // Плавно поворачиваем игрока

            yield return null; // Ждем следующий кадр
        }

        transform.rotation = targetRotation; // В конце точно ставим нужный поворот
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

    private void BeginHiddenPeekView() // Запускает обзор из щели после завершения входа
    {
        if (enableHiddenPeekCamera == false) return; // Если функция выключена, ничего не делаем

        if (cameraTarget == null) return; // Без Camera Target камера не сможет двигаться

        hiddenBaseYaw = transform.eulerAngles.y; // Запоминаем центральное направление игрока

        hiddenYawOffset = 0f; // Сбрасываем горизонтальное отклонение

        hiddenPeekViewActive = true; // Разрешаем обработку мыши и выдвижение камеры
    }

    private void EndHiddenPeekView() // Завершает обзор из щели перед выходом
    {
        hiddenPeekViewActive = false; // Запрещаем дальнейший ввод мыши в режиме шкафа

        hiddenYawOffset = 0f; // Сбрасываем горизонтальный угол

        transform.rotation = Quaternion.Euler(0f, hiddenBaseYaw, 0f); // Возвращаем игрока в центральное направление
    }

    private void UpdateHiddenPeekCamera() // Двигает камеру и позволяет ограниченно смотреть мышью
    {
        if (cameraTarget == null) return; // Без Camera Target обновлять нечего

        Vector3 targetLocalPosition = cameraTargetStartLocalPosition; // По умолчанию камера возвращается в исходную позицию

        if (hiddenPeekViewActive == true) // Если игрок сидит в шкафу и двери находятся в Peek Position
        {
            targetLocalPosition += Vector3.forward * cameraForwardOffset; // Выдвигаем камеру немного вперёд

            if (starterAssetsInputs != null) // Если ввод Starter Assets найден
            {
                Vector2 lookInput = starterAssetsInputs.look; // Получаем движение мыши или стика

                hiddenYawOffset += lookInput.x * hiddenLookSensitivity; // Используем только горизонтальное движение мыши

                hiddenYawOffset = Mathf.Clamp(
                    hiddenYawOffset,
                    -hiddenHorizontalLookLimit,
                    hiddenHorizontalLookLimit
                ); // Ограничиваем взгляд влево и вправо стенками шкафа
            }

            transform.rotation = Quaternion.Euler(
                0f,
                hiddenBaseYaw + hiddenYawOffset,
                0f
            ); // Поворачиваем игрока только на ограниченный горизонтальный угол

            cameraTarget.localRotation = Quaternion.Slerp(
                cameraTarget.localRotation,
                cameraTargetStartLocalRotation,
                Time.deltaTime * cameraMoveSpeed
            ); // Удерживаем исходный вертикальный угол: вверх и вниз смотреть нельзя
        }
        else // Если Peek View выключен
        {
            cameraTarget.localRotation = Quaternion.Slerp(
                cameraTarget.localRotation,
                cameraTargetStartLocalRotation,
                Time.deltaTime * cameraMoveSpeed
            ); // Плавно возвращаем вертикальный поворот камеры
        }

        cameraTarget.localPosition = Vector3.Lerp(
            cameraTarget.localPosition,
            targetLocalPosition,
            Time.deltaTime * cameraMoveSpeed
        ); // Плавно выдвигаем или возвращаем Camera Target
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

            if (movementScriptsToDisable[i] == firstPersonController) continue; // FirstPersonController не выключаем целиком, потому что его состояние контролируем отдельно

            movementScriptsToDisable[i].enabled = enabledState; // Включаем или выключаем скрипт
        }
    }

    private void SetFirstPersonControl(bool enabledState) // Включает или выключает ввод движения и обзора
    {
        if (firstPersonController == null) return; // Если контроллер игрока не найден, выходим

        firstPersonController.SetControlEnabled(enabledState); // Передаем новое состояние контроллеру игрока
    }

    private void SyncFirstPersonView() // Синхронизирует внутренние углы камеры с фактическим поворотом игрока
    {
        if (firstPersonController == null) return; // Если контроллер игрока не найден, выходим

        firstPersonController.SetViewRotation(transform.eulerAngles.y, 0f); // Запоминаем текущий горизонтальный угол и выравниваем взгляд
    }
}