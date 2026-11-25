using System;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class WorkshopWardrobeSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float MinimumTravel = 72f;
    private const float Cooldown = 0.22f;

    private Action<int> cycle;
    private Func<bool> canMove;
    private Action<Vector2> move;
    private RectTransform surface;
    private Vector2 origin;
    private Vector2 previous;
    private int? activePointer;
    private bool moving;
    private float nextSwipe;

    internal void Configure(Action<int> action, Func<bool> canMove = null, Action<Vector2> move = null)
    {
        surface = (RectTransform)transform;
        cycle = action;
        this.canMove = canMove;
        this.move = move;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (surface == null || activePointer.HasValue || eventData.button != PointerEventData.InputButton.Left) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, eventData.pressPosition,
                eventData.pressEventCamera, out origin)) return;
        activePointer = eventData.pointerId;
        moving = canMove?.Invoke() == true;
        previous = origin;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!moving || activePointer != eventData.pointerId ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, eventData.position,
                eventData.pressEventCamera, out Vector2 destination)) return;
        Vector2 delta = destination - previous;
        previous = destination;
        if (delta != Vector2.zero) move?.Invoke(delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (activePointer != eventData.pointerId) return;
        if (moving) OnDrag(eventData);
        activePointer = null;
        if (moving || Time.unscaledTime < nextSwipe || !RectTransformUtility.ScreenPointToLocalPointInRectangle(surface,
                eventData.position, eventData.pressEventCamera, out Vector2 destination)) return;

        Vector2 travel = destination - origin;
        if (Mathf.Abs(travel.x) < MinimumTravel || Mathf.Abs(travel.x) < Mathf.Abs(travel.y) * 1.25f) return;
        nextSwipe = Time.unscaledTime + Cooldown;
        cycle?.Invoke(travel.x < 0f ? 1 : -1);
    }

    private void OnDisable() => activePointer = null;
}
