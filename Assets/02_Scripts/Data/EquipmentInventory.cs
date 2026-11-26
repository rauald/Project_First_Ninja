using System;
using System.Collections.Generic;
using System.Linq;

public sealed class EquipmentInventory
{
    private readonly List<EquipmentInstanceData> _items;
    private readonly SaveLoadData _data;
    private readonly Func<string, CsvTable> _getTable;
    private readonly Func<bool> _save;

    public CsvTable Definitions { get; }
    public IReadOnlyList<EquipmentInstanceData> Items => _items;

    public EquipmentInventory(SaveLoadData data, Func<string, CsvTable> getTable, Func<bool> save)
    {
        _items = data.Equipment ??= new List<EquipmentInstanceData>();
        _data = data;
        _getTable = getTable;
        Definitions = getTable("EquipData");
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


    public int GetLevel(EquipmentInstanceData item)
    {
        return item.Level > 0 ? item.Level : Definitions.GetInt(item.DefinitionId, 3);
    }

    public int GetRank(EquipmentInstanceData item)
    {
        return item.Rank >= 0 ? item.Rank : Definitions.GetInt(item.DefinitionId, 4);
    }

    public bool GetLevelUpCost(EquipmentInstanceData item, out CharacterMaterialData[] cost)
    {
        cost = Array.Empty<CharacterMaterialData>();
        if (GetSlot(item) == EquipmentSlotType.None || !_items.Contains(item))
        {
            return false;
        }

        int level = GetLevel(item);
        int birthRank = Definitions.GetInt(item.DefinitionId, 2);
        CsvTable table = _getTable("EquipLevelUpMaterials");
        foreach (int row in table.GetIds())
        {
            if (table.GetInt(row, 0) == birthRank && level >= table.GetInt(row, 1) && level <= table.GetInt(row, 2))
            {
                cost = new[] { ReadCost(table, row, 3), ReadCost(table, row, 6) };
                return true;
            }
        }

        return false;
    }

    private static CharacterMaterialData ReadCost(CsvTable table, int row, int field)
    {
        return new CharacterMaterialData
        {
            ItemType = table.GetInt(row, field),
            DetailType = table.GetInt(row, field + 1),
            Amount = table.GetInt(row, field + 2)
        };
    }

    public bool TryLevelUp(EquipmentInstanceData item, out string message)
    {
        if (!GetLevelUpCost(item, out CharacterMaterialData[] costs))
        {
            message = "더 이상 레벨업할 수 없습니다.";
            return false;
        }

        var payments = new List<(CharacterMaterialData Balance, long Amount)>();
        foreach (var cost in costs.Where(value => value.Amount > 0).GroupBy(value => (value.ItemType, value.DetailType)))
        {
            long amount = cost.Sum(value => value.Amount);
            CharacterMaterialData balance = _data.CharacterMaterials.Find(value =>
                value.ItemType == cost.Key.ItemType && value.DetailType == cost.Key.DetailType);
            if (balance == null || balance.Amount < amount)
            {
                message = "레벨업 재료가 부족합니다.";
                return false;
            }

            payments.Add((balance, amount));
        }

        int previousLevel = item.Level;
        item.Level = GetLevel(item) + 1;
        foreach (var payment in payments)
        {
            payment.Balance.Amount -= payment.Amount;
        }
        if (_save())
        {
            message = "레벨업했습니다.";
            return true;
        }

        item.Level = previousLevel;
        foreach (var payment in payments)
        {
            payment.Balance.Amount += payment.Amount;
        }
        message = "저장하지 못했습니다. 다시 시도해 주세요.";
        return false;
    }

    public sealed class RankUpCost
    {
        public EquipmentSlotType RequiredSlot;
        public int RequiredBirthRank;
        public int RequiredRank;
        public int RequiredCount;
        public EquipmentSlotType AdditionalSlot;
        public int AdditionalRank;
        public int AdditionalCount;
    }

    public RankUpCost GetRankUpCost(EquipmentInstanceData item)
    {
        EquipmentSlotType slot = GetSlot(item);
        if (slot == EquipmentSlotType.None || !_items.Contains(item))
        {
            return null;
        }

        int rank = GetRank(item);
        int birthRank = Definitions.GetInt(item.DefinitionId, 2);
        CsvTable table = _getTable("EquipRankUpMaterials");
        foreach (int row in table.GetIds())
        {
            if (table.GetInt(row, 0) == (int)slot && table.GetInt(row, 1) == birthRank && table.GetInt(row, 4) == rank)
            {
                return new RankUpCost
                {
                    RequiredSlot = (EquipmentSlotType)table.GetInt(row, 2),
                    RequiredBirthRank = table.GetInt(row, 3),
                    RequiredRank = table.GetInt(row, 4),
                    RequiredCount = table.GetInt(row, 5),
                    AdditionalSlot = (EquipmentSlotType)table.GetInt(row, 6),
                    AdditionalRank = table.GetInt(row, 7),
                    AdditionalCount = table.GetInt(row, 8)
                };
            }
        }

        return null;
    }

    private bool MatchesRequiredRankMaterial(EquipmentInstanceData candidate, EquipmentInstanceData item, RankUpCost cost)
    {
        return candidate.DefinitionId == item.DefinitionId
            && GetSlot(candidate) == cost.RequiredSlot && GetRank(candidate) == cost.RequiredRank
            && Definitions.GetInt(candidate.DefinitionId, 2) == cost.RequiredBirthRank;
    }

    private bool MatchesAdditionalRankMaterial(EquipmentInstanceData candidate, RankUpCost cost)
    {
        return GetSlot(candidate) == cost.AdditionalSlot && GetRank(candidate) == cost.AdditionalRank;
    }

    public IEnumerable<EquipmentInstanceData> GetRankUpCandidates(EquipmentInstanceData item, RankUpCost cost,
        bool isRequired)
    {
        return _items.Where(candidate => IsRankUpMaterial(candidate, item, cost, isRequired));
    }

    private bool IsRankUpMaterial(EquipmentInstanceData candidate, EquipmentInstanceData item, RankUpCost cost,
        bool isRequired)
    {
        return candidate != null && candidate != item && !candidate.IsEquipped
            && (isRequired ? MatchesRequiredRankMaterial(candidate, item, cost) : MatchesAdditionalRankMaterial(candidate, cost));
    }

    public bool TryRankUp(EquipmentInstanceData item, IReadOnlyCollection<EquipmentInstanceData> requiredMaterials,
        IReadOnlyCollection<EquipmentInstanceData> additionalMaterials, out string message)
    {
        RankUpCost cost = GetRankUpCost(item);
        if (cost == null)
        {
            message = "더 이상 등급업할 수 없습니다.";
            return false;
        }

        var chosen = new HashSet<EquipmentInstanceData>();
        if (requiredMaterials.Count != cost.RequiredCount || additionalMaterials.Count != cost.AdditionalCount
            || !requiredMaterials.All(candidate => _items.Contains(candidate)
                && IsRankUpMaterial(candidate, item, cost, true) && chosen.Add(candidate))
            || !additionalMaterials.All(candidate => _items.Contains(candidate)
                && IsRankUpMaterial(candidate, item, cost, false) && chosen.Add(candidate)))
        {
            message = "필수 재료와 추가 재료를 각각 수량에 맞게 선택해 주세요.";
            return false;
        }

        var previousItems = new List<EquipmentInstanceData>(_items);
        int previousRank = item.Rank;
        item.Rank = GetRank(item) + 1;
        _items.RemoveAll(chosen.Contains);
        if (_save())
        {
            message = "등급업했습니다.";
            return true;
        }

        item.Rank = previousRank;
        _items.Clear();
        _items.AddRange(previousItems);
        message = "저장하지 못했습니다. 다시 시도해 주세요.";
        return false;
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

    public bool TryUnequip(EquipmentInstanceData item)
    {
        if (item == null || !item.IsEquipped || !_items.Contains(item))
        {
            return false;
        }

        item.IsEquipped = false;
        if (_save())
        {
            return true;
        }

        item.IsEquipped = true;
        return false;
    }
}
