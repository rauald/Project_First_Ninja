using UnityEngine;

public sealed class WindowManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _windows = new GameObject[(int)WindowType.Max];

    private GameObject _current;

    public void Open(WindowType type)
    {
        int index = (int)type;
        if (index < 0 || index >= _windows.Length)
        {
            return;
        }

        GameObject window = _windows[index];
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
