using System;
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

        private bool hasContinued;

        private void Awake()
        {
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
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

        private void HandleGameOver()
        {
            gameObject.SetActive(true);
            StartCoroutine(FadeIn(0.3f));

            int currentScore = ScoreSystem.DisplayScore;
            scoreText.text = $"{currentScore}m";

            var saveData = SaveSystem.Load();
            if (currentScore > saveData.BestScore)
            {
                saveData.BestScore = currentScore;
                SaveSystem.Save(saveData);
                if (newBestBadge != null) newBestBadge.SetActive(true);
            }
            else
            {
                if (newBestBadge != null) newBestBadge.SetActive(false);
            }

            bestScoreText.text = $"BEST: {saveData.BestScore}m";

            bool canContinue = !hasContinued && currentScore >= 30;
            continueButton.gameObject.SetActive(canContinue);
            if (continueNoteText != null) continueNoteText.gameObject.SetActive(canContinue);
        }

        private IEnumerator FadeIn(float duration)
        {
            float elapsed = 0f;
            canvasGroup.alpha = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private void OnPlayAgainClicked()
        {
            GameManager.Instance.StartGame();
        }

        private void OnContinueClicked()
        {
            GameManager.Instance.ContinueWithAd();
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
