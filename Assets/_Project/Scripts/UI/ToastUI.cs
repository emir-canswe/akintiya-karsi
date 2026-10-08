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

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        public void Show(string text, Color color)
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
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

            Vector3 startPos = transform.localPosition;
            Vector3 endPos = startPos + Vector3.up * 50f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

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
                transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                canvasGroup.alpha = t > 0.7f ? Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f) : 1f;

                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            activeRoutine = null;
        }
    }
}
