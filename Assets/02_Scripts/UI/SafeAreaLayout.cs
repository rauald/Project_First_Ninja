using UnityEngine;
using Screen = UnityEngine.Device.Screen;

[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaLayout : MonoBehaviour
{
    [SerializeField] private RectTransform[] _fullScreenLayers;
    [SerializeField] private RectTransform _lobbyTopLetterbox;
    [SerializeField] private RectTransform _lobbyBottomLetterbox;
    [SerializeField] private RectTransform _lobbyLeftLetterbox;
    [SerializeField] private RectTransform _lobbyRightLetterbox;

    private RectTransform _layout;
    private Vector2Int _screenSize;
    private Rect _safeArea;

    private void OnEnable()
    {
        _layout = GetComponent<RectTransform>();
        _screenSize = Vector2Int.zero;
        UpdateLayout();
    }

    private void LateUpdate()
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        Vector2Int size = new(Screen.width, Screen.height);
        Rect area = Screen.safeArea;
        if (size.x <= 0 || size.y <= 0 || area.width <= 0 || area.height <= 0
            || (size == _screenSize && area == _safeArea))
        {
            return;
        }

        // 화면 픽셀 좌표를 부모 Canvas의 정규화된 앵커로 바꾼다.
        Rect fittedArea = GetContentRect(size, area);
        _layout.anchorMin = new Vector2(fittedArea.xMin / size.x, fittedArea.yMin / size.y);
        _layout.anchorMax = new Vector2(fittedArea.xMax / size.x, fittedArea.yMax / size.y);
        _layout.offsetMin = Vector2.zero;
        _layout.offsetMax = Vector2.zero;

        // 배경 레이어는 안전 영역을 기준으로 전체 화면까지 확장한다.
        Vector2 fullMin = new(-fittedArea.xMin / fittedArea.width, -fittedArea.yMin / fittedArea.height);
        Vector2 fullMax = new((size.x - fittedArea.xMin) / fittedArea.width, (size.y - fittedArea.yMin) / fittedArea.height);
        foreach (RectTransform layer in _fullScreenLayers)
        {
            layer.anchorMin = fullMin;
            layer.anchorMax = fullMax;
        }

        SetLetterbox(_lobbyTopLetterbox, new Vector2(fullMin.x, 1f), fullMax);
        SetLetterbox(_lobbyBottomLetterbox, fullMin, new Vector2(fullMax.x, 0f));
        SetLetterbox(_lobbyLeftLetterbox, new Vector2(fullMin.x, 0f), Vector2.up);
        SetLetterbox(_lobbyRightLetterbox, Vector2.right, new Vector2(fullMax.x, 1f));

        _screenSize = size;
        _safeArea = area;
    }

    private static Rect GetContentRect(Vector2Int size, Rect safeArea)
    {
        const float REFERENCE_ASPECT = 1080f / 1920f;
        float width = Mathf.Min(safeArea.width, safeArea.height * REFERENCE_ASPECT);
        float height = Mathf.Min(safeArea.height, safeArea.width / REFERENCE_ASPECT);
        float left = Mathf.Clamp((size.x - width) * 0.5f, safeArea.xMin, safeArea.xMax - width);
        float bottom = Mathf.Clamp((size.y - height) * 0.5f, safeArea.yMin, safeArea.yMax - height);
        return new Rect(left, bottom, width, height);
    }

    private static void SetLetterbox(RectTransform strip, Vector2 min, Vector2 max)
    {
        if (strip == null)
        {
            return;
        }

        strip.anchorMin = min;
        strip.anchorMax = max;
    }
}
