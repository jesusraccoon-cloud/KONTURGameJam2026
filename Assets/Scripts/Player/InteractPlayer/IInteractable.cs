public interface IInteractable
// Создаём интерфейс IInteractable
// interface = контракт для интерактивных объектов
{
    string Hint { get; }
    void Interact();
    // Любой объект с этим интерфейсом ОБЯЗАН иметь метод Interact()
}