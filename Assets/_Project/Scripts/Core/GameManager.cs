using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Upstream.Data;

namespace Upstream.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Menu;

        [SerializeField] private GameConfig gameConfig;

        public GameConfig Config
        {
            get
            {
                if (gameConfig == null) gameConfig = ScriptableObject.CreateInstance<GameConfig>();
                return gameConfig;
            }
            set => gameConfig = value;
        }

        public float CurrentDistance { get; set; }

        /// <summary>Score (meters) of the run that just ended. Valid once OnGameOver fires.</summary>
        public int LastScore { get; private set; }
        /// <summary>True if the run that just ended beat the previous best score.</summary>
        public bool LastRunWasNewBest { get; private set; }
        public int BestScore { get; private set; }
        /// <summary>Stage index a new run now starts at, if the run that just ended unlocked it; otherwise -1.</summary>
        public int LastRunUnlockedStartStage { get; private set; } = -1;
        /// <summary>Coins the run that just ended paid into the wallet (pickups + distance, with the fish bonus).</summary>
        public int LastRunCoins { get; private set; }

        /// <summary>Needed for the start-stage unlock rule; assigned by the bootstrap.</summary>
        public GrowthConfig GrowthConfig { get; set; }

        private int sharksEatenThisRun;
        private int maxStageThisRun;
        private int coinsPickedThisRun;
        // Meters already paid out as coins; after a continue only the new distance pays again.
        private int metersPaid;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                BestScore = SaveSystem.Load().bestScore;
                if (gameConfig == null) gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnSharkEaten += HandleSharkEaten;
            GameEvents.OnGrowthStageChanged += HandleStageChanged;
            GameEvents.OnCoinCollected += HandleCoinCollected;
        }

        private void OnDisable()
        {
            GameEvents.OnSharkEaten -= HandleSharkEaten;
            GameEvents.OnGrowthStageChanged -= HandleStageChanged;
            GameEvents.OnCoinCollected -= HandleCoinCollected;
        }

        private void HandleSharkEaten()
        {
            sharksEatenThisRun++;
            HandleCoinCollected(FishShop.SharkCoins);
        }

        private void HandleCoinCollected(int amount)
        {
            if (CurrentState != GameState.Playing) return;
            coinsPickedThisRun += amount;
            GameEvents.OnRunCoinsChanged?.Invoke(coinsPickedThisRun);
        }

        private void HandleStageChanged(int stage) => maxStageThisRun = Mathf.Max(maxStageThisRun, stage);

        private void Update()
        {
            // Android back button arrives as Escape in the Input System.
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (UIBackStack.TryPop()) return; // close the open shop/settings panel first

            switch (CurrentState)
            {
                case GameState.Playing: PauseGame(); break;
                case GameState.Paused: ResumeGame(); break;
                case GameState.GameOver: ReturnToMenu(); break;
                case GameState.Menu: Application.Quit(); break;
            }
        }

        // Leaving the app (home button, call, notification shade) must not keep the run going in the
        // background: the player would come back to a dead fish.
        private void OnApplicationPause(bool paused)
        {
            if (paused) PauseGame();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
#if !UNITY_EDITOR
            if (!hasFocus) PauseGame();
#endif
        }

        public void StartGame()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            CurrentDistance = 0f;
            sharksEatenThisRun = 0;
            maxStageThisRun = 0; // GrowthSystem re-raises the start stage on OnGameStart
            LastRunUnlockedStartStage = -1;
            coinsPickedThisRun = 0;
            metersPaid = 0;
            LastRunCoins = 0;
            CurrentState = GameState.Playing;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Always load-modify-save: other systems (sound toggle etc.) write the same file,
            // so a cached copy here would overwrite their changes.
            var save = SaveSystem.Load();
            save.totalGamesPlayed++;
            SaveSystem.Save(save);

            GameEvents.OnGameStart?.Invoke();
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Paused;
                Time.timeScale = 0f;
                AudioListener.pause = true;
                GameEvents.OnGamePause?.Invoke();
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Playing;
                Time.timeScale = 1f;
                AudioListener.pause = false;
                GameEvents.OnGameResume?.Invoke();
            }
        }

        public void GameOver()
        {
            if (CurrentState == GameState.Playing)
            {
                StartCoroutine(GameOverRoutine());
            }
        }

        private IEnumerator GameOverRoutine()
        {
            CurrentState = GameState.Dying;
            GameEvents.OnPlayerDied?.Invoke();

            float animDuration = Config.deathAnimDuration;
            yield return new WaitForSecondsRealtime(animDuration);

            CurrentState = GameState.GameOver;
            RecordRunResults();
            GameEvents.OnGameOver?.Invoke();
        }

        /// <summary>
        /// Saves what the run earned: best score, sharks, start-stage unlocks and coins. Used when the
        /// fish dies and when the player quits a run from the pause menu (nothing earned is lost).
        /// </summary>
        private void RecordRunResults()
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            int score = Config.distanceToMetersDivisor > 0f
                ? Mathf.FloorToInt(CurrentDistance / Config.distanceToMetersDivisor)
                : 0;

            var save = SaveSystem.Load();

            // Single source of truth for the best score. UI reads LastRunWasNewBest instead of
            // re-comparing against the save file (which would already contain this score).
            LastScore = score;
            LastRunWasNewBest = score > 0 && score > save.bestScore;
            if (LastRunWasNewBest) save.bestScore = score;
            BestScore = save.bestScore;

            save.totalSharksEaten += sharksEatenThisRun;
            sharksEatenThisRun = 0;

            // Persistent progression: remember the best stage and report a newly unlocked start stage.
            LastRunUnlockedStartStage = -1;
            if (maxStageThisRun > save.bestStageReached)
            {
                var growth = GrowthConfig != null ? GrowthConfig : ScriptableObject.CreateInstance<GrowthConfig>();
                int oldStart = growth.GetStartStage(save.bestStageReached);
                int newStart = growth.GetStartStage(maxStageThisRun);
                save.bestStageReached = maxStageThisRun;
                save.unlockedStartStage = newStart;
                if (newStart > oldStart) LastRunUnlockedStartStage = newStart;
            }

            // Coins: pickups + distance not yet paid, boosted by the equipped fish.
            float bonus = FishCatalog.Get(save.equippedFish).coinBonus;
            LastRunCoins = FishShop.RunReward(coinsPickedThisRun, score - metersPaid, bonus);
            FishShop.Deposit(save, LastRunCoins);
            coinsPickedThisRun = 0;
            metersPaid = score;
            SaveSystem.Save(save);
            FishShop.RaiseBalanceChanged(save.coins);
        }

        /// <summary>Pause menu → Main Menu: ends the run, keeps its coins and best score.</summary>
        public void QuitRunToMenu()
        {
            if (CurrentState != GameState.Paused) return;
            RecordRunResults();
            CurrentState = GameState.GameOver; // ReturnToMenu leaves from the end-of-run state
            ReturnToMenu();
        }

        /// <summary>Leaves the game over screen for the main menu (to visit the shop, settings...).</summary>
        public void ReturnToMenu()
        {
            if (CurrentState != GameState.GameOver) return;

            StopAllCoroutines();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            CurrentState = GameState.Menu;
            CurrentDistance = 0f;
            GameEvents.OnReturnToMenu?.Invoke();
        }

        /// <summary>
        /// Resumes the run after a rewarded ad. Call ONLY from the ad's reward callback.
        /// </summary>
        public void ContinueWithAd()
        {
            if (CurrentState != GameState.GameOver) return;

            StopAllCoroutines();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            CurrentState = GameState.Playing;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            // HealthSystem restores HP and raises OnHealthChanged itself.
            GameEvents.OnContinueUsed?.Invoke();
        }
    }
}
