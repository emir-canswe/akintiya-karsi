using UnityEngine;
using Upstream.Core;

namespace Upstream.Entities
{
    /// <summary>
    /// Spinning gold coin. Collected coins are paid into the wallet at the end of the run.
    /// </summary>
    public class CoinPickup : EntityBase
    {
        public override EntityType Type => EntityType.Coin;
        private float phase;

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            // Neighbouring coins spin slightly out of sync, which reads as a shimmering trail.
            phase = position.y * 0.05f;
            cachedTransform.localScale = Vector3.one;
        }

        public override void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;
            base.Tick(dt, flowSpeed);
            if (isFlying || !isActive) return;

            // Fake 3D spin: squash the width with |cos|, never fully flat so it doesn't vanish.
            phase += dt * 5f;
            float w = Mathf.Lerp(0.25f, 1f, Mathf.Abs(Mathf.Cos(phase)));
            cachedTransform.localScale = new Vector3(w, 1f, 1f);
        }

        public void Collect()
        {
            GameEvents.OnCoinCollected?.Invoke(1);
            isActive = false;
        }
    }
}
