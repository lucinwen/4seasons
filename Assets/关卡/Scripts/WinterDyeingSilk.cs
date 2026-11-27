using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>将木托盘里的缬拖入染缸。</summary>
public class WinterDyeingSilk : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private WinterDyeingOverlay _overlay;
    private RectTransform _rt;
    private RectTransform _parent;
    private Vector2 _homeAnchored;
    private Vector2 _pointerOffset;
    private bool _dragging;

    public void Init(WinterDyeingOverlay overlay)
    {
        _overlay = overlay;
        _rt = GetComponent<RectTransform>();
        _parent = (RectTransform)_rt.parent;
        _homeAnchored = _rt.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_overlay == null || !_overlay.CanDragSilk)
        {
            if (_overlay != null && !_overlay.IsDyeing && !_overlay.HasFinishedCloth)
                _overlay.SetStatus("先点左侧染料，再把缬拖入染缸。");
            _dragging = false;
            return;
        }

        _dragging = RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent,
            eventData.position, eventData.pressEventCamera, out Vector2 pointer);
        if (!_dragging) return;
        _pointerOffset = _rt.anchoredPosition - pointer;
        transform.SetAsLastSibling();
        _overlay.SetStatus("把缬拖到缸口，松手染色。");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent,
                eventData.position, eventData.pressEventCamera, out Vector2 pointer))
            _rt.anchoredPosition = pointer + _pointerOffset;
        _overlay.HighlightDropTarget(_overlay.IsOverVat(eventData.position));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging) return;
        _dragging = false;
        _overlay.HighlightDropTarget(false);

        if (_overlay.IsOverVat(eventData.position) && _overlay.TryDyeCloth())
            return;

        _rt.anchoredPosition = _homeAnchored;
        _overlay.SetStatus("缬已放回木托盘。拖到缸口再松手。");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_overlay != null && eventData.button == PointerEventData.InputButton.Left)
            _overlay.CollectFinishedCloth();
    }
}
