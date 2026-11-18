using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class GameManager : Singleton<GameManager>
{
    private const string TITLE_PROMPT = "First Ninja | Click or tap to start";

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
        _ui.SetTitleStatus(scene.buildIndex == (int)SceneType.Title ? TITLE_PROMPT : null);
    }

    private void Update()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex == (int)SceneType.Title
            && (Mouse.current?.leftButton.wasPressedThisFrame == true
                || Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true))
        {
            StartAsyncLoad(SceneType.Lobby);
        }
    }

    public void StartAsyncLoad(SceneType sceneType)
    {
        int index = (int)sceneType;
        if (_isLoading || index < 0 || index >= (int)SceneType.Max
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
            _ui.SetTitleStatus("Loading... 0%");
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
            }

            _isLoading = false;
            yield break;
        }

        while (!operation.isDone)
        {
            if (fromTitle && SceneManager.GetActiveScene().buildIndex == (int)SceneType.Title)
            {
                _ui.SetTitleStatus($"Loading... {Mathf.Clamp01(operation.progress / 0.9f):P0}");
            }
            yield return null;
        }

        _isLoading = false;
    }
}
