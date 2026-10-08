using UnityEngine;
using Upstream.Core;
using Upstream.Data;

namespace Upstream.Entities
{
    /// <summary>
    /// Shark enemy entity that moves independently and tracks the player.
    /// </summary>
    public class SharkEnemy : EntityBase
    {
        public override EntityType Type => EntityType.Shark;
        
        [SerializeField] private SpawnConfig spawnConfig;
        
        public bool isLarge;
        public bool isDead;

        private float downwardVelocity;
        private float phase;
        private Vector2 flyVelocity;
        private float flyRotation;
        private Transform playerTransform;

        public void Initialize(Transform player)
        {
            playerTransform = player;
        }

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            isDead = false;
            phase = Random.Range(0f, Mathf.PI * 2f);
            downwardVelocity = spawnConfig != null ? spawnConfig.baseSharkSpeed : 80f;
            cachedTransform.rotation = Quaternion.identity;
        }

        public override void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;

            if (isDead)
            {
                // Fly-away animation
                Vector3 pos = cachedTransform.position;
                pos += (Vector3)flyVelocity * dt;
                cachedTransform.position = pos;
                cachedTransform.Rotate(0, 0, flyRotation * dt);
                
                yPosition = pos.y;
                if (yPosition > 800f || pos.x < -400f || pos.x > 400f)
                {
                    isActive = false;
                }
                return;
            }

            base.Tick(dt, flowSpeed);
            
            // Shark has its own downward speed relative to river
            yPosition += downwardVelocity * dt;
            Vector3 newPos = cachedTransform.position;
            newPos.y = yPosition;

            // Track player X if above player Y
            if (playerTransform != null && yPosition < playerTransform.position.y)
            {
                float trackingSpeed = spawnConfig != null ? spawnConfig.sharkTrackingSpeed : 30f;
                newPos.x = Mathf.MoveTowards(newPos.x, playerTransform.position.x, trackingSpeed * dt);
            }

            cachedTransform.position = newPos;

            // Tail wag animation
            phase += dt * 10f;
            float wag = Mathf.Sin(phase) * 10f;
            cachedTransform.rotation = Quaternion.Euler(0, 0, wag);
        }

        /// <summary>
        /// Called when the shark is eaten or smashed during turbo.
        /// </summary>
        public void Smash()
        {
            if (isDead) return;
            isDead = true;
            flyVelocity = new Vector2(Random.Range(-300f, 300f), Random.Range(-200f, -500f));
            flyRotation = Random.Range(400f, 800f);
        }
    }
}
