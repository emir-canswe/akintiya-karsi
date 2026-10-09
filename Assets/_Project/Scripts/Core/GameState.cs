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
        /// <summary>Fired after a gold food is resolved: true = healed a heart, false = HP was full so it became power.</summary>
        public static Action<bool> OnGoldFoodResolved;
        public static Action OnPlayerDied;
        public static Action OnTurboStart;
        public static Action OnTurboEnd;
        public static Action<int> OnGrowthStageChanged;
        public static Action<bool> OnFoodEaten;
        public static Action OnSharkEaten;
        public static Action OnContinueUsed;
    }
}
