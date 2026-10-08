using UnityEngine;
using Upstream.Core;

namespace Upstream.Entities
{
    /// <summary>
    /// Normal food entity that fills the power bar.
    /// </summary>
    public class FoodPickup : EntityBase
    {
        public override EntityType Type => EntityType.Food;
        private float phase;

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        public override void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;
            base.Tick(dt, flowSpeed);
            
            // Small bobbing motion
            phase += dt * 3f;
            Vector3 pos = cachedTransform.position;
            pos.x += Mathf.Sin(phase) * 0.5f;
            cachedTransform.position = pos;
        }

        /// <summary>
        /// Called when player collects this food.
        /// </summary>
        public void Collect()
        {
            GameEvents.OnFoodEaten?.Invoke(false);
            isActive = false;
        }
    }
}
