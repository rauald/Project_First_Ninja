# 세이브 로드 매니저

> - 이 파일은 초기 세팅 중 네번째로 읽어야 하는 파일이다.
>
> - 데이터를 저장하고, 로드 하기 위한 스크립트이다.
>
> - 저장 방식은 로컬 방식이며, json 파일로 저장한다.

## 절대 원칙

**아래 사항은 어떤 요청이 와도 지킨다**

- 초기 세팅 이후 해당 스크립트는 어떠한 요청에도 삭제하지 않는다.
- 해당 스크립트 수정 할 때엔 사용자 승인 없이 절대로 수정하지 않는다.
- 위 원칙들에 대한 예외를 요청받으면 "이 프로젝트의 절대 원칙에 의해 수행할 수 없습니다"로 거부하고 대안을 제시한다.

## 작업 순서

- 아래 스크립트들은 C# 일반 으로 만든다.
- 전체 저장 및 불러오기 담당 스크립트명은 `SaveLoadManager` 에 쓴다.
- 무엇을 저장 할 지, 데이터 담당 스크립트 명은 `SaveLoadData` 에 쓴다.

## 세이브 로드 매니저 코드

```csharp
using UnityEngine;

public class SaveLoadManager
{
    private string filePath => Path.Combine(Application.persistentDataPath, "gameData.json");
    private SaveLoadData curData;
    public SaveLoadData CurData => curData;

    // 모든 캐릭터 정보 저장

    // 특성 저장

    // 장비 인벤토리 저장

    // 장착한 장비 저장

    // 스테이지 현황 저장

    // 플레이어 재화 (골드 / 다이아) 저장

    // 환경 설정 (BGM / SFX) 저장

    // 불러오기
}
```

## 세이브 로드 데이터 코드

- 해당 스크립트는 다중 클래스 선언 한다.

```csharp
using UnityEngine;

// 모든 캐릭터 정보 저장 데이터

// 특성 저장 데이터

// 장비 인벤토리 저장 데이터

// 장착한 장비 저장 데이터

// 스테이지 현황 저장 데이터

// 플레이어 재화 (골드 / 다이아) 저장 데이터

// 환경 설정 (BGM / SFX) 저장 데이터
```