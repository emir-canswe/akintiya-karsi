using UnityEngine;
using UnityEngine.UI;

namespace Upstream.UI
{
    /// <summary>
    /// Keeps the UI layout working from tall phones to tablets. Phones (as tall as 9:16 or taller) keep the
    /// balanced width/height match the screens were designed with. Wider screens (tablets, foldables) match
    /// the height instead, so the vertical layout stays exactly as designed and the extra width just
    /// becomes margin; otherwise the menu gets squashed and the title runs into the fish and coin counter.
    /// Re-applies when the screen size changes (foldables, split screen).
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class AdaptiveCanvasScaler : MonoBehaviour
    {
        /// <summary>Width / height of the design resolution (360 x 640).</summary>
        public const float DesignAspect = 9f / 16f;

        private CanvasScaler scaler;
        private int lastWidth, lastHeight;

        public static float MatchFor(float aspect) => aspect > DesignAspect + 0.01f ? 1f : 0.5f;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) Apply();
        }

        private void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            if (lastHeight <= 0) return;
            scaler.matchWidthOrHeight = MatchFor((float)lastWidth / lastHeight);
        }
    }
}
