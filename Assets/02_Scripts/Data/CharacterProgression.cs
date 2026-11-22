using System;
using System.Collections.Generic;

public sealed class CharacterProgression
{
    private const long TEST_SILVER_AMOUNT = 1_000_000_000;
    private readonly Func<bool> _save;
    private readonly Func<string, CsvTable> _getTable;

    public SaveLoadData Data { get; }
    public CsvTable Characters => _getTable("CharacterData");

    public CharacterProgression(SaveLoadData data, Func<string, CsvTable> getTable, Func<bool> save)
    {
        _save = save;
        _getTable = getTable;
        Data = data;
    }

    public void Initialize()
    {
        Data.Characters ??= new List<CharacterProgressData>();
        Data.CharacterMaterials ??= new List<CharacterMaterialData>();
        foreach (int id in Characters.GetIds())
        {
            if (Data.Characters.Exists(value => value.CharacterId == id))
            {
                continue;
            }

            Data.Characters.Add(new CharacterProgressData
            {
                CharacterId = id,
                IsUnlocked = id == 1,
                Level = Characters.GetInt(id, 4),
                Stars = Characters.GetInt(id, 5),
                Rank = Characters.GetInt(id, 6)
            });
        }

        if (!Characters.Contains(Data.EquippedCharacterId) || !GetCharacter(Data.EquippedCharacterId).IsAvailable)
        {
            Data.EquippedCharacterId = Data.Characters.Find(value => value.IsAvailable)?.CharacterId ?? 1;
        }

        GrantTestSilver();
    }

    private void GrantTestSilver()
    {
        if (Data.HasReceivedTestSilver)
        {
            return;
        }

        CharacterMaterialData silver = Data.CharacterMaterials.Find(value => value.ItemType == 0 && value.DetailType == 0);
        if (silver == null)
        {
            silver = new CharacterMaterialData { ItemType = 0, DetailType = 0 };
            Data.CharacterMaterials.Add(silver);
        }

        silver.Amount = Math.Max(silver.Amount, TEST_SILVER_AMOUNT);
        Data.HasReceivedTestSilver = true;
        _save();
    }

    public CharacterProgressData GetCharacter(int id)
    {
        return Data.Characters.Find(value => value.CharacterId == id);
    }

    public long GetAmount(int itemType, int detailType)
    {
        return Data.CharacterMaterials.Find(value => value.ItemType == itemType && value.DetailType == detailType)?.Amount ?? 0;
    }

    public bool EquipCharacter(int id, out string message)
    {
        if (!GetCharacter(id).IsAvailable)
        {
            message = "아직 해금하지 않은 캐릭터입니다.";
            return false;
        }

        int previous = Data.EquippedCharacterId;
        Data.EquippedCharacterId = id;
        if (!_save())
        {
            Data.EquippedCharacterId = previous;
            message = "저장하지 못했습니다. 다시 시도해 주세요.";
            return false;
        }

        message = "캐릭터를 장착했습니다.";
        return true;
    }

    public int[] GetStats(int id)
    {
        CharacterProgressData state = GetCharacter(id);
        int[] stats = { Characters.GetInt(id, 8), Characters.GetInt(id, 9), Characters.GetInt(id, 10) };
        if (!state.IsAvailable)
        {
            return stats;
        }

        int birthRank = Characters.GetInt(id, 3);
        CsvTable levels = _getTable("CharacterLevelUpData");
        // 구간은 성장 전 레벨을 기준으로 한다. 1레벨 기본 능력치에 완료한 성장만 더한다.
        for (int level = 1; level < state.Level; level++)
        {
            foreach (int row in levels.GetIds())
            {
                if (levels.GetInt(row, 0) == birthRank && levels.GetInt(row, 1) <= level && level <= levels.GetInt(row, 2))
                {
                    AddStats(stats, levels, row, 3);
                    break;
                }
            }
        }

        AddGrowthStats(stats, "CharacterStarUpData", birthRank, state.Stars);
        AddGrowthStats(stats, "CharacterRankUpData", birthRank, state.Rank);
        return stats;
    }

    private void AddGrowthStats(int[] stats, string name, int birthRank, int count)
    {
        CsvTable table = _getTable(name);
        foreach (int row in table.GetIds())
        {
            if (table.GetInt(row, 0) == birthRank && table.GetInt(row, 1) < count)
            {
                AddStats(stats, table, row, 2);
            }
        }
    }

    private static void AddStats(int[] stats, CsvTable table, int row, int firstField)
    {
        for (int i = 0; i < stats.Length; i++)
        {
            stats[i] += table.GetInt(row, firstField + i);
        }
    }

    public bool GetGrowthCost(int id, CharacterGrowthType type, out List<CharacterMaterialData> cost, out string message)
    {
        cost = new List<CharacterMaterialData>();
        CharacterProgressData state = GetCharacter(id);
        if (!state.IsAvailable)
        {
            message = "아직 해금하지 않은 캐릭터입니다.";
            return false;
        }

        int element = Characters.GetInt(id, 7);
        int row;
        CsvTable table;
        switch (type)
        {
            case CharacterGrowthType.Level:
                table = _getTable("CharacterLevelUpMaterialsData");
                row = state.Level;
                break;
            case CharacterGrowthType.Star:
                table = _getTable("CharacterStarUpMaterialsData");
                row = element * 5 + state.Stars + 1;
                if (state.Stars >= 5)
                {
                    message = "최대 성급입니다.";
                    return false;
                }
                break;
            case CharacterGrowthType.Rank:
                table = _getTable("CharacterRankUpMaterialsData");
                row = element * 3 + state.Rank + 1;
                if (state.Rank >= 3)
                {
                    message = "최대 등급입니다.";
                    return false;
                }
                break;
            default:
                message = "지원하지 않는 성장입니다.";
                return false;
        }

        if (!table.Contains(row))
        {
            message = "최대 레벨입니다.";
            return false;
        }

        int firstField = type == CharacterGrowthType.Level ? 0 : 1;
        cost.Add(ReadCost(table, row, firstField));
        if (type == CharacterGrowthType.Star)
        {
            cost.Add(ReadCost(table, row, firstField + 3));
        }

        message = string.Empty;
        return true;
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

    public bool GrowCharacter(int id, CharacterGrowthType type, out string message)
    {
        if (!GetGrowthCost(id, type, out List<CharacterMaterialData> cost, out message))
        {
            return false;
        }

        foreach (CharacterMaterialData item in cost)
        {
            if (GetAmount(item.ItemType, item.DetailType) < item.Amount)
            {
                message = item.ItemType == 0 ? "재화가 부족합니다." : "재료가 부족합니다.";
                return false;
            }
        }

        CharacterProgressData state = GetCharacter(id);
        int previousLevel = state.Level;
        int previousStars = state.Stars;
        int previousRank = state.Rank;
        foreach (CharacterMaterialData item in cost)
        {
            Data.CharacterMaterials.Find(value => value.ItemType == item.ItemType && value.DetailType == item.DetailType).Amount -= item.Amount;
        }

        switch (type)
        {
            case CharacterGrowthType.Level: state.Level++; break;
            case CharacterGrowthType.Star: state.Stars++; break;
            case CharacterGrowthType.Rank: state.Rank++; break;
        }

        if (!_save())
        {
            state.Level = previousLevel;
            state.Stars = previousStars;
            state.Rank = previousRank;
            foreach (CharacterMaterialData item in cost)
            {
                Data.CharacterMaterials.Find(value => value.ItemType == item.ItemType && value.DetailType == item.DetailType).Amount += item.Amount;
            }

            message = "저장하지 못했습니다. 변경을 취소했습니다.";
            return false;
        }

        message = "성장했습니다.";
        return true;
    }
}
