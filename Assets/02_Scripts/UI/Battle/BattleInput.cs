using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public sealed class BattleInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
{
    // 키보드와 게임패드 이동 바인딩 원본이다.
    [SerializeField] private InputActionAsset _actions;
    // 터치 시작 지점에 나타나는 조이스틱 배경이다.
    [SerializeField] private RectTransform _pad;
    // 이동 방향과 강도를 표시하는 손잡이다.
    [SerializeField] private RectTransform _handle;

    // 전투 화면이 활성화된 동안 사용하는 이동 액션이다.
    private InputAction _move;
    // 조이스틱을 처음 누른 포인터만 추적한다.
    private int? _pointer;
    // 조이스틱 반경을 기준으로 정규화한 이동 방향이다.
    private Vector2 _touch;

    // 터치와 물리 입력을 합치고 최대 이동 강도를 제한한다.
    public Vector2 Direction => Vector2.ClampMagnitude(_touch + (_move?.ReadValue<Vector2>() ?? Vector2.zero), 1f);

    // 화면을 열면 물리 입력을 활성화하고 조이스틱을 숨긴다.
    private void OnEnable()
    {
        _move = _actions == null ? CreateMoveAction() : _actions.FindAction("Player/Move", true).Clone();
        _move.Enable();
        ResetStick();
    }

    // 바인딩 에셋이 없을 때 방향키와 게임패드로 이동할 수 있게 한다.
    private static InputAction CreateMoveAction()
    {
        InputAction move = new("Move");
        move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");
        return move;
    }

    // 화면이 닫힐 때 복제한 입력과 터치 상태를 해제한다.
    private void OnDisable()
    {
        _move?.Dispose();
        _move = null;
        ResetStick();
    }

    // 터치가 취소되는 포커스 전환에서는 포인터 상태를 초기화한다.
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            ResetStick();
        }
    }

    // 짧게 움직여도 가상 조이스틱이 즉시 반응한다.
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    // 첫 터치 위치를 조이스틱의 중심으로 잡는다.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_pointer.HasValue || eventData.button != PointerEventData.InputButton.Left
            || (!Application.isEditor && eventData is ExtendedPointerEventData input
                && input.pointerType != UIPointerType.Touch))
        {
            return;
        }

        // 안전 영역의 부모 좌표계에서 터치 위치를 구한다.
        RectTransform parent = (RectTransform)_pad.parent;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position,
            eventData.pressEventCamera, out Vector2 center))
        {
            return;
        }

        // 손을 댄 순간은 중립 입력이며 이후 드래그만 이동을 발생시킨다.
        _pointer = eventData.pointerId;
        _pad.localPosition = new Vector3(center.x, center.y, _pad.localPosition.z);
        _pad.gameObject.SetActive(true);
    }

    // 해당 포인터의 변위를 읽고 조이스틱 반경 안에 손잡이를 배치한다.
    public void OnDrag(PointerEventData eventData)
    {
        if (_pointer != eventData.pointerId
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_pad, eventData.position,
                eventData.pressEventCamera, out Vector2 local))
        {
            return;
        }

        float radius = Mathf.Min(_pad.rect.width, _pad.rect.height) * .36f;
        _touch = Vector2.ClampMagnitude(local / Mathf.Max(1f, radius), 1f);
        _handle.anchoredPosition = _touch * radius;
    }

    // 조이스틱을 조작 중인 손가락을 떼면 이동을 종료한다.
    public void OnPointerUp(PointerEventData eventData)
    {
        if (_pointer == eventData.pointerId)
        {
            ResetStick();
        }
    }

    // 포인터 소유권과 이동 방향을 초기화하고 조이스틱을 숨긴다.
    public void ResetStick()
    {
        _pointer = null;
        _touch = Vector2.zero;
        _handle.anchoredPosition = Vector2.zero;
        _pad.gameObject.SetActive(false);
    }
}
