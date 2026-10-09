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
        public float difficultyRampDistance = 5000f;

        public RegionData[] regions = new RegionData[]
        {
            new RegionData
            {
                regionName = "Mountain Creek",
                startDistance = 0f,
                waterColor1 = new Color(0.2f, 0.8f, 0.9f),
                waterColor2 = new Color(0.1f, 0.6f, 0.7f),
                bankColor = new Color(0.3f, 0.6f, 0.2f),
                grassColor = new Color(0.4f, 0.8f, 0.3f),
                speedMultiplier = 1.0f
            },
            new RegionData
            {
                regionName = "Wide Lake",
                startDistance = 10000f,
                waterColor1 = new Color(0.1f, 0.3f, 0.8f),
                waterColor2 = new Color(0.05f, 0.2f, 0.6f),
                bankColor = new Color(0.9f, 0.8f, 0.6f),
                grassColor = new Color(0.8f, 0.7f, 0.5f),
                speedMultiplier = 0.9f
            },
            new RegionData
            {
                regionName = "Rapids",
                startDistance = 20000f,
                // Churning grey-teal: light enough to read as "white water" but food/foam stay visible.
                waterColor1 = new Color(0.36f, 0.70f, 0.74f),
                waterColor2 = new Color(0.22f, 0.50f, 0.58f),
                bankColor = new Color(0.5f, 0.5f, 0.5f),
                grassColor = new Color(0.4f, 0.4f, 0.4f),
                speedMultiplier = 1.15f
            }
        };
    }
}
