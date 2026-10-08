using System;
using System.Collections;
using UnityEngine;

namespace Upstream.Ads
{
    /// <summary>
    /// Mock ad service for testing purposes.
    /// </summary>
    public class MockAdsService : MonoBehaviour, IAdsService
    {
        public bool IsRewardedAdReady => true;
        public bool IsInterstitialAdReady => true;

        public void Initialize()
        {
            Debug.Log("[MockAds] Initialized.");
        }

        public void LoadRewardedAd()
        {
            Debug.Log("[MockAds] Loading Rewarded Ad.");
        }

        public void ShowRewardedAd(Action onRewardGranted, Action onAdFailed)
        {
            Debug.Log("[MockAds] Showing Rewarded Ad...");
            StartCoroutine(ShowAdRoutine(3f, onRewardGranted));
        }

        public void LoadInterstitialAd()
        {
            Debug.Log("[MockAds] Loading Interstitial Ad.");
        }

        public void ShowInterstitialAd(Action onAdClosed)
        {
            Debug.Log("[MockAds] Showing Interstitial Ad...");
            StartCoroutine(ShowAdRoutine(2f, onAdClosed));
        }

        private IEnumerator ShowAdRoutine(float duration, Action onComplete)
        {
            yield return new WaitForSecondsRealtime(duration);
            Debug.Log("[MockAds] Ad closed.");
            onComplete?.Invoke();
        }
    }
}
