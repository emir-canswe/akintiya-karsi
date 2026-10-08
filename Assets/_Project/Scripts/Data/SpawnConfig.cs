using UnityEngine;

namespace Upstream.Data
{
    [CreateAssetMenu(fileName = "SpawnConfig", menuName = "Upstream/Spawn Config")]
    public class SpawnConfig : ScriptableObject
    {
        [Header("Intervals")]
        public float minSpawnInterval = 150f;
        public float maxSpawnInterval = 230f;
        public float intervalReductionPerDifficulty = 50f;

        [Header("Food")]
        public int foodPerLineMin = 3;
        public int foodPerLineMax = 6;
        public float goldFoodBaseChance = 0.07f;
        public float goldFoodChanceIncrease = 0.05f;

        [Header("Shark")]
        public float baseSharkSpeed = 70f;
        public float sharkSpeedIncrease = 90f;
        public float sharkTrackingSpeed = 55f;

        [Header("Log")]
        public float minLogWidthRatio = 0.42f;
        public float maxLogWidthRatio = 0.58f;
        public float logWidthIncreasePerDifficulty = 0.1f;

        [Header("Pattern Weights (Diff 0)")]
        public float diff0FoodLineWeight = 1f;
        public float diff0RockWeight = 1f;
        public float diff0LogWeight = 1f;
        public float diff0SharkWeight = 1f;
        public float diff0HookWeight = 1f;

        [Header("Pattern Weights (Diff 1)")]
        public float diff1FoodLineWeight = 1f;
        public float diff1RockWeight = 1f;
        public float diff1LogWeight = 1f;
        public float diff1SharkWeight = 1f;
        public float diff1HookWeight = 1f;

        public int largeSharksAfterStage = 3;
    }
}
