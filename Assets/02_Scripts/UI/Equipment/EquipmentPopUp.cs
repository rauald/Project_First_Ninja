using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentPopUp : MonoBehaviour
{
    private const float MATERIAL_ROW_HEIGHT = 62f;
    private const float MATERIAL_ROW_SPACING = 6f;
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
    [SerializeField] private Text _nameLine;
    [SerializeField] private Text _slotLine;
    [SerializeField] private Text _rankLine;
    [SerializeField] private Text _levelLine;
    [SerializeField] private Text _firstStatLine;
    [SerializeField] private Text _secondStatLine;
    [Tooltip("노말, 레어, 유니크, 에픽 순서로 연결합니다.")]
    [SerializeField] private Text[] _rankStatLines;
    [SerializeField] private GameObject _specialBadge;
    [SerializeField] private Text _message;
    [SerializeField] private Button _equip;
    [SerializeField] private Button _levelUpButton;
    [SerializeField] private Button _rankUpButton;

    private EquipmentInstanceData _selected;
    private Image _rankImage;
    private Image _itemImage;
    private float[] _rankStatAlphas;
    private GameObject _growthPanel;
    private Text _growthTitle;
    private Text _growthCost;
    private Text _growthStatus;
    private Button _confirmGrowth;
    private MaterialList _requiredMaterials;
    private MaterialList _additionalMaterials;
    private EquipmentInventory.RankUpCost _rankCost;

    private sealed class MaterialList
    {
        public GameObject Root;
        public Text Heading;
        public ScrollRect Scroll;
        public string Requirement;
        public int RequiredCount;
        public EquipmentInstanceData[] Candidates = Array.Empty<EquipmentInstanceData>();
        public readonly HashSet<EquipmentInstanceData> Selected = new();
        public readonly List<(Button Button, Text Label)> Rows = new();
    }

    private EquipmentInventory Inventory => GameManager.Instance.Equipment;

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
        _equip.interactable = !item.IsEquipped;
        if (_equip.GetComponentInChildren<Text>(true) is Text label)
        {
            label.text = item.IsEquipped ? "장착 중" : inventory.GetEquipped(slot) == null ? "장착" : "교체";
        }
        EnsureGrowthControls();
        _growthPanel.SetActive(false);
        _levelUpButton.interactable = inventory.GetLevelUpCost(item, out _);
        _rankUpButton.interactable = inventory.GetRankUpCost(item) != null;
        UIManager.Instance.PopUp.Open(PopUpType.Equipment);
    }

    private void EnsureGrowthControls()
    {
        if (_growthPanel != null)
        {
            return;
        }

        Transform panel = _panel;
        var growth = CreateRect(panel, "EquipmentGrowth", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _growthPanel = growth.gameObject;
        growth.gameObject.AddComponent<Image>().color = new Color(.17f, .12f, .08f);
        _growthTitle = CreateText(growth, "Title", 44, .05f, .86f, .95f, .98f);
        _growthCost = CreateText(growth, "Cost", 28, .07f, .69f, .93f, .86f);
        _growthStatus = CreateText(growth, "Status", 27, .07f, .11f, .93f, .18f);
        _requiredMaterials = CreateMaterialList(growth, "RequiredMaterials", .51f, .84f);
        _additionalMaterials = CreateMaterialList(growth, "AdditionalMaterials", .18f, .51f);
        _confirmGrowth = CreateButton(growth, "Confirm", "확인", new Vector2(.52f, .03f),
            new Vector2(.91f, .11f), Vector2.zero, Vector2.zero, ConfirmGrowth);
        CreateButton(growth, "Cancel", "취소", new Vector2(.09f, .03f),
            new Vector2(.48f, .11f), Vector2.zero, Vector2.zero, () => _growthPanel.SetActive(false));
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
        _growthCost.text = string.Join("\n", costs.Where(cost => cost.Amount > 0).Select(cost =>
        {
            string material = cost.ItemType == 0 && cost.DetailType == 0 ? "은자" : "장비 강화석";
            long held = GameManager.Instance.Characters.GetAmount(cost.ItemType, cost.DetailType);
            return $"{material}  {cost.Amount:N0} 필요 / {held:N0} 보유";
        }));
        _confirmGrowth.interactable = true;
        _growthStatus.text = "";
        SetRankGrowthLayout(false);
        _growthPanel.SetActive(true);
    }

    public void ShowRankUp()
    {
        _rankCost = Inventory.GetRankUpCost(_selected);
        if (_rankCost == null)
        {
            _message.text = "더 이상 등급업할 수 없습니다.";
            return;
        }

        string birthRank = _rankCost.RequiredBirthRank == 1 ? "특수" : "일반";
        int rank = Inventory.GetRank(_selected);
        _growthTitle.text = $"등급업  {CharacterDetailView.GetRankName(rank)} → {CharacterDetailView.GetRankName(rank + 1)}";
        SetRankGrowthLayout(true);
        BindMaterials(_requiredMaterials, _rankCost.RequiredCount,
            $"필수 재료 선택 · {EquipmentWindow.GetSlotName(_rankCost.RequiredSlot)} · {birthRank} · {CharacterDetailView.GetRankName(_rankCost.RequiredRank)}", true);
        BindMaterials(_additionalMaterials, _rankCost.AdditionalCount,
            $"추가 재료 선택 · {EquipmentWindow.GetSlotName(_rankCost.AdditionalSlot)} · {CharacterDetailView.GetRankName(_rankCost.AdditionalRank)}", false);
        RefreshMaterialSelection();
        _growthPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        _requiredMaterials.Scroll.verticalNormalizedPosition = 1f;
        _additionalMaterials.Scroll.verticalNormalizedPosition = 1f;
    }

    private void SetRankGrowthLayout(bool isRankUp)
    {
        var growth = (RectTransform)_growthPanel.transform;
        var availableArea = (RectTransform)_panel.parent;
        float extension = isRankUp ? Mathf.Clamp((availableArea.rect.height - _panel.rect.height - 48f) * .5f, 0f, 160f) : 0f;
        growth.offsetMin = new Vector2(0f, -extension);
        growth.offsetMax = new Vector2(0f, extension);
        _growthCost.gameObject.SetActive(!isRankUp);
        _requiredMaterials.Root.SetActive(isRankUp);
        _additionalMaterials.Root.SetActive(isRankUp);
    }

    private MaterialList CreateMaterialList(RectTransform parent, string name, float bottom, float top)
    {
        RectTransform group = CreateRect(parent, name, new Vector2(.07f, bottom), new Vector2(.93f, top),
            Vector2.zero, Vector2.zero);
        Text heading = CreateText(group, "Heading", 25, 0f, .77f, 1f, 1f);
        RectTransform viewport = CreateRect(group, "Viewport", Vector2.zero, new Vector2(1f, .77f),
            new Vector2(0f, 8f), Vector2.zero);
        viewport.gameObject.AddComponent<Image>().color = new Color(.12f, .09f, .07f);
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = CreateRect(viewport, "Content", new Vector2(0f, 1f), Vector2.one,
            Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(.5f, 1f);
        var scroll = group.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 58f;

        return new MaterialList { Root = group.gameObject, Heading = heading, Scroll = scroll };
    }

    private void BindMaterials(MaterialList list, int count, string requirement, bool isRequired)
    {
        list.RequiredCount = count;
        list.Requirement = requirement;
        list.Selected.Clear();
        list.Candidates = Inventory.GetRankUpCandidates(_selected, _rankCost, isRequired)
            .OrderBy(item => item.DefinitionId).ThenBy(Inventory.GetLevel).ToArray();
        list.Scroll.StopMovement();

        RectTransform content = list.Scroll.content;
        content.sizeDelta = new Vector2(0f, list.Candidates.Length * (MATERIAL_ROW_HEIGHT + MATERIAL_ROW_SPACING));
        while (list.Rows.Count < list.Candidates.Length)
        {
            int index = list.Rows.Count;
            float top = -index * (MATERIAL_ROW_HEIGHT + MATERIAL_ROW_SPACING);
            Button button = CreateButton(content, $"Material_{index + 1}", "", new Vector2(0f, 1f), Vector2.one,
                new Vector2(4f, top - MATERIAL_ROW_HEIGHT), new Vector2(-4f, top),
                () => ToggleMaterial(list, index));
            list.Rows.Add((button, button.GetComponentInChildren<Text>(true)));
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
        _growthStatus.text = _requiredMaterials.Candidates.Length == 0 || _additionalMaterials.Candidates.Length == 0
            ? "선택할 수 있는 미장착 재료 장비가 부족합니다."
            : "확인하면 선택한 장비가 소모됩니다.";
        _confirmGrowth.interactable = _requiredMaterials.Selected.Count == _requiredMaterials.RequiredCount
            && _additionalMaterials.Selected.Count == _additionalMaterials.RequiredCount;
    }

    private static void RefreshMaterialList(MaterialList list, MaterialList other)
    {
        list.Heading.text = $"{list.Requirement}\n선택 {list.Selected.Count} / {list.RequiredCount}";
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

    private Button CreateButton(Transform parent, string name, string title, Vector2 min, Vector2 max,
        Vector2 offsetMin, Vector2 offsetMax, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateRect(parent, name, min, max, offsetMin, offsetMax);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.29f, .20f, .13f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        CreateText(rect, "Label", 28, .02f, 0f, .98f, 1f).text = title;
        return button;
    }

    private Text CreateText(Transform parent, string name, int size, float x0, float y0, float x1, float y1)
    {
        RectTransform rect = CreateRect(parent, name, new Vector2(x0, y0), new Vector2(x1, y1), Vector2.zero, Vector2.zero);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = _nameLine.font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 min, Vector2 max,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        var rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private void RefreshEquipmentImages(EquipmentInstanceData item, int rank)
    {
        if (_rankImage == null)
        {
            Transform rankDisplay = _panel.Find("Rank");
            _rankImage = rankDisplay?.GetComponent<Image>();
            _itemImage = rankDisplay?.Find("Equip")?.GetComponent<Image>();
        }

        if (_rankImage != null)
        {
            _window.ApplyRankImage(_rankImage, rank);
        }

        if (_itemImage != null)
        {
            _window.ApplyItemImage(_itemImage, item);
        }
    }

    private void RefreshRankStats(CsvTable definitions, int id, int rank)
    {
        _rankStatAlphas ??= _rankStatLines.Select(line => line.color.a).ToArray();
        for (int index = 0; index < _rankStatLines.Length; index++)
        {
            Text line = _rankStatLines[index];
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
        if (!GameManager.Instance.Equipment.TryEquip(_selected))
        {
            _message.text = "장착 상태를 저장할 수 없습니다.";
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
