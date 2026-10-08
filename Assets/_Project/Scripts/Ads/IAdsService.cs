using System;

namespace Upstream.Ads
{
    /// <summary>
    /// Interface for ad services.
    /// </summary>
    public interface IAdsService
    {
        void Initialize();
        void LoadRewardedAd();
        void ShowRewardedAd(Action onRewardGranted, Action onAdFailed);
        void LoadInterstitialAd();
        void ShowInterstitialAd(Action onAdClosed);
        bool IsRewardedAdReady { get; }
        bool IsInterstitialAdReady { get; }
    }
}
