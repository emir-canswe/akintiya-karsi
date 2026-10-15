using UnityEngine;
using Upstream.River;

namespace Upstream.Entities
{
    /// <summary>
    /// Rolling wave across part of the river. Touching it doesn't hurt, it slows the river down for a moment;
    /// a turbo fish bursts straight through. Each wave counts once, then fades out. (No drop shadow: it's water.)
    /// A big wave (<see cref="isBig"/>) spans the whole river, comes in sets when the river hits its speed limit
    /// and takes some speed away for good; it rises as it rolls in, its foam churns and it throws spray.
    /// </summary>
    public class WaveObstacle : EntityBase
    {
        public override EntityType Type => EntityType.Wave;

        public const float Height = 26f;

        public float width = 160f;
        /// <summary>Set before OnSpawn: a river-wide wave of a speed-limit set.</summary>
        public bool isBig;
        /// <summary>Already touched (or burst): no further effect, fading out.</summary>
        public bool spent;

        // Big wave crest (foam) position and hit band, local to the wave's center.
        private const float BigCrestY = -22f;
        private const float BigHitHalfHeight = 12f;
        private const int SprayCount = 22;
        private const float RiseTime = 0.8f;

        /// <summary>World Y of the band that touches the fish.</summary>
        public float HitCenterY => yPosition + (isBig ? BigCrestY : 0f);
        public float HitHalfHeight => isBig ? BigHitHalfHeight : Height * 0.5f;

        private SpriteRenderer body;
        private Sprite smallSprite;
        private float phase;
        private float fade;
        private float rise;

        // Big wave parts, built the first time this pooled object is used as a big wave.
        private GameObject bigParts;
        private SpriteRenderer foamFront, foamBack;
        private Transform[] spray;
        private SpriteRenderer[] sprayRenderers;
        private Vector2[] sprayVelocity;
        private float[] sprayLife, sprayMaxLife, spraySize;

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            spent = false;
            fade = 1f;
            rise = isBig ? 0f : 1f;
            phase = Random.Range(0f, Mathf.PI * 2f);
            cachedTransform.localScale = Vector3.one;

            if (body == null)
            {
                body = GetComponent<SpriteRenderer>();
                smallSprite = body.sprite;
            }
            // Tiled center keeps the foam scallops round however wide the wave is.
            body.drawMode = SpriteDrawMode.Tiled;
            body.tileMode = SpriteTileMode.Continuous;
            if (isBig)
            {
                Sprite big = CartoonSpriteFactory.GetBigWave();
                body.sprite = big;
                body.size = new Vector2(width, big.bounds.size.y);
                EnsureBigParts();
                bigParts.SetActive(true);
                float foamH = foamFront.sprite.bounds.size.y;
                foamFront.size = new Vector2(width + 4f, foamH);
                foamBack.size = new Vector2(width + 4f, foamH);
                for (int i = 0; i < SprayCount; i++) LaunchDroplet(i, Random.Range(-width * 0.5f, width * 0.5f), false, Random.value);
            }
            else
            {
                body.sprite = smallSprite;
                body.size = new Vector2(width, 36f);
                if (bigParts != null) bigParts.SetActive(false);
            }
            ApplyAlpha(1f);
        }

        /// <summary>
        /// Builds the parts of a big wave (foam layers, 22 spray drops) ahead of time and leaves them hidden:
        /// creating that many objects when the first set rolls in would hitch the run.
        /// </summary>
        public void PrewarmBigParts()
        {
            if (body == null)
            {
                body = GetComponent<SpriteRenderer>();
                if (body == null) return;
                smallSprite = body.sprite;
            }
            EnsureBigParts();
            bigParts.SetActive(false);
        }

        /// <summary>Marks the wave as used up; it fades away over the next moment.</summary>
        public void Spend() => spent = true;

        /// <summary>Big wave breaking over the fish at <paramref name="atX"/>: a burst of spray, then it fades.</summary>
        public void Spend(float atX)
        {
            spent = true;
            if (!isBig || spray == null) return;
            float localX = atX - cachedTransform.position.x;
            for (int i = 0; i < SprayCount; i++) LaunchDroplet(i, localX + Random.Range(-70f, 70f), true, 0f);
        }

        public override void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;
            base.Tick(dt, flowSpeed);
            if (!isActive) return;

            if (isBig)
            {
                // Rises up as it rolls in, then heaves.
                rise = Mathf.Min(1f, rise + dt / RiseTime);
                float r = 1f - (1f - rise) * (1f - rise);
                phase += dt * 3.2f;
                float sy = Mathf.Lerp(0.35f, 1f, r) * (1f + Mathf.Sin(phase) * 0.05f);
                cachedTransform.localScale = new Vector3(1f, sy, 1f);

                // Foam churns: two layers sway against each other and bob on the crest.
                foamFront.transform.localPosition = new Vector3(Mathf.Sin(phase * 1.7f) * 7f, BigCrestY + Mathf.Sin(phase * 2.3f) * 1.5f, 0f);
                foamBack.transform.localPosition = new Vector3(-Mathf.Sin(phase * 1.3f + 1f) * 6f, BigCrestY + 6f + Mathf.Cos(phase * 2f) * 1.5f, 0f);
                TickSpray(dt);
            }
            else
            {
                // Gentle swell: the band breathes a little.
                phase += dt * 4f;
                cachedTransform.localScale = new Vector3(1f, 1f + Mathf.Sin(phase) * 0.06f, 1f);
            }

            if (spent && fade > 0f)
            {
                // A big wave keeps rolling on past the fish for a moment before it dies down.
                fade = Mathf.Max(0f, fade - dt * (isBig ? 0.9f : 2.5f));
                ApplyAlpha(fade);
            }
            else if (isBig && rise < 1f)
            {
                ApplyAlpha(Mathf.Clamp01(rise * 2f));
            }
        }

        private void ApplyAlpha(float a)
        {
            SetAlpha(body, a);
            if (!isBig || bigParts == null) return;
            SetAlpha(foamFront, a);
            SetAlpha(foamBack, a * 0.75f);
        }

        private static void SetAlpha(SpriteRenderer sr, float a)
        {
            if (sr == null) return;
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }

        private void TickSpray(float dt)
        {
            for (int i = 0; i < SprayCount; i++)
            {
                sprayLife[i] -= dt;
                if (sprayLife[i] <= 0f)
                {
                    // Spent waves stop throwing spray: the last drops just fade.
                    if (spent) { sprayRenderers[i].enabled = false; continue; }
                    LaunchDroplet(i, Random.Range(-width * 0.5f, width * 0.5f), false, 0f);
                }

                // Thrown off the crest downstream, slowing down as it falls back.
                sprayVelocity[i].y += 90f * dt;
                Vector3 p = spray[i].localPosition;
                p.x += sprayVelocity[i].x * dt;
                p.y += sprayVelocity[i].y * dt;
                spray[i].localPosition = p;

                float t = 1f - sprayLife[i] / sprayMaxLife[i]; // 0 at launch .. 1 at the end
                float s = spraySize[i] * Mathf.Sin(Mathf.Lerp(0.25f, 1f, t) * Mathf.PI) / 0.64f;
                spray[i].localScale = new Vector3(s, s, 1f);
                Color c = sprayRenderers[i].color;
                c.a = (1f - t * t) * 0.9f * fade;
                sprayRenderers[i].color = c;
            }
        }

        /// <param name="age">0..1: start part-way through its life (spreads the first droplets out).</param>
        private void LaunchDroplet(int i, float localX, bool burst, float age)
        {
            float maxLife = burst ? Random.Range(0.5f, 0.8f) : Random.Range(0.45f, 0.85f);
            sprayMaxLife[i] = maxLife;
            sprayLife[i] = maxLife * (1f - age);
            spraySize[i] = burst ? Random.Range(7f, 12f) : Random.Range(4.5f, 8f);
            sprayVelocity[i] = burst
                ? new Vector2(Random.Range(-100f, 100f), Random.Range(-190f, -90f))
                : new Vector2(Random.Range(-22f, 22f), Random.Range(-85f, -40f));
            // From the front of the foam, so the drops show against the water below it.
            spray[i].localPosition = new Vector3(Mathf.Clamp(localX, -width * 0.5f, width * 0.5f), BigCrestY - 8f + Random.Range(-3f, 3f), 0f);
            sprayRenderers[i].enabled = true;
        }

        private void EnsureBigParts()
        {
            if (bigParts != null) return;
            bigParts = new GameObject("BigWaveParts");
            bigParts.transform.SetParent(transform, false);
            int order = body.sortingOrder;

            Sprite foam = CartoonSpriteFactory.GetFoamStrip();
            foamBack = AddPart("FoamBack", foam, order + 1, new Color(0.9f, 0.97f, 1f, 0.75f));
            foamFront = AddPart("FoamFront", foam, order + 2, Color.white);
            foreach (var f in new[] { foamBack, foamFront })
            {
                f.drawMode = SpriteDrawMode.Tiled;
                f.tileMode = SpriteTileMode.Continuous;
            }

            spray = new Transform[SprayCount];
            sprayRenderers = new SpriteRenderer[SprayCount];
            sprayVelocity = new Vector2[SprayCount];
            sprayLife = new float[SprayCount];
            sprayMaxLife = new float[SprayCount];
            spraySize = new float[SprayCount];
            Sprite drop = WatercolorSpriteFactory.GetCircleSprite();
            for (int i = 0; i < SprayCount; i++)
            {
                // Above the fish (sorting 10): the spray splashes over it as the wave breaks.
                sprayRenderers[i] = AddPart("Spray", drop, 12, new Color(1f, 1f, 1f, 0.9f));
                spray[i] = sprayRenderers[i].transform;
            }
        }

        private SpriteRenderer AddPart(string name, Sprite sprite, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bigParts.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = color;
            return sr;
        }
    }
}
