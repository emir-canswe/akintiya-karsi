using UnityEngine;

namespace Upstream.Data
{
    [CreateAssetMenu(fileName = "SpawnConfig", menuName = "Upstream/Spawn Config")]
    public class SpawnConfig : ScriptableObject
    {
        [Header("Intervals")]
        public float minSpawnInterval = 140f;
        public float maxSpawnInterval = 215f;
        public float intervalReductionPerDifficulty = 85f;

        [Header("Food")]
        public int foodPerLineMin = 3;
        public int foodPerLineMax = 6;
        public float goldFoodBaseChance = 0.07f;
        public float goldFoodChanceIncrease = 0.05f;

        [Header("Shark")]
        public float baseSharkSpeed = 80f;
        public float sharkSpeedIncrease = 120f;
        public float sharkTrackingSpeed = 62f;

        [Header("Log")]
        public float minLogWidthRatio = 0.42f;
        public float maxLogWidthRatio = 0.58f;
        public float logWidthIncreasePerDifficulty = 0.16f;

        // Relative weights (they don't need to sum to 1). Interpolated between Diff 0 and Diff 1.
        [Header("Pattern Weights (Diff 0)")]
        public float diff0FoodLineWeight = 0.30f;
        public float diff0RockWeight = 0.20f;
        public float diff0LogWeight = 0.16f;
        public float diff0SharkWeight = 0.18f;
        public float diff0HookWeight = 0.16f;

        [Header("Pattern Weights (Diff 1)")]
        public float diff1FoodLineWeight = 0.22f;
        public float diff1RockWeight = 0.22f;
        public float diff1LogWeight = 0.16f;
        public float diff1SharkWeight = 0.28f;
        public float diff1HookWeight = 0.14f;

        [Header("Growth")]
        public int largeSharksAfterStage = 3;
    }
}
