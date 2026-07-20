using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using R3;
using TMPro;

namespace Presentation.Elements
{
    /// <summary>
    /// Компонент прикрепляемый к кнопке (может быть что угодно) для переключения вкладок по нажатию на нее
    /// </summary>
    [AddComponentMenu("UI/Tabs/Tab Button")]
    public class TabButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Settings")]
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text textCaption;
        [Header("Events Callbacks"), Space]
        [SerializeField] private UnityEvent onSelected;
        [SerializeField] private UnityEvent onDeselected;

        internal Subject<int> OnClick { get; private set; } = new(); // проблема инкапсуляции, но она не критичная
        public int TabIndex { get; set; }
        public Image Background => background;
        public TMP_Text Caption => textCaption;


        public void SetSelected(bool isSelected)
        {
            if (isSelected)
                onSelected.Invoke();
            else
                onDeselected.Invoke();
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            OnClick.OnNext(TabIndex);
        }
    }
}
