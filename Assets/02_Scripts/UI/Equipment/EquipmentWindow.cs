using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentWindow : MonoBehaviour, IWindowable
{
    private static readonly string[] SLOT_NAMES = { "무기", "투구", "상의", "하의", "장갑", "신발", "목걸이", "반지" };

    [SerializeField] private CharacterWindow _characterWindow;
    [SerializeField] private EquipmentIconCatalog _equipmentArt;
    [SerializeField] private Animator _character;
    [SerializeField] private TextMeshProUGUI _silver;
    [SerializeField] private TextMeshProUGUI _gold;
    [SerializeField] private Button[] _equipped;
    [Tooltip("장착 및 인벤토리 슬롯의 등급 배경을 N, R, U, E 순서로 연결합니다.")]
    [SerializeField] private Sprite[] _rankSprites = System.Array.Empty<Sprite>();
    [SerializeField] private Button _inventoryTemplate;
    [SerializeField] private TextMeshProUGUI _empty;
    [SerializeField] private ScrollRect _inventoryScroll;
    [SerializeField] private EquipmentPopUp _equipmentPopUp;
    [SerializeField] private EquipmentFusionView _fusion;
    [SerializeField] private GameObject _equipmentOverview;
    [SerializeField] private Button _equipmentTab;
    [SerializeField] private Button _fusionTab;

    private readonly List<Button> _inventoryButtons = new();
    [SerializeField] private Button[] _filterButtons;
    private EquipmentSlotType _filter = EquipmentSlotType.None;

    private void Awake()
    {
        _fusion.Initialize(this);
        _equipmentTab.onClick.AddListener(() => SetFusionTab(false));
        _fusionTab.onClick.AddListener(OpenFusionTab);
        for (int index = 0; index < _filterButtons.Length; index++)
        {
            EquipmentSlotType slot = (EquipmentSlotType)(index - 1);
            _filterButtons[index].onClick.AddListener(() => SelectInventoryFilter(slot));
        }
        RefreshFilterColors();
    }

    public static string GetSlotName(EquipmentSlotType slot)
    {
        return SLOT_NAMES[(int)slot];
    }

    public void ResetUI()
    {
        LobbyWindow.BindCurrencyLabels(transform, ref _silver, ref _gold);
        UIManager.Instance.PopUp.Close();
        CharacterProgression characters = GameManager.Instance.Characters;
        CharacterDetailView.ApplyPresentation(_character,
            _characterWindow.GetPresentation(characters.Data.EquippedCharacterId));
        _silver.text = CurrencyFormatter.Format(characters.GetAmount(0, 0));
        _gold.text = CurrencyFormatter.Format(characters.GetAmount(0, 1));
        SetFusionTab(false);
    }

    internal void OpenFusionTab() => SetFusionTab(true);

    private void SetFusionTab(bool showFusion)
    {
        _fusion.gameObject.SetActive(showFusion);
        _equipmentOverview.SetActive(!showFusion);
        _fusion.ResetSelection();
        _equipmentTab.interactable = showFusion;
        _fusionTab.interactable = !showFusion;
        RefreshEquipment();
        ScrollInventoryToTop();
    }

    internal void PrepareInventorySlots()
    {
        if (_inventoryButtons.Count == 0)
        {
            foreach (Transform child in _inventoryScroll.content)
            {
                if (child.TryGetComponent(out Button slot))
                {
                    _inventoryButtons.Add(slot);
                }
            }
        }

        int count = GameManager.Instance.Equipment.Items.Count;
        while (_inventoryButtons.Count < count)
        {
            Button slot = Instantiate(_inventoryTemplate, _inventoryScroll.content);
            slot.name = $"Inventory_Slot_{_inventoryButtons.Count + 1:00}";
            slot.gameObject.SetActive(false);
            _inventoryButtons.Add(slot);
        }
    }

    public void RefreshEquipment()
    {
        EquipmentInventory inventory = GameManager.Instance.Equipment;
        PrepareInventorySlots();
        for (int i = 0; i < _equipped.Length; i++)
        {
            EquipmentSlotType slot = (EquipmentSlotType)i;
            EquipmentInstanceData item = inventory.GetEquipped(slot);
            Button button = _equipped[i];
            Bind(button, item, GetSlotName(slot), false, inventory, false);
        }

        int index = 0;
        var visibleItems = inventory.Items
            .Select(item => (Item: item, Slot: inventory.GetSlot(item)))
            .Where(entry => entry.Slot != EquipmentSlotType.None
                && (_filter == EquipmentSlotType.None || entry.Slot == _filter))
            .Select(entry => (entry.Item, entry.Slot, IsSpecial: inventory.Definitions.GetInt(entry.Item.DefinitionId, 2) == 1))
            .OrderByDescending(entry => entry.Item.IsEquipped)
            .ThenByDescending(entry => GetRank(entry.Item, inventory.Definitions))
            .ThenBy(entry => entry.Slot)
            .ThenByDescending(entry => entry.IsSpecial)
            .ThenBy(entry => entry.Item.DefinitionId);

        foreach (var entry in visibleItems)
        {
            EquipmentInstanceData item = entry.Item;

            Button button = _inventoryButtons[index++];
            Bind(button, item, GetItemName(inventory, item), entry.IsSpecial, inventory, true);
            button.gameObject.SetActive(true);
        }

        _empty.text = _filter == EquipmentSlotType.None ? "보유 장비가 없습니다." : "해당 부위의 장비가 없습니다.";
        _empty.gameObject.SetActive(index == 0);
        for (int i = index; i < _inventoryButtons.Count; i++)
        {
            _inventoryButtons[i].gameObject.SetActive(false);
        }
    }

    internal void ApplyRankImage(Image rankImage, int rank)
    {
        rankImage.sprite = GetRankSprite(rank);
        rankImage.enabled = rankImage.sprite != null;
    }

    internal Sprite GetRankSprite(int rank)
    {
        return (uint)rank < (uint)_rankSprites.Length ? _rankSprites[rank] : null;
    }

    private static int GetRank(EquipmentInstanceData item, CsvTable definitions)
    {
        return item.Rank >= 0 ? item.Rank : definitions.GetInt(item.DefinitionId, 4);
    }

    private static string GetItemName(EquipmentInventory inventory, EquipmentInstanceData item)
    {
        return inventory.Definitions.GetString(item.DefinitionId, 0);
    }

    internal void ApplyItemImage(Image icon, EquipmentInstanceData item)
    {
        icon.sprite = item != null ? GetItemIcon(item.DefinitionId) : null;
        icon.enabled = icon.sprite != null;
    }

    internal Sprite GetItemIcon(int definitionId) => _equipmentArt.GetIcon(definitionId);

    private void Bind(Button button, EquipmentInstanceData item, string label, bool isSpecial,
        EquipmentInventory inventory, bool isInventorySlot)
    {
        Sprite icon = item != null ? GetItemIcon(item.DefinitionId) : null;
        Sprite rank = item != null ? GetRankSprite(inventory.GetRank(item)) : null;
        bool isFusion = _fusion.gameObject.activeSelf;
        string selection = isFusion && isInventorySlot ? _fusion.GetSlotLabel(item) : "";
        button.GetComponent<EquipmentSlotView>().Render(icon, rank, label, item != null && item.IsEquipped, isSpecial, selection);
        button.interactable = item != null && (!isFusion || isInventorySlot);
        button.onClick.RemoveAllListeners();
        if (button.interactable)
        {
            if (isFusion)
            {
                button.onClick.AddListener(() => _fusion.Select(item));
            }
            else
            {
                button.onClick.AddListener(() => _equipmentPopUp.ShowEquipment(item));
            }
        }
    }

    private void SelectInventoryFilter(EquipmentSlotType slot)
    {
        if (_filter == slot)
        {
            return;
        }

        _filter = slot;
        RefreshFilterColors();
        RefreshEquipment();
        ScrollInventoryToTop();
    }

    private void RefreshFilterColors()
    {
        for (int index = 0; index < _filterButtons.Length; index++)
        {
            _filterButtons[index].image.color = index == (int)_filter + 1
                ? new Color(.49f, .30f, .12f)
                : new Color(.23f, .19f, .16f);
        }
    }

    private void ScrollInventoryToTop()
    {
        _inventoryScroll.StopMovement();
        Canvas.ForceUpdateCanvases();
        _inventoryScroll.verticalNormalizedPosition = 1f;
    }

    public void OpenSettings()
    {
        UIManager.Instance.PopUp.Open(PopUpType.Settings);
    }
}
