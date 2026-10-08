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
        public static Action<int> OnPlayerHurt;
        public static Action OnPlayerDied;
        public static Action OnTurboStart;
        public static Action OnTurboEnd;
        public static Action<int> OnGrowthStageChanged;
        public static Action<bool> OnFoodEaten;
        public static Action OnSharkEaten;
        public static Action OnContinueUsed;
    }
}
