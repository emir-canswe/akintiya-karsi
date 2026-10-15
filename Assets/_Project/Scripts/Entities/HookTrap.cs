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
            lineShown = false;
            if (fishingLine == null) fishingLine = GetComponent<LineRenderer>();
            if (fishingLine != null)
            {
                // The line is hung from a fixed point at the top of the screen: set once per spawn, not per frame.
                fishingLine.positionCount = 2;
                fishingLine.SetPosition(0, new Vector3(startX, GameCamera.ViewHalfHeight + 40f, 0.1f));
            }
            UpdateLine(cachedTransform.position);
        }

        public override void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;
            base.Tick(dt, flowSpeed);

            Vector3 pos = cachedTransform.position;
            if (!isFlying)
            {
                phase += dt * 1.8f;
                pos.x = CurrentX;
                cachedTransform.position = pos;
            }
            UpdateLine(pos);
        }

        private bool lineShown;

        private void UpdateLine(Vector3 hookPos)
        {
            if (fishingLine == null) return;

            bool show = !isFlying;
            if (show != lineShown)
            {
                lineShown = show;
                fishingLine.enabled = show;
            }
            if (show) fishingLine.SetPosition(1, new Vector3(hookPos.x, hookPos.y + 12f, 0.1f));
        }
    }
}
