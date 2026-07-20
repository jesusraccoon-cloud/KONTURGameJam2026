using UnityEngine;

namespace Presentation.Elements
{
    /// <summary>
    /// Компонент, прикрепляемый к вкладке для управления ее отображением
    /// </summary>
    [AddComponentMenu("UI/Tabs/Tab View")]
    public class TabView : MonoBehaviour
    {
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
