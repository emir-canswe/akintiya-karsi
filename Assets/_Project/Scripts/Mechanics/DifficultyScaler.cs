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
            float start = Mathf.Clamp01(Config.startingDifficulty);
            return start + (1f - start) * CalculateDifficulty(distance, Config.difficultyRampDistance);
        }

        /// <summary>Meters between two speed-ups.</summary>
        public const float SpeedStepMeters = 200f;
        /// <summary>River speed added at every step (about 11% of the start speed).</summary>
        public const float SpeedStep = 16f;

        /// <summary>
        /// River speed for the distance swum: a small step up every <see cref="SpeedStepMeters"/>, reaching
        /// <paramref name="maxSpeed"/> after a few minutes; past it the steps get a third as big (capped at 1.2x max)
        /// so long runs still get harder.
        /// </summary>
        public static float SpeedForDistance(float meters, float startSpeed, float maxSpeed)
        {
            float steps = Mathf.Floor(Mathf.Max(0f, meters) / SpeedStepMeters);
            float speed = startSpeed + steps * SpeedStep;
            if (speed > maxSpeed) speed = maxSpeed + (speed - maxSpeed) / 3f;
            return Mathf.Min(speed, maxSpeed * 1.2f);
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
    }
}
