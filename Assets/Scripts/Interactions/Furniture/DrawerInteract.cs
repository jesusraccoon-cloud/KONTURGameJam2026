using UnityEngine; // Подключаем базовые функции Unity.

public class DrawerInteract : MonoBehaviour, IInteractable // Скрипт для выдвижения ящика.
{
    public enum SlideDirection // Список направлений движения ящика.
    {
        Forward, // Вперёд по локальной Z.
        Back, // Назад по локальной Z.
        Right, // Вправо по локальной X.
        Left, // Влево по локальной X.
        Up, // Вверх по локальной Y.
        Down // Вниз по локальной Y.
    }

    [Header("Drawer Movement Settings")] // Блок движения.
    public float slideDistance = 0.4f; // Дистанция выдвижения ящика.

    public float moveSpeed = 3f; // Скорость движения ящика.

    public SlideDirection slideDirection = SlideDirection.Back; // Направление движения ящика.

    [Header("Noise")] // Блок шума.
    public NoiseEmitter noiseEmitter; // Источник шума ящика.

    [Range(1, 10)]
    public int openNoisePower = 3; // Шум открытия ящика.

    [Range(1, 10)]
    public int closeNoisePower = 2; // Шум закрытия ящика.

    private Vector3 closedLocalPosition; // Закрытая локальная позиция.

    private Vector3 openLocalPosition; // Открытая локальная позиция.

    private Vector3 targetLocalPosition; // Целевая локальная позиция.

    private bool isOpen = false; // Открыт ли ящик.

    private void Start() // Запуск сцены.
    {
        closedLocalPosition = transform.localPosition; // Запоминаем закрытую позицию.

        openLocalPosition =
            closedLocalPosition +
            GetSlideVector() * slideDistance; // Считаем открытую позицию.

        targetLocalPosition = closedLocalPosition; // В начале цель — закрытая позиция.

        if (noiseEmitter == null) // Если NoiseEmitter не назначен вручную.
        {
            noiseEmitter = GetComponent<NoiseEmitter>(); // Пробуем найти NoiseEmitter на этом же объекте.
        }
    }

    private void Update() // Каждый кадр.
    {
        transform.localPosition = Vector3.Lerp( // Плавно двигаем ящик.
            transform.localPosition, // От текущей позиции.
            targetLocalPosition, // К целевой позиции.
            Time.deltaTime * moveSpeed // С учётом скорости и времени.
        );
    }

    public void Interact() // Вызывается PlayerInteractor при нажатии E.
    {
        ToggleDrawer(); // Переключаем ящик.
    }

    private void ToggleDrawer() // Метод открытия и закрытия.
    {
        isOpen = !isOpen; // Меняем состояние на противоположное.

        targetLocalPosition =
            isOpen
                ? openLocalPosition
                : closedLocalPosition; // Выбираем открытую или закрытую позицию.

        EmitDrawerNoise(); // Создаём шум ящика.
    }

    private void EmitDrawerNoise() // Метод шума ящика.
    {
        if (noiseEmitter == null) // Если NoiseEmitter не назначен.
        {
            return; // Прекращаем выполнение.
        }

        int noisePower =
            isOpen
                ? openNoisePower
                : closeNoisePower; // Выбираем силу шума открытия или закрытия.

        noiseEmitter.EmitNoise(noisePower); // Отправляем шум в систему.
    }

    private Vector3 GetSlideVector() // Получить направление движения.
    {
        switch (slideDirection) // Проверяем выбранное направление.
        {
            case SlideDirection.Forward: // Если выбрано движение вперёд.
                return Vector3.forward; // Возвращаем локальное направление вперёд.

            case SlideDirection.Back: // Если выбрано движение назад.
                return Vector3.back; // Возвращаем локальное направление назад.

            case SlideDirection.Right: // Если выбрано движение вправо.
                return Vector3.right; // Возвращаем локальное направление вправо.

            case SlideDirection.Left: // Если выбрано движение влево.
                return Vector3.left; // Возвращаем локальное направление влево.

            case SlideDirection.Up: // Если выбрано движение вверх.
                return Vector3.up; // Возвращаем локальное направление вверх.

            case SlideDirection.Down: // Если выбрано движение вниз.
                return Vector3.down; // Возвращаем локальное направление вниз.

            default: // Если направление по какой-то причине не определено.
                return Vector3.back; // По умолчанию двигаем назад.
        }
    }
}