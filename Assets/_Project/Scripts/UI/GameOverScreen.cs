using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Upstream.Ads;
using Upstream.Core;
using Upstream.Mechanics;

namespace Upstream.UI
{
    /// <summary>
    /// Controls the game over overlay.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        private const int MinScoreForContinue = 15;

        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestScoreText;
        [SerializeField] private GameObject newBestBadge;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueLabel;
        [SerializeField] private TMP_Text unlockNoteText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private ScoreSystem scoreSystem;

        private IAdsService adsService;
        private bool hasContinued;
        private bool waitingForAd;

        public void Configure(
            TMP_Text score,
            TMP_Text best,
            GameObject badge,
            Button againBtn,
            Button contBtn,
            TMP_Text contLabel,
            TMP_Text unlockNote,
            CanvasGroup group,
            ScoreSystem scorer,
            IAdsService ads)
        {
            scoreText = score;
            bestScoreText = best;
            newBestBadge = badge;
            playAgainButton = againBtn;
            continueButton = contBtn;
            continueLabel = contLabel;
            unlockNoteText = unlockNote;
            canvasGroup = group;
            scoreSystem = scorer;
            adsService = ads;
            BindButtons();
        }

        // Subscribed in Awake/OnDestroy (not OnEnable/OnDisable): this screen starts hidden, and an
        // inactive object with OnEnable-based subscriptions would never receive OnGameOver,
        // leaving the player stuck on an empty screen after dying.
        private void Awake()
        {
            if (scoreSystem == null) scoreSystem = FindFirstObjectByType<ScoreSystem>();
            BindButtons();

            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnContinueUsed += HandleContinueUsed;
            LocalizationManager.OnLanguageChanged += RefreshTexts;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnContinueUsed -= HandleContinueUsed;
            LocalizationManager.OnLanguageChanged -= RefreshTexts;
        }

        private void BindButtons()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
                continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        public void ShowGameOver()
        {
            HandleGameOver();
        }

        private void HandleGameOver()
        {
            gameObject.SetActive(true);
            waitingForAd = false;
            SetButtonsInteractable(true);
            if (canvasGroup != null) StartCoroutine(FadeIn(0.28f));

            RefreshTexts();

            bool canContinue = !hasContinued && CurrentScore() >= MinScoreForContinue;
            if (continueButton != null) continueButton.gameObject.SetActive(canContinue);
        }

        private int CurrentScore()
        {
            var gm = GameManager.Instance;
            if (gm != null) return gm.LastScore;
            if (scoreSystem == null) scoreSystem = FindFirstObjectByType<ScoreSystem>();
            return scoreSystem != null ? scoreSystem.DisplayScore : 0;
        }

        private void RefreshTexts()
        {
            var gm = GameManager.Instance;
            int currentScore = CurrentScore();
            // GameManager already compared against and saved the best score.
            bool isNewBest = gm != null && gm.LastRunWasNewBest;
            int best = gm != null ? gm.BestScore : SaveSystem.Load().bestScore;
            int unlocked = gm != null ? gm.LastRunUnlockedStartStage : -1;

            if (scoreText != null) scoreText.text = $"{currentScore}<size=45%>m</size>";
            if (newBestBadge != null) newBestBadge.SetActive(isNewBest);
            if (bestScoreText != null) bestScoreText.text = LocalizationManager.Format("best_score", best);

            if (unlockNoteText != null)
            {
                unlockNoteText.gameObject.SetActive(unlocked > 0);
                if (unlocked > 0)
                {
                    unlockNoteText.text = LocalizationManager.Format("unlock_note", LocalizationManager.Get($"stage_name_{unlocked}"));
                }
            }

            if (continueLabel != null && !waitingForAd)
            {
                continueLabel.text = LocalizationManager.Get("watch_ad_continue");
            }
        }

        private IEnumerator FadeIn(float duration)
        {
            float elapsed = 0f;
            canvasGroup.alpha = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private void OnPlayAgainClicked()
        {
            if (waitingForAd) return;
            if (GameManager.Instance == null) return;

            if (AdsManager.Instance != null)
            {
                // Interstitial (if due by the frequency policy) plays first, then the new run starts.
                waitingForAd = true;
                SetButtonsInteractable(false);
                AdsManager.Instance.ShowInterstitialIfDueThen(() =>
                {
                    waitingForAd = false;
                    GameManager.Instance.StartGame();
                });
            }
            else
            {
                GameManager.Instance.StartGame();
            }
        }

        private void OnContinueClicked()
        {
            if (waitingForAd || hasContinued) return;

            if (adsService == null)
            {
                // No ad service wired (e.g. editor test scene): grant directly.
                GrantContinue();
                return;
            }

            if (!adsService.IsRewardedAdReady)
            {
                adsService.LoadRewardedAd();
                if (continueLabel != null) continueLabel.text = LocalizationManager.Get("ad_unavailable");
                return;
            }

            // The reward is granted only from the ad's completion callback.
            waitingForAd = true;
            SetButtonsInteractable(false);
            if (continueLabel != null) continueLabel.text = LocalizationManager.Get("watching_ad");
            adsService.ShowRewardedAd(GrantContinue, OnAdFailed);
        }

        private void GrantContinue()
        {
            waitingForAd = false;
            if (GameManager.Instance != null) GameManager.Instance.ContinueWithAd();
        }

        private void OnAdFailed()
        {
            waitingForAd = false;
            SetButtonsInteractable(true);
            if (continueLabel != null) continueLabel.text = LocalizationManager.Get("ad_unavailable");
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (playAgainButton != null) playAgainButton.interactable = interactable;
            if (continueButton != null) continueButton.interactable = interactable;
        }

        private void HandleGameStart()
        {
            hasContinued = false;
            waitingForAd = false;
            gameObject.SetActive(false);
        }

        private void HandleContinueUsed()
        {
            hasContinued = true;
            gameObject.SetActive(false);
        }
    }
}
