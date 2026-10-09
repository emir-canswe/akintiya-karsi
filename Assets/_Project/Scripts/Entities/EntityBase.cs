using UnityEngine;
using Upstream.Core;
using Upstream.Camera;

namespace Upstream.Entities
{
    /// <summary>
    /// Type of the entity for collision and pooling purposes.
    /// </summary>
    public enum EntityType { Food, GoldFood, Rock, Log, Shark, Hook, Coin }

    /// <summary>
    /// Abstract base class for all river entities that scroll with the flow.
    /// In Unity 2D space: top of screen is +300, bottom is -300.
    /// River flows top-to-bottom, so yPosition decreases by flowSpeed * dt.
    /// </summary>
    public abstract class EntityBase : MonoBehaviour
    {
        public bool isActive;
        public bool isFlying;
        public float yPosition;
        protected Vector2 flyVelocity;
        protected float flyRotationSpeed;
        protected Transform cachedTransform;

        public abstract EntityType Type { get; }

        protected virtual void Awake()
        {
            cachedTransform = transform;
        }

        public virtual void OnSpawn(Vector2 position)
        {
            if (cachedTransform == null) cachedTransform = transform;
            isActive = true;
            isFlying = false;
            cachedTransform.position = new Vector3(position.x, position.y, 0f);
            cachedTransform.rotation = Quaternion.identity;
            yPosition = position.y;
            gameObject.SetActive(true);
        }

        public virtual void OnDespawn()
        {
            isActive = false;
            isFlying = false;
            gameObject.SetActive(false);
        }

        public virtual void Smash(float playerX = 0f)
        {
            if (isFlying) return;
            isFlying = true;
            float dir = cachedTransform.position.x >= playerX ? 1f : -1f;
            flyVelocity = new Vector2(dir * 320f, 260f);
            flyRotationSpeed = -dir * 460f;
        }

        public virtual void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;

            if (isFlying)
            {
                Vector3 fpos = cachedTransform.position;
                fpos.x += flyVelocity.x * dt;
                fpos.y += flyVelocity.y * dt;
                flyVelocity.y -= 900f * dt;
                cachedTransform.position = fpos;
                cachedTransform.Rotate(0f, 0f, flyRotationSpeed * dt);
                yPosition = fpos.y;

                float h = GameCamera.ViewHalfHeight;
                if (yPosition < -h - 150f || yPosition > h + 250f || Mathf.Abs(fpos.x) > 420f)
                {
                    isActive = false;
                }
                return;
            }

            // River flows downward in Unity 2D space (+Y top -> -Y bottom)
            yPosition -= flowSpeed * dt;

            Vector3 pos = cachedTransform.position;
            pos.y = yPosition;
            cachedTransform.position = pos;

            if (yPosition < -GameCamera.ViewHalfHeight - 100f)
            {
                isActive = false;
            }
        }
    }
}
