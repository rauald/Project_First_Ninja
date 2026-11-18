using UnityEngine;
using UnityEngine.UI;

public sealed class UIManager : Singleton<UIManager>
{
    private Text _titleStatus;

    public WindowManager Window { get; private set; }
    public PopUpManager PopUp { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        GameObject ui = new("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        ui.transform.SetParent(transform, false);
        ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = ui.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        RectTransform safeArea = CreateLayer("SafeArea", ui.transform);
        safeArea.gameObject.AddComponent<SafeAreaLayout>();
        RectTransform window = CreateLayer(nameof(WindowManager), safeArea);
        Window = window.gameObject.AddComponent<WindowManager>();
        PopUp = CreateLayer(nameof(PopUpManager), safeArea).gameObject.AddComponent<PopUpManager>();

        RectTransform status = CreateLayer("TitleStatus", window);
        status.anchorMin = new Vector2(0f, 0.5f);
        status.anchorMax = new Vector2(1f, 0.5f);
        status.offsetMin = new Vector2(32f, -80f);
        status.offsetMax = new Vector2(-32f, 80f);
        _titleStatus = status.gameObject.AddComponent<Text>();
        _titleStatus.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _titleStatus.alignment = TextAnchor.MiddleCenter;
        _titleStatus.fontSize = 44;
        _titleStatus.resizeTextForBestFit = true;
        _titleStatus.resizeTextMinSize = 24;
        _titleStatus.resizeTextMaxSize = 44;
        _titleStatus.color = Color.white;
        _titleStatus.raycastTarget = false;
        SetTitleStatus(null);
    }

    public void SetTitleStatus(string message)
    {
        _titleStatus.text = message;
        _titleStatus.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    private static RectTransform CreateLayer(string name, Transform parent)
    {
        GameObject layer = new(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)layer.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }
}
