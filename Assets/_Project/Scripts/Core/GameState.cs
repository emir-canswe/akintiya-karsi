using System;

namespace Upstream.Core
{
    /// <summary>
    /// Represents the current state of the game.
    /// </summary>
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        Dying,
        GameOver
    }

    /// <summary>
    /// Static class containing all global game events.
    /// </summary>
    public static class GameEvents
    {
        public static Action OnGameStart;
        public static Action OnGameOver;
        public static Action OnGamePause;
        public static Action OnGameResume;
        public static Action<float> OnScoreChanged;
        /// <summary>Fired only when the player actually takes damage (arg: remaining HP).</summary>
        public static Action<int> OnPlayerHurt;
        /// <summary>Fired whenever HP changes for any reason: damage, heal, reset, continue (arg: current HP).</summary>
        public static Action<int> OnHealthChanged;
        /// <summary>Fired after a gold food is resolved with the health it gave back; 0 = HP was full so it became power.</summary>
        public static Action<int> OnGoldFoodResolved;
        public static Action OnPlayerDied;
        public static Action OnTurboStart;
        public static Action OnTurboEnd;
        public static Action<int> OnGrowthStageChanged;
        public static Action<bool> OnFoodEaten;
        public static Action OnSharkEaten;
        public static Action OnContinueUsed;
        /// <summary>A coin pickup was collected (arg: coins).</summary>
        public static Action<int> OnCoinCollected;
        /// <summary>Coins picked up in the current run changed (arg: run total, before the end-of-run bonus).</summary>
        public static Action<int> OnRunCoinsChanged;
        /// <summary>The fish's shield ability blocked a hit (arg: shield charges left).</summary>
        public static Action<int> OnShieldBlocked;
        /// <summary>The fish swam into a wave and got slowed down.</summary>
        public static Action OnWaveHit;
        /// <summary>The river reached its speed limit: a set of big waves is on its way.</summary>
        public static Action OnWaveSurge;
        /// <summary>A big wave rolled over the fish and took some river speed away.</summary>
        public static Action OnBigWaveHit;
        /// <summary>The player left the game over screen for the main menu.</summary>
        public static Action OnReturnToMenu;
    }
}
