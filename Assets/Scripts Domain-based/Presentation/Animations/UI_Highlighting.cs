using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Presentation.Components
{
    /// <summary>
    /// Компонент, прикрепляемый к элементам UI, который меняет Scale при наведении курсора на объект
    /// </summary>
    public class UI_HighlightScaling : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Settings")]
        [SerializeField] private Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.0f);
        [SerializeField] private float animationDuration = .2f;

        private Vector3 originalScale;
        private Coroutine scaleCoroutine;

        private void Start()
        {
            originalScale = transform.localScale;
        }

        private IEnumerator ScaleTo(Vector3 targetScale)
        {
            float elapsedTime = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / animationDuration);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(ScaleTo(hoverScale));
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(ScaleTo(originalScale));
        }
    }
}
