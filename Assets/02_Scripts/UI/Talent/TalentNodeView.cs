using UnityEngine;
using UnityEngine.UI;

public sealed class TalentNodeView : MonoBehaviour
{
    [SerializeField] private int _id;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _learnedAura;
    [SerializeField] private Button _button;
    [SerializeField] private RectTransform[] _links;

    public int Id => _id;
    public Sprite Icon => _icon.sprite;
    public Button Button => _button;
    public Vector2 Position => ((RectTransform)transform).anchoredPosition;

    internal void SetIcon(Sprite icon)
    {
        _icon.sprite = icon;
    }

    public void SetLearned(bool isLearned, Color unlearnedColor, Sprite auraSprite)
    {
        _icon.color = isLearned ? Color.white : unlearnedColor;
        if (_learnedAura == null)
        {
            return;
        }

        bool showAura = isLearned && auraSprite != null;
        if (showAura)
        {
            _learnedAura.sprite = auraSprite;
            _learnedAura.rectTransform.anchoredPosition = Position;
        }
        _learnedAura.gameObject.SetActive(showAura);
    }

    public void PlaceLink(int index, Vector2 start)
    {
        RectTransform link = _links[index];
        Vector2 delta = Position - start;
        link.anchoredPosition = (Position + start) * 0.5f;
        link.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, delta.magnitude);
        link.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }
}
