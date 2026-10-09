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
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text bestScoreText;
        [SerializeField] private GameObject backdrop;
        private TMP_Text coinBalanceText;
        private FishPreview mascot;

        /// <summary>The equipped fish, swimming under the title; follows shop changes.</summary>
        public void SetMascot(FishPreview preview)
        {
            mascot = preview;
            if (mascot != null) mascot.SetFish(FishShop.Equipped);
        }

        private void HandleFishEquipped(Data.FishDef fish)
        {
            if (mascot != null) mascot.SetFish(fish);
        }

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
            FishShop.OnFishEquipped += HandleFishEquipped;
        }

        private void OnDestroy()
        {
            GameEvents.OnReturnToMenu -= HandleReturnToMenu;
            FishShop.OnBalanceChanged -= UpdateCoins;
            FishShop.OnFishEquipped -= HandleFishEquipped;
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
        public void Configure(TMP_Text title, Button play, TMP_Text best, GameObject menuBackdrop = null)
        {
            backdrop = menuBackdrop;
            if (backdrop != null) backdrop.SetActive(isActiveAndEnabled);
            titleText = title;
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
                if (titleText != null)
                {
                    // Title gently floats and wobbles like it's bobbing on the water.
                    float t = Time.unscaledTime;
                    titleText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f) * 2.5f);
                    float ts = 1f + 0.025f * Mathf.Sin(t * 2.1f);
                    titleText.rectTransform.localScale = new Vector3(ts, ts, 1f);
                }

                if (mascot != null)
                {
                    // Mascot bobs and drifts a little, like it's swimming in place.
                    float mt = Time.unscaledTime;
                    var rt = (RectTransform)mascot.transform;
                    rt.anchoredPosition = new Vector2(Mathf.Sin(mt * 0.9f) * 10f, Mathf.Sin(mt * 1.7f) * 5f);
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(mt * 0.9f + 1.2f) * 8f);
                }

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
