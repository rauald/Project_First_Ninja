using UnityEngine;

public sealed class WindowManager : MonoBehaviour
{
    private GameObject _current;

    public void Open(GameObject window)
    {
        if (window == null || window == _current)
        {
            return;
        }

        if (_current != null)
        {
            _current.SetActive(false);
        }

        _current = window;
        _current.SetActive(true);
        _current.GetComponent<IWindowable>()?.ResetUI();
    }
}
