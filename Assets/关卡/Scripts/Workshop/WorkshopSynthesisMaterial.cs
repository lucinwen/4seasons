using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class WorkshopSynthesisMaterial : MonoBehaviour, IBeginDragHandler, IDragHandler,
    IEndDragHandler, IPointerClickHandler
{
    private WorkshopSynthesis synthesis;
    private RectTransform table;
    private RectTransform piece;
    private Vector2 offset;
    private int index;
    private int? pointer;

    internal void Bind(WorkshopSynthesis synthesis, RectTransform table, UnityEngine.UI.RawImage image, int index)
    {
        this.synthesis = synthesis;
        this.table = table;
        this.index = index;
        piece = image.rectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (pointer.HasValue || eventData.button != PointerEventData.InputButton.Left) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(table, eventData.pressPosition,
            eventData.pressEventCamera, out Vector2 press) || !synthesis.BeginMaterialDrag(index)) return;

        pointer = eventData.pointerId;
        offset = piece.anchoredPosition - press;
        piece.SetAsLastSibling();
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (pointer != eventData.pointerId) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(table, eventData.position,
            eventData.pressEventCamera, out Vector2 point)) return;

        piece.anchoredPosition = point + offset;
        synthesis.HoverMaterial(index, eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (pointer != eventData.pointerId) return;
        pointer = null;
        synthesis.DropMaterial(index, eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
            synthesis.ReadMaterial(index);
    }

    private void OnDisable()
    {
        if (!pointer.HasValue) return;
        pointer = null;
        if (synthesis != null) synthesis.CancelMaterialDrag(index);
    }
}
