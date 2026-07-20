using UnityEngine;
using UnityEngine.Events;

namespace Core.Presentation
{
    public class ScreenBase : MonoBehaviour, IScreen<ScreenBase>
    {
        [Tooltip("Будет ли экран виден после инициализации")]
        [SerializeField] private bool isVisible;
        [Header("Actinos")]
        [SerializeField] private UnityEvent onShow;
        [SerializeField] private UnityEvent onHide;

        public bool IsVisible => isVisible;


        protected virtual void Awake()
        {
            Show(isVisible);
        }

        public void Show(bool isShow)
        {
            isVisible = isShow;
            this.gameObject.SetActive(isShow);

            if (isShow) onShow.Invoke();
            else onHide.Invoke();
        }
    }
}
