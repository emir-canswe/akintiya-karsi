namespace Upstream.Ads
{
    /// <summary>
    /// Pure C# rules for deciding when to show interstitial ads at Game Over.
    /// Never shows ads during the first 3 games, never shows an interstitial if the player
    /// already watched a rewarded continue ad in that run, and shows every 3 games thereafter.
    /// </summary>
    public static class InterstitialPolicy
    {
        public const int GraceGamesCount = 3;
        public const int FrequencyGames = 3;

        public static bool ShouldShowInterstitial(int totalGamesPlayed, bool usedRewardedContinueThisRun, bool adsRemoved = false)
        {
            if (adsRemoved) return false;
            if (usedRewardedContinueThisRun) return false;
            if (totalGamesPlayed <= GraceGamesCount) return false;
            return (totalGamesPlayed - GraceGamesCount) % FrequencyGames == 0;
        }
    }
}
