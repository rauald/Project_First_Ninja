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


## FUNC-004 — 캐릭터·몬스터 애니메이션

- 날짜: 2026-11-19
- 관련 기획: 캐릭터는 로비 Idle/Attack, 전투 Idle/Run/Death. 몬스터는 Move/Attack/Death.
- 사용한 기능·기술: Unity 6000.3.9f1 AnimationClip, Animator Controller, uGUI Image, SpriteRenderer, 기존 Multiple Sprite 시트.
- 선택 이유: 기존 이미지와 스프라이트 GUID·fileID를 유지하며 프레임 애니메이션을 재사용한다. 로비의 Image와 전투의 SpriteRenderer에 맞춰 바인딩을 분리한다.
- 구현 결과: 캐릭터 6종 × 5개 = 30개 클립, 몬스터 19종 × 3개 = 57개 클립, Controller 31개(캐릭터 로비 6·전투 6, 몬스터 19)를 생성했다. 총 원본 프레임 1,254개를 사용한다. 모든 클립은 12 FPS이며 마지막 프레임을 한 프레임 길이 동안 유지한다. Idle/Run/Move는 반복, Attack/Death는 1회 재생한다.
- 원본 처리: 시트의 위→아래, 왼쪽→오른쪽 순서다. 오크의 Attack 22프레임, Death 24프레임도 모두 사용한다. Skeleton_Attacl, Skeleton_Deatj, Skeleton_move, Slime_Attacl 원본 파일명은 유지하고 출력 에셋의 상태 이름만 Attack/Death/Move로 통일했다.

### 구현 구조와 사용 방법

```text
캐릭터 Lobby Controller (Animator와 Image가 같은 오브젝트)
  Entry → Idle -- Attack Trigger → Attack -- Exit Time 1 → Idle
캐릭터 Battle Controller (Animator와 SpriteRenderer가 같은 오브젝트)
  Entry → Idle ← IsMoving bool → Run
  Idle/Run -- Death Trigger → Death (종료 상태)
몬스터 Battle Controller (Animator와 SpriteRenderer가 같은 오브젝트)
  Entry → Move -- Attack Trigger → Attack -- Exit Time 1 → Move
  Move/Attack -- Death Trigger → Death (종료 상태)
```

- 상태 이름: Idle, Attack, Run, Move, Death. Controller별로 해당 상태만 포함한다.
- 파라미터: 로비는 Attack(Trigger), 캐릭터 전투는 IsMoving(Bool)·Death(Trigger), 몬스터는 Attack(Trigger)·Death(Trigger).
- 호출 예: Animator.SetTrigger("Attack"), Animator.SetBool("IsMoving", true), Animator.SetTrigger("Death").
- 전환 시간은 0초다. Death 전환을 이동·공격 전환보다 우선 배치하고 Death에는 나가는 전환을 두지 않는다.
- 재사용 시 Animator.Rebind()와 Animator.Update(0f)로 기본 상태를 초기화한다. 현재 요청은 에셋 생성 범위로, 호출 스크립트와 씬·프리팹 연결은 포함하지 않는다.
- 기존 씬과 이미지의 사용자 변경은 수정하지 않았다.

### 검증 결과와 미검증 항목

- 정적 검사: 클립 87개, Controller 31개, 원본 프레임 1,254개의 순서·스프라이트 GUID/fileID·12 FPS·반복 설정·Controller 내부 참조를 확인했다.
- Unity 6000.3.9f1 격리 프로젝트에서 전체 클립과 Controller 임포트, 로비 Image/전투 SpriteRenderer 바인딩, 재생 길이, 반복 여부, 모든 1,254프레임의 SampleAnimation 적용을 검사했다. 검증 오브젝트에는 Animator를 포함했다.
- 원본 프로젝트의 배치 실행은 프로젝트 경로 선택 후 반환 코드 1로 종료했다. 원본 Temp/UnityLockfile을 변경하지 않고 격리 프로젝트에서 에셋을 검증했다.
- 최종 클립은 Unity AnimationUtility로 직렬화한 에셋이다. 재생 길이는 캐릭터·일반 공격/죽음 16프레임 기준 약 1.333초, 몬스터 이동 8프레임 약 0.667초, 오크 공격 22프레임 약 1.833초, 오크 죽음 24프레임 2초다.
- 미검증: 원본 씬에서의 Play Mode 재생, 실제 게임 코드가 호출하는 상태 전환, 기기 화면 표시, Windows·Android 빌드와 실기기. 씬·프리팹 연결은 이번 승인 범위에 포함하지 않았다.
- 검증 로그: C:/Users/user/.codex/visualizations/2026/11/19/01a1f241-c0e8-7d63-b581-b1d4f49a68fc/animation-validation.log

### 관련 파일

| 파일 | 줄 수 |
|---|---:|
| `Assets/07_Animations/Character/Earth_Ninja/Earth_Battle_Death.anim` | 92 |
| `Assets/07_Animations/Character/Earth_Ninja/Earth_Battle_Idle.anim` | 92 |
| `Assets/07_Animations/Character/Earth_Ninja/Earth_Battle_Run.anim` | 92 |
| `Assets/07_Animations/Character/Earth_Ninja/Earth_Select_Attack.anim` | 92 |
| `Assets/07_Animations/Character/Earth_Ninja/Earth_Select_Idle.anim` | 92 |
| `Assets/07_Animations/Character/Fire_Ninja/Fire_Battle_Death.anim` | 92 |
| `Assets/07_Animations/Character/Fire_Ninja/Fire_Battle_Idle.anim` | 92 |
| `Assets/07_Animations/Character/Fire_Ninja/Fire_Battle_Run.anim` | 92 |
| `Assets/07_Animations/Character/Fire_Ninja/Fire_Select_Attack.anim` | 92 |
| `Assets/07_Animations/Character/Fire_Ninja/Fire_Select_Idle.anim` | 92 |
| `Assets/07_Animations/Character/Non_Ninja/Non_Battle_Death.anim` | 92 |
| `Assets/07_Animations/Character/Non_Ninja/Non_Battle_Idle.anim` | 92 |
| `Assets/07_Animations/Character/Non_Ninja/Non_Battle_Run.anim` | 92 |
| `Assets/07_Animations/Character/Non_Ninja/Non_Select_Attack.anim` | 92 |
| `Assets/07_Animations/Character/Non_Ninja/Non_Select_Idle.anim` | 92 |
| `Assets/07_Animations/Character/Water_Ninja/Water_Battle_Death.anim` | 92 |
| `Assets/07_Animations/Character/Water_Ninja/Water_Battle_Idle.anim` | 92 |
| `Assets/07_Animations/Character/Water_Ninja/Water_Battle_Run.anim` | 92 |
| `Assets/07_Animations/Character/Water_Ninja/Water_Select_Attack.anim` | 92 |
| `Assets/07_Animations/Character/Water_Ninja/Water_Select_Idle.anim` | 92 |
| `Assets/07_Animations/Character/Wind_Ninja/Wind_Battle_Death.anim` | 92 |
| `Assets/07_Animations/Character/Wind_Ninja/Wind_Battle_Idle.anim` | 92 |
| `Assets/07_Animations/Character/Wind_Ninja/Wind_Battle_Run.anim` | 92 |
| `Assets/07_Animations/Character/Wind_Ninja/Wind_Select_Attack.anim` | 92 |
| `Assets/07_Animations/Character/Wind_Ninja/Wind_Select_Idle.anim` | 92 |
| `Assets/07_Animations/Character/Wood_Ninja/Wood_Battle_Death.anim` | 92 |
| `Assets/07_Animations/Character/Wood_Ninja/Wood_Battle_Idle.anim` | 92 |
| `Assets/07_Animations/Character/Wood_Ninja/Wood_Battle_Run.anim` | 92 |
| `Assets/07_Animations/Character/Wood_Ninja/Wood_Select_Attack.anim` | 92 |
| `Assets/07_Animations/Character/Wood_Ninja/Wood_Select_Idle.anim` | 92 |
| `Assets/07_Animations/Monster/Boar/Boar_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/Boar/Boar_Death.anim` | 92 |
| `Assets/07_Animations/Monster/Boar/Boar_Move.anim` | 76 |
| `Assets/07_Animations/Monster/Cobra/Cobra_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/Cobra/Cobra_Death.anim` | 92 |
| `Assets/07_Animations/Monster/Cobra/Cobra_Move.anim` | 76 |
| `Assets/07_Animations/Monster/Deer/Deer_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/Deer/Deer_Death.anim` | 92 |
| `Assets/07_Animations/Monster/Deer/Deer_Move.anim` | 76 |
| `Assets/07_Animations/Monster/ElderGoblin/ElderGoblin_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/ElderGoblin/ElderGoblin_Death.anim` | 92 |
| `Assets/07_Animations/Monster/ElderGoblin/ElderGoblin_Move.anim` | 76 |
| `Assets/07_Animations/Monster/FireElemental/FireElemental_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/FireElemental/FireElemental_Death.anim` | 92 |
| `Assets/07_Animations/Monster/FireElemental/FireElemental_Move.anim` | 76 |
| `Assets/07_Animations/Monster/FireSlime/FireSlime_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/FireSlime/FireSlime_Death.anim` | 92 |
| `Assets/07_Animations/Monster/FireSlime/FireSlime_Move.anim` | 76 |
| `Assets/07_Animations/Monster/Goblin/Goblin_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/Goblin/Goblin_Death.anim` | 92 |
| `Assets/07_Animations/Monster/Goblin/Goblin_Move.anim` | 76 |
| `Assets/07_Animations/Monster/IceElemental/IceElemental_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/IceElemental/IceElemental_Death.anim` | 92 |
| `Assets/07_Animations/Monster/IceElemental/IceElemental_Move.anim` | 76 |
| `Assets/07_Animations/Monster/IceSlime/IceSlime_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/IceSlime/IceSlime_Death.anim` | 92 |
| `Assets/07_Animations/Monster/IceSlime/IceSlime_Move.anim` | 76 |
| `Assets/07_Animations/Monster/LesserFireDemon/LesserFireDemon_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/LesserFireDemon/LesserFireDemon_Death.anim` | 92 |
| `Assets/07_Animations/Monster/LesserFireDemon/LesserFireDemon_Move.anim` | 76 |
| `Assets/07_Animations/Monster/Mummy/Mummy_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/Mummy/Mummy_Death.anim` | 92 |
| `Assets/07_Animations/Monster/Mummy/Mummy_Move.anim` | 76 |
| `Assets/07_Animations/Monster/Orc/Orc_Attack.anim` | 104 |
| `Assets/07_Animations/Monster/Orc/Orc_Death.anim` | 108 |
| `Assets/07_Animations/Monster/Orc/Orc_Move.anim` | 76 |
| `Assets/07_Animations/Monster/PoisonSlime/PoisonSlime_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/PoisonSlime/PoisonSlime_Death.anim` | 92 |
| `Assets/07_Animations/Monster/PoisonSlime/PoisonSlime_Move.anim` | 76 |
| `Assets/07_Animations/Monster/Scorpion/Scorpion_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/Scorpion/Scorpion_Death.anim` | 92 |
| `Assets/07_Animations/Monster/Scorpion/Scorpion_Move.anim` | 76 |
| `Assets/07_Animations/Monster/Skeleton/Skeleton_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/Skeleton/Skeleton_Death.anim` | 92 |
| `Assets/07_Animations/Monster/Skeleton/Skeleton_Move.anim` | 76 |
| `Assets/07_Animations/Monster/Slime/Slime_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/Slime/Slime_Death.anim` | 92 |
| `Assets/07_Animations/Monster/Slime/Slime_Move.anim` | 76 |
| `Assets/07_Animations/Monster/Sphinx/Sphinx_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/Sphinx/Sphinx_Death.anim` | 92 |
| `Assets/07_Animations/Monster/Sphinx/Sphinx_Move.anim` | 76 |
| `Assets/07_Animations/Monster/Wolf/Wolf_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/Wolf/Wolf_Death.anim` | 92 |
| `Assets/07_Animations/Monster/Wolf/Wolf_Move.anim` | 76 |
| `Assets/07_Animations/Monster/Yeti/Yeti_Attack.anim` | 92 |
| `Assets/07_Animations/Monster/Yeti/Yeti_Death.anim` | 92 |
| `Assets/07_Animations/Monster/Yeti/Yeti_Move.anim` | 76 |
| `Assets/07_Animations/Character/Earth_Ninja/Earth_Ninja_Lobby.controller` | 156 |
| `Assets/07_Animations/Character/Earth_Ninja/Earth_Ninja_Battle.controller` | 246 |
| `Assets/07_Animations/Character/Fire_Ninja/Fire_Ninja_Lobby.controller` | 156 |
| `Assets/07_Animations/Character/Fire_Ninja/Fire_Ninja_Battle.controller` | 246 |
| `Assets/07_Animations/Character/Non_Ninja/Non_Ninja_Lobby.controller` | 156 |
| `Assets/07_Animations/Character/Non_Ninja/Non_Ninja_Battle.controller` | 246 |
| `Assets/07_Animations/Character/Water_Ninja/Water_Ninja_Lobby.controller` | 156 |
| `Assets/07_Animations/Character/Water_Ninja/Water_Ninja_Battle.controller` | 246 |
| `Assets/07_Animations/Character/Wind_Ninja/Wind_Ninja_Lobby.controller` | 156 |
| `Assets/07_Animations/Character/Wind_Ninja/Wind_Ninja_Battle.controller` | 246 |
| `Assets/07_Animations/Character/Wood_Ninja/Wood_Ninja_Lobby.controller` | 156 |
| `Assets/07_Animations/Character/Wood_Ninja/Wood_Ninja_Battle.controller` | 246 |
| `Assets/07_Animations/Monster/Boar/Boar_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/Cobra/Cobra_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/Deer/Deer_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/ElderGoblin/ElderGoblin_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/FireElemental/FireElemental_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/FireSlime/FireSlime_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/Goblin/Goblin_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/IceElemental/IceElemental_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/IceSlime/IceSlime_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/LesserFireDemon/LesserFireDemon_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/Mummy/Mummy_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/Orc/Orc_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/PoisonSlime/PoisonSlime_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/Scorpion/Scorpion_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/Skeleton/Skeleton_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/Slime/Slime_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/Sphinx/Sphinx_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/Wolf/Wolf_Battle.controller` | 243 |
| `Assets/07_Animations/Monster/Yeti/Yeti_Battle.controller` | 243 |


## FUNC-005 — 로비 기본 캐릭터와 주기 공격 애니메이션

- 날짜: 2026-11-19
- 관련 기획: 로비 중앙의 Non_Ninja는 Idle을 반복하며 로비 진입 후 10초마다 Attack을 한 번 재생한다. 사용자 요청의 None_Ninja/Attak는 프로젝트의 Non_Ninja/Attack을 뜻한다.
- 관련 파일: Assets/02_Scripts/UI/Lobby/LobbyWindow.cs (65줄, OnEnable/Update 추가), Assets/01_Scenes/00_Tilte_Scene.unity (1956줄 Animator 참조, 4369줄 Animator 추가).
- 사용한 기능·기술: uGUI Image, Animator, Time.unscaledDeltaTime, Animator.Rebind, Animator.Update, Animator.SetTrigger.
- 선택 이유: 기존 Non_Ninja_Lobby Controller의 Idle → Attack → Idle 전환과 원본 스프라이트를 재사용한다. UI 애니메이션과 타이머는 게임 시간 배율의 영향을 받지 않도록 UnscaledTime을 사용한다.
- 구현 구조: Lobby_Window.OnEnable → _attackTimer=0 → _characterAnimator.Rebind/Update(0) → Idle. Update → _attackTimer 누적 → 10초마다 Attack Trigger → Attack 1회 종료 후 Controller가 Idle로 복귀. 로비 비활성화 시 Update가 멈추고 재활성화 시 초기화한다.
- 구현 결과: 기존 Kage 오브젝트를 Non_Ninja로 표시하고 가로·세로 중심을 로비 영역의 (0.5, 0.5)로 배치했다. 기존 크기를 유지한다. Non_Select_Idle 첫 스프라이트와 Non_Ninja_Lobby Controller를 연결하고 LobbyWindow의 Inspector Animator 참조를 지정했다. 기존 로비 버튼 기능은 유지했다.
- 검증 결과: 씬의 Animator 참조·Controller GUID·중앙 앵커·중복 fileID 부재를 정적으로 검사했다. 실제 수정된 LobbyWindow.cs와 기존 Controller/클립을 복사한 격리 Unity 6000.3.9f1 Play Mode에서 Attack 진입 10.00003초·20.00001초, Idle 복귀 2회, 재진입 Idle·타이머 0을 확인했다. Time.timeScale=0에서도 재생됐다. 격리 검증의 다른 매니저는 스텁을 사용했다.
- 검증 중 에디터 이슈: UnityEditor.Search.SearchDatabase의 IndexOutOfRange 예외가 시작 인덱싱 중 기록됐으나 재생 주기 검증은 통과했다.
- 미검증: 원본 씬 Play Mode의 전체 타이틀→로비 흐름, 실제 기기에서의 중앙 표시, Windows·Android 빌드.
- 검증 로그: C:/Users/user/.codex/visualizations/2026/11/19/01a1f241-c0e8-7d63-b581-b1d4f49a68fc/lobby-animation-validation.log

### 로비 캐릭터 재생 속도 조정 (2026-11-19)

- 관련 파일: Assets/02_Scripts/UI/Lobby/LobbyWindow.cs:16.
- 변경: OnEnable의 Animator.Rebind 후 speed를 1.5f로 설정한다. 로비 Non_Ninja의 Idle·Attack을 모두 기존보다 1.5배 빠르게 재생한다.
- 공격 주기: 사용자가 수정한 ATTACK_INTERVAL = 3f를 유지한다. Time.unscaledDeltaTime 타이머는 Animator.speed와 독립적이므로 공격 시작 간격은 3초다.
- 검증: 소스에서 speed = 1.5f 적용, ATTACK_INTERVAL = 3f 유지 및 기존 타이머 처리 보존을 확인했다. 이번 변경의 Unity Play Mode 재생은 미검증이다.


### 로비 애니메이션 최종 설정 및 통합 검증 (2026-11-19)

- 최종 공격 간격은 사용자가 수정한 ATTACK_INTERVAL = 5f를 유지한다. 재생 속도는 Animator.speed = 1.5f다. 앞선 10초·3초 기록은 변경 이력이다.
- 실제 LobbyWindow.cs와 기존 Controller를 사용한 격리 Unity Play Mode 검증 결과: Attack 진입 5.000165초·10.00033초, Idle 복귀 2회, 재진입 Idle·타이머 0·speed 1.5. Time.timeScale=0에서도 동작했다.
- 전체 87개 클립·31개 Controller·1,254프레임 검증은 이전 통과 결과를 따른다. 원본 씬의 전체 흐름과 기기 화면 및 독립 빌드는 미검증이다.
- 사용자가 현재 로비 씬 및 UI 시트 meta 변경을 모두 포함하여 main 대상 PR로 통합하도록 승인했다.
- 최종 검증 로그: C:/Users/user/.codex/visualizations/2026/11/19/01a1f241-c0e8-7d63-b581-b1d4f49a68fc/lobby-animation-final-validation.log

## FUNC-006 — 캐릭터 화면 슬라이드 전환

- 날짜: 2026-11-19
- 관련 기획: 로비 캐릭터 버튼을 누르면 왼쪽의 캐릭터 화면이 들어오고 로비가 오른쪽으로 이동한다. 페이드 없이 이동하며 로비는 화면 밖에 도착한 뒤 비활성화한다.
- 관련 파일: Assets/02_Scripts/Manager/WindowManager.cs:7, :44 / Assets/01_Scenes/00_Tilte_Scene.unity:929, :4374.
- 사용한 기능·기술: RectTransform.anchoredPosition, Coroutine, Mathf.SmoothStep, Time.unscaledDeltaTime, CanvasGroup.
- 선택 이유: 기존 LobbyWindow.OpenCharacterWindow와 WindowType.Character를 유지하고 WindowManager에서 두 화면의 이동 및 활성 상태를 관리한다. 새 이미지 에셋은 사용하지 않는다.
- 구현 구조: CharacterButton → LobbyWindow.OpenCharacterWindow → WindowManager.Open(Character) → SlideToCharacter → Character_Window 활성화 → 두 화면 오른쪽 이동 → 로비 비활성화 → 로비 좌표 복원 → 현재 화면 변경.
- 주요 필드: _slideDuration(Inspector 기본 0.35초), _inputGroup(WindowManager의 CanvasGroup), _isSliding(중복 전환 차단), _current(현재 화면).
- 구현 결과: 비활성 Character_Window 기본 컨테이너를 생성하고 Character 슬롯에 연결했다. 컨테이너는 단색 Image이며 상세 캐릭터 UI는 이번 범위에 포함하지 않는다. SafeAreaLayout의 전체 화면 레이어에 등록해 기기 화면 전체를 덮는다. 전체 Canvas 너비만큼 이동하며 전환 중 CanvasGroup의 interactable/blocksRaycasts를 차단한다. alpha는 변경하지 않는다. 완료 후 입력을 복구한다. 이동 시간 0은 즉시 완료된다. 이후 로비를 다시 열면 원래 중앙 위치에서 활성화된다. 캐릭터 화면의 복귀 버튼 및 역방향 전환은 이번 범위에 포함하지 않는다.
- 검증 결과: dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet 성공(경고 0, 오류 0). 씬 fileID 중복 부재, 신규 컴포넌트 존재, Character 슬롯과 기존 버튼 연결을 정적으로 검사했다. 너비 1080/1200/1440에서 진행률 0/0.25/0.5/0.75/1의 좌표를 계산해 두 화면의 간격과 시작·종료 위치를 확인했다. git diff --check를 확인했다.
- 미검증: Unity Play Mode 실제 버튼 클릭·슬라이드 표시·전환 중 입력, 기기 해상도 변경 중 표시, Android 실기기 및 독립 빌드. 정적 좌표 검사는 실제 렌더링 검증을 대체하지 않는다.

### 로비 메뉴별 슬라이드 방향 변경 (2026-11-19)

- 현재 적용 기준: 방향은 대상 화면이 들어오는 위치를 뜻한다. 상점·장비는 왼쪽에서 진입하고 로비는 오른쪽으로 이동한다. 던전(StageSelect)은 아래에서 진입하고 로비는 위로 이동한다. 캐릭터·특성은 오른쪽에서 진입하고 로비는 왼쪽으로 이동한다. 앞선 캐릭터 왼쪽 진입 기록은 변경 이력이다.
- 관련 파일: Assets/02_Scripts/Manager/WindowManager.cs(GetEntryDirection, SlideToWindow), Assets/01_Scenes/00_Tilte_Scene.unity(Shop_Window, Equipment_Window, StageSelect_Window, Character_Window, Talent_Window).
- 구현 구조: LobbyWindow의 기존 메뉴 버튼 → Open(WindowType) → GetEntryDirection → SlideToWindow. 수평 전환은 전체 Canvas 너비, 수직 전환은 높이를 사용한다. 대상 컨테이너를 진입 방향 화면 밖에서 활성화하고 두 화면을 반대 방향으로 함께 이동시킨다. 로비는 끝에 도착한 뒤 비활성화하고 좌표를 복구한다.
- 유지 동작: 기본 0.35초(SmoothStep), unscaledDeltaTime, 전환 중 UI 입력 및 중복 전환 차단, 완료 후 입력 복원, 페이드 없음, 시간 0의 즉시 완료. 로비에서 각 메뉴로 진입할 때 적용한다. 복귀 UI와 메뉴 간 슬라이드는 이번 범위 밖이다.
- 씬 연결: 다섯 대상 모두 기존 WindowType 슬롯, WindowManager 자식, SafeAreaLayout 전체 화면 레이어에 연결했다. 화면은 상세 UI가 없는 단색 기본 컨테이너이며 신규 이미지 에셋은 사용하지 않는다.
- 검증 결과: dotnet build Assembly-CSharp.csproj --no-restore --verbosity quiet 성공(경고 0, 오류 0). 대상 5개 슬롯, 초기 비활성 상태, 컴포넌트 참조, 전체 화면 레이어와 자식 등록, 씬 fileID 중복 부재를 정적으로 검사했다. 세 가지 화면 크기(1080×1920, 1200×2400, 1440×2560)에서 세 방향의 진행률 0/0.25/0.5/0.75/1 좌표를 계산해 간격과 끝점을 확인했다. git diff --check 통과.
- 미검증: Unity Play Mode 실제 클릭·렌더링·입력 차단, 전환 중 해상도 변경, Android 실기기와 독립 빌드. 좌표 계산은 렌더링 검증을 대체하지 않는다.

## FUNC-007 — 타이틀 클릭·터치 안내 Image 전환

- 날짜: 2026-11-19
- 관련 기획: 인트로 종료 후 클릭·터치 안내가 위아래 잘리지 않게 표시한다.
- 관련 파일: Assets/01_Scenes/00_Tilte_Scene.unity(DesktopPrompt, MobilePrompt), Assets/07_Animations/Title/TitlePromptBlink.anim.
- 사용한 기능·기술: uGUI Image, 기존 Multiple Sprite, Preserve Aspect, Animator 알파 곡선.
- 선택 이유: 시트 일부를 수동 UV 좌표로 표시하던 RawImage를 실제 문구 영역으로 슬라이스된 스프라이트 참조로 바꾼다.
- 구현 구조: TitleScreen의 기존 안내 활성화 → Image에 문구 스프라이트 표시 → TitlePromptBlink가 Image.m_Color.a를 반복 변경한다. TitleScreen과 Animator Controller 참조는 유지한다.
- 구현 결과: DesktopPrompt는 Etc_Sheet_1_32(548×67, fileID -5065698487816741814), MobilePrompt는 Etc_Sheet_1_33(552×69, fileID -4026338688705661175)을 연결했다. 두 안내의 RawImage를 Image(Simple, PreserveAspect=1)로 바꾸고 수동 Texture/UVRect 설정을 제거했다. 기존 위치와 표시 영역을 유지한다. 깜빡임 클립의 실행 곡선·genericBindings·편집 곡선에 있는 컴포넌트 GUID 3곳을 Image로 변경했다. 알파 키와 1초 반복 주기는 유지한다. 원본 이미지와 meta는 수정하지 않았다.
- 검증 결과: 원본 시트를 직접 열어 클릭·터치 문구 위치를 확인했다. 두 Image 타입·스프라이트 fileID 존재·비율 유지·UV 설정 제거를 정적 검사했다. 애니메이션의 바인딩 3개가 Image를 가리키고 GUID 외의 클립 내용이 기존과 같은지 검사했다. git diff --check 통과. C# 변경은 없다.
- 미검증: Unity Play Mode에서 인트로 종료 후 실제 안내 표시·깜빡임, Android 실기기 및 독립 빌드.

## FUNC-008 — 캐릭터 선택 화면

- 날짜: 2026-11-19
- 관련 기획: `UI_기조.xlsb`의 `<로비 화면>`, `<캐릭터 화면>`, `<기능 팝업>`, `<공용 팝업>`. `캐릭터_기조.xlsb`와 `스킬_기조.xlsb`의 캐릭터·성장·패시브·각성 설명.
- 사용자 확정: 최초에는 카게만 해금한다. 사용자가 수정한 캐릭터 DB의 패시브 참조 18개를 CharacterData.csv에 동기화했다. 원본 XLSB는 수정하지 않았다.
- 사용한 기능·기술: uGUI Image·Text·Button·Slider, Inspector 참조, 기존 로비 Animator, CSV 기반 성장 계산, 기존 JSON 저장, EventSystem 레이캐스트.
- 선택 이유: 기존 화면 전환과 에셋을 유지하며, UI를 씬에 미리 배치해 사용자가 직접 RectTransform과 스프라이트를 수정할 수 있게 한다. 성장·재료 처리는 UI 밖 CharacterProgression이 담당한다.
- 구현 구조: GameManager의 최초 데이터 로드 → CharacterProgression.Initialize → WindowManager.Open(Character) → CharacterWindow.ResetUI → 장착 캐릭터 정보·전체 캐릭터 6슬롯 표시. 인벤토리 클릭 → CharacterPopUp.ShowCharacter → 장착 또는 성장 확인. 스킬 클릭 → ShowSkill → 닫으면 이전 정보 화면 복원.
- 구현 결과: 재화·설정·로비 복귀·하단 메뉴, 등급·레벨·성급·공격력·방어력·체력, 캐릭터별 애니메이션, 패시브 3개와 각성기·잠금·설명, 정보 팝업, 장착과 저장·로비 반영, 레벨업·성급업·등급업 비용 확인·확인·취소·재료 부족 알림을 연결했다. 미해금 캐릭터는 1레벨·0성·N등급 기본 능력치를 보여주고 장착·성장을 막는다.
- 성장 기준: 태생 등급은 고정하고 현재 등급은 N부터 시작한다. 현재 CSV 범위에 따라 최대 레벨 100·5성·E등급이다. 레벨업은 은자, 성급업은 은자와 해당 캐릭터 조각, 등급업은 해당 속성석을 사용한다. 능력치는 기본 값에 완료한 레벨·성급·등급 성장치를 누적한다. 레벨 구간은 성장 전 레벨 기준으로 해석했다. 패시브는 1·3·5성, 각성기는 세 번째 패시브와 함께 5성에서 열린다. 각성 설명은 캐릭터의 0~5 식별자를 기존 전투 액티브 DB 19~24행에 연결한다.
- 저장 구조: SaveLoadData에 EquippedCharacterId, Characters, CharacterMaterials, IsDamageTextEnabled를 추가했다. 재료는 ItemType·DetailType·Amount로 저장한다. 기존 볼륨만 있는 저장 파일에도 기본 캐릭터 상태를 채운다. 장착과 성장은 저장 실패 시 상태와 차감 재료를 복구한다. SaveLoadManager 자체는 변경하지 않았다.
- 설정: BGM·SFX 0~100 표시·슬라이더와 데미지 폰트 OFF/ON 저장을 연결했다. 데미지 폰트의 전투 렌더링은 아직 존재하지 않아 이번 범위는 설정 저장까지다.
- 편집 위치: 타이틀 씬의 Canvas/SafeArea/WindowManager/Character_Window 아래 Background·TopBar·EquippedCharacter·Inventory·BottomBar·LobbyButton. 팝업은 같은 SafeArea의 PopUpManager/Character_PopUp·Settings_PopUp 아래다. 배경만 전체 화면 레이어로 등록하고 UI 내용은 안전 영역 안에 둔다.
- 에셋: 기존 BG_Character, 6종 캐릭터·로비 Controller, 상·하단 바·재화·메뉴·스킬 아이콘을 재사용했다. 새 이미지는 만들지 않았다. 패널과 잠금 표시는 기본 Image·Text로 틀을 잡았다.
- 편집 도구: CharacterScreenSetup은 최초 씬 생성용 Editor 도구다. 이미 CharacterWindow가 있으면 재생성을 거부하므로 사용자 배치를 덮어쓰지 않는다. 실행 중 UI 오브젝트는 생성하지 않는다.
- 검증: 전체 C# 및 Editor 생성 코드 Roslyn 컴파일 성공(경고 0·오류 0). 순수 성장 로직 37개 검사 통과. 격리 Unity 6000.3.9f1 Play Mode에서 타이틀→로비→캐릭터, 6종 정보·24개 스킬 참조, 팝업 복귀, 비용 확인·취소·재료 부족·3종 성장, 장착·로비 반영, 실제 JSON 저장·재로드와 설정을 실행했다. 1080×1920·1080×2340 렌더 텍스처와 팝업 이미지를 직접 확인했다. 렌더 텍스처 검증은 기기 안전 영역 검증을 대신하지 않는다.
- 검증 환경 이슈: 원본 프로젝트 배치 실행은 시작 단계에서 반환 코드 1로 종료하여 원본 잠금 파일을 건드리지 않고 전체 Assets·Packages·ProjectSettings 복사본으로 검증했다. 격리 프로젝트의 긴 경로에 따른 일부 에디터 패키지 임포트 경고, Burst DLL 로딩 오류 4551과 UnityEditor.Search.SearchDatabase의 시작 인덱싱 예외가 기록됐다. 게임 코드 오류와 에디터 인덱서 오류는 분리해 기록한다.
- 미검증: 원본 프로젝트에서 사용자 직접 클릭, Android 실기기 터치·안전 영역·폰트, 독립 Windows·Android 빌드, 다른 캐릭터의 획득 경로와 전투 사용. 보유 재화의 초기값은 기존처럼 0이며 지급·뽑기 기능을 추가하지 않았다.
- 로그·캡처: `C:/Users/user/.codex/visualizations/2026/11/19/01a1f286-4ab2-7bd0-9bcb-13feaf931814/`의 character-build.log, character-playmode-final.log, character-*.png.
- 관련 파일: Assets/01_Scenes/00_Tilte_Scene.unity, Assets/02_Scripts/UI/Character/ 아래 화면·뷰·팝업·슬롯·표현 데이터·Editor 생성 코드와 meta, Assets/02_Scripts/Data/CharacterProgression.cs·CharacterProgressData.cs·CharacterMaterialData.cs·CharacterGrowthType.cs·SaveLoadData.cs·CsvTable.cs, Assets/02_Scripts/Manager/GameManager.cs, Assets/02_Scripts/Core/EnumList.cs, Assets/02_Scripts/UI/Lobby/LobbyWindow.cs, Assets/10_Data/CharacterData.csv.
- 최종 실행 검증: CHARACTER_PLAYMODE_PASS checks=99 runtimeErrors=0 editorSearchErrors=1, Unity 반환 코드 0. 실제 EventSystem의 최상위 레이캐스트를 거쳐 인벤토리·장착·성장 확인 버튼을 실행했고, 설정 UI 렌더 프레임 이후 슬라이더 트랙을 포인터로 눌러 볼륨 저장을 확인했다. 버튼뿐 아니라 트랙·핸들에도 레이캐스트를 연결했다. 최종 전체 C# 컴파일은 경고 0·오류 0, git diff --check와 씬 ID·내부 참조·에셋/패키지 GUID·신규 meta·패시브 참조 18개 검사도 통과했다.
- 신규 코드 줄 수: CharacterGrowthType.cs 8, CharacterMaterialData.cs 9, CharacterProgressData.cs 11, CharacterProgression.cs 244, CharacterDetailView.cs 88, CharacterInventorySlot.cs 25, CharacterPopUp.cs 159, CharacterPresentation.cs 12, CharacterSettingsPopUp.cs 53, CharacterWindow.cs 53, Editor/CharacterScreenSetup.cs 416.
- 기능명세서: `C:/Users/user/OneDrive/프로젝트/11월 개인 프로젝트/기능명세서/20261119/캐릭터 선택 화면.md`.

## FUNC-009 — 공통 레터박스와 메뉴 위치별 슬라이드

- 날짜: 2026-11-19
- 관련 기획: 모든 화면에 동일한 레터박스를 유지하고 하단 메뉴의 상대 위치로 화면 전환 방향을 결정한다는 사용자 수정 요청.
- 관련 파일: Assets/02_Scripts/UI/SafeAreaLayout.cs(공통 레터박스 참조·계산), Assets/02_Scripts/Manager/WindowManager.cs(Open, GetEntryDirection, GetMenuIndex, SlideToWindow), Assets/01_Scenes/00_Tilte_Scene.unity(SafeArea·로비·레터박스 4개 계층).
- 사용한 기능·기술: 기존 SafeAreaLayout의 1080:1920 비율·기기 안전 영역 계산, 공용 Canvas의 형제 렌더링 순서, RectTransform 슬라이드·Coroutine·CanvasGroup.
- 선택 이유: 로비 안의 기존 검은 띠 4개를 재사용해, 각 화면에 중복된 띠를 만들지 않고 활성 상태와 위치를 공통으로 유지한다. 기존 전환 코드에서 시작·대상 메뉴 비교만 확장한다.
- 레터박스 구조: Canvas/SafeArea 아래 WindowManager, PopUpManager, TopSafeAreaLetterbox, BottomSafeAreaLetterbox, LeftSafeAreaLetterbox, RightSafeAreaLetterbox 순서로 배치했다. 로비의 자식 목록에서 띠를 제거하고 부모를 SafeArea로 바꿨다. 검은 띠는 화면·팝업보다 나중에 그려지며 메뉴와 함께 이동하거나 비활성화되지 않는다. Canvas의 SceneFade는 기존 위치를 유지한다. 띠 참조 이름은 _topLetterbox·_bottomLetterbox·_leftLetterbox·_rightLetterbox로 변경하고 씬의 직렬화 필드도 맞췄다.
- 슬라이드 구조: Open → 현재 _current의 WindowType 조회 → GetEntryDirection(previous, next) → GetMenuIndex 비교 → SlideToWindow. 메뉴 순서는 Shop=0, Equipment=1, Lobby/StageSelect=2, Character=3, Talent=4다. 작은 번호의 대상은 왼쪽에서 들어오며 현재 화면은 오른쪽으로 이동한다. 큰 번호의 대상은 오른쪽에서 들어오며 현재 화면은 왼쪽으로 이동한다. 모든 메뉴 간 이동에 적용한다.
- 같은 메뉴 위치: 로비→StageSelect는 아래에서, StageSelect→로비는 위에서 들어온다. 타이틀·전투는 메뉴 순위가 없으므로 기존 즉시 전환을 유지한다. 같은 화면 재요청은 무시한다.
- 유지 동작: 기본 0.35초·SmoothStep·unscaledDeltaTime, 중복 전환·전환 중 입력 차단, 완료 후 입력 복구. 대상의 anchoredPosition을 nextOrigin으로 보관해 사용자가 조정한 시작 위치도 유지한다.
- 검증: 전체 런타임·Editor C# 컴파일 성공(경고 0·오류 0). 공통 부모·화면/팝업 위 형제 순서·씬 내부 참조·직렬화 이름·중복 ID 부재를 검사했다. 추가 실행 검증 결과는 아래 최종 검증 항목을 따른다.
- 미검증: 원본 프로젝트 직접 클릭, Android 실기기·터치·안전 영역, 독립 Windows·Android 빌드. 상점·장비·던전·특성 화면은 기존 기본 컨테이너로 전환을 검증하며 상세 화면을 새로 구현하지 않는다.
- 기능명세서: `C:/Users/user/OneDrive/프로젝트/11월 개인 프로젝트/기능명세서/20261119/공통 레터박스와 메뉴 슬라이드.md`.
- 최종 검증: 격리 Unity 6000.3.9f1 Play Mode에서 NAVIGATION_PASS checks=167 transitions=30 runtimeErrors=0 editorSearchErrors=1, 반환 코드 0. 여섯 메뉴의 서로 다른 30개 조합에서 진입 방향·Canvas 크기 기준 시작 좌표·입력 차단/복구·중복 요청 무시·최종 활성 상태·양쪽 원점 복원을 확인했다. 타이틀·로비 진입·전체 전환·설정 팝업·전투용 임시 컨테이너에서 띠가 활성화되고 공용 원점에 머무는지 확인했다. 실제 전투 상세 UI는 없는 상태이므로 전투는 임시 컨테이너의 활성 상태 검증이다. 1080×2340의 상하 여백, 1920×1080의 좌우 여백, 노치 안전 영역의 9:16 콘텐츠 계산을 검사했다. 이번 ScreenCapture 배치 호출은 PNG를 생성하지 않아 새 화면 캡처의 시각 검증은 수행하지 못했다. git diff --check 통과.
- 관련 코드 줄 수: SafeAreaLayout.cs 84줄, WindowManager.cs 124줄. 실행 로그: `C:/Users/user/.codex/visualizations/2026/11/19/01a1f286-4ab2-7bd0-9bcb-13feaf931814/navigation-validation.log`.

## FUNC-010 — UI 기본 앵커 프리셋과 수동 배치 유지

- 날짜: 2026-11-19
- 관련 기획: 일반 UI는 기본 앵커 프리셋과 위치·크기 값으로 편집하고 코드가 사용자 배치를 되돌리지 않도록 한다는 사용자 요청. 이전 기록의 임의 비율 앵커·전체 화면 레이어 확장 방식은 이번 변경으로 대체한다.
- 관련 파일: Assets/01_Scenes/00_Tilte_Scene.unity, Assets/02_Scripts/UI/SafeAreaLayout.cs, Assets/02_Scripts/UI/Title/TitleScreen.cs, Assets/02_Scripts/UI/Character/Editor/CharacterScreenSetup.cs.
- 사용한 기능·기술: RectTransform 기본 앵커(0·0.5·1), anchoredPosition·sizeDelta, Image.Type.Filled·fillAmount, 기존 CanvasScaler·안전 영역 계산.
- 선택 이유: 이미 9:16 콘텐츠 영역이 있으므로 일반 UI 전체를 임의 비율 앵커로 구성할 필요가 없다. 위치·크기를 Inspector에서 조정하기 쉽게 하고 화면 컨테이너의 실행 중 앵커 덮어쓰기를 제거한다.
- 배치 변환: 현재 저장된 씬의 커스텀 앵커 179개를 기본 프리셋으로 바꿨다. 가로·세로로 넓게 펼친 요소는 Stretch, 나머지는 가까운 고정 앵커를 사용하고 오프셋·크기로 원래 영역을 보존했다. 1080×1920 기준 변환 대상의 부모 내 위치·크기 최대 오차는 약 4.92×10^-8 Canvas 단위였다. 기존 기본 프리셋은 유지했다.
- 자동 덮어쓰기 제거: SafeAreaLayout의 _fullScreenLayers와 일반 화면·배경 anchorMin/anchorMax 변경 루프를 제거했다. 배경 2개의 AspectRatioFitter도 제거하고 안전 영역에 맞는 기본 전체 Stretch로 설정했다. 배경은 안전 영역을 채우며 바깥은 공통 레터박스가 가린다.
- 자동 영역: SafeArea는 중앙 고정, 위·아래 띠는 Top/Bottom Stretch, 좌·우 띠는 Left/Right Stretch 프리셋을 씬에 저장했다. SafeAreaLayout은 앵커를 쓰지 않고 위치·크기만 계산한다. _canvasSize 변경도 감지해 CanvasScaler 단위가 바뀌어도 다시 계산한다. 안전 영역과 띠 자체의 위치·크기는 계속 자동 관리 대상이다.
- 로딩 게이지: TitleScreen.SetProgress는 RectTransform.anchorMax 대신 Image.fillAmount를 변경한다. 씬 Image를 Filled/Horizontal로 설정하고 Unity 내장 UISprite를 연결했다. Sprite가 없는 Image는 fillAmount에 따른 메시가 생성되지 않으므로 기존 내장 리소스를 재사용했다. 새 이미지 파일은 만들지 않았다.
- 생성 도구: CharacterScreenSetup.SetBounds는 기본 프리셋과 부모 크기에 따른 anchoredPosition·sizeDelta를 기록한다. 제거된 전체 화면 레이어 필드에 접근하는 생성 코드는 삭제했다. 도구는 최초 생성 전용이며 기존 사용자 씬을 자동 재생성하지 않는다.
- 제어 구분: 일반 화면·배치용 UI의 앵커는 런타임 코드가 수정하지 않는다. WindowManager는 슬라이드 중 화면 위치를 바꾸고 시작 원점으로 복구한다. Unity Slider의 Fill·Handle은 Slider가 값을 표시하기 위해 관리하므로 런타임에 커스텀 앵커가 될 수 있다. 편집 대상은 슬라이더 루트이며 Fill·Handle은 자동 관리 자식이다. Play Mode에서 한 임시 편집은 Unity 기본 동작대로 종료 시 복원되므로 지속 배치는 Edit Mode에서 저장한다.
- 검증: 전체 C#·Editor 컴파일 성공(경고 0·오류 0). 격리 Unity 6000.3.9f1 Play Mode에서 ANCHOR_PASS checks=439 runtimeErrors=0 editorSearchErrors=1, 반환 코드 0. 일반 UI의 기본 프리셋, 로딩 0·25·50·100% 앵커 보존과 50% 실제 메시 너비, 배경 Fitter 부재, UI 이름·화면 컨테이너를 수동 편집한 뒤 안전 영역 재활성화·CanvasScaler 기준 변경에도 앵커·위치·크기가 유지되는지 확인했다. 편집한 화면 원점의 왕복 전환과 공통 레터박스 프리셋·9:16 영역도 확인했다.
- 미검증: 원본 에디터에서 사용자 직접 편집·클릭, Android 실기기·폰트·안전 영역, 독립 빌드. 기존 검색 인덱서 예외 1건과 임포트 단계의 에디터/Burst 문제는 게임 코드 오류와 구분했다.
- 로그: `C:/Users/user/.codex/visualizations/2026/11/19/01a1f286-4ab2-7bd0-9bcb-13feaf931814/anchor-validation.log`, `anchor-conversion-report.json`.
- 기능명세서: `C:/Users/user/OneDrive/프로젝트/11월 개인 프로젝트/기능명세서/20261119/UI 앵커 프리셋과 배치 유지.md`.
- 최종 정적 검증: 씬의 모든 직렬화 앵커가 0·0.5·1의 기본 프리셋 값임을 확인했다. ID 중복·내부 참조 이상이 없고 _fullScreenLayers·배경 Fitter 참조가 제거됐으며 git diff --check를 통과했다. 관련 코드 줄 수: SafeAreaLayout.cs 81, TitleScreen.cs 114, CharacterScreenSetup.cs 409.

## FUNC-011 — 공용 재화 표기와 테스트 은자

- 날짜 2026-11-19
- 모든 재화 UI에 CurrencyFormatter.Format을 사용한다. 0~999는 정수, 1,000부터 a, 1,000,000부터 b, 1,000,000,000부터 c로 표시한다. c 이상은 c 단위를 유지한다. 소수는 둘째 자리 아래를 버린다. 저장 및 차감에는 원래 정수 값을 사용한다.
- 로비와 캐릭터 화면의 은자·금자 및 성장 확인의 재화 보유량·비용에 적용했다.
- CharacterProgression.Initialize에서 새 저장과 미지급 기존 저장의 은자를 최소 1c로 맞춘다. HasReceivedTestSilver를 저장해 이후 소모분이 보충되지 않게 한다.
- 검증은 포맷 7개 사례, 초기 지급, 재초기화, 금자 및 기존 1c 초과 잔액 유지까지 통과했다. Unity Play Mode는 미실행이다.

## FUNC-012 — 성장 팝업 버튼과 재료 표시

- 날짜 2026-11-19
- CharacterPopUp의 선택 모드는 확인을 왼쪽, 취소를 오른쪽에 표시한다. 일반 메시지 모드는 가운데 확인 버튼만 표시한다.
- 재료는 이미지와 보유량 / 필요량으로 표시한다. 하나면 가운데, 둘이면 좌우 슬롯을 사용한다. 충분하면 흰색, 부족하면 빨간색이며 은자·금자는 공용 재화 포맷을 사용한다.
- 은자·금자 이미지는 상단바 아이콘을 재사용한다. 캐릭터 조각과 속성석은 캐릭터 초상과 기존 속성 아이콘으로 임시 표시한다.
- 관련 파일은 CharacterPopUp.cs, CharacterScreenSetup.cs, 00_Tilte_Scene.unity다. 팝업 버튼과 슬롯은 씬에서 편집할 수 있다.
- 전체 C# 컴파일 성공. 씬 참조 및 버튼 연결을 정적으로 확인했다. Unity Play Mode는 미실행이다.

## FUNC-013 — 성장 재료 아이콘 연결

- 날짜 2026-11-20
- 기존 Icon_Sheet_2의 빈 칸 34~45를 사용했다. 34~39는 카게부터 나기까지의 조각, 40~45는 무·화·수·목·토·풍 속성석이다.
- 원본 시트·좌표 JSON·개별 PNG와 Unity의 시트·분할 메타데이터를 맞췄다. 기존 슬롯의 픽셀과 Unity 참조를 유지했다.
- CharacterPopUp은 재료 상세 타입으로 스프라이트를 조회한다. 씬과 CharacterScreenSetup에 두 배열을 연결했다.
- 개별 PNG와 양쪽 시트의 픽셀 일치 및 기존 영역 보존을 검사했다. Unity Play Mode는 미실행이다.

## FUNC-014 — 장비 화면

- 날짜 2026-11-20
- 관련 기획은 UI_기조.xlsb의 <장비 화면> 기능표와 배치 그림이다. 장비 부위와 능력치 명칭은 장비_기조.xlsb와 EquipData.csv를 따른다.
- 00_Tilte_Scene.unity의 Canvas/SafeArea/WindowManager/Equipment_Window에 기존 장비 배경, 재화·설정, 좌우 8부위, 현재 선택된 캐릭터, 5열 스크롤 인벤토리와 하단 메뉴를 배치했다. 슬롯 위치는 RectTransform, 인벤토리 열 수와 간격은 Content의 GridLayoutGroup, 카드 모양은 InventorySlotTemplate에서 편집한다. 장비 카드는 소지 데이터에 따라 생성한다.
- WindowManager.Open(WindowType.Equipment) → EquipmentWindow.ResetUI → RefreshEquipment가 화면을 갱신한다. 장비 버튼은 EquipmentPopUp.ShowEquipment로 정보와 장착 또는 교체 버튼을 표시한다. Settings_PopUp과 메뉴는 기존 씬 참조를 사용한다.
- EquipmentPopUp.EquipSelected → EquipmentInventory.TryEquip → SaveLoadManager.SaveData 순서로 처리한다. SaveLoadData.Equipment의 각 항목은 장비 정의 ID, 레벨·등급과 장착 여부를 저장한다. 같은 정의 ID의 여러 소지 장비를 구분하고 교체된 장비는 인벤토리에 보관한다. 저장 실패 시 양쪽 장비의 장착 상태를 복구한다.
- 초기 지급은 별도로 추가하지 않았다. 장비가 없는 저장에는 빈 상태를 표시한다. 장비별 이미지는 이름 라벨로 임시 표시한다. 뽑기와 구매는 이번 장비 관리 범위에서 제외했다.
- 관련 파일은 00_Tilte_Scene.unity, EquipmentWindow.cs, EquipmentPopUp.cs, EquipmentInventory.cs, EquipmentInstanceData.cs, SaveLoadData.cs, GameManager.cs, EnumList.cs다.
- Unity 참조를 사용한 Roslyn C# 컴파일 및 씬 참조 정적 검사에 통과했다. 장비 74종의 CSV 참조 열, 8개 부위 슬롯, 팝업과 메뉴 이벤트, 계층의 참조와 앵커를 확인했다.
- Unity 6000.3.9f1 배치 모드는 프로젝트 경로 설정 직후 반환 코드 1로 종료했다. 별도 모델 실행 파일은 Windows 애플리케이션 제어 정책에서 차단됐다. Play Mode 입력·표시, 실제 저장·재실행과 Android 실기기는 미검증이다.


## FUNC-015 — 상점 화면

- 날짜 2026-11-21
- 관련 기획은 UI_기조.xlsb의 <상점 화면>과 <기능 팝업>, 장비_기조.xlsb의 뽑기 확률 및 N등급 지급 규칙이다.
- 상점 UI는 00_Tilte_Scene.unity의 Canvas/SafeArea/WindowManager/Shop_Window에 배치했다. ShopContent 아래 PremiumDraw, NormalDraw, GoldExchange의 RectTransform을 Inspector에서 수정할 수 있다. TopBar와 BottomBar는 기존 재화, 설정, 메뉴 구조를 사용한다. 패널과 버튼은 임시 배치다.
- WindowManager.Open(WindowType.Shop) → ShopWindow.ResetUI → RefreshShop 순서로 가격과 잔액을 갱신한다. Shop_Modal은 확률 목록, 거래 확인, 뽑기 결과 및 오류를 표시한다.
- ShopService.GetDrawCost와 GetExchangeCost는 DefaultData.csv의 구분으로 가격을 조회한다. 일반 1·10회는 은자 1,000·9,000, 고급 1·10회는 금자 10·90이다. 금자 1·10·100개 교환은 은자 1,000·9,500·90,000이다. 금자와 다이아는 같은 재화이며 기존 금자 저장 항목을 사용한다.
- ShopService.TryDraw는 EquipNomalGacha 또는 EquipSpecialGacha의 가중치로 뽑고, EquipmentInstanceData에 장비를 1레벨 N등급으로 지급한다. GetOdds는 같은 가중치를 사용한다. TryExchange는 은자를 차감하고 금자를 지급한다. 거래당 한 번 저장하며, 실패하면 잔액과 지급 목록을 복구한다.
- 관련 파일은 Assets/01_Scenes/00_Tilte_Scene.unity, Assets/02_Scripts/UI/Shop/ShopWindow.cs, Assets/02_Scripts/Data/ShopService.cs, Assets/02_Scripts/Manager/GameManager.cs, Assets/10_Data/DefaultData.csv와 해당 Unity 메타 파일이다.
- 검증 결과는 Unity 참조를 사용한 Roslyn C# 컴파일, 씬의 계층·컴포넌트와 CSV 연결 검사, DefaultData의 가격 7종 일치 확인이다. 각 뽑기 풀의 장비 74종과 가중치를 확인했다. 특수 장비 확률은 일반 2.2%, 고급 8.8%다.
- Unity 배치 모드가 프로젝트 경로 설정 직후 종료 코드 1로 종료됐다. 독립 거래 테스트 실행은 Windows 애플리케이션 제어 정책에 차단됐다. Play Mode 화면·클릭, 저장 후 재실행, 실기기는 미검증이다.


### 뽑기 연출 추가

- 날짜 2026-11-21
- ShopWindow.DrawEquipment → ShopService.TryDraw → ShopDrawPresentation.Play 순서로 실행한다. 뽑기 거래 저장을 완료한 뒤 획득 장비 ID 순서대로 검은 전체 화면에서 연출한다. 실패 시 기존 상점 오류 팝업 또는 결과 화면 하단에 메시지를 표시한다. 교환은 기존 확인 팝업을 사용한다.
- 장비 하나마다 0.3초 낙하, 0.3초 펼침, 0.4초 연기 소멸과 장비 공개를 진행한다. 장비 DB의 태생 구분으로 회색과 금색 두루마리를 고른다. 연출 화면을 클릭하거나 터치하면 남은 연출을 생략하고 전체 획득 결과를 표시한다.
- 1회 결과는 가운데, 10회 결과는 4·4·2로 배치한다. 돌아가기는 오버레이를 닫고, 다시 뽑기는 마지막 종류와 횟수로 새 거래를 시작한다. 연출 중에는 추가 뽑기 입력을 받지 않는다.
- 씬의 Canvas/Shop_Draw_Overlay에서 애니메이션과 결과 카드 배치를 편집할 수 있다. 프로젝트에 가져온 Etc_Sheet_2·3의 연출 스프라이트 5개와 기존 Icon_Sheet_0·1의 장비 아이콘 74개를 연결했다.
- 관련 파일은 Assets/02_Scripts/Data/ShopService.cs, Assets/02_Scripts/UI/Shop/ShopWindow.cs, Assets/02_Scripts/UI/Shop/ShopDrawPresentation.cs, Assets/01_Scenes/00_Tilte_Scene.unity다.
- 검증 결과는 Unity 참조를 사용한 Roslyn 컴파일과 오버레이 계층, 스프라이트 연결, 결과 위치 정적 검사다. Unity 배치 모드가 종료 코드 1로 종료돼 Play Mode에서 애니메이션과 입력을 확인하지 못했다.
- 특수 장비 S 배지 추가. 기존 Icon_Sheet_1의 SpecialS를 장비 아이콘 왼쪽 아래에 배치했다. 장비 태생 구분에 따라 연출 중 공개 이미지, 1회 결과와 10회 결과에 표시한다. C# 컴파일, 스프라이트와 배지 12개 참조를 확인했다. Play Mode는 미검증이다.
- 뽑기 확률 팝업을 세로 ScrollRect로 변경했다. 일반 장비 30종과 특수장비 44종을 가운데 정렬한 제목으로 나눴다. 행은 왼쪽 아이콘, 가운데 장비명, 오른쪽 확률 순서이며 팝업을 열면 목록 맨 위부터 표시한다. 기존 장비 아이콘과 추첨 가중치를 사용한다. 페이지 번호와 좌우 버튼을 제거했다. C# 컴파일, 뷰포트와 열 정렬, 양쪽 뽑기의 분류별 행 수와 확률 합계를 검사했다. Unity 배치 모드 종료로 Play Mode 입력과 실제 표시를 확인하지 못했다.

## 특성 화면

- 날짜 2026-11-21
- 관련 기획은 UI_기조.xlsb의 <특성 화면> 시안, 특성_기조.xlsb의 <특성>과 <특성DB>다. 중앙 1개와 속성별 25개씩 총 151개를 배치했다.
- `00_Tilte_Scene.unity`의 `Talent_Window`에서 화면을 편집한다. `TalentViewport/TalentTree/Nodes`에서 특성 위치와 스프라이트를 수정할 수 있다. TopBar와 BottomBar는 트리 바깥에 고정했다. 두 바를 각각 230.4 UI 단위로 맞춰 안전 영역 가운데에 1번 노드가 표시되도록 했다.
- 1번을 기준으로 12시 무속성, 2시 화속성, 4시 수속성, 6시 목속성, 8시 토속성, 10시 풍속성 순으로 뻗는다. 일반 효과 노드는 DB의 선행·후속 관계에 따라 가지 안에 배치했다. AbilityData.csv의 선행 조건 174개를 검은 선으로 표시한다. 수정된 선행 조건에 맞춰 6개 가지마다 25개 노드를 배치했다.
- 노드와 상세 팝업은 Etc_Sheet_0·1·2의 전용 육각형 스프라이트 19종을 원본 크기인 128×112로 표시한다. 1번은 TalentEmpty를 사용한다. 습득 전에는 RGB 밝기 0.72를 적용한다.
- `WindowManager.Open → TalentWindow.ResetUI → TalentNodeView.PlaceLink → TalentPanViewport.ResetPosition` 순서로 노드 위치에 맞춰 선을 다시 연결하고 화면을 1번 위치로 돌린다. 이동 한계는 축별 가장 바깥 노드 위치로 계산해 끝 노드의 중심이 뷰포트 중심까지 이동할 수 있게 했다.
- 노드를 누르면 특성 효과, 선행 특성마다의 습득 여부, 비용 및 습득 상태를 표시한다. `TalentService.TryLearn`은 모든 선행 조건과 잔액을 확인하고 기존 재화에서 비용을 차감한 뒤 `LearnedTalentIds`에 기록한다. 저장이 실패하면 재화와 습득 상태를 복구한다.
- 관련 파일은 `Assets/01_Scenes/00_Tilte_Scene.unity` 19655행, `Assets/02_Scripts/UI/Talent/TalentWindow.cs` 41행, `TalentNodeView.cs` 21행, `TalentPanViewport.cs` 16행, `Assets/02_Scripts/Data/TalentService.cs` 78행, `SaveLoadData.cs` 15행, `Assets/02_Scripts/Manager/GameManager.cs` 94행, `Assets/10_Data/AbilityData.csv`다.
- Unity 참조를 사용한 Roslyn 컴파일과 씬의 ID·스프라이트·버튼·선행 연결 검사를 통과했다. 독립 서비스 테스트에서 은자·금자 차감, 복수 선행 조건, 중복 습득 차단, 저장 실패 복구와 저장 모델의 습득 상태 재사용을 확인했다.
- Unity 배치 모드가 두 차례 모두 프로젝트 초기화 중 종료됐다. Play Mode 화면, 실제 마우스와 터치 드래그, 로컬 저장 파일 재실행, Android 실기기는 미검증이다. 전투 능력치 적용은 특성 화면 작업 범위 밖이다.
