using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Upstream.Ads
{
    /// <summary>
    /// Ad service for the editor and for builds without the AdMob plugin.
    /// Shows a visible full-screen "TEST AD" placeholder for the ad's duration so the flow
    /// (continue reward, interstitial frequency) can be tested on a phone without real ads.
    /// </summary>
    public class MockAdsService : MonoBehaviour, IAdsService
    {
        private const float RewardedSeconds = 3f;
        private const float InterstitialSeconds = 2f;

        private GameObject overlay;
        private TMP_Text overlayLabel;
        private bool showing;

        public bool IsRewardedAdReady => !showing;
        public bool IsInterstitialAdReady => !showing;

        public void Initialize()
        {
            Debug.Log("[MockAds] Initialized.");
        }

        public void LoadRewardedAd() { }
        public void LoadInterstitialAd() { }

        public void ShowRewardedAd(Action onRewardGranted, Action onAdFailed)
        {
            if (showing)
            {
                onAdFailed?.Invoke();
                return;
            }
            StartCoroutine(ShowAdRoutine("TEST REKLAMI / TEST AD\n<size=50%>(ödüllü / rewarded)</size>", RewardedSeconds, onRewardGranted));
        }

        public void ShowInterstitialAd(Action onAdClosed)
        {
            if (showing)
            {
                onAdClosed?.Invoke();
                return;
            }
            StartCoroutine(ShowAdRoutine("TEST REKLAMI / TEST AD\n<size=50%>(geçiş / interstitial)</size>", InterstitialSeconds, onAdClosed));
        }

        private IEnumerator ShowAdRoutine(string text, float duration, Action onComplete)
        {
            showing = true;
            ShowOverlay(text);
            yield return new WaitForSecondsRealtime(duration);
            HideOverlay();
            showing = false;
            onComplete?.Invoke();
        }

        private void ShowOverlay(string text)
        {
            if (overlay == null) BuildOverlay();
            overlayLabel.text = text;
            overlay.SetActive(true);
        }

        private void HideOverlay()
        {
            if (overlay != null) overlay.SetActive(false);
        }

        private void BuildOverlay()
        {
            overlay = new GameObject("MockAdOverlay");
            overlay.transform.SetParent(transform, false);
            var canvas = overlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // above all game UI, like a real full-screen ad
            overlay.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            overlay.AddComponent<GraphicRaycaster>(); // swallow taps while the "ad" is up

            var bg = new GameObject("Background");
            bg.transform.SetParent(overlay.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.96f);

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(overlay.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            overlayLabel = labelObj.AddComponent<TextMeshProUGUI>();
            overlayLabel.alignment = TextAlignmentOptions.Center;
            overlayLabel.fontSize = 48f;
            overlayLabel.color = Color.white;
            overlayLabel.raycastTarget = false;

            overlay.SetActive(false);
        }
    }
}
