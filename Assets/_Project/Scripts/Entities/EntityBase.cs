using UnityEngine;
using Upstream.Core;

namespace Upstream.Entities
{
    /// <summary>
    /// Type of the entity for collision and pooling purposes.
    /// </summary>
    public enum EntityType { Food, GoldFood, Rock, Log, Shark, Hook }

    /// <summary>
    /// Abstract base class for all river entities that scroll with the flow.
    /// </summary>
    public abstract class EntityBase : MonoBehaviour
    {
        public bool isActive;
        public float yPosition;
        protected Transform cachedTransform;

        public abstract EntityType Type { get; }

        protected virtual void Awake()
        {
            cachedTransform = transform;
        }

        /// <summary>
        /// Called when the entity is spawned from the pool.
        /// </summary>
        public virtual void OnSpawn(Vector2 position)
        {
            isActive = true;
            cachedTransform.position = position;
            yPosition = position.y;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Called when the entity is returned to the pool.
        /// </summary>
        public virtual void OnDespawn()
        {
            isActive = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates the entity position based on river flow.
        /// </summary>
        public virtual void Tick(float dt, float flowSpeed)
        {
            if (!isActive) return;

            // River flows downward, so Y increases over time (assuming top is negative Y and bottom is positive Y)
            yPosition += flowSpeed * dt;

            Vector3 pos = cachedTransform.position;
            pos.y = yPosition;
            cachedTransform.position = pos;

            // Despawn if it goes too far below screen
            if (yPosition > 600f)
            {
                isActive = false; // The SpawnManager will pool it in its next update
            }
        }
    }
}
