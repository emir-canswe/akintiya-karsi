using UnityEngine;
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
        private SaveData saveData;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                saveData = SaveSystem.Load();
                if (gameConfig == null) gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void StartGame()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            CurrentDistance = 0f;
            CurrentState = GameState.Playing;
            if (saveData == null) saveData = SaveSystem.Load();
            saveData.totalGamesPlayed++;
            SaveSystem.Save(saveData);
            GameEvents.OnGameStart?.Invoke();
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Paused;
                Time.timeScale = 0f;
                GameEvents.OnGamePause?.Invoke();
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Playing;
                Time.timeScale = 1f;
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

            int score = Mathf.FloorToInt(CurrentDistance / Config.distanceToMetersDivisor);
            if (saveData == null) saveData = SaveSystem.Load();
            if (score > saveData.bestScore)
            {
                saveData.bestScore = score;
                SaveSystem.Save(saveData);
            }

            GameEvents.OnGameOver?.Invoke();
        }

        public void ContinueWithAd()
        {
            if (CurrentState == GameState.GameOver || CurrentState == GameState.Dying)
            {
                StopAllCoroutines();
                Time.timeScale = 1f;
                int hp = Config.continueHP;
                CurrentState = GameState.Playing;
                GameEvents.OnContinueUsed?.Invoke();
                GameEvents.OnPlayerHurt?.Invoke(hp);
            }
        }
    }
}
