using System;
using UnityEngine;
#if USE_ADMOB
using GoogleMobileAds.Api;
#endif

namespace Upstream.Ads
{
    /// <summary>
    /// AdMob implementation of IAdsService. Compiled for real only when the Google Mobile Ads plugin is
    /// installed and the USE_ADMOB scripting define is set; otherwise every call fails gracefully.
    ///
    /// Ad unit ids: Google's official TEST ids are the defaults. Real ids are read at runtime from
    /// Assets/Resources/ad_config.json (gitignored, see Assets/_Project/Config/ad_config.example.json).
    /// </summary>
    public class AdMobService : MonoBehaviour, IAdsService
    {
        private const string ConfigResource = "ad_config";

#if UNITY_IOS
        private string rewardedAdId = "ca-app-pub-3940256099942544/1712485313";
        private string interstitialAdId = "ca-app-pub-3940256099942544/4411468910";
#else
        private string rewardedAdId = "ca-app-pub-3940256099942544/5224354917";
        private string interstitialAdId = "ca-app-pub-3940256099942544/1033173712";
#endif

        public string RewardedAdId => rewardedAdId;
        public string InterstitialAdId => interstitialAdId;

#if USE_ADMOB
        private RewardedAd rewardedAd;
        private InterstitialAd interstitialAd;
#endif

        public bool IsRewardedAdReady
        {
            get
            {
#if USE_ADMOB
                return rewardedAd != null && rewardedAd.CanShowAd();
#else
                return false;
#endif
            }
        }

        public bool IsInterstitialAdReady
        {
            get
            {
#if USE_ADMOB
                return interstitialAd != null && interstitialAd.CanShowAd();
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            // Resources (not StreamingAssets): on Android StreamingAssets lives inside the APK,
            // so File.Exists on it is always false and the real ids would never be read.
            var asset = Resources.Load<TextAsset>(ConfigResource);
            if (asset == null) return;

            try
            {
                var config = JsonUtility.FromJson<AdConfig>(asset.text);
                if (config == null) return;
#if UNITY_IOS
                if (!string.IsNullOrEmpty(config.ios_rewarded_id)) rewardedAdId = config.ios_rewarded_id;
                if (!string.IsNullOrEmpty(config.ios_interstitial_id)) interstitialAdId = config.ios_interstitial_id;
#else
                if (!string.IsNullOrEmpty(config.android_rewarded_id)) rewardedAdId = config.android_rewarded_id;
                if (!string.IsNullOrEmpty(config.android_interstitial_id)) interstitialAdId = config.android_interstitial_id;
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdMob] Failed to parse ad_config.json: {e.Message}");
            }
        }

        public void Initialize()
        {
#if USE_ADMOB
            // Ad callbacks otherwise arrive on a background thread; touching Unity objects from there crashes.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.Initialize(initStatus =>
            {
                LoadRewardedAd();
                LoadInterstitialAd();
            });
#else
            Debug.Log("[AdMob] USE_ADMOB is not defined; AdMob is disabled.");
#endif
        }

        public void LoadRewardedAd()
        {
#if USE_ADMOB
            if (rewardedAd != null)
            {
                rewardedAd.Destroy();
                rewardedAd = null;
            }

            RewardedAd.Load(rewardedAdId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[AdMob] Rewarded ad failed to load: {error}");
                    return;
                }
                rewardedAd = ad;
            });
#endif
        }

        public void ShowRewardedAd(Action onRewardGranted, Action onAdFailed)
        {
#if USE_ADMOB
            if (!IsRewardedAdReady)
            {
                LoadRewardedAd();
                onAdFailed?.Invoke();
                return;
            }

            // The reward callback fires while the ad is still on screen. Remember it and grant the
            // continue only once the ad is closed, so the run never resumes behind the ad.
            bool earned = false;
            bool finished = false;

            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                if (finished) return;
                finished = true;
                LoadRewardedAd();
                if (earned) onRewardGranted?.Invoke();
                else onAdFailed?.Invoke();
            };
            rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
            {
                if (finished) return;
                finished = true;
                Debug.LogWarning($"[AdMob] Rewarded ad failed to show: {error}");
                LoadRewardedAd();
                onAdFailed?.Invoke();
            };

            rewardedAd.Show((Reward reward) => earned = true);
#else
            onAdFailed?.Invoke();
#endif
        }

        public void LoadInterstitialAd()
        {
#if USE_ADMOB
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
                interstitialAd = null;
            }

            InterstitialAd.Load(interstitialAdId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[AdMob] Interstitial ad failed to load: {error}");
                    return;
                }
                interstitialAd = ad;
            });
#endif
        }

        public void ShowInterstitialAd(Action onAdClosed)
        {
#if USE_ADMOB
            if (!IsInterstitialAdReady)
            {
                LoadInterstitialAd();
                onAdClosed?.Invoke();
                return;
            }

            bool finished = false;
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                if (finished) return;
                finished = true;
                LoadInterstitialAd();
                onAdClosed?.Invoke();
            };
            interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
            {
                if (finished) return;
                finished = true;
                LoadInterstitialAd();
                onAdClosed?.Invoke();
            };

            interstitialAd.Show();
#else
            onAdClosed?.Invoke();
#endif
        }
    }

    [Serializable]
    public class AdConfig
    {
        public string android_rewarded_id;
        public string android_interstitial_id;
        public string ios_rewarded_id;
        public string ios_interstitial_id;
    }
}
