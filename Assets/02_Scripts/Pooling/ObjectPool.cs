using System;
using System.Collections.Generic;
using UnityEngine;

// 재사용 가능한 오브젝트와 대여 상태를 관리한다.
public sealed class ObjectPool<T> where T : Component
{
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
        while (_available.Count + _rented.Count < count)
        {
            _available.Push(Create());
        }
    }

    // 재사용 가능한 인스턴스가 모두 사용 중일 때만 추가한다.
    public T Rent(Vector3 position, Quaternion rotation)
    {
        T item = null;
        while (_available.Count > 0 && item == null)
        {
            item = _available.Pop();
        }
        if (item == null)
        {
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
