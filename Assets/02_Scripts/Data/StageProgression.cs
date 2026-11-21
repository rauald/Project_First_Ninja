using System;
using System.Collections.Generic;
using System.Linq;

public sealed class StageProgression
{
    private const int FIRST_REWARD = 7;
    private const int REPEAT_REWARD = 10;
    private const int SILVER = 0;
    private const int GOLD = 1;

    private readonly CsvTable _stages;
    private readonly SaveLoadData _data;
    private readonly Func<bool> _save;
    private readonly int[] _stageIds;

    public IReadOnlyList<int> StageIds => _stageIds;

    public StageProgression(CsvTable stages, SaveLoadData data, Func<bool> save)
    {
        _stages = stages;
        _data = data;
        _save = save;
        _stageIds = stages.GetIds().OrderBy(id => id).ToArray();
        _data.ClearedStageIds ??= new List<int>();
    }

    public string GetStageName(int id) => _stages.GetString(id, 0);
    public int GetMapType(int id) => _stages.GetInt(id, 1);
    public bool HasCleared(int id) => _data.ClearedStageIds.Contains(id);

    public bool CanEnter(int id)
    {
        int index = Array.BinarySearch(_stageIds, id);
        return index >= 0 && (index == 0 || HasCleared(id) || HasCleared(_stageIds[index - 1]));
    }

    public bool TryRecordClear(int id, out long silver, out long gold)
    {
        silver = gold = 0;
        if (!CanEnter(id))
        {
            return false;
        }

        bool isFirstClear = !HasCleared(id);
        long[] rewards = new long[2];
        if (!TryAddReward(id, REPEAT_REWARD, rewards)
            || (isFirstClear && !TryAddReward(id, FIRST_REWARD, rewards)))
        {
            return false;
        }

        List<CharacterMaterialData> materials = _data.CharacterMaterials;
        CharacterMaterialData[] balances = new CharacterMaterialData[rewards.Length];
        for (int currency = 0; currency < rewards.Length; currency++)
        {
            balances[currency] = materials.Find(item => item.ItemType == 0 && item.DetailType == currency);
            if (balances[currency]?.Amount > long.MaxValue - rewards[currency])
            {
                return false;
            }
        }

        int originalCount = materials.Count;
        for (int currency = 0; currency < rewards.Length; currency++)
        {
            if (balances[currency] != null)
            {
                balances[currency].Amount += rewards[currency];
            }
            else if (rewards[currency] > 0)
            {
                materials.Add(new CharacterMaterialData
                {
                    ItemType = 0,
                    DetailType = currency,
                    Amount = rewards[currency]
                });
            }
        }

        if (isFirstClear)
        {
            _data.ClearedStageIds.Add(id);
        }

        if (!_save())
        {
            if (isFirstClear)
            {
                _data.ClearedStageIds.Remove(id);
            }

            for (int currency = 0; currency < rewards.Length; currency++)
            {
                if (balances[currency] != null)
                {
                    balances[currency].Amount -= rewards[currency];
                }
            }

            materials.RemoveRange(originalCount, materials.Count - originalCount);
            return false;
        }

        silver = rewards[SILVER];
        gold = rewards[GOLD];
        return true;
    }

    private bool TryAddReward(int id, int field, long[] rewards)
    {
        int itemType = _stages.GetInt(id, field);
        int currency = _stages.GetInt(id, field + 1);
        int amount = _stages.GetInt(id, field + 2);
        if (itemType != 0 || currency < SILVER || currency > GOLD || amount < 0)
        {
            return false;
        }

        rewards[currency] += amount;
        return true;
    }
}
