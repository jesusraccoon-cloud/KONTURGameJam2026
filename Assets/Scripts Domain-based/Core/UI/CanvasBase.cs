using System;
using UnityEngine;

namespace Core.Presentation
{
    /// <summary>
    /// Базовый класс для всех Canvas
    /// </summary>
    /// <remarks>
    /// Имеет возможность скрывать/раскрывать Canvas через SetActive(bool)
    /// и публичное событие <see cref="VisibleUpdated"/>
    /// </remarks>
    public class CanvasBase : MonoBehaviour, IScreen<CanvasBase>
    {
        public bool IsVisible { get; private set; } = true;

        public event Action<CanvasBase, bool> VisibleUpdated;

        public void Show(bool isShow)
        {
            IsVisible = isShow;
            this.gameObject.SetActive(isShow);

            VisibleUpdated?.Invoke(this, isShow);
        }
    }
}
