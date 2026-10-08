using UnityEngine;
using System.Collections;
using Upstream.Core;

namespace Upstream.Camera
{
    public class GameCamera : MonoBehaviour
    {
        private UnityEngine.Camera cam;
        private float baseOrthoSize;
        private float currentShakeAmount;
        private Vector3 originalPos;

        private Coroutine zoomCoroutine;
        private Coroutine bulletTimeCoroutine;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            baseOrthoSize = cam.orthographicSize;
            originalPos = transform.localPosition;
            HandleSafeArea();
        }

        private void Update()
        {
            if (currentShakeAmount > 0)
            {
                Vector3 randomOffset = Random.insideUnitSphere * currentShakeAmount;
                randomOffset.z = 0;
                transform.localPosition = originalPos + randomOffset;
                currentShakeAmount = Mathf.MoveTowards(currentShakeAmount, 0f, Time.unscaledDeltaTime * 5f);
            }
            else
            {
                transform.localPosition = originalPos;
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

        private IEnumerator ZoomRoutine(float targetSize, float duration)
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
            yield return new WaitForSecondsRealtime(0.5f);

            elapsed = 0f;
            startSize = cam.orthographicSize;
            while (elapsed < duration)
            {
                cam.orthographicSize = Mathf.Lerp(startSize, baseOrthoSize, elapsed / duration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            cam.orthographicSize = baseOrthoSize;
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
        }

        private void HandleSafeArea()
        {
            Rect safeArea = Screen.safeArea;
            Vector2 minAnchor = safeArea.position;
            Vector2 maxAnchor = minAnchor + safeArea.size;

            minAnchor.x /= Screen.width;
            minAnchor.y /= Screen.height;
            maxAnchor.x /= Screen.width;
            maxAnchor.y /= Screen.height;

            cam.rect = new Rect(minAnchor.x, minAnchor.y, maxAnchor.x - minAnchor.x, maxAnchor.y - minAnchor.y);
        }
    }
}
