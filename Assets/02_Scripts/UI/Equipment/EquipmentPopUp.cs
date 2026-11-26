using TMPro;
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
        if (_equip.GetComponentInChildren<TextMeshProUGUI>(true) is TextMeshProUGUI label)
        {
            label.text = item.IsEquipped ? "해제" : inventory.GetEquipped(slot) == null ? "장착" : "교체";
        }
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

        EquipmentInstanceData item = _selected;
        int level = Inventory.GetLevel(item);
        var materials = costs.Where(cost => cost.Amount > 0).Select(cost => (
            cost.ItemType == 0 ? _silverIcon : _equipmentStoneIcon,
            GameManager.Instance.Characters.GetAmount(cost.ItemType, cost.DetailType),
            cost.Amount, cost.ItemType == 0)).ToArray();
        UIManager.Instance.PopUp.ShowChoice($"레벨업  Lv. {level} → {level + 1}", () => ConfirmGrowth(item), materials);
    }

    public void ShowRankUp()
    {
        Close();
        _window.OpenFusionTab();
    }

    private void ConfirmGrowth(EquipmentInstanceData item)
    {
        if (!Inventory.TryLevelUp(item, out string message))
        {
            UIManager.Instance.PopUp.ShowNotice(message);
            return;
        }

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
