using UnityEngine; // Подключаем основные классы Unity.

public class PlayerAnimatorSync : MonoBehaviour // Синхронизирует движение и присед игрока с Animator.
{
    [Header("Основные ссылки")] // Заголовок ссылок в Inspector.

    [SerializeField]
    private Animator playerAnimator; // Animator визуальной модели PlayerModel.

    [SerializeField]
    private CharacterController characterController; // CharacterController объекта PlayerCapsule.

    [Header("Определение движения")] // Настройки определения движения.

    [SerializeField]
    [Min(0.001f)]
    private float movingThreshold = 0.05f; // Минимальная горизонтальная скорость для включения движения.

    [Header("Определение приседа")] // Настройки автоматического определения приседа.

    [SerializeField]
    [Min(0.001f)]
    private float crouchHeightDifference = 0.05f; // Насколько должна уменьшиться высота капсулы, чтобы считать игрока присевшим.

    private float standingControllerHeight; // Запоминаем высоту CharacterController в обычной стойке.

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving"); // Получаем идентификатор параметра IsMoving.

    private static readonly int IsCrouchingHash =
        Animator.StringToHash("IsCrouching"); // Получаем идентификатор параметра IsCrouching.

    private void Awake() // Вызывается один раз при запуске объекта.
    {
        if (playerAnimator == null) // Если Animator не назначен вручную.
        {
            playerAnimator = GetComponent<Animator>(); // Ищем Animator на PlayerModel.
        }

        if (characterController == null) // Если CharacterController не назначен вручную.
        {
            characterController =
                GetComponentInParent<CharacterController>(); // Ищем его на родительском PlayerCapsule.
        }

        if (playerAnimator == null) // Если Animator найти не удалось.
        {
            Debug.LogError(
                "PlayerAnimatorSync: не найден Animator объекта PlayerModel.",
                this); // Выводим понятную ошибку в Console.
        }

        if (characterController == null) // Если CharacterController найти не удалось.
        {
            Debug.LogError(
                "PlayerAnimatorSync: не найден CharacterController объекта PlayerCapsule.",
                this); // Выводим понятную ошибку в Console.

            return; // Не продолжаем без CharacterController.
        }

        standingControllerHeight =
            characterController.height; // Запоминаем начальную высоту капсулы как высоту стоящего игрока.
    }

    private void Update() // Выполняется каждый кадр.
    {
        if (playerAnimator == null || characterController == null) // Проверяем обязательные ссылки.
        {
            return; // Не выполняем логику без нужных компонентов.
        }

        if (!characterController.enabled) // Если CharacterController временно выключен, например во время пряток.
        {
            playerAnimator.SetBool(IsMovingHash, false); // Возвращаем обычный Idle.
            playerAnimator.SetBool(IsCrouchingHash, false); // Отключаем анимацию приседа.
            return; // Завершаем текущий кадр.
        }

        Vector3 horizontalVelocity =
            characterController.velocity; // Получаем фактическую скорость CharacterController.

        horizontalVelocity.y = 0f; // Убираем прыжок, падение и гравитацию.

        bool isMoving =
            horizontalVelocity.sqrMagnitude >
            movingThreshold * movingThreshold; // Проверяем горизонтальное движение.

        bool isCrouching =
            characterController.height <
            standingControllerHeight - crouchHeightDifference; // Проверяем, уменьшилась ли высота капсулы.

        playerAnimator.SetBool(
            IsMovingHash,
            isMoving); // Передаём движение в Animator.

        playerAnimator.SetBool(
            IsCrouchingHash,
            isCrouching); // Передаём реальное состояние приседа в Animator.
    }

    private void OnDisable() // Вызывается при отключении объекта или компонента.
    {
        if (playerAnimator == null) // Проверяем наличие Animator.
        {
            return; // Не продолжаем без Animator.
        }

        playerAnimator.SetBool(IsMovingHash, false); // Возвращаем параметр движения в false.
        playerAnimator.SetBool(IsCrouchingHash, false); // Возвращаем параметр приседа в false.
    }
}