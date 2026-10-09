using UnityEngine;
using UnityEngine.UI;

namespace Upstream.UI
{
    /// <summary>
    /// iOS-style on/off switch: a pill track whose color fades and a knob that slides between ends.
    /// Purely visual; the owning screen decides what a tap does and calls <see cref="SetOn"/>.
    /// </summary>
    public class ToggleSwitch : MonoBehaviour
    {
        private static readonly Color OnColor = new Color(0.36f, 0.85f, 0.62f, 1f);
        private static readonly Color OffColor = new Color(1f, 1f, 1f, 0.22f);
        private const float SlideSpeed = 10f;

        private Image track;
        private RectTransform knob;
        private float knobTravel;
        private float t;       // 0 = off, 1 = on (animated)
        private float target;

        public bool IsOn => target > 0.5f;

        public void Configure(Image trackImage, RectTransform knobRect, float travel)
        {
            track = trackImage;
            knob = knobRect;
            knobTravel = travel;
            Apply();
        }

        public void SetOn(bool on, bool animate)
        {
            target = on ? 1f : 0f;
            if (!animate || !isActiveAndEnabled) t = target;
            Apply();
        }

        private void Update()
        {
            if (Mathf.Approximately(t, target)) return;
            // Unscaled: settings can be open while the game is paused (timeScale 0).
            t = Mathf.MoveTowards(t, target, Time.unscaledDeltaTime * SlideSpeed);
            Apply();
        }

        private void Apply()
        {
            float eased = t * t * (3f - 2f * t);
            if (track != null) track.color = Color.Lerp(OffColor, OnColor, eased);
            if (knob != null) knob.anchoredPosition = new Vector2(Mathf.Lerp(-knobTravel, knobTravel, eased), 0f);
        }
    }
}
