using UnityEngine; // Подключаем Unity-классы

public class MovementCameraShake : MonoBehaviour // Плавное покачивание камеры при движении
{
    [Header("References")] // Ссылки
    public Transform cameraTarget; // Объект, который будет покачиваться
    public CharacterController characterController; // CharacterController игрока

    [Header("Movement Check")] // Проверка движения
    public bool shakeOnlyWhenGrounded = true; // Качать только на земле
    public float minMoveSpeed = 0.15f; // Минимальная скорость для включения покачивания

    [Header("Side Sway")] // Боковое покачивание
    public float sidePositionAmount = 0.045f; // Насколько камера смещается вправо-влево
    public float sideRotationAmount = 1.2f; // Насколько камера наклоняется вправо-влево
    public float swaySpeed = 4.5f; // Скорость покачивания

    [Header("Vertical Bob")] // Вертикальное покачивание
    public bool useVerticalBob = false; // Использовать ли движение вверх-вниз
    public float verticalAmount = 0.01f; // Сила движения вверх-вниз

    [Header("Smoothing")] // Сглаживание
    public float fadeInSpeed = 5f; // Скорость появления покачивания
    public float fadeOutSpeed = 8f; // Скорость исчезновения покачивания
    public float returnSpeed = 10f; // Скорость возврата камеры в исходное положение

    [Header("Debug")] // Отладка
    public bool forceShakeForTest = false; // Принудительно включить покачивание для проверки

    private Vector3 startLocalPosition; // Исходная локальная позиция камеры
    private Quaternion startLocalRotation; // Исходный локальный поворот камеры
    private float swayTimer = 0f; // Таймер покачивания
    private float swayWeight = 0f; // Текущая сила покачивания от 0 до 1

    private void Reset() // Вызывается при добавлении скрипта
    {
        cameraTarget = transform; // По умолчанию качаем объект со скриптом
        characterController = GetComponentInParent<CharacterController>(); // Ищем CharacterController выше
    }

    private void Start() // Запускается при старте сцены
    {
        if (cameraTarget == null) cameraTarget = transform; // Если цель не назначена, берем этот объект
        startLocalPosition = cameraTarget.localPosition; // Запоминаем стартовую позицию
        startLocalRotation = cameraTarget.localRotation; // Запоминаем стартовый поворот
    }

    private void LateUpdate() // Обновляется после движения игрока
    {
        if (cameraTarget == null) return; // Если цели нет, выходим

        bool shouldSway = ShouldSway(); // Проверяем, нужно ли качать камеру

        float targetWeight = shouldSway ? 1f : 0f; // Целевая сила покачивания

        float weightSpeed = shouldSway ? fadeInSpeed : fadeOutSpeed; // Скорость изменения силы

        swayWeight = Mathf.MoveTowards(swayWeight, targetWeight, weightSpeed * Time.deltaTime); // Плавно меняем силу

        if (swayWeight > 0f) swayTimer += Time.deltaTime * swaySpeed; // Двигаем таймер только когда есть покачивание

        Vector3 positionOffset = GetPositionOffset(); // Считаем смещение позиции

        Quaternion rotationOffset = GetRotationOffset(); // Считаем наклон камеры

        cameraTarget.localPosition = Vector3.Lerp(cameraTarget.localPosition, startLocalPosition + positionOffset, returnSpeed * Time.deltaTime); // Плавно применяем позицию

        cameraTarget.localRotation = Quaternion.Slerp(cameraTarget.localRotation, startLocalRotation * rotationOffset, returnSpeed * Time.deltaTime); // Плавно применяем поворот
    }

    private bool ShouldSway() // Проверяет, должен ли работать эффект
    {
        if (forceShakeForTest) return true; // Если включен тест, качаем всегда

        if (characterController == null) return false; // Если CharacterController нет, не качаем

        Vector3 velocity = characterController.velocity; // Берем скорость игрока

        velocity.y = 0f; // Убираем вертикальную скорость

        if (velocity.magnitude < minMoveSpeed) return false; // Если игрок почти не движется, не качаем

        if (shakeOnlyWhenGrounded && characterController.isGrounded == false) return false; // Если не на земле, не качаем

        return true; // Все условия выполнены
    }

    private Vector3 GetPositionOffset() // Считает смещение позиции
    {
        float side = Mathf.Sin(swayTimer) * sidePositionAmount * swayWeight; // Плавное смещение вправо-влево

        float vertical = 0f; // По умолчанию вертикального движения нет

        if (useVerticalBob) vertical = Mathf.Abs(Mathf.Cos(swayTimer)) * verticalAmount * swayWeight; // Если включено, добавляем легкий вертикальный bob

        return new Vector3(side, vertical, 0f); // Возвращаем итоговое смещение
    }

    private Quaternion GetRotationOffset() // Считает наклон камеры
    {
        float zTilt = -Mathf.Sin(swayTimer) * sideRotationAmount * swayWeight; // Наклон камеры вправо-влево

        return Quaternion.Euler(0f, 0f, zTilt); // Возвращаем поворот только по Z
    }
}