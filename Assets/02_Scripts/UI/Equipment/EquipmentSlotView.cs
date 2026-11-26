using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentSlotView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Image _rank;
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private GameObject _equippedBadge;
    [SerializeField] private GameObject _specialBadge;
    [SerializeField] private TextMeshProUGUI _selectionLabel;

    internal void Render(Sprite icon, Sprite rank, string label, bool isEquipped, bool isSpecial, string selection)
    {
        SetSprite(_icon, icon);
        SetSprite(_rank, rank);
        if (_label != null)
        {
            _label.text = icon == null ? label : "";
        }
        if (_equippedBadge != null)
        {
            _equippedBadge.SetActive(isEquipped);
        }
        if (_specialBadge != null)
        {
            _specialBadge.SetActive(isSpecial);
        }
        if (_selectionLabel != null)
        {
            _selectionLabel.text = selection;
            _selectionLabel.transform.parent.gameObject.SetActive(selection.Length > 0);
        }
    }

    private static void SetSprite(Image image, Sprite sprite)
    {
        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}
