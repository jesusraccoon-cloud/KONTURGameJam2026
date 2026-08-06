using UnityEngine; // Подключаем основные классы Unity.
using StarterAssets; // Подключаем FirstPersonController из Starter Assets.

public class PlayerCrouch : MonoBehaviour // Управляет приседом, камерой и скоростью игрока.
{
    [Header("References")] // Основные ссылки.

    public CharacterController controller; // CharacterController игрока.

    public Transform cameraRoot; // Объект PlayerCameraRoot.

    public FirstPersonController firstPersonController; // Основной контроллер движения игрока.

    [Header("Crouch Settings")] // Настройки приседа.

    public KeyCode crouchKey = KeyCode.LeftControl; // Кнопка удержания приседа.

    public float crouchHeight = 1.0f; // Высота CharacterController во время приседа.

    public float crouchCameraOffset = 0.45f; // Насколько камера опускается вниз по оси Y.

    public float crouchCameraZOffset = 0.15f; // Насколько камера уходит вперёд по оси Z.

    public float crouchSpeed = 10f; // Скорость плавного приседания и вставания.

    [Header("Movement While Crouching")] // Настройки движения во время приседа.

    public float crouchMoveSpeed = 1.5f; // Скорость ходьбы игрока в приседе.

    private float standingHeight; // Высота CharacterController в положении стоя.

    private Vector3 standingCenter; // Центр CharacterController в положении стоя.

    private Vector3 crouchingCenter; // Центр CharacterController в приседе.

    private Vector3 standingCameraLocalPos; // Исходная позиция камеры стоя.

    private Vector3 crouchingCameraLocalPos; // Целевая позиция камеры в приседе.

    private float standingMoveSpeed; // Обычная скорость ходьбы.

    private float standingSprintSpeed; // Обычная скорость бега.

    public bool IsCrouching { get; private set; } // Текущее состояние приседа для других скриптов.

    private void Start() // Вызывается один раз при запуске сцены.
    {
        if (controller == null) // Если CharacterController не назначен.
        {
            controller = GetComponent<CharacterController>(); // Ищем его на этом объекте.
        }

        if (firstPersonController == null) // Если FirstPersonController не назначен.
        {
            firstPersonController = GetComponent<FirstPersonController>(); // Ищем его на этом объекте.
        }

        if (controller == null) // Если CharacterController не найден.
        {
            Debug.LogError(
                "PlayerCrouch: не найден CharacterController.",
                this); // Показываем ошибку в Console.

            enabled = false; // Отключаем скрипт.
            return; // Прерываем запуск.
        }

        if (cameraRoot == null) // Если PlayerCameraRoot не назначен.
        {
            Debug.LogError(
                "PlayerCrouch: не назначен Camera Root.",
                this); // Показываем ошибку.

            enabled = false; // Отключаем скрипт.
            return; // Прерываем запуск.
        }

        if (firstPersonController == null) // Если FirstPersonController не найден.
        {
            Debug.LogError(
                "PlayerCrouch: не найден FirstPersonController.",
                this); // Показываем ошибку.

            enabled = false; // Отключаем скрипт.
            return; // Прерываем запуск.
        }

        standingHeight = controller.height; // Запоминаем обычную высоту игрока.

        standingCenter = controller.center; // Запоминаем обычный центр капсулы.

        crouchingCenter = new Vector3(
            standingCenter.x, // Центр по X не меняем.
            crouchHeight * 0.5f, // Опускаем центр согласно высоте приседа.
            standingCenter.z // Центр по Z не меняем.
        );

        standingCameraLocalPos =
            cameraRoot.localPosition; // Запоминаем исходную позицию камеры.

        crouchingCameraLocalPos = standingCameraLocalPos + new Vector3(
            0f, // По X камеру не перемещаем.
            -crouchCameraOffset, // По Y опускаем камеру.
            crouchCameraZOffset // По Z перемещаем камеру вперёд.
        );

        standingMoveSpeed =
            firstPersonController.MoveSpeed; // Запоминаем обычную скорость ходьбы.

        standingSprintSpeed =
            firstPersonController.SprintSpeed; // Запоминаем обычную скорость бега.
    }

    private void Update() // Выполняется каждый кадр.
    {
        IsCrouching =
            Input.GetKey(crouchKey); // Пока Ctrl зажат, игрок находится в приседе.

        float targetHeight =
            IsCrouching
                ? crouchHeight
                : standingHeight; // Выбираем высоту капсулы.

        Vector3 targetCenter =
            IsCrouching
                ? crouchingCenter
                : standingCenter; // Выбираем центр капсулы.

        Vector3 targetCameraPos =
            IsCrouching
                ? crouchingCameraLocalPos
                : standingCameraLocalPos; // Выбираем положение камеры.

        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            Time.deltaTime * crouchSpeed
        ); // Плавно изменяем высоту капсулы.

        controller.center = Vector3.Lerp(
            controller.center,
            targetCenter,
            Time.deltaTime * crouchSpeed
        ); // Плавно изменяем центр капсулы.

        cameraRoot.localPosition = Vector3.Lerp(
            cameraRoot.localPosition,
            targetCameraPos,
            Time.deltaTime * crouchSpeed
        ); // Плавно опускаем или возвращаем камеру.

        if (IsCrouching) // Если игрок находится в приседе.
        {
            firstPersonController.MoveSpeed =
                crouchMoveSpeed; // Устанавливаем уменьшенную скорость ходьбы.

            firstPersonController.SprintSpeed =
                crouchMoveSpeed; // Не разрешаем ускоряться бегом во время приседа.
        }
        else // Если игрок встал.
        {
            firstPersonController.MoveSpeed =
                standingMoveSpeed; // Возвращаем обычную скорость ходьбы.

            firstPersonController.SprintSpeed =
                standingSprintSpeed; // Возвращаем обычную скорость бега.
        }
    }

    private void OnDisable() // Вызывается при отключении компонента.
    {
        IsCrouching = false; // Сбрасываем состояние приседа.

        if (firstPersonController == null) // Проверяем наличие контроллера.
        {
            return; // Не продолжаем без контроллера.
        }

        firstPersonController.MoveSpeed =
            standingMoveSpeed; // Возвращаем обычную скорость ходьбы.

        firstPersonController.SprintSpeed =
            standingSprintSpeed; // Возвращаем обычную скорость бега.
    }
}