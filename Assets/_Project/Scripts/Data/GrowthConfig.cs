using System;
using UnityEngine;

namespace Upstream.Data
{
    [Serializable]
    public class GrowthStage
    {
        public string stageName = "Stage";
        public float sizeMultiplier = 1.0f;
        public int foodRequired = 0;
        public bool canEatSmallSharks = false;
        public bool canEatSmallSharksWithoutTurbo = false;
        public bool canEatLargeSharks = false;
        public bool smallRockImmune = false;
        public float powerBarFillMultiplier = 1.0f;
        public float hitboxMultiplier = 1.0f;
        public bool healOnStageUp = false;
    }

    [CreateAssetMenu(fileName = "GrowthConfig", menuName = "Upstream/Growth Config")]
    public class GrowthConfig : ScriptableObject
    {
        public GrowthStage[] stages = new GrowthStage[5]
        {
            new GrowthStage { stageName = "Fry",    sizeMultiplier = 1.0f, foodRequired = 0,   canEatSmallSharks = false, canEatSmallSharksWithoutTurbo = false, canEatLargeSharks = false, smallRockImmune = false, powerBarFillMultiplier = 1.0f,  hitboxMultiplier = 1.0f, healOnStageUp = false },
            new GrowthStage { stageName = "Young",  sizeMultiplier = 1.3f, foodRequired = 15,  canEatSmallSharks = false, canEatSmallSharksWithoutTurbo = false, canEatLargeSharks = false, smallRockImmune = true,  powerBarFillMultiplier = 0.85f, hitboxMultiplier = 1.25f, healOnStageUp = true },
            new GrowthStage { stageName = "Adult",  sizeMultiplier = 1.6f, foodRequired = 35,  canEatSmallSharks = true,  canEatSmallSharksWithoutTurbo = false, canEatLargeSharks = false, smallRockImmune = true,  powerBarFillMultiplier = 0.72f, hitboxMultiplier = 1.5f, healOnStageUp = true },
            new GrowthStage { stageName = "Alpha",  sizeMultiplier = 2.0f, foodRequired = 60,  canEatSmallSharks = true,  canEatSmallSharksWithoutTurbo = true,  canEatLargeSharks = true,  smallRockImmune = true,  powerBarFillMultiplier = 0.61f, hitboxMultiplier = 1.8f, healOnStageUp = true },
            new GrowthStage { stageName = "Titan",  sizeMultiplier = 2.5f, foodRequired = 100, canEatSmallSharks = true,  canEatSmallSharksWithoutTurbo = true,  canEatLargeSharks = true,  smallRockImmune = true,  powerBarFillMultiplier = 0.52f, hitboxMultiplier = 2.2f, healOnStageUp = true }
        };

        [Header("Persistent progression")]
        [Tooltip("Reaching this stage index in any run (3 = Alpha) unlocks a bigger starting fish.")]
        public int unlockStartStageAtReached = 3;
        [Tooltip("Stage index new runs start at once unlocked (1 = Young). 0 disables the unlock.")]
        public int unlockedStartStage = 1;

        public int sharkEatBonusDistance = 500;
        public float bulletTimeScale = 0.3f;
        public float bulletTimeDuration = 0.3f;
        public float zoomInAmount = 0.2f;

        /// <summary>
        /// Pure rule: which stage a new run starts at, given the best stage ever reached.
        /// </summary>
        public int GetStartStage(int bestStageReached)
        {
            if (stages == null || stages.Length == 0) return 0;
            int start = bestStageReached >= unlockStartStageAtReached ? unlockedStartStage : 0;
            return Mathf.Clamp(start, 0, stages.Length - 1);
        }
    }
}
