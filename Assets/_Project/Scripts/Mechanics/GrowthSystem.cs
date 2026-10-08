using UnityEngine;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.Mechanics
{
    /// <summary>
    /// Tracks food eaten and manages player growth stages.
    /// </summary>
    public class GrowthSystem : MonoBehaviour
    {
        [SerializeField] private GrowthConfig config;
        
        private int currentStageIndex;
        private int totalFoodEaten;
        private HealthSystem healthSystem;

        /// <summary>
        /// The current growth stage index.
        /// </summary>
        public int CurrentStage => currentStageIndex;

        /// <summary>
        /// The configuration data for the current growth stage.
        /// </summary>
        public GrowthStage CurrentStageData => (config != null && config.stages != null && currentStageIndex < config.stages.Length) ? config.stages[currentStageIndex] : null;

        /// <summary>
        /// Visual size multiplier for the current stage.
        /// </summary>
        public float SizeMultiplier => CurrentStageData?.sizeMultiplier ?? 1f;

        /// <summary>
        /// Hitbox scaling multiplier for the current stage.
        /// </summary>
        public float HitboxMultiplier => CurrentStageData?.hitboxMultiplier ?? 1f;

        private void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
        }

        private void OnEnable()
        {
            GameEvents.OnFoodEaten += OnFoodEaten;
            GameEvents.OnGameStart += ResetGrowth;
        }

        private void OnDisable()
        {
            GameEvents.OnFoodEaten -= OnFoodEaten;
            GameEvents.OnGameStart -= ResetGrowth;
        }

        private void OnFoodEaten(bool isGold)
        {
            if (isGold) return; // Gold food doesn't contribute to standard growth

            totalFoodEaten++;
            CheckGrowth();
        }

        private void CheckGrowth()
        {
            if (config == null || config.stages == null) return;
            if (currentStageIndex >= config.stages.Length - 1) return; // Already at max stage

            int nextStageReq = config.stages[currentStageIndex + 1].foodRequired;
            if (totalFoodEaten >= nextStageReq)
            {
                currentStageIndex++;
                GameEvents.OnGrowthStageChanged?.Invoke(currentStageIndex);

                if (CurrentStageData != null && CurrentStageData.healOnStageUp && healthSystem != null)
                {
                    healthSystem.Heal(1);
                }
            }
        }

        /// <summary>
        /// Determines if a shark can be eaten based on current stage capabilities.
        /// </summary>
        public bool CanEatShark(bool isLarge, bool hasTurbo)
        {
            if (CurrentStageData == null) return false;

            if (isLarge)
            {
                return CurrentStageData.canEatLargeSharks;
            }
            else
            {
                if (CurrentStageData.canEatSmallSharksWithoutTurbo) return true;
                return CurrentStageData.canEatSmallSharks && hasTurbo;
            }
        }

        /// <summary>
        /// Resets growth progress to the beginning.
        /// </summary>
        public void ResetGrowth()
        {
            currentStageIndex = 0;
            totalFoodEaten = 0;
            GameEvents.OnGrowthStageChanged?.Invoke(currentStageIndex);
        }
    }
}
