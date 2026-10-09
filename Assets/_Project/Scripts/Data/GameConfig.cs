using UnityEngine;

namespace Upstream.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Upstream/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Player")]
        public float startSpeed = 140f;
        /// <summary>Speed the distance curve eases toward (see DifficultyScaler.SpeedForDistance).</summary>
        public float maxSpeed = 420f;
        public float playerMoveSmoothing = 12f;
        public float turboSpeedMultiplier = 1.7f;
        public float turboTime = 3.2f;
        public int maxHP = 3;
        public float invincibilityDuration = 1.3f;
        public int continueHP = 2;
        public float continueInvincibility = 2.2f;

        [Header("Power")]
        public float powerPerFood = 12f;
        public float maxPower = 100f;

        [Header("River")]
        public float riverHalfWidth = 150f;
        public float playerRadius = 14f;

        [Header("Scoring")]
        public float distanceToMetersDivisor = 10f;

        [Header("Death")]
        public float deathAnimDuration = 1f;
    }
}
