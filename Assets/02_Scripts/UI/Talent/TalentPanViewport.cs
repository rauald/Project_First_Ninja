using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class TalentPanViewport : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform _content;

    private Vector2 _minimum;
    private Vector2 _maximum;
    private Vector2 _dragOrigin;
    private Vector2 _pointerOrigin;
    private int? _pointerId;

    public void ResetPosition(Vector2 center, IReadOnlyList<TalentNodeView> nodes)
    {
        _minimum = _maximum = -center;
        foreach (TalentNodeView node in nodes)
        {
            _minimum = Vector2.Min(_minimum, -node.Position);
            _maximum = Vector2.Max(_maximum, -node.Position);
        }

        _pointerId = null;
        _content.anchoredPosition = -center;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_pointerId.HasValue || eventData.button != PointerEventData.InputButton.Left
            || !TryGetLocalPoint(eventData, eventData.pressPosition, out _pointerOrigin))
        {
            return;
        }

        _pointerId = eventData.pointerId;
        _dragOrigin = _content.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_pointerId != eventData.pointerId || !TryGetLocalPoint(eventData, eventData.position, out Vector2 pointer))
        {
            return;
        }

        Vector2 position = _dragOrigin + pointer - _pointerOrigin;
        Vector2 clamped = Vector2.Min(Vector2.Max(position, _minimum), _maximum);
        _content.anchoredPosition = clamped;
        _dragOrigin = clamped;
        _pointerOrigin = pointer;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_pointerId == eventData.pointerId)
        {
            _pointerId = null;
        }
    }

    private bool TryGetLocalPoint(PointerEventData eventData, Vector2 position, out Vector2 local)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, position, eventData.pressEventCamera, out local);
    }
}
