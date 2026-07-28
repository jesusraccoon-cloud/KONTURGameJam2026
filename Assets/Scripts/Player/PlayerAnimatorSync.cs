using UnityEngine; // Подключаем основные классы Unity.

public class PlayerAnimatorSync : MonoBehaviour // Синхронизирует движение игрока с Animator.
{
    [Header("Основные ссылки")] // Заголовок раздела ссылок в Inspector.

    [SerializeField]
    private Animator playerAnimator; // Animator визуальной модели PlayerModel.

    [SerializeField]
    private CharacterController characterController; // CharacterController объекта PlayerCapsule.

    [Header("Настройка движения")] // Заголовок настроек движения.

    [SerializeField]
    [Min(0.001f)]
    private float movingThreshold = 0.05f; // Минимальная скорость для включения Walk.

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving"); // Получаем идентификатор параметра IsMoving.

    private void Awake() // Вызывается при запуске объекта.
    {
        if (playerAnimator == null) // Если Animator вручную не назначен.
        {
            playerAnimator = GetComponent<Animator>(); // Ищем Animator на PlayerModel.
        }

        if (characterController == null) // Если CharacterController вручную не назначен.
        {
            characterController =
                GetComponentInParent<CharacterController>(); // Ищем его на PlayerCapsule.
        }

        if (playerAnimator == null) // Если Animator найти не удалось.
        {
            Debug.LogError(
                "PlayerAnimatorSync: не найден Animator на PlayerModel.",
                this); // Выводим понятную ошибку.
        }

        if (characterController == null) // Если CharacterController найти не удалось.
        {
            Debug.LogError(
                "PlayerAnimatorSync: не найден CharacterController на PlayerCapsule.",
                this); // Выводим понятную ошибку.
        }
    }

    private void Update() // Выполняется каждый кадр.
    {
        if (playerAnimator == null || characterController == null) // Проверяем ссылки.
        {
            return; // Не продолжаем без нужных компонентов.
        }

        if (!characterController.enabled) // Если контроллер отключён, например при прятках.
        {
            playerAnimator.SetBool(IsMovingHash, false); // Включаем Idle.
            return; // Завершаем текущий кадр.
        }

        Vector3 horizontalVelocity =
            characterController.velocity; // Получаем фактическую скорость игрока.

        horizontalVelocity.y = 0f; // Убираем падение, прыжок и гравитацию.

        bool isMoving =
            horizontalVelocity.sqrMagnitude >
            movingThreshold * movingThreshold; // Проверяем горизонтальное движение.

        playerAnimator.SetBool(
            IsMovingHash,
            isMoving); // Передаём результат в параметр Animator.
    }

    private void OnDisable() // Вызывается при отключении объекта или компонента.
    {
        if (playerAnimator != null) // Проверяем наличие Animator.
        {
            playerAnimator.SetBool(IsMovingHash, false); // Возвращаем Idle.
        }
    }
}