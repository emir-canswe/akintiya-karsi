using UnityEngine;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.Mechanics
{
    /// <summary>
    /// Manages player health, damage taking, and invincibility states.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        
        private int currentHP;
        private float invincibilityTimer;
        private TurboSystem turboSystem;

        /// <summary>
        /// Current health points.
        /// </summary>
        public int CurrentHP => currentHP;

        /// <summary>
        /// Whether the player is currently invincible.
        /// </summary>
        public bool IsInvincible => invincibilityTimer > 0f;

        /// <summary>
        /// Maximum health points available.
        /// </summary>
        public int MaxHP => config.maxHP;

        private void Awake()
        {
            turboSystem = GetComponent<TurboSystem>();
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += ResetHealth;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= ResetHealth;
        }

        private void Update()
        {
            if (invincibilityTimer > 0f)
            {
                invincibilityTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Resets health to maximum and clears invincibility.
        /// </summary>
        public void ResetHealth()
        {
            currentHP = config.maxHP;
            invincibilityTimer = 0f;
        }

        /// <summary>
        /// Attempts to apply damage to the player.
        /// </summary>
        /// <returns>True if damage was successfully taken, false if invincible or in turbo mode.</returns>
        public bool TakeDamage()
        {
            if (IsInvincible) return false;
            if (turboSystem != null && turboSystem.IsTurboActive) return false;

            currentHP--;
            GameEvents.OnPlayerHurt?.Invoke(currentHP);

            if (currentHP <= 0)
            {
                GameEvents.OnPlayerDied?.Invoke();
            }
            else
            {
                SetInvincible(config.invincibilityDuration);
            }

            return true;
        }

        /// <summary>
        /// Restores health by the specified amount, up to the maximum.
        /// </summary>
        /// <param name="amount">Amount to heal.</param>
        public void Heal(int amount)
        {
            if (currentHP >= config.maxHP) return;
            
            currentHP = Mathf.Min(currentHP + amount, config.maxHP);
        }

        /// <summary>
        /// Sets the invincibility timer to the given duration if it is longer than the current remaining time.
        /// </summary>
        /// <param name="duration">Duration of invincibility in seconds.</param>
        public void SetInvincible(float duration)
        {
            if (duration > invincibilityTimer)
            {
                invincibilityTimer = duration;
            }
        }
    }
}
