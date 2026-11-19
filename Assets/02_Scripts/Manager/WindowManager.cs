using System.Collections;
using UnityEngine;

public sealed class WindowManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _windows = new GameObject[(int)WindowType.Max];
    [SerializeField, Min(0f)] private float _slideDuration = 0.35f;
    [SerializeField] private CanvasGroup _inputGroup;

    private GameObject _current;
    private bool _isSliding;

    public void Open(WindowType type)
    {
        int index = (int)type;
        if (_isSliding || index < 0 || index >= _windows.Length)
        {
            return;
        }

        GameObject window = _windows[index];
        if (window == null || window == _current)
        {
            return;
        }

        Vector2 entryDirection = GetEntryDirection(type);
        if (entryDirection != Vector2.zero && _current == _windows[(int)WindowType.Lobby])
        {
            StartCoroutine(SlideToWindow(window, entryDirection));
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

    private static Vector2 GetEntryDirection(WindowType type)
    {
        switch (type)
        {
            case WindowType.Shop:
            case WindowType.Equipment:
                return Vector2.left;
            case WindowType.StageSelect:
                return Vector2.down;
            case WindowType.Character:
            case WindowType.Talent:
                return Vector2.right;
            default:
                return Vector2.zero;
        }
    }

    private IEnumerator SlideToWindow(GameObject window, Vector2 entryDirection)
    {
        _isSliding = true;
        _inputGroup.blocksRaycasts = false;
        _inputGroup.interactable = false;

        RectTransform previous = (RectTransform)_current.transform;
        RectTransform next = (RectTransform)window.transform;
        Vector2 previousOrigin = previous.anchoredPosition;
        Vector2 nextOrigin = Vector2.zero;
        RectTransform canvas = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;

        // 진입 방향에 따라 전체 화면의 너비 또는 높이만큼 이동한다.
        float elapsed = 0f;
        Vector2 offset = Vector2.Scale(entryDirection, canvas.rect.size);
        next.anchoredPosition = nextOrigin + offset;
        window.SetActive(true);
        window.GetComponent<IWindowable>()?.ResetUI();
        while (elapsed < _slideDuration)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / _slideDuration);
            offset = Vector2.Scale(entryDirection, canvas.rect.size);
            previous.anchoredPosition = previousOrigin - offset * progress;
            next.anchoredPosition = nextOrigin + offset * (1f - progress);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }

        offset = Vector2.Scale(entryDirection, canvas.rect.size);
        previous.anchoredPosition = previousOrigin - offset;
        next.anchoredPosition = nextOrigin;
        _current.SetActive(false);
        previous.anchoredPosition = previousOrigin;
        _current = window;
        _inputGroup.blocksRaycasts = true;
        _inputGroup.interactable = true;
        _isSliding = false;
    }
}
