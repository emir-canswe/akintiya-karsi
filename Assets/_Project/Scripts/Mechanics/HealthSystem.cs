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

        private int currentHP = 100;
        private float invincibilityTimer;
        private TurboSystem turboSystem;
        // Extra max health from a legendary fish; refreshed at the start of every run.
        private int bonusHP;
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
        public int MaxHP => Config.maxHP + bonusHP;
        public float HealthPercent => MaxHP > 0 ? (float)currentHP / MaxHP : 0f;

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

            int before = currentHP;
            bool healed = Heal(Config.goldFoodHeal);
            if (!healed && Turbo != null)
            {
                // Full health: the rare gold shrimp shouldn't be wasted, turn it into a big power boost.
                Turbo.AddPower(Config.powerPerFood * GoldOverflowPowerFoods);
            }
            GameEvents.OnGoldFoodResolved?.Invoke(currentHP - before);
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
            bonusHP = fish.extraHeart ? Config.legendaryBonusHP : 0;
            shieldCharges = fish.ShieldCharges;
            currentHP = MaxHP;
            invincibilityTimer = 0f;
            GameEvents.OnHealthChanged?.Invoke(currentHP);
        }

        /// <summary>Takes <paramref name="amount"/> health (see HitDamage). Returns true if the hit landed.</summary>
        public bool TakeDamage(int amount)
        {
            // Already dead: a second hazard touched in the same frame must not hurt (sound/vibration) again.
            if (currentHP <= 0 || amount <= 0) return false;
            if (IsInvincible) return false;
            if (Turbo != null && Turbo.IsTurboActive) return false;

            if (shieldCharges > 0)
            {
                // Shield ability soaks the hit: no health lost, short grace period.
                shieldCharges--;
                SetInvincible(1f);
                GameEvents.OnShieldBlocked?.Invoke(shieldCharges);
                return false;
            }

            currentHP = Mathf.Max(0, currentHP - amount);
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
