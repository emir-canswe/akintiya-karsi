using UnityEngine;
using Upstream.Data;

namespace Upstream.Mechanics
{
    /// <summary>
    /// Utility for calculating difficulty scaling and active regions over distance.
    /// </summary>
    public class DifficultyScaler : MonoBehaviour
    {
        /// <summary>Ramp distance used when no DifficultyConfig is assigned.</summary>
        public const float DefaultRampDistance = 5000f;

        [SerializeField] private DifficultyConfig config;

        public DifficultyConfig Config
        {
            get
            {
                if (config == null) config = ScriptableObject.CreateInstance<DifficultyConfig>();
                return config;
            }
            set => config = value;
        }

        /// <summary>
        /// Calculates the difficulty multiplier (0 to 1) based on raw distance.
        /// </summary>
        public float GetDifficulty(float distance)
        {
            return CalculateDifficulty(distance, Config.difficultyRampDistance);
        }

        /// <summary>
        /// Pure difficulty curve: linear 0..1 over the ramp distance.
        /// </summary>
        public static float CalculateDifficulty(float distance, float rampDistance)
        {
            if (rampDistance <= 0f) return 1f;
            return Mathf.Clamp01(distance / rampDistance);
        }

        /// <summary>
        /// Retrieves the current RegionData based on the accumulated distance.
        /// </summary>
        public RegionData GetCurrentRegion(float distance)
        {
            return FindRegion(Config.regions, distance);
        }

        /// <summary>
        /// Returns the region with the highest startDistance that is &lt;= distance.
        /// Regions don't need to be sorted.
        /// </summary>
        public static RegionData FindRegion(RegionData[] regions, float distance)
        {
            if (regions == null || regions.Length == 0) return null;

            RegionData current = null;
            float bestStart = float.NegativeInfinity;
            for (int i = 0; i < regions.Length; i++)
            {
                var region = regions[i];
                if (region == null) continue;
                if (distance >= region.startDistance && region.startDistance >= bestStart)
                {
                    bestStart = region.startDistance;
                    current = region;
                }
            }

            return current ?? regions[0];
        }

        /// <summary>
        /// Calculates the updated base speed using acceleration.
        /// </summary>
        public float GetCurrentSpeed(float baseSpeed, float maxSpeed, float speedIncrease, float deltaTime)
        {
            return Mathf.Min(baseSpeed + (speedIncrease * deltaTime), maxSpeed);
        }
    }
}
