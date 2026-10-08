using System.Collections.Generic;
using UnityEngine;

namespace Upstream.Core
{
    /// <summary>
    /// A generic object pool for Unity Components.
    /// </summary>
    public class ObjectPool<T> : MonoBehaviour where T : Component
    {
        [SerializeField] private T prefab;
        [SerializeField] private int initialSize = 10;
        [SerializeField] private Transform parent;

        private Queue<T> pool = new Queue<T>();

        /// <summary>
        /// Initializes the pool with the initial size.
        /// </summary>
        public void Initialize()
        {
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        private T CreateNewObject()
        {
            T obj = Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
            return obj;
        }

        /// <summary>
        /// Gets an object from the pool, activating it.
        /// </summary>
        public T Get()
        {
            if (pool.Count == 0)
            {
                CreateNewObject();
            }

            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Returns an object to the pool, deactivating it.
        /// </summary>
        public void Return(T obj)
        {
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// Manager for holding references to named object pools.
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        private Dictionary<string, object> pools = new Dictionary<string, object>();

        /// <summary>
        /// Registers a pool with a specific name.
        /// </summary>
        public void RegisterPool<T>(string name, ObjectPool<T> pool) where T : Component
        {
            if (!pools.ContainsKey(name))
            {
                pools.Add(name, pool);
            }
        }

        /// <summary>
        /// Gets a registered pool by name.
        /// </summary>
        public ObjectPool<T> GetPool<T>(string name) where T : Component
        {
            if (pools.TryGetValue(name, out object pool))
            {
                return pool as ObjectPool<T>;
            }
            return null;
        }
    }
}
