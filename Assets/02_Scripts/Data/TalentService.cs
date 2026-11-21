using System;
using System.Collections.Generic;
using System.Linq;

public sealed class TalentService
{
    public sealed class Talent
    {
        public int Id { get; }
        public string Name { get; }
        public string Effect { get; }
        public int Value { get; }
        public IReadOnlyList<int> Prerequisites { get; }
        public int Currency { get; }
        public int Cost { get; }

        internal Talent(CsvTable table, int id)
        {
            Id = id;
            Name = table.GetString(id, 1);
            Effect = table.GetLabel(id).TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            Value = table.GetInt(id, 3);
            Prerequisites = Enumerable.Range(4, 3).Select(field => table.GetInt(id, field))
                .Where(prerequisite => prerequisite != 0).Distinct().ToArray();
            Currency = table.GetInt(id, 8);
            Cost = table.GetInt(id, 9);
            if (table.GetInt(id, 0) != id || table.GetInt(id, 7) != 0 || Currency is < 0 or > 1 || Cost < 0)
            {
                throw new FormatException($"특성 {id}의 번호 또는 비용을 확인하세요.");
            }
        }
    }

    private readonly Dictionary<int, Talent> _talents;
    private readonly SaveLoadData _data;
    private readonly Func<bool> _save;

    public TalentService(CsvTable table, SaveLoadData data, Func<bool> save)
    {
        _talents = table.GetIds().ToDictionary(id => id, id => new Talent(table, id));
        foreach (Talent talent in _talents.Values)
        {
            foreach (int prerequisite in talent.Prerequisites)
            {
                if (!_talents.ContainsKey(prerequisite))
                {
                    throw new FormatException($"특성 {talent.Id}의 선행 특성 {prerequisite}이 없습니다.");
                }
            }
        }

        _data = data;
        _data.LearnedTalentIds ??= new List<int>();
        _save = save;
    }

    public Talent GetTalent(int id) => _talents[id];

    public bool IsLearned(int id) => _data.LearnedTalentIds.Contains(id);

    public long GetBalance(int currency) => FindCurrency(currency)?.Amount ?? 0;

    public string GetBlockReason(Talent talent)
    {
        if (IsLearned(talent.Id))
        {
            return "이미 습득한 특성입니다.";
        }

        if (talent.Prerequisites.Any(id => !IsLearned(id)))
        {
            return "선행 특성을 모두 습득해야 합니다.";
        }

        return GetBalance(talent.Currency) < talent.Cost ? "재화가 부족합니다." : string.Empty;
    }

    public bool TryLearn(int id, out string message)
    {
        Talent talent = GetTalent(id);
        message = GetBlockReason(talent);
        if (message.Length > 0)
        {
            return false;
        }

        CharacterMaterialData payment = FindCurrency(talent.Currency);
        if (payment != null)
        {
            payment.Amount -= talent.Cost;
        }

        _data.LearnedTalentIds.Add(id);
        if (_save())
        {
            return true;
        }

        _data.LearnedTalentIds.Remove(id);
        if (payment != null)
        {
            payment.Amount += talent.Cost;
        }

        message = "저장에 실패했습니다. 다시 시도해 주세요.";
        return false;
    }

    private CharacterMaterialData FindCurrency(int currency)
    {
        return _data.CharacterMaterials.Find(item => item.ItemType == 0 && item.DetailType == currency);
    }
}
