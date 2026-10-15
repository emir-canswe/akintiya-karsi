using UnityEngine;
using Upstream.Core;
using Upstream.Data;
using Upstream.River;

namespace Upstream.Entities
{
    /// <summary>
    /// A river predator (shark, pike, piranha, catfish or one of the late hunters) swimming downstream at the player. All kinds swim like
    /// real fish: the tail beats from its base, the body counter-sways and the nose turns into the direction of
    /// travel. Each kind moves differently:
    /// shark chases steadily; pike drifts, shivers as a warning, then lunges; piranhas zigzag in schools;
    /// catfish are slow but big (only a very big fish can eat one).
    /// The hunters of the late river: barracuda are the fastest and home in hard; swordfish wind up longer and charge
    /// almost straight (dash sideways to dodge); anglerfish are huge, slow and wide.
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
        private const float PikeTriggerDistance = 300f;
        private const float PikeWindup = 0.4f;
        /// <summary>Lunge speed (own swimming, on top of the river) at difficulty 0 and 1.</summary>
        private const float PikeLungeEasy = 230f, PikeLungeHard = 330f;

        // Hit circles along the body (offsets in local Y) and their radius, per kind, before scaling.
        private static readonly float[] SharkParts = { -16f, 4f, 24f };
        private static readonly float[] PikeParts = { -32f, -12f, 8f, 26f };
        private static readonly float[] PiranhaParts = { -6f, 6f };
        private static readonly float[] CatfishParts = { -18f, 2f, 20f };
        private static readonly float[] SwordfishParts = { -42f, -28f, -10f, 8f, 24f };
        private const float SwordfishWindup = 0.55f;
        private const float SwordfishTriggerDistance = 380f;

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

        public float[] HitOffsets
        {
            get
            {
                switch (kind)
                {
                    case PredatorKind.Pike:
                    case PredatorKind.Barracuda: return PikeParts;
                    case PredatorKind.Piranha: return PiranhaParts;
                    case PredatorKind.Catfish:
                    case PredatorKind.Anglerfish: return CatfishParts;
                    case PredatorKind.Swordfish: return SwordfishParts;
                    default: return SharkParts;
                }
            }
        }

        public float HitRadius
        {
            get
            {
                switch (kind)
                {
                    case PredatorKind.Pike:
                    case PredatorKind.Piranha: return 11f;
                    case PredatorKind.Barracuda:
                    case PredatorKind.Swordfish: return 10f;
                    case PredatorKind.Catfish: return 21f;
                    case PredatorKind.Anglerfish: return 22f;
                    default: return 16f;
                }
            }
        }

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
                case PredatorKind.Barracuda: cruiseSpeed = spd * 1.25f; break;
                case PredatorKind.Swordfish: cruiseSpeed = spd * 0.45f; break;
                case PredatorKind.Anglerfish: cruiseSpeed = spd * 0.6f; break;
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
            // Transform reads are engine calls: look at the predator and the fish once per tick.
            Vector3 myPos = cachedTransform.position;
            Vector3 playerPos = playerTransform != null ? playerTransform.position : myPos;
            float playerX = playerPos.x;
            bool above = playerTransform != null && yPosition > playerPos.y + 40f;
            float targetVx = 0f;
            float beatSpeed = 7f;
            float shiver = 0f;

            switch (kind)
            {
                case PredatorKind.Pike:
                case PredatorKind.Swordfish:
                    bool sword = kind == PredatorKind.Swordfish;
                    if (pikeTimer < 0f)
                    {
                        // Cruise slowly, stalking: lines up with the player like a shark does
                        if (above) targetVx = Mathf.Clamp((playerX - myPos.x) * 2f, -trackingSpeed, trackingSpeed);
                        if (playerTransform != null && yPosition - playerPos.y < (sword ? SwordfishTriggerDistance : PikeTriggerDistance)) pikeTimer = 0f;
                    }
                    else if (pikeTimer < (sword ? SwordfishWindup : PikeWindup))
                    {
                        // Wind-up: nearly stops and shivers (the warning), aiming at the player
                        pikeTimer += dt;
                        downwardVelocity = Mathf.MoveTowards(downwardVelocity, -flowSpeed * 0.6f, 900f * dt);
                        targetVx = Mathf.Clamp((playerX - myPos.x) * 3f, -trackingSpeed * 1.4f, trackingSpeed * 1.4f);
                        shiver = Mathf.Sin(Time.time * 60f) * 6f;
                        beatSpeed = 24f;
                    }
                    else
                    {
                        // Lunge: fast, still bending a little toward the player (a quick sideways dash dodges it)
                        float lunge = Mathf.Lerp(PikeLungeEasy, PikeLungeHard, Mathf.Clamp01(difficulty)) * (sword ? 1.4f : 1f);
                        downwardVelocity = Mathf.MoveTowards(downwardVelocity, lunge, 1800f * dt);
                        // A swordfish barely steers once it charges: a sideways dash is the way out.
                        float steer = sword ? 0.3f : 0.9f;
                        if (above) targetVx = Mathf.Clamp((playerX - myPos.x) * 1.5f, -trackingSpeed * steer, trackingSpeed * steer);
                        beatSpeed = 20f;
                    }
                    break;

                case PredatorKind.Barracuda:
                    // The fastest swimmer: homes in hard and tight, tail whipping
                    if (above) targetVx = Mathf.Clamp((playerX - myPos.x) * 3f, -trackingSpeed * 1.5f, trackingSpeed * 1.5f);
                    beatSpeed = 18f;
                    break;

                case PredatorKind.Anglerfish:
                    // Huge and slow: drifts toward the player with lazy, heavy tail beats
                    if (above) targetVx = Mathf.Clamp((playerX - myPos.x) * 1.4f, -trackingSpeed * 0.8f, trackingSpeed * 0.8f);
                    beatSpeed = 5f;
                    break;

                case PredatorKind.Piranha:
                    // Quick, nervous zigzag while homing in
                    if (above) targetVx = Mathf.Clamp((playerX - myPos.x) * 2.5f, -trackingSpeed * 1.2f, trackingSpeed * 1.2f);
                    targetVx += Mathf.Sin(phase * 0.45f) * 70f;
                    beatSpeed = 14f;
                    break;

                case PredatorKind.Catfish:
                    if (above) targetVx = Mathf.Clamp((playerX - myPos.x) * 1.2f, -trackingSpeed * 0.5f, trackingSpeed * 0.5f);
                    beatSpeed = 4.5f;
                    break;

                default:
                    if (above) targetVx = Mathf.Clamp((playerX - myPos.x) * 2.2f, -trackingSpeed, trackingSpeed);
                    break;
            }

            // Own swimming speed on top of the river flow
            yPosition -= downwardVelocity * dt;
            Vector3 newPos = myPos;
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
