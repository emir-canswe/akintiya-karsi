using UnityEngine;
using Upstream.Camera;

namespace Upstream.Entities
{
    /// <summary>
    /// Hook trap that sways left and right disguised as food, with a fishing line up to the top of screen.
    /// </summary>
    public class HookTrap : EntityBase
    {
        public override EntityType Type => EntityType.Hook;

        private float phase;
        private float startX;
        private LineRenderer fishingLine;

        public float CurrentX => startX + Mathf.Sin(phase) * 12f;

        protected override void Awake()
        {
            base.Awake();
            fishingLine = GetComponent<LineRenderer>();
        }

        public override void OnSpawn(Vector2 position)
        {
            base.OnSpawn(position);
            phase = Random.Range(0f, Mathf.PI * 2f);
            startX = position.x;
            UpdateLine();
        }

        public override void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;
            base.Tick(dt, flowSpeed);

            if (!isFlying)
            {
                phase += dt * 1.8f;
                Vector3 pos = cachedTransform.position;
                pos.x = CurrentX;
                cachedTransform.position = pos;
            }
            UpdateLine();
        }

        private void UpdateLine()
        {
            if (fishingLine == null) fishingLine = GetComponent<LineRenderer>();
            if (fishingLine != null)
            {
                fishingLine.enabled = !isFlying;
                if (!isFlying)
                {
                    fishingLine.positionCount = 2;
                    fishingLine.SetPosition(0, new Vector3(startX, GameCamera.ViewHalfHeight + 40f, 0.1f));
                    fishingLine.SetPosition(1, new Vector3(cachedTransform.position.x, cachedTransform.position.y + 12f, 0.1f));
                }
            }
        }
    }
}
