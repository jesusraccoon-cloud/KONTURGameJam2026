namespace Core.Presentation
{
    public interface ISavable<T>
    {
        void SaveData(T data);
    }
}
