using UnityEngine;
using UnityEngine.UI;

// 씬 전환 동안 화면을 덮고 실제 시간으로 표시를 회전시킨다.
internal sealed class SceneLoadingOverlay : MonoBehaviour
{
    [SerializeField] private Image _backdrop;
    [SerializeField] private RectTransform _spinner;
    [SerializeField] private Image _spinnerImage;
    [SerializeField] private GameObject _spinnerSpokes;

    private Quaternion _initialSpinnerRotation;

    private void Awake()
    {
        _initialSpinnerRotation = _spinner.localRotation;
    }

    internal void Show(SceneLoadingArtwork artwork)
    {
        if (_backdrop.sprite == null && artwork != null && artwork.Backdrop != null)
        {
            _backdrop.sprite = artwork.Backdrop;
        }
        _backdrop.gameObject.SetActive(_backdrop.sprite != null);

        if (_spinnerImage.sprite == null && artwork != null)
        {
            _spinnerImage.sprite = artwork.Spinner;
        }

        bool hasSpinner = _spinnerImage.sprite != null;
        _spinnerImage.enabled = hasSpinner;
        _spinnerSpokes.SetActive(!hasSpinner);
        gameObject.SetActive(true);
        _spinner.localRotation = _initialSpinnerRotation;
    }

    private void Update()
    {
        _spinner.Rotate(0f, 0f, -310f * Time.unscaledDeltaTime);
    }
}
