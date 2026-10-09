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
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text bestScoreText;
        [SerializeField] private GameObject backdrop;
        private TMP_Text coinBalanceText;

        /// <summary>Coin balance shown on the menu (top-left).</summary>
        public void SetCoinBalance(TMP_Text label)
        {
            coinBalanceText = label;
            UpdateCoins(FishShop.Balance);
        }

        // Awake/OnDestroy: the menu is inactive during a run and must still hear "back to menu".
        private void Awake()
        {
            GameEvents.OnReturnToMenu += HandleReturnToMenu;
            FishShop.OnBalanceChanged += UpdateCoins;
        }

        private void OnDestroy()
        {
            GameEvents.OnReturnToMenu -= HandleReturnToMenu;
            FishShop.OnBalanceChanged -= UpdateCoins;
        }

        private void HandleReturnToMenu()
        {
            gameObject.SetActive(true);
            UpdateCoins(FishShop.Balance);
        }

        private void UpdateCoins(int balance)
        {
            if (coinBalanceText != null) coinBalanceText.text = balance.ToString();
        }

        /// <param name="menuBackdrop">Full-screen dim that lives outside the safe area; shown and hidden with the menu.</param>
        public void Configure(TMP_Text title, TMP_Text how, Button play, TMP_Text best, GameObject menuBackdrop = null)
        {
            backdrop = menuBackdrop;
            if (backdrop != null) backdrop.SetActive(isActiveAndEnabled);
            titleText = title;
            instructionText = how;
            if (playButton != null) playButton.onClick.RemoveListener(StartFromMenu);
            playButton = play;
            if (playButton != null) playButton.onClick.AddListener(StartFromMenu);
            bestScoreText = best;
            UpdateBestScore();
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
            if (backdrop != null) backdrop.SetActive(true);
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            LocalizationManager.OnLanguageChanged -= UpdateBestScore;
            if (backdrop != null) backdrop.SetActive(false);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Menu)
            {
                if (playButton != null)
                {
                    // Gentle "breathing" pulse so the Play button draws the eye.
                    float scale = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 3f);
                    playButton.transform.localScale = new Vector3(scale, scale, 1f);
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
