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

        private int currentHP = 3;
        private float invincibilityTimer;
        private TurboSystem turboSystem;

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

        public int CurrentHP => currentHP;
        public bool IsInvincible => invincibilityTimer > 0f;
        public int MaxHP => Config.maxHP;

        private void Awake()
        {
            turboSystem = GetComponent<TurboSystem>();
            currentHP = Config.maxHP;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += ResetHealth;
            GameEvents.OnFoodEaten += HandleFoodEaten;
            GameEvents.OnContinueUsed += HandleContinueUsed;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= ResetHealth;
            GameEvents.OnFoodEaten -= HandleFoodEaten;
            GameEvents.OnContinueUsed -= HandleContinueUsed;
        }

        private void Update()
        {
            if (invincibilityTimer > 0f)
            {
                invincibilityTimer -= Time.deltaTime;
            }
        }

        private void HandleFoodEaten(bool isGold)
        {
            if (isGold)
            {
                Heal(1);
            }
        }

        private void HandleContinueUsed()
        {
            currentHP = Config.continueHP;
            SetInvincible(Config.continueInvincibility);
        }

        public void ResetHealth()
        {
            currentHP = Config.maxHP;
            invincibilityTimer = 0f;
        }

        public bool TakeDamage()
        {
            if (IsInvincible) return false;
            if (turboSystem != null && turboSystem.IsTurboActive) return false;

            currentHP = Mathf.Max(0, currentHP - 1);
            GameEvents.OnPlayerHurt?.Invoke(currentHP);

            if (currentHP <= 0)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }
                else
                {
                    GameEvents.OnPlayerDied?.Invoke();
                }
            }
            else
            {
                SetInvincible(Config.invincibilityDuration);
            }

            return true;
        }

        public void Heal(int amount)
        {
            if (currentHP >= Config.maxHP) return;
            currentHP = Mathf.Min(currentHP + amount, Config.maxHP);
            GameEvents.OnPlayerHurt?.Invoke(currentHP);
        }

        public void SetInvincible(float duration)
        {
            if (duration > invincibilityTimer)
            {
                invincibilityTimer = duration;
            }
        }
    }
}
