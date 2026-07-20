using UnityEngine;
using R3;

namespace Core
{
    /// <summary>
    /// Класс, предназначенный для реализации игровых менеджеров и систем
    /// </summary>
    public abstract class ManagerBase : MonoBehaviour, IManager
    {
        protected ReactiveProperty<LifecycleState> status = new();

        public ReadOnlyReactiveProperty<LifecycleState> Status =>
            status.ToReadOnlyReactiveProperty();


        public abstract void Initialize();
        public abstract void Startup();
        public abstract void Dispose();


        protected virtual void Awake()
        {
            Status.Subscribe(OnStatusUpdated).AddTo(this);
            Initialize();
        }

        protected virtual void Start()
        {
            Startup();
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }


        private void OnStatusUpdated(LifecycleState status)
        {
            print($"{name} is {status}");
        }
    }
}
