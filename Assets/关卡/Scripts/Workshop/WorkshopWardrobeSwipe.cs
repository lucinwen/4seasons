using System;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class WorkshopWardrobeSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float MinimumTravel = 72f;
    private const float Cooldown = 0.22f;

    private Action<int> cycle;
    private RectTransform surface;
    private Vector2 origin;
    private int? activePointer;
    private float nextSwipe;

    internal void Configure(Action<int> action)
    {
        surface = (RectTransform)transform;
        cycle = action;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (surface == null || activePointer.HasValue || eventData.button != PointerEventData.InputButton.Left) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, eventData.pressPosition,
                eventData.pressEventCamera, out origin))
            activePointer = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (activePointer != eventData.pointerId) return;
        activePointer = null;
        if (Time.unscaledTime < nextSwipe || !RectTransformUtility.ScreenPointToLocalPointInRectangle(surface,
                eventData.position, eventData.pressEventCamera, out Vector2 destination)) return;

        Vector2 travel = destination - origin;
        if (Mathf.Abs(travel.x) < MinimumTravel || Mathf.Abs(travel.x) < Mathf.Abs(travel.y) * 1.25f) return;
        nextSwipe = Time.unscaledTime + Cooldown;
        cycle?.Invoke(travel.x < 0f ? 1 : -1);
    }

    private void OnDisable() => activePointer = null;
}
