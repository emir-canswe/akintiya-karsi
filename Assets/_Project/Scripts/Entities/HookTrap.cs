using UnityEngine;
using Upstream.Core;

namespace Upstream.Entities
{
    /// <summary>
    /// Hook trap that sways left and right disguised as food.
    /// </summary>
    public class HookTrap : EntityBase
    {
        public override EntityType Type => EntityType.Hook;
        
        private float phase;
        private float startX;
        
        public float CurrentX => startX + Mathf.Sin(phase) * 10f;

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            phase = Random.Range(0f, Mathf.PI * 2f);
            startX = position.x;
        }

        public override void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;
            base.Tick(dt, flowSpeed);
            
            // Sway back and forth
            phase += dt * 2f;
            Vector3 pos = cachedTransform.position;
            pos.x = CurrentX;
            cachedTransform.position = pos;
        }

        /// <summary>
        /// Called when the hook is smashed by a turbo player.
        /// </summary>
        public void Smash()
        {
            isActive = false;
        }
    }
}
