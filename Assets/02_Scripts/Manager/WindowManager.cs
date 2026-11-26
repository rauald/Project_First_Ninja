using System.Collections;
using UnityEngine;

public sealed class WindowManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _windows = new GameObject[(int)WindowType.Max];
    [SerializeField, Min(0f)] private float _slideDuration = 0.35f;
    [SerializeField] private CanvasGroup _inputGroup;

    private GameObject _current;
    private bool _isSliding;

    internal void PrepareLobby()
    {
        _windows[(int)WindowType.Equipment].GetComponent<EquipmentWindow>().PrepareInventorySlots();
    }

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

        WindowType previousType = (WindowType)System.Array.IndexOf(_windows, _current);
        Vector2 entryDirection = GetEntryDirection(previousType, type);
        if (GetMenuIndex(type) >= 0 && GetMenuIndex(previousType) >= 0)
        {
            SoundManager.Instance?.PlayConfirm();
        }
        if (_current != null && entryDirection != Vector2.zero)
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

    private static Vector2 GetEntryDirection(WindowType previous, WindowType next)
    {
        int previousIndex = GetMenuIndex(previous);
        int nextIndex = GetMenuIndex(next);
        if (previousIndex < 0 || nextIndex < 0)
        {
            return Vector2.zero;
        }

        if (nextIndex != previousIndex)
        {
            return nextIndex < previousIndex ? Vector2.left : Vector2.right;
        }

        // 로비와 던전은 같은 중앙 메뉴다. 두 화면 사이에는 세로 전환을 유지한다.
        if (previous == WindowType.Lobby && next == WindowType.StageSelect)
        {
            return Vector2.down;
        }

        if (previous == WindowType.StageSelect && next == WindowType.Lobby)
        {
            return Vector2.up;
        }

        return Vector2.zero;
    }

    private static int GetMenuIndex(WindowType type)
    {
        return type switch
        {
            WindowType.Shop => 0,
            WindowType.Equipment => 1,
            WindowType.Lobby or WindowType.StageSelect => 2,
            WindowType.Character => 3,
            WindowType.Talent => 4,
            _ => -1
        };
    }

    private IEnumerator SlideToWindow(GameObject window, Vector2 entryDirection)
    {
        _isSliding = true;
        _inputGroup.blocksRaycasts = false;
        _inputGroup.interactable = false;

        RectTransform previous = (RectTransform)_current.transform;
        RectTransform next = (RectTransform)window.transform;
        Vector2 previousOrigin = previous.anchoredPosition;
        Vector2 nextOrigin = next.anchoredPosition;
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
