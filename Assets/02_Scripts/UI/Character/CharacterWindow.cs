using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterWindow : MonoBehaviour, IWindowable
{
    [SerializeField] private CharacterPresentation[] _characters;
    [SerializeField] private CharacterDetailView _detail;
    [SerializeField] private CharacterInventorySlot[] _slots;
    [SerializeField] private CharacterPopUp _characterPopUp;
    [SerializeField] private Text _silver;
    [SerializeField] private Text _gold;

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
        CharacterProgression characters = GameManager.Instance.Characters;
        int id = characters.Data.EquippedCharacterId;
        _detail.RefreshCharacter(GetPresentation(id), slot => _characterPopUp.ShowSkill(id, slot, false));
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].RefreshCharacter(_characters[i], this);
        }

        _silver.text = CurrencyFormatter.Format(characters.GetAmount(0, 0));
        _gold.text = CurrencyFormatter.Format(characters.GetAmount(0, 1));
    }

    public void OpenCharacter(int id)
    {
        _characterPopUp.ShowCharacter(id);
    }

    public void OpenWindow(int type)
    {
        UIManager.Instance.PopUp.Close();
        UIManager.Instance.Window.Open((WindowType)type);
    }

    public void OpenSettings()
    {
        UIManager.Instance.PopUp.Open(PopUpType.Settings);
    }
}
