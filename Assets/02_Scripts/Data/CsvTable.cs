using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public sealed class CsvTable
{
    private readonly Dictionary<int, string[]> _rows = new();

    public int Count => _rows.Count;

    public bool Contains(int id)
    {
        return _rows.ContainsKey(id);
    }

    public static CsvTable Parse(string csv)
    {
        using StringReader reader = new(csv.TrimStart('\uFEFF'));
        string header = reader.ReadLine();
        if (header == null || !header.StartsWith("인덱스,구분,", StringComparison.Ordinal))
        {
            throw new FormatException("CSV 열 머리글을 확인하세요.");
        }

        int columnCount = header.Split(',').Length;
        CsvTable table = new();
        int lineNumber = 1;
        while (reader.ReadLine() is string line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // 현재 원본은 따옴표, 필드 내부 쉼표 또는 줄바꿈을 사용하지 않는다.
            string[] columns = line.Split(',');
            if (columns.Length != columnCount || line.Contains('"')
                || !int.TryParse(columns[0], NumberStyles.None, CultureInfo.InvariantCulture, out int id))
            {
                throw new FormatException($"{lineNumber}행의 열 수 또는 ID를 확인하세요.");
            }

            if (id == 0)
            {
                continue;
            }

            // 첫 번째 열은 키이며, 두 번째 열은 기획용 구분이다.
            if (!table._rows.TryAdd(id, columns[2..]))
            {
                throw new FormatException($"{lineNumber}행에 중복 ID {id}가 있습니다.");
            }
        }

        return table;
    }

    public int GetInt(int id, int field)
    {
        return int.Parse(_rows[id][field], CultureInfo.InvariantCulture);
    }

    public string GetString(int id, int field)
    {
        return _rows[id][field];
    }

    public IEnumerable<int> GetIds()
    {
        return _rows.Keys;
    }
}
