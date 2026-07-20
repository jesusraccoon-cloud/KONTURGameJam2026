using R3;

namespace Core
{
    public interface IService
    {
        ReadOnlyReactiveProperty<LifecycleState> Status { get; }

        /// <summary>
        /// Запуск сервиса
        /// </summary>
        void Startup();
    }
}
