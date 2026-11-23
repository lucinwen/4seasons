using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>研磨界面右侧的研磨棒，拖到钵体上来回滑动即可研磨。</summary>
public class WinterGrindingRod : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float GrindSoundDistance = 20f;
    private const float GrindSoundInterval = 0.20f;

    private WinterGrindingOverlay _overlay;
    private RectTransform _rt;
    private Vector2 _homeAnchored;
    private Quaternion _homeRotation;
    private Vector2 _lastMortarPoint;
    private bool _wasOverMortar;
    private bool _dragging;
    private float _distanceSinceGrindSound;
    private float _nextGrindSoundTime;

    public void Init(WinterGrindingOverlay overlay)
    {
        _overlay = overlay;
        _rt = GetComponent<RectTransform>();
        _homeAnchored = _rt.anchoredPosition;
        _homeRotation = _rt.localRotation;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_overlay == null || !_overlay.HasMaterial)
        {
            if (_overlay != null) _overlay.SetStatus("请先把染料原料拖入钵体");
            _dragging = false;
            return;
        }

        _dragging = true;
        _distanceSinceGrindSound = 0f;
        _wasOverMortar = _overlay.TryGetMortarPoint(eventData.position, eventData.pressEventCamera,
            out _lastMortarPoint);
        _rt.localRotation = Quaternion.Euler(0f, 0f, 16f);
        transform.SetAsLastSibling();
        _overlay.SetStatus(WinterGrindingOverlay.UseRodHint);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || !_overlay.HasMaterial) return;

        _rt.position = eventData.position;
        bool overMortar = _overlay.TryGetMortarPoint(eventData.position, eventData.pressEventCamera,
            out Vector2 point);
        _overlay.HighlightMortar(overMortar);
        // Only measure strokes inside the bowl, in worktable units independent of window size.
        if (overMortar && _wasOverMortar)
        {
            float distance = Vector2.Distance(_lastMortarPoint, point);
            _overlay.AddGrind(distance);
            if (_overlay.HasMaterial) PlayGrindSound(distance);
        }
        else
        {
            _distanceSinceGrindSound = 0f;
        }
        _lastMortarPoint = point;
        _wasOverMortar = overMortar;
    }

    private void PlayGrindSound(float distance)
    {
        if (distance <= 0f) return;

        _distanceSinceGrindSound += distance;
        float now = Time.unscaledTime;
        if (_distanceSinceGrindSound < GrindSoundDistance || now < _nextGrindSoundTime) return;

        WorkshopAudioFeedback.Grind();
        _distanceSinceGrindSound = 0f;
        _nextGrindSoundTime = now + GrindSoundInterval;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging) return;
        _dragging = false;
        _overlay.HighlightMortar(false);

        if (!_overlay.IsOverMortar(eventData.position)) ResetPosition();
    }

    internal void ResetPosition()
    {
        _rt.anchoredPosition = _homeAnchored;
        _rt.localRotation = _homeRotation;
    }
}
