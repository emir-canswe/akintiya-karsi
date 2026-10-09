using System.Collections.Generic;
using UnityEngine;

namespace Upstream.Core
{
    /// <summary>
    /// A generic object pool for Unity Components with zero runtime GC allocation during gameplay.
    /// Plain C# class on purpose: Unity can't AddComponent a generic MonoBehaviour
    /// ("Generic MonoBehaviours are not supported!"), which returned null and aborted the bootstrap.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> pool;

        public ObjectPool(T prefabTemplate, int initialSize, Transform poolParent)
        {
            prefab = prefabTemplate;
            parent = poolParent;
            pool = new Queue<T>(Mathf.Max(1, initialSize));

            if (prefab == null) return;
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        public int AvailableCount => pool.Count;

        private void CreateNewObject()
        {
            T obj = Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
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
}
