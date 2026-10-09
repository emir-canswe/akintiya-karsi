using UnityEngine;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.Mechanics
{
    /// <summary>
    /// Manages the turbo power bar and turbo state.
    /// </summary>
    [RequireComponent(typeof(GrowthSystem))]
    public class TurboSystem : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GrowthConfig growthConfig;

        private float currentPower;
        private float turboTimer;
        private GrowthSystem growthSystem;

        public GameConfig Config
        {
            get
            {
                if (config == null)
                {
                    config = GameManager.Instance != null
                        ? GameManager.Instance.Config
                        : ScriptableObject.CreateInstance<GameConfig>();
                }
                return config;
            }
            set => config = value;
        }

        public float PowerPercent => Mathf.Clamp01(currentPower / Config.maxPower);
        public float TurboTimeRemaining => turboTimer;
        public bool IsTurboActive => turboTimer > 0f;
        public float TurboPercent => IsTurboActive ? Mathf.Clamp01(turboTimer / Config.turboTime) : 0f;

        private void Awake()
        {
            growthSystem = GetComponent<GrowthSystem>();
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += ResetTurbo;
            GameEvents.OnReturnToMenu += ResetTurbo;
            GameEvents.OnFoodEaten += HandleFoodEaten;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= ResetTurbo;
            GameEvents.OnReturnToMenu -= ResetTurbo;
            GameEvents.OnFoodEaten -= HandleFoodEaten;
        }

        private void HandleFoodEaten(bool isGold)
        {
            if (!isGold)
            {
                AddPower(Config.powerPerFood);
            }
        }

        private void Update()
        {
            if (IsTurboActive)
            {
                turboTimer -= Time.deltaTime;
                if (turboTimer <= 0f)
                {
                    turboTimer = 0f;
                    GameEvents.OnTurboEnd?.Invoke();

                    if (TryGetComponent<HealthSystem>(out var health))
                    {
                        health.SetInvincible(0.8f);
                    }
                }
            }
        }

        public void AddPower(float amount)
        {
            if (IsTurboActive) return;

            float multiplier = (growthSystem != null && growthSystem.CurrentStageData != null)
                ? growthSystem.CurrentStageData.powerBarFillMultiplier
                : 1f;
            currentPower += amount * multiplier;

            if (currentPower >= Config.maxPower)
            {
                ActivateTurbo();
            }
        }

        public void ActivateTurbo()
        {
            currentPower = 0f;
            turboTimer = Config.turboTime;
            GameEvents.OnTurboStart?.Invoke();
        }

        public void ResetTurbo()
        {
            bool wasActive = IsTurboActive;
            currentPower = 0f;
            turboTimer = 0f;
            // Quitting mid-turbo must stop the turbo sound/glow too.
            if (wasActive) GameEvents.OnTurboEnd?.Invoke();
        }
    }
}
