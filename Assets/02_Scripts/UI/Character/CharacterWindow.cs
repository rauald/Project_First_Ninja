using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterWindow : MonoBehaviour, IWindowable
{
    [Tooltip("캐릭터 ID별 초상화, 애니메이터 컨트롤러와 스킬 아이콘을 편집합니다.")]
    [SerializeField] private CharacterPresentation[] _characters;
    [SerializeField] private CharacterDetailView _detail;
    [SerializeField] private CharacterInventorySlot[] _slots;
    [SerializeField] private CharacterPopUp _characterPopUp;
    [SerializeField] private TextMeshProUGUI _silver;
    [SerializeField] private TextMeshProUGUI _gold;

    public CharacterPresentation GetPresentation(int id)
    {
        return System.Array.Find(_characters, value => value.CharacterId == id);
    }

    public void ResetUI()
    {
        UIManager.Instance.PopUp.Close();
        RefreshUI();
    }

    public void RefreshUI()
    {
        LobbyWindow.BindCurrencyLabels(transform, ref _silver, ref _gold);
        CharacterProgression characters = GameManager.Instance.Characters;
        int id = characters.Data.EquippedCharacterId;
        _detail.RefreshCharacter(GetPresentation(id), slot => _characterPopUp.ShowSkill(id, slot, false));
        if (_slots == null || System.Array.Exists(_slots, slot => slot == null))
        {
            _slots = transform.Find("Inventory").GetComponentsInChildren<CharacterInventorySlot>(true);
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            bool hasData = i < _characters.Length;
            _slots[i].gameObject.SetActive(hasData);
            if (hasData)
            {
                _slots[i].RefreshCharacter(_characters[i], this);
            }
        }

        _silver.text = CurrencyFormatter.Format(characters.GetAmount(0, 0));
        _gold.text = CurrencyFormatter.Format(characters.GetAmount(0, 1));
    }

    public void OpenCharacter(int id)
    {
        _characterPopUp.ShowCharacter(id);
    }

    public void OpenSettings()
    {
        UIManager.Instance.PopUp.Open(PopUpType.Settings);
    }
}
