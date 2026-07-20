using Core.Presentation;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace Presentation.Elements
{
    /// <summary>
    /// UI-элемент реализует ProgressBar c помощью <see cref="TMP_Text"/> и <see cref="Image"/>,
    /// который используется для визуальной индикации
    /// </summary>
    /// <remarks>
    /// Для работы с элементом нужно изменять данные с помощью метода <see cref="UpdateData(float)"/>
    /// </remarks>
    public class ProgressBar : MonoBehaviour, IDataUpdatable<float>, IRefreshable
    {
        [Header("Settings")]
        [Range(0f, .5f)]
        [SerializeField] private float animationSmoothDuration = 0.1f;
        [Header("Bindings")]
        [SerializeField] private TMP_Text textBar;
        [SerializeField] private Image imageProggres;

        private float progress;

        public void Refresh()
        {
            textBar?.SetText(progress.ToString("P0"));
            imageProggres?.DOFillAmount(progress, animationSmoothDuration);
        }

        public void UpdateData(float data)
        {
            progress = data;
            Refresh();
        }
    }
}
