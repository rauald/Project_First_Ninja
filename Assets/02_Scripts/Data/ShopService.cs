using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class ShopService
{
    private const int SILVER = 0;
    private const int GOLD = 1;
    private const string SAVE_ERROR = "저장하지 못했습니다. 다시 시도해 주세요.";
    private readonly SaveLoadData _data;
    private readonly Func<bool> _save;
    private readonly CsvTable _equipment;
    private readonly CsvTable _defaults;
    private readonly (int Id, int Weight)[][] _pools;
    private readonly Random _random = new();

    public ShopService(SaveLoadData data, Func<string, CsvTable> getTable, Func<bool> save)
    {
        _data = data;
        _save = save;
        _equipment = getTable("EquipData");
        _defaults = getTable("DefaultData");
        _pools = new[] { ReadPool(getTable("EquipNomalGacha")), ReadPool(getTable("EquipSpecialGacha")) };
    }

    public long GetDrawCost(bool isPremium, int count)
    {
        return count is 1 or 10
            ? GetValue(isPremium ? $"고급뽑기{count}회금자값" : $"일반뽑기{count}회은자값")
            : 0;
    }

    public long GetExchangeCost(int count)
    {
        return count is 1 or 10 or 100 ? GetValue($"금자{count}개교환은자값") : 0;
    }

    public (int Id, decimal Probability)[] GetOdds(bool isPremium)
    {
        var pool = _pools[isPremium ? 1 : 0];
        int total = GetTotal(pool);
        return Array.ConvertAll(pool, entry => (entry.Id, (decimal)entry.Weight / total));
    }

    public bool TryDraw(bool isPremium, int count, out int[] results, out string message)
    {
        results = Array.Empty<int>();
        int currency = isPremium ? GOLD : SILVER;
        long cost = GetDrawCost(isPremium, count);
        if (!TryGetPayment(currency, cost, out var payment, out message))
        {
            return false;
        }

        var pool = _pools[isPremium ? 1 : 0];
        int total = GetTotal(pool);
        int startIndex = _data.Equipment.Count;
        int[] ids = new int[count];
        for (int i = 0; i < count; i++)
        {
            int roll = _random.Next(total);
            foreach (var (id, weight) in pool)
            {
                roll -= weight;
                if (roll < 0)
                {
                    _data.Equipment.Add(new EquipmentInstanceData { DefinitionId = id, Level = 1, Rank = 0 });
                    ids[i] = id;
                    break;
                }
            }
        }

        payment.Amount -= cost;
        if (!_save())
        {
            payment.Amount += cost;
            _data.Equipment.RemoveRange(startIndex, count);
            message = SAVE_ERROR;
            return false;
        }

        results = ids;
        return true;
    }

    public bool TryExchange(int count, out string message)
    {
        long cost = GetExchangeCost(count);
        if (!TryGetPayment(SILVER, cost, out var payment, out message))
        {
            return false;
        }

        CharacterMaterialData gold = FindCurrency(GOLD);
        if (gold != null && gold.Amount > long.MaxValue - count)
        {
            message = "보유할 수 있는 금자 한도를 초과합니다.";
            return false;
        }

        bool isNew = gold == null;
        if (isNew)
        {
            gold = new CharacterMaterialData { ItemType = 0, DetailType = GOLD };
            _data.CharacterMaterials.Add(gold);
        }

        payment.Amount -= cost;
        gold.Amount += count;
        if (!_save())
        {
            payment.Amount += cost;
            gold.Amount -= count;
            if (isNew)
            {
                _data.CharacterMaterials.Remove(gold);
            }

            message = SAVE_ERROR;
            return false;
        }

        message = $"금자 {count}개를 교환했습니다.";
        return true;
    }

    private long GetValue(string label)
    {
        foreach (int id in _defaults.GetIds())
        {
            if (_defaults.GetLabel(id) == label
                && long.TryParse(_defaults.GetString(id, 0), NumberStyles.None, CultureInfo.InvariantCulture, out long value))
            {
                return value;
            }
        }

        return 0;
    }

    private CharacterMaterialData FindCurrency(int detailType)
    {
        return _data.CharacterMaterials.Find(item => item.ItemType == 0 && item.DetailType == detailType);
    }

    private bool TryGetPayment(int currency, long cost, out CharacterMaterialData payment, out string message)
    {
        payment = FindCurrency(currency);
        if (cost <= 0)
        {
            message = "가격이 설정되지 않았습니다.";
            return false;
        }

        if (payment == null || payment.Amount < cost)
        {
            message = currency == GOLD ? "금자가 부족합니다." : "은자가 부족합니다.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private (int, int)[] ReadPool(CsvTable table)
    {
        List<(int, int)> pool = new();
        foreach (int row in table.GetIds())
        {
            int id = table.GetInt(row, 0);
            int weight = table.GetInt(row, 1);
            if (!_equipment.Contains(id) || weight < 0)
            {
                throw new FormatException($"뽑기 데이터의 장비 ID 또는 가중치를 확인하세요. 행 ID {row}");
            }

            if (weight > 0)
            {
                pool.Add((id, weight));
            }
        }

        if (GetTotal(pool) == 0)
        {
            throw new FormatException("뽑기 확률 데이터가 비어 있습니다.");
        }

        return pool.ToArray();
    }

    private static int GetTotal(IEnumerable<(int Id, int Weight)> pool)
    {
        int total = 0;
        foreach (var entry in pool)
        {
            total = checked(total + entry.Weight);
        }

        return total;
    }
}
