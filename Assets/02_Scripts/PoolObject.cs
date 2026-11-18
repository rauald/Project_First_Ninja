using UnityEngine;
using UnityEngine.Events;

public sealed class PoolObject : MonoBehaviour, IPoolable
{
    [SerializeField] private UnityEvent _onRent;
    [SerializeField] private UnityEvent _onReturn;

    public void OnRent()
    {
        _onRent?.Invoke();
    }

    public void OnReturn()
    {
        _onReturn?.Invoke();
    }
}
