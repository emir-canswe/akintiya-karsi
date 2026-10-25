namespace Upstream.Ads
{
    /// <summary>
    /// Pure C# rules for deciding when to show interstitial ads at Game Over.
    /// Shows an interstitial after every 5th game, never if the player already watched a
    /// rewarded continue ad in that run, and never when ads are removed.
    /// </summary>
    public static class InterstitialPolicy
    {
        public const int FrequencyGames = 5;

        public static bool ShouldShowInterstitial(int totalGamesPlayed, bool usedRewardedContinueThisRun, bool adsRemoved = false)
        {
            if (adsRemoved) return false;
            if (usedRewardedContinueThisRun) return false;
            return totalGamesPlayed > 0 && totalGamesPlayed % FrequencyGames == 0;
        }
    }
}
