using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.Elements
{
    /// <summary>
    /// Компонент, прикрепляемый к <see cref="ScrollRect"/> для автоматического
    /// цикличного прокручивания
    /// </summary>
    /// <remarks>
    /// Можно использовать для прокручивания текста на экране авторов
    /// </remarks>
    [RequireComponent(typeof(ScrollRect))]
    public class AutoScrollRect : MonoBehaviour
    {
        [Header("Scroll Settings")]
        [SerializeField, Tooltip("Скорость прокрутки")]
        private float scrollSpeed = 0.1f;
        [SerializeField, Tooltip("Пауза перед возвратом")]
        private float delayAtEnd = 2f;
        [SerializeField, Tooltip("Пауза перед повторной прокруткой")]
        private float delayAtStart = 0.5f;

        private ScrollRect scrollRect;
        private Coroutine scrollCoroutine;


        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void OnEnable()
        {
            scrollCoroutine = StartCoroutine(ScrollLoop());
        }

        private void OnDisable()
        {
            if (scrollCoroutine != null)
                StopCoroutine(scrollCoroutine);
        }

        private IEnumerator ScrollLoop()
        {
            while (true)
            {
                while (scrollRect.verticalNormalizedPosition > 0f)
                {
                    scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;
                    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
                    yield return null;
                }

                yield return new WaitForSeconds(delayAtEnd);

                scrollRect.verticalNormalizedPosition = 1f;

                yield return new WaitForSeconds(delayAtStart);
            }
        }
    }
}
