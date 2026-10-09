using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Upstream.UI
{
    /// <summary>
    /// Main menu "how to play" card: lights the steps up one after another (a little bounce, full brightness)
    /// so it reads as a story to follow, not as a row of navigation buttons.
    /// </summary>
    public class TipsAnimator : MonoBehaviour
    {
        private const float StepSeconds = 1.1f;
        private const float DimAlpha = 0.6f;

        private RectTransform[] icons;
        private Image[] iconImages;
        private TMP_Text[] labels;
        private float timer;
        private int current;

        public void Configure(RectTransform[] tipIcons, TMP_Text[] tipLabels)
        {
            icons = tipIcons;
            labels = tipLabels;
            iconImages = new Image[icons.Length];
            for (int i = 0; i < icons.Length; i++) iconImages[i] = icons[i].GetComponent<Image>();
        }

        private void OnEnable()
        {
            timer = 0f;
            current = 0;
        }

        private void Update()
        {
            if (icons == null) return;
            // Unscaled: the menu can show while time is stopped.
            timer += Time.unscaledDeltaTime;
            if (timer >= StepSeconds)
            {
                timer -= StepSeconds;
                current = (current + 1) % icons.Length;
            }

            float t = timer / StepSeconds;
            for (int i = 0; i < icons.Length; i++)
            {
                bool on = i == current;
                // Hop up and settle at the start of its turn.
                float hop = on ? Mathf.Sin(Mathf.Clamp01(t * 2.5f) * Mathf.PI) : 0f;
                float s = on ? 1.12f + hop * 0.12f : 1f;
                icons[i].localScale = new Vector3(s, s, 1f);
                icons[i].anchoredPosition = new Vector2(icons[i].anchoredPosition.x, 9f + hop * 5f);
                float a = on ? 1f : DimAlpha;
                if (iconImages[i] != null) iconImages[i].color = new Color(1f, 1f, 1f, a);
                if (labels[i] != null) labels[i].alpha = on ? 1f : 0.75f;
            }
        }
    }
}
