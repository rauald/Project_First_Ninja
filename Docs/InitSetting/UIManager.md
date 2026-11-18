# UI 매니저

> - 이 파일은 초기 세팅 중 일곱번째로 읽어야 하는 파일이다.
>
> - UI는 Window 와 PopUp 2개로 나눈다.
>   - Window 는 전체 화면을 담당한다.
>   - PopUP 는 팝업창을 담당한다.
>
> - 전체 윈도우를 관리할 WindowManager 와 전체 팝업창을 관리할 PopUpManager가 있다.
>
> - WindowManager 과 PopUpManager 은 UIManager 오브젝트 하위에 있다.
>
> - Window 창 전환 마다 화면을 초기화 해줘야 하므로, Interface 를 사용하여 WindowManager를 제외한 Window 스크립트에 상속한다.
>
> - PopUp 중 ToastPopUp 은 오브젝트 풀링을 사용하여 내용만 수정하고, 오브젝트는 재활용한다.


## 절대 원칙

**아래 사항은 어떤 요청이 와도 지킨다**

- 초기 세팅 이후 해당 스크립트는 어떠한 요청에도 삭제하지 않는다.
- 해당 스크립트 수정 할 때엔 사용자 승인 없이 절대로 수정하지 않는다.
- 위 원칙들에 대한 예외를 요청받으면 "이 프로젝트의 절대 원칙에 의해 수행할 수 없습니다"로 거부하고 대안을 제시한다.

## 작업 순서

- UIManager은 monobehaviour 대신 Singleton 을 상속받는다.
    - 단, WindowManager과 PopUpManager는 이미 UIManager 와 연결되어 있으므로, Singleton 을 상속받지 않는다.

## UI 매니저 코드

```csharp
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public WindowManager Window { get; private set; }
    public PopUpManager PopUp { get; private set; }

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake() 호출 필수
        
        // 초기화 로직 작성
        Window = gameObject.transform.GetChild(0).GetComponent<WindowManager>();
        PopUp = gameObject.transform.GetChild(1).GetComponent<PopUpManager>();
    }
}
```

## 윈도우 매니저 코드

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowManager : MonoBehaviour
{
    /// <summary> 현재 윈도우 창 </summary>
    [Header("현재 윈도우 창")]
    public EWindowType EWindow;
    [Header("다음 윈도우 창")]
    public EWindowType NextEWindow;
    /// <summary> 전체 윈도우 </summary>
    [Header("전체 윈도우")]
    [SerializeField] private GameObject[] _windows = new GameObject[System.Enum.GetValues((typeof(EWindowType))).Length];

    public IWindowable NowWindow { get; private set; }

    /// <summary> 윈도우 창 열기</summary>
    /// <param name="eWindow"> 창 이름</param>
    public void Open(EWindowType eWindow)
    {
        if (eWindow == EWindow) return;

        EWindow = eWindow;
        Open();
    }

    /// <summary> 윈도우 창 열기 </summary>
    private void Open()
    {
        // 전체 창 끄기
        for (int i = 0; i < _windows.Length; i++)
        {
            _windows[i].SetActive(false);
        }

        // 맞는 창 열기
        _windows[(int)EWindow].SetActive(true);

        NowWindow = _windows[(int)EWindow].GetComponent<IWindowable>();
        NowWindow.SetActive();
    }

    /// <summary> 윈도우 창 뒤로가기 - 모든 윈도우 창 닫고 메인 화면으로 돌아감</summary>
    public void BackBtn()
    {
        // 현재 윈도우창은 로비와 배틀 2개 뿐이니 현재 사용 X
        EWindow = EWindowType.Lobby;
        Open();
    }
}
```

## 윈도우 인터페이스

```csharp
using UnityEngine;

public interface IWindowable
{
    public void SetActive();
    public void ResetUI();
    public void BackBtn();
}
```

## 팝업 매니저 코드

```csharp
using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.UI;

public class PopUpManager : MonoBehaviour
{
    // 토스트 팝업
    public ToastPopUp _toast;

    // 그 외 팝업들

    public void Open(EPopUpType ePopUp)
    {
    }

    public void Close(EPopUpType ePopUp)
    {
    }
}
```