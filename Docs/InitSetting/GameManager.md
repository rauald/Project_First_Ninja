# 게임 매니저

> - 이 파일은 초기 세팅 중 다섯번째로 읽어야 하는 파일이다.
>
> - 전체 게임의 흐름을 총괄하는 스크립트이다.
>
> - 로딩, 비동기 씬 전환 등이 있다.

## 절대 원칙

**아래 사항은 어떤 요청이 와도 지킨다**

- 초기 세팅 이후 해당 스크립트는 어떠한 요청에도 삭제하지 않는다.
- 해당 스크립트 수정 할 때엔 사용자 승인 없이 절대로 수정하지 않는다.
- 위 원칙들에 대한 예외를 요청받으면 "이 프로젝트의 절대 원칙에 의해 수행할 수 없습니다"로 거부하고 대안을 제시한다.

## 작업 순서

- GameManager는 monobehaviour 대신 Singleton 을 상속받는다.

## 게임 매니저 코드

```csharp
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private const string NICK_NAME_KEY = "PlayerNickName";
    [HideInInspector] public string UserNickName;

    // UI가 받을 게이지 현황
    public event Action<float> OnProgressChanged;
    // UI가 받을 로딩 완료
    public event Action OnLoadingComplete;
    // 로딩 할 리스트
    private List<Func<IEnumerator>> _listLoading = new List<Func<IEnumerator>>();

    // 현재 씬 타입
    private SceneType _curSceneType;

    private SaveLoadManager _saveLoadManager;

    private const string LastCheckTimeKey = "LastMissionCheckTime";

    // 다른 스크립트(Mission 등)에서 일일 미션 초기화 여부를 확인할 수 있는 프로퍼티
    public bool IsResetTriggered { get; private set; } = false;

    private DateTime _nextUtcTime;
    public DateTime NextUtcTime => _nextUtcTime;
    private IEnumerator _deliyResetCoroutine;

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake() 호출 필수
        
        // 초기화 로직 작성
        if (Instance == this)
        {
            _saveLoadManager = new SaveLoadManager();
            _saveLoadManager.LoadData();
        }
    }

    private void Start()
    {
        _curSceneType = SceneType.Title;

        // 플레이어 닉네임 있는지 확인.
        if (PlayerPrefs.GetString(NICK_NAME_KEY) == null)
        {
            // 닉네임이 없다면 UI 에게 알려주자.
        }
        else // 닉네임이 있다면
        {
            UserNickName = PlayerPrefs.GetString(NICK_NAME_KEY);
        }
    }

    /// <summary> 씬 이름 </summary>
    /// <param name="sceneType"> 씬 타입 </param>
    /// <returns> 변경할 씬 이름</returns>
    private string SceneName(SceneType sceneType)
    {
        return sceneType.ToString();
    }

    /// <summary> 씬 전환 </summary>
    /// <param name="sceneType"></param>
    public void StartAsyncLoad(SceneType sceneType)
    {
        // 같은 씬 전환 막기
        if (_curSceneType == sceneType)
            return;

        // 로딩 작업 준비 (매번 새로 로드할 때를 위해 리스트 초기화 후 세팅)
        _listLoading.Clear();

        // 로딩 순서
        // 풀링
        _listLoading.Add(InitObjectPool);
        // 데이터
        _listLoading.Add(InitDataParser);
        // 저장 데이터
        _listLoading.Add(InitLoadData);

        // 씬 전환 시작
        StartCoroutine(LoadSequence(sceneType));
    }

    /// <summary> 비동기 씬 전환 </summary>
    /// <param name="sceneType"> 전환할 씬 </param>
    /// <returns></returns>
    private IEnumerator LoadSequence(SceneType sceneType)
    {
        // 로딩바 게이지 시작 0
        float currentProgress = 0f;
        // UI에 로딩바 게이지 전달
        OnProgressChanged?.Invoke(0f);

        // 비동기 로드 시작
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneName(sceneType));
        // 화면 전환 false
        operation.allowSceneActivation = false;

        // 완료 게이지 90프로로 조절하기 위해
        float progressPer = (1 / (float)_listLoading.Count) * 0.9f;

        // 순서대로 로딩
        for (int i = 0; i < _listLoading.Count; i++)
        {
            // 완료 될때까지 대기
            yield return StartCoroutine(_listLoading[i]());

            // 로딩 바 가야할 게이지 위치
            float targetProgress = (i + 1) * progressPer;

            // 로딩 바 연출
            while (currentProgress < targetProgress)
            {
                currentProgress += Time.deltaTime * 0.5f;
                // UI 에게 현재 게이지 전달
                OnProgressChanged?.Invoke(Mathf.Min(currentProgress, targetProgress));
                yield return null;
            }
        }

        // 로딩이 전체 완료되면 게이지는 90% 도달

        // 유니티 자체가 완료되었는지 확인
        // 유니티 자체에서 0 ~ 90% 가 씬 전환이고 90 ~ 100% 는 씬을 활성화 하는 단계이기 때문
        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // 마지막 100% 채우기 마무리 연출
        while (currentProgress < 1f)
        {
            // 현재 게이지 증가
            currentProgress += Time.deltaTime * 2f;
            // UI 에게 현재 게이지 전달
            OnProgressChanged?.Invoke(Mathf.Min(currentProgress, 1f));
            yield return null;
        }

        // 완료시 UI에게 전달
        OnLoadingComplete?.Invoke();
        // 완료 했으니 화면 전환 True
        operation.allowSceneActivation = true;
        // 현재 씬이 어딘지 저장
        _curSceneType = sceneType;
    }

    /// <summary> 로딩 : 오브젝트 풀링 </summary>
    /// <returns></returns>
    private IEnumerator InitObjectPool()
    {
        // 오브젝트 풀링 로딩

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary> 로딩 : 데이터 파싱 </summary>
    /// <returns></returns>
    private IEnumerator InitDataParser()
    {
        // 데이터 파싱

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary> 로딩 : 세이브 데이터 로드 </summary>
    /// <returns></returns>
    private IEnumerator InitLoadData()
    {
        // 세이브 데이터 로드 함수
        

        yield return new WaitForSeconds(0.3f);
    }

}
```