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
        /// <summary>One health bar; each hit takes a HitDamage amount from it.</summary>
        public int maxHP = 100;
        public float invincibilityDuration = 1.3f;
        public int continueHP = 50;
        public float continueInvincibility = 2.2f;
        /// <summary>Health a gold shrimp gives back: kept small so hits really matter.</summary>
        public int goldFoodHeal = 10;
        /// <summary>Health given back on reaching a new growth stage (stages with healOnStageUp).</summary>
        public int stageUpHeal = 10;
        /// <summary>Extra max health from a legendary fish (FishDef.extraHeart).</summary>
        public int legendaryBonusHP = 25;

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
