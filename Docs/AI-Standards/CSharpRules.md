# 코드 컨벤션

## 표기법의 종류
- 파스칼 표기법
    - 대문자로 시작하며 띄어쓰기 대신 대문자로 구분한다.
        - ex) ThisIsCollectJellies

- 카멜 표기법
    - 소문자로 시작하며 띄어쓰기 대신 대문자로 구분한다.
        - ex) thisIsCollectJellies

- 스네이크 표기법
    - 소문자만 사용하며 띄어쓰기 대신 언더바를 사용한다.
        - ex) this_is_collect_jellies

- 스크림 스네이크 표기법
    - 대문자만 사용하며 띄어쓰기 대신 언더바를 사용한다.
        - ex) THIS_IS_COLLECT_JELLIES

- 헝가리안 표기법
    - 접두어에 자료형을 붙이고 사용하며 띄어쓰기 대신 대소문자로 구분한다.
        - ex) interface → IDamageable

## 코드 표기법
- 클래스와 구조체는 파스칼 표기법을 사용한다.
    - 클래스 → public class Player { } 
    - 구조체 → public struct PlayerData { }

- private 변수는 카멜 표기법을  사용한다. (단, 필드형은 _를 붙인다.)
    - 전역변수 private → _thisIsCollectJellies
    - 지역변수 private → thisIsCollectJellies

- public 변수는 파스칼 표기법을 사용한다.
    - 전역변수 public → ThisIsCollectJellies

- 매개변수는 카멜 표기법을 사용한다.
    - 매개변수 public void ThisCollectJellies(int index)

- 메서드의 이름은 파스칼 표기법을 사용하며, 동사(명령형) + 명사(목적어)의 형태로 짓는다.
    - 명사 부분에 여러개가 들어간다면, 누가 - 무엇을 - 왜 순서로 작성한다.
        - ex) GetJelliesSkinCost → Get 젤리 스킨 가격

- bool 이름에는 의미에 맞게 is, has, can을 사용한다. 대소문자와 밑줄은 선언 위치의 명명 규칙을 따른다.
    - is 사용법
        - 어떤 상태인지를 구별한다.
            - ex) isDead → 죽은 상태인가?
            - ex) isMove → 움직이고 있는가?
            - ex) isJump → 점프 상태 인가?

    - has 사용법
        - 소유하고 있는지를 구별한다.
            - ex) hasItem → 해당 아이템을 가지고 있는가?
    - can 사용법
        - 조건을 만족했는지 구별한다.
            - ex) canJump →  점프할 수 있는가?

- 상수 이름은 모두 스크림 스네이크 표기법을 사용한다.
    - ex) private const string ITEM_NAME = "젤리 수집";

- 초기화 후 재할당할 필요가 없는 필드에는 readonly를 사용한다.
    - ex) private readonly List<int> _collectedIds = new List<int>();

- 지원하지 않는 입력을 별도로 처리해야 할 때 default 분기를 작성한다.

- Enum 문은 모든 곳에 Type 을 뒤에 붙이고, 첫 부분에 None = -1, 마지막에 Max 를 넣는다.
    - Max 를 넣는 이유는 enum 의 최대 길이를 알기 위함이다.
        - ex) public enum JelliesSkinType { None = -1, Basic = 0, Max }

- Awake에서 접근 가능한 자체 컴포넌트는 Awake에서 참조를 설정할 수 있다. 나중에 생성되거나 주입되는 참조는 해당 초기화 흐름에서 설정한다.

-  if 문 만족시 바로 return 할 경우 같은 줄이 아닌 다음 줄에 return; 을 적는다.
    - 유니티 c# 코드 읽기 오류로 가끔 같은 줄에 있는 return 을 못 읽고 넘어가는 오류가 있다.

## 코드 원칙
- 클래스명과 파일명을 일치시킨다.
- Inspector에 노출할 필드는 기본적으로 [SerializeField] private 로 작성한다.
- 입력 처리와 게임 동작을 분리한다.
- 현재 요구사항에 비해 과도한 구조를 만들지 않는다.