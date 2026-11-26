using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterPopUp : MonoBehaviour
{
    [SerializeField] private CharacterWindow _window;
    [SerializeField] private GameObject _infoPanel;
    [SerializeField] private CharacterDetailView _detail;
    [SerializeField] private Button _equip;
    [SerializeField] private Button[] _growthButtons;
    [SerializeField] private GameObject _skillPanel;
    [SerializeField] private TextMeshProUGUI _skillTitle;
    [SerializeField] private TextMeshProUGUI _skillDescription;
    [SerializeField] private Image _skillIcon;
    [Tooltip("캐릭터 성장 확인 창에서 은자 비용을 표시하는 아이콘입니다.")]
    [SerializeField] private Sprite _silverIcon;
    [Tooltip("캐릭터 성장 확인 창에서 금자 비용을 표시하는 아이콘입니다.")]
    [SerializeField] private Sprite _goldIcon;
    [Tooltip("캐릭터 성장 확인 창의 조각 아이콘입니다. 재료 상세 타입이 배열 인덱스입니다.")]
    [SerializeField] private Sprite[] _fragmentIcons;
    [Tooltip("캐릭터 성장 확인 창의 강화석 아이콘입니다. 재료 상세 타입이 배열 인덱스입니다.")]
    [SerializeField] private Sprite[] _stoneIcons;

    private int _characterId;
    private bool _hasInfo;

    public void ShowCharacter(int id)
    {
        UIManager.Instance.PopUp.Open(PopUpType.Character);
        _characterId = id;
        _hasInfo = true;
        _infoPanel.SetActive(true);
        _skillPanel.SetActive(false);
        RefreshInfo();
    }

    private void RefreshInfo()
    {
        CharacterProgression characters = GameManager.Instance.Characters;
        _detail.RefreshCharacter(_window.GetPresentation(_characterId), slot => ShowSkill(_characterId, slot, true));
        CharacterProgressData state = characters.GetCharacter(_characterId);
        _equip.interactable = state.IsAvailable && characters.Data.EquippedCharacterId != _characterId;
        _equip.GetComponentInChildren<TextMeshProUGUI>().text = characters.Data.EquippedCharacterId == _characterId ? "장착 중" : "장착";
        for (int i = 0; i < _growthButtons.Length; i++)
        {
            _growthButtons[i].interactable = characters.GetGrowthCost(_characterId, (CharacterGrowthType)i, out _, out _);
        }
    }

    public void EquipCharacter()
    {
        GameManager.Instance.Characters.EquipCharacter(_characterId, out string message);
        _window.RefreshUI();
        RefreshInfo();
        ShowMessage(message);
    }

    public void RequestGrowth(int type)
    {
        var growth = (CharacterGrowthType)type;
        int id = _characterId;
        CharacterProgression characters = GameManager.Instance.Characters;
        if (!characters.GetGrowthCost(id, growth, out List<CharacterMaterialData> cost, out string message))
        {
            ShowMessage(message);
            return;
        }

        string name = growth switch { CharacterGrowthType.Level => "레벨업", CharacterGrowthType.Star => "성급업", _ => "등급업" };
        var materials = cost.Select(item => (
            GetMaterialIcon(item), characters.GetAmount(item.ItemType, item.DetailType), item.Amount, item.ItemType == 0)).ToArray();
        UIManager.Instance.PopUp.ShowChoice($"{name} 하시겠습니까?", () => ConfirmGrowth(id, growth), materials);
    }

    private Sprite GetMaterialIcon(CharacterMaterialData item)
    {
        return item.ItemType switch
        {
            0 => item.DetailType == 0 ? _silverIcon : _goldIcon,
            1 => _fragmentIcons[item.DetailType],
            _ => _stoneIcons[item.DetailType]
        };
    }

    private void ConfirmGrowth(int id, CharacterGrowthType growth)
    {
        GameManager.Instance.Characters.GrowCharacter(id, growth, out string message);
        _window.RefreshUI();
        RefreshInfo();
        ShowMessage(message);
    }

    private void ShowMessage(string message) => UIManager.Instance.PopUp.ShowNotice(message);

    public void ShowSkill(int id, int slot, bool hasInfo)
    {
        UIManager.Instance.PopUp.Open(PopUpType.Character);
        _characterId = id;
        _hasInfo = hasInfo;
        _infoPanel.SetActive(hasInfo);
        CharacterPresentation presentation = _window.GetPresentation(id);
        CsvTable table = DataManager.Instance.GetTable(slot == 3 ? "BattleActiveSkillData" : "CharacterPassiveSkillData");
        CsvTable characterTable = GameManager.Instance.Characters.Characters;
        int row = slot == 3 ? 19 + characterTable.GetInt(id, 22) : characterTable.GetInt(id, 16 + slot * 2);
        _skillTitle.text = table.Contains(row) ? table.GetString(row, 1) : presentation.SkillNames[slot];
        _skillIcon.sprite = presentation.SkillIcons[slot];
        _skillDescription.text = table.Contains(row)
            ? (slot == 3 ? BattleSkills.GetActiveDescription(row) : BattleSkills.GetCharacterPassiveDescription(row))
            : "스킬 정보가 아직 연결되지 않았습니다.";
        int condition = GameManager.Instance.Characters.Characters.GetInt(id, 17 + Mathf.Min(slot, 2) * 2);
        _skillDescription.text += $"\n\n개방 조건: 캐릭터 해금 · {condition}성\n" +
            (CharacterDetailView.IsSkillUnlocked(id, slot) ? "개방됨" : "잠금 상태");
        _skillPanel.SetActive(true);
    }

    public void CloseSkill()
    {
        _skillPanel.SetActive(false);
        if (!_hasInfo)
        {
            Close();
        }
    }

    public void Close()
    {
        UIManager.Instance.PopUp.Close();
    }
}
