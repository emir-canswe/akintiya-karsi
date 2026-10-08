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

        private void Awake()
        {
            if (turboSystem == null)
            {
                turboSystem = FindFirstObjectByType<TurboSystem>();
            }
        }

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

        private void HandlePlayerHurt(int currentHp)
        {
            UpdateHearts(currentHp);
        }

        public void UpdateHearts(int hp)
        {
            if (heartIcons == null) return;
            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] != null)
                {
                    heartIcons[i].color = i < hp
                        ? new Color(1f, 0.36f, 0.42f, 1f)
                        : new Color(0f, 0.12f, 0.18f, 0.45f);
                }
            }
        }

        private void HandleTurboStart()
        {
            if (turboGlowEffect != null) turboGlowEffect.SetActive(true);
            if (toastUI != null) toastUI.Show("TURBO!", new Color(0.49f, 0.94f, 0.78f));
        }

        private void HandleTurboEnd()
        {
            if (turboGlowEffect != null) turboGlowEffect.SetActive(false);
        }

        private void HandleGrowthStageChanged(int stage)
        {
            if (stage > 0 && toastUI != null)
            {
                string[] names = { "YAVRU", "GENÇ!", "YETİŞKİN!", "ALFA!", "DEV BALIK!" };
                string label = stage < names.Length ? names[stage] : $"KADEME {stage + 1}!";
                toastUI.Show(label, new Color(1f, 0.84f, 0.35f));
            }
        }

        private void HandleFoodEaten(bool isGold)
        {
            if (isGold && toastUI != null)
            {
                toastUI.Show("+1 CAN", new Color(1f, 0.84f, 0.35f));
            }
        }

        private void HandleGameStart()
        {
            gameObject.SetActive(true);
            if (turboGlowEffect != null) turboGlowEffect.SetActive(false);
            UpdateHearts(3);
            if (powerBarFill != null) powerBarFill.fillAmount = 0f;
            if (distanceText != null) distanceText.text = "0<size=50%>m</size>";
        }

        private void HandleGameOver()
        {
            gameObject.SetActive(false);
        }
    }
}
