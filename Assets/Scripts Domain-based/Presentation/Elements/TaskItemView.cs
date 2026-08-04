using Gameplay.Quest;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.Elements
{
    /// <summary>
    /// Строка списка задач: иконка, текст и защищённый от игрока Toggle
    /// </summary>
    /// <remarks>
    /// Привязывается к одной задаче через <see cref="Setup"/>. При выполнении задачи
    /// текст зачёркивается и Toggle включается. Игрок не может влиять на Toggle
    /// (interactable = false)
    /// </remarks>
    public class TaskItemView : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private Toggle toggle;
        [SerializeField] private TMP_Text text;
        [SerializeField] private Image icon;
        [SerializeField] private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (toggle != null)
            {
                toggle.interactable = false;
                toggle.transition = Selectable.Transition.None;
            }
        }

        /// <summary>
        /// Привязать строку к задаче
        /// </summary>
        public void Setup(QuestManager.TaskState state)
        {
            if (state?.Data == null) return;

            if (icon != null && state.Data.Icon != null)
                icon.sprite = state.Data.Icon;

            if (text != null)
                text.text = state.Data.Title;

            state.IsCompleted
                .Subscribe(OnCompletedChanged)
                .AddTo(this);
        }

        private void OnCompletedChanged(bool isCompleted)
        {
            if (toggle != null)
                toggle.SetIsOnWithoutNotify(isCompleted);

            if (text != null)
            {
                if (isCompleted)
                    text.fontStyle |= FontStyles.Strikethrough;
                else
                    text.fontStyle &= ~FontStyles.Strikethrough;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = isCompleted ? 0.5f : 1f;
        }
    }
}
