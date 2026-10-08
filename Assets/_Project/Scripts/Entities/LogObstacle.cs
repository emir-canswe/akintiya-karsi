using UnityEngine;

namespace Upstream.Entities
{
    /// <summary>
    /// Log obstacle that extends from a river bank.
    /// </summary>
    public class LogObstacle : EntityBase
    {
        public override EntityType Type => EntityType.Log;

        public float width = 140f;
        public float height = 22f;

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            if (width <= 0f) width = 140f;
            height = 22f;
            cachedTransform.localScale = new Vector3(width / 120f, 1f, 1f);
        }
    }
}
