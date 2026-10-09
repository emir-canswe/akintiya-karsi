using UnityEngine;
using Upstream.Core;
using Upstream.Data;
using Upstream.River;

namespace Upstream.Entities
{
    /// <summary>
    /// Shark enemy that swims downstream toward the player and steers after them. Swims like a real fish:
    /// the tail beats from its base, the body counter-sways a little, and the nose turns into the direction
    /// of travel while it accelerates sideways smoothly.
    /// </summary>
    public class SharkEnemy : EntityBase
    {
        public override EntityType Type => EntityType.Shark;

        [SerializeField] private SpawnConfig spawnConfig;

        public bool isLarge;
        /// <summary>0..1 difficulty at spawn time; set before OnSpawn. Faster sharks later in the run.</summary>
        public float difficulty;
        public bool isDead => isFlying;

        private const float SteerAccel = 240f;   // sideways acceleration (units/s^2)
        private const float TailSwing = 26f;     // tail beat amplitude (degrees)
        private const float BodySway = 5f;       // body counter-sway (degrees)

        private float downwardVelocity = 80f;
        private float lateralVelocity;
        private float heading;
        private float phase;
        private Transform playerTransform;
        private Transform tail;

        protected override void Awake()
        {
            base.Awake();
            EnsureTail();
        }

        private void EnsureTail()
        {
            if (tail != null) return;
            var body = GetComponent<SpriteRenderer>();
            var go = new GameObject("Tail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, CartoonSpriteFactory.SharkTailAttachY, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CartoonSpriteFactory.GetSharkTail();
            sr.sortingOrder = body != null ? body.sortingOrder - 1 : 6; // tucked under the body
            tail = go.transform;
        }

        public void Initialize(Transform player, SpawnConfig cfg = null)
        {
            playerTransform = player;
            if (cfg != null) spawnConfig = cfg;
        }

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            phase = Random.Range(0f, Mathf.PI * 2f);
            float baseSpd = spawnConfig != null ? spawnConfig.baseSharkSpeed : 70f;
            float spdIncrease = spawnConfig != null ? spawnConfig.sharkSpeedIncrease : 90f;
            float spd = baseSpd + Mathf.Clamp01(difficulty) * spdIncrease;
            downwardVelocity = isLarge ? spd * 0.85f : spd;
            cachedTransform.localScale = Vector3.one * (isLarge ? 1.4f : 1.0f);
            lateralVelocity = 0f;
            heading = 0f;
            EnsureTail();
            tail.localRotation = Quaternion.identity;
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

            // Steer toward the player's X while still above them: accelerate smoothly, no instant sliding.
            // Chases harder later in the run.
            float trackingSpeed = (spawnConfig != null ? spawnConfig.sharkTrackingSpeed : 62f) * (1f + 0.7f * Mathf.Clamp01(difficulty));
            float targetVx = 0f;
            if (playerTransform != null && yPosition > playerTransform.position.y + 40f)
            {
                targetVx = Mathf.Clamp((playerTransform.position.x - newPos.x) * 2.2f, -trackingSpeed, trackingSpeed);
            }
            lateralVelocity = Mathf.MoveTowards(lateralVelocity, targetVx, SteerAccel * dt);
            newPos.x += lateralVelocity * dt;
            cachedTransform.position = newPos;

            // Swim cycle: faster sharks beat their tails faster.
            phase += dt * (7f + downwardVelocity * 0.025f);
            if (tail != null) tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * TailSwing);

            // Nose points along the travel direction (it faces down, so +X motion turns it counter-clockwise),
            // plus a small counter-sway that lags the tail.
            float targetHeading = Mathf.Atan2(lateralVelocity, downwardVelocity + flowSpeed) * Mathf.Rad2Deg;
            heading = Mathf.Lerp(heading, targetHeading, 1f - Mathf.Exp(-6f * dt));
            float sway = -Mathf.Sin(phase - 0.9f) * BodySway;
            cachedTransform.rotation = Quaternion.Euler(0f, 0f, heading + sway);
        }
    }
}
