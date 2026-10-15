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

        private TMP_Text coinsEarnedText;
        private TMP_Text goalText;
        private RectTransform card;
        private float cardHeightWithContinue;
        private float cardHeightWithoutContinue;
        private float animTime = 1f;
        private int shownScore = -1; // count-up: last number put on screen
        private Button menuButton;
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

        /// <summary>Coins line ("+42 coins") and the button back to the main menu (shop lives there).</summary>
        public void SetCoinsAndMenu(TMP_Text coinsText, Button menuBtn)
        {
            coinsEarnedText = coinsText;
            menuButton = menuBtn;
            BindButtons();
        }

        /// <summary>"N more coins for the next fish" line: gives every run a goal.</summary>
        public void SetGoalText(TMP_Text label) => goalText = label;

        /// <summary>Card to pop in and resize (taller when the continue button is offered).</summary>
        public void SetCard(RectTransform cardRect, float heightWithContinue, float heightWithoutContinue)
        {
            card = cardRect;
            cardHeightWithContinue = heightWithContinue;
            cardHeightWithoutContinue = heightWithoutContinue;
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
            GameEvents.OnReturnToMenu += HandleReturnToMenu;
            LocalizationManager.OnLanguageChanged += RefreshTexts;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnContinueUsed -= HandleContinueUsed;
            GameEvents.OnReturnToMenu -= HandleReturnToMenu;
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
            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(OnMenuClicked);
                menuButton.onClick.AddListener(OnMenuClicked);
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
            if (card != null)
            {
                card.sizeDelta = new Vector2(card.sizeDelta.x, canContinue ? cardHeightWithContinue : cardHeightWithoutContinue);
            }
            animTime = 0f; // card pop + score count-up
            shownScore = -1;
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
            if (coinsEarnedText != null) coinsEarnedText.text = LocalizationManager.Format("coins_earned", gm != null ? gm.LastRunCoins : 0);
            if (goalText != null)
            {
                var save = SaveSystem.Load();
                var next = FishShop.NextGoal(save);
                if (next == null) goalText.text = LocalizationManager.Get("all_fish_owned");
                else if (save.coins >= next.price) goalText.text = LocalizationManager.Format("can_buy_fish", next.Name);
                else goalText.text = string.Format(LocalizationManager.Get("next_fish_goal"), next.Name, next.price - save.coins);
            }

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

        private void Update()
        {
            if (animTime >= 1.2f) return;
            animTime += Time.unscaledDeltaTime;

            // Card pops in with a little overshoot
            if (card != null)
            {
                float k = Mathf.Clamp01(animTime / 0.35f);
                float s = k < 1f ? Mathf.LerpUnclamped(0.7f, 1f, 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f)) : 1f;
                card.localScale = new Vector3(s, s, 1f);
            }

            // Distance counts up from 0
            if (scoreText != null)
            {
                float t = Mathf.Clamp01((animTime - 0.15f) / 0.7f);
                int shown = Mathf.RoundToInt(CurrentScore() * (1f - Mathf.Pow(1f - t, 3f)));
                if (shown != shownScore)
                {
                    // Only when the number changes, and formatted without building a new string each time.
                    shownScore = shown;
                    scoreText.SetText("{0:0}<size=45%>m</size>", shown);
                }
            }

            // NEW BEST sticker throbs
            if (newBestBadge != null && newBestBadge.activeSelf)
            {
                float b = 1f + 0.08f * Mathf.Sin(animTime * 12f) * Mathf.Clamp01(1.2f - animTime);
                newBestBadge.transform.localScale = new Vector3(b, b, 1f);
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
                    // Only if the player is still on this screen (not sent to the menu meanwhile).
                    if (GameManager.Instance.CurrentState == GameState.GameOver) GameManager.Instance.StartGame();
                });
            }
            else
            {
                GameManager.Instance.StartGame();
            }
        }

        private void OnMenuClicked()
        {
            if (waitingForAd || GameManager.Instance == null) return;
            GameManager.Instance.ReturnToMenu();
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
            if (menuButton != null) menuButton.interactable = interactable;
        }

        private void HandleGameStart()
        {
            hasContinued = false;
            waitingForAd = false;
            gameObject.SetActive(false);
        }

        private void HandleReturnToMenu()
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
