using UnityEngine;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.Entities
{
    /// <summary>
    /// Shark enemy entity that moves independently downward and tracks the player.
    /// </summary>
    public class SharkEnemy : EntityBase
    {
        public override EntityType Type => EntityType.Shark;

        [SerializeField] private SpawnConfig spawnConfig;

        public bool isLarge;
        public bool isDead => isFlying;

        private float downwardVelocity = 80f;
        private float phase;
        private Transform playerTransform;

        public void Initialize(Transform player, SpawnConfig cfg = null)
        {
            playerTransform = player;
            if (cfg != null) spawnConfig = cfg;
        }

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            phase = Random.Range(0f, Mathf.PI * 2f);
            float baseSpd = spawnConfig != null ? spawnConfig.baseSharkSpeed : 75f;
            downwardVelocity = isLarge ? baseSpd * 0.85f : baseSpd;
            cachedTransform.localScale = Vector3.one * (isLarge ? 1.4f : 1.0f);
        }

        public override void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;

            if (isFlying)
            {
                base.Tick(dt, flowSpeed);
                return;
            }

            base.Tick(dt, flowSpeed);

            // Shark has its own downward speed on top of river flow
            yPosition -= downwardVelocity * dt;
            Vector3 newPos = cachedTransform.position;
            newPos.y = yPosition;

            // Track player X while above player Y
            if (playerTransform != null && yPosition > playerTransform.position.y + 40f)
            {
                float trackingSpeed = spawnConfig != null ? spawnConfig.sharkTrackingSpeed : 55f;
                newPos.x = Mathf.MoveTowards(newPos.x, playerTransform.position.x, trackingSpeed * dt);
            }

            cachedTransform.position = newPos;

            // Tail wag animation
            phase += dt * 9f;
            float wag = Mathf.Sin(phase) * 9f;
            cachedTransform.rotation = Quaternion.Euler(0f, 0f, wag);
        }
    }
}
