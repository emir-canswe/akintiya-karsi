using UnityEngine;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.Mechanics
{
    /// <summary>
    /// Tracks and calculates the player's score based on distance and bonuses.
    /// </summary>
    public class ScoreSystem : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        
        private float rawDistance;
        private float bonusDistance;
        private float lastReportedScore = -1f;

        /// <summary>
        /// The computed display score in meters.
        /// </summary>
        public int DisplayScore => config == null || config.distanceToMetersDivisor == 0 
            ? 0 : Mathf.FloorToInt((rawDistance + bonusDistance) / config.distanceToMetersDivisor);

        /// <summary>
        /// The raw unadjusted distance accumulated.
        /// </summary>
        public float RawDistance => rawDistance;

        private void OnEnable()
        {
            GameEvents.OnGameStart += ResetScore;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= ResetScore;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                int currentScore = DisplayScore;
                if (currentScore > lastReportedScore + 5) // Periodically fire event to update UI without spamming every frame
                {
                    GameEvents.OnScoreChanged?.Invoke(currentScore);
                    lastReportedScore = currentScore;
                }
            }
        }

        /// <summary>
        /// Accumulate physical distance travelled.
        /// </summary>
        public void AddDistance(float amount)
        {
            rawDistance += amount;
        }

        /// <summary>
        /// Add flat bonus distance (e.g. from eating sharks).
        /// </summary>
        public void AddBonus(float amount)
        {
            bonusDistance += amount;
            GameEvents.OnScoreChanged?.Invoke(DisplayScore); // Update immediately on receiving bonus
        }

        /// <summary>
        /// Reset the score for a new run.
        /// </summary>
        public void ResetScore()
        {
            rawDistance = 0f;
            bonusDistance = 0f;
            lastReportedScore = -1f;
            GameEvents.OnScoreChanged?.Invoke(0);
        }
    }
}
