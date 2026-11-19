using System.Collections.Generic;
using UnityEngine;

public sealed class PoolManager : Singleton<PoolManager>
{
    private readonly Dictionary<Component, object> _pools = new();
    private readonly HashSet<Component> _instances = new();

    public ObjectPool<T> GetPool<T>(T prefab) where T : Component, IPoolable
    {
        if (!_pools.TryGetValue(prefab, out object pool))
        {
            pool = new ObjectPool<T>(prefab, transform, _instances);
            _pools.Add(prefab, pool);
        }

        return (ObjectPool<T>)pool;
    }
}
