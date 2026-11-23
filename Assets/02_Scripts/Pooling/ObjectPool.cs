using System;
using System.Collections.Generic;
using UnityEngine;

// 한 종류의 오브젝트를 최대 200개까지 보유하고 대여 상태를 관리한다.
public sealed class ObjectPool<T> where T : Component
{
    public const int MAX_INSTANCES = 200;
    private readonly Func<Transform, T> _create;
    private readonly Transform _root;
    private readonly Stack<T> _available = new();
    private readonly HashSet<T> _rented = new();

    internal ObjectPool(Func<Transform, T> create, Transform root)
    {
        _create = create;
        _root = root;
    }

    public int AvailableCount => _available.Count;
    public int RentedCount => _rented.Count;

    // 전투 시작 전에 목표 수량까지 생성해 비활성화한다.
    public void Prewarm(int count)
    {
        int target = Mathf.Clamp(count, 0, MAX_INSTANCES);
        while (_available.Count + _rented.Count < target)
        {
            _available.Push(Create());
        }
    }

    // 재사용 가능한 인스턴스를 우선 사용하고 풀별 상한에서 대여 실패를 알린다.
    public T Rent(Vector3 position, Quaternion rotation)
    {
        T item = null;
        while (_available.Count > 0 && item == null)
        {
            item = _available.Pop();
        }
        if (item == null)
        {
            if (_rented.Count >= MAX_INSTANCES)
            {
                _rented.RemoveWhere(rented => rented == null);
                if (_rented.Count >= MAX_INSTANCES)
                {
                    return null;
                }
            }
            item = Create();
        }

        item.transform.SetPositionAndRotation(position, rotation);
        _rented.Add(item);
        (item as IPoolable)?.OnRent();
        item.gameObject.SetActive(true);
        return item;
    }

    // 다른 풀의 객체나 두 번 반납하는 호출을 거부한다.
    public void Return(T item)
    {
        if (item == null || !_rented.Remove(item))
        {
            throw new InvalidOperationException("이 풀에서 대여한 오브젝트만 한 번 반납할 수 있습니다.");
        }

        Store(item);
    }

    // 재도전 전에 현재 대여분을 모두 회수한다.
    public void ReturnAll()
    {
        foreach (T item in _rented)
        {
            if (item != null)
            {
                Store(item);
            }
        }
        _rented.Clear();
    }

    private T Create()
    {
        T item = _create(_root);
        item.gameObject.SetActive(false);
        return item;
    }

    private void Store(T item)
    {
        (item as IPoolable)?.OnReturn();
        item.gameObject.SetActive(false);
        _available.Push(item);
    }
}
