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
        private SpriteRenderer[] renderers;

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            if (width <= 0f) width = 140f;
            height = 22f;
            // 9-sliced: the cut ends keep their shape at any length (the drop shadow too).
            cachedTransform.localScale = Vector3.one;
            if (renderers == null) renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(width, 27f);
            }
        }
    }
}
