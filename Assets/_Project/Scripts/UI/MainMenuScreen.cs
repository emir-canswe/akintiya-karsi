using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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

        public void Configure(TMP_Text title, TMP_Text how, TMP_Text tap, TMP_Text best)
        {
            titleText = title;
            instructionText = how;
            tapToStartText = tap;
            bestScoreText = best;
            UpdateBestScore();
        }

        private void Awake()
        {
            // "Tap anywhere" is a full-screen UI Button on this object, so taps on other UI
            // (e.g. the sound toggle drawn above it) are consumed by that UI and don't start a run.
            var tapArea = GetComponent<Button>();
            if (tapArea != null) tapArea.onClick.AddListener(StartFromMenu);
        }

        private void StartFromMenu()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Menu)
            {
                GameManager.Instance.StartGame();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            LocalizationManager.OnLanguageChanged += UpdateBestScore;
            UpdateBestScore();
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            LocalizationManager.OnLanguageChanged -= UpdateBestScore;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Menu)
            {
                if (tapToStartText != null)
                {
                    Color c = tapToStartText.color;
                    c.a = 0.45f + Mathf.PingPong(Time.unscaledTime * 1.4f, 0.55f);
                    tapToStartText.color = c;
                }

                if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    StartFromMenu();
                }
            }
        }

        public void UpdateBestScore()
        {
            var saveData = SaveSystem.Load();
            if (bestScoreText != null)
            {
                string text = saveData.bestScore > 0 ? LocalizationManager.Format("best_score", saveData.bestScore) : "";
                if (saveData.unlockedStartStage > 0)
                {
                    string stageName = LocalizationManager.Get($"stage_name_{Mathf.Min(saveData.unlockedStartStage, 4)}");
                    text += (text.Length > 0 ? "\n" : "") + "<size=80%>" + LocalizationManager.Format("start_stage_unlocked", stageName) + "</size>";
                }
                bestScoreText.text = text;
            }
        }

        private void HandleGameStart()
        {
            gameObject.SetActive(false);
        }
    }
}
