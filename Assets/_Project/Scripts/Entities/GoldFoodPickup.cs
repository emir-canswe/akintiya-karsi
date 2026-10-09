using UnityEngine;
using Upstream.Core;

namespace Upstream.Entities
{
    /// <summary>
    /// Gold food entity that heals the player.
    /// </summary>
    public class GoldFoodPickup : EntityBase
    {
        public override EntityType Type => EntityType.GoldFood;
        private float phase;
        private float baseX;

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            phase = Random.Range(0f, Mathf.PI * 2f);
            baseX = position.x;
        }

        public override void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;
            base.Tick(dt, flowSpeed);
            if (isFlying || !isActive || magnetized) return;

            // Small bobbing motion (absolute offset so it doesn't depend on frame rate)
            phase += dt * 3f;
            Vector3 pos = cachedTransform.position;
            pos.x = baseX + Mathf.Sin(phase) * 8f;
            cachedTransform.position = pos;
        }

        /// <summary>
        /// Called when player collects this gold food.
        /// </summary>
        public void Collect()
        {
            GameEvents.OnFoodEaten?.Invoke(true);
            isActive = false;
        }
    }
}
