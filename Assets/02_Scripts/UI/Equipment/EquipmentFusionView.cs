using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentFusionView : MonoBehaviour
{
    private static readonly string[] RANK_NAMES = { "노말", "레어", "유니크", "에픽" };

    [SerializeField] private Image _targetIcon;
    [SerializeField] private TextMeshProUGUI _targetName;
    [SerializeField] private Image _targetRankBackground;
    [SerializeField] private GameObject _targetSpecialBadge;
    [SerializeField] private Image _resultIcon;
    [SerializeField] private TextMeshProUGUI _resultName;
    [SerializeField] private Image _resultRankBackground;
    [SerializeField] private GameObject _resultSpecialBadge;
    [SerializeField] private GrowthMaterialRow _requiredCost;
    [SerializeField] private GrowthMaterialRow _additionalCost;
    [SerializeField] private TextMeshProUGUI _selection;
    [SerializeField] private TextMeshProUGUI _status;
    [SerializeField] private Button _confirm;
    [SerializeField] private Button _reset;
    [SerializeField] private Button _batch;

    private readonly HashSet<EquipmentInstanceData> _required = new();
    private readonly HashSet<EquipmentInstanceData> _additional = new();
    private EquipmentWindow _window;
    private EquipmentInstanceData _target;
    private EquipmentInventory.RankUpCost _cost;

    private EquipmentInventory Inventory => GameManager.Instance.Equipment;

    internal void Initialize(EquipmentWindow window)
    {
        _window = window;
        _confirm.onClick.AddListener(RequestFusion);
        _batch.onClick.AddListener(() => UIManager.Instance.PopUp.ShowChoice("일괄 합성하시겠습니까?", ConfirmBatch));
        _reset.onClick.AddListener(() =>
        {
            ResetSelection();
            _window.RefreshEquipment();
        });
    }

    internal void ResetSelection()
    {
        _target = null;
        _cost = null;
        _required.Clear();
        _additional.Clear();
        Refresh("");
    }

    internal string GetSlotLabel(EquipmentInstanceData item)
    {
        if (item == _target)
        {
            return "대상";
        }
        if (_required.Contains(item))
        {
            return "필수";
        }
        return _additional.Contains(item) ? "추가" : "";
    }

    internal void Select(EquipmentInstanceData item)
    {
        string message = "";
        if (_target == null)
        {
            _cost = Inventory.GetRankUpCost(item);
            if (_cost == null)
            {
                Refresh("더 이상 등급업할 수 없습니다.");
                return;
            }
            _target = item;
        }
        else if (_target == item)
        {
            ResetSelection();
            _window.RefreshEquipment();
            return;
        }
        else if (!_required.Remove(item) && !_additional.Remove(item))
        {
            if (_required.Count < _cost.RequiredCount
                && Inventory.GetRankUpCandidates(_target, _cost, true).Contains(item))
            {
                _required.Add(item);
            }
            else if (_additional.Count < _cost.AdditionalCount
                && Inventory.GetRankUpCandidates(_target, _cost, false).Contains(item))
            {
                _additional.Add(item);
            }
            else
            {
                message = "필수 재료와 추가 재료를 각각 수량에 맞게 선택해 주세요.";
            }
        }
        Refresh(message);
        _window.RefreshEquipment();
    }

    private void Refresh(string message)
    {
        bool hasTarget = _target != null;
        _window.ApplyItemImage(_targetIcon, _target);
        _window.ApplyItemImage(_resultIcon, _target);
        _window.ApplyRankImage(_targetRankBackground, hasTarget ? Inventory.GetRank(_target) : -1);
        _window.ApplyRankImage(_resultRankBackground, hasTarget ? Inventory.GetRankUpResult(_target) : -1);
        bool isSpecial = hasTarget && Inventory.Definitions.GetInt(_target.DefinitionId, 2) == 1;
        _targetSpecialBadge.SetActive(isSpecial);
        _resultSpecialBadge.SetActive(isSpecial);
        _targetName.text = hasTarget ? Inventory.Definitions.GetString(_target.DefinitionId, 0) : "합성할 장비를 선택해 주세요.";
        _resultName.text = hasTarget ? FormatItem(_target, Inventory.GetRankUpResult(_target)) : "합성 결과";
        _selection.gameObject.SetActive(hasTarget);
        _reset.interactable = hasTarget;
        _confirm.interactable = hasTarget && _required.Count == _cost.RequiredCount
            && _additional.Count == _cost.AdditionalCount;
        _status.text = message;
        if (!hasTarget)
        {
            _requiredCost.Hide();
            _additionalCost.Hide();
            return;
        }

        var requiredCandidates = Inventory.GetRankUpCandidates(_target, _cost, true).Except(_additional).ToArray();
        var additionalCandidates = Inventory.GetRankUpCandidates(_target, _cost, false).Except(_required).ToHashSet();
        int fulfilledByRequiredOnly = requiredCandidates.Count(item => !additionalCandidates.Contains(item));
        int reservedForRequired = requiredCandidates.Count(item => additionalCandidates.Contains(item));
        reservedForRequired = Mathf.Clamp(_cost.RequiredCount - fulfilledByRequiredOnly, 0, reservedForRequired);
        _requiredCost.Show(_targetIcon.sprite, requiredCandidates.Length, _cost.RequiredCount);
        _additionalCost.Show(_window.GetRankSprite(_cost.AdditionalRank),
            additionalCandidates.Count - reservedForRequired, _cost.AdditionalCount);
        _selection.text = $"필수 선택 {_required.Count} / {_cost.RequiredCount}    추가 선택 {_additional.Count} / {_cost.AdditionalCount}";
    }

    private void RequestFusion()
    {
        UIManager.Instance.PopUp.ShowChoice("합성하시겠습니까?", Confirm,
            (_targetIcon.sprite, _required.Count, _cost.RequiredCount, false),
            (_window.GetRankSprite(_cost.AdditionalRank), _additional.Count, _cost.AdditionalCount, false));
    }

    private void Confirm()
    {
        if (Inventory.TryRankUp(_target, _required, _additional, out string message))
        {
            ResetSelection();
            _window.RefreshEquipment();
            _status.text = message;
            return;
        }

        UIManager.Instance.PopUp.ShowNotice(message);
    }

    private void ConfirmBatch()
    {
        IReadOnlyList<EquipmentInventory.RankUpPlan> plans = Inventory.PlanBatchRankUp();
        if (plans.Count == 0)
        {
            UIManager.Instance.PopUp.ShowNotice("합성할 수 있는 장비가 없습니다.");
            return;
        }

        if (!Inventory.TryBatchRankUp(plans))
        {
            UIManager.Instance.PopUp.ShowNotice("합성에 실패했습니다. 다시 시도해 주세요.");
            return;
        }

        ResetSelection();
        _window.RefreshEquipment();
        Refresh($"합성 {plans.Count}개 완료");
    }

    private string FormatItem(EquipmentInstanceData item, int rank)
    {
        return $"{Inventory.Definitions.GetString(item.DefinitionId, 0)} ({RANK_NAMES[rank]} Lv.{Inventory.GetLevel(item)})";
    }
}
