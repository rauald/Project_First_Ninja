using UnityEngine;
using UnityEngine.UI;

public sealed class UIManager : Singleton<UIManager>
{
    [SerializeField] private Text _titleStatus;
    [SerializeField] private TitleScreen _title;
    [SerializeField] private WindowManager _window;
    [SerializeField] private PopUpManager _popUp;

    public TitleScreen Title => _title;

    public WindowManager Window => _window;
    public PopUpManager PopUp => _popUp;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        SetTitleStatus(null);
    }

    public void SetTitleStatus(string message)
    {
        _titleStatus.text = message;
        _titleStatus.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

}
