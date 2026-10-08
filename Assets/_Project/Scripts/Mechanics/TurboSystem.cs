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

        /// <summary>
        /// Current power percentage from 0 to 1.
        /// </summary>
        public float PowerPercent => Mathf.Clamp01(currentPower / config.maxPower);

        /// <summary>
        /// Remaining time for active turbo mode.
        /// </summary>
        public float TurboTimeRemaining => turboTimer;

        /// <summary>
        /// Whether turbo mode is currently active.
        /// </summary>
        public bool IsTurboActive => turboTimer > 0f;

        /// <summary>
        /// Turbo active percentage from 0 to 1 (remaining / total).
        /// </summary>
        public float TurboPercent => IsTurboActive ? Mathf.Clamp01(turboTimer / config.turboTime) : 0f;

        private void Awake()
        {
            growthSystem = GetComponent<GrowthSystem>();
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += ResetTurbo;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= ResetTurbo;
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
                    
                    // Give brief invincibility upon exiting turbo
                    if (TryGetComponent<HealthSystem>(out var health))
                    {
                        health.SetInvincible(1f); 
                    }
                }
            }
        }

        /// <summary>
        /// Adds power to the turbo bar, scaled by the current growth stage multiplier.
        /// Activates turbo if it reaches max capacity.
        /// </summary>
        /// <param name="amount">Base power amount to add.</param>
        public void AddPower(float amount)
        {
            if (IsTurboActive) return;

            float multiplier = growthSystem.CurrentStageData?.powerBarFillMultiplier ?? 1f;
            currentPower += amount * multiplier;

            if (currentPower >= config.maxPower)
            {
                ActivateTurbo();
            }
        }

        /// <summary>
        /// Manually activates turbo mode, resetting power and starting the timer.
        /// </summary>
        public void ActivateTurbo()
        {
            currentPower = 0f;
            turboTimer = config.turboTime;
            GameEvents.OnTurboStart?.Invoke();
        }

        /// <summary>
        /// Resets the turbo state completely.
        /// </summary>
        public void ResetTurbo()
        {
            currentPower = 0f;
            turboTimer = 0f;
        }
    }
}
