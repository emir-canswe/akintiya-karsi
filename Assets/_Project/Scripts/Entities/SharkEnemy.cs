using UnityEngine;
using Upstream.Core;
using Upstream.Data;
using Upstream.River;

namespace Upstream.Entities
{
    /// <summary>
    /// A river predator (shark, pike, piranha or catfish) swimming downstream at the player. All kinds swim like
    /// real fish: the tail beats from its base, the body counter-sways and the nose turns into the direction of
    /// travel. Each kind moves differently:
    /// shark chases steadily; pike drifts, shivers as a warning, then lunges; piranhas zigzag in schools;
    /// catfish are slow but big (only a very big fish can eat one).
    /// </summary>
    public class SharkEnemy : EntityBase
    {
        public override EntityType Type => EntityType.Shark;

        [SerializeField] private SpawnConfig spawnConfig;

        /// <summary>Set before OnSpawn.</summary>
        public PredatorKind kind;
        public bool isLarge;
        /// <summary>0..1 difficulty at spawn time; set before OnSpawn. Faster predators later in the run.</summary>
        public float difficulty;
        public bool isDead => isFlying;

        private const float SteerAccel = 240f;   // sideways acceleration (units/s^2)
        private const float TailSwing = 26f;     // tail beat amplitude (degrees)
        private const float BodySway = 5f;       // body counter-sway (degrees)
        private const float PikeTriggerDistance = 250f;
        private const float PikeWindup = 0.45f;

        // Hit circles along the body (offsets in local Y) and their radius, per kind, before scaling.
        private static readonly float[] SharkParts = { -16f, 4f, 24f };
        private static readonly float[] PikeParts = { -32f, -12f, 8f, 26f };
        private static readonly float[] PiranhaParts = { -6f, 6f };
        private static readonly float[] CatfishParts = { -18f, 2f, 20f };

        private float cruiseSpeed;
        private float downwardVelocity = 80f;
        private float lateralVelocity;
        private float heading;
        private float phase;
        private float pikeTimer;     // < 0: cruising, 0..windup: winding up, > windup: lunging
        private Transform playerTransform;
        private Transform tail;
        private SpriteRenderer body;
        private SpriteRenderer shadow;
        private SpriteRenderer tailRenderer;

        public float[] HitOffsets => kind == PredatorKind.Pike ? PikeParts
                                   : kind == PredatorKind.Piranha ? PiranhaParts
                                   : kind == PredatorKind.Catfish ? CatfishParts
                                   : SharkParts;

        public float HitRadius => kind == PredatorKind.Pike ? 9f
                                : kind == PredatorKind.Piranha ? 11f
                                : kind == PredatorKind.Catfish ? 21f
                                : 16f;

        /// <summary>Current visual scale (large sharks are drawn bigger).</summary>
        public float HitScale => cachedTransform != null ? cachedTransform.localScale.x : 1f;

        protected override void Awake()
        {
            base.Awake();
            EnsureParts();
        }

        private void EnsureParts()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (shadow == null)
            {
                var s = transform.Find("Shadow");
                if (s != null) shadow = s.GetComponent<SpriteRenderer>();
            }
            if (tail != null) return;
            var go = new GameObject("Tail");
            go.transform.SetParent(transform, false);
            tailRenderer = go.AddComponent<SpriteRenderer>();
            tailRenderer.sortingOrder = body != null ? body.sortingOrder - 1 : 6; // tucked under the body
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
            EnsureParts();

            // Look of this kind
            Sprite bodySprite = CartoonSpriteFactory.GetPredatorBody(kind);
            if (body != null) body.sprite = bodySprite;
            if (shadow != null) shadow.sprite = bodySprite;
            tailRenderer.sprite = CartoonSpriteFactory.GetPredatorTail(kind);
            tail.localPosition = new Vector3(0f, CartoonSpriteFactory.PredatorTailAttachY(kind), 0f);
            tail.localRotation = Quaternion.identity;

            phase = Random.Range(0f, Mathf.PI * 2f);
            float baseSpd = spawnConfig != null ? spawnConfig.baseSharkSpeed : 80f;
            float spdIncrease = spawnConfig != null ? spawnConfig.sharkSpeedIncrease : 120f;
            float spd = baseSpd + Mathf.Clamp01(difficulty) * spdIncrease;
            switch (kind)
            {
                case PredatorKind.Pike: cruiseSpeed = spd * 0.45f; break;
                case PredatorKind.Piranha: cruiseSpeed = spd * 0.95f; break;
                case PredatorKind.Catfish: cruiseSpeed = spd * 0.55f; break;
                default: cruiseSpeed = isLarge ? spd * 0.85f : spd; break;
            }
            downwardVelocity = cruiseSpeed;
            cachedTransform.localScale = Vector3.one * (kind == PredatorKind.Shark && isLarge ? 1.4f : 1.0f);
            lateralVelocity = 0f;
            heading = 0f;
            pikeTimer = -1f;
        }

        public override void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;

            base.Tick(dt, flowSpeed);
            if (isFlying || !isActive) return;

            float trackingSpeed = (spawnConfig != null ? spawnConfig.sharkTrackingSpeed : 62f) * (1f + 0.7f * Mathf.Clamp01(difficulty));
            float playerX = playerTransform != null ? playerTransform.position.x : cachedTransform.position.x;
            bool above = playerTransform != null && yPosition > playerTransform.position.y + 40f;
            float targetVx = 0f;
            float beatSpeed = 7f;
            float shiver = 0f;

            switch (kind)
            {
                case PredatorKind.Pike:
                    if (pikeTimer < 0f)
                    {
                        // Cruise slowly, lining up a little with the player
                        if (above) targetVx = Mathf.Clamp((playerX - cachedTransform.position.x) * 1.2f, -trackingSpeed * 0.6f, trackingSpeed * 0.6f);
                        if (playerTransform != null && yPosition - playerTransform.position.y < PikeTriggerDistance) pikeTimer = 0f;
                    }
                    else if (pikeTimer < PikeWindup)
                    {
                        // Wind-up: nearly stops and shivers (the warning), aiming at the player
                        pikeTimer += dt;
                        downwardVelocity = Mathf.MoveTowards(downwardVelocity, -flowSpeed * 0.6f, 900f * dt);
                        targetVx = Mathf.Clamp((playerX - cachedTransform.position.x) * 3f, -trackingSpeed, trackingSpeed);
                        shiver = Mathf.Sin(Time.time * 60f) * 6f;
                        beatSpeed = 24f;
                    }
                    else
                    {
                        // Lunge: straight and fast
                        downwardVelocity = Mathf.MoveTowards(downwardVelocity, cruiseSpeed * 4.8f, 1400f * dt);
                        beatSpeed = 20f;
                    }
                    break;

                case PredatorKind.Piranha:
                    // Quick, nervous zigzag while homing in
                    if (above) targetVx = Mathf.Clamp((playerX - cachedTransform.position.x) * 2.5f, -trackingSpeed * 1.2f, trackingSpeed * 1.2f);
                    targetVx += Mathf.Sin(phase * 0.45f) * 70f;
                    beatSpeed = 14f;
                    break;

                case PredatorKind.Catfish:
                    if (above) targetVx = Mathf.Clamp((playerX - cachedTransform.position.x) * 1.2f, -trackingSpeed * 0.5f, trackingSpeed * 0.5f);
                    beatSpeed = 4.5f;
                    break;

                default:
                    if (above) targetVx = Mathf.Clamp((playerX - cachedTransform.position.x) * 2.2f, -trackingSpeed, trackingSpeed);
                    break;
            }

            // Own swimming speed on top of the river flow
            yPosition -= downwardVelocity * dt;
            Vector3 newPos = cachedTransform.position;
            newPos.y = yPosition;
            lateralVelocity = Mathf.MoveTowards(lateralVelocity, targetVx, SteerAccel * (kind == PredatorKind.Piranha ? 2f : 1f) * dt);
            newPos.x += lateralVelocity * dt;
            cachedTransform.position = newPos;

            // Swim cycle: tail beats from its base, body counter-sways, nose follows the travel direction.
            phase += dt * (beatSpeed + Mathf.Abs(downwardVelocity) * 0.025f);
            tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * TailSwing);
            float forward = Mathf.Max(30f, downwardVelocity + flowSpeed);
            float targetHeading = Mathf.Atan2(lateralVelocity, forward) * Mathf.Rad2Deg;
            heading = Mathf.Lerp(heading, targetHeading, 1f - Mathf.Exp(-6f * dt));
            float sway = -Mathf.Sin(phase - 0.9f) * BodySway;
            cachedTransform.rotation = Quaternion.Euler(0f, 0f, heading + sway + shiver);
        }
    }
}
