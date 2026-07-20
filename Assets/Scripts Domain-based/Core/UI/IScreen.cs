namespace Core.Presentation
{
    public interface IScreen<T> where T : IScreen<T>
    {
        bool IsVisible { get; }

        //void Initialize(); // TODO: мб пригодится
        void Show(bool isShow);
    }
}
