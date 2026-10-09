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

        private const float GoldOverflowPowerFoods = 3f;
        private const float ResumeGraceSeconds = 1f;

        private int currentHP = 3;
        private float invincibilityTimer;
        private TurboSystem turboSystem;
        // +1 heart from a legendary fish; refreshed at the start of every run.
        private int bonusHearts;
        private int shieldCharges;

        /// <summary>Hits the shield ability will still block this run.</summary>
        public int ShieldCharges => shieldCharges;

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
        public int MaxHP => Config.maxHP + bonusHearts;

        // Resolved lazily: the bootstrap adds TurboSystem after HealthSystem, so it doesn't exist yet in Awake.
        private TurboSystem Turbo
        {
            get
            {
                if (turboSystem == null) turboSystem = GetComponent<TurboSystem>();
                return turboSystem;
            }
        }

        private void Awake()
        {
            currentHP = MaxHP;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += ResetHealth;
            GameEvents.OnFoodEaten += HandleFoodEaten;
            GameEvents.OnContinueUsed += HandleContinueUsed;
            GameEvents.OnGameResume += HandleGameResume;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= ResetHealth;
            GameEvents.OnFoodEaten -= HandleFoodEaten;
            GameEvents.OnContinueUsed -= HandleContinueUsed;
            GameEvents.OnGameResume -= HandleGameResume;
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
            if (!isGold) return;

            bool healed = Heal(1);
            if (!healed && Turbo != null)
            {
                // Full health: the rare gold shrimp shouldn't be wasted, turn it into a big power boost.
                Turbo.AddPower(Config.powerPerFood * GoldOverflowPowerFoods);
            }
            GameEvents.OnGoldFoodResolved?.Invoke(healed);
        }

        private void HandleGameResume()
        {
            // Short grace period after un-pausing so the player can re-orient.
            SetInvincible(ResumeGraceSeconds);
        }

        private void HandleContinueUsed()
        {
            currentHP = Mathf.Clamp(Config.continueHP, 1, MaxHP);
            SetInvincible(Config.continueInvincibility);
            GameEvents.OnHealthChanged?.Invoke(currentHP);
        }

        public void ResetHealth()
        {
            FishDef fish = FishShop.Active;
            bonusHearts = fish.extraHeart ? 1 : 0;
            shieldCharges = fish.ShieldCharges;
            currentHP = MaxHP;
            invincibilityTimer = 0f;
            GameEvents.OnHealthChanged?.Invoke(currentHP);
        }

        public bool TakeDamage()
        {
            if (IsInvincible) return false;
            if (Turbo != null && Turbo.IsTurboActive) return false;

            if (shieldCharges > 0)
            {
                // Shield ability soaks the hit: no heart lost, short grace period.
                shieldCharges--;
                SetInvincible(1f);
                GameEvents.OnShieldBlocked?.Invoke(shieldCharges);
                return false;
            }

            currentHP = Mathf.Max(0, currentHP - 1);
            GameEvents.OnPlayerHurt?.Invoke(currentHP);
            GameEvents.OnHealthChanged?.Invoke(currentHP);

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

        /// <summary>Returns true if any HP was actually restored.</summary>
        public bool Heal(int amount)
        {
            if (amount <= 0 || currentHP <= 0 || currentHP >= MaxHP) return false;
            currentHP = Mathf.Min(currentHP + amount, MaxHP);
            GameEvents.OnHealthChanged?.Invoke(currentHP);
            return true;
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
