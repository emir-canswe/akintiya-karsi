using System;
using UnityEngine;
using Upstream.Core;

namespace Upstream.Ads
{
    /// <summary>
    /// Single entry point for ads: picks the implementation, asks for UMP consent first (EU/Turkey),
    /// then initializes ads, and applies the interstitial frequency policy.
    ///
    /// Real AdMob is used only in device builds with the USE_ADMOB define; the editor and builds
    /// without the plugin use MockAdsService. A future "Remove Ads" purchase only needs to set
    /// SaveData.adsRemoved = true (interstitials stop; rewarded continues stay opt-in).
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance { get; private set; }

        public IAdsService Ads { get; private set; }
        public bool IsInitialized { get; private set; }

        private bool usedContinueThisRun;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

#if USE_ADMOB && !UNITY_EDITOR
            Ads = gameObject.AddComponent<AdMobService>();
#else
            Ads = gameObject.AddComponent<MockAdsService>();
#endif

            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnContinueUsed += HandleContinueUsed;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnContinueUsed -= HandleContinueUsed;
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // Consent must be resolved before any ad request is made.
            var consent = gameObject.AddComponent<UmpConsentService>();
            consent.RequestConsent(canRequestAds =>
            {
                if (!canRequestAds)
                {
                    Debug.Log("[Ads] Consent not given; ads stay disabled.");
                    return;
                }
                Ads.Initialize();
                IsInitialized = true;
            });
        }

        private void HandleGameStart() => usedContinueThisRun = false;
        private void HandleContinueUsed() => usedContinueThisRun = true;

        /// <summary>
        /// Called when the player taps "Play Again": shows an interstitial if the policy allows,
        /// then runs <paramref name="next"/> (immediately when no ad is shown).
        /// </summary>
        public void ShowInterstitialIfDueThen(Action next)
        {
            var save = SaveSystem.Load();
            bool due = IsInitialized &&
                       InterstitialPolicy.ShouldShowInterstitial(save.totalGamesPlayed, usedContinueThisRun, save.adsRemoved) &&
                       Ads.IsInterstitialAdReady;

            if (!due)
            {
                next?.Invoke();
                return;
            }

            Ads.ShowInterstitialAd(() => next?.Invoke());
        }
    }
}
