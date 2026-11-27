using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>A cut piece can only snap into its matching pattern.</summary>
public sealed class WorkshopTailorPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform piece;
    private RectTransform target;
    private RectTransform board;
    private Vector2 home;
    private Vector2 lastPointer;
    private Action<bool> onDrop;
    private bool placed;
    private int? draggingPointer;

    internal void Bind(RectTransform target, Action<bool> onDrop)
    {
        this.target = target;
        this.onDrop = onDrop;
        piece = (RectTransform)transform;
        board = (RectTransform)piece.parent;
        home = piece.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (placed || draggingPointer.HasValue || eventData.button != PointerEventData.InputButton.Left) return;
        if (!TryPointer(eventData.pressPosition, eventData.pressEventCamera, out lastPointer)) return;

        draggingPointer = eventData.pointerId;
        piece.SetAsLastSibling();
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingPointer != eventData.pointerId) return;
        if (!TryPointer(eventData.position, eventData.pressEventCamera, out Vector2 point)) return;

        piece.anchoredPosition += point - lastPointer;
        lastPointer = point;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggingPointer != eventData.pointerId) return;
        draggingPointer = null;
        placed = RectTransformUtility.RectangleContainsScreenPoint(target, eventData.position, eventData.pressEventCamera);
        if (placed)
        {
            piece.anchoredPosition = target.anchoredPosition;
            piece.sizeDelta = target.sizeDelta;
            GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        }
        else
        {
            piece.anchoredPosition = home;
        }

        onDrop?.Invoke(placed);
    }

    private bool TryPointer(Vector2 screen, Camera camera, out Vector2 position)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(board, screen, camera, out position);
    }
}
