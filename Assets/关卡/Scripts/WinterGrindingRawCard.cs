using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>将工作台左侧的染料原料拖入钵口。</summary>
public class WinterGrindingRawCard : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public WinterDyeMaterial Material { get; private set; }

    private WinterGrindingOverlay _overlay;
    private RectTransform _dragIconRt;
    private bool _dragging;

    public void Init(WinterGrindingOverlay overlay, WinterDyeMaterial material)
    {
        _overlay = overlay;
        Material = material;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = _overlay != null && _overlay.CanAcceptMaterial;
        if (!_dragging) return;

        // 独立图标随指针移动，原料落入钵口前保留在台面上。
        _dragIconRt = _overlay.CreateDragIcon(Material.Icon);
        _dragIconRt.position = eventData.position;
        _overlay.SetStatus(WinterGrindingOverlay.PlaceMaterialHint);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || _dragIconRt == null) return;
        _dragIconRt.position = eventData.position;
        _overlay.HighlightMortar(_overlay.CanAcceptMaterial && _overlay.IsOverMortar(eventData.position));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging) return;
        _dragging = false;

        DestroyDragIcon();

        if (_overlay == null) return;
        _overlay.HighlightMortar(false);
        if (!_overlay.CanAcceptMaterial) return;
        if (_overlay.IsOverMortar(eventData.position) && _overlay.AcceptMaterial(this)) return;
        _overlay.SetStatus("原料已放回台面。拖到钵口再松手。");
    }

    private void DestroyDragIcon()
    {
        if (_dragIconRt != null)
        {
            Destroy(_dragIconRt.gameObject);
            _dragIconRt = null;
        }
    }

    private void OnDisable()
    {
        _dragging = false;
        DestroyDragIcon();
    }
}
