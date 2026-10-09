using System;
using UnityEngine;

namespace Upstream.Data
{
    [Serializable]
    public class RegionData
    {
        public string regionName;
        public float startDistance;
        public Color waterColor1;
        public Color waterColor2;
        public Color bankColor;
        public Color grassColor;
        public float speedMultiplier;
    }

    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Upstream/Difficulty Config")]
    public class DifficultyConfig : ScriptableObject
    {
        /// <summary>Distance (world units, 10 per meter) over which difficulty climbs to 1.</summary>
        public float difficultyRampDistance = 12000f;
        /// <summary>Difficulty at the very start of a run (0..1): no "too easy" warm-up.</summary>
        public float startingDifficulty = 0.15f;

        public RegionData[] regions = new RegionData[]
        {
            new RegionData
            {
                regionName = "Mountain Creek",
                startDistance = 0f,
                // Sunny turquoise creek
                waterColor1 = new Color(0.36f, 0.90f, 0.95f),
                waterColor2 = new Color(0.10f, 0.64f, 0.88f),
                bankColor = new Color(0.3f, 0.6f, 0.2f),
                grassColor = new Color(0.4f, 0.8f, 0.3f),
                speedMultiplier = 1.0f
            },
            new RegionData
            {
                regionName = "Wide Lake",
                startDistance = 10000f,
                // Deep friendly blue lake
                waterColor1 = new Color(0.42f, 0.72f, 1.00f),
                waterColor2 = new Color(0.20f, 0.42f, 0.92f),
                bankColor = new Color(0.9f, 0.8f, 0.6f),
                grassColor = new Color(0.8f, 0.7f, 0.5f),
                speedMultiplier = 0.9f
            },
            new RegionData
            {
                regionName = "Rapids",
                startDistance = 20000f,
                // Bright emerald rapids: clearly "a new place", food and foam stay visible.
                waterColor1 = new Color(0.30f, 0.90f, 0.78f),
                waterColor2 = new Color(0.06f, 0.62f, 0.58f),
                bankColor = new Color(0.5f, 0.5f, 0.5f),
                grassColor = new Color(0.4f, 0.4f, 0.4f),
                speedMultiplier = 1.15f
            }
        };
    }
}
