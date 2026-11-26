using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct GrowthMaterialRow
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _amount;

    public GrowthMaterialRow(Image icon, TextMeshProUGUI amount)
    {
        _icon = icon;
        _amount = amount;
    }

    public void Show(Sprite sprite, long owned, long required, bool isCurrency = false)
    {
        _icon.sprite = sprite;
        _icon.gameObject.SetActive(sprite != null);
        _amount.text = isCurrency
            ? $"{CurrencyFormatter.Format(owned)} / {CurrencyFormatter.Format(required)}"
            : $"{owned:N0} / {required:N0}";
        _amount.color = owned >= required ? Color.white : new Color(1f, .3f, .3f);
        _amount.gameObject.SetActive(true);
    }

    public void Hide()
    {
        _icon.gameObject.SetActive(false);
        _amount.gameObject.SetActive(false);
    }
}
