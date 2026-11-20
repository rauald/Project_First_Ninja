using UnityEngine;
using Screen = UnityEngine.Device.Screen;

[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaLayout : MonoBehaviour
{
    [SerializeField] private RectTransform _topLetterbox;
    [SerializeField] private RectTransform _bottomLetterbox;
    [SerializeField] private RectTransform _leftLetterbox;
    [SerializeField] private RectTransform _rightLetterbox;

    private RectTransform _layout;
    private Vector2Int _screenSize;
    private Rect _safeArea;
    private Vector2 _canvasSize;

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
        Vector2 canvasSize = ((RectTransform)_layout.parent).rect.size;
        if (size.x <= 0 || size.y <= 0 || area.width <= 0 || area.height <= 0
            || (size == _screenSize && area == _safeArea && canvasSize == _canvasSize))
        {
            return;
        }

        // 중앙 고정 앵커를 유지하고 Canvas 단위의 위치와 크기만 계산한다.
        Rect fittedArea = GetContentRect(size, area);
        Vector2 scale = new(canvasSize.x / size.x, canvasSize.y / size.y);
        _layout.sizeDelta = Vector2.Scale(fittedArea.size, scale);
        _layout.anchoredPosition = Vector2.Scale(fittedArea.center - (Vector2)size * 0.5f, scale);

        float top = (size.y - fittedArea.yMax) * scale.y;
        float bottom = fittedArea.yMin * scale.y;
        float left = fittedArea.xMin * scale.x;
        float right = (size.x - fittedArea.xMax) * scale.x;
        float extraWidth = canvasSize.x - _layout.sizeDelta.x;
        SetLetterbox(_topLetterbox, new Vector2(-_layout.anchoredPosition.x, top * 0.5f), new Vector2(extraWidth, top));
        SetLetterbox(_bottomLetterbox, new Vector2(-_layout.anchoredPosition.x, -bottom * 0.5f), new Vector2(extraWidth, bottom));
        SetLetterbox(_leftLetterbox, new Vector2(-left * 0.5f, 0f), new Vector2(left, 0f));
        SetLetterbox(_rightLetterbox, new Vector2(right * 0.5f, 0f), new Vector2(right, 0f));

        _screenSize = size;
        _safeArea = area;
        _canvasSize = canvasSize;
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

    private static void SetLetterbox(RectTransform strip, Vector2 position, Vector2 size)
    {
        if (strip == null)
        {
            return;
        }

        strip.anchoredPosition = position;
        strip.sizeDelta = size;
    }
}
