using System;
using System.IO;
using UnityEngine;

public sealed class SaveLoadManager
{
    private string FilePath => Path.Combine(Application.persistentDataPath, "gameData.json");
    private bool _canSave = true;

    public SaveLoadData CurData { get; private set; } = new();

    public void LoadData()
    {
        if (!File.Exists(FilePath))
        {
            CurData = new SaveLoadData();
            _canSave = true;
            return;
        }

        try
        {
            SaveLoadData data = new();
            JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), data);
            CurData = data;
            _canSave = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            CurData = new SaveLoadData();
            _canSave = false;
            Debug.LogWarning($"저장 파일을 읽지 못했습니다. {exception.Message}");
        }
    }

    public bool SaveData()
    {
        if (!_canSave)
        {
            return false;
        }

        string tempPath = FilePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(tempPath, JsonUtility.ToJson(CurData, true));
            if (File.Exists(FilePath))
            {
                File.Replace(tempPath, FilePath, null);
            }
            else
            {
                File.Move(tempPath, FilePath);
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Debug.LogWarning($"저장 파일을 쓰지 못했습니다. {exception.Message}");
            return false;
        }
    }
}
