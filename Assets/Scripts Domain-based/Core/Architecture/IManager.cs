using System;
using R3;

namespace Core
{
    public interface IManager : IDisposable
    {
        ReadOnlyReactiveProperty<LifecycleState> Status { get; }

        /// <summary>
        /// Инициализация
        /// </summary>
        void Initialize();

        /// <summary>
        /// Заапуск менеджера
        /// </summary>
        void Startup();
    }
}
