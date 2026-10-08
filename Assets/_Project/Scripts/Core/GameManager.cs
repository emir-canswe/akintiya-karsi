using UnityEngine;
using System.Collections;
using Upstream.Data;

namespace Upstream.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }

        [SerializeField] private GameConfig gameConfig;

        public float CurrentDistance { get; private set; }
        private SaveData saveData;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                saveData = SaveSystem.Load();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (CurrentState == GameState.Playing && gameConfig != null)
            {
                CurrentDistance += Time.deltaTime * gameConfig.startSpeed; // Basic calculation
                GameEvents.OnScoreChanged?.Invoke(CurrentDistance / gameConfig.distanceToMetersDivisor);
            }
        }

        public void StartGame()
        {
            CurrentDistance = 0f;
            CurrentState = GameState.Playing;
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
            
            float animDuration = gameConfig != null ? gameConfig.deathAnimDuration : 1f;
            yield return new WaitForSeconds(animDuration);
            
            CurrentState = GameState.GameOver;
            
            if (gameConfig != null)
            {
                int score = Mathf.FloorToInt(CurrentDistance / gameConfig.distanceToMetersDivisor);
                if (score > saveData.bestScore)
                {
                    saveData.bestScore = score;
                    SaveSystem.Save(saveData);
                }
            }
            
            GameEvents.OnGameOver?.Invoke();
        }

        public void ContinueWithAd()
        {
            if (CurrentState == GameState.GameOver || CurrentState == GameState.Dying)
            {
                int hp = gameConfig != null ? gameConfig.continueHP : 2;
                GameEvents.OnPlayerHurt?.Invoke(hp);
                GameEvents.OnContinueUsed?.Invoke();
                CurrentState = GameState.Playing;
            }
        }
    }
}
