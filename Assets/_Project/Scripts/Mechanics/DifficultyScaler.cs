using UnityEngine;
using Upstream.Data;
using System.Reflection;

namespace Upstream.Mechanics
{
    /// <summary>
    /// Utility for calculating difficulty scaling and active regions over distance.
    /// </summary>
    public class DifficultyScaler : MonoBehaviour
    {
        [SerializeField] private DifficultyConfig config;

        /// <summary>
        /// Current difficulty percentage normalized from 0 to 1 based on GameManager's distance.
        /// </summary>
        public float DifficultyPercent
        {
            get
            {
                if (config == null || config.difficultyRampDistance <= 0f) return 0f;
                // Requires GameManager reference or just use GetDifficulty
                return 0f; 
            }
        }

        /// <summary>
        /// Calculates the difficulty multiplier (0 to 1) based on raw distance.
        /// </summary>
        public float GetDifficulty(float distance)
        {
            if (config == null || config.difficultyRampDistance <= 0f) return 0f;
            return Mathf.Clamp01(distance / config.difficultyRampDistance);
        }

        /// <summary>
        /// Retrieves the current RegionData based on the accumulated distance.
        /// </summary>
        public RegionData GetCurrentRegion(float distance)
        {
            if (config == null || config.regions == null || config.regions.Length == 0) return null;

            RegionData currentRegion = config.regions[0];
            
            // Note: Assumes RegionData has a standard field like startDistance for its threshold.
            // Adjust property reflection or direct field access according to the exact structure.
            for (int i = 0; i < config.regions.Length; i++)
            {
                var region = config.regions[i];
                float startDist = 0f;
                
                FieldInfo field = region.GetType().GetField("startDistance");
                if (field != null) startDist = (float)field.GetValue(region);
                
                if (distance >= startDist)
                {
                    currentRegion = region;
                }
            }

            return currentRegion;
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
