using UnityEngine;

namespace Upstream.Entities
{
    /// <summary>
    /// Rolling wave across part of the river. Touching it doesn't hurt, it slows the river down for a moment;
    /// a turbo fish bursts straight through. Each wave counts once, then fades out. (No drop shadow: it's water.)
    /// </summary>
    public class WaveObstacle : EntityBase
    {
        public override EntityType Type => EntityType.Wave;

        public const float Height = 26f;

        public float width = 160f;
        /// <summary>Already touched (or burst): no further effect, fading out.</summary>
        public bool spent;

        private SpriteRenderer[] renderers;
        private float phase;
        private float fade;

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            spent = false;
            fade = 1f;
            phase = Random.Range(0f, Mathf.PI * 2f);
            cachedTransform.localScale = Vector3.one;
            if (renderers == null) renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                // Tiled center keeps the foam scallops round however wide the wave is.
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(width, 36f);
                Color c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }

        /// <summary>Marks the wave as used up; it fades away over the next moment.</summary>
        public void Spend() => spent = true;

        public override void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;
            base.Tick(dt, flowSpeed);
            if (!isActive) return;

            // Gentle swell: the band breathes a little.
            phase += dt * 4f;
            cachedTransform.localScale = new Vector3(1f, 1f + Mathf.Sin(phase) * 0.06f, 1f);

            if (spent && fade > 0f)
            {
                fade = Mathf.Max(0f, fade - dt * 2.5f);
                foreach (var sr in renderers)
                {
                    Color c = sr.color;
                    c.a = fade;
                    sr.color = c;
                }
            }
        }
    }
}
