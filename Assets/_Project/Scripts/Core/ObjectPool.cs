using System.Collections.Generic;
using UnityEngine;

namespace Upstream.Core
{
    /// <summary>
    /// A generic object pool for Unity Components with zero runtime GC allocation during gameplay.
    /// </summary>
    public class ObjectPool<T> : MonoBehaviour where T : Component
    {
        [SerializeField] private T prefab;
        [SerializeField] private int initialSize = 12;
        [SerializeField] private Transform parent;

        private readonly Queue<T> pool = new Queue<T>();

        public void Configure(T prefabTemplate, int size = 12, Transform poolParent = null)
        {
            prefab = prefabTemplate;
            initialSize = size;
            parent = poolParent != null ? poolParent : transform;
            Initialize();
        }

        public void Initialize()
        {
            if (prefab == null) return;
            if (parent == null) parent = transform;
            for (int i = pool.Count; i < initialSize; i++)
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

        public void Return(T obj)
        {
            if (obj == null) return;
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public class ObjectPoolManager : MonoBehaviour
    {
        private readonly Dictionary<string, object> pools = new Dictionary<string, object>();

        public void RegisterPool<T>(string name, ObjectPool<T> pool) where T : Component
        {
            pools[name] = pool;
        }

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
