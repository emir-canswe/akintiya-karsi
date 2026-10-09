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
        private static readonly Color ActiveLabel = new Color(1f, 0.88f, 0.4f, 1f);

        private RectTransform[] icons;
        private TMP_Text[] labels;
        private Image[] glows;
        private float timer;
        private int current;

        public void Configure(RectTransform[] tipIcons, TMP_Text[] tipLabels, Image[] tipGlows)
        {
            icons = tipIcons;
            labels = tipLabels;
            glows = tipGlows;
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
                // Every step stays fully readable; the current one gets a warm glow and a yellow word.
                if (glows != null && glows[i] != null)
                {
                    Color g = glows[i].color;
                    g.a = on ? 0.55f * Mathf.Clamp01(t * 6f) * (1f - Mathf.Clamp01((t - 0.85f) * 6f)) : 0f;
                    glows[i].color = g;
                }
                if (labels[i] != null) labels[i].color = on ? ActiveLabel : Color.white;
            }
        }
    }
}
