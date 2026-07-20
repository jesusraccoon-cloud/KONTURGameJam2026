using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Presentation.Elements
{
    /// <summary>
    /// UI-компонент, объединяющий слайдер, поле ввода и кнопки увеличения/уменьшения.
    /// Позволяет изменять значение слайдера вручную или с помощью кнопок, 
    /// автоматически синхронизируя все элементы между собой.
    /// </summary>
    public class SliderField : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Range(.01f, .2f)] private float creaseStep = 0.1f;
        [Header("Bindings")]
        [SerializeField] private Slider slider;
        [SerializeField] private Button buttonIncrease;
        [SerializeField] private Button buttonDecrease;
        [SerializeField] private TMP_InputField inputField;

        private void Awake()
        {
            slider.onValueChanged.AddListener(SliderValueChangedHandler);
            buttonIncrease.onClick.AddListener(Increase);
            buttonDecrease.onClick.AddListener(Decrease);
            inputField.onSubmit.AddListener(TextInputHandler);
        }

        private void Start()
        {
            slider.onValueChanged.Invoke(slider.value);
        }

        private void OnDestroy()
        {
            slider.onValueChanged.RemoveListener(SliderValueChangedHandler);
            buttonIncrease.onClick.RemoveListener(Increase);
            buttonDecrease.onClick.RemoveListener(Decrease);
            inputField.onSubmit.RemoveListener(TextInputHandler);
        }

        private void Increase()
        {
            slider.value += creaseStep;
        }

        private void Decrease()
        {
            slider.value -= creaseStep;
        }

        private void SliderValueChangedHandler(float newValue)
        {
            inputField.text = FormatNumber(newValue);
        }

        private void TextInputHandler(string text)
        {
            if (float.TryParse(text, out float newValue))
            {
                slider.value = newValue;
            }
            else
            {
                inputField.text = FormatNumber(slider.value);
            }
        }

        private string FormatNumber(float number)
        {
            if (number % 1 == 0)
                return number.ToString();
            else
                return number.ToString("F2");
        }
    }
}
