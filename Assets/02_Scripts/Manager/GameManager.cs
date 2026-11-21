using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : Singleton<GameManager>
{
    [SerializeField] private TitleScreen _title;

    private readonly SaveLoadManager _save = new();
    private bool _isReady;
    private bool _isLoading;

    public CharacterProgression Characters { get; private set; }
    public EquipmentInventory Equipment { get; private set; }
    public ShopService Shop { get; private set; }
    public TalentService Talents { get; private set; }
    public SaveLoadManager Save => _save;

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        ShowScenePresentation(SceneManager.GetActiveScene());
    }

    protected override void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        base.OnDestroy();
    }

    private void OnActiveSceneChanged(Scene previous, Scene current)
    {
        ShowScenePresentation(current);
    }

    private void ShowScenePresentation(Scene scene)
    {
        SceneType sceneType = (SceneType)scene.buildIndex;
        SoundManager.Instance.PlaySceneBgm(sceneType);

        switch (sceneType)
        {
            case SceneType.Title:
                UIManager.Instance.Window.Open(WindowType.Title);
                break;
            case SceneType.Lobby:
                UIManager.Instance.Window.Open(WindowType.Lobby);
                break;
            case SceneType.Game:
                UIManager.Instance.Window.Open(WindowType.Battle);
                break;
        }
    }

    public void StartAsyncLoad(SceneType sceneType)
    {
        int index = (int)sceneType;
        if (_isLoading || index < 0 || index >= (int)SceneType.Max
            || (SceneManager.GetActiveScene().buildIndex == (int)SceneType.Title && !_title.CanStart)
            || index == SceneManager.GetActiveScene().buildIndex)
        {
            return;
        }

        _isLoading = true;
        StartCoroutine(LoadScene(index));
    }

    private IEnumerator LoadScene(int index)
    {
        bool fromTitle = SceneManager.GetActiveScene().buildIndex == (int)SceneType.Title;
        if (fromTitle)
        {
            _title.SetProgress(0f);
        }

        yield return null;

        if (!_isReady)
        {
            try
            {
                DataManager.Instance.LoadTables();
                _save.LoadData();
                Characters = new CharacterProgression(_save.CurData, DataManager.Instance.GetTable, _save.SaveData);
                Equipment = new EquipmentInventory(_save.CurData, DataManager.Instance.GetTable("EquipData"), _save.SaveData);
                Shop = new ShopService(_save.CurData, DataManager.Instance.GetTable, _save.SaveData);
                Talents = new TalentService(DataManager.Instance.GetTable("AbilityData"), _save.CurData, _save.SaveData);
                Characters.Initialize();
                SoundManager.Instance.Initialize(_save);
                _isReady = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                if (fromTitle)
                {
                    _title.ShowRetry();
                }

                _isLoading = false;
                yield break;
            }
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(index);
        if (operation == null)
        {
            if (fromTitle)
            {
                _title.ShowRetry();
            }

            _isLoading = false;
            yield break;
        }

        if (fromTitle)
        {
            operation.allowSceneActivation = false;
        }

        while (operation.progress < 0.9f)
        {
            if (fromTitle && SceneManager.GetActiveScene().buildIndex == (int)SceneType.Title)
            {
                _title.SetProgress(Mathf.Clamp01(operation.progress / 0.9f));
            }
            yield return null;
        }

        if (fromTitle)
        {
            _title.SetProgress(1f);
            yield return null;
            yield return _title.FadeTo(1f);
            yield return new WaitForSecondsRealtime(1f);
        }

        operation.allowSceneActivation = true;
        yield return operation;
        if (fromTitle)
        {
            yield return _title.FadeTo(0f);
        }

        _isLoading = false;
    }
}
