using System;
using System.Collections.Generic;

public sealed class EquipmentInventory
{
    private readonly List<EquipmentInstanceData> _items;
    private readonly Func<bool> _save;

    public CsvTable Definitions { get; }
    public IReadOnlyList<EquipmentInstanceData> Items => _items;

    public EquipmentInventory(SaveLoadData data, CsvTable definitions, Func<bool> save)
    {
        _items = data.Equipment ??= new List<EquipmentInstanceData>();
        Definitions = definitions;
        _save = save;
    }

    public EquipmentSlotType GetSlot(EquipmentInstanceData item)
    {
        if (item == null || !Definitions.Contains(item.DefinitionId))
        {
            return EquipmentSlotType.None;
        }

        int slot = Definitions.GetInt(item.DefinitionId, 1);
        return slot >= 0 && slot < (int)EquipmentSlotType.Max ? (EquipmentSlotType)slot : EquipmentSlotType.None;
    }

    public EquipmentInstanceData GetEquipped(EquipmentSlotType slot)
    {
        return _items.Find(item => item != null && item.IsEquipped && GetSlot(item) == slot);
    }

    public bool TryEquip(EquipmentInstanceData item)
    {
        EquipmentSlotType slot = GetSlot(item);
        if (slot == EquipmentSlotType.None || !_items.Contains(item))
        {
            return false;
        }

        EquipmentInstanceData previous = GetEquipped(slot);
        if (previous == item)
        {
            return true;
        }

        if (previous != null)
        {
            previous.IsEquipped = false;
        }

        item.IsEquipped = true;
        if (_save())
        {
            return true;
        }

        item.IsEquipped = false;
        if (previous != null)
        {
            previous.IsEquipped = true;
        }

        return false;
    }
}
