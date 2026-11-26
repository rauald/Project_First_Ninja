using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class StageCardView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _number;
    [SerializeField] private TextMeshProUGUI _stageName;
    [SerializeField] private Image _mapPreview;
    [SerializeField] private GameObject _clearBadge;
    [SerializeField] private GameObject _lockedOverlay;

    private int _stageId;
    private Action<int> _onSelect;

    public void Bind(int id, string stageName, Sprite preview, bool canEnter, bool hasCleared, Action<int> onSelect)
    {
        _stageId = id;
        _onSelect = onSelect;
        _number.text = id.ToString();
        _stageName.text = stageName;
        _mapPreview.sprite = preview;
        _mapPreview.enabled = preview != null;
        _button.interactable = canEnter;
        _clearBadge.SetActive(hasCleared);
        _lockedOverlay.SetActive(!canEnter);
    }

    public void SelectStage() => _onSelect?.Invoke(_stageId);
}
