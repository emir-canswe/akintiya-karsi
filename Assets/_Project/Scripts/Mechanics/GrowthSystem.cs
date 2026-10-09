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

        public GrowthConfig Config
        {
            get
            {
                if (config == null) config = ScriptableObject.CreateInstance<GrowthConfig>();
                return config;
            }
            set => config = value;
        }

        public int CurrentStage => currentStageIndex;
        public int TotalFoodEaten => totalFoodEaten;

        public GrowthStage CurrentStageData =>
            (Config.stages != null && currentStageIndex >= 0 && currentStageIndex < Config.stages.Length)
                ? Config.stages[currentStageIndex]
                : null;

        public float SizeMultiplier => CurrentStageData?.sizeMultiplier ?? 1f;
        public float HitboxMultiplier => CurrentStageData?.hitboxMultiplier ?? 1f;

        /// <summary>
        /// Progress (0 to 1) toward the next growth stage. Returns 1 at max stage.
        /// </summary>
        public float StageProgress
        {
            get
            {
                if (Config.stages == null || currentStageIndex >= Config.stages.Length - 1) return 1f;
                int prevReq = Config.stages[currentStageIndex].foodRequired;
                int nextReq = Config.stages[currentStageIndex + 1].foodRequired;
                if (nextReq <= prevReq) return 1f;
                return Mathf.Clamp01((float)(totalFoodEaten - prevReq) / (nextReq - prevReq));
            }
        }

        // Resolved lazily: the bootstrap adds HealthSystem after GrowthSystem, so it doesn't exist yet in Awake.
        private HealthSystem Health
        {
            get
            {
                if (healthSystem == null) healthSystem = GetComponent<HealthSystem>();
                return healthSystem;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnFoodEaten += OnFoodEaten;
            GameEvents.OnGameStart += ResetGrowth;
            GameEvents.OnReturnToMenu += ResetGrowthSilently;
        }

        private void OnDisable()
        {
            GameEvents.OnFoodEaten -= OnFoodEaten;
            GameEvents.OnGameStart -= ResetGrowth;
            GameEvents.OnReturnToMenu -= ResetGrowthSilently;
        }

        public void OnFoodEaten(bool isGold)
        {
            if (isGold) return;

            totalFoodEaten++;
            CheckGrowth();
        }

        private void CheckGrowth()
        {
            if (Config.stages == null) return;
            while (currentStageIndex < Config.stages.Length - 1 &&
                   totalFoodEaten >= Config.stages[currentStageIndex + 1].foodRequired)
            {
                currentStageIndex++;
                GameEvents.OnGrowthStageChanged?.Invoke(currentStageIndex);

                if (CurrentStageData != null && CurrentStageData.healOnStageUp && Health != null)
                {
                    Health.Heal(1);
                }
            }
        }

        public bool CanEatShark(bool isLarge, bool hasTurbo)
        {
            if (CurrentStageData == null) return false;

            if (isLarge)
            {
                return CurrentStageData.canEatLargeSharks && hasTurbo;
            }
            else
            {
                if (CurrentStageData.canEatSmallSharksWithoutTurbo) return true;
                return CurrentStageData.canEatSmallSharks && hasTurbo;
            }
        }

        public void ResetGrowth() => ResetGrowth(true);

        // Back on the menu the fish shrinks to its start size without a "stage up" toast.
        private void ResetGrowthSilently() => ResetGrowth(false);

        private void ResetGrowth(bool announce)
        {
            var save = SaveSystem.Load();
            currentStageIndex = Config.GetStartStage(save.bestStageReached);
            totalFoodEaten = Config.stages[currentStageIndex].foodRequired;
            if (announce) GameEvents.OnGrowthStageChanged?.Invoke(currentStageIndex);
        }
    }
}
