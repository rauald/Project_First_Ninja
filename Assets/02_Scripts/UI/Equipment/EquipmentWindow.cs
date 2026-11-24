using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentWindow : MonoBehaviour, IWindowable
{
    private const float FILTER_HEIGHT = 72f;
    private static readonly string[] SLOT_NAMES = { "무기", "투구", "상의", "하의", "장갑", "신발", "목걸이", "반지" };

    [SerializeField] private CharacterWindow _characterWindow;
    [SerializeField] private Animator _character;
    [SerializeField] private Text _silver;
    [SerializeField] private Text _gold;
    [SerializeField] private Button[] _equipped;
    [Tooltip("장착 및 인벤토리 슬롯의 등급 배경을 N, R, U, E 순서로 연결합니다.")]
    [SerializeField] private Sprite[] _rankSprites = System.Array.Empty<Sprite>();
    [SerializeField] private Button _inventoryTemplate;
    [SerializeField] private Text _empty;
    [SerializeField] private ScrollRect _inventoryScroll;
    [SerializeField] private EquipmentPopUp _equipmentPopUp;

    private readonly List<Button> _inventoryButtons = new();
    private ShopDrawPresentation _itemArt;
    private Image[] _filterBackgrounds;
    private EquipmentSlotType _filter = EquipmentSlotType.None;

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
        if (_filterBackgrounds == null)
        {
            CreateInventoryFilters();
        }

        RefreshEquipment();
        ScrollInventoryToTop();
    }

    public void RefreshEquipment()
    {
        EquipmentInventory inventory = GameManager.Instance.Equipment;
        if (_inventoryTemplate == null)
        {
            _inventoryTemplate = _inventoryScroll.content.Find("InventorySlotTemplate").GetComponent<Button>();
        }
        _inventoryTemplate.gameObject.SetActive(false);
        for (int i = 0; i < _equipped.Length; i++)
        {
            EquipmentSlotType slot = (EquipmentSlotType)i;
            EquipmentInstanceData item = inventory.GetEquipped(slot);
            Button button = _equipped[i];
            Bind(button, item, GetSlotName(slot), button.transform.Find("Equip")?.GetComponent<Image>());
            BindRank(button, item, inventory.Definitions);
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

            if (index == _inventoryButtons.Count)
            {
                Button slotButton = Instantiate(_inventoryTemplate, _inventoryScroll.content);
                CreateEquippedBadge(slotButton.transform);
                _inventoryButtons.Add(slotButton);
            }

            Button button = _inventoryButtons[index++];
            Image icon = button.transform.Find("Equip").GetComponent<Image>();
            Bind(button, item, GetItemName(inventory, item), icon);
            BindRank(button, item, inventory.Definitions);
            button.transform.Find("EquippedBadge").gameObject.SetActive(item.IsEquipped);
            button.transform.Find("Special").gameObject.SetActive(entry.IsSpecial);
            button.gameObject.SetActive(true);
        }

        _empty.text = _filter == EquipmentSlotType.None ? "보유 장비가 없습니다." : "해당 부위의 장비가 없습니다.";
        _empty.gameObject.SetActive(index == 0);
        for (int i = index; i < _inventoryButtons.Count; i++)
        {
            _inventoryButtons[i].gameObject.SetActive(false);
        }
    }

    private void BindRank(Button button, EquipmentInstanceData item, CsvTable definitions)
    {
        Image rankImage = button.transform.Find("Rank")?.GetComponent<Image>();
        if (rankImage == null)
        {
            return;
        }

        int rank = item == null ? -1 : GetRank(item, definitions);
        ApplyRankImage(rankImage, rank);
    }

    internal void ApplyRankImage(Image rankImage, int rank)
    {
        rankImage.sprite = (uint)rank < (uint)_rankSprites.Length ? _rankSprites[rank] : null;
        rankImage.enabled = rankImage.sprite != null;
    }

    private static int GetRank(EquipmentInstanceData item, CsvTable definitions)
    {
        return item.Rank >= 0 ? item.Rank : definitions.GetInt(item.DefinitionId, 4);
    }

    private static string GetItemName(EquipmentInventory inventory, EquipmentInstanceData item)
    {
        return inventory.Definitions.GetString(item.DefinitionId, 0);
    }

    private void CreateEquippedBadge(Transform slot)
    {
        var badge = new GameObject("EquippedBadge", typeof(RectTransform), typeof(Image));
        badge.layer = slot.gameObject.layer;
        var rect = (RectTransform)badge.transform;
        rect.SetParent(slot, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(0f, -32f);
        rect.offsetMax = Vector2.zero;

        var background = badge.GetComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = false;

        var title = new GameObject("Label", typeof(RectTransform), typeof(Text));
        title.layer = badge.layer;
        var titleRect = (RectTransform)title.transform;
        titleRect.SetParent(rect, false);
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        var label = title.GetComponent<Text>();
        label.font = _silver.font;
        label.fontSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "장착중";
        label.raycastTarget = false;
    }

    internal void ApplyItemImage(Image icon, EquipmentInstanceData item)
    {
        if (_itemArt == null)
        {
            _itemArt = UIManager.Instance.GetComponentInChildren<ShopDrawPresentation>(true);
        }

        icon.sprite = item != null ? _itemArt.GetEquipmentIcon(item.DefinitionId) : null;
        icon.enabled = icon.sprite != null;
    }

    private void Bind(Button button, EquipmentInstanceData item, string label, Image icon)
    {
        if (icon != null)
        {
            ApplyItemImage(icon, item);
        }

        if (button.transform.Find("Label")?.GetComponent<Text>() is Text text)
        {
            text.text = icon != null && icon.enabled ? "" : label;
        }
        button.interactable = item != null;
        button.onClick.RemoveAllListeners();
        if (item != null)
        {
            button.onClick.AddListener(() => _equipmentPopUp.ShowEquipment(item));
        }
    }

    private void CreateInventoryFilters()
    {
        var filterBar = new GameObject("InventoryFilters", typeof(RectTransform));
        filterBar.layer = _inventoryScroll.gameObject.layer;
        var bar = (RectTransform)filterBar.transform;
        bar.SetParent(_inventoryScroll.transform, false);
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = Vector2.one;
        bar.offsetMin = new Vector2(8f, -FILTER_HEIGHT);
        bar.offsetMax = new Vector2(-8f, 0f);
        _inventoryScroll.viewport.offsetMax -= new Vector2(0f, FILTER_HEIGHT);

        _filterBackgrounds = new Image[SLOT_NAMES.Length + 1];
        for (int index = 0; index < _filterBackgrounds.Length; index++)
        {
            EquipmentSlotType slot = (EquipmentSlotType)(index - 1);
            string title = index == 0 ? "전체" : GetSlotName(slot);
            var filterObject = new GameObject("Filter_" + slot, typeof(RectTransform), typeof(Image), typeof(Button));
            filterObject.layer = filterBar.layer;
            var rect = (RectTransform)filterObject.transform;
            rect.SetParent(bar, false);
            rect.anchorMin = new Vector2((float)index / _filterBackgrounds.Length, 0f);
            rect.anchorMax = new Vector2((float)(index + 1) / _filterBackgrounds.Length, 1f);
            rect.offsetMin = new Vector2(3f, 6f);
            rect.offsetMax = new Vector2(-3f, -6f);

            var background = filterObject.GetComponent<Image>();
            _filterBackgrounds[index] = background;
            var button = filterObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => SelectInventoryFilter(slot));

            var titleObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            titleObject.layer = filterBar.layer;
            var labelRect = (RectTransform)titleObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var label = titleObject.GetComponent<Text>();
            label.font = _silver.font;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 25;
            label.color = Color.white;
            label.text = title;
            label.raycastTarget = false;
        }

        RefreshFilterColors();
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
        for (int index = 0; index < _filterBackgrounds.Length; index++)
        {
            _filterBackgrounds[index].color = index == (int)_filter + 1
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
