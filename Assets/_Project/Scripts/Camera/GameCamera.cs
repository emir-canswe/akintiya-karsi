using UnityEngine;
using System.Collections;
using Upstream.Core;

namespace Upstream.Camera
{
    public class GameCamera : MonoBehaviour
    {
        /// <summary>Design-time vertical half-extent of the view (world units).</summary>
        public const float DesignHalfHeight = 300f;

        /// <summary>
        /// Current vertical half-extent of the (un-zoomed) view in world units. Spawning, despawning and
        /// background art use this so nothing pops in on screen when tall phones get a taller view.
        /// </summary>
        public static float ViewHalfHeight { get; private set; } = DesignHalfHeight;

        private UnityEngine.Camera cam;
        private float baseOrthoSize;
        private float currentShakeAmount;
        private bool shakeApplied;
        private float lastImpactTime = -10f;
        private Vector3 originalPos;

        private float fitHalfWidth = -1f;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private Coroutine zoomCoroutine;
        private Coroutine bulletTimeCoroutine;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            baseOrthoSize = cam.orthographicSize;
            originalPos = transform.localPosition;
            // The game world renders full-bleed; the safe area is applied to the UI only
            // (shrinking the camera rect left unrendered black bars on notched phones).
            cam.rect = new Rect(0f, 0f, 1f, 1f);
        }

        /// <summary>
        /// Sizes the orthographic camera so the whole river (plus a little bank) is always visible,
        /// using the design height on normal phones and a taller view on very tall/narrow screens.
        /// </summary>
        public void FitToRiver(float riverHalfWidth, float bankMargin = 22f)
        {
            fitHalfWidth = riverHalfWidth + bankMargin;
            ApplyFit();
        }

        public void SetBasePosition(Vector3 position)
        {
            originalPos = position;
            transform.localPosition = position;
        }

        private void ApplyFit()
        {
            if (cam == null || fitHalfWidth <= 0f) return;

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 9f / 16f;

            baseOrthoSize = Mathf.Max(DesignHalfHeight, fitHalfWidth / Mathf.Max(0.01f, aspect));
            ViewHalfHeight = baseOrthoSize;

            if (zoomCoroutine != null)
            {
                StopCoroutine(zoomCoroutine);
                zoomCoroutine = null;
            }
            cam.orthographicSize = baseOrthoSize;
        }

        private void Update()
        {
            if (fitHalfWidth > 0f && (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight))
            {
                ApplyFit();
            }

            if (currentShakeAmount > 0)
            {
                Vector3 randomOffset = Random.insideUnitSphere * currentShakeAmount;
                randomOffset.z = 0;
                transform.localPosition = originalPos + randomOffset;
                currentShakeAmount = Mathf.MoveTowards(currentShakeAmount, 0f, Time.unscaledDeltaTime * 5f);
                shakeApplied = true;
            }
            else if (shakeApplied)
            {
                // Back to rest once; moving the camera transform every frame would dirty its matrices for nothing.
                transform.localPosition = originalPos;
                shakeApplied = false;
            }
        }

        public void Shake(float intensity)
        {
            currentShakeAmount = Mathf.Max(currentShakeAmount, intensity);
        }

        public void ZoomIn(float amount, float duration)
        {
            if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
            zoomCoroutine = StartCoroutine(ZoomRoutine(baseOrthoSize * (1f - amount), duration));
        }

        public void ZoomOut(float amount, float duration)
        {
            if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
            zoomCoroutine = StartCoroutine(ZoomRoutine(baseOrthoSize * (1f + amount), duration));
        }

        /// <summary>
        /// A short, heavy hit: a shake, a tiny freeze and a quick zoom punch. Skipped when a longer effect
        /// (like devouring a shark) is already playing, and rate-limited so a chain of hits stays readable.
        /// </summary>
        public void Impact(float shake)
        {
            Shake(shake);
            if (zoomCoroutine != null || bulletTimeCoroutine != null) return;
            if (Time.unscaledTime - lastImpactTime < 0.18f) return;
            lastImpactTime = Time.unscaledTime;
            zoomCoroutine = StartCoroutine(ZoomRoutine(baseOrthoSize * 0.94f, 0.06f, 0.04f));
            bulletTimeCoroutine = StartCoroutine(BulletTimeRoutine(0.15f, 0.05f));
        }

        private IEnumerator ZoomRoutine(float targetSize, float duration, float hold = 0.5f)
        {
            float startSize = cam.orthographicSize;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                cam.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / duration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            cam.orthographicSize = targetSize;

            // Wait briefly before resetting
            yield return new WaitForSecondsRealtime(hold);

            elapsed = 0f;
            startSize = cam.orthographicSize;
            while (elapsed < duration)
            {
                cam.orthographicSize = Mathf.Lerp(startSize, baseOrthoSize, elapsed / duration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            cam.orthographicSize = baseOrthoSize;
            zoomCoroutine = null;
        }

        public void BulletTime(float timeScale, float duration)
        {
            if (bulletTimeCoroutine != null) StopCoroutine(bulletTimeCoroutine);
            bulletTimeCoroutine = StartCoroutine(BulletTimeRoutine(timeScale, duration));
        }

        private IEnumerator BulletTimeRoutine(float targetScale, float duration)
        {
            Time.timeScale = targetScale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused ? 0f : 1f;
            bulletTimeCoroutine = null;
        }
    }
}
