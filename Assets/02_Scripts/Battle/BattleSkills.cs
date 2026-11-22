using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BattleSkills
{
    private const int MAX_LEVEL = 5;
    private const int ACTIVE_LIMIT = 6;
    private const int PASSIVE_LIMIT = 5;
    private const int AWAKENING_START = 19;
    private const int SHARED_ACTIVE_START = 7;
    private static readonly string[] EffectNames = { "", "피해", "범위", "개수", "지속시간", "재사용 대기시간" };
    private static readonly string[] _activeDescriptions =
    {
        "전방을 반달 모양으로 벱니다.", "적에게 쿠나이를 던집니다.", "물 화살을 쏩니다.",
        "덩굴 채찍을 휘두릅니다.", "돌 주먹을 내지릅니다.", "바람탄을 날립니다.",
        "전방에 검기를 날립니다.", "넓게 벤 뒤 검격이 잠시 남습니다.", "불 구슬을 날립니다.",
        "주변에 회전하는 불표창을 소환합니다.", "지면에 피해를 주는 물 웅덩이를 만듭니다.",
        "지면에서 물기둥을 솟게 합니다.", "폭발하는 열매를 던집니다.", "지면에 독 안개를 퍼뜨립니다.",
        "적 위에 돌덩이를 떨어뜨립니다.", "지면에서 돌기둥을 솟게 합니다.", "회전하는 톱날을 소환합니다.",
        "돌풍을 일으켜 적을 밀어냅니다.", "응축한 기운을 주변에 폭발시킵니다.", "불타는 유성을 떨어뜨립니다.",
        "거대한 파도가 전방을 휩쓸고 지나갑니다.", "지면에서 여러 가시를 솟게 합니다.",
        "주먹으로 땅을 내려쳐 충격파를 일으킵니다.", "거대한 회오리로 주변을 휩씁니다."
    };

    private static readonly string[] _characterPassiveDescriptions =
    {
        "무속성 스킬의 범위가 증가합니다.", "무속성 스킬 적중으로 표식을 남깁니다. 다른 스킬로 적중하면 추가 피해를 줍니다.", "카게의 각성기를 개방합니다.",
        "불속성 스킬로 적에게 화상을 입힙니다.", "화상 상태의 적을 처치하면 폭발해 주변에 피해를 줍니다.", "쿠노이치의 각성기를 개방합니다.",
        "수속성 스킬에 맞은 적의 이동 속도가 감소합니다.", "수속성 스킬을 다섯 번 사용할 때마다 수룡을 날립니다.", "류이의 각성기를 개방합니다.",
        "적을 처치하면 체력을 회복합니다.", "최대 체력을 초과한 회복분을 보호막으로 전환합니다.", "코노하의 각성기를 개방합니다.",
        "적을 열 명 처치할 때마다 공격력이 증가하며 최대 20중첩됩니다.", "현재 체력 비율에 따라 치명타 피해가 증가합니다.", "간타의 각성기를 개방합니다.",
        "경험치와 아이템 획득 범위가 증가합니다.", "모든 스킬의 재사용 대기시간이 감소합니다.", "나기의 각성기를 개방합니다."
    };
    private static readonly string[] _passiveEffects =
    {
        "공격력", "방어력", "최대 체력", "치명타 확률", "치명타 피해", "이동 속도",
        "재사용 대기시간", "스킬 지속시간", "투사체 개수", "재화 획득량", "경험치 획득량"
    };

    public static string GetActiveDescription(int id) => _activeDescriptions[id - 1];
    public static string GetCharacterPassiveDescription(int id) => _characterPassiveDescriptions[id - 1];

    private readonly Dictionary<int, int> _active = new();
    private readonly Dictionary<int, int> _passive = new();
    private readonly List<int> _activeOrder = new();
    private readonly List<int> _passiveOrder = new();
    private readonly CsvTable _activeTable;
    private readonly CsvTable _passiveTable;
    private readonly CsvTable _activeUpgrades;
    private readonly CsvTable _passiveUpgrades;

    public readonly struct Choice
    {
        public readonly int Id;
        public readonly bool IsActive;

        public Choice(int id, bool isActive)
        {
            Id = id;
            IsActive = isActive;
        }
    }

    public IReadOnlyDictionary<int, int> Active => _active;
    public IReadOnlyDictionary<int, int> Passive => _passive;
    public IReadOnlyList<int> ActiveOrder => _activeOrder;
    public IReadOnlyList<int> PassiveOrder => _passiveOrder;

    public BattleSkills(DataManager data, int basicId)
    {
        _activeTable = data.GetTable("BattleActiveSkillData");
        _passiveTable = data.GetTable("BattlePassiveSkillData");
        _activeUpgrades = data.GetTable("BattleActiveSkillLevelUpData");
        _passiveUpgrades = data.GetTable("BattlePassiveSkillLevelUpData");
        _active.Add(basicId, 1);
        _activeOrder.Add(basicId);
    }

    public List<Choice> DrawChoices(IReadOnlyList<Choice> preferred = null)
    {
        List<Choice> choices = new();
        foreach (int id in _activeTable.GetIds())
        {
            if (id >= SHARED_ACTIVE_START && id < AWAKENING_START && CanLearn(_active, id, ACTIVE_LIMIT))
            {
                choices.Add(new Choice(id, true));
            }
        }
        foreach (int id in _passiveTable.GetIds())
        {
            if (CanLearn(_passive, id, PASSIVE_LIMIT))
            {
                choices.Add(new Choice(id, false));
            }
        }

        List<Choice> selected = new(3);
        if (preferred != null)
        {
            foreach (Choice choice in preferred)
            {
                if (selected.Count == 3)
                {
                    break;
                }
                if (choices.Remove(choice))
                {
                    selected.Add(choice);
                }
            }
        }
        while (selected.Count < 3 && choices.Count > 0)
        {
            int index = Random.Range(0, choices.Count);
            selected.Add(choices[index]);
            choices.RemoveAt(index);
        }
        return selected;
    }

    private static bool CanLearn(Dictionary<int, int> learned, int id, int limit)
    {
        return learned.TryGetValue(id, out int level) ? level < MAX_LEVEL : learned.Count < limit;
    }

    public void Learn(Choice choice)
    {
        Dictionary<int, int> learned = choice.IsActive ? _active : _passive;
        learned.TryGetValue(choice.Id, out int level);
        if (level == 0)
        {
            (choice.IsActive ? _activeOrder : _passiveOrder).Add(choice.Id);
        }
        learned[choice.Id] = level + 1;
    }

    public static int GetElement(Choice choice)
    {
        if (!choice.IsActive)
        {
            return 0;
        }

        // 기본기 6개는 무·화·수·목·토·풍 순서이며, 추가 액티브는 속성당 두 개씩 등록된다.
        return GetActiveElement(choice.Id);
    }

    public static int GetActiveElement(int id) => id >= 19 ? id - 19 : id <= 6 ? id - 1 : (id - 7) / 2;

    public string GetName(int id, bool isActive)
    {
        return (isActive ? _activeTable : _passiveTable).GetString(id, 1);
    }

    public string Describe(Choice choice)
    {
        int id = choice.Id;
        int current = (choice.IsActive ? _active : _passive).GetValueOrDefault(id);
        if (!choice.IsActive)
        {
            string effect = _passiveEffects[id - 1];
            if (current == 0)
            {
                return id == 7 ? $"{effect}이 감소합니다." : $"{effect}이 증가합니다.";
            }
            int value = GetPassiveUpgrade(id, current + 1);
            return value == 0 ? string.Empty : $"{effect} {(id == 7 ? "−" : "+")}{FormatEffect(value, id != 9)}";
        }
        if (current == 0)
        {
            return GetActiveDescription(id);
        }

        return string.Join("\n", ReadUpgrades(id, current + 1)
            .Select(effect => $"{EffectNames[effect.Type]} {(effect.Type == 5 ? "−" : "+")}{FormatActiveEffect(effect)}"));
    }

    private static string FormatActiveEffect((int Type, int Value) effect)
    {
        return effect.Type switch
        {
            2 => $"{effect.Value * .0025f:0.##}",
            3 => effect.Value.ToString(),
            4 => $"{effect.Value / 10f:0.#}초",
            _ => FormatEffect(effect.Value, true)
        };
    }

    private static string FormatEffect(int value, bool isPercentage)
    {
        return isPercentage ? $"{value / 10f:0.#}%" : value.ToString();
    }

    private int GetPassiveUpgrade(int id, int level)
    {
        foreach (int row in _passiveUpgrades.GetIds())
        {
            if (_passiveUpgrades.GetInt(row, 0) == id && _passiveUpgrades.GetInt(row, 1) == level)
            {
                return _passiveUpgrades.GetInt(row, 2);
            }
        }
        return 0;
    }

    public int GetPassiveBonus(int id)
    {
        if (!_passive.TryGetValue(id, out int level))
        {
            return 0;
        }
        int value = _passiveTable.GetInt(id, 3);
        for (int i = 2; i <= level; i++)
        {
            value += GetPassiveUpgrade(id, i);
        }
        return value;
    }

    public (float Damage, float Radius, int Count, float Duration, float Cooldown) GetAttack(int id)
    {
        int damage = _activeTable.GetInt(id, 3);
        int range = _activeTable.GetInt(id, 4);
        int count = _activeTable.GetInt(id, 5);
        int duration = _activeTable.GetInt(id, 6);
        int cooldown = _activeTable.GetInt(id, 7);
        for (int level = 2; level <= _active.GetValueOrDefault(id); level++)
        {
            foreach ((int type, int value) in ReadUpgrades(id, level))
            {
                switch (type)
                {
                    case 1: damage += value; break;
                    case 2: range += value; break;
                    case 3: count += value; break;
                    case 4: duration += value; break;
                    case 5: cooldown = Mathf.RoundToInt(cooldown * (1f - value / 1000f)); break;
                }
            }
        }
        return (damage / 1000f, range * .0025f, count + GetPassiveBonus(9),
            duration / 10f * (1f + GetPassiveBonus(8) / 1000f),
            Mathf.Max(.25f, cooldown / 10f * (1f - GetPassiveBonus(7) / 1000f)));
    }

    private IEnumerable<(int Type, int Value)> ReadUpgrades(int id, int level)
    {
        foreach (int row in _activeUpgrades.GetIds())
        {
            if (_activeUpgrades.GetInt(row, 0) != id || _activeUpgrades.GetInt(row, 1) != level)
            {
                continue;
            }
            for (int column = 2; column <= 4; column += 2)
            {
                int type = _activeUpgrades.GetInt(row, column);
                if (type > 0 && type < EffectNames.Length)
                {
                    yield return (type, _activeUpgrades.GetInt(row, column + 1));
                }
            }
            yield break;
        }
    }
}
