# 기능별 구현 기록

- 새 기능을 구현하거나 기존 기능의 구조를 변경할 때 기록을 작성하거나 갱신한다.
- 게임 동작의 요구사항은 `Docs/GameDesign.md`의 해당 항목을 참조한다.
- 검증 결과에는 실제로 확인한 내용과 확인하지 못한 내용을 구분해 적는다.
- 작성 후, `C:\Users\user\OneDrive\프로젝트\11월 개인 프로젝트\기능명세서` 경로에 저장한다.
- 작업 날에 맞는 폴더에 저장한다. 만약 해당 폴더가 없다면 폴더를 만든다. (폴더명 예 - 20261111)
- 파일명은 `기능명.md`로 만든 후 저장한다.

## FUNC-004 — [기능명]

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
  - Assets/02_Scripts/Data/CsvTable.cs: 69줄
  - Assets/02_Scripts/Manager/DataManager.cs: 74줄
  - Assets/02_Scripts/Core/EnumList.cs: 43줄
  - Assets/02_Scripts/Manager/GameManager.cs: 98줄
  - Assets/02_Scripts/Pooling/IPoolable.cs: 5줄
  - Assets/02_Scripts/UI/IWindowable.cs: 4줄
  - Assets/02_Scripts/Pooling/ObjectPool.cs: 49줄
  - Assets/02_Scripts/Manager/PoolManager.cs: 18줄
  - Assets/02_Scripts/Pooling/PoolObject.cs: 18줄
  - Assets/02_Scripts/Manager/PopUpManager.cs: 26줄
  - Assets/02_Scripts/Data/SaveLoadData.cs: 8줄
  - Assets/02_Scripts/Manager/SaveLoadManager.cs: 65줄
  - Assets/02_Scripts/Core/Singleton.cs: 26줄
  - Assets/02_Scripts/Manager/SoundManager.cs: 48줄
  - Assets/02_Scripts/Manager/UIManager.cs: 24줄
  - Assets/02_Scripts/Manager/WindowManager.cs: 23줄
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
  - Assets/02_Scripts/Manager/UIManager.cs: 68줄
  - Assets/02_Scripts/UI/SafeAreaLayout.cs: 41줄
  - Assets/02_Scripts/Manager/GameManager.cs: 125줄
  - Assets/01_Scenes/00_Tilte_Scene.unity: 510줄

## FUNC-003 — 타이틀 화면

- 날짜: 2026-11-19
- 관련 기획: UI 기조의 타이틀 화면. 배경, 인트로, 클릭·터치, 데이터 로딩, 진행률과 로비 페이드 전환.
- 관련 파일: Assets/02_Scripts/UI/Title/TitleScreen.cs, Assets/02_Scripts/Manager/GameManager.cs, Assets/02_Scripts/Manager/UIManager.cs, Assets/01_Scenes/00_Tilte_Scene.unity.
- 사용한 기능·기술: uGUI Image와 RawImage, CanvasScaler와 안전 영역, Animator Controller, AnimationClip, Animation Event, 비동기 씬 로딩 활성화 제어.
- 구현 구조: 씬의 Inspector 참조 → Animator의 TitleIntro 클립 → CompleteIntro 이벤트 → 입력 허용 → GameManager.LoadScene → 데이터·저장 로드 → SetProgress → FadeTo(1) → 1초 대기 → 로비 활성화 → HideTitle → FadeTo(0).
- 구현 결과: 타이틀 씬에 Canvas, SafeArea, WindowManager, PopUpManager, 배경, 애니메이션, 안내, 로딩바, 페이드와 BGM/SFX AudioSource를 미리 배치하고 Inspector 참조로 연결했다. UIManager, TitleScreen, SoundManager의 런타임 오브젝트·컴포넌트 생성 코드를 제거했다. BG_Title과 Name_Sheet_00~09의 59개 고해상도 서브 스프라이트를 재사용했다. 애니메이션 완료 전 입력과 로딩 중 중복 입력을 차단한다. 로딩 실패 시 오류 문구와 재시도 입력을 표시한다. 타이틀 배경은 화면을 채우며, 안내와 로딩바는 안전 영역 안에 표시한다.
- 임시 선택: TitleIntro.anim에 첫 프레임 고정 페이드인 0.5초 + 프레임 재생 3.5초(전체 4초)을 기록했다. TitleFadeOut.anim과 TitleFadeIn.anim에서 0.5초 동안 CanvasGroup 알파를 변경한다. 타이틀·페이드 Animator를 씬에 미리 연결하고 클립 종료 이벤트로 입력을 허용한다. 기존 Etc_Sheet_1에서 클릭·터치 문구 영역을 표시한다. 로딩바는 기본 UI Image의 어두운 배경과 녹색 게이지를 사용한다.
- 검증 결과: Roslyn 컴파일 통과. Unity Device Simulator Pixel 5에서 인트로 중간 프레임, 마지막 프레임과 시작 안내, 입력 후 검은 화면 전환 및 로비 진입을 확인했다. 콘솔 오류는 0개였다.
- 미검증: Android 실기기, 독립 Windows 빌드, 이번 타이틀 화면의 다른 화면비, 로딩 실패 주입과 재시도 실행. 진행률은 코드와 씬 연결을 확인했으며 실행 캡처에서 개별 퍼센트 단계까지 관찰하지 못했다.

- 애니메이션 에셋: Assets/07_Animations/Title/TitleIntro.anim, TitleIntro.controller, TitleClear.anim, TitleFadeOut.anim, TitleFadeIn.anim, TitleFade.controller.

- Animator 전환 검증: Unity Play Mode에서 TitleIntro 재생, CompleteIntro 이벤트에 의한 시작 안내, TitleFadeOut과 TitleFadeIn을 거쳐 로비 진입을 확인했다. 콘솔 오류는 0개였다.

- 고해상도 이미지 갱신: 4096×4096 시트 10개(Name_Sheet_00~09)의 스프라이트 59개를 시트 번호와 프레임 번호 순서로 TitleIntro.anim에 재연결했다. 초당 8프레임, 종료 이벤트 7.375초를 유지하고 씬의 이전 스프라이트 참조 2개를 갱신했다.
- 고해상도 갱신 실행 검증: Pixel 5 시뮬레이터에서 새 프레임 표시, 인트로 종료 안내, 페이드와 로비 진입을 확인했다. 콘솔 오류는 0개였다. 씬에 추가된 정적 Image와 애니메이션 이미지가 겹쳐 보이는 상태를 확인했다.

- 인트로 연출 수정: TitleIntro.anim은 0~0.5초 첫 프레임 고정과 Image 알파 0→1, 이후 3.5초 프레임 재생으로 총 4초다. 4초 종료 이벤트를 유지한다. 인트로 중 입력은 SkipIntro에서 마지막 프레임과 알파 1을 즉시 적용하고 반환하며, 다음 입력에서 로딩한다. 안내는 TitlePromptBlink.anim의 1초 반복 주기로 밝아졌다 어두워진다. 두 안내 오브젝트의 Animator를 씬에 연결했다.
- 검증: 자연 종료와 안내 깜빡임을 Play Mode에서 관찰했다. 별도의 임시 실행 검증으로 클립 길이 4초, 재생 0.2초 뒤 SkipIntro 호출 시 마지막 스프라이트와 알파 1, 로딩 전 대기 상태, 안내 알파의 반복 변화, 이후 로비 전환을 확인했다. 입력 시스템의 합성 마우스 주입은 Simulator에서 전달되지 않아 상태 검증은 메서드 호출로 수행했다. 실제 Android 터치 검증은 미수행이다.

- 인트로 속도 조정: 첫 프레임을 1초 고정한 채 알파 0→1로 올리고, 이어서 5초 프레임을 재생한다. 총 길이와 CompleteIntro 이벤트를 6초로 변경했다. 스프라이트 순서와 참조, 종료 후 안내 및 건너뛰기 처리는 기존 구성을 사용한다. 클립의 알파 곡선·프레임 키 시간·종료 시간·이벤트 일치를 정적 검사했다.

## 로비 메인 화면 (구현 이력)

- 로비 씬의 Canvas와 SafeArea 아래 상단 재화 표시, 설정 아이콘, 기본 카게 스프라이트, 하단 메뉴를 배치했다. 1080×1920 기준 Canvas Scaler를 사용하고 Canvas 정렬 순서를 -1로 설정해 타이틀 페이드 뒤에 표시되게 했다.
- 재화는 시안용 0으로 표시한다. 첫 재화 슬롯은 기존 빨간 구슬 아이콘을 사용했다. 설정 및 하단 메뉴의 화면 연결은 별도 구현 범위다.
- Unity Pixel 5 Simulator에서 타이틀에서 로비로 진입한 뒤 배경, 캐릭터, 재화와 안전 영역 내 메뉴 배치를 확인했다. 콘솔에는 UnityEditor.Graphs.Edge.WakeUp 오류가 타이틀 단계부터 표시됐다. 다른 화면 비율과 실기기는 미검증이다.

- 계층 정리: UIManager의 Canvas 아래에 WindowManager와 PopUpManager를 배치했다. WindowManager가 Title_Window와 Lobby_Window를 씬 활성화에 맞춰 전환한다. 공유 페이드는 Canvas에 배치했다. 타이틀 Animator의 바인딩 경로를 새 계층에 맞췄으며, 에셋 이동 시 meta를 함께 옮겼다. C# 컴파일, 씬 참조 및 클립 경로를 확인했다.
- Canvas 아래 공용 SafeArea 하나를 배치하고 WindowManager와 PopUpManager를 그 아래에 연결했다. 각 화면의 배경 레이어는 전체 화면까지 확장하고 SceneFade는 Canvas 바로 아래에 둔다.
- 로비 상단 안전 영역 밖의 여백에 검은 띠를 적용했다. 띠는 상단바 위부터 화면 끝까지 채운다. 씬 참조와 여백 계산을 검사했다.
- 로비의 상하좌우 검은 띠는 기기별 안전 영역 밖 여백에 맞춰 크기를 조절한다.


## 로비 버튼 화면 전환 연결 (초기 구현 이력)

- 날짜: 2026-11-19
- 관련 기획: UI 기조의 로비 하단 메뉴와 환경 설정.
- 관련 파일: Assets/01_Scenes/00_Tilte_Scene.unity (4505~4738), Assets/02_Scripts/Manager/UIManager.cs (12~17, 53~84).
- 구현 구조: uGUI Button.onClick → UIManager.Open... → WindowManager.Open 또는 PopUpManager.Open. 씬에 배치한 EventSystem과 InputSystemUIInputModule이 클릭과 터치를 처리한다.
- 구현 결과: 상점, 장비, 던전(스테이지 선택), 캐릭터, 특성, 설정 버튼을 연결했다. 대상 참조가 비어 있으면 로비에 머문다. 이후 각 대상 오브젝트를 UIManager Inspector에 할당한다.
- 검증: 버튼 6개의 이미지 레이캐스트, 클릭 이벤트의 메서드, 대상 필드 및 EventSystem 계층을 파일에서 확인했다. Unity Play Mode와 모바일 실기기 입력은 미검증이다.

## 로비 UI 스프라이트와 재화 아이콘

- 날짜: 2026-11-19
- 관련 파일: Assets/01_Scenes/00_Tilte_Scene.unity (2103~4068 부근).
- 구현 결과: 로비 상·하단 바와 테두리, 재화 슬롯, 설정 버튼과 하단 버튼에 기존 Etc_Sheet 스프라이트를 연결했다. 금자는 Icon_Sheet_1_63, 은자는 Icon_Sheet_0_1을 사용한다.
- 검증: 스프라이트 참조 21개와 버튼 클릭 이벤트 6개를 파일에서 검사했다. Unity 화면 표시는 미검증이다.

## UI 책임 분리와 초기 설정 보완

- 날짜: 2026-11-19
- 관련 기획: Docs/InitSetting/UIManager.md, GameManager.md, PoolManager.md, SoundManager.md 및 1080×1920 기준 로비.
- 관련 파일: Assets/01_Scenes/00_Tilte_Scene.unity, Assets/02_Scripts/UI/Lobby/LobbyWindow.cs, UI/Title/TitleScreen.cs, UI/SafeAreaLayout.cs, Manager/UIManager.cs, Manager/WindowManager.cs, Manager/PopUpManager.cs, Manager/GameManager.cs, Manager/SoundManager.cs, Manager/PoolManager.cs, Pooling/ObjectPool.cs, Core/EnumList.cs.
- 로비: Lobby_Window의 LobbyWindow가 버튼 이벤트 6개를 받아 WindowType과 PopUpType으로 열기를 요청한다. 대상 슬롯이 비어 있으면 현재 화면을 유지한다.
- 타이틀: TitleScreen이 클릭·터치, 인트로, 재시도 표시를 관리한다. GameManager가 데이터 로딩과 씬 전환을 진행한다. WindowManager는 등록된 화면을 열고 IWindowable.ResetUI를 호출하며, UIManager는 두 매니저의 Inspector 참조를 보유한다.
- 공용 UI: 타이틀 씬의 UIManager → Canvas → SafeArea → WindowManager 및 PopUpManager 계층을 사용한다. SafeAreaLayout는 안전 영역 안에 9:16을 유지하는 영역을 배치하고 로비에서 그 바깥을 검은 띠로 채운다.
- 풀링: 프리팹별 풀 사이에 살아 있는 인스턴스 수를 공유하여 합계 200개를 제한한다. 재사용할 인스턴스가 없고 제한에 도달한 경우 Rent는 null을 반환한다. 사전 생성은 대상 프리팹과 수량을 정하는 단계에서 연결한다.
- 사운드: 씬별 BGM 슬롯을 Inspector에 두고 씬 전환에 맞춰 재생한다. 현재 오디오 클립 에셋이 없어 세 슬롯은 비어 있다. SetMute로 BGM과 SFX를 제어할 수 있으며 음소거 저장과 설정 팝업 연결은 후속 작업이다.
- 검증: Unity 참조로 Roslyn 컴파일, 버튼 이벤트 6개 및 Inspector 참조 정적 검사, 9:16·긴 화면·넓은 화면·비대칭 안전 영역을 포함한 5개 배치 계산을 확인했다. Unity Play Mode와 실기기는 미검증이다.

## 싱글톤 매니저 참조 정리

- 날짜: 2026-11-19
- 관련 파일: Assets/02_Scripts/Manager/GameManager.cs, Assets/02_Scripts/UI/Title/TitleScreen.cs, Assets/02_Scripts/UI/Lobby/LobbyWindow.cs, Assets/01_Scenes/00_Tilte_Scene.unity.
- 구현 결과: 매니저 소비 코드는 Singleton의 Instance로 접근한다. LobbyWindow는 UIManager.Instance를 통해 WindowManager와 PopUpManager를 사용한다. 씬에 배치된 매니저, UIManager의 하위 매니저 연결, GameManager의 TitleScreen 참조는 기존 Inspector 구성을 사용한다.
- 검증: C# 컴파일, 씬의 중복 참조 6개 제거, LobbyWindow 버튼 연결 6개를 확인했다. Unity Play Mode는 미실행이다.
