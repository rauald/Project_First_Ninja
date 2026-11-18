using UnityEngine;

public sealed class PopUpManager : MonoBehaviour
{
    private GameObject _current;

    public void Open(GameObject popUp)
    {
        Close();
        _current = popUp;
        if (_current != null)
        {
            _current.SetActive(true);
        }
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
