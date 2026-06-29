using UnityEngine; // Подключаем базовые функции Unity

public class DrawerInteract : MonoBehaviour, IInteractable // Скрипт для выдвижения ящика
{
    public enum SlideDirection // Список направлений движения ящика
    {
        Forward, // Вперед по локальной Z
        Back, // Назад по локальной Z
        Right, // Вправо по локальной X
        Left // Влево по локальной X
    }

    [Header("Drawer Movement Settings")] // Блок движения
    public float slideDistance = 0.4f; // Дистанция выдвижения ящика

    public float moveSpeed = 3f; // Скорость движения ящика

    public SlideDirection slideDirection = SlideDirection.Back; // Направление выезда ящика

    [Header("Noise")] // Блок шума
    public NoiseEmitter noiseEmitter; // Источник шума ящика

    [Range(1, 10)] public int openNoisePower = 3; // Шум открытия ящика

    [Range(1, 10)] public int closeNoisePower = 2; // Шум закрытия ящика

    private Vector3 closedLocalPosition; // Закрытая локальная позиция

    private Vector3 openLocalPosition; // Открытая локальная позиция

    private Vector3 targetLocalPosition; // Целевая локальная позиция

    private bool isOpen = false; // Открыт ли ящик

    void Start() // Запуск сцены
    {
        closedLocalPosition = transform.localPosition; // Запоминаем закрытую позицию

        openLocalPosition = closedLocalPosition + GetSlideVector() * slideDistance; // Считаем открытую позицию

        targetLocalPosition = closedLocalPosition; // В начале цель — закрытая позиция

        if (noiseEmitter == null) // Если NoiseEmitter не назначен вручную
        {
            noiseEmitter = GetComponent<NoiseEmitter>(); // Пробуем найти NoiseEmitter на этом же объекте
        }
    }

    void Update() // Каждый кадр
    {
        transform.localPosition = Vector3.Lerp( // Плавно двигаем ящик
            transform.localPosition, // От текущей позиции
            targetLocalPosition, // К целевой позиции
            Time.deltaTime * moveSpeed // С учетом скорости и времени
        );
    }

    public void Interact() // Вызывается PlayerInteractor при E
    {
        ToggleDrawer(); // Переключаем ящик
    }

    void ToggleDrawer() // Метод открытия/закрытия
    {
        isOpen = !isOpen; // Меняем состояние на противоположное

        targetLocalPosition = isOpen ? openLocalPosition : closedLocalPosition; // Выбираем позицию

        EmitDrawerNoise(); // Создаем шум ящика
    }

    void EmitDrawerNoise() // Метод шума ящика
    {
        if (noiseEmitter == null) return; // Если NoiseEmitter нет — выходим

        int noisePower = isOpen ? openNoisePower : closeNoisePower; // Если открываем — openNoisePower, если закрываем — closeNoisePower

        noiseEmitter.EmitNoise(noisePower); // Отправляем шум
    }

    Vector3 GetSlideVector() // Получить направление движения
    {
        switch (slideDirection) // Проверяем выбранное направление
        {
            case SlideDirection.Forward: // Если Forward
                return Vector3.forward; // Возвращаем вперед

            case SlideDirection.Back: // Если Back
                return Vector3.back; // Возвращаем назад

            case SlideDirection.Right: // Если Right
                return Vector3.right; // Возвращаем вправо

            case SlideDirection.Left: // Если Left
                return Vector3.left; // Возвращаем влево

            default: // Если что-то пошло не так
                return Vector3.back; // По умолчанию назад
        }
    }
}