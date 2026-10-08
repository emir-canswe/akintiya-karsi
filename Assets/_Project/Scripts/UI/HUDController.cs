using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Upstream.Core;
using Upstream.Mechanics;

namespace Upstream.UI
{
    /// <summary>
    /// Controls the in-game heads-up display.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private Image[] heartIcons;
        [SerializeField] private Image powerBarFill;
        [SerializeField] private GameObject powerBarContainer;
        [SerializeField] private GameObject turboGlowEffect;
        [SerializeField] private ToastUI toastUI;

        private void OnEnable()
        {
            GameEvents.OnScoreChanged += HandleScoreChanged;
            GameEvents.OnPlayerHurt += HandlePlayerHurt;
            GameEvents.OnTurboStart += HandleTurboStart;
            GameEvents.OnTurboEnd += HandleTurboEnd;
            GameEvents.OnGrowthStageChanged += HandleGrowthStageChanged;
            GameEvents.OnFoodEaten += HandleFoodEaten;
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            GameEvents.OnScoreChanged -= HandleScoreChanged;
            GameEvents.OnPlayerHurt -= HandlePlayerHurt;
            GameEvents.OnTurboStart -= HandleTurboStart;
            GameEvents.OnTurboEnd -= HandleTurboEnd;
            GameEvents.OnGrowthStageChanged -= HandleGrowthStageChanged;
            GameEvents.OnFoodEaten -= HandleFoodEaten;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnGameOver -= HandleGameOver;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                powerBarFill.fillAmount = TurboSystem.IsTurboActive ? TurboSystem.TurboPercent : TurboSystem.PowerPercent;
            }
        }

        private void HandleScoreChanged(float rawDistance)
        {
            distanceText.text = $"{Mathf.FloorToInt(rawDistance / 10f)}m";
        }

        private void HandlePlayerHurt(int currentHp)
        {
            UpdateHearts(currentHp);
        }

        private void UpdateHearts(int hp)
        {
            for (int i = 0; i < heartIcons.Length; i++)
            {
                heartIcons[i].color = i < hp ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }

        private void HandleTurboStart()
        {
            if (turboGlowEffect != null) turboGlowEffect.SetActive(true);
            if (toastUI != null) toastUI.Show("TURBO!", Color.yellow);
        }

        private void HandleTurboEnd()
        {
            if (turboGlowEffect != null) turboGlowEffect.SetActive(false);
        }

        private void HandleGrowthStageChanged(int stage)
        {
            if (toastUI != null) toastUI.Show("STAGE UP!", Color.green);
        }

        private void HandleFoodEaten(bool isGold)
        {
            // Small power bar pulse animation could be added here
            if (isGold && toastUI != null)
            {
                toastUI.Show("+1 LIFE", Color.red);
            }
        }

        private void HandleGameStart()
        {
            gameObject.SetActive(true);
            if (turboGlowEffect != null) turboGlowEffect.SetActive(false);
            UpdateHearts(3);
            powerBarFill.fillAmount = 0;
            distanceText.text = "0m";
        }

        private void HandleGameOver()
        {
            gameObject.SetActive(false);
        }
    }
}
