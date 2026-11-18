using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DataManager : Singleton<DataManager>
{
    [SerializeField] private TextAsset[] _tables;

    private Dictionary<string, CsvTable> _data = new();

    public int TableCount => _data.Count;

    public CsvTable GetTable(string tableName)
    {
        return _data[tableName];
    }

    public void LoadTables()
    {
        Dictionary<string, CsvTable> data = new(StringComparer.Ordinal);
        foreach (TextAsset asset in _tables)
        {
            if (asset == null)
            {
                throw new InvalidOperationException("연결되지 않은 CSV가 있습니다.");
            }

            try
            {
                data.Add(asset.name, CsvTable.Parse(asset.text));
            }
            catch (FormatException exception)
            {
                throw new FormatException($"{asset.name}: {exception.Message}", exception);
            }
        }

        CsvTable stages = data["StageData"];
        CsvTable monsters = data["MonsterData"];
        foreach (int stageId in stages.GetIds())
        {
            for (int field = 2; field <= 6; field++)
            {
                int monsterId = stages.GetInt(stageId, field);
                if (!monsters.Contains(monsterId))
                {
                    throw new FormatException($"스테이지 {stageId}의 몬스터 ID {monsterId}가 없습니다.");
                }
            }
        }

        ValidateLevelUpSkills(data["BattleActiveSkillData"], data["BattleActiveSkillLevelUpData"]);
        ValidateLevelUpSkills(data["BattlePassiveSkillData"], data["BattlePassiveSkillLevelUpData"]);
        _data = data;
    }

    private static void ValidateLevelUpSkills(CsvTable skills, CsvTable levelUps)
    {
        HashSet<int> skillTypes = new();
        foreach (int id in skills.GetIds())
        {
            skillTypes.Add(skills.GetInt(id, 0));
        }

        foreach (int id in levelUps.GetIds())
        {
            int skillType = levelUps.GetInt(id, 0);
            if (!skillTypes.Contains(skillType))
            {
                throw new FormatException($"레벨업 행 {id}의 기본 스킬 유형 {skillType}이 없습니다.");
            }
        }
    }
}
