using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentWindow : MonoBehaviour, IWindowable
{
    private static readonly string[] SLOT_NAMES = { "무기", "투구", "상의", "하의", "장갑", "신발", "목걸이", "반지" };

    [SerializeField] private CharacterWindow _characterWindow;
    [SerializeField] private Animator _character;
    [SerializeField] private Text _silver;
    [SerializeField] private Text _gold;
    [SerializeField] private Button[] _equipped;
    [SerializeField] private Button _inventoryTemplate;
    [SerializeField] private Text _empty;
    [SerializeField] private ScrollRect _inventoryScroll;
    [SerializeField] private EquipmentPopUp _equipmentPopUp;

    private readonly List<Button> _inventoryButtons = new();

    public static string GetSlotName(EquipmentSlotType slot)
    {
        return SLOT_NAMES[(int)slot];
    }

    public void ResetUI()
    {
        UIManager.Instance.PopUp.Close();
        CharacterProgression characters = GameManager.Instance.Characters;
        CharacterDetailView.ApplyPresentation(_character,
            _characterWindow.GetPresentation(characters.Data.EquippedCharacterId));
        _silver.text = CurrencyFormatter.Format(characters.GetAmount(0, 0));
        _gold.text = CurrencyFormatter.Format(characters.GetAmount(0, 1));
        RefreshEquipment();
        Canvas.ForceUpdateCanvases();
        _inventoryScroll.verticalNormalizedPosition = 1f;
    }

    public void RefreshEquipment()
    {
        EquipmentInventory inventory = GameManager.Instance.Equipment;
        for (int i = 0; i < _equipped.Length; i++)
        {
            EquipmentSlotType slot = (EquipmentSlotType)i;
            EquipmentInstanceData item = inventory.GetEquipped(slot);
            Bind(_equipped[i], item, item == null ? GetSlotName(slot) : GetSlotName(slot) + "\n" + GetItemName(inventory, item));
        }

        int index = 0;
        foreach (EquipmentInstanceData item in inventory.Items)
        {
            if (inventory.GetSlot(item) == EquipmentSlotType.None)
            {
                continue;
            }

            if (index == _inventoryButtons.Count)
            {
                _inventoryButtons.Add(Instantiate(_inventoryTemplate, _inventoryTemplate.transform.parent));
            }

            Button button = _inventoryButtons[index++];
            Bind(button, item, GetItemName(inventory, item) + (item.IsEquipped ? "\n장착 중" : ""));
            button.gameObject.SetActive(true);
        }

        _empty.gameObject.SetActive(index == 0);
        for (int i = index; i < _inventoryButtons.Count; i++)
        {
            _inventoryButtons[i].gameObject.SetActive(false);
        }
    }

    private static string GetItemName(EquipmentInventory inventory, EquipmentInstanceData item)
    {
        return inventory.Definitions.GetString(item.DefinitionId, 0);
    }

    private void Bind(Button button, EquipmentInstanceData item, string label)
    {
        button.GetComponentInChildren<Text>(true).text = label;
        button.interactable = item != null;
        button.onClick.RemoveAllListeners();
        if (item != null)
        {
            button.onClick.AddListener(() => _equipmentPopUp.ShowEquipment(item));
        }
    }
}
