using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Upstream.Core;
using Upstream.Mechanics;

namespace Upstream.UI
{
    /// <summary>
    /// Controls the game over overlay.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestScoreText;
        [SerializeField] private GameObject newBestBadge;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueNoteText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private ScoreSystem scoreSystem;

        private bool hasContinued;

        public void Configure(
            TMP_Text score,
            TMP_Text best,
            GameObject badge,
            Button againBtn,
            Button contBtn,
            TMP_Text contNote,
            CanvasGroup group,
            ScoreSystem scorer)
        {
            scoreText = score;
            bestScoreText = best;
            newBestBadge = badge;
            playAgainButton = againBtn;
            continueButton = contBtn;
            continueNoteText = contNote;
            canvasGroup = group;
            scoreSystem = scorer;
            BindButtons();
        }

        private void Awake()
        {
            if (scoreSystem == null) scoreSystem = FindFirstObjectByType<ScoreSystem>();
            BindButtons();
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

        private void OnEnable()
        {
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnContinueUsed += HandleContinueUsed;
        }

        private void OnDisable()
        {
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnContinueUsed -= HandleContinueUsed;
        }

        public void ShowGameOver()
        {
            HandleGameOver();
        }

        private void HandleGameOver()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null) StartCoroutine(FadeIn(0.28f));

            if (scoreSystem == null) scoreSystem = FindFirstObjectByType<ScoreSystem>();
            int currentScore = scoreSystem != null
                ? scoreSystem.DisplayScore
                : (GameManager.Instance != null ? Mathf.FloorToInt(GameManager.Instance.CurrentDistance / 10f) : 0);

            if (scoreText != null) scoreText.text = $"{currentScore}<size=45%>m</size>";

            var saveData = SaveSystem.Load();
            bool isNewBest = currentScore > saveData.bestScore && currentScore > 0;
            if (isNewBest)
            {
                saveData.bestScore = currentScore;
                SaveSystem.Save(saveData);
            }
            if (newBestBadge != null) newBestBadge.SetActive(isNewBest);

            if (bestScoreText != null) bestScoreText.text = $"En iyi: {saveData.bestScore} m";

            bool canContinue = !hasContinued && currentScore >= 15;
            if (continueButton != null) continueButton.gameObject.SetActive(canContinue);
            if (continueNoteText != null) continueNoteText.gameObject.SetActive(canContinue);
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
            if (GameManager.Instance != null) GameManager.Instance.StartGame();
        }

        private void OnContinueClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.ContinueWithAd();
        }

        private void HandleGameStart()
        {
            hasContinued = false;
            gameObject.SetActive(false);
        }

        private void HandleContinueUsed()
        {
            hasContinued = true;
            gameObject.SetActive(false);
        }
    }
}
