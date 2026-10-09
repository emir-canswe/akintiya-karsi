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
        [SerializeField] private Image heartIcon;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image healthGhost;
        [SerializeField] private Image powerBarFill;
        [SerializeField] private GameObject powerBarContainer;
        [SerializeField] private GameObject turboGlowEffect;
        [SerializeField] private ToastUI toastUI;
        [SerializeField] private TurboSystem turboSystem;
        private HealthSystem healthSystem;
        private TMP_Text runCoinsText;
        private RectTransform coinPill;
        private int shownCoins;
        private float coinPopTime = 1f;

        // Health bar animation: the fill drops at once on a hit, a pale "ghost" shows the chunk that was
        // lost and drains after a short hold; heals fill up smoothly.
        private const float GhostHold = 0.35f;
        private const float GhostDrainPerSecond = 0.7f;
        private const float HealFillPerSecond = 0.6f;
        private const float LowHealth = 0.5f; // one more hit ends the run
        private static readonly Color HealthColor = new Color(1f, 0.36f, 0.42f, 1f);
        private static readonly Color LowHealthColor = new Color(1f, 0.16f, 0.2f, 1f);
        private float healthTarget = 1f;
        private float healthShown = 1f;
        private float ghostShown = 1f;
        private float ghostHoldTimer;
        private float heartPopTime = 1f;

        /// <summary>Optional coin counter (coins picked up this run); <paramref name="pill"/> bounces on every pickup.</summary>
        public void SetCoinCounter(TMP_Text label, RectTransform pill = null)
        {
            runCoinsText = label;
            coinPill = pill;
            shownCoins = 0;
            if (runCoinsText != null) runCoinsText.text = "0";
        }

        public void Configure(
            TMP_Text distText,
            Image heart,
            Image healthBarFill,
            Image healthBarGhost,
            Image powerFill,
            GameObject powerContainer,
            GameObject turboGlow,
            ToastUI toast,
            TurboSystem turbo)
        {
            distanceText = distText;
            heartIcon = heart;
            healthFill = healthBarFill;
            healthGhost = healthBarGhost;
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
            GameEvents.OnWaveSurge += HandleWaveSurge;
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnGameOver += HandleGameOver;
            GameEvents.OnContinueUsed += HandleContinueUsed;
            GameEvents.OnRunCoinsChanged += HandleRunCoinsChanged;
            GameEvents.OnReturnToMenu += HandleGameOver;
        }

        private void OnDestroy()
        {
            GameEvents.OnScoreChanged -= HandleScoreChanged;
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            GameEvents.OnTurboStart -= HandleTurboStart;
            GameEvents.OnTurboEnd -= HandleTurboEnd;
            GameEvents.OnGrowthStageChanged -= HandleGrowthStageChanged;
            GameEvents.OnGoldFoodResolved -= HandleGoldFoodResolved;
            GameEvents.OnWaveSurge -= HandleWaveSurge;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnGameOver -= HandleGameOver;
            GameEvents.OnContinueUsed -= HandleContinueUsed;
            GameEvents.OnRunCoinsChanged -= HandleRunCoinsChanged;
            GameEvents.OnReturnToMenu -= HandleGameOver;
        }

        private void HandleRunCoinsChanged(int coins)
        {
            if (runCoinsText != null) runCoinsText.SetText("{0:0}", coins);
            if (coins > shownCoins) coinPopTime = 0f;
            shownCoins = coins;
        }

        private void Update()
        {
            if (coinPill != null && coinPopTime < 1f)
            {
                // Quick bounce: unscaled so it isn't stretched by the shark-eat slow motion.
                coinPopTime = Mathf.Min(1f, coinPopTime + Time.unscaledDeltaTime * 5f);
                float s = 1f + Mathf.Sin(coinPopTime * Mathf.PI) * 0.08f; // small: must not touch the power bar
                coinPill.localScale = new Vector3(s, s, 1f);
            }

            // Only touch the bar while something moves, so an idle HUD doesn't rebuild its canvas every frame.
            bool lowHealth = healthTarget > 0f && healthTarget <= LowHealth;
            if (healthShown != healthTarget || ghostShown != healthShown || heartPopTime < 1f || lowHealth)
            {
                AnimateHealthBar(Time.unscaledDeltaTime);
            }

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
                // SetText with an argument formats without allocating a new string (this runs every meter).
                distanceText.SetText("{0:0}<size=50%>m</size>", Mathf.FloorToInt(metersScore));
            }
        }

        private void HandleHealthChanged(int currentHp)
        {
            if (healthSystem == null) healthSystem = FindFirstObjectByType<HealthSystem>();
            int maxHp = healthSystem != null ? healthSystem.MaxHP : 100;
            float target = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
            if (target < healthTarget)
            {
                // Hit: the bar drops right away, the ghost marks how much it cost.
                ghostShown = Mathf.Max(ghostShown, healthShown);
                healthShown = target;
                ghostHoldTimer = GhostHold;
                heartPopTime = 0f;
            }
            healthTarget = target;
            ApplyHealthBar();
        }

        private void AnimateHealthBar(float dt)
        {
            if (healthShown < healthTarget) healthShown = Mathf.MoveTowards(healthShown, healthTarget, HealFillPerSecond * dt);
            if (ghostHoldTimer > 0f) ghostHoldTimer -= dt;
            else if (ghostShown > healthShown) ghostShown = Mathf.MoveTowards(ghostShown, healthShown, GhostDrainPerSecond * dt);
            if (ghostShown < healthShown) ghostShown = healthShown;
            if (heartPopTime < 1f) heartPopTime = Mathf.Min(1f, heartPopTime + dt * 4f);
            ApplyHealthBar();
        }

        private void ApplyHealthBar()
        {
            bool low = healthTarget > 0f && healthTarget <= LowHealth;
            // Low health: the bar and heart throb so "one more hit and you're out" is obvious at a glance.
            float beat = low ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f) : 0f;
            if (healthFill != null)
            {
                healthFill.fillAmount = healthShown;
                healthFill.color = low ? Color.Lerp(HealthColor, LowHealthColor, beat) : HealthColor;
            }
            if (healthGhost != null) healthGhost.fillAmount = ghostShown;
            if (heartIcon != null)
            {
                float s = 1f + Mathf.Sin(heartPopTime * Mathf.PI) * 0.3f + beat * 0.12f;
                heartIcon.rectTransform.localScale = new Vector3(s, s, 1f);
                heartIcon.color = healthTarget > 0f ? HealthColor : new Color(0f, 0.12f, 0.18f, 0.45f);
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

        private void HandleGoldFoodResolved(int healed)
        {
            if (toastUI == null) return;
            if (healed > 0) toastUI.Show(LocalizationManager.Format("heal_toast", healed), new Color(1f, 0.84f, 0.35f));
            else toastUI.Show(LocalizationManager.Get("power_toast"), new Color(0.49f, 0.94f, 0.78f));
        }

        private void HandleWaveSurge()
        {
            if (toastUI != null) toastUI.Show(LocalizationManager.Get("surge_toast"), new Color(0.45f, 0.85f, 1f));
        }

        private void HandleGameStart()
        {
            gameObject.SetActive(true);
            if (turboGlowEffect != null) turboGlowEffect.SetActive(false);
            // A new run starts with a full bar (HealthSystem.ResetHealth also raises OnHealthChanged).
            healthTarget = healthShown = ghostShown = 1f;
            ghostHoldTimer = 0f;
            heartPopTime = 1f;
            ApplyHealthBar();
            if (powerBarFill != null) powerBarFill.fillAmount = 0f;
            if (distanceText != null) distanceText.text = "0<size=50%>m</size>";
            if (runCoinsText != null) runCoinsText.text = "0";
            shownCoins = 0;
            coinPopTime = 1f;
            if (coinPill != null) coinPill.localScale = Vector3.one;
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
