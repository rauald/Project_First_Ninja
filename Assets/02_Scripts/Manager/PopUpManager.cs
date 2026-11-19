using UnityEngine;

public sealed class PopUpManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _popUps = new GameObject[(int)PopUpType.Max];

    private GameObject _current;

    public void Open(PopUpType type)
    {
        int index = (int)type;
        if (index < 0 || index >= _popUps.Length)
        {
            return;
        }

        GameObject popUp = _popUps[index];
        if (popUp == null || popUp == _current)
        {
            return;
        }

        Close();
        _current = popUp;
        _current.SetActive(true);
    }

    public void Close()
    {
        if (_current != null)
        {
            _current.SetActive(false);
        }

        _current = null;
    }
}
