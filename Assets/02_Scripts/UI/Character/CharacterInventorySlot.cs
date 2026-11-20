using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterInventorySlot : MonoBehaviour
{
    [SerializeField] private Image _portrait;
    [SerializeField] private Text _label;
    [SerializeField] private GameObject _lock;
    [SerializeField] private GameObject _equipped;
    [SerializeField] private Button _button;

    public void RefreshCharacter(CharacterPresentation presentation, CharacterWindow window)
    {
        CharacterProgression characters = GameManager.Instance.Characters;
        int id = presentation.CharacterId;
        CharacterProgressData state = characters.GetCharacter(id);
        _portrait.sprite = presentation.Portrait;
        _portrait.color = state.IsUnlocked ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
        _label.text = characters.Characters.GetString(id, 0) + (state.IsUnlocked ? $"\nLv. {state.Level}" : "\n미해금");
        _lock.SetActive(!state.IsUnlocked);
        _equipped.SetActive(characters.Data.EquippedCharacterId == id);
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => window.OpenCharacter(id));
    }
}
