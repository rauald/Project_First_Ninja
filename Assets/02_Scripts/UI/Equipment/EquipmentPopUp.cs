using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentPopUp : MonoBehaviour
{
    private static readonly string[] RANK_LABELS = { "노말", "레어", "유니크", "에픽" };
    private static readonly string[] STAT_NAMES =
    {
        "", "공격력", "방어력", "체력", "치명타 확률", "치명타 데미지", "재사용 대기 시간 감소", "회피율",
        "기본 스킬 데미지", "무속성 스킬 데미지", "화속성 스킬 데미지", "수속성 스킬 데미지", "목속성 스킬 데미지",
        "토속성 스킬 데미지", "풍속성 스킬 데미지", "화상 부여", "독 부여", "출혈 부여", "화상 데미지", "독 데미지",
        "출혈 데미지", "화상 대상 공격 데미지", "독 대상 공격 데미지", "출혈 대상 공격 데미지", "획득 골드",
        "저체력 시 데미지", "치명타 확률 및 데미지", "부활", "스킬 범위", "모든 스킬 데미지", "각성기 데미지",
        "전투 시작 보호막", "이동 속도", "저체력 적 처형", "아이템 획득 범위", "스킬 2회 사용 확률",
        "저체력 시 보호막", "흡혈", "경험치 획득량", "받는 데미지 감소", "적 방어력 무시", "일반 몬스터 대상 데미지",
        "보스 몬스터 대상 데미지", "상태이상 대상 데미지", "보호막 추가 생성량", "공격력", "방어력", "체력"
    };

    [SerializeField] private EquipmentWindow _window;
    [SerializeField] private RectTransform _panel;
    [SerializeField] private TextMeshProUGUI _nameLine;
    [SerializeField] private TextMeshProUGUI _slotLine;
    [SerializeField] private TextMeshProUGUI _rankLine;
    [SerializeField] private TextMeshProUGUI _levelLine;
    [SerializeField] private TextMeshProUGUI _firstStatLine;
    [SerializeField] private TextMeshProUGUI _secondStatLine;
    [Tooltip("노말, 레어, 유니크, 에픽 순서로 연결합니다.")]
    [SerializeField] private TextMeshProUGUI[] _rankStatLines;
    [SerializeField] private GameObject _specialBadge;
    [SerializeField] private TextMeshProUGUI _message;
    [SerializeField] private Button _equip;
    [SerializeField] private Button _levelUpButton;
    [SerializeField] private Button _rankUpButton;
    [SerializeField] private Image _rankImage;
    [SerializeField] private Image _itemImage;
    [SerializeField] private Sprite _silverIcon;
    [SerializeField] private Sprite _equipmentStoneIcon;

    private EquipmentInstanceData _selected;
    private float[] _rankStatAlphas;
    [SerializeField] private RectTransform _growthPanel;
    [SerializeField] private TextMeshProUGUI _growthTitle;
    [SerializeField] private GrowthMaterialRow[] _levelMaterials;
    [SerializeField] private TextMeshProUGUI _growthStatus;
    [SerializeField] private Button _confirmGrowth;
    [SerializeField] private Button _cancelGrowth;
    [SerializeField] private Button _materialRowTemplate;
    [SerializeField] private MaterialList _requiredMaterials;
    [SerializeField] private MaterialList _additionalMaterials;

    private Vector2 _growthOffsetMin;
    private Vector2 _growthOffsetMax;
    private EquipmentInventory.RankUpCost _rankCost;

    [Serializable]
    private sealed class MaterialList
    {
        public TextMeshProUGUI Heading;
        public TextMeshProUGUI Selection;
        public GrowthMaterialRow Cost;
        public ScrollRect Scroll;
        [NonSerialized] public int RequiredCount;
        [NonSerialized] public EquipmentInstanceData[] Candidates = Array.Empty<EquipmentInstanceData>();
        public readonly HashSet<EquipmentInstanceData> Selected = new();
        public readonly List<(Button Button, TextMeshProUGUI Label, Image Icon)> Rows = new();
    }

    private EquipmentInventory Inventory => GameManager.Instance.Equipment;

    private void Awake()
    {
        _growthOffsetMin = _growthPanel.offsetMin;
        _growthOffsetMax = _growthPanel.offsetMax;
        _confirmGrowth.onClick.AddListener(ConfirmGrowth);
        _cancelGrowth.onClick.AddListener(() => _growthPanel.gameObject.SetActive(false));
    }

    public void ShowEquipment(EquipmentInstanceData item)
    {
        _selected = item;
        EquipmentInventory inventory = Inventory;
        CsvTable definitions = inventory.Definitions;
        int id = item.DefinitionId;
        int rank = inventory.GetRank(item);
        int level = inventory.GetLevel(item);
        bool isSpecial = definitions.GetInt(id, 2) == 1;
        EquipmentSlotType slot = inventory.GetSlot(item);
        RefreshEquipmentImages(item, rank);

        _nameLine.text = definitions.GetString(id, 0);
        _slotLine.text = EquipmentWindow.GetSlotName(slot);
        _rankLine.text = CharacterDetailView.GetRankName(rank);
        _levelLine.text = $"Lv. {level}";
        _firstStatLine.text = FormatStat(definitions, id, 5);
        _secondStatLine.text = FormatStat(definitions, id, 7);
        _secondStatLine.gameObject.SetActive(_secondStatLine.text.Length > 0);
        RefreshRankStats(definitions, id, rank);
        _specialBadge.SetActive(isSpecial);

        _message.text = "";
        if (_equip.GetComponentInChildren<TextMeshProUGUI>(true) is TextMeshProUGUI label)
        {
            label.text = item.IsEquipped ? "해제" : inventory.GetEquipped(slot) == null ? "장착" : "교체";
        }
        _growthPanel.gameObject.SetActive(false);
        _levelUpButton.interactable = inventory.GetLevelUpCost(item, out _);
        _rankUpButton.interactable = inventory.GetRankUpCost(item) != null;
        UIManager.Instance.PopUp.Open(PopUpType.Equipment);
    }

    public void ShowLevelUp()
    {
        if (!Inventory.GetLevelUpCost(_selected, out CharacterMaterialData[] costs))
        {
            _message.text = "더 이상 레벨업할 수 없습니다.";
            return;
        }

        _rankCost = null;
        _growthTitle.text = $"레벨업  Lv. {Inventory.GetLevel(_selected)} → {Inventory.GetLevel(_selected) + 1}";
        SetRankGrowthLayout(false);
        int visibleIndex = 0;
        foreach (CharacterMaterialData cost in costs)
        {
            if (cost.Amount <= 0)
            {
                continue;
            }

            bool isCurrency = cost.ItemType == 0;
            long held = GameManager.Instance.Characters.GetAmount(cost.ItemType, cost.DetailType);
            _levelMaterials[visibleIndex++].Show(isCurrency ? _silverIcon : _equipmentStoneIcon, held, cost.Amount);
        }
        _confirmGrowth.interactable = true;
        _growthStatus.text = "";
        _growthPanel.gameObject.SetActive(true);
    }

    public void ShowRankUp()
    {
        _rankCost = Inventory.GetRankUpCost(_selected);
        if (_rankCost == null)
        {
            _message.text = "더 이상 등급업할 수 없습니다.";
            return;
        }

        string requiredItemName = Inventory.Definitions.GetString(_selected.DefinitionId, 0);
        int rank = Inventory.GetRank(_selected);
        _growthTitle.text = $"등급업  {CharacterDetailView.GetRankName(rank)} → {CharacterDetailView.GetRankName(rank + 1)}";
        SetRankGrowthLayout(true);
        BindMaterials(_requiredMaterials, _rankCost.RequiredCount,
            $"필수 재료 · {requiredItemName} · {CharacterDetailView.GetRankName(_rankCost.RequiredRank)}",
            _window.GetItemIcon(_selected.DefinitionId), true);
        BindMaterials(_additionalMaterials, _rankCost.AdditionalCount,
            $"추가 재료 · {EquipmentWindow.GetSlotName(_rankCost.AdditionalSlot)} · {CharacterDetailView.GetRankName(_rankCost.AdditionalRank)}",
            _window.GetRankSprite(_rankCost.AdditionalRank), false);
        RefreshMaterialSelection();
        _growthPanel.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        _requiredMaterials.Scroll.verticalNormalizedPosition = 1f;
        _additionalMaterials.Scroll.verticalNormalizedPosition = 1f;
    }

    private void SetRankGrowthLayout(bool isRankUp)
    {
        var availableArea = (RectTransform)_panel.parent;
        float extension = isRankUp ? Mathf.Clamp((availableArea.rect.height - _panel.rect.height - 48f) * .5f, 0f, 160f) : 0f;
        _growthPanel.offsetMin = _growthOffsetMin - Vector2.up * extension;
        _growthPanel.offsetMax = _growthOffsetMax + Vector2.up * extension;
        foreach (GrowthMaterialRow row in _levelMaterials)
        {
            row.Hide();
        }
        _requiredMaterials.Scroll.gameObject.SetActive(isRankUp);
        _additionalMaterials.Scroll.gameObject.SetActive(isRankUp);
    }

    private void BindMaterials(MaterialList list, int count, string requirement, Sprite icon, bool isRequired)
    {
        list.RequiredCount = count;
        list.Selected.Clear();
        list.Candidates = Inventory.GetRankUpCandidates(_selected, _rankCost, isRequired)
            .OrderBy(item => item.DefinitionId).ThenBy(Inventory.GetLevel).ToArray();
        list.Heading.text = requirement;
        list.Cost.Show(icon, list.Candidates.Length, count);
        list.Scroll.StopMovement();

        RectTransform content = list.Scroll.content;
        while (list.Rows.Count < list.Candidates.Length)
        {
            int index = list.Rows.Count;
            Button button = Instantiate(_materialRowTemplate, content);
            button.name = $"Material_{index + 1}";
            button.onClick.AddListener(() => ToggleMaterial(list, index));
            list.Rows.Add((button, button.GetComponentInChildren<TextMeshProUGUI>(true),
                button.transform.Find("Material_Icon").GetComponent<Image>()));
        }

        for (int index = 0; index < list.Rows.Count; index++)
        {
            var row = list.Rows[index];
            bool isAvailable = index < list.Candidates.Length;
            row.Button.gameObject.SetActive(isAvailable);
            if (isAvailable)
            {
                EquipmentInstanceData item = list.Candidates[index];
                string name = Inventory.Definitions.GetString(item.DefinitionId, 0);
                string type = Inventory.Definitions.GetInt(item.DefinitionId, 2) == 1 ? "특수" : "일반";
                row.Label.text = $"{name}  ·  Lv. {Inventory.GetLevel(item)}  ·  {type}";
                _window.ApplyItemImage(row.Icon, item);
            }
        }
    }

    private void ToggleMaterial(MaterialList list, int index)
    {
        EquipmentInstanceData item = list.Candidates[index];
        MaterialList other = list == _requiredMaterials ? _additionalMaterials : _requiredMaterials;
        if (!list.Selected.Remove(item) && list.Selected.Count < list.RequiredCount && !other.Selected.Contains(item))
        {
            list.Selected.Add(item);
        }

        RefreshMaterialSelection();
    }

    private void RefreshMaterialSelection()
    {
        RefreshMaterialList(_requiredMaterials, _additionalMaterials);
        RefreshMaterialList(_additionalMaterials, _requiredMaterials);
        _growthStatus.text = _requiredMaterials.Candidates.Length < _requiredMaterials.RequiredCount
            || _additionalMaterials.Candidates.Length < _additionalMaterials.RequiredCount
            ? "선택할 수 있는 미장착 재료 장비가 부족합니다."
            : "확인하면 선택한 장비가 소모됩니다.";
        _confirmGrowth.interactable = _requiredMaterials.Selected.Count == _requiredMaterials.RequiredCount
            && _additionalMaterials.Selected.Count == _additionalMaterials.RequiredCount;
    }

    private static void RefreshMaterialList(MaterialList list, MaterialList other)
    {
        list.Selection.text = $"선택 {list.Selected.Count} / {list.RequiredCount}";
        for (int index = 0; index < list.Candidates.Length; index++)
        {
            Button button = list.Rows[index].Button;
            EquipmentInstanceData item = list.Candidates[index];
            bool selected = list.Selected.Contains(item);
            button.interactable = selected || (list.Selected.Count < list.RequiredCount && !other.Selected.Contains(item));
            ((Image)button.targetGraphic).color = selected ? new Color(.54f, .35f, .14f) : new Color(.29f, .20f, .13f);
        }
    }

    private void ConfirmGrowth()
    {
        string message;
        bool succeeded = _rankCost == null
            ? Inventory.TryLevelUp(_selected, out message)
            : Inventory.TryRankUp(_selected, _requiredMaterials.Selected, _additionalMaterials.Selected, out message);
        if (!succeeded)
        {
            _growthStatus.text = message;
            return;
        }

        EquipmentInstanceData item = _selected;
        _window.ResetUI();
        ShowEquipment(item);
        _message.text = message;
    }

    private void RefreshEquipmentImages(EquipmentInstanceData item, int rank)
    {
        _window.ApplyRankImage(_rankImage, rank);
        _window.ApplyItemImage(_itemImage, item);
    }

    private void RefreshRankStats(CsvTable definitions, int id, int rank)
    {
        _rankStatAlphas ??= _rankStatLines.Select(line => line.color.a).ToArray();
        for (int index = 0; index < _rankStatLines.Length; index++)
        {
            TextMeshProUGUI line = _rankStatLines[index];
            string stat = FormatStat(definitions, id, 9 + index * 2);
            line.text = $"{RANK_LABELS[index]}  {(stat.Length > 0 ? stat : "없음")}";
            Color color = line.color;
            color.a = index <= rank ? _rankStatAlphas[index] : _rankStatAlphas[index] * .45f;
            line.color = color;
        }
    }

    private static string FormatStat(CsvTable definitions, int id, int field)
    {
        int type = definitions.GetInt(id, field);
        if (type <= 0 || type >= STAT_NAMES.Length)
        {
            return "";
        }

        int value = definitions.GetInt(id, field + 1);
        string suffix = type >= 4 && type != 27 ? "%" : "";
        return $"{STAT_NAMES[type]}  {value}{suffix}";
    }

    public void EquipSelected()
    {
        if (_selected == null)
        {
            return;
        }

        bool isUnequipping = _selected.IsEquipped;
        bool isSaved = isUnequipping ? Inventory.TryUnequip(_selected) : Inventory.TryEquip(_selected);
        if (!isSaved)
        {
            _message.text = isUnequipping ? "해제 상태를 저장할 수 없습니다." : "장착 상태를 저장할 수 없습니다.";
            return;
        }

        _window.RefreshEquipment();
        Close();
    }

    public void Close()
    {
        UIManager.Instance.PopUp.Close();
    }
}
