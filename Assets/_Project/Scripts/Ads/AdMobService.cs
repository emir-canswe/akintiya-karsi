using System;
using System.IO;
using UnityEngine;
#if USE_ADMOB
using GoogleMobileAds.Api;
#endif

namespace Upstream.Ads
{
    /// <summary>
    /// AdMob implementation of IAdsService.
    /// </summary>
    public class AdMobService : MonoBehaviour, IAdsService
    {
        [Header("IMPORTANT: These are TEST IDs. Replace with real IDs from ad_config.json before release.")]
#if UNITY_ANDROID
        private string rewardedAdId = "ca-app-pub-3940256099942544/5224354917";
        private string interstitialAdId = "ca-app-pub-3940256099942544/1033173712";
#elif UNITY_IOS
        private string rewardedAdId = "ca-app-pub-3940256099942544/1712485313";
        private string interstitialAdId = "ca-app-pub-3940256099942544/4411468910";
#else
        private string rewardedAdId = "unexpected_platform";
        private string interstitialAdId = "unexpected_platform";
#endif

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
            string configPath = Path.Combine(Application.streamingAssetsPath, "ad_config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonUtility.FromJson<AdConfig>(json);
#if UNITY_ANDROID
                    if (!string.IsNullOrEmpty(config.android_rewarded_id)) rewardedAdId = config.android_rewarded_id;
                    if (!string.IsNullOrEmpty(config.android_interstitial_id)) interstitialAdId = config.android_interstitial_id;
#elif UNITY_IOS
                    if (!string.IsNullOrEmpty(config.ios_rewarded_id)) rewardedAdId = config.ios_rewarded_id;
                    if (!string.IsNullOrEmpty(config.ios_interstitial_id)) interstitialAdId = config.ios_interstitial_id;
#endif
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AdMob] Failed to load ad_config.json: {e.Message}");
                }
            }
        }

        public void Initialize()
        {
#if USE_ADMOB
            MobileAds.Initialize(initStatus => 
            {
                LoadRewardedAd();
                LoadInterstitialAd();
            });
#else
            Debug.LogWarning("USE_ADMOB is not defined. Ads will not initialize.");
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

            var adRequest = new AdRequest();
            RewardedAd.Load(rewardedAdId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError($"[AdMob] Rewarded ad failed to load: {error}");
                    return;
                }
                rewardedAd = ad;
            });
#endif
        }

        public void ShowRewardedAd(Action onRewardGranted, Action onAdFailed)
        {
#if USE_ADMOB
            if (IsRewardedAdReady)
            {
                rewardedAd.OnAdFullScreenContentClosed += () => 
                {
                    LoadRewardedAd(); // Load next ad
                };
                rewardedAd.OnAdFullScreenContentFailed += (AdError error) => 
                {
                    LoadRewardedAd();
                    onAdFailed?.Invoke();
                };

                rewardedAd.Show((Reward reward) =>
                {
                    onRewardGranted?.Invoke();
                });
            }
            else
            {
                onAdFailed?.Invoke();
            }
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

            var adRequest = new AdRequest();
            InterstitialAd.Load(interstitialAdId, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError($"[AdMob] Interstitial ad failed to load: {error}");
                    return;
                }
                interstitialAd = ad;
            });
#endif
        }

        public void ShowInterstitialAd(Action onAdClosed)
        {
#if USE_ADMOB
            if (IsInterstitialAdReady)
            {
                interstitialAd.OnAdFullScreenContentClosed += () => 
                {
                    LoadInterstitialAd();
                    onAdClosed?.Invoke();
                };
                interstitialAd.OnAdFullScreenContentFailed += (AdError error) => 
                {
                    LoadInterstitialAd();
                    onAdClosed?.Invoke();
                };

                interstitialAd.Show();
            }
            else
            {
                onAdClosed?.Invoke();
            }
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
