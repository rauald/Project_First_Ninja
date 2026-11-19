using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPool<T> where T : Component, IPoolable
{
    private const int MAX_INSTANCES = 200;

    private readonly HashSet<Component> _instances;
    private readonly Stack<T> _available = new();
    private readonly HashSet<T> _rented = new();
    private readonly T _prefab;
    private readonly Transform _root;

    internal ObjectPool(T prefab, Transform root, HashSet<Component> instances)
    {
        _prefab = prefab;
        _root = root;
        _instances = instances;
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
            if (_instances.Count >= MAX_INSTANCES)
            {
                _instances.RemoveWhere(instance => instance == null);
                if (_instances.Count >= MAX_INSTANCES)
                {
                    return null;
                }
            }

            item = UnityEngine.Object.Instantiate(_prefab, _root);
            _instances.Add(item);
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
