using System.Collections.Generic;
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
    [SerializeField] private Text _skillTitle;
    [SerializeField] private Text _skillDescription;
    [SerializeField] private Image _skillIcon;
    [SerializeField] private GameObject _confirmPanel;
    [SerializeField] private Text _confirmText;
    [SerializeField] private Button _confirm;
    [SerializeField] private Button _cancel;

    [SerializeField] private Button _messageConfirm;
    [SerializeField] private Image[] _materialIcons;
    [SerializeField] private Text[] _materialAmounts;
    [SerializeField] private Sprite _silverIcon;
    [SerializeField] private Sprite _goldIcon;
    [SerializeField] private Sprite[] _fragmentIcons;
    [SerializeField] private Sprite[] _stoneIcons;

    private int _characterId;
    private CharacterGrowthType _pendingGrowth;
    private bool _isConfirming;
    private bool _hasInfo;

    public void ShowCharacter(int id)
    {
        UIManager.Instance.PopUp.Open(PopUpType.Character);
        _characterId = id;
        _hasInfo = true;
        _infoPanel.SetActive(true);
        _skillPanel.SetActive(false);
        _confirmPanel.SetActive(false);
        _isConfirming = false;
        RefreshInfo();
    }

    private void RefreshInfo()
    {
        CharacterProgression characters = GameManager.Instance.Characters;
        _detail.RefreshCharacter(_window.GetPresentation(_characterId), slot => ShowSkill(_characterId, slot, true));
        CharacterProgressData state = characters.GetCharacter(_characterId);
        _equip.interactable = state.IsUnlocked && characters.Data.EquippedCharacterId != _characterId;
        _equip.GetComponentInChildren<Text>().text = characters.Data.EquippedCharacterId == _characterId ? "장착 중" : "장착";
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
        if (_isConfirming)
        {
            return;
        }

        _pendingGrowth = (CharacterGrowthType)type;
        CharacterProgression characters = GameManager.Instance.Characters;
        if (!characters.GetGrowthCost(_characterId, _pendingGrowth, out List<CharacterMaterialData> cost, out string message))
        {
            ShowMessage(message);
            return;
        }

        string name = _pendingGrowth switch { CharacterGrowthType.Level => "레벨업", CharacterGrowthType.Star => "성급업", _ => "등급업" };
        _confirmText.text = $"{name} 하시겠습니까?";
        SetConfirmationMode(true);
        for (int i = 0; i < cost.Count; i++)
        {
            int slot = cost.Count == 1 ? 0 : i + 1;
            CharacterMaterialData item = cost[i];
            long owned = characters.GetAmount(item.ItemType, item.DetailType);
            Image icon = _materialIcons[slot];
            Text amount = _materialAmounts[slot];
            icon.sprite = item.ItemType switch
            {
                0 => item.DetailType == 0 ? _silverIcon : _goldIcon,
                1 => _fragmentIcons[item.DetailType],
                _ => _stoneIcons[item.DetailType]
            };
            amount.text = item.ItemType == 0
                ? $"{CurrencyFormatter.Format(owned)} / {CurrencyFormatter.Format(item.Amount)}"
                : $"{owned:N0} / {item.Amount:N0}";
            amount.color = owned >= item.Amount ? Color.white : new Color(1f, .3f, .3f);
            icon.gameObject.SetActive(true);
            amount.gameObject.SetActive(true);
        }

        _confirmPanel.SetActive(true);
    }

    public void ConfirmGrowth()
    {
        if (!_isConfirming)
        {
            _confirmPanel.SetActive(false);
            return;
        }

        // 확정 순간 다시 비용을 조회하고, 저장 성공 시에만 성장 결과를 유지한다.
        _isConfirming = false;
        GameManager.Instance.Characters.GrowCharacter(_characterId, _pendingGrowth, out string message);
        _window.RefreshUI();
        RefreshInfo();
        ShowMessage(message);
    }

    public void CancelGrowth()
    {
        _isConfirming = false;
        _confirmPanel.SetActive(false);
    }

    private void ShowMessage(string message)
    {
        _confirmText.text = message;
        SetConfirmationMode(false);
        _confirmPanel.SetActive(true);
    }

    private void SetConfirmationMode(bool isConfirming)
    {
        _isConfirming = isConfirming;
        _confirm.gameObject.SetActive(isConfirming);
        _cancel.gameObject.SetActive(isConfirming);
        _messageConfirm.gameObject.SetActive(!isConfirming);
        for (int i = 0; i < _materialIcons.Length; i++)
        {
            _materialIcons[i].gameObject.SetActive(false);
            _materialAmounts[i].gameObject.SetActive(false);
        }
    }

    public void ShowSkill(int id, int slot, bool hasInfo)
    {
        UIManager.Instance.PopUp.Open(PopUpType.Character);
        _characterId = id;
        _hasInfo = hasInfo;
        _infoPanel.SetActive(hasInfo);
        _confirmPanel.SetActive(false);
        CharacterPresentation presentation = _window.GetPresentation(id);
        CsvTable table = DataManager.Instance.GetTable(slot == 3 ? "BattleActiveSkillData" : "CharacterPassiveSkillData");
        CsvTable characterTable = GameManager.Instance.Characters.Characters;
        int row = slot == 3 ? 19 + characterTable.GetInt(id, 22) : characterTable.GetInt(id, 16 + slot * 2);
        _skillTitle.text = presentation.SkillNames[slot];
        _skillIcon.sprite = presentation.SkillIcons[slot];
        _skillDescription.text = table.Contains(row) ? table.GetLabel(row) : "스킬 정보가 아직 연결되지 않았습니다.";
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
        _isConfirming = false;
        UIManager.Instance.PopUp.Close();
    }
}
