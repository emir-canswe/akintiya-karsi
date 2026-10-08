using UnityEngine;
using Upstream.Core;

namespace Upstream.Entities
{
    /// <summary>
    /// Stationary rock obstacle in the river.
    /// </summary>
    public class RockObstacle : EntityBase
    {
        public override EntityType Type => EntityType.Rock;
        
        public float radius;
        public bool isSmall;

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            radius = Random.Range(20f, 28f);
            isSmall = radius < 22f;
            
            // Set scale based on random radius
            cachedTransform.localScale = Vector3.one * (radius / 24f);
        }
        
        /// <summary>
        /// Called when the player smashes the rock (e.g. during turbo).
        /// </summary>
        public void Smash()
        {
            isActive = false; // Could add particle/flyaway here
        }
    }
}
