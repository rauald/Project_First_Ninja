using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPool<T> where T : Component, IPoolable
{
    private readonly Stack<T> _available = new();
    private readonly HashSet<T> _rented = new();
    private readonly T _prefab;
    private readonly Transform _root;

    public ObjectPool(T prefab, Transform root)
    {
        _prefab = prefab;
        _root = root;
    }

    public T Rent(Vector3 position, Quaternion rotation)
    {
        T item = null;
        while (_available.Count > 0 && item == null)
        {
            item = _available.Pop();
        }

        if (item == null)
        {
            item = UnityEngine.Object.Instantiate(_prefab, _root);
        }

        item.transform.SetPositionAndRotation(position, rotation);
        _rented.Add(item);
        item.OnRent();
        item.gameObject.SetActive(true);
        return item;
    }

    public void Return(T item)
    {
        if (item == null || !_rented.Remove(item))
        {
            throw new InvalidOperationException("이 풀에서 대여한 오브젝트만 한 번 반납할 수 있습니다.");
        }

        item.OnReturn();
        item.gameObject.SetActive(false);
        _available.Push(item);
    }
}
