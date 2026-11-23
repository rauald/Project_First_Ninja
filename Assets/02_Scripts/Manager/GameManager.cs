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
    private bool _hasSettledStage;
    private SceneLoadingOverlay _loadingOverlay;
    private const float MIN_LOADING_SCREEN_SECONDS = 1f;

    public CharacterProgression Characters { get; private set; }
    public EquipmentInventory Equipment { get; private set; }
    public ShopService Shop { get; private set; }
    public TalentService Talents { get; private set; }
    public StageProgression Stages { get; private set; }
    public int ActiveStageId { get; private set; }
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
                ActiveStageId = 0;
                UIManager.Instance.Window.Open(WindowType.Lobby);
                break;
            case SceneType.Game:
                UIManager.Instance.PopUp.Close();
                UIManager.Instance.Window.Open(WindowType.Battle);
                break;
        }
    }

    public bool TryEnterStage(int stageId)
    {
        if (_isLoading || SceneManager.GetActiveScene().buildIndex != (int)SceneType.Lobby
            || Stages == null || !Stages.CanEnter(stageId))
        {
            return false;
        }

        ActiveStageId = stageId;
        _hasSettledStage = false;
        StartAsyncLoad(SceneType.Game);
        return true;
    }

    public bool TryCompleteStage(out long silver, out long gold)
    {
        return TrySettleBattle(true, 0, 0, out silver, out gold);
    }

    public bool TrySettleBattle(bool isVictory, long collectedSilver, long collectedGold, out long silver, out long gold)
    {
        silver = gold = 0;
        if (_hasSettledStage || ActiveStageId == 0
            || SceneManager.GetActiveScene().buildIndex != (int)SceneType.Game
            || !Stages.TryRecordResult(ActiveStageId, isVictory, collectedSilver, collectedGold, out silver, out gold))
        {
            return false;
        }

        _hasSettledStage = true;
        return true;
    }

    public bool TryRestartBattle(int stageId)
    {
        if (!_hasSettledStage || SceneManager.GetActiveScene().buildIndex != (int)SceneType.Game
            || (stageId != ActiveStageId && stageId != Stages.GetNextStage(ActiveStageId)) || !Stages.CanEnter(stageId))
        {
            return false;
        }

        ActiveStageId = stageId;
        _hasSettledStage = false;
        return true;
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

        float overlayStartedAt = 0f;
        if (!fromTitle)
            overlayStartedAt = ShowLoadingOverlay();

        // 다음 프레임에서 데이터를 읽기 전에 현재 로딩 UI를 먼저 그린다.
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
                Stages = new StageProgression(DataManager.Instance.GetTable("StageData"), _save.CurData, _save.SaveData);
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

                EndSceneLoading();
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

            EndSceneLoading();
            yield break;
        }

        operation.allowSceneActivation = false;

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
            overlayStartedAt = ShowLoadingOverlay();
        }

        // 디스크가 빨라도 별도 화면을 최소 시간만큼 표시한 뒤 씬을 활성화한다.
        while (Time.realtimeSinceStartup - overlayStartedAt < MIN_LOADING_SCREEN_SECONDS)
            yield return null;

        operation.allowSceneActivation = true;
        yield return operation;
        yield return null;
        EndSceneLoading();
    }

    // 타이틀 진행 표시와 독립적으로 사용하는 씬 공통 로딩 화면이다.
    private float ShowLoadingOverlay()
    {
        if (_loadingOverlay == null)
            _loadingOverlay = SceneLoadingOverlay.Create(transform);
        _loadingOverlay.Show();
        return Time.realtimeSinceStartup;
    }

    private void EndSceneLoading()
    {
        if (_loadingOverlay != null)
            _loadingOverlay.gameObject.SetActive(false);
        _isLoading = false;
    }
}
