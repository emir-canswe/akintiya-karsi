using UnityEngine;

namespace Upstream.Entities
{
    /// <summary>
    /// Stationary rock obstacle in the river.
    /// </summary>
    public class RockObstacle : EntityBase
    {
        public override EntityType Type => EntityType.Rock;

        public float radius = 24f;
        public bool isSmall;

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            radius = Random.Range(20f, 28f);
            isSmall = radius < 23f;
            cachedTransform.localScale = Vector3.one * (radius / 24f);
        }
    }
}
