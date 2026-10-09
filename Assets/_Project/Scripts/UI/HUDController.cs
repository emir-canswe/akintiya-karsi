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
        [SerializeField] private TurboSystem turboSystem;
        private HealthSystem healthSystem;
        private TMP_Text runCoinsText;

        /// <summary>Optional coin counter (coins picked up this run).</summary>
        public void SetCoinCounter(TMP_Text label)
        {
            runCoinsText = label;
            if (runCoinsText != null) runCoinsText.text = "0";
        }

        public void Configure(
            TMP_Text distText,
            Image[] hearts,
            Image powerFill,
            GameObject powerContainer,
            GameObject turboGlow,
            ToastUI toast,
            TurboSystem turbo)
        {
            distanceText = distText;
            heartIcons = hearts;
            powerBarFill = powerFill;
            powerBarContainer = powerContainer;
            turboGlowEffect = turboGlow;
            toastUI = toast;
            turboSystem = turbo;
        }

        // Subscribed in Awake/OnDestroy (not OnEnable/OnDisable) because this HUD hides itself with
        // SetActive(false). With OnEnable-based subscriptions it would never hear OnGameStart again
        // and would stay hidden forever.
        private void Awake()
        {
            if (turboSystem == null)
            {
                turboSystem = FindFirstObjectByType<TurboSystem>();
            }

            GameEvents.OnScoreChanged += HandleScoreChanged;
            GameEvents.OnHealthChanged += HandleHealthChanged;
            GameEvents.OnTurboStart += HandleTurboStart;
            GameEvents.OnTurboEnd += HandleTurboEnd;
            GameEvents.OnGrowthStageChanged += HandleGrowthStageChanged;
            GameEvents.OnGoldFoodResolved += HandleGoldFoodResolved;
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnContinueUsed += HandleContinueUsed;
            GameEvents.OnRunCoinsChanged += HandleRunCoinsChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnScoreChanged -= HandleScoreChanged;
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            GameEvents.OnTurboStart -= HandleTurboStart;
            GameEvents.OnTurboEnd -= HandleTurboEnd;
            GameEvents.OnGrowthStageChanged -= HandleGrowthStageChanged;
            GameEvents.OnGoldFoodResolved -= HandleGoldFoodResolved;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnContinueUsed -= HandleContinueUsed;
            GameEvents.OnRunCoinsChanged -= HandleRunCoinsChanged;
        }

        private void HandleRunCoinsChanged(int coins)
        {
            if (runCoinsText != null) runCoinsText.text = coins.ToString();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                if (turboSystem == null) turboSystem = FindFirstObjectByType<TurboSystem>();
                if (powerBarFill != null && turboSystem != null)
                {
                    powerBarFill.fillAmount = turboSystem.IsTurboActive ? turboSystem.TurboPercent : turboSystem.PowerPercent;
                }
            }
        }

        private void HandleScoreChanged(float metersScore)
        {
            if (distanceText != null)
            {
                distanceText.text = $"{Mathf.FloorToInt(metersScore)}<size=50%>m</size>";
            }
        }

        private void HandleHealthChanged(int currentHp)
        {
            UpdateHearts(currentHp);
        }

        public void UpdateHearts(int hp)
        {
            if (heartIcons == null) return;
            if (healthSystem == null) healthSystem = FindFirstObjectByType<HealthSystem>();
            int maxHp = healthSystem != null ? healthSystem.MaxHP : 3;
            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] != null)
                {
                    // Extra heart slots (legendary fish) only show when that fish is worn.
                    heartIcons[i].gameObject.SetActive(i < maxHp);
                    heartIcons[i].color = i < hp
                        ? new Color(1f, 0.36f, 0.42f, 1f)
                        : new Color(0f, 0.12f, 0.18f, 0.45f);
                }
            }
        }

        private void HandleTurboStart()
        {
            if (turboGlowEffect != null) turboGlowEffect.SetActive(true);
            if (toastUI != null) toastUI.Show(LocalizationManager.Get("turbo_toast"), new Color(0.49f, 0.94f, 0.78f));
        }

        private void HandleTurboEnd()
        {
            if (turboGlowEffect != null) turboGlowEffect.SetActive(false);
        }

        private void HandleGrowthStageChanged(int stage)
        {
            if (stage > 0 && toastUI != null)
            {
                toastUI.Show(LocalizationManager.Get($"stage_up_{Mathf.Min(stage, 4)}"), new Color(1f, 0.84f, 0.35f));
            }
        }

        private void HandleGoldFoodResolved(bool healed)
        {
            if (toastUI == null) return;
            if (healed) toastUI.Show(LocalizationManager.Get("heal_toast"), new Color(1f, 0.84f, 0.35f));
            else toastUI.Show(LocalizationManager.Get("power_toast"), new Color(0.49f, 0.94f, 0.78f));
        }

        private void HandleGameStart()
        {
            gameObject.SetActive(true);
            if (turboGlowEffect != null) turboGlowEffect.SetActive(false);
            // Hearts are refreshed by HealthSystem.ResetHealth -> OnHealthChanged.
            if (powerBarFill != null) powerBarFill.fillAmount = 0f;
            if (distanceText != null) distanceText.text = "0<size=50%>m</size>";
            if (runCoinsText != null) runCoinsText.text = "0";
        }

        private void HandleGameOver()
        {
            gameObject.SetActive(false);
        }

        private void HandleContinueUsed()
        {
            gameObject.SetActive(true);
        }
    }
}
