using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class BattleInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private InputActionAsset _actions;
    [SerializeField] private RectTransform _pad;
    [SerializeField] private RectTransform _handle;

    private InputAction _move;
    private int? _pointer;
    private Vector2 _touch;

    public Vector2 Direction => Vector2.ClampMagnitude(_touch + (_move?.ReadValue<Vector2>() ?? Vector2.zero), 1f);

    private void OnEnable()
    {
        _move = _actions == null ? CreateMoveAction() : _actions.FindAction("Player/Move", true).Clone();
        _move.Enable();
    }

    private static InputAction CreateMoveAction()
    {
        InputAction move = new("Move");
        move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");
        return move;
    }

    private void OnDisable()
    {
        _move?.Dispose();
        _move = null;
        ResetStick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_pointer.HasValue || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }
        _pointer = eventData.pointerId;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_pointer != eventData.pointerId
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_pad, eventData.position, eventData.pressEventCamera, out Vector2 local))
        {
            return;
        }
        float radius = Mathf.Min(_pad.rect.width, _pad.rect.height) * .36f;
        _touch = Vector2.ClampMagnitude(local / Mathf.Max(1f, radius), 1f);
        _handle.anchoredPosition = _touch * radius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_pointer == eventData.pointerId)
        {
            ResetStick();
        }
    }

    public void ResetStick()
    {
        _pointer = null;
        _touch = Vector2.zero;
        if (_handle != null)
        {
            _handle.anchoredPosition = Vector2.zero;
        }
    }
}
