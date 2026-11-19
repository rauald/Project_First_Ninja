using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class GameManager : Singleton<GameManager>
{
    [SerializeField] private DataManager _data;
    [SerializeField] private SoundManager _sound;
    [SerializeField] private UIManager _ui;

    private readonly SaveLoadManager _save = new();
    private bool _isReady;
    private bool _isLoading;

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        UpdateTitleStatus(SceneManager.GetActiveScene());
    }

    protected override void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        base.OnDestroy();
    }

    private void OnActiveSceneChanged(Scene previous, Scene current)
    {
        UpdateTitleStatus(current);
    }

    private void UpdateTitleStatus(Scene scene)
    {
        _ui.SetTitleStatus(null);
    }

    private void Update()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex == (int)SceneType.Title && !_isLoading
            && (Mouse.current?.leftButton.wasPressedThisFrame == true
                || Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true))
        {
            if (!_ui.Title.IsIntroComplete)
            {
                _ui.Title.SkipIntro();
                return;
            }

            StartAsyncLoad(SceneType.Lobby);
        }
    }

    public void StartAsyncLoad(SceneType sceneType)
    {
        int index = (int)sceneType;
        if (_isLoading || index < 0 || index >= (int)SceneType.Max
            || (SceneManager.GetActiveScene().buildIndex == (int)SceneType.Title && !_ui.Title.CanStart)
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
            _ui.SetTitleStatus(null);
            _ui.Title.SetProgress(0f);
        }

        yield return null;

        if (!_isReady)
        {
            try
            {
                _data.LoadTables();
                _save.LoadData();
                _sound.Initialize(_save);
                _isReady = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                if (fromTitle)
                {
                    _ui.SetTitleStatus("Loading failed. Click or tap to retry.");
                    _ui.Title.ShowRetry();
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
                _ui.SetTitleStatus("Loading failed. Click or tap to retry.");
                _ui.Title.ShowRetry();
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
                _ui.Title.SetProgress(Mathf.Clamp01(operation.progress / 0.9f));
            }
            yield return null;
        }

        if (fromTitle)
        {
            _ui.Title.SetProgress(1f);
            yield return null;
            yield return _ui.Title.FadeTo(1f);
            yield return new WaitForSecondsRealtime(1f);
        }

        operation.allowSceneActivation = true;
        yield return operation;
        if (fromTitle)
        {
            _ui.Title.HideTitle();
            yield return _ui.Title.FadeTo(0f);
        }

        _isLoading = false;
    }
}
