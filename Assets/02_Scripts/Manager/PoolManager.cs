using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PoolManager : Singleton<PoolManager>
{
    // 씬을 넘겨 공유하는 프리팹 풀만 싱글턴에서 보관한다.
    private readonly Dictionary<Component, object> _pools = new();

    public ObjectPool<T> GetPool<T>(T prefab) where T : Component, IPoolable
    {
        if (!_pools.TryGetValue(prefab, out object pool))
        {
            pool = new ObjectPool<T>(root =>
            {
                T instance = UnityEngine.Object.Instantiate(prefab, root);
                instance.name = prefab.name;
                return instance;
            }, transform);
            _pools.Add(prefab, pool);
        }
        return (ObjectPool<T>)pool;
    }

    // 전투와 UI 풀은 대상 씬의 루트 아래에 두어 씬과 함께 정리한다.
    public ObjectPool<T> CreateScenePool<T>(Transform root, Func<Transform, T> create, int prewarm)
        where T : Component
    {
        var pool = new ObjectPool<T>(create, root);
        pool.Prewarm(prewarm);
        return pool;
    }
}
