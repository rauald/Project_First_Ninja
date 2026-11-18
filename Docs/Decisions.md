# 기능별 구현 기록

- 새 기능을 구현하거나 기존 기능의 구조를 변경할 때 기록을 작성하거나 갱신한다.
- 게임 동작의 요구사항은 `Docs/GameDesign.md`의 해당 항목을 참조한다.
- 검증 결과에는 실제로 확인한 내용과 확인하지 못한 내용을 구분해 적는다.
- 작성 후, `C:\Users\user\OneDrive\프로젝트\11월 개인 프로젝트\기능명세서` 경로에 저장한다.
- 작업 날에 맞는 폴더에 저장한다. 만약 해당 폴더가 없다면 폴더를 만든다. (폴더명 예 - 20261111)
- 파일명은 `기능명.md`로 만든 후 저장한다.

## FUNC-003 — [기능명]

- 날짜:
- 관련 기획:
- 관련 파일: 파일명 및 줄 수 기입
- 사용한 기능·기술:
- 선택 이유:
- 구현 구조: 도식화 및 함수명, 변수명 기입
- 구현 결과:
- 검증 결과와 미검증 항목:

## FUNC-001 — 초기 설정

- 날짜: 2026-11-18
- 관련 기획: UI 기조의 타이틀 로딩과 로비 진입, 스킬 기조의 DB 5개, Docs/InitSetting 초기 세팅 문서
- 사용한 기능·기술: Unity 6000.3.9f1, Input System, TextAsset, JsonUtility, 비동기 씬 전환
- 선택 이유: 기존 타이틀·로비·전투 씬과 CSV 에셋을 재사용한다. 씬 등록 순서와 SceneType 값을 일치시킨다.
- 구현 구조: 타이틀 클릭·터치 → GameManager.StartAsyncLoad → DataManager.LoadTables → SaveLoadManager.LoadData → SoundManager.Initialize → 로비 비동기 전환. UIManager는 WindowManager와 PopUpManager를 관리한다.
- 구현 결과: Windows 기본 창 1080×1920, 세로 Android 설정, 공용 스크립트 16개, CSV 18개를 타이틀 씬에 연결했다. 기존 Move에 방향키와 게임패드 스틱이 등록되어 있다. 특성 CSV는 기획 확정 이후 등록한다.
- 데이터 적용: 사용자 수정 스킬 CSV 5개를 스킬 기조의 대응 DB 시트와 셀 단위로 대조했다. 유효 행은 캐릭터 패시브 18, 전투 액티브 24, 액티브 레벨업 72, 전투 패시브 11, 패시브 레벨업 44개다. 전투 스킬 레벨업은 기본 스킬 유형을 참조한다.
- 검증 결과: Roslyn C# 컴파일 성공. CSV 18개 참조, 스테이지의 몬스터 참조 500개, 스킬 레벨업 참조 116개 검사. Unity Play Mode에서 타이틀 시작 안내와 로비 전환을 확인했다. 로비 콘솔 카운터는 오류 0이었다. 앞선 임포트 중 Unity Burst 네이티브 DLL 로딩 오류가 한 차례 표시됐다.
- 미검증: Windows·Android 빌드, Android 터치 조이스틱, 저장 파일 손상·재실행, 풀 재대여, 로비·전투 왕복 및 UI 화면 전환.
- 관련 파일:
  - Assets/01_Scenes/00_Tilte_Scene.unity: 5개 매니저 오브젝트와 CSV 에셋 18개 참조
  - ProjectSettings/ProjectSettings.asset: Windows 창 크기와 창 모드
  - Assets/02_Scripts/CsvTable.cs: 69줄
  - Assets/02_Scripts/DataManager.cs: 74줄
  - Assets/02_Scripts/EnumList.cs: 43줄
  - Assets/02_Scripts/GameManager.cs: 98줄
  - Assets/02_Scripts/IPoolable.cs: 5줄
  - Assets/02_Scripts/IWindowable.cs: 4줄
  - Assets/02_Scripts/ObjectPool.cs: 49줄
  - Assets/02_Scripts/PoolManager.cs: 18줄
  - Assets/02_Scripts/PoolObject.cs: 18줄
  - Assets/02_Scripts/PopUpManager.cs: 26줄
  - Assets/02_Scripts/SaveLoadData.cs: 8줄
  - Assets/02_Scripts/SaveLoadManager.cs: 65줄
  - Assets/02_Scripts/Singleton.cs: 26줄
  - Assets/02_Scripts/SoundManager.cs: 48줄
  - Assets/02_Scripts/UIManager.cs: 24줄
  - Assets/02_Scripts/WindowManager.cs: 23줄
  - Assets/01_Scenes/00_Tilte_Scene.unity: 464줄
  - ProjectSettings/ProjectSettings.asset: 889줄

## FUNC-002 — 기기 해상도에 맞춘 공용 UI

- 날짜: 2026-11-18
- 관련 기획: 세로형 타이틀·로비·전투 UI
- 사용한 기술: uGUI Canvas, Canvas Scaler, 기기 화면의 안전 영역, 비동기 씬 전환
- 구현 구조: UIManager → Canvas(1080×1920, Expand) → SafeArea → WindowManager와 PopUpManager. SafeAreaLayout.UpdateLayout은 기기 해상도 또는 안전 영역이 바뀔 때 앵커를 다시 계산한다.
- 구현 결과: Android는 기기 해상도로 렌더링한다. GameManager가 공용 Canvas에 시작 안내·로딩·오류 상태를 표시하며 로비 진입 시 타이틀 안내를 숨긴다.
- 검증: Unity Device Simulator에서 iPhone SE 2세대(16:9), iPhone 12(약 19.5:9), Pixel 5(19.5:9) 화면의 안내 배치와 Pixel 5 프로필의 타이틀 → 로비 전환을 확인했다. Android 실기기 및 독립 빌드는 미검증이다.
- 관련 파일:
  - Assets/02_Scripts/UIManager.cs: 68줄
  - Assets/02_Scripts/SafeAreaLayout.cs: 41줄
  - Assets/02_Scripts/GameManager.cs: 125줄
  - Assets/01_Scenes/00_Tilte_Scene.unity: 510줄
