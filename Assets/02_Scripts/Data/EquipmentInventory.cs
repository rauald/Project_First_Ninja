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

    // EquipData의 장비 능력치 타입 번호다. 전투에 직접 합산하는 항목만 읽는다.
    public enum EffectType
    {
        None = -1,
        FlatAttack = 1, FlatDefense = 2, FlatHealth = 3,
        CriticalChance = 4, CriticalDamage = 5, Cooldown = 6, Dodge = 7,
        BasicDamage = 8, NeutralDamage = 9, FireDamage = 10, WaterDamage = 11,
        WoodDamage = 12, EarthDamage = 13, WindDamage = 14,
        GoldGain = 24, CriticalChanceAndDamage = 26, SkillRange = 28,
        AllSkillDamage = 29, AwakeningDamage = 30, StartingShield = 31,
        MoveSpeed = 32, PickupRange = 34, ExperienceGain = 38,
        DamageReduction = 39, IgnoreDefense = 40, NormalEnemyDamage = 41, BossDamage = 42,
        Attack = 45, Defense = 46, Health = 47,
        Max
    }

    // 슬롯마다 장착한 장비 하나의 기본·레벨·해금 등급 효과를 합산한다.
    public int[] GetEquippedEffectTotals(CsvTable levelTable)
    {
        int[] totals = new int[(int)EffectType.Max];
        for (int slot = 0; slot < (int)EquipmentSlotType.Max; slot++)
        {
            EquipmentInstanceData item = GetEquipped((EquipmentSlotType)slot);
            if (item == null)
            {
                continue;
            }

            int id = item.DefinitionId;
            int birthRank = Definitions.GetInt(id, 2);
            int level = item.Level > 0 ? item.Level : Definitions.GetInt(id, 3);
            int rank = item.Rank >= 0 ? item.Rank : Definitions.GetInt(id, 4);
            AddEffect(totals, Definitions, id, 5);
            AddEffect(totals, Definitions, id, 7);
            if (birthRank == 1)
            {
                AddEffect(totals, Definitions, id, 17);
            }

            // 획득한 등급까지 누적하고 각 성장 행에 해당하는 완료한 레벨 횟수를 곱한다.
            for (int tier = 0; tier <= Math.Min(rank, 3); tier++)
            {
                AddEffect(totals, Definitions, id, 9 + tier * 2);
            }

            foreach (int row in levelTable.GetIds())
            {
                if (levelTable.GetInt(row, 0) != slot || levelTable.GetInt(row, 1) != birthRank)
                {
                    continue;
                }

                int first = Math.Max(Definitions.GetInt(id, 3), levelTable.GetInt(row, 2));
                int last = Math.Min(level - 1, levelTable.GetInt(row, 3));
                int upgrades = last - first + 1;
                if (upgrades > 0)
                {
                    AddEffect(totals, levelTable, row, 4, upgrades);
                    AddEffect(totals, levelTable, row, 6, upgrades);
                }
            }
        }

        return totals;
    }

    // 타입 번호가 유효한 효과만 기록하고 빈 능력치 칸은 건너뛴다.
    private static void AddEffect(int[] totals, CsvTable table, int row, int field, int count = 1)
    {
        int type = table.GetInt(row, field);
        if (type > 0 && type < totals.Length)
        {
            totals[type] += table.GetInt(row, field + 1) * count;
        }
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
