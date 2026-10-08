using UnityEngine;
using TMPro;
using Upstream.Core;

namespace Upstream.UI
{
    /// <summary>
    /// Controls the main menu screen.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text tapToStartText;
        [SerializeField] private TMP_Text bestScoreText;

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnGameOver += HandleGameOver;
            UpdateBestScore();
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnGameOver -= HandleGameOver;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Menu)
            {
                // Pulsing tap to start text
                if (tapToStartText != null)
                {
                    Color c = tapToStartText.color;
                    c.a = 0.5f + Mathf.PingPong(Time.time * 2f, 0.5f);
                    tapToStartText.color = c;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    // Check if not clicking UI buttons (implementation depends on EventSystem, omitted for brevity)
                    GameManager.Instance.StartGame();
                }
            }
        }

        private void UpdateBestScore()
        {
            var saveData = SaveSystem.Load();
            if (bestScoreText != null)
            {
                bestScoreText.text = $"BEST: {saveData.BestScore}m";
            }
        }

        private void HandleGameStart()
        {
            gameObject.SetActive(false);
        }

        private void HandleGameOver()
        {
            UpdateBestScore();
            gameObject.SetActive(true);
        }
    }
}
