public interface IHoldInteractable // Интерфейс для объектов, которые реагируют на удержание кнопки
{
    void HoldInteract(float holdTime); // Метод вызывается каждый кадр, пока игрок держит E

    void HoldCancel(float holdTime); // Метод вызывается, когда игрок отпустил E после долгого удержания
}