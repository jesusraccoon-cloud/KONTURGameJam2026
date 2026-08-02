using R3;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Класс, предназначенный для реализации глобальных и локальных сервисов игры
    /// </summary>
    public abstract class ServiceBase<T> : MonoSingleton<T>, IService
        where T : class
    {
        protected ReactiveProperty<LifecycleState> status = new();

        public ReadOnlyReactiveProperty<LifecycleState> Status => status.ToReadOnlyReactiveProperty();

        public abstract void Startup();

        protected virtual void Awake()
        {
            base.Awake();
            Status.Subscribe(OnStatusUpdated).AddTo(this);
            Startup();
        }

        private void OnStatusUpdated(LifecycleState status)
        {
            print($"{name} is {status}");
        }
    }
}
