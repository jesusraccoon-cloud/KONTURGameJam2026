using Core;
using UnityEngine;
using R3;

namespace Presentation.Elements
{
    /// <summary>
    /// Компонент управляющий переключением вкладок
    /// </summary>
    [AddComponentMenu("UI/Tabs/Tab Group")]
    public class TabGroup : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Sprite backgroundInactive;
        [SerializeField] private Sprite backgroundActive;
        [Header("Bindgins")]
        [SerializeField] private KeyValuePair<TabButton, TabView>[] tabButtons;

        public int CurrentTabIndex { get; private set; } = -1;


        private void Awake()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                TabButton btn = tabButtons[i].Key;
                btn.TabIndex = i;
                btn.OnClick.Subscribe(SelectTab).AddTo(this);
                tabButtons[i].Value.Hide();
            }
        }

        private void Start()
        {
            if (tabButtons.Length > 0) SelectTab(0); // Автовыбор первой вкладки
        }

        public void SelectTab(int index)
        {
            if (index == CurrentTabIndex ||
                index >= tabButtons.Length ||
                index < 0) return;

            if (CurrentTabIndex > -1)
            {
                tabButtons[CurrentTabIndex].Key.SetSelected(false);
                tabButtons[CurrentTabIndex].Value.Hide();
                tabButtons[CurrentTabIndex].Key.Background.sprite = backgroundInactive;
            }

            tabButtons[index].Key.SetSelected(true);
            tabButtons[index].Value.Show();
            tabButtons[index].Key.Background.sprite = backgroundActive;

            CurrentTabIndex = index;
        }
    }
}
