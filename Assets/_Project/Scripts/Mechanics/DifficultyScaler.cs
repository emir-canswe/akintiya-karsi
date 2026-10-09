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

        /// <summary>Extra river speed per meter once the main curve has flattened (keeps long runs getting harder).</summary>
        public const float LateSpeedPerMeter = 0.03f;

        /// <summary>
        /// River speed for the distance swum: rises quickly at first, eases toward <paramref name="maxSpeed"/>,
        /// then keeps creeping up so there's never a plateau (capped at 1.35x max).
        /// </summary>
        public static float SpeedForDistance(float meters, float startSpeed, float maxSpeed)
        {
            meters = Mathf.Max(0f, meters);
            float curve = (maxSpeed - startSpeed) * (1f - Mathf.Exp(-meters / 800f));
            return Mathf.Min(startSpeed + curve + meters * LateSpeedPerMeter, maxSpeed * 1.35f);
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
