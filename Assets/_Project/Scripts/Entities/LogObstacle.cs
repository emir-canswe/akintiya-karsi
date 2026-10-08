using UnityEngine;
using Upstream.Core;

namespace Upstream.Entities
{
    /// <summary>
    /// Log obstacle that extends from a river bank.
    /// </summary>
    public class LogObstacle : EntityBase
    {
        public override EntityType Type => EntityType.Log;
        
        public float width;
        public float height;

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            width = Random.Range(80f, 150f);
            height = 20f;
            
            // Adjust local scale to represent width/height visually
            cachedTransform.localScale = new Vector3(width / 100f, height / 20f, 1f);
        }

        /// <summary>
        /// Called when the player smashes the log.
        /// </summary>
        public void Smash()
        {
            isActive = false;
        }
    }
}
