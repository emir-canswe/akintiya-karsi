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
        private int lastReportedScore = -1;

        public GameConfig Config
        {
            get
            {
                if (config == null)
                {
                    config = GameManager.Instance != null
                        ? GameManager.Instance.Config
                        : ScriptableObject.CreateInstance<GameConfig>();
                }
                return config;
            }
            set => config = value;
        }

        public int DisplayScore => Config.distanceToMetersDivisor <= 0f
            ? 0
            : Mathf.FloorToInt((rawDistance + bonusDistance) / Config.distanceToMetersDivisor);

        public float RawDistance => rawDistance;
        public float TotalDistance => rawDistance + bonusDistance;

        private void OnEnable()
        {
            GameEvents.OnGameStart += ResetScore;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= ResetScore;
        }

        public void AddDistance(float amount)
        {
            rawDistance += amount;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CurrentDistance = rawDistance + bonusDistance;
            }

            int currentScore = DisplayScore;
            if (currentScore != lastReportedScore)
            {
                lastReportedScore = currentScore;
                GameEvents.OnScoreChanged?.Invoke(currentScore);
            }
        }

        public void AddBonus(float amount)
        {
            bonusDistance += amount;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CurrentDistance = rawDistance + bonusDistance;
            }
            lastReportedScore = DisplayScore;
            GameEvents.OnScoreChanged?.Invoke(lastReportedScore);
        }

        public void ResetScore()
        {
            rawDistance = 0f;
            bonusDistance = 0f;
            lastReportedScore = 0;
            GameEvents.OnScoreChanged?.Invoke(0);
        }
    }
}
