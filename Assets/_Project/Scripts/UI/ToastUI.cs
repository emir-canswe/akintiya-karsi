using System.Collections;
using UnityEngine;
using TMPro;

namespace Upstream.UI
{
    /// <summary>
    /// Displays floating toast messages.
    /// </summary>
    public class ToastUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;

        private Coroutine activeRoutine;
        private RectTransform rectTransform;
        private Vector2 restPosition;

        private void Awake()
        {
            // When added at runtime via AddComponent, Awake runs before the bootstrap can wire the
            // serialized fields, so resolve them from the hierarchy instead of assuming they're set.
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);

            // anchoredPosition, not localPosition: it doesn't depend on the canvas having been laid out yet.
            rectTransform = (RectTransform)transform;
            restPosition = rectTransform.anchoredPosition;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        public void Show(string text, Color color)
        {
            if (label == null) return;

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            label.text = text;
            label.color = color;
            gameObject.SetActive(true);

            activeRoutine = StartCoroutine(AnimateToast());
        }

        private IEnumerator AnimateToast()
        {
            float duration = 1f;
            float elapsed = 0f;

            // Always animate from the rest position; using the current position would make the toast
            // creep upward every time a new toast interrupts one that is mid-animation.
            Vector2 startPos = restPosition;
            Vector2 endPos = startPos + Vector2.up * 50f;

            while (elapsed < duration)
            {
                // Unscaled: toasts shown during shark-eat bullet time shouldn't linger.
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Scale up and then back to normal
                float scale = 1f;
                if (t < 0.2f)
                {
                    scale = Mathf.Lerp(0.7f, 1.12f, t / 0.2f);
                }
                else if (t < 0.4f)
                {
                    scale = Mathf.Lerp(1.12f, 1.0f, (t - 0.2f) / 0.2f);
                }

                transform.localScale = Vector3.one * scale;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                if (canvasGroup != null) canvasGroup.alpha = t > 0.7f ? Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f) : 1f;

                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            rectTransform.anchoredPosition = restPosition;
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
            activeRoutine = null;
        }
    }
}
