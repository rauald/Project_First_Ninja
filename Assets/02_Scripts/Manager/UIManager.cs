using UnityEngine;

public sealed class UIManager : Singleton<UIManager>
{
    [SerializeField] private WindowManager _window;
    [SerializeField] private PopUpManager _popUp;

    public WindowManager Window => _window;
    public PopUpManager PopUp => _popUp;
}
