using UnityEngine;
using Screen = UnityEngine.Device.Screen;

[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaLayout : MonoBehaviour
{
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
        _layout.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
        _layout.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
        _layout.offsetMin = Vector2.zero;
        _layout.offsetMax = Vector2.zero;
        _screenSize = size;
        _safeArea = area;
    }
}
