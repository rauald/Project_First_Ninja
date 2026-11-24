using UnityEngine;
using UnityEngine.UI;

// 씬 전환 동안 화면을 덮고 실제 시간으로 표시를 회전시킨다.
internal sealed class SceneLoadingOverlay : MonoBehaviour
{
    private RectTransform _spinner;

    internal static SceneLoadingOverlay Create(Transform parent)
    {
        var root = new GameObject("Scene loading overlay", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(parent, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = .5f;

        var backdrop = CreateRect(root.transform, "Black backdrop", Vector2.zero, Vector2.zero);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        var artwork = parent.GetComponent<SceneLoadingArtwork>();
        var backdropImage = backdrop.gameObject.AddComponent<Image>();
        backdropImage.sprite = artwork != null ? artwork.Backdrop : null;
        backdropImage.color = backdropImage.sprite != null ? Color.white : Color.black;

        var overlay = root.AddComponent<SceneLoadingOverlay>();
        overlay._spinner = CreateRect(backdrop, "Spinner", new Vector2(0f, 33f), new Vector2(100f, 100f));
        if (artwork != null && artwork.Spinner != null)
        {
            var image = overlay._spinner.gameObject.AddComponent<Image>();
            image.sprite = artwork.Spinner;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
        else
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f;
                var rotation = Quaternion.Euler(0f, 0f, angle);
                var spoke = CreateRect(overlay._spinner, "Spinner spoke",
                    rotation * new Vector3(0f, 36f), new Vector2(7f, 19f));
                spoke.localRotation = rotation;
                var image = spoke.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, Mathf.Lerp(.16f, 1f, i / 11f));
                image.raycastTarget = false;
            }
        }

        var label = CreateRect(backdrop, "Loading label", new Vector2(0f, -54f), new Vector2(380f, 60f))
            .gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "로딩중...";
        label.fontSize = 42;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return overlay;
    }

    internal void Show()
    {
        _spinner.localRotation = Quaternion.identity;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        _spinner.Rotate(0f, 0f, -310f * Time.unscaledDeltaTime);
    }

    // 팝업 요소를 화면 가운데 기준으로 배치한다.
    private static RectTransform CreateRect(Transform parent, string objectName, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
