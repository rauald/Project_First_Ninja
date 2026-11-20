using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentPopUp : MonoBehaviour
{
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
    [SerializeField] private Text _title;
    [SerializeField] private Text _details;
    [SerializeField] private Text _message;
    [SerializeField] private Button _equip;

    private EquipmentInstanceData _selected;

    public void ShowEquipment(EquipmentInstanceData item)
    {
        _selected = item;
        EquipmentInventory inventory = GameManager.Instance.Equipment;
        CsvTable definitions = inventory.Definitions;
        int id = item.DefinitionId;
        int rank = item.Rank >= 0 ? item.Rank : definitions.GetInt(id, 4);
        int level = item.Level > 0 ? item.Level : definitions.GetInt(id, 3);
        bool isSpecial = definitions.GetInt(id, 2) == 1;
        EquipmentSlotType slot = inventory.GetSlot(item);

        _title.text = definitions.GetString(id, 0);
        StringBuilder details = new();
        details.AppendLine($"{EquipmentWindow.GetSlotName(slot)}  ·  {(isSpecial ? "S급 태생" : "일반")}  ·  {CharacterDetailView.GetRankName(rank)}  ·  Lv. {level}");
        AppendStat(details, definitions, id, 5);
        AppendStat(details, definitions, id, 7);
        if (isSpecial)
        {
            AppendStat(details, definitions, id, 17);
        }

        for (int i = 0; i <= Mathf.Min(rank, 3); i++)
        {
            AppendStat(details, definitions, id, 9 + i * 2);
        }

        _details.text = details.ToString();
        _message.text = "";
        _equip.interactable = !item.IsEquipped;
        _equip.GetComponentInChildren<Text>().text = item.IsEquipped ? "장착 중" : inventory.GetEquipped(slot) == null ? "장착" : "교체";
        UIManager.Instance.PopUp.Open(PopUpType.Equipment);
    }

    private static void AppendStat(StringBuilder details, CsvTable definitions, int id, int field)
    {
        int type = definitions.GetInt(id, field);
        if (type <= 0 || type >= STAT_NAMES.Length)
        {
            return;
        }

        int value = definitions.GetInt(id, field + 1);
        string suffix = type >= 4 && type != 27 ? "%" : "";
        details.AppendLine($"{STAT_NAMES[type]}  {value}{suffix}");
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
